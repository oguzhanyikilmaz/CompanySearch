namespace CompanySearch.Infrastructure.Options;

public sealed class OpenStreetMapOptions
{
    public const string SectionName = "OpenStreetMap";

    public string NominatimBaseUrl { get; set; } = "https://nominatim.openstreetmap.org/";

    public string OverpassBaseUrl { get; set; } = "https://overpass-api.de/api/";

    /// <summary>Free geocoding fallback (no API key). Used when Nominatim returns 403/429 or no hits.</summary>
    public string PhotonBaseUrl { get; set; } = "https://photon.komoot.io/";

    public string UserAgent { get; set; } = "CompanySearch/1.0 (leadfinder@example.com)";

    public int RequestDelayMs { get; set; } = 200;

    public double TileSizeKm { get; set; } = 2.5;

    public int MaxParallelTiles { get; set; } = 4;

    public int CacheMinutes { get; set; } = 20;
}
