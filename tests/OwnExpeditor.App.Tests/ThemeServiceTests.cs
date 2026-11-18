using Avalonia.Styling;
using OwnExpeditor.App.Services;
using OwnExpeditor.Domain.Settings;

namespace OwnExpeditor.App.Tests;

public class ThemeServiceTests
{
    private sealed class FakeHost : IThemeHost
    {
        public ThemeVariant RequestedThemeVariant { get; set; } = ThemeVariant.Default;
    }

    [Theory]
    [InlineData(ThemePreference.Light, "Light")]
    [InlineData(ThemePreference.Dark, "Dark")]
    [InlineData(ThemePreference.System, "Default")]
    public void Apply_Preference_SetsMatchingVariant(ThemePreference preference, string expected)
    {
        var host = new FakeHost();
        var service = new ThemeService(host);

        service.Apply(preference);

        Assert.Equal(expected, host.RequestedThemeVariant.Key.ToString());
    }

    [Fact]
    public void Apply_Dark_ExposesCurrentPreference()
    {
        var service = new ThemeService(new FakeHost());

        service.Apply(ThemePreference.Dark);

        Assert.Equal(ThemePreference.Dark, service.Current);
    }

    [Fact]
    public void Apply_Twice_RaisesChangedOnlyWhenDifferent()
    {
        var service = new ThemeService(new FakeHost());
        var raised = 0;
        service.Changed += (_, _) => raised++;

        service.Apply(ThemePreference.Dark);
        service.Apply(ThemePreference.Dark);
        service.Apply(ThemePreference.Light);

        Assert.Equal(2, raised);
    }

    [Fact]
    public void Current_Initially_IsSystem()
    {
        var service = new ThemeService(new FakeHost());

        Assert.Equal(ThemePreference.System, service.Current);
    }
}
