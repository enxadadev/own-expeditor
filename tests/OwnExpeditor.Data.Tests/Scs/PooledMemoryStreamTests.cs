using System.Buffers;
using OwnExpeditor.Data.Scs;

namespace OwnExpeditor.Data.Tests.Scs;

public class PooledMemoryStreamTests
{
    [Fact]
    public void Ctor_BufferLargerThanLength_ExposesOnlyLengthBytesAndIsReadOnly()
    {
        var buffer = ArrayPool<byte>.Shared.Rent(100);
        using var stream = new PooledMemoryStream(buffer, 10);

        Assert.Equal(10, stream.Length);
        Assert.False(stream.CanWrite);
        Assert.True(stream.TryGetBuffer(out var segment));
        Assert.Same(buffer, segment.Array);
        Assert.Equal(10, segment.Count);
        Assert.Throws<NotSupportedException>(() => stream.WriteByte(1));
    }

    [Fact]
    public void Dispose_ReturnsBufferToSharedPool()
    {
        var buffer = ArrayPool<byte>.Shared.Rent(256);
        var stream = new PooledMemoryStream(buffer, 8);

        stream.Dispose();
        var again = ArrayPool<byte>.Shared.Rent(256);

        Assert.Same(buffer, again);
        ArrayPool<byte>.Shared.Return(again);
    }

    [Fact]
    public void Dispose_CalledTwice_ReturnsBufferOnlyOnce()
    {
        var buffer = ArrayPool<byte>.Shared.Rent(256);
        var stream = new PooledMemoryStream(buffer, 8);

        stream.Dispose();
        var first = ArrayPool<byte>.Shared.Rent(256);
        stream.Dispose();
        var second = ArrayPool<byte>.Shared.Rent(256);

        Assert.Same(buffer, first);
        Assert.NotSame(first, second);
        ArrayPool<byte>.Shared.Return(first);
        ArrayPool<byte>.Shared.Return(second);
    }

    [Fact]
    public void Dispose_ConcurrentCalls_ReturnBufferOnlyOnce()
    {
        var buffer = ArrayPool<byte>.Shared.Rent(512);
        var stream = new PooledMemoryStream(buffer, 8);

        var ex = Record.Exception(() => Parallel.For(0, 8, _ => stream.Dispose()));

        Assert.Null(ex);
    }

    [Fact]
    public void Read_AfterDispose_Throws()
    {
        var stream = new PooledMemoryStream(ArrayPool<byte>.Shared.Rent(16), 4);
        stream.Dispose();

        Assert.Throws<ObjectDisposedException>(() => stream.ReadByte());
    }
}
