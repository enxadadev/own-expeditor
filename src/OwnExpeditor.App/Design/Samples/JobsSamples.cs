using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using OwnExpeditor.App.Controls;
using OwnExpeditor.App.Modules.Jobs;
using OwnExpeditor.App.Services;
using OwnExpeditor.App.ViewModels;
using OwnExpeditor.Core.Games;
using OwnExpeditor.Core.Modules.Jobs;
using OwnExpeditor.Core.Session;
using OwnExpeditor.Domain.Games;
using OwnExpeditor.Domain.Saves;
using OwnExpeditor.Domain.Settings;

namespace OwnExpeditor.App.Design.Samples;

/// <summary>
/// Amostras do módulo Fretes (uma por tela de design/fretes): o shell real com o <c>JobsPageViewModel</c> real sobre um save
/// sintético (ids inventados; nomes nos dois idiomas). A sessão só lê e enfileira; nada vai ao disco.
/// </summary>
internal static class JobsSamples
{
    public static IReadOnlyList<DesignSample> All() =>
    [
        // Escolher.dc.html
        new("Jobs_01_Start", () => Scenario(_ => { })),
        new("Jobs_02_Country", () => Scenario(v => Pick(v.FromCountry, "bulgaria"))),
        new("Jobs_03_Departure", () => Scenario(v => { Pick(v.FromCountry, "bulgaria"); Pick(v.FromCompany, Radus); })),
        new("Jobs_04_Arrival", () => Scenario(v => { Pick(v.FromCountry, "bulgaria"); Pick(v.FromCompany, Radus); Pick(v.ToCountry, "turkey"); })),
        new("Jobs_05_Ready", () => Scenario(Ready)),
        new("Jobs_06_Added", () => Scenario(v => { Ready(v); v.AddCommand.Execute(null); })),
        new("Jobs_07_Error", () => Scenario(Ready, text: DesignJobsData.SaveText(economy: false), after: v => v.AddCommand.Execute(null))),
        // Seletores.dc.html
        new("Jobs_20_FromCountryPicker", () => Scenario(v => v.FromCountry.IsOpen = true)),
        new("Jobs_21_FromCityPicker", () => Scenario(v => { Pick(v.FromCountry, "austria"); v.FromCity.IsOpen = true; })),
        new("Jobs_22_ToCountryPicker", () => Scenario(v => { Pick(v.FromCountry, "bulgaria"); Pick(v.FromCompany, Radus); v.ToCountry.IsOpen = true; })),
        new("Jobs_23_ToCityPicker", () => Scenario(v => { Pick(v.FromCountry, "bulgaria"); Pick(v.FromCompany, Radus); Pick(v.ToCountry, ""); v.ToCity.IsOpen = true; })),
        new("Jobs_24_SlotPicker", () => Scenario(v => { Pick(v.FromCountry, "bulgaria"); Pick(v.FromCompany, Radus); v.Slot.IsOpen = true; })),
        new("Jobs_25_OnlyOption", () => Scenario(v => Pick(v.FromCountry, "hungary"))),
        new("Jobs_26_OffersOnly", () => Scenario(v => Pick(v.FromCountry, "turkey"))),
        // Estados.dc.html
        new("Jobs_40_NoCatalog", () => Scenario(v => { Pick(v.FromCity, "sofia"); Pick(v.FromCompany, Radus); Pick(v.ToCity, "belgrad"); }, catalog: false)),
        new("Jobs_41_NoOrigin", () => Scenario(_ => { }, text: DesignJobsData.SaveText(slots: false))),
        new("Jobs_42_Pending", () => Scenario(v => { Ready(v); v.AddCommand.Execute(null); })),
        new("Jobs_43_EditingFree", () => Scenario(v => { Ready(v); v.AddCommand.Execute(null); v.PendingRows[0].EditCommand.Execute(null); })),
        new("Jobs_44_EditingReplace", () => Scenario(v =>
        {
            Pick(v.FromCountry, "bulgaria");
            Pick(v.FromCompany, Radus);
            Pick(v.Slot, JobSlotChoice.Replace("_nameless.a"));
            Pick(v.ToCountry, "turkey");
            Pick(v.Cargo, "machinery");
            v.AddCommand.Execute(null);
            v.PendingRows[0].EditCommand.Execute(null);
        })),
        new("Jobs_45_Swap", () => Scenario(v => { Ready(v); v.SwapCommand.Execute(null); })),
        new("Jobs_46_Cleared", () => Scenario(v => { Ready(v); Pick(v.FromCountry, "serbia"); })),
        new("Jobs_47_NothingFree", () => Scenario(v =>
        {
            Pick(v.FromCountry, "turkey");
            Pick(v.ToCountry, "serbia");
            Pick(v.Cargo, "bricks");
            v.AddCommand.Execute(null);
        }, text: DesignJobsData.SaveText(oneOfferInIstanbul: true))),
        new("Jobs_70_SteamCloud", () => Scenario(v => { Pick(v.FromCountry, "bulgaria"); Pick(v.FromCompany, Radus); }, cloud: true)),
    ];

