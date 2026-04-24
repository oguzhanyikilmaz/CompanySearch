namespace CompanySearch.Api.Contracts.Search;

public sealed record SearchBusinessesRequest(
    string? Location,
    double? Latitude,
    double? Longitude,
    double RadiusKm,
    string Source = "OpenStreetMap",
    bool AutoAnalyzeWebsites = true,
    bool AutoGenerateEmails = true);
