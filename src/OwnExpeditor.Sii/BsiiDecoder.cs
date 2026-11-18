using System.Buffers.Binary;
using System.Globalization;
using System.Text;

namespace OwnExpeditor.Sii;

/// <summary>
/// Converte SII binário (BSII, versões 1–3) em texto SiiNunit.
/// Portado de github.com/liam-dong/SII-Decrypt-cpp (MIT), src/core/sii_bin_*.
/// </summary>
public static class BsiiDecoder
{
    private const string Nl = "\r\n";

    private sealed record Field(uint Type, string Name, Dictionary<uint, string>? Ordinals);

    private sealed record Structure(string Name, List<Field> Fields);

    public static string ToText(byte[] data)
    {
        ArgumentNullException.ThrowIfNull(data);
        var r = new Reader(data);
        if (r.U32() != 0x49495342)
            throw new SiiFormatException(SiiErrorKind.UnknownFormat, "Não é um arquivo BSII.", offset: 0);
        var version = r.U32();
        if (version is < 1 or > 3)
            throw new SiiFormatException(SiiErrorKind.UnsupportedVersion, $"Versão BSII {version} não suportada.", offset: 4);

        var structures = new Dictionary<uint, Structure>();
        var sb = new StringBuilder(data.Length * 8);
        sb.Append("SiiNunit").Append(Nl).Append('{').Append(Nl);

        while (!r.Eof)
        {
            var blockType = r.U32();
            if (blockType == 0)
            {
                if (r.U8() == 0) break; // bloco de estrutura inválido = fim
                var id = r.U32();
                var name = r.Str();
                var fields = new List<Field>();
                while (r.U32() is var vt && vt != 0)
                {
                    var fname = r.Str();
                    Dictionary<uint, string>? ords = null;
                    if (vt == 0x37)
                    {
                        ords = [];
                        var n = r.U32();
                        for (var i = 0; i < n; i++) ords[r.U32()] = r.Str();
                    }
                    fields.Add(new Field(vt, fname, ords));
                }
                if (id == 0 || !structures.TryAdd(id, new Structure(name, fields)))
                    throw new SiiFormatException(SiiErrorKind.Syntax,
                        $"Estrutura BSII inválida ou duplicada: {id} (offset {r.Position}).", offset: r.Position);
                continue;
            }

            if (!structures.TryGetValue(blockType, out var st))
                throw new SiiFormatException(SiiErrorKind.Syntax,
                    $"Estrutura BSII desconhecida: {blockType} (offset {r.Position}).", offset: r.Position);

            sb.Append(st.Name).Append(" : ").Append(ReadId(r, version)).Append(" {").Append(Nl);
            foreach (var f in st.Fields)
            {
                sb.Append(' ').Append(f.Name).Append(": ");
                WriteValue(sb, r, f, version);
                sb.Append(Nl);
            }
            sb.Append('}').Append(Nl).Append(Nl);
        }

        sb.Append('}').Append(Nl);
        return sb.ToString();
    }

