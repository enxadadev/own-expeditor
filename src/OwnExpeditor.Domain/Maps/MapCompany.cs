namespace OwnExpeditor.Domain.Maps;

public sealed record MapCompany(string CompanyToken, string CityToken, MapPos Position, int? NodeId);
