using System;
using System.Collections.Generic;
using System.Linq;
using CommunityToolkit.Mvvm.Input;
using OwnExpeditor.App.Controls;
using OwnExpeditor.App.ViewModels;
using OwnExpeditor.Core.Modules.Jobs;

namespace OwnExpeditor.App.Modules.Jobs;

/// <summary>O que a View lê: textos, visibilidade e listas derivadas do estado do fluxo. Sem regra de negócio.</summary>
public sealed partial class JobsPageViewModel
{
    private IReadOnlyList<PendingJobViewModel> _pendingRows = [];
    private IReadOnlyList<JobExpirationChoice> _expirationChoices = [];

    // ---- página

    public string Title => Loc["Module_" + Id];

    public bool IsLoading => State == PageState.Loading;

    public bool HasLoadError => State == PageState.Error;

    public bool IsEmptyModule => State == PageState.Empty;

    public bool ShowForm => State == PageState.Ready;

    // ---- avisos

    public bool ShowNoCatalog => ShowForm && _session is { GameData.IsAvailable: false };

    public string NoCatalogTitle => Loc.Get("Jobs_NoCatalog_Title", Loc["Game_" + _session?.Game]);

    public bool ShowCloud => ShowForm && _session is { Save.Profile.IsSteamCloud: true } && !_cloudDismissed;

    public bool ShowEditing => IsEditing;

    /// <summary>Só por ação do usuário: "Limpamos …" e "…: só restou uma opção, então ela foi escolhida."</summary>
    public string? Notice
    {
        get
        {
            var parts = new List<string>();
            if (_noticeCleared.Count > 0) parts.Add(Loc.Get("Jobs_Notice_Cleared", FieldNames(_noticeCleared)));
            var auto = AutoNotice;
            if (auto.Count > 0)
            {
                var names = FieldNames(auto);
                parts.Add(Loc.Get("Jobs_Notice_Replaced", char.ToUpper(names[0], Loc.Culture) + names[1..]));
            }
            return parts.Count == 0 ? null : string.Join(" ", parts);
        }
    }

    public bool HasNotice => _noticeCleared.Count > 0 || AutoNotice.Count > 0;

    /// <summary>A vaga fica de fora: escolhida sozinha ela fica oculta (1 opção) ou com 2+ opções nunca é automática, então o aviso seria falso.</summary>
    private IReadOnlyList<JobField> AutoNotice => [.. _noticeAuto.Where(f => f != JobField.Slot)];

    public string? ErrorMessage => _error;

    public bool HasError => _error is not null;

    private string FieldNames(IEnumerable<JobField> fields) => string.Join(", ", fields.Select(f => Loc["Jobs_Missing_" + f]));

    // ---- campos

    public bool ShowCountries => _board?.HasCountries ?? false;

    /// <summary>A vaga só aparece com 2+ opções (ou travada, na edição).</summary>
    public bool ShowSlot => _board is not null && _sel.FromCompany is { } from && (IsEditing || JobSelectionRules.SlotOptionCount(_board, from) >= 2);

    public bool IsDepartureLocked => IsEditing;

    public bool ShowNoSlots => ShowForm && _sel.FromCity is not null && !_hasDepartureCompanies;

    public string NoSlotsTitle => _sel.FromCity is null || _board is null ? "" : Loc.Get("Jobs_NoSlots_Title", CityName(_sel.FromCity));

    public string NoSlotsBody => _sel.FromCity is null || _board is null ? "" : Loc.Get("Jobs_NoSlots_Body", CityName(_sel.FromCity));

    public bool ShowDetails => _sel.Cargo is not null;

    public bool FromDone => _board is not null && !_missing.Any(f => f <= JobField.Slot);

    public bool ToDone => _board is not null && !_missing.Any(f => f is >= JobField.ToCountry and <= JobField.ToCompany);

    public bool CargoDone => _sel.Cargo is not null;

    public bool ShowPendingCard => ShowForm && !IsEditing && _pendingRows.Count > 0;

    public IReadOnlyList<PendingJobViewModel> PendingRows => _pendingRows;

    public bool CanSwapCities => CanSwap;

    // ---- mapa (último bloco da página)

    private bool HasBothCities => _board is not null && _sel.FromCity is not null && _sel.ToCity is not null;

    public MapPoint? MapOrigin => HasBothCities ? new MapPoint(CityName(_sel.FromCity!)) : null;

    public MapPoint? MapDestination => HasBothCities ? new MapPoint(CityName(_sel.ToCity!)) : null;

    public string? MapRouteLabel => !HasBothCities ? null
        : _sel.FromCompany is not null && _sel.ToCompany is not null
            ? $"{CompanyName(_sel.FromCompany)} · {CityName(_sel.FromCompany.City)} → {CompanyName(_sel.ToCompany)} · {CityName(_sel.ToCompany.City)}"
            : $"{Loc["Jobs_Map_Route"]}: {CityName(_sel.FromCity!)} → {CityName(_sel.ToCity!)}";