    private static void WriteValue(StringBuilder sb, Reader r, Field f, uint ver)
    {
        switch (f.Type)
        {
            case 0x01: sb.Append(Quote(r.Str())); break;
            case 0x03: sb.Append(Quote(DecodeToken(r.U64()))); break;
            case 0x05: sb.Append(Float(r.F32())); break;
            case 0x07: sb.Append(Tuple(r, 2)); break;
            case 0x09: sb.Append(Tuple(r, 3)); break;
            case 0x11: sb.Append($"({r.I32()}, {r.I32()}, {r.I32()})"); break;
            case 0x17: sb.Append(Quat(r)); break;
            case 0x19: sb.Append(Placement(r, ver)); break;
            case 0x25: sb.Append(Int(r.I32())); break;
            case 0x27 or 0x2F: sb.Append(UInt(r.U32())); break;
            case 0x29: sb.Append((short)r.U16()); break;
            case 0x2B: sb.Append(UShort(r.U16())); break;
            case 0x31: sb.Append((long)r.U64()); break;
            case 0x33: sb.Append(r.U64()); break;
            case 0x35: sb.Append(r.U8() != 0 ? "true" : "false"); break;
            case 0x37:
            {
                var v = r.U32();
                sb.Append(f.Ordinals!.TryGetValue(v, out var s) ? Quote(s) : v.ToString());
                break;
            }
            case 0x39 or 0x3B or 0x3D: sb.Append(ReadId(r, ver)); break;
            case 0x41: sb.Append($"({r.I32()}, {r.I32()})"); break;

            case 0x02: Array(sb, r, f, () => Quote(r.Str())); break;
            case 0x04: Array(sb, r, f, () => Quote(DecodeToken(r.U64()))); break;
            case 0x06: Array(sb, r, f, () => Float(r.F32())); break;
            case 0x0A: Array(sb, r, f, () => Tuple(r, 3)); break;
            case 0x12: Array(sb, r, f, () => $"({r.I32()}, {r.I32()}, {r.I32()})"); break;
            case 0x18: Array(sb, r, f, () => Quat(r)); break;
            case 0x1A: Array(sb, r, f, () => Placement(r, ver)); break;
            case 0x26: Array(sb, r, f, () => Int(r.I32())); break;
            case 0x28: Array(sb, r, f, () => UInt(r.U32())); break;
            case 0x2A: Array(sb, r, f, () => ((short)r.U16()).ToString()); break;
            case 0x2C: Array(sb, r, f, () => UShort(r.U16())); break;
            case 0x32: Array(sb, r, f, () => ((long)r.U64()).ToString()); break;
            case 0x34: Array(sb, r, f, () => r.U64().ToString()); break;
            case 0x36: Array(sb, r, f, () => r.U8() != 0 ? "true" : "false"); break;
            case 0x38:
                Array(sb, r, f, () =>
                {
                    var v = r.U32();
                    return f.Ordinals is not null && f.Ordinals.TryGetValue(v, out var s) ? Quote(s) : v.ToString();
                });
                break;
            case 0x3A or 0x3C or 0x3E: Array(sb, r, f, () => ReadId(r, ver)); break;

            default:
                throw new SiiFormatException(SiiErrorKind.UnknownValueType,
                    $"Tipo de valor BSII 0x{f.Type:X2} ('{f.Name}') não suportado (offset {r.Position}).", offset: r.Position);
        }
    }

    private static void Array(StringBuilder sb, Reader r, Field f, Func<string> item)
    {
        var n = r.U32();
        sb.Append(n);
        for (var i = 0; i < n; i++)
            sb.Append(Nl).Append(' ').Append(f.Name).Append('[').Append(i).Append("]: ").Append(item());
    }

    // O jogo grava "nil" para inteiros com valor máximo (vazio).
    private static string Int(int v) => v == int.MaxValue ? "nil" : v.ToString(CultureInfo.InvariantCulture);
    private static string UInt(uint v) => v == uint.MaxValue ? "nil" : v.ToString(CultureInfo.InvariantCulture);
    private static string UShort(ushort v) => v == ushort.MaxValue ? "nil" : v.ToString(CultureInfo.InvariantCulture);

    private static string Tuple(Reader r, int n)
    {
        var parts = new string[n];
        for (var i = 0; i < n; i++) parts[i] = Float(r.F32());
        return "(" + string.Join(", ", parts) + ")";
    }

    private static string Quat(Reader r)
    {
        float w = r.F32(), x = r.F32(), y = r.F32(), z = r.F32();
        return $"({Float(w)}; {Float(x)}, {Float(y)}, {Float(z)})";
    }

