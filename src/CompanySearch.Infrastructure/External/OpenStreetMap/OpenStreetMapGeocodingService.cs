using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CompanySearch.Application.Abstractions.Caching;
using CompanySearch.Application.Abstractions.Geocoding;
using CompanySearch.Application.Common.Exceptions;
using CompanySearch.Application.Common.Models;
using CompanySearch.Infrastructure.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CompanySearch.Infrastructure.External.OpenStreetMap;

public sealed class OpenStreetMapGeocodingService(
    IHttpClientFactory httpClientFactory,
    IAppCache cache,
    IOptions<OpenStreetMapOptions> options,
    ILogger<OpenStreetMapGeocodingService> logger)
    : IGeocodingService
{
    public const string NominatimHttpClientName = "openstreetmap-nominatim";

    public const string PhotonHttpClientName = "photon-komoot";

    private static readonly JsonSerializerOptions PhotonJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly OpenStreetMapOptions _options = options.Value;

    public async Task<GeoCoordinate> ResolveAsync(string location, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(location))
        {
            throw new BusinessRuleException("Location is required for geocoding.");
        }

        var cacheKey = $"geo:{location.Trim().ToLowerInvariant()}";
        var cached = await cache.GetAsync<GeoCoordinate>(cacheKey, cancellationToken);
        if (cached is not null)
        {
            return cached;
        }

        var nominatimClient = httpClientFactory.CreateClient(NominatimHttpClientName);
        var nominatimRequest = new HttpRequestMessage(
            HttpMethod.Get,
            $"search?format=jsonv2&limit=1&q={Uri.EscapeDataString(location.Trim())}");

        var nominatimResponse = await nominatimClient.SendAsync(nominatimRequest, cancellationToken);

        GeoCoordinate? coordinate = null;

        if (nominatimResponse.StatusCode is HttpStatusCode.Forbidden or HttpStatusCode.TooManyRequests)
        {
            logger.LogWarning(
                "Nominatim returned {Status} for geocoding; using Photon fallback.",
                (int)nominatimResponse.StatusCode);
        }
        else if (nominatimResponse.IsSuccessStatusCode)
        {
            var results = await nominatimResponse.Content.ReadFromJsonAsync<List<NominatimResult>>(
                              cancellationToken: cancellationToken)
                          ?? [];
            var first = results.FirstOrDefault();
            if (first is not null)
            {
                coordinate = new GeoCoordinate(
                    double.Parse(first.Lat, CultureInfo.InvariantCulture),
                    double.Parse(first.Lon, CultureInfo.InvariantCulture));
            }
            else
            {
                logger.LogInformation("Nominatim returned no results for {Location}; trying Photon.", location);
            }
        }
        else
        {
            nominatimResponse.EnsureSuccessStatusCode();
        }

        coordinate ??= await TryResolveWithPhotonAsync(location, cancellationToken);

        if (coordinate is null)
        {
            throw new BusinessRuleException(
                $"No geocoding result for '{location}'. Set OpenStreetMap:UserAgent in appsettings to a unique value with contact info (see https://operations.osmfoundation.org/policies/nominatim/).");
        }

        await cache.SetAsync(cacheKey, coordinate, TimeSpan.FromMinutes(_options.CacheMinutes), cancellationToken);
        logger.LogInformation(
            "Resolved location {Location} to {Latitude},{Longitude}",
            location,
            coordinate.Latitude,
            coordinate.Longitude);

        return coordinate;
    }

    private async Task<GeoCoordinate?> TryResolveWithPhotonAsync(string location, CancellationToken cancellationToken)
    {
        try
        {
            var client = httpClientFactory.CreateClient(PhotonHttpClientName);
            var request = new HttpRequestMessage(
                HttpMethod.Get,
                $"api/?q={Uri.EscapeDataString(location.Trim())}&limit=1");

            var response = await client.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("Photon geocoding returned {Status}.", (int)response.StatusCode);
                return null;
            }

            var doc = await response.Content.ReadFromJsonAsync<PhotonFeatureCollection>(
                PhotonJsonOptions,
                cancellationToken);
            var coords = doc?.Features?.FirstOrDefault()?.Geometry?.Coordinates;
            if (coords is null || coords.Length < 2)
            {
                return null;
            }

            return new GeoCoordinate(coords[1], coords[0]);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            logger.LogWarning(ex, "Photon geocoding failed for {Location}.", location);
            return null;
        }
    }

    private sealed record NominatimResult(string Lat, string Lon);

    private sealed record PhotonFeatureCollection(List<PhotonFeature>? Features);

    private sealed record PhotonFeature(PhotonGeometry? Geometry);

    private sealed record PhotonGeometry(double[]? Coordinates);
}
