using OwnExpeditor.Core.Profiles;
using OwnExpeditor.Domain.Games;
using OwnExpeditor.Domain.Saves;

namespace OwnExpeditor.Core.Tests;

public class ProfileServiceTests
{
    private static ProfileInfo Profile(string id, GameKind game = GameKind.Ets2) =>
        new(game, id, id, "/docs/" + id, false);

    [Fact]
    public async Task GetProfilesAsync_ReturnsLocatorProfilesInOrder()
    {
        var locator = new FakeProfileLocator { Profiles = { Profile("a"), Profile("b", GameKind.Ats) } };

        var result = await new ProfileService(locator).GetProfilesAsync(default);

        Assert.Equal([Profile("a"), Profile("b", GameKind.Ats)], result);
    }

    [Fact]
    public async Task GetSavesAsync_ReturnsSavesOfThatProfile()
    {
        var a = Profile("a");
        var slot = SampleSave.Slot();
        var locator = new FakeProfileLocator { Saves = { [a] = [slot] } };

        var result = await new ProfileService(locator).GetSavesAsync(a, default);

        Assert.Equal([slot], result);
    }

    [Fact]
    public async Task GetSavesAsync_ProfileWithoutSaves_ReturnsEmpty()
    {
        var result = await new ProfileService(new FakeProfileLocator()).GetSavesAsync(Profile("x"), default);

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetProfilesAsync_AlreadyCancelled_ThrowsOperationCanceled()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => new ProfileService(new FakeProfileLocator()).GetProfilesAsync(cts.Token));
    }

    [Fact]
    public async Task GetProfilesAsync_AccessDenied_ThrowsCoreAccessDenied()
    {
        var locator = new FakeProfileLocator { Throws = new UnauthorizedAccessException() };

        var e = await Assert.ThrowsAsync<CoreException>(() => new ProfileService(locator).GetProfilesAsync(default));

        Assert.Equal(CoreError.AccessDenied, e.Code);
        Assert.IsType<UnauthorizedAccessException>(e.InnerException);
    }

    [Fact]
    public async Task GetSavesAsync_AccessDenied_ThrowsCoreAccessDenied()
    {
        var locator = new FakeProfileLocator { Throws = new UnauthorizedAccessException() };

        var e = await Assert.ThrowsAsync<CoreException>(() => new ProfileService(locator).GetSavesAsync(Profile("a"), default));

        Assert.Equal(CoreError.AccessDenied, e.Code);
    }

    [Fact]
    public async Task GetSavesAsync_NullProfile_ThrowsArgumentNull() =>
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => new ProfileService(new FakeProfileLocator()).GetSavesAsync(null!, default));
}
