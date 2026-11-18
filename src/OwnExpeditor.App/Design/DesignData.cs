using System;
using OwnExpeditor.Core.Games;
using OwnExpeditor.Core.Session;
using OwnExpeditor.Core.Text;
using OwnExpeditor.Domain.Games;
using OwnExpeditor.Domain.Saves;

namespace OwnExpeditor.App.Design;

/// <summary>Dados sintéticos das amostras (ids inventados, nada de dados pessoais).</summary>
public static class DesignData
{
    public const string SaveText =
        "SiiNunit\r\n{\r\n" +
        "economy : _nameless.1 {\r\n game_time: 1000\r\n money_account: 4200000\r\n}\r\n\r\n" +
        "job_offer_data : _nameless.a {\r\n target: \"demo_co.city_b\"\r\n cargo: cargo.demo_goods\r\n distance: 569\r\n}\r\n\r\n" +
        "job_offer_data : _nameless.b {\r\n target: \"demo_co.city_d\"\r\n cargo: cargo.demo_other\r\n distance: 300\r\n}\r\n\r\n" +
        "}\r\n";

    public static ProfileInfo ProfileA(bool cloud = false) =>
        new(GameKind.Ets2, "44656D6F41", "Demo A", "/demo/profiles/44656D6F41", cloud);

    public static ProfileInfo ProfileB { get; } = new(GameKind.Ets2, "44656D6F42", "Demo B", "/demo/profiles/44656D6F42", false);

    public static ProfileInfo ProfileAts { get; } = new(GameKind.Ats, "44656D6F415453", "Demo ATS", "/demo/ats/44656D6F415453", false);

    public static SaveSlotInfo Slot(ProfileInfo profile, string id, string name, int minutesAgo, long size = 90_000_000) =>
        new(profile, id, name, $"{profile.DirectoryPath}/save/{id}/game.sii", DateTime.UtcNow.AddMinutes(-minutesAgo), size);

    public static SaveSlotInfo Save4(bool cloud = false) => Slot(ProfileA(cloud), "4", "Save 4", 12, 90_600_000);

    public static SaveSlotInfo[] SavesOf(ProfileInfo profile) =>
    [
        Slot(profile, "4", "Save 4", 12),
        Slot(profile, "autosave", "Autosave", 40),
        Slot(profile, "quicksave", "Quicksave", 95),
        Slot(profile, "2", "Rota longa", 2000),
    ];

    /// <summary>Sessão somente leitura com 3 pendências (2 de Fretes e 1 de Perfil), sobre o save sintético.</summary>
    public static IEditSession SessionWithChanges(SaveSlotInfo save, int changes = 3)
    {
        var session = EditSessionFactory.FromText(SaveText, save);
        if (changes >= 1)
            session.Stage(JobChange("jobs:slot:a", "_nameless.a", "Demo A", "Demo B", "demo_co.city_b", "demo_co.city_c", "cargo.demo_goods", "cargo.demo_other", 569, 394));
        if (changes >= 2)
            session.Stage(JobChange("jobs:slot:b", "_nameless.b", "Demo C", "Demo D", "demo_co.city_d", "demo_co.city_e", "cargo.demo_other", "cargo.demo_goods", 300, 410));
        if (changes >= 3)
            session.Stage(new ChangeSet("profile", "profile:money", TextRef.Of("Profile_Change_Money_Title"),
                [new DiffLine(TextRef.Of("Profile_Field_Money"), new DiffValue.Money(4_200_000, save.Profile.Game), new DiffValue.Money(5_000_000, save.Profile.Game))],
                [new SetAttribute("_nameless.1", "money_account", "4200000", "5000000")]));
        return session;
    }

    private static ChangeSet JobChange(string key, string unit, string from, string to, string oldTarget, string newTarget,
        string oldCargo, string newCargo, int oldKm, int newKm) =>
        new("jobs", key, TextRef.Of("Jobs_Change_Title", from, to),
            [
                new DiffLine(TextRef.Of("Jobs_Field_Destination"), new DiffValue.Raw(oldTarget), new DiffValue.Raw(newTarget)),
                new DiffLine(TextRef.Of("Jobs_Field_Cargo"), new DiffValue.GameName(GameNameKind.Cargo, oldCargo), new DiffValue.GameName(GameNameKind.Cargo, newCargo)),
                new DiffLine(TextRef.Of("Jobs_Field_Distance"), new DiffValue.Number(oldKm, "Format_Km"), new DiffValue.Number(newKm, "Format_Km")),
            ],
            [
                new SetAttribute(unit, "target", $"\"{oldTarget}\"", $"\"{newTarget}\""),
                new SetAttribute(unit, "cargo", oldCargo, newCargo),
                new SetAttribute(unit, "distance", oldKm.ToString(), newKm.ToString()),
            ]);
}
