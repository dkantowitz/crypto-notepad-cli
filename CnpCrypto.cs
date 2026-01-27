using System.Security.Cryptography;
using System.Text;

#pragma warning disable SYSLIB0041 // PasswordDeriveBytes is obsolete but required for Crypto Notepad compatibility

namespace CryptoNotepadCli;

public static class CnpCrypto
{
    private const int DefaultSaltLength = 64;
    private const int IvLength = 16;

    public static byte[] Encrypt(string plainText, string password, string? salt,
        string hashAlgorithm, int iterations, int keySize)
    {
        byte[] saltBytes = salt != null
            ? Encoding.ASCII.GetBytes(salt)
            : GenerateNonZeroBytes(DefaultSaltLength);

        byte[] ivBytes = GenerateNonZeroBytes(IvLength);

        byte[] key = DeriveKey(password, saltBytes, hashAlgorithm, iterations, keySize);

        byte[] cipherText;
        using (var aes = Aes.Create())
        {
            aes.KeySize = keySize;
            aes.BlockSize = 128;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;
            aes.Key = key;
            aes.IV = ivBytes;

            using var encryptor = aes.CreateEncryptor();
            byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);
            cipherText = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);
        }

        // Build blob: IV + 0x00 + salt + 0x00 + ciphertext
        byte[] blob = new byte[ivBytes.Length + 1 + saltBytes.Length + 1 + cipherText.Length];
        int offset = 0;
        Buffer.BlockCopy(ivBytes, 0, blob, offset, ivBytes.Length);
        offset += ivBytes.Length;
        blob[offset++] = 0x00;
        Buffer.BlockCopy(saltBytes, 0, blob, offset, saltBytes.Length);
        offset += saltBytes.Length;
        blob[offset++] = 0x00;
        Buffer.BlockCopy(cipherText, 0, blob, offset, cipherText.Length);

        return Encoding.ASCII.GetBytes(Convert.ToBase64String(blob));
    }

    public static string Decrypt(byte[] cipherData, string password,
        string hashAlgorithm, int iterations, int keySize)
    {
        string base64 = Encoding.ASCII.GetString(cipherData).Trim();
        byte[] blob = Convert.FromBase64String(base64);

        // Parse: IV + 0x00 + salt + 0x00 + ciphertext
        int firstNull = Array.IndexOf(blob, (byte)0x00);
        if (firstNull < 0)
            throw new CryptographicException("Invalid .cnp format: missing IV delimiter.");

        byte[] ivBytes = new byte[firstNull];
        Buffer.BlockCopy(blob, 0, ivBytes, 0, firstNull);

        int secondNull = Array.IndexOf(blob, (byte)0x00, firstNull + 1);
        if (secondNull < 0)
            throw new CryptographicException("Invalid .cnp format: missing salt delimiter.");

        int saltLength = secondNull - firstNull - 1;
        byte[] saltBytes = new byte[saltLength];
        Buffer.BlockCopy(blob, firstNull + 1, saltBytes, 0, saltLength);

        int cipherStart = secondNull + 1;
        int cipherLength = blob.Length - cipherStart;
        byte[] cipherText = new byte[cipherLength];
        Buffer.BlockCopy(blob, cipherStart, cipherText, 0, cipherLength);

        byte[] key = DeriveKey(password, saltBytes, hashAlgorithm, iterations, keySize);

        using var aes = Aes.Create();
        aes.KeySize = keySize;
        aes.BlockSize = 128;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;
        aes.Key = key;
        aes.IV = ivBytes;

        using var decryptor = aes.CreateDecryptor();
        byte[] plainBytes = decryptor.TransformFinalBlock(cipherText, 0, cipherText.Length);
        return Encoding.UTF8.GetString(plainBytes);
    }

    private static byte[] DeriveKey(string password, byte[] salt,
        string hashAlgorithm, int iterations, int keySize)
    {
        var pdb = new PasswordDeriveBytes(password, salt, hashAlgorithm, iterations);
        return pdb.GetBytes(keySize / 8);
    }

    private static byte[] GenerateNonZeroBytes(int length)
    {
        byte[] bytes = new byte[length];
        using var rng = RandomNumberGenerator.Create();
        rng.GetNonZeroBytes(bytes);
        return bytes;
    }
}
