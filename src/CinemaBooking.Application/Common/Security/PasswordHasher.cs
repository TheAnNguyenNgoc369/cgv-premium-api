using System.Security.Cryptography;
using System.Text;

namespace CinemaBooking.Application.Common.Security;

public static class PasswordHasher
{
    private const string Algorithm = "pbkdf2-sha256";
    private const string Version = "v1";
    private const int Iterations = 700_000;
    private const int SaltSize = 16;
    private const int HashSize = 32;

    public static string Hash(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(password), salt, Iterations,
            HashAlgorithmName.SHA256, HashSize);

        return $"${Algorithm}${Version}${Iterations}${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
    }

    public static bool Verify(string password, string encodedHash, out bool requiresRehash)
    {
        requiresRehash = false;
        var parts = encodedHash.Split('$');

        if (parts.Length == 6 && parts[0].Length == 0 && parts[1] == Algorithm
            && parts[2] == Version && int.TryParse(parts[3], out var iterations)
            && iterations == Iterations)
        {
            return VerifyPbkdf2(password, parts, iterations);
        }

        return VerifyLegacySha256(password, encodedHash, out requiresRehash);
    }

    private static bool VerifyPbkdf2(string password, string[] parts, int iterations)
    {
        try
        {
            var salt = Convert.FromBase64String(parts[4]);
            var expectedHash = Convert.FromBase64String(parts[5]);
            if (salt.Length != SaltSize || expectedHash.Length != HashSize) return false;

            var actualHash = Rfc2898DeriveBytes.Pbkdf2(
                Encoding.UTF8.GetBytes(password), salt, iterations,
                HashAlgorithmName.SHA256, HashSize);
            return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private static bool VerifyLegacySha256(
        string password, string encodedHash, out bool requiresRehash)
    {
        requiresRehash = false;
        if (encodedHash.Length != 64) return false;

        try
        {
            var expectedHash = Convert.FromHexString(encodedHash);
            var actualHash = SHA256.HashData(Encoding.UTF8.GetBytes(password));
            var verified = CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
            requiresRehash = verified;
            return verified;
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
