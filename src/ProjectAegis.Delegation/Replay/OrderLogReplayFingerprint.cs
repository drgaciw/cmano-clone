namespace ProjectAegis.Delegation.Replay;

using System.Security.Cryptography;
using System.Text;
using ProjectAegis.Delegation.Decision;

/// <summary>SHA-256 over canonical order-log text (order-log-replay GDD § Formulas).</summary>
public static class OrderLogReplayFingerprint
{
    public static string ComputeSha256Hex(IOrderLog log)
    {
        var bytes = Encoding.UTF8.GetBytes(log.ComputeFingerprint());
        using var sha = SHA256.Create();
        return ToHexLower(sha.ComputeHash(bytes));
    }

    private static string ToHexLower(byte[] bytes)
    {
        var chars = new char[bytes.Length * 2];
        for (var i = 0; i < bytes.Length; i++)
        {
            var b = bytes[i];
            chars[i * 2] = GetHexNibble(b >> 4);
            chars[i * 2 + 1] = GetHexNibble(b & 0xF);
        }

        return new string(chars);
    }

    private static char GetHexNibble(int value) =>
        (char)(value < 10 ? '0' + value : 'a' + (value - 10));
}