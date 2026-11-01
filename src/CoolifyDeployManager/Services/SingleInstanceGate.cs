// Copyright (c) 2026 Cris0338. Licensed under the CDM Free Redistribution License v1.0.
// One instance per exact personal executable path. A second launch signals the
// first window to reopen instead of racing for the same local SSH tunnel.
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace CoolifyDeployManager;

internal sealed class SingleInstanceGate : IDisposable
{
    private readonly Mutex mutex;
    private readonly EventWaitHandle reopen;
    private readonly EventWaitHandle stop = new(false, EventResetMode.ManualReset);
    private readonly Task listener;
    private bool disposed;

    private SingleInstanceGate(Mutex mutex, EventWaitHandle reopen, Action restore)
    {
        this.mutex = mutex;
        this.reopen = reopen;
        listener = Task.Run(() =>
        {
            try
            {
                while (WaitHandle.WaitAny(new WaitHandle[] { reopen, stop }) == 0)
                    restore();
            }
            catch (ObjectDisposedException) { /* application is exiting */ }
        });
    }

    internal static string IdentityFor(string executable)
    {
        var path = Path.GetFullPath(executable).ToUpperInvariant();
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(path)))[..24];
    }

    internal static SingleInstanceGate? TryCreate(string executable, Action restore)
    {
        var id = IdentityFor(executable);
        var mutex = new Mutex(false, @"Local\CDM-" + id, out bool createdNew);
        if (!createdNew)
        {
            mutex.Dispose();
            // The first process may still be creating its event; retry briefly.
            for (int i = 0; i < 10; i++)
            {
                try
                {
                    using var signal = EventWaitHandle.OpenExisting(@"Local\CDM-Reopen-" + id);
                    signal.Set();
                    break;
                }
                catch (WaitHandleCannotBeOpenedException)
                {
                    Thread.Sleep(100);
                }
            }
            return null;
        }

        try
        {
            var signal = new EventWaitHandle(
                false, EventResetMode.AutoReset, @"Local\CDM-Reopen-" + id);
            return new SingleInstanceGate(mutex, signal, restore);
        }
        catch
        {
            mutex.Dispose();
            throw;
        }
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        stop.Set();
        // Listener is asynchronous and never holds WPF's dispatcher thread.
        // Allow it to observe stop before destroying its wait handles.
        try { listener.Wait(TimeSpan.FromSeconds(2)); } catch (AggregateException) { }
        reopen.Dispose();
        stop.Dispose();
        mutex.Dispose();
    }
}
