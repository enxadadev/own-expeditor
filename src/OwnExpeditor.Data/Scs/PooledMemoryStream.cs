using System.Buffers;

namespace OwnExpeditor.Data.Scs;

/// <summary>MemoryStream somente leitura que devolve o buffer ao <see cref="ArrayPool{T}"/> ao ser descartado.</summary>
internal sealed class PooledMemoryStream : MemoryStream
{
    private byte[]? _buffer;

    public PooledMemoryStream(byte[] buffer, int length) : base(buffer, 0, length, writable: false, publiclyVisible: true)
        => _buffer = buffer;

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        var buffer = Interlocked.Exchange(ref _buffer, null);
        if (buffer is not null) ArrayPool<byte>.Shared.Return(buffer);
    }
}