    public string? MapDistanceText => _distance is null ? null
        : _distance.Source == DistanceSource.Estimated ? $"{KmText(_distance)} · {Loc["Jobs_Distance_EstimatedShort"]}" : KmText(_distance);

    /// <summary>O mapa é o último bloco da página, com 300 px de altura e largura total (design/fretes-pais-cidade §1).</summary>
    public double MapHeight => 300;

    public string MapEmptyTitle => Loc["Jobs_Map_Empty"];

    public string MapNote => Loc["Jobs_Map_Schematic"];

    // ---- detalhes (só com a carga)

    /// <summary>Traço de "sem valor" (símbolo, igual nos dois idiomas).</summary>
    private const string NoValue = "—";

    public string DistanceValue => _distance is null ? NoValue : KmText(_distance);

    public bool IsDistanceEstimated => _distance?.Source == DistanceSource.Estimated;

    public string? DistanceTag => IsDistanceEstimated ? Loc["Jobs_Distance_Estimated"] : null;

    public IReadOnlyList<JobExpirationChoice> ExpirationChoices => _expirationChoices;

    public JobExpirationChoice? SelectedExpiration
    {
        get => _expirationChoices.FirstOrDefault(c => c.Hours == _hours);
        set
        {
            if (value is not null) SelectExpiration(value.Hours);
        }
    }

    private JobTemplate? Template => _board is not null && _sel.Cargo is not null ? _board.Template(_sel.Cargo) : null;

    public string UnitsValue => Template is { } t ? t.UnitsCount.ToString("N0", Loc.Culture) : NoValue;

    public string TrailerValue => Template is { } t ? _board!.Names.Get(Domain.Games.GameNameKind.Trailer, TrailerId(t)) : NoValue;

    private string HoursText(int hours) => hours % 24 == 0 && hours >= 168 ? Loc.Get("Format_Days", hours / 24) : Loc.Get("Format_Hours", hours);

    // ---- ação

    public bool CanAdd => _board is not null && State == PageState.Ready && _missing.Count == 0 && _sel.ToCompany is not null && _sel.Slot is not null;

    public string AddText => Loc[IsEditing ? "Jobs_Update" : "Jobs_Add"];

    /// <summary>"Falta escolher: …": até 3 campos na ordem da cadeia; com mais, agrupa em saída, chegada e carga.</summary>
    public string MissingText
    {
        get
        {
            if (_missing.Count == 0)
            {
                if (CanAdd) return Loc["Jobs_Ready"];
                // saída escolhida, mas sem vaga nem oferta a usar (0 opções): nada a escolher, então o motivo é o texto
                return State == PageState.Ready && _board is not null && _sel.FromCompany is not null && _sel.Slot is null ? Loc["Jobs_Missing_NoSlot"] : "";
            }
            var names = _missing.Count <= 3
                ? _missing.Select(f => Loc["Jobs_Missing_" + f])
                : Groups();
            return Loc.Get("Jobs_Missing", string.Join(", ", names));
        }
    }

    private IEnumerable<string> Groups()
    {
        if (_missing.Any(f => f <= JobField.Slot)) yield return Loc["Jobs_Missing_GroupFrom"];
        if (_missing.Any(f => f is >= JobField.ToCountry and <= JobField.ToCompany)) yield return Loc["Jobs_Missing_GroupTo"];
        if (_missing.Contains(JobField.Cargo)) yield return Loc["Jobs_Missing_Cargo"];
    }

    // ---- cartão de pendentes

    private IReadOnlyList<PendingJobViewModel> BuildPending()
    {
        if (_board is null || _sel.FromCity is not { } city) return [];
        return PendingJobs().Where(p => p.Staged.Request.Origin.City == city).Select(p =>
        {
            var (key, staged) = p;
            var r = staged.Request;
            var title = $"{CompanyName(r.Origin)} · {CityName(r.Origin.City)} → {CompanyName(r.Target)} · {CityName(r.Target.City)}";
            var detail = string.Join(" · ", CargoName(r.Cargo), KmText(_board.Distance(r.Origin.City, r.Target.City)),
                HoursText(r.ExpirationHours), Loc[r.Mode == JobSlotMode.ReplaceOffer ? "Jobs_Pending_Mode_Replace" : "Jobs_Pending_Mode_Free"]);
            return new PendingJobViewModel(key, title, detail, Loc.Get("Jobs_Pending_EditAria", title), Loc.Get("Review_DiscardItem_Aria", title),
                new RelayCommand(() => EditPending(key)), new RelayCommand(() => DiscardPending(key)));
        }).ToList();
    }
}
