using System.Globalization;
using OwnExpeditor.Core.Session;
using OwnExpeditor.Core.Text;
using OwnExpeditor.Domain.Games;

namespace OwnExpeditor.Core.Modules.Jobs;

/// <summary>
/// Transforma um <see cref="JobRequest"/> em um <see cref="ChangeSet"/> de patches <see cref="SetAttribute"/>
/// (uma <c>Key</c> por slot: <c>jobs:slot:&lt;id&gt;</c>; re-enfileirar a mesma <c>Key</c> substitui a anterior).
/// Substituir uma oferta sobrescreve todos os campos dela, reboque incluso. Violações:
/// <c>CoreException(InvalidEdit)</c> com <c>Reason = Jobs_Error_*</c>. Não grava nada.
/// </summary>
public static class JobPlanner
{
    public const string ModuleId = "jobs";

    public static readonly IReadOnlyList<int> AllowedExpirationHours = [24, 48, 72, 168];

    private const string NullValue = "null";

    private static readonly string[] Mandatory =
    [
        "target", "cargo", "trailer_variant", "trailer_definition", "company_truck",
        "units_count", "expiration_time", "shortest_distance_km",
    ];

    /// <param name="original">
    /// O save <b>base</b> (<c>IEditSession.Original</c>, sem pendências): o <c>OldValue</c> de cada patch vem daqui,
    /// porque <c>Stage</c> o confere contra o base. Assim editar uma pendente (mesma <c>Key</c>) não dá <c>StalePatch</c>.
    /// </param>
    /// <param name="board">Construído sobre a <c>View</c> (inclui pendências): decide o próximo slot livre.</param>
    public static ChangeSet Plan(ISaveView original, JobBoard board, JobRequest request)
    {
        ArgumentNullException.ThrowIfNull(original);
        ArgumentNullException.ThrowIfNull(board);
        ArgumentNullException.ThrowIfNull(request);
        var (origin, target, cargo) = (request.Origin, request.Target, request.Cargo);

        if (origin.City == target.City) throw Invalid(JobsTextKeys.ErrorSameCity);
        if (!AllowedExpirationHours.Contains(request.ExpirationHours)) throw Invalid(JobsTextKeys.ErrorExpirationInvalid);
        if (!board.Contains(origin)) throw Invalid(JobsTextKeys.ErrorOriginMissing);
        if (!board.Contains(target)) throw Invalid(JobsTextKeys.ErrorTargetMissing);
        if (!board.Ships(origin.Company, cargo)) throw Invalid(JobsTextKeys.ErrorCargoNotShipped);
        if (!board.Accepts(target.Company, cargo)) throw Invalid(JobsTextKeys.ErrorCargoNotAccepted);
        var template = board.Template(origin.Company, cargo) ?? throw Invalid(JobsTextKeys.ErrorNoTrailer);
        var gameTime = int.TryParse(original.OfType("economy").FirstOrDefault()?.Get("game_time"),
            NumberStyles.Integer, CultureInfo.InvariantCulture, out var t)
            ? t : throw Invalid(JobsTextKeys.ErrorNoGameTime);

        var slotId = ResolveSlot(original, board, request);
        var slot = original.Find(slotId) ?? throw Invalid(request.Mode == JobSlotMode.ReplaceOffer
            ? JobsTextKeys.ErrorOfferMissing : JobsTextKeys.ErrorNoFreeSlot);
        var replacing = request.Mode == JobSlotMode.ReplaceOffer;

        var distance = board.Distance(origin.City, target.City);
        var expiration = gameTime + request.ExpirationHours * 60;
        var wanted = new (string Attribute, string Value)[]
        {
            ("target", $"\"{target.Key}\""),
            ("expiration_time", expiration.ToString(CultureInfo.InvariantCulture)),
            ("urgency", request.Urgency.ToString(CultureInfo.InvariantCulture)),
            ("shortest_distance_km", distance.Km.ToString(CultureInfo.InvariantCulture)),
            ("ferry_time", "0"),
            ("ferry_price", "0"),
            ("cargo", "cargo." + cargo),
            ("company_truck", template.CompanyTruck),
            ("trailer_variant", template.TrailerVariant),
            ("trailer_definition", template.TrailerDefinition),
            ("units_count", template.UnitsCount.ToString(CultureInfo.InvariantCulture)),
            ("fill_ratio", "1"),
            ("trailer_place", "0"),
        };

        // um slot sem algum atributo obrigatório não é um slot de frete utilizável: recusar em vez de gravar um frete incompleto
        foreach (var attribute in Mandatory)
            if (slot.Get(attribute) is null) throw Invalid(replacing ? JobsTextKeys.ErrorOfferMissing : JobsTextKeys.ErrorNoFreeSlot);

        var patches = new List<SavePatch>();
        foreach (var (attribute, value) in wanted)
        {
            var old = slot.Get(attribute);
            if (old is null || old == value) continue; // opcional ausente nesta unidade: Stage o recusaria
            patches.Add(new SetAttribute(slotId, attribute, old, value));
        }

        return new ChangeSet(ModuleId, "jobs:slot:" + slotId,
            TextRef.Of(JobsTextKeys.ChangeTitle, board.Names.Get(GameNameKind.City, origin.City),
                board.Names.Get(GameNameKind.City, target.City)),
            BuildLines(slot, replacing, origin, target, cargo, template, distance, request.ExpirationHours), patches);
    }

