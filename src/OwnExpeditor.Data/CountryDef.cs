namespace OwnExpeditor.Data;

/// <summary>País definido em def/country/&lt;pais&gt;.sui (country_data). Ver docs/sii-format.md §9.</summary>
public sealed class CountryDef(string name)
{
    public string Name { get; } = name;          // id sem "country.data.": ex.: hungary (casa com city.country)
    public string? NativeName { get; set; }      // name: "Magyarország"
    public string? LocalizationKey { get; set; } // name_localized: "@@hungary@@"
}
