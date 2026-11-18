using OwnExpeditor.Core.Legacy;
using OwnExpeditor.Sii;

namespace OwnExpeditor.Core.Tests;

#pragma warning disable CS0618 // LegacyJobs é transitório (ADR 0006)
public class LegacyErrorTests
{
    [Theory]
    [InlineData(SiiErrorKind.Truncated, CoreError.SaveCorrupt)]
    [InlineData(SiiErrorKind.DecryptFailed, CoreError.SaveCorrupt)]
    [InlineData(SiiErrorKind.Syntax, CoreError.SaveParseFailed)]
    [InlineData(SiiErrorKind.UnknownFormat, CoreError.SaveParseFailed)]
    public void SiiErrorMapsToCoreError(SiiErrorKind kind, CoreError expected)
    {
        var e = LegacyJobs.ToCoreError(new SiiFormatException(kind, "x", line: 7));

        Assert.Equal(expected, e.Code);
        Assert.Equal(7, e.Line);
        Assert.IsType<SiiFormatException>(e.InnerException);
    }
}
