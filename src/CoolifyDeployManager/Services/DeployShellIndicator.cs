// Copyright (c) 2026 Cris0338. Licensed under the CDM Free Redistribution License v1.0.
// Reuses the per-card Coolify deployment state; never polls or triggers deployments.
// Pre-render all frames once: timer ticks do not allocate GDI icons/handles.
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Shell;
using System.Windows.Threading;
using Forms = System.Windows.Forms;
using WpfMedia = System.Windows.Media;

namespace CoolifyDeployManager;

internal sealed class DeployShellIndicator : IDisposable
{
    private const int FrameCount = 12;
    private readonly Window window;
    private readonly DispatcherTimer clock;
    private readonly WpfMedia.ImageSource[] taskbarFrames = new WpfMedia.ImageSource[FrameCount];
    private Icon[]? trayFrames;
    private Forms.NotifyIcon? tray;
    private Icon? normalIcon;
    private int frame;
    private bool active;
    private bool disposed;

    internal bool IsActive => active;

    internal DeployShellIndicator(Window window)
    {
        this.window = window;
        window.TaskbarItemInfo ??= new TaskbarItemInfo();
        for (int i = 0; i < FrameCount; i++)
            taskbarFrames[i] = BuildTaskbarFrame(i);

        clock = new DispatcherTimer(DispatcherPriority.Background, window.Dispatcher)
        {
            Interval = TimeSpan.FromMilliseconds(120)
        };
        clock.Tick += (_, _) => Advance();
    }

    internal void AttachTray(Forms.NotifyIcon icon, Icon defaultIcon)
    {
        if (disposed) throw new ObjectDisposedException(nameof(DeployShellIndicator));
        tray = icon;
        normalIcon = defaultIcon;
        if (active)
        {
            EnsureTrayFrames();
            tray.Icon = trayFrames![frame];
        }
        else tray.Icon = defaultIcon;
    }

    internal void SetActive(bool isActive)
    {
        if (disposed || active == isActive) return;
        active = isActive;
        if (active)
        {
            frame = 0;
            if (tray is not null) EnsureTrayFrames();
            Present();
            clock.Start();
        }
        else
        {
            clock.Stop();
            window.TaskbarItemInfo!.Overlay = null;
            if (tray is not null && normalIcon is not null)
                tray.Icon = normalIcon;
        }
    }

    private void Advance()
    {
        if (!active || disposed) return;
        frame = (frame + 1) % FrameCount;
        Present();
    }

    internal void AdvanceForSmoke() => Advance();

    private void Present()
    {
        if (!active) return;
        // A hidden startup-to-tray window has no taskbar button; setting the
        // overlay never makes one appear, and it is restored with the window.
        window.TaskbarItemInfo!.Overlay = taskbarFrames[frame];
        if (tray is not null)
        {
            EnsureTrayFrames();
            tray.Icon = trayFrames![frame];
        }
    }

    private void EnsureTrayFrames()
    {
        if (trayFrames is not null) return;
        var original = normalIcon ?? SystemIcons.Application;
        var frames = new Icon[FrameCount];
        try
        {
            for (int i = 0; i < frames.Length; i++)
                frames[i] = BuildTrayFrame(original, i);
            trayFrames = frames;
        }
        catch
        {
            foreach (var icon in frames) icon?.Dispose();
            throw;
        }
    }

    private static WpfMedia.ImageSource BuildTaskbarFrame(int index)
    {
        var circle = new WpfMedia.EllipseGeometry(new System.Windows.Point(8, 8), 6.3, 6.3);
        var backdrop = new WpfMedia.GeometryDrawing(
            new WpfMedia.SolidColorBrush(WpfMedia.Color.FromArgb(238, 12, 26, 37)),
            new WpfMedia.Pen(new WpfMedia.SolidColorBrush(
                WpfMedia.Color.FromArgb(220, 235, 245, 250)), 0.85),
            circle);

        const double radius = 4.9;
        double start = index * 360.0 / FrameCount - 90;
        static System.Windows.Point OnRing(double degrees)
        {
            const double r = 4.9;
            double radians = degrees * Math.PI / 180;
            return new System.Windows.Point(8 + r * Math.Cos(radians),
                8 + r * Math.Sin(radians));
        }
        var arc = new WpfMedia.StreamGeometry();
        using (var pen = arc.Open())
        {
            pen.BeginFigure(OnRing(start), isFilled: false, isClosed: false);
            pen.ArcTo(OnRing(start + 255), new System.Windows.Size(radius, radius), 0,
                isLargeArc: true, WpfMedia.SweepDirection.Clockwise,
                isStroked: true, isSmoothJoin: false);
        }
        arc.Freeze();
        var stroke = new WpfMedia.Pen(
            new WpfMedia.SolidColorBrush(WpfMedia.Color.FromRgb(30, 232, 137)), 2.3)
        {
            StartLineCap = WpfMedia.PenLineCap.Round,
            EndLineCap = WpfMedia.PenLineCap.Round
        };
        var group = new WpfMedia.DrawingGroup();
        group.Children.Add(backdrop);
        group.Children.Add(new WpfMedia.GeometryDrawing(null, stroke, arc));
        group.Freeze();
        var result = new WpfMedia.DrawingImage(group);
        result.Freeze();
        return result;
    }

    private static Icon BuildTrayFrame(Icon original, int index)
    {
        using var bitmap = new Bitmap(32, 32, PixelFormat.Format32bppArgb);
        using (var graphics = Graphics.FromImage(bitmap))
        {
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            graphics.Clear(Color.Transparent);
            graphics.DrawIcon(original, new Rectangle(0, 0, 32, 32));
            using var backdrop = new SolidBrush(Color.FromArgb(238, 12, 26, 37));
            using var rim = new Pen(Color.FromArgb(230, 230, 245, 248), 1.2f);
            using var stroke = new Pen(Color.FromArgb(255, 30, 232, 137), 2.8f)
            {
                StartCap = LineCap.Round,
                EndCap = LineCap.Round
            };
            graphics.FillEllipse(backdrop, 15, 15, 16, 16);
            graphics.DrawEllipse(rim, 15, 15, 16, 16);
            graphics.DrawArc(stroke, 18.5f, 18.5f, 9f, 9f,
                (float)(index * 360.0 / FrameCount - 90), 255);
        }

        var handle = bitmap.GetHicon();
        try
        {
            using var borrowed = Icon.FromHandle(handle);
            return (Icon)borrowed.Clone();
        }
        finally { DestroyIcon(handle); }
    }

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DestroyIcon(IntPtr icon);

    public void Dispose()
    {
        if (disposed) return;
        SetActive(false);
        disposed = true;
        clock.Stop();
        tray = null;
        normalIcon = null;
        if (trayFrames is not null)
        {
            foreach (var icon in trayFrames) icon.Dispose();
            trayFrames = null;
        }
    }
}
