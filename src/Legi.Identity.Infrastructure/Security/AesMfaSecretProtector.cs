using System.Security.Cryptography;
using System.Text;
using Legi.Identity.Application.Common.Interfaces;
using Legi.Identity.Application.Common.Models;

namespace Legi.Identity.Infrastructure.Security;

/// <summary>
/// Encrypts TOTP secrets at rest with AES-256-GCM (authenticated encryption). The stored
/// value is base64(nonce ‖ tag ‖ ciphertext); a tampered value fails to decrypt.
/// </summary>
public sealed class AesMfaSecretProtector : IMfaSecretProtector
{
    private const int NonceSize = 12; // 96-bit nonce, standard for AES-GCM
    private const int TagSize = 16;   // 128-bit authentication tag
    private readonly byte[] _key;

    public AesMfaSecretProtector(MfaSettings settings)
    {
        if (string.IsNullOrWhiteSpace(settings.EncryptionKey))
            throw new InvalidOperationException(
                "Mfa:EncryptionKey is required to protect MFA secrets (base64-encoded 32-byte key).");

        _key = Convert.FromBase64String(settings.EncryptionKey);
        if (_key.Length != 32)
            throw new InvalidOperationException(
                "Mfa:EncryptionKey must decode to exactly 32 bytes (256-bit AES key).");
    }

    public string Protect(string plaintext)
    {
        var plain = Encoding.UTF8.GetBytes(plaintext);
        var nonce = RandomNumberGenerator.GetBytes(NonceSize);
        var cipher = new byte[plain.Length];
        var tag = new byte[TagSize];

        using var aes = new AesGcm(_key, TagSize);
        aes.Encrypt(nonce, plain, cipher, tag);

        var combined = new byte[NonceSize + TagSize + cipher.Length];
        Buffer.BlockCopy(nonce, 0, combined, 0, NonceSize);
        Buffer.BlockCopy(tag, 0, combined, NonceSize, TagSize);
        Buffer.BlockCopy(cipher, 0, combined, NonceSize + TagSize, cipher.Length);
        return Convert.ToBase64String(combined);
    }

    public string Unprotect(string protectedValue)
    {
        var combined = Convert.FromBase64String(protectedValue);
        if (combined.Length < NonceSize + TagSize)
            throw new CryptographicException("Malformed protected MFA secret.");

        var nonce = combined.AsSpan(0, NonceSize);
        var tag = combined.AsSpan(NonceSize, TagSize);
        var cipher = combined.AsSpan(NonceSize + TagSize);
        var plain = new byte[cipher.Length];

        using var aes = new AesGcm(_key, TagSize);
        aes.Decrypt(nonce, cipher, tag, plain); // throws CryptographicException if tampered
        return Encoding.UTF8.GetString(plain);
    }
}
