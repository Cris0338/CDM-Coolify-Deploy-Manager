// Copyright (c) 2026 Cris0338. Licensed under the MIT License.
// Portable, password-encrypted application settings; the executable is never modified.
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace CoolifyDeployManager;

internal sealed class PortableVault
{
    private const int CurrentFormat = 1;
    private const int Pbkdf2Iterations = 600_000;
    private static readonly byte[] AuthenticatedContext = Encoding.UTF8.GetBytes("CDM portable profile v1");
    private readonly string path;

    public PortableVault(string filePath) => path = filePath;
    public bool Exists => File.Exists(path);

    public string Read(string passphrase)
    {
        if (new FileInfo(path).Length > 4_000_000)
            throw new InvalidDataException("The portable profile is unexpectedly large.");

        var envelope = JsonSerializer.Deserialize<Envelope>(File.ReadAllText(path, Encoding.UTF8))
            ?? throw new InvalidDataException("The portable profile is empty.");
        if (envelope.Version != CurrentFormat ||
            envelope.Iterations < 300_000 || envelope.Iterations > 2_000_000 ||
            envelope.Algorithm != "AES-256-GCM/PBKDF2-SHA256")
            throw new InvalidDataException("Unsupported portable profile format.");

        byte[] salt = Convert.FromBase64String(envelope.Salt);
        byte[] nonce = Convert.FromBase64String(envelope.Nonce);
        byte[] tag = Convert.FromBase64String(envelope.Tag);
        byte[] cipher = Convert.FromBase64String(envelope.Ciphertext);
        if (salt.Length != 16 || nonce.Length != 12 || tag.Length != 16 || cipher.Length > 2_000_000)
            throw new InvalidDataException("The portable profile has invalid encryption parameters.");

        byte[] key = Rfc2898DeriveBytes.Pbkdf2(passphrase, salt, envelope.Iterations, HashAlgorithmName.SHA256, 32);
        byte[] plain = new byte[cipher.Length];
        try
        {
            using var aes = new AesGcm(key, 16);
            aes.Decrypt(nonce, cipher, tag, plain, AuthenticatedContext);
            return Encoding.UTF8.GetString(plain);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(key);
            CryptographicOperations.ZeroMemory(plain);
        }
    }

    public void Write(string passphrase, string serializedSettings)
    {
        if (string.IsNullOrEmpty(passphrase)) throw new ArgumentException("A profile password is required.", nameof(passphrase));
        byte[] salt = RandomNumberGenerator.GetBytes(16);
        byte[] nonce = RandomNumberGenerator.GetBytes(12);
        byte[] plain = Encoding.UTF8.GetBytes(serializedSettings);
        byte[] cipher = new byte[plain.Length];
        byte[] tag = new byte[16];
        byte[] key = Rfc2898DeriveBytes.Pbkdf2(passphrase, salt, Pbkdf2Iterations, HashAlgorithmName.SHA256, 32);
        try
        {
            using (var aes = new AesGcm(key, 16))
                aes.Encrypt(nonce, plain, cipher, tag, AuthenticatedContext);

            var envelope = new Envelope
            {
                Version = CurrentFormat,
                Algorithm = "AES-256-GCM/PBKDF2-SHA256",
                Iterations = Pbkdf2Iterations,
                Salt = Convert.ToBase64String(salt),
                Nonce = Convert.ToBase64String(nonce),
                Tag = Convert.ToBase64String(tag),
                Ciphertext = Convert.ToBase64String(cipher)
            };
            string temporary = path + ".tmp";
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            try
            {
                File.WriteAllText(temporary, JsonSerializer.Serialize(envelope, new JsonSerializerOptions { WriteIndented = true }), new UTF8Encoding(false));
                File.Move(temporary, path, true);
            }
            finally
            {
                if (File.Exists(temporary)) File.Delete(temporary);
            }
        }
        finally
        {
            CryptographicOperations.ZeroMemory(key);
            CryptographicOperations.ZeroMemory(plain);
        }
    }

    private sealed class Envelope
    {
        public int Version { get; set; }
        public string Algorithm { get; set; } = "";
        public int Iterations { get; set; }
        public string Salt { get; set; } = "";
        public string Nonce { get; set; } = "";
        public string Tag { get; set; } = "";
        public string Ciphertext { get; set; } = "";
    }
}
