using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OwnExpeditor.App.Services;
using OwnExpeditor.Core;
using OwnExpeditor.Domain.Games;
using OwnExpeditor.Domain.Saves;

namespace OwnExpeditor.App.ViewModels;

/// <summary>Qual das telas de erro de abertura mostrar (design/shell, seção 4).</summary>
public enum OpenErrorKind { Parse, Corrupt, NotFound, AccessDenied, Unknown }

/// <summary>
/// Erro ao abrir o save, no lugar do módulo (nunca diálogo). "Não foi possível entender" (<c>SaveParseFailed</c>) e
/// "Não foi possível ler" (<c>SaveCorrupt</c>, não encontrado, sem permissão) têm causas e ações diferentes.
/// </summary>
public sealed partial class OpenErrorViewModel : LocalizedViewModel
{
    private const string ParseDetailsKey = "OpenError_Parse_DetailsText";

    private readonly IClipboardService _clipboard;
    private readonly IShellLauncher _launcher;
    private readonly CoreException _error;
    private readonly SaveSlotInfo _save;
    private readonly string _appVersion;
    private readonly TimeProvider _time;
    private readonly IUiDispatcher _ui;
    private ITimer? _copiedTimer;

    public OpenErrorViewModel(ILocalizer loc, IClipboardService clipboard, IShellLauncher launcher, CoreException error,
        SaveSlotInfo save, string appVersion, TimeProvider time, IUiDispatcher ui, Action retry, Action chooseAnother) : base(loc)
    {
        _clipboard = clipboard;
        _launcher = launcher;
        _error = error;
        _save = save;
        _appVersion = appVersion;
        _time = time;
        _ui = ui;
        Kind = KindFor(error.Code);
        RetryCommand = new RelayCommand(retry);
        ChooseAnotherCommand = new RelayCommand(chooseAnother);
    }

    public OpenErrorKind Kind { get; }

    public RelayCommand RetryCommand { get; }

    public RelayCommand ChooseAnotherCommand { get; }

    public SaveSlotInfo Save => _save;

    [ObservableProperty] private bool _isDetailsOpen;
    [ObservableProperty] [NotifyPropertyChangedFor(nameof(CopyText))] private bool _isCopied;

    public static OpenErrorKind KindFor(CoreError code) => code switch
    {
        CoreError.SaveParseFailed => OpenErrorKind.Parse,
        CoreError.SaveCorrupt => OpenErrorKind.Corrupt,
        CoreError.SaveNotFound => OpenErrorKind.NotFound,
        CoreError.AccessDenied => OpenErrorKind.AccessDenied,
        _ => OpenErrorKind.Unknown,
    };

    private string Prefix => Kind switch
    {
        OpenErrorKind.Parse => "OpenError_Parse",
        OpenErrorKind.Corrupt => "OpenError_Corrupt",
        OpenErrorKind.NotFound => "OpenError_NotFound",
        OpenErrorKind.AccessDenied => "OpenError_AccessDenied",
        _ => "OpenError_Unknown",
    };

    public string Title => Loc[Prefix + "_Title"];

    public string TitleUpper => Title.ToUpper(Loc.Culture);

    public string Body => Loc[Prefix + "_Body"];

    public string ProfileSaveText => $"{_save.Profile.DisplayName} · {_save.DisplayName}";

    public bool ShowCauses => Kind == OpenErrorKind.Parse;

    public string CausesTitleUpper => Loc["OpenError_Parse_CausesTitle"].ToUpper(Loc.Culture);

    public IReadOnlyList<string> Causes => Kind == OpenErrorKind.Parse
        ? [Loc["OpenError_Parse_Cause1"], Loc["OpenError_Parse_Cause2"], Loc["OpenError_Parse_Cause3"]]
        : [];

    public IReadOnlyList<string> Tips => Kind switch
    {
        OpenErrorKind.Parse => [Loc["OpenError_Parse_Tip1"], Loc["OpenError_Parse_Tip2"]],
        OpenErrorKind.Corrupt => [Loc["OpenError_Corrupt_Tip1"], Loc["OpenError_Corrupt_Tip2"]],
        _ => [],
    };

    public bool ShowTips => Tips.Count > 0;

    /// <summary>Bloco "Detalhes técnicos": só código, jogo, perfil, save, tamanho, etapa e versão (nunca o conteúdo do save).</summary>
    public string DetailsText => Kind == OpenErrorKind.Parse ? ParseDetails() : ShortDetails();

    public string DetailsLabel => Loc["OpenError_Details"];

    public bool ShowCopyDetails => Kind == OpenErrorKind.Parse;

    public bool ShowOpenFolder => Kind == OpenErrorKind.Parse;

    public string CopyText => Loc[IsCopied ? "OpenError_Copied" : "OpenError_CopyDetails"];

    private string ShortDetails() => _error.Line is { } line
        ? Loc.Get("OpenError_DetailsText", _error.Code, line)
        : Loc.Get("OpenError_DetailsCode", _error.Code);

    private string ParseDetails()
    {
        var game = Loc[_save.Profile.Game == GameKind.Ats ? "Game_Ats" : "Game_Ets2"];
        var size = _save.SizeBytes > 0 ? Loc.Get("Loading_Size", _save.SizeBytes / 1024.0 / 1024.0) : "-";
        var stage = _error.StageKey is { } key ? Loc[key] : "-";
        return Loc.Get(ParseDetailsKey, _error.Code, game, _save.Profile.DisplayName, _save.DisplayName, size, stage, _appVersion);
    }

    [RelayCommand]
    private void ToggleDetails() => IsDetailsOpen = !IsDetailsOpen;

    [RelayCommand]
    private async Task CopyDetailsAsync()
    {
        await _clipboard.SetTextAsync(DetailsText);
        IsCopied = true;
        _copiedTimer?.Dispose();
        _copiedTimer = _time.CreateTimer(_ => _ui.Post(() => IsCopied = false), null, TimeSpan.FromSeconds(2), Timeout.InfiniteTimeSpan);
    }

    [RelayCommand]
    private async Task OpenFolderAsync()
    {
        if (Path.GetDirectoryName(_save.GameSiiPath) is { } dir) await _launcher.OpenFolderAsync(dir);
    }

    protected override void OnDisposed() => _copiedTimer?.Dispose();
}
