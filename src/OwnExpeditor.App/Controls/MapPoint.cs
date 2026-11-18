namespace OwnExpeditor.App.Controls;

/// <summary>Ponto do <see cref="RouteMap"/>: nome e, se houver, coordenadas (geográfico só com as duas pontas).</summary>
public sealed record MapPoint(string Label, double? Longitude = null, double? Latitude = null)
{
    public bool HasCoordinates => Longitude is not null && Latitude is not null;
}