    private static string Placement(Reader r, uint ver)
    {
        var v = new float[8];
        if (ver == 1)
        {
            v[0] = r.F32(); v[1] = r.F32(); v[2] = r.F32();
            for (var i = 4; i < 8; i++) v[i] = r.F32();
        }
        else
        {
            for (var i = 0; i < 8; i++) v[i] = r.F32();
            var bias = (int)v[3];
            v[0] += ((bias & 0xFFF) - 2048) << 9;
            v[2] += (((bias >> 12) & 0xFFF) - 2048) << 9;
        }
        return $"({Float(v[0])}, {Float(v[1])}, {Float(v[2])}) ({Float(v[4])}; {Float(v[5])}, {Float(v[6])}, {Float(v[7])})";
    }

    private static string Float(float f)
    {
        if (float.IsFinite(f) && f == MathF.Truncate(f) && MathF.Abs(f) < 1e7f)
            return ((long)f).ToString(CultureInfo.InvariantCulture);
        return "&" + BitConverter.SingleToUInt32Bits(f).ToString("x8");
    }

    // Como o jogo: sem aspas quando o texto só tem [A-Za-z0-9_].
    private static string Quote(string s)
    {
        if (s.Length > 0 && s.All(c => char.IsAsciiLetterOrDigit(c) || c == '_')) return s;
        var sb = new StringBuilder(s.Length + 2).Append('"');
        foreach (var c in s)
        {
            if (c is '"' or '\\') sb.Append('\\');
            sb.Append(c);
        }
        return sb.Append('"').ToString();
    }

    private static string ReadId(Reader r, uint ver)
    {
        var len = r.U8();
        if (len == 0) return "null";
        if (len == 0xFF)
        {
            var v = r.U64();
            if (ver < 2 && v >> 32 == 0) return $"_nameless.{(v >> 16) & 0xFFFF:X4}.{v & 0xFFFF:X4}";
            if (v == 0) return "_nameless.0";
            var parts = new List<string>();
            while (v != 0)
            {
                var chunk = v & 0xFFFF;
                v >>= 16;
                parts.Add(v != 0 ? chunk.ToString("x4") : chunk.ToString("x"));
            }
            parts.Reverse();
            return "_nameless." + string.Join('.', parts);
        }
        var tokens = new string[len];
        for (var i = 0; i < len; i++) tokens[i] = DecodeToken(r.U64());
        return string.Join('.', tokens);
    }

    private const string Alphabet = "0123456789abcdefghijklmnopqrstuvwxyz_";

    /// <summary>Token de até 12 caracteres codificado em base 38 (primeiro caractere no dígito menos significativo).</summary>
    internal static string DecodeToken(ulong v)
    {
        v &= ~(1UL << 63);
        var sb = new StringBuilder(12);
        while (v != 0)
        {
            var idx = (int)(v % 38);
            v /= 38;
            if (idx is < 1 or > 37) throw new SiiFormatException(SiiErrorKind.Syntax, "Token BSII inválido.");
            sb.Append(Alphabet[idx - 1]);
        }
        return sb.ToString();
    }

    private sealed class Reader(byte[] data)
    {
        private int _pos;
        public bool Eof => _pos >= data.Length;
        public int Position => _pos;

        private ReadOnlySpan<byte> Take(int n)
        {
            if (n < 0 || n > data.Length - _pos)
                throw new SiiFormatException(SiiErrorKind.Truncated,
                    $"Fim inesperado do arquivo BSII no offset {_pos}.", offset: _pos);
            var span = data.AsSpan(_pos, n);
            _pos += n;
            return span;
        }

        public byte U8() => Take(1)[0];
        public ushort U16() => BinaryPrimitives.ReadUInt16LittleEndian(Take(2));
        public uint U32() => BinaryPrimitives.ReadUInt32LittleEndian(Take(4));
        public int I32() => BinaryPrimitives.ReadInt32LittleEndian(Take(4));
        public ulong U64() => BinaryPrimitives.ReadUInt64LittleEndian(Take(8));
        public float F32() => BinaryPrimitives.ReadSingleLittleEndian(Take(4));
        public string Str() => Encoding.UTF8.GetString(Take((int)U32()));
    }
}
