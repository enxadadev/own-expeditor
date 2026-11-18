using System.Globalization;
using OwnExpeditor.Data.Games;
using OwnExpeditor.Data.Settings;
using OwnExpeditor.Domain.Games;
using OwnExpeditor.Domain.Settings;

namespace OwnExpeditor.Core.Settings;

/// <summary>
/// Configurações do app. Padrão: tema do sistema; idioma pt-BR se o SO for pt-*, senão en.
/// Uma pasta de jogo nova que não passa em <c>Validate</c> é recusada e nada é gravado.
/// </summary>
internal sealed class AppSettingsService(ISettingsStore store, IGameInstallLocator locator,
    Func<CultureInfo>? uiCulture = null) : IAppSettingsService
{
    private AppSettings _current = new(ThemePreference.System,
        (uiCulture ?? (() => CultureInfo.CurrentUICulture))().TwoLetterISOLanguageName == "pt" ? Language.PtBr : Language.En,
        null, new GameFolders());

    public AppSettings Current => Volatile.Read(ref _current);

    public event EventHandler<AppSettings>? Changed;

    public async Task LoadAsync(CancellationToken ct)
    {
        var loaded = await store.LoadAsync(ct).ConfigureAwait(false);
        if (loaded is null || loaded == Current) return; // primeira execução: segue com os padrões
        Volatile.Write(ref _current, loaded);
        Changed?.Invoke(this, loaded);
    }

    public async Task UpdateAsync(AppSettings settings, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(settings);
        var current = Current;
        foreach (var game in Enum.GetValues<GameKind>())
        {
            var path = settings.GameFolders.For(game);
            // Só valida o que mudou: uma pasta antiga que sumiu não pode travar a troca de tema.
            if (path is null || path == current.GameFolders.For(game)) continue;
            if (locator.Validate(game, path) != GameFolderCheck.Ok)
                throw new CoreException(CoreError.InvalidEdit, $"Pasta do jogo inválida ({game}): '{path}'.");
        }
        if (settings == current) return;

        try
        {
            await store.SaveAsync(settings, ct).ConfigureAwait(false);
        }
        catch (UnauthorizedAccessException e)
        {
            throw new CoreException(CoreError.AccessDenied, e.Message, e);
        }
        catch (IOException e)
        {
            throw new CoreException(CoreError.WriteFailed, e.Message, e);
        }
        Volatile.Write(ref _current, settings);
        Changed?.Invoke(this, settings);
    }
}
