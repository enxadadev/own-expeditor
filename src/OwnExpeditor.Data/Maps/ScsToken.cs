namespace OwnExpeditor.Data.Maps;

/// <summary>Token SCS: texto de até 12 caracteres <c>[0-9a-z_]</c> em base 38 (primeiro caractere no dígito menos significativo).</summary>
internal static class ScsToken
{
    private const string Alphabet = "0123456789abcdefghijklmnopqrstuvwxyz_";
    private const int MaxLength = 12;

    /// <summary>Decodifica o token; falha para zero, dígito intermediário zero ou mais de 12 caracteres.</summary>
    public static bool TryDecode(ulong value, out string token)
    {
        Span<char> chars = stackalloc char[MaxLength];
        var n = 0;
        while (value != 0)
        {
            var digit = (int)(value % 38);
            if (digit == 0 || n == MaxLength)
            {
                token = "";
                return false;
            }
            chars[n++] = Alphabet[digit - 1];
            value /= 38;
        }
        token = n == 0 ? "" : new string(chars[..n]);
        return n != 0;
    }
}
