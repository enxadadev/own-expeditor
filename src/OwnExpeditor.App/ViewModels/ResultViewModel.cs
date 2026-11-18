using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using OwnExpeditor.App.Services;
using OwnExpeditor.Core.Session;
using OwnExpeditor.Core.Text;

namespace OwnExpeditor.App.ViewModels;

/// <summary>Página Resultado (shell): onde ficou o save gravado e, se sobrescreveu, o backup (design/review-apply, tela 3).</summary>
public sealed partial class ResultViewModel : LocalizedViewModel
{
    private readonly IShellLauncher _launcher;
    private readonly IClipboardService _clipboard;
    private readonly IToastService _toast;
    private readonly ApplyResult _result;
    private readonly bool _isNew;
    private readonly bool _isSteamCloud;

    public ResultViewModel(ILocalizer loc, IShellLauncher launcher, IClipboardService clipboard, IToastService toast,
        ApplyResult result, ApplyTarget target, bool isSteamCloud, Action keepEditing) : base(loc)
    {
        _launcher = launcher;
        _clipboard = clipboard;
        _toast = toast;
        _result = result;
        _isNew = target is ApplyTarget.NewSlot;
        _isSteamCloud = isSteamCloud;
        KeepEditingCommand = new RelayCommand(keepEditing);
    }

    public RelayCommand KeepEditingCommand { get; }

    public bool IsNewSave => _isNew;

    public bool HasBackup => !_isNew && _result.BackupPath is not null;

    public string Title => Loc[_isNew ? "Result_Title_New" : "Result_Title"];

    public string TitleUpper => Title.ToUpper(Loc.Culture);

    public string SlotTitleUpper => SlotTitle.ToUpper(Loc.Culture);

    public string BackupTitleUpper => BackupTitle.ToUpper(Loc.Culture);

    public string NowTitleUpper => NowTitle.ToUpper(Loc.Culture);

    public string HelpTitleUpper => HelpTitle.ToUpper(Loc.Culture);

    public string Summary
    {
        get
        {
            var n = _result.ChangeSetCount;
            var name = _result.WrittenSlot.DisplayName;
            return _isNew
                ? n == 1 ? Loc.Get("Result_WrittenNew_One", name) : Loc.Get("Result_WrittenNew_Other", n, name)
                : n == 1 ? Loc["Result_Written_One"] : Loc.Get("Result_Written_Other", n);
        }
    }

    public string SlotTitle => Loc[_isNew ? "Result_Slot_Title_New" : "Result_Slot_Title"];

    public string SlotName => Loc.Get("Result_Slot_Name", _result.WrittenSlot.DisplayName, _result.WrittenSlot.Id);

    public string SlotHelp => Loc[_isNew ? "Result_Slot_Help_New" : "Result_Slot_Help"];

    public string SlotFolder => Path.GetDirectoryName(_result.WrittenSlot.GameSiiPath) ?? "";

    public string BackupTitle => Loc["Result_Backup_Title"];

    public string BackupHelp => Loc["Result_Backup_Help"];

    public string BackupPath => _result.BackupPath ?? "";

    public bool ShowCloud => _isSteamCloud;

    public string CloudText => Loc[_isNew ? "Result_Cloud_New" : "Result_Cloud_Overwrite"];

    public string NowTitle => Loc["Result_Now_Title"];

    public string NowBody => _isNew ? Loc.Get("Result_Now_Body_New", _result.WrittenSlot.DisplayName) : Loc["Result_Now_Body"];

    public string SessionNote => Loc["Result_Session_New"];

    public string HelpTitle => Loc[_isNew ? "Result_Discard_Title" : "Result_Restore_Title"];

    /// <summary>Passos numerados ("1. Feche o jogo.").</summary>
    public string[] HelpSteps => (_isNew
        ? [Loc["Result_Discard_Step1"], Loc["Result_Discard_Step2"]]
        : (string[])[Loc["Result_Restore_Step1"], Loc["Result_Restore_Step2"], Loc["Result_Restore_Step3"]])
        .Select((text, i) => $"{i + 1}. {text}").ToArray();

    public string TextNote => Loc[_isNew ? "Result_TextNote_New" : "Result_TextNote"];

    /// <summary>O que "Abrir pasta" e "Copiar caminho" tratam: o backup, ao sobrescrever; a pasta do novo save, ao criar.</summary>
    private string PrimaryPath => HasBackup ? BackupPath : SlotFolder;

    [RelayCommand]
    private async Task OpenFolderAsync()
    {
        var ok = HasBackup ? await _launcher.RevealFileAsync(BackupPath) : await _launcher.OpenFolderAsync(SlotFolder);
        if (!ok) _toast.Show(TextRef.Of("Result_OpenFolder_Failed", PrimaryPath));
    }

    [RelayCommand]
    private async Task CopyPathAsync()
    {
        await _clipboard.SetTextAsync(PrimaryPath);
        _toast.Show(TextRef.Of("Result_PathCopied"));
    }
}
