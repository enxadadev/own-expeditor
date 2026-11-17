using System.Buffers.Binary;
using System.Numerics;

namespace OwnExpeditor.Data.Scs;

/// <summary>
/// CityHash64 na variante usada pelos arquivos .scs (Google CityHash ~2011).
/// Portado de github.com/Wilps93/scs_tools src/cityhash.rs (MIT).
/// </summary>
public static class CityHash
{
    private const ulong K0 = 0xc3a5c85c97cb3127;
    private const ulong K1 = 0xb492b66fbe98f273;
    private const ulong K2 = 0x9ae16a3b2f90404f;
    private const ulong K3 = 0xc949d7c7509e6557;
    private const ulong KMul = 0x9ddfea08eb382d69;

    public static ulong Hash64(string s) => Hash64(System.Text.Encoding.UTF8.GetBytes(s));

    public static ulong Hash64(ReadOnlySpan<byte> s)
    {
        var len = s.Length;
        if (len <= 16) return HashLen0To16(s);
        if (len <= 32) return HashLen17To32(s);
        if (len <= 64) return HashLen33To64(s);

        var x = F64(s, len - 40);
        var y = F64(s, len - 16) + F64(s, len - 56);
        var z = HashLen16(F64(s, len - 48) + (ulong)len, F64(s, len - 24));
        var v = WeakHash(s, len - 64, (ulong)len, z);
        var w = WeakHash(s, len - 32, y + K1, x);
        x = x * K1 + F64(s, 0);

        var off = 0;
        var remaining = (len - 1) & ~63;
        do
        {
            x = Rot(x + y + v.A + F64(s, off + 8), 37) * K1;
            y = Rot(y + v.B + F64(s, off + 48), 42) * K1;
            x ^= w.B;
            y += v.A + F64(s, off + 40);
            z = Rot(z + w.A, 33) * K1;
            v = WeakHash(s, off, v.B * K1, x + w.A);
            w = WeakHash(s, off + 32, z + w.B, y + F64(s, off + 16));
            (z, x) = (x, z);
            off += 64;
            remaining -= 64;
        } while (remaining != 0);

        return HashLen16(HashLen16(v.A, w.A) + ShiftMix(y) * K1 + z, HashLen16(v.B, w.B) + x);
    }

    private static ulong F64(ReadOnlySpan<byte> s, int i) => BinaryPrimitives.ReadUInt64LittleEndian(s[i..]);
    private static ulong F32(ReadOnlySpan<byte> s, int i) => BinaryPrimitives.ReadUInt32LittleEndian(s[i..]);
    private static ulong Rot(ulong v, int shift) => shift == 0 ? v : BitOperations.RotateRight(v, shift);
    private static ulong ShiftMix(ulong v) => v ^ (v >> 47);

    private static ulong HashLen16(ulong u, ulong v)
    {
        var a = (u ^ v) * KMul;
        a ^= a >> 47;
        var b = (v ^ a) * KMul;
        b ^= b >> 47;
        return b * KMul;
    }

    private static ulong HashLen0To16(ReadOnlySpan<byte> s)
    {
        var len = s.Length;
        if (len > 8)
        {
            var a = F64(s, 0);
            var b = F64(s, len - 8);
            return HashLen16(a, BitOperations.RotateRight(b + (ulong)len, len)) ^ b;
        }
        if (len >= 4)
        {
            var a = F32(s, 0);
            return HashLen16((ulong)len + (a << 3), F32(s, len - 4));
        }
        if (len > 0)
        {
            uint a = s[0], b = s[len >> 1], c = s[len - 1];
            ulong y = a + (b << 8);
            ulong z = (uint)len + (c << 2);
            return ShiftMix(y * K2 ^ z * K3) * K2;
        }
        return K2;
    }

    private static ulong HashLen17To32(ReadOnlySpan<byte> s)
    {
        var len = s.Length;
        var a = F64(s, 0) * K1;
        var b = F64(s, 8);
        var c = F64(s, len - 8) * K2;
        var d = F64(s, len - 16) * K0;
        return HashLen16(Rot(a - b, 43) + Rot(c, 30) + d, a + Rot(b ^ K3, 20) - c + (ulong)len);
    }

    private static ulong HashLen33To64(ReadOnlySpan<byte> s)
    {
        var len = s.Length;
        var z = F64(s, 24);
        var a = F64(s, 0) + ((ulong)len + F64(s, len - 16)) * K0;
        var b = Rot(a + z, 52);
        var c = Rot(a, 37);
        a += F64(s, 8);
        c += Rot(a, 7);
        a += F64(s, 16);
        var vf = a + z;
        var vs = b + Rot(a, 31) + c;
        a = F64(s, 16) + F64(s, len - 32);
        z = F64(s, len - 8);
        b = Rot(a + z, 52);
        c = Rot(a, 37);
        a += F64(s, len - 24);
        c += Rot(a, 7);
        a += F64(s, len - 16);
        var wf = a + z;
        var ws = b + Rot(a, 31) + c;
        var r = ShiftMix((vf + ws) * K2 + (wf + vs) * K0);
        return ShiftMix(r * K0 + vs) * K2;
    }

    private readonly record struct Pair(ulong A, ulong B);

    private static Pair WeakHash(ReadOnlySpan<byte> s, int off, ulong a, ulong b)
    {
        ulong w = F64(s, off), x = F64(s, off + 8), y = F64(s, off + 16), z = F64(s, off + 24);
        a += w;
        b = Rot(b + a + z, 21);
        var c = a;
        a += x;
        a += y;
        b += Rot(a, 44);
        return new Pair(a + z, b + c);
    }
}