    private static readonly CompanyLocation Radus = new("radus", "sofia");

    // ---- montagem

    private static ShellViewModel Scenario(Action<JobsPageViewModel> drive, string? text = null, bool catalog = true,
        bool cloud = false, Action<JobsPageViewModel>? after = null)
    {
        var save = DesignData.Save4(cloud);
        var data = new DesignJobsData(catalog);
        var host = new DesignHost
        {
            Open = (s, _, _) => Task.FromResult<IEditSession>(new DesignSession(
                EditSessionFactory.FromText(text ?? DesignJobsData.SaveText(), s, data),
                (_, _) => throw new NotSupportedException())),
        };
        var shell = ShellSamples.Boot(host, extra: s => s.AddSingleton<IJobBoardFactory>(new DesignBoardFactory()));
        shell.OpenSaveAsync(save).GetAwaiter().GetResult();
        var page = (JobsPageViewModel)shell.CurrentPage!;
        drive(page);
        after?.Invoke(page);
        return shell;
    }

    private static void Ready(JobsPageViewModel v)
    {
        Pick(v.FromCountry, "bulgaria");
        Pick(v.FromCompany, Radus);
        Pick(v.ToCountry, "turkey");
        Pick(v.Cargo, "machinery");
    }

    private static void Pick(JobPickerViewModel picker, object key)
    {
        var option = picker.AllOptions.FirstOrDefault(o => Equals(o.Key, key));
        if (option is not null) picker.Selected = option; // sem a opção, a amostra segue sem ela (ex.: sem pasta do jogo)
    }

    private sealed class DesignBoardFactory : IJobBoardFactory
    {
        public JobBoard Create(IEditSession session) => JobBoard.Build(session.View, session.GameData);

        public Task<int> RefreshDistancesAsync(GameKind game, CancellationToken ct) => Task.FromResult(0);
    }
}

/// <summary>Save e regras de carga sintéticos para as amostras (ids inventados; nomes em pt-BR e en).</summary>
internal sealed class DesignJobsData(bool catalog) : IGameData
{
    private static readonly Dictionary<string, (string Pt, string En)> Names = new()
    {
        ["sofia"] = ("Sófia", "Sofia"), ["belgrad"] = ("Belgrado", "Belgrade"), ["istanbul"] = ("Istambul", "Istanbul"),
        ["budapest"] = ("Budapeste", "Budapest"), ["wien"] = ("Viena", "Vienna"), ["praha"] = ("Praga", "Prague"), ["oslo"] = ("Oslo", "Oslo"),
        ["radus"] = ("Radus", "Radus"), ["tradeaux"] = ("Tradeaux", "Tradeaux"), ["lognstick"] = ("LogNStick", "LogNStick"),
        ["kaarfor"] = ("Kaarfor", "Kaarfor"), ["dfh"] = ("DFH", "DFH"), ["lkwlog"] = ("LKW Log", "LKW Log"),
        ["euroacres"] = ("EuroAcres", "EuroAcres"), ["nordfrakt"] = ("Nordfrakt", "Nordfrakt"), ["graz"] = ("Graz", "Graz"),
        ["bulgaria"] = ("Bulgária", "Bulgaria"), ["serbia"] = ("Sérvia", "Serbia"), ["turkey"] = ("Turquia", "Turkey"),
        ["hungary"] = ("Hungria", "Hungary"), ["austria"] = ("Áustria", "Austria"),
        ["machinery"] = ("Maquinário", "Machinery"), ["concrete_beams"] = ("Vigas de concreto", "Concrete beams"),
        ["bricks"] = ("Tijolos", "Bricks"), ["used_plastic"] = ("Plástico usado", "Used plastic"),
        ["lowboy"] = ("Rebaixado", "Lowboy"), ["flatbed"] = ("Plataforma", "Flatbed"), ["curtain"] = ("Cortina", "Curtain"),
    };

    private static readonly Dictionary<string, string[]> Out = new()
    {
        ["radus"] = ["machinery", "bricks"], ["kaarfor"] = ["machinery", "used_plastic"], ["tradeaux"] = ["concrete_beams", "bricks"],
        ["lkwlog"] = ["used_plastic", "machinery"], ["lognstick"] = ["bricks", "concrete_beams"], ["dfh"] = ["machinery"], ["euroacres"] = ["used_plastic"],
    };

    private static readonly Dictionary<string, string[]> In = new()
    {
        ["tradeaux"] = ["machinery", "used_plastic"], ["radus"] = ["concrete_beams", "bricks"], ["lkwlog"] = ["bricks", "machinery"],
        ["lognstick"] = ["machinery", "used_plastic"], ["kaarfor"] = ["concrete_beams", "bricks"], ["dfh"] = ["machinery", "used_plastic", "bricks"],
        ["euroacres"] = ["machinery"],
    };

