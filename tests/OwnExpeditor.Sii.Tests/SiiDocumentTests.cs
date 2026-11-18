using System.Text;
using OwnExpeditor.Sii;

namespace OwnExpeditor.Sii.Tests;

public class SiiDocumentTests
{
    private const string Sample =
        "SiiNunit\r\n{\r\n" +
        "economy : _nameless.2da.b098.b5e8 {\r\n" +
        " game_time: 10915\r\n" +
        " time_zone_name: \"@@tz_cest@@\"\r\n" +
        "}\r\n\r\n" +
        "company : company.volatile.wilnet_trans.lemans {\r\n" +
        " permanent_data: company.permanent.wilnet_trans\r\n" +
        " job_offer: 2\r\n" +
        " job_offer[0]: _nameless.2d8.21c9.d5d8\r\n" +
        " job_offer[1]: _nameless.2d8.21c9.c4f8\r\n" +
        "}\r\n\r\n" +
        "job_offer_data : _nameless.2d8.21c9.d5d8 {\r\n" +
        " target: \"lognstick.istanbul\"\r\n" +
        " cargo: cargo.concr_beams2\r\n" +
        "}\r\n\r\n" +
        "job_offer_data : _nameless.2d8.21c9.c4f8 {\r\n" +
        " target: \"\"\r\n" +
        " cargo: null\r\n" +
        "}\r\n\r\n" +
        "}\r\n";

    [Fact]
    public void Parse_reads_units_and_attributes()
    {
        var doc = SiiDocument.Parse(Sample);

        Assert.Equal(4, doc.Units.Count);
        Assert.Equal("10915", doc.OfType("economy").Single().Get("game_time"));

        var company = doc.Find("company.volatile.wilnet_trans.lemans")!;
        Assert.Equal(["_nameless.2d8.21c9.d5d8", "_nameless.2d8.21c9.c4f8"], company.GetArray("job_offer"));

        var job = doc.Find("_nameless.2d8.21c9.d5d8")!;
        Assert.Equal("\"lognstick.istanbul\"", job.Get("target"));
    }

    [Fact]
    public void Roundtrip_without_changes_is_identical()
    {
        Assert.Equal(Sample, SiiDocument.Parse(Sample).ToText());
    }

    [Fact]
    public void Set_changes_only_target_attribute()
    {
        var doc = SiiDocument.Parse(Sample);
        doc.Find("_nameless.2d8.21c9.c4f8")!.Set("cargo", "cargo.apples");

        var text = doc.ToText();
        Assert.Contains(" cargo: cargo.apples\r\n", text);
        Assert.Equal(Sample.Length - "null".Length + "cargo.apples".Length, text.Length);
    }

    [Fact]
    public void Value_with_colon_space_inside_quotes_is_kept()
    {
        var doc = SiiDocument.Parse("SiiNunit\r\n{\r\nx : a {\r\n name: \"a: b\"\r\n}\r\n\r\n}\r\n");
        Assert.Equal("\"a: b\"", doc.Units[0].Get("name"));
    }

    [Fact]
    public void Parse_rejects_unclosed_unit()
    {
        Assert.Throws<SiiFormatException>(() => SiiDocument.Parse("SiiNunit\r\n{\r\nx : a {\r\n k: v\r\n"));
    }

    [Fact]
    public void Bsii_token_decodes_base38()
    {
        // "ab" = a(11) + b(12) * 38
        Assert.Equal("ab", BsiiDecoder.DecodeToken(11 + 12 * 38));
    }

    [Fact]
    public void Decoder_detects_formats()
    {
        Assert.Equal(SiiFormat.Text, SiiDecoder.DetectFormat("SiiNunit"u8));
        Assert.Equal(SiiFormat.Encrypted, SiiDecoder.DetectFormat("ScsC1234"u8));
        Assert.Equal(SiiFormat.Binary, SiiDecoder.DetectFormat("BSII1234"u8));
        Assert.Equal(SiiFormat.Unknown, SiiDecoder.DetectFormat("abc"u8));
        Assert.Equal(Sample, SiiDecoder.DecodeToText(Encoding.UTF8.GetBytes(Sample)));
    }
}
