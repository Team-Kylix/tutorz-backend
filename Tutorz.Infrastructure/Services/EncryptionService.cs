using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Tutorz.Application.Interfaces;

namespace Tutorz.Infrastructure.Services
{
    /// <summary>
    /// AES-256-CBC field-level encryption.
    /// The encryption key is loaded from IConfiguration ("Encryption:Key").
    /// A SHA-256 hash of the configured string is used as the actual 32-byte key,
    /// so the key string can be any length.
    ///
    /// Security guarantees:
    /// - Each encryption generates a fresh random IV (16 bytes), prepended to the ciphertext.
    /// - The output is Base64-encoded: the DB column stores a plain-looking Base64 string.
    /// - Without the key, the ciphertext is computationally infeasible to decrypt.
    /// - Developers with direct DB access only see Base64 garbage.
    /// </summary>
    public class EncryptionService : IEncryptionService
    {
        private readonly byte[] _key;

        public EncryptionService(IConfiguration config)
        {
            var rawKey = config["Encryption:Key"]
                ?? throw new InvalidOperationException("Encryption:Key is not configured.");

            // Hash the string to get exactly 32 bytes for AES-256
            using var sha = SHA256.Create();
            _key = sha.ComputeHash(Encoding.UTF8.GetBytes(rawKey));
        }

        public string Encrypt(string plainText)
        {
            if (string.IsNullOrEmpty(plainText)) return string.Empty;

            using var aes = Aes.Create();
            aes.Key = _key;
            aes.GenerateIV(); // Random fresh IV each time for semantic security

            using var encryptor = aes.CreateEncryptor();
            using var ms = new MemoryStream();

            // Prepend IV so we can read it back during decryption
            ms.Write(aes.IV, 0, aes.IV.Length);

            using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
            using (var sw = new StreamWriter(cs))
            {
                sw.Write(plainText);
            }

            return Convert.ToBase64String(ms.ToArray());
        }

        public string Decrypt(string cipherText)
        {
            if (string.IsNullOrEmpty(cipherText)) return string.Empty;

            try
            {
                var fullBytes = Convert.FromBase64String(cipherText);

                // Must have at least 17 bytes: 16 IV + 1 byte ciphertext
                if (fullBytes.Length <= 16)
                    return string.Empty;

                using var aes = Aes.Create();
                aes.Key = _key;

                // Extract prepended IV (first 16 bytes)
                var iv = new byte[16];
                Array.Copy(fullBytes, 0, iv, 0, iv.Length);
                aes.IV = iv;

                using var decryptor = aes.CreateDecryptor();
                using var ms = new MemoryStream(fullBytes, iv.Length, fullBytes.Length - iv.Length);
                using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
                using var sr = new StreamReader(cs);

                return sr.ReadToEnd();
            }
            catch (FormatException)
            {
                // Input was not valid Base64
                return string.Empty;
            }
            catch (CryptographicException)
            {
                // Wrong key, corrupt padding, or truncated ciphertext
                return string.Empty;
            }
            catch (Exception)
            {
                return string.Empty;
            }
        }

        public string Mask(string value, int visibleChars = 4)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;

            // Clean any spaces for accurate masking
            var clean = value.Replace(" ", "");
            if (clean.Length <= visibleChars)
                return new string('*', clean.Length);

            var masked = new string('*', clean.Length - visibleChars)
                         + clean[^visibleChars..];

            // Re-add spaces every 4 chars for card-like display
            var parts = new System.Collections.Generic.List<string>();
            for (int i = 0; i < masked.Length; i += 4)
            {
                int len = Math.Min(4, masked.Length - i);
                parts.Add(masked.Substring(i, len));
            }
            return string.Join(" ", parts);
        }
    }
}