    public GameKind Game => GameKind.Ets2;

    public bool IsAvailable => catalog;

    public string Get(GameNameKind kind, string id) =>
        catalog && Names.TryGetValue(id, out var n) ? (Localizer.Instance.Language == Language.PtBr ? n.Pt : n.En) : GameIds.Humanize(id);

    public bool Ships(string company, string cargo) => catalog && Out.TryGetValue(company, out var c) && c.Contains(cargo);

    public bool Accepts(string company, string cargo) => catalog && In.TryGetValue(company, out var c) && c.Contains(cargo);

    public IReadOnlyCollection<string> CargoOut(string company) => catalog && Out.TryGetValue(company, out var c) ? c : [];

    /// <summary>Praga e Oslo ficam sem país (como cidades de mod): caem em "Outros".</summary>
    public string? CityCountry(string city) => catalog ? Countries.GetValueOrDefault(city) : null;

    private static readonly Dictionary<string, string> Countries = new()
    {
        ["sofia"] = "bulgaria", ["belgrad"] = "serbia", ["istanbul"] = "turkey", ["budapest"] = "hungary", ["wien"] = "austria", ["graz"] = "austria",
    };


    private static string Job(string id, string target, string cargo, int km, string variant, int units, string expiration = "1900") =>
        $"job_offer_data : {id} {{\r\n target: {target}\r\n expiration_time: {expiration}\r\n urgency: 0\r\n shortest_distance_km: {km}\r\n" +
        $" ferry_time: 0\r\n ferry_price: 0\r\n cargo: {cargo}\r\n company_truck: truck_demo\r\n trailer_variant: {variant}\r\n" +
        $" trailer_definition: trailer_def.{variant}\r\n units_count: {units}\r\n fill_ratio: 1\r\n trailer_place: 0\r\n}}\r\n\r\n";

    private static string Free(string id) => Job(id, "\"\"", "null", 0, "null", 0, "nil");

    private static string Company(string key, params string[] offers) =>
        $"company : company.volatile.{key} {{\r\n job_offer: {offers.Length}\r\n" +
        string.Concat(offers.Select((o, i) => $" job_offer[{i}]: {o}\r\n")) + "}\r\n\r\n";

    /// <param name="economy">Sem o bloco da economia, o Core não sabe a hora do jogo e recusa o frete (erro natural da amostra).</param>
    /// <param name="slots">Sem vagas livres nem ofertas (só as empresas, sem fretes).</param>
    /// <param name="oneOfferInIstanbul">Istambul com uma só oferta e nenhuma vaga (para esgotar a cidade com pendências).</param>
    public static string SaveText(bool economy = true, bool slots = true, bool oneOfferInIstanbul = false)
    {
        if (!slots)
            return "SiiNunit\r\n{\r\neconomy : _nameless.1 {\r\n game_time: 1000\r\n}\r\n\r\n" + Company("radus.sofia") + Company("tradeaux.belgrad") + "}\r\n";
        return "SiiNunit\r\n{\r\n" + (economy ? "economy : _nameless.1 {\r\n game_time: 1000\r\n}\r\n\r\n" : "") +
            Company("radus.sofia", "_nameless.a", "_nameless.f1", "_nameless.f2") +
            Company("kaarfor.sofia", "_nameless.f3") +
            Company("tradeaux.belgrad", "_nameless.b", "_nameless.f4") +
            Company("lkwlog.belgrad", "_nameless.f5") +
            (oneOfferInIstanbul ? Company("lognstick.istanbul", "_nameless.c") : Company("lognstick.istanbul", "_nameless.c", "_nameless.d")) +
            Company("kaarfor.budapest", "_nameless.e", "_nameless.f6") +
            Company("dfh.wien", "_nameless.f7") +
            Company("dfh.graz", "_nameless.f9") +
            Company("euroacres.praha", "_nameless.f8") +
            Company("nordfrakt.oslo") +
            Job("_nameless.a", "\"tradeaux.belgrad\"", "cargo.machinery", 394, "lowboy", 1, "1700") +
            Job("_nameless.b", "\"radus.sofia\"", "cargo.concrete_beams", 394, "flatbed", 24, "1800") +
            Job("_nameless.c", "\"tradeaux.belgrad\"", "cargo.bricks", 980, "flatbed", 22, "2300") +
            Job("_nameless.d", "\"lkwlog.belgrad\"", "cargo.used_plastic", 980, "curtain", 33, "2000") +
            Job("_nameless.e", "\"dfh.wien\"", "cargo.machinery", 245, "lowboy", 1, "1800") +
            string.Concat(Enumerable.Range(1, 9).Select(i => Free($"_nameless.f{i}"))) +
            "}\r\n";
    }
}
