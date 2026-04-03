namespace Tutorz.Application.Interfaces
{
    /// <summary>
    /// AES-256-CBC field-level encryption service.
    /// Only the server has the key — even if a developer queries the DB they
    /// see Base64 ciphertext, not plaintext values.
    /// </summary>
    public interface IEncryptionService
    {
        /// <summary>Encrypts plaintext and returns a Base64-encoded AES ciphertext.</summary>
        string Encrypt(string plainText);

        /// <summary>Decrypts a Base64-encoded AES ciphertext back to plaintext.</summary>
        string Decrypt(string cipherText);

        /// <summary>
        /// Returns a masked display version, e.g. "**** **** 5678".
        /// visibleChars controls how many trailing chars are shown.
        /// </summary>
        string Mask(string value, int visibleChars = 4);
    }
}
