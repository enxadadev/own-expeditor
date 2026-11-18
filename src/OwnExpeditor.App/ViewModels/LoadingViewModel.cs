using System;
using System.Collections.Generic;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OwnExpeditor.App.Services;
using OwnExpeditor.Core.Session;
using OwnExpeditor.Domain.Saves;

namespace OwnExpeditor.App.ViewModels;

public enum StageState { Done, Current, Pending }

public sealed record LoadingStageItem(string Text, StageState State)
{
    public bool IsDone => State == StageState.Done;

    public bool IsCurrent => State == StageState.Current;

    public bool IsPending => State == StageState.Pending;
}

/// <summary>Lendo o save: barra, etapa atual, lista de etapas e Cancelar (design/shell, tela 3).</summary>
public sealed partial class LoadingViewModel : LocalizedViewModel
{
    private static readonly string[] Stages = [LoadStages.Read, LoadStages.Parse, LoadStages.Index];

    private Action? _cancel;
    private LoadProgress? _progress;
    private SaveSlotInfo? _save;

    public LoadingViewModel(ILocalizer loc) : base(loc)
    {
        CancelCommand = new RelayCommand(Cancel, () => !IsCancelling);
    }

    public RelayCommand CancelCommand { get; }

    /// <summary>Cargas mais rápidas que isso não mostram a tela (sem piscar); as amostras de design usam zero.</summary>
    public TimeSpan ShowDelay { get; set; } = TimeSpan.FromMilliseconds(300);

    [ObservableProperty] [NotifyCanExecuteChangedFor(nameof(CancelCommand))] private bool _isCancelling;

    public string SaveName => _save is null ? "" : $"{_save.Profile.DisplayName} · {_save.DisplayName}";

    /// <summary>0–1; <c>null</c> = indeterminado (<c>Percent &lt; 0</c> ou nada reportado ainda).</summary>
    public double? Progress => _progress is { Percent: >= 0 } p ? Math.Clamp(p.Percent, 0, 100) / 100.0 : null;

    public bool IsIndeterminate => Progress is null;

    public string PercentText => Progress is { } v ? Loc.Get("Loading_Progress_A11y", Math.Round(v * 100)) : "";

    public string PercentShort => Progress is { } v ? $"{Math.Round(v * 100).ToString("N0", Loc.Culture)}%" : "";

    public string StageText => _progress is { } p && Stages.Contains(p.StageKey) ? Loc[p.StageKey] : Loc["Loading_Default"];

    public string SizeText => _progress?.FileSizeBytes is { } bytes
        ? Loc.Get("Loading_Size", bytes / 1024.0 / 1024.0)
        : _save is { SizeBytes: > 0 } s ? Loc.Get("Loading_Size", s.SizeBytes / 1024.0 / 1024.0) : "";

    public string AutomationName => Progress is { } v ? Loc.Get("Loading_Automation", Math.Round(v * 100)) : Loc["Loading_Title"];

    /// <summary>Lista de etapas com ✓ / atual / pendente; só quando o percentual é conhecido.</summary>
    public IReadOnlyList<LoadingStageItem> StageItems
    {
        get
        {
            if (IsIndeterminate) return [];
            var current = Math.Max(0, Array.IndexOf(Stages, _progress!.StageKey));
            return Stages.Select((key, i) => new LoadingStageItem(Loc[key],
                i < current ? StageState.Done : i == current ? StageState.Current : StageState.Pending)).ToList();
        }
    }

    /// <summary>Começa uma carga nova; <paramref name="cancel"/> é chamado por Cancelar.</summary>
    public void Begin(SaveSlotInfo save, Action cancel)
    {
        _save = save;
        _cancel = cancel;
        _progress = null;
        IsCancelling = false;
        OnPropertyChanged(string.Empty);
    }

    public void Report(LoadProgress progress)
    {
        _progress = progress;
        OnPropertyChanged(string.Empty);
    }

    public void Cancel()
    {
        if (IsCancelling) return;
        IsCancelling = true;
        OnPropertyChanged(string.Empty);
        _cancel?.Invoke();
    }

    public string CancelText => Loc[IsCancelling ? "Loading_Cancelling" : "Loading_Cancel"];
}
