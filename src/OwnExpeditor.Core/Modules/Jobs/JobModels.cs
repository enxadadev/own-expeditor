namespace OwnExpeditor.Core.Modules.Jobs;

/// <summary>Uma empresa numa cidade, como aparece no save ("kaarfor", "budapest"). Sem texto de tela: o nome vem de <c>IGameNames</c>.</summary>
public sealed record CompanyLocation(string Company, string City)
{
    public string Key => $"{Company}.{City}";

    public string SaveId => "company.volatile." + Key;
}

/// <summary>Caminhão e reboque copiados de um frete existente da mesma carga.</summary>
public sealed record JobTemplate(string TrailerVariant, string TrailerDefinition, string CompanyTruck, int UnitsCount);

/// <summary>Onde entra o frete: numa vaga livre da empresa ou no lugar de uma oferta existente (escolha do usuário).</summary>
public enum JobSlotMode { FillEmpty, ReplaceOffer }

/// <param name="ReplaceOfferId">Oferta a substituir (obrigatória em <c>ReplaceOffer</c>; vem de <c>JobBoard.ReplaceableOffers</c>).</param>
/// <param name="FillSlotId">
/// Em <c>FillEmpty</c>: o slot já usado, ao editar uma mudança pendente (a <c>Key</c> é por slot e re-enfileirar a substitui).
/// Nulo = próximo slot livre da empresa.
/// </param>
public sealed record JobRequest(CompanyLocation Origin, CompanyLocation Target, string Cargo,
    JobSlotMode Mode, string? ReplaceOfferId = null, int Urgency = 0, int ExpirationHours = 72, string? FillSlotId = null);

public enum DistanceSource { Known, Estimated }

public sealed record RouteDistance(int Km, DistanceSource Source);

/// <summary>Oferta atual de uma empresa (substituível). <c>ExpiresIn</c> em tempo de jogo; nulo se o save não traz o prazo.</summary>
public sealed record ExistingOffer(string OfferId, CompanyLocation Target, string Cargo, int DistanceKm, TimeSpan? ExpiresIn);

/// <summary>Chaves de texto do módulo (ver design/fretes/strings.md).</summary>
public static class JobsTextKeys
{
    public const string ChangeTitle = "Jobs_Change_Title";
    public const string FieldOrigin = "Jobs_Field_Origin";
    public const string FieldDestination = "Jobs_Field_Destination";
    public const string FieldCargo = "Jobs_Field_Cargo";
    public const string FieldDistance = "Jobs_Field_Distance";
    public const string FieldTrailer = "Jobs_Field_Trailer";
    public const string FieldExpiration = "Jobs_Field_Expiration";
    public const string FieldUnits = "Jobs_Field_Units";

    public const string ErrorNoFreeSlot = "Jobs_Error_NoFreeSlot";
    public const string ErrorOfferMissing = "Jobs_Error_OfferMissing";
    public const string ErrorCargoNotShipped = "Jobs_Error_CargoNotShipped";
    public const string ErrorCargoNotAccepted = "Jobs_Error_CargoNotAccepted";
    public const string ErrorNoTrailer = "Jobs_Error_NoTrailer";
    public const string ErrorOriginMissing = "Jobs_Error_OriginMissing";
    public const string ErrorTargetMissing = "Jobs_Error_TargetMissing";
    public const string ErrorSameCity = "Jobs_Error_SameCity";
    public const string ErrorExpirationInvalid = "Jobs_Error_ExpirationInvalid";
    public const string ErrorNoGameTime = "Jobs_Error_NoGameTime";
}
