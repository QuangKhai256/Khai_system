using System.Globalization;
using System.Security.Cryptography;

namespace Khai_Login_System.Services;

public static class PasswordService
{
    private const int SaltSize = 16; // 128 bit
    private const int KeySize = 32;  // 256 bit
    private const int Iterations = 100_000;

    public static string HashPassword(string password)
    {
        ArgumentException.ThrowIfNullOrEmpty(password);

        Span<byte> salt = stackalloc byte[SaltSize];
        RandomNumberGenerator.Fill(salt);

        var key = Rfc2898DeriveBytes.Pbkdf2(password, salt.ToArray(), Iterations, HashAlgorithmName.SHA256, KeySize);

        return string.Join('.',
            Convert.ToHexString(salt),
            Convert.ToHexString(key),
            Iterations.ToString(CultureInfo.InvariantCulture));
    }

    public static bool VerifyPassword(string password, string storedHash)
    {
        if (string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(storedHash))
        {
            return false;
        }

        var parts = storedHash.Split('.', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 3)
        {
            return false;
        }

        var salt = Convert.FromHexString(parts[0]);
        var expectedHash = parts[1];

        if (!int.TryParse(parts[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out var iterations))
        {
            return false;
        }

        var actualHash = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA256, KeySize);

        return CryptographicOperations.FixedTimeEquals(Convert.FromHexString(expectedHash), actualHash);
    }
}