    private static string ResolveSlot(ISaveView original, JobBoard board, JobRequest request)
    {
        var company = original.Find(request.Origin.SaveId) ?? throw Invalid(JobsTextKeys.ErrorOriginMissing);
        var ids = company.GetArray("job_offer");

        if (request.Mode == JobSlotMode.ReplaceOffer)
        {
            var id = request.ReplaceOfferId;
            if (id is null || !ids.Contains(id) || IsEmpty(original.Find(id))) throw Invalid(JobsTextKeys.ErrorOfferMissing);
            return id;
        }

        var slot = request.FillSlotId ?? board.FreeSlotIds(request.Origin).FirstOrDefault();
        if (slot is null || !ids.Contains(slot) || !IsEmpty(original.Find(slot))) throw Invalid(JobsTextKeys.ErrorNoFreeSlot);
        return slot;
    }

    private static bool IsEmpty(SaveUnitView? job) => job is null || (job.Get("target")?.Trim('"') ?? "").Length == 0;

    private static List<DiffLine> BuildLines(SaveUnitView slot, bool replacing, CompanyLocation origin, CompanyLocation target,
        string cargo, JobTemplate template, RouteDistance distance, int expirationHours)
    {
        DiffValue Before(Func<DiffValue> value) => replacing ? value() : new DiffValue.Empty();

        var oldTarget = slot.Get("target")?.Trim('"') ?? "";
        var oldCompany = oldTarget.Contains('.') ? oldTarget[..oldTarget.LastIndexOf('.')] : oldTarget;
        var oldKm = int.TryParse(slot.Get("shortest_distance_km"), out var km) ? km : 0;
        var oldVariant = Raw(slot.Get("trailer_variant"));
        var oldDefinition = Raw(slot.Get("trailer_definition"));
        var oldCargo = Id(slot.Get("cargo"), "cargo.");

        return
        [
            Line(JobsTextKeys.FieldOrigin, Before(() => new DiffValue.GameName(GameNameKind.Company, origin.Company)),
                new DiffValue.GameName(GameNameKind.Company, origin.Company)),
            Line(JobsTextKeys.FieldDestination, Before(() => new DiffValue.GameName(GameNameKind.Company, oldCompany)),
                new DiffValue.GameName(GameNameKind.Company, target.Company)),
            Line(JobsTextKeys.FieldCargo, Before(() => oldCargo is null ? new DiffValue.Empty() : new DiffValue.GameName(GameNameKind.Cargo, oldCargo)),
                new DiffValue.GameName(GameNameKind.Cargo, cargo)),
            Line(JobsTextKeys.FieldDistance, Before(() => new DiffValue.Number(oldKm, "Format_Km")),
                new DiffValue.Number(distance.Km, distance.Source == DistanceSource.Estimated ? "Format_KmApprox" : "Format_Km")),
            Line(JobsTextKeys.FieldTrailer, Before(() => oldVariant is null && oldDefinition is null ? new DiffValue.Empty() : new DiffValue.Trailer(oldDefinition ?? "", oldVariant)),
                new DiffValue.Trailer(template.TrailerDefinition, template.TrailerVariant)),
            Line(JobsTextKeys.FieldExpiration, new DiffValue.Empty(), new DiffValue.Localized(
                expirationHours == 168
                    ? TextRef.Of("Format_Days", expirationHours / 24)
                    : TextRef.Of("Format_Hours", expirationHours))),
            Line(JobsTextKeys.FieldUnits, Before(() => new DiffValue.Raw(slot.Get("units_count") ?? "")),
                new DiffValue.Raw(template.UnitsCount.ToString(CultureInfo.InvariantCulture))),
        ];

        static DiffLine Line(string label, DiffValue before, DiffValue after) => new(TextRef.Of(label), before, after);
    }

    private static string? Raw(string? value) => value is null or "" or NullValue or "nil" ? null : value;

    private static string? Id(string? value, string prefix)
    {
        if (value is null or "" or NullValue or "nil") return null;
        return value.StartsWith(prefix, StringComparison.Ordinal) ? value[prefix.Length..] : value;
    }

    private static CoreException Invalid(string reasonKey) =>
        new(CoreError.InvalidEdit, reasonKey) { Reason = TextRef.Of(reasonKey) };
}
