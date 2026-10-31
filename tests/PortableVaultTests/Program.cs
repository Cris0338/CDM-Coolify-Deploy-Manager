// Copyright (c) 2026 Cris0338. Licensed under the MIT License.
using System.Security.Cryptography;
using System.Text.Json.Nodes;
using CoolifyDeployManager;

string folder = Path.Combine(Path.GetTempPath(), "cdm-vault-tests-" + Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(folder);
try
{
    var path = Path.Combine(folder, "CDM.profile");
    var vault = new PortableVault(path);
    const string password = "Correct-Horse-Battery-2026";
    const string secret = "{\"ReadToken\":\"read-secret-test-123\",\"DeployToken\":\"deploy-secret-test-456\",\"Language\":\"fr\"}";

    if (vault.Exists) throw new Exception("Vault unexpectedly exists.");
    vault.Write(password, secret);
    if (!vault.Exists) throw new Exception("Vault file was not written.");
    if (vault.Read(password) != secret) throw new Exception("Profile round trip mismatch.");
    Console.WriteLine("PASS encrypted profile round trip");

    var text = File.ReadAllText(path);
    if (text.Contains("read-secret-test-123") || text.Contains("deploy-secret-test-456"))
        throw new Exception("Token plaintext leaked into profile.");
    Console.WriteLine("PASS tokens not stored in plaintext");

    try
    {
        _ = vault.Read("Wrong-Password-2026");
        throw new Exception("Wrong password unexpectedly unlocked the vault.");
    }
    catch (CryptographicException) { Console.WriteLine("PASS wrong password rejected"); }

    var original = JsonNode.Parse(text)!.AsObject();
    var cipher = Convert.FromBase64String(original["Ciphertext"]!.GetValue<string>());
    cipher[0] ^= 0x01;
    original["Ciphertext"] = Convert.ToBase64String(cipher);
    File.WriteAllText(path, original.ToJsonString());
    try
    {
        _ = vault.Read(password);
        throw new Exception("Tampered vault unexpectedly decrypted.");
    }
    catch (CryptographicException) { Console.WriteLine("PASS tampering detected"); }

    vault.Write(password, secret);
    if (vault.Read(password) != secret) throw new Exception("Rewritten vault could not be read.");
    Console.WriteLine("PASS profile recovery after rewrite");
    Console.WriteLine("5/5 PASS");
}
finally
{
    Directory.Delete(folder, recursive: true);
}
