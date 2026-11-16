namespace NewsHub.Infrastructure.Services;

using System.Security.Cryptography;
using NewsHub.Application.Common.Interfaces;

/// <summary>
/// Triển khai IPasswordHasher băm mật khẩu bằng PBKDF2 với HMAC-SHA512,
/// tự động sinh salt bảo mật ngẫu nhiên và so khớp constant-time chống timing attack.
/// </summary>
public class PasswordHasher : IPasswordHasher
{
    private const int SaltSize = 16; // 128 bit
    private const int KeySize = 32;  // 256 bit
    private const int Iterations = 100_000;
    private static readonly HashAlgorithmName Algorithm = HashAlgorithmName.SHA512;
    private const char SegmentDelimiter = ':';

    public string HashPassword(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            Iterations,
            Algorithm,
            KeySize);

        return string.Join(
            SegmentDelimiter,
            Convert.ToHexString(hash),
            Convert.ToHexString(salt),
            Iterations,
            Algorithm);
    }

    public bool VerifyPassword(string password, string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(passwordHash))
        {
            return false;
        }

        var segments = passwordHash.Split(SegmentDelimiter);
        if (segments.Length != 4)
        {
            return false;
        }

        try
        {
            var hash = Convert.FromHexString(segments[0]);
            var salt = Convert.FromHexString(segments[1]);
            if (!int.TryParse(segments[2], out var iterations))
            {
                return false;
            }
            var algorithm = new HashAlgorithmName(segments[3]);

            var inputHash = Rfc2898DeriveBytes.Pbkdf2(
                password,
                salt,
                iterations,
                algorithm,
                hash.Length);

            return CryptographicOperations.FixedTimeEquals(inputHash, hash);
        }
        catch (Exception)
        {
            return false;
        }
    }
}
