using System.Collections.Concurrent;
using System.Globalization;
using System.Net.Http.Json;
using CompanySearch.Application.Abstractions.Caching;
using CompanySearch.Application.Abstractions.Geocoding;
using CompanySearch.Application.Common.Exceptions;
using CompanySearch.Application.Common.Models;
using CompanySearch.Domain.Enums;
using CompanySearch.Infrastructure.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CompanySearch.Infrastructure.External.OpenStreetMap;

public sealed class OpenStreetMapBusinessDiscoveryService(
    IHttpClientFactory httpClientFactory,
    IAppCache cache,
    IOptions<OpenStreetMapOptions> options,
    ILogger<OpenStreetMapBusinessDiscoveryService> logger)
    : IBusinessDiscoveryService
{
    public const string HttpClientName = "openstreetmap-overpass";

    private readonly OpenStreetMapOptions _options = options.Value;

    public async Task<IReadOnlyCollection<BusinessSearchCandidate>> SearchAsync(
        GeoCoordinate center,
        double radiusKm,
        BusinessSourceType source,
        CancellationToken cancellationToken)
    {
        if (source != BusinessSourceType.OpenStreetMap)
        {
            throw new BusinessRuleException($"Configured infrastructure currently supports '{BusinessSourceType.OpenStreetMap}' search only.");
        }

        var cacheKey = $"osm:search:v3:{center.Latitude:F5}:{center.Longitude:F5}:{radiusKm:F2}";
        var cached = await cache.GetAsync<List<BusinessSearchCandidate>>(cacheKey, cancellationToken);
        if (cached is not null)
        {
            return cached;
        }

        var tiles = BuildTiles(center, radiusKm).ToArray();
        var results = new ConcurrentDictionary<string, BusinessSearchCandidate>(StringComparer.OrdinalIgnoreCase);

        await Parallel.ForEachAsync(
            tiles,
            new ParallelOptions
            {
                CancellationToken = cancellationToken,
                MaxDegreeOfParallelism = Math.Max(1, _options.MaxParallelTiles)
            },
            async (tile, token) =>
            {
                await Task.Delay(_options.RequestDelayMs, token);
                var tileResults = await QueryTileAsync(tile, token);

                foreach (var candidate in tileResults)
                {
                    if (GetDistanceKm(center, new GeoCoordinate(candidate.Latitude, candidate.Longitude)) <= radiusKm)
                    {
                        results.TryAdd(candidate.ExternalId, candidate);
                    }
                }
            });

        var orderedResults = results.Values
            .OrderBy(candidate => candidate.Name)
            .ToList();

        await cache.SetAsync(cacheKey, orderedResults, TimeSpan.FromMinutes(_options.CacheMinutes), cancellationToken);
        logger.LogInformation("Discovered {Count} businesses via OSM for {Latitude},{Longitude} within {RadiusKm}km", orderedResults.Count, center.Latitude, center.Longitude, radiusKm);

        return orderedResults;
    }

    private async Task<IReadOnlyCollection<BusinessSearchCandidate>> QueryTileAsync(BoundingBox tile, CancellationToken cancellationToken)
    {
        var client = httpClientFactory.CreateClient(HttpClientName);
        var query = string.Create(CultureInfo.InvariantCulture, $$"""
[out:json][timeout:45];
(
  node["name"]["amenity"]({{tile.South}},{{tile.West}},{{tile.North}},{{tile.East}});
  way["name"]["amenity"]({{tile.South}},{{tile.West}},{{tile.North}},{{tile.East}});
  relation["name"]["amenity"]({{tile.South}},{{tile.West}},{{tile.North}},{{tile.East}});
  node["name"]["shop"]({{tile.South}},{{tile.West}},{{tile.North}},{{tile.East}});
  way["name"]["shop"]({{tile.South}},{{tile.West}},{{tile.North}},{{tile.East}});
  relation["name"]["shop"]({{tile.South}},{{tile.West}},{{tile.North}},{{tile.East}});
  node["name"]["office"]({{tile.South}},{{tile.West}},{{tile.North}},{{tile.East}});
  way["name"]["office"]({{tile.South}},{{tile.West}},{{tile.North}},{{tile.East}});
  relation["name"]["office"]({{tile.South}},{{tile.West}},{{tile.North}},{{tile.East}});
  node["name"]["craft"]({{tile.South}},{{tile.West}},{{tile.North}},{{tile.East}});
  way["name"]["craft"]({{tile.South}},{{tile.West}},{{tile.North}},{{tile.East}});
  relation["name"]["craft"]({{tile.South}},{{tile.West}},{{tile.North}},{{tile.East}});
);
out center tags qt;
""");

        using var request = new HttpRequestMessage(HttpMethod.Post, "interpreter");
        request.Headers.UserAgent.ParseAdd(_options.UserAgent);
        request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["data"] = query
        });

        using var response = await client.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<OverpassResponse>(cancellationToken: cancellationToken);
        if (payload?.Elements is null || payload.Elements.Count == 0)
        {
            return [];
        }

        return payload.Elements
            .Select(MapCandidate)
            .Where(candidate => candidate is not null)
            .Cast<BusinessSearchCandidate>()
            .ToArray();
    }

    private static BusinessSearchCandidate? MapCandidate(OverpassElement element)
    {
        if (element.Tags is null ||
            !element.Tags.TryGetValue("name", out var name) ||
            string.IsNullOrWhiteSpace(name))
        {
            return null;
        }

        if (IsPermanentlyClosedOsmFeature(element.Tags))
        {
            return null;
        }

        var latitude = element.Lat ?? element.Center?.Lat;
        var longitude = element.Lon ?? element.Center?.Lon;
        if (!latitude.HasValue || !longitude.HasValue)
        {
            return null;
        }

        var address = element.Tags.TryGetValue("addr:full", out var fullAddress)
            ? fullAddress
            : string.Join(", ", new[]
            {
                BuildStreetAddress(element.Tags),
                GetTag(element.Tags, "addr:city") ?? GetTag(element.Tags, "addr:district") ?? GetTag(element.Tags, "addr:state")
            }.Where(value => !string.IsNullOrWhiteSpace(value)));

        return new BusinessSearchCandidate(
            $"{element.Type}/{element.Id}",
            name.Trim(),
            string.IsNullOrWhiteSpace(address) ? "Address not available" : address.Trim(),
            latitude.Value,
            longitude.Value,
            GetTag(element.Tags, "contact:phone") ?? GetTag(element.Tags, "phone"),
            GetTag(element.Tags, "contact:email") ?? GetTag(element.Tags, "email"),
            GetTag(element.Tags, "contact:website") ?? GetTag(element.Tags, "website") ?? GetTag(element.Tags, "url"),
            BusinessSourceType.OpenStreetMap);
    }

    private IEnumerable<BoundingBox> BuildTiles(GeoCoordinate center, double radiusKm)
    {
        var latKmPerDegree = 111.32;
        var lngKmPerDegree = Math.Max(1d, 111.32 * Math.Cos(center.Latitude * Math.PI / 180d));

        var latRadius = radiusKm / latKmPerDegree;
        var lngRadius = radiusKm / lngKmPerDegree;
        var latStep = Math.Max(_options.TileSizeKm / latKmPerDegree, latRadius * 2);
        var lngStep = Math.Max(_options.TileSizeKm / lngKmPerDegree, lngRadius * 2);

        for (var south = center.Latitude - latRadius; south < center.Latitude + latRadius; south += latStep)
        {
            var north = Math.Min(south + latStep, center.Latitude + latRadius);

            for (var west = center.Longitude - lngRadius; west < center.Longitude + lngRadius; west += lngStep)
            {
                var east = Math.Min(west + lngStep, center.Longitude + lngRadius);
                yield return new BoundingBox(south, west, north, east);
            }
        }
    }

    private static double GetDistanceKm(GeoCoordinate first, GeoCoordinate second)
    {
        const double earthRadiusKm = 6371d;
        var latDelta = ToRadians(second.Latitude - first.Latitude);
        var lonDelta = ToRadians(second.Longitude - first.Longitude);

        var a = Math.Sin(latDelta / 2) * Math.Sin(latDelta / 2) +
                Math.Cos(ToRadians(first.Latitude)) * Math.Cos(ToRadians(second.Latitude)) *
                Math.Sin(lonDelta / 2) * Math.Sin(lonDelta / 2);

        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return earthRadiusKm * c;
    }

    private static double ToRadians(double degrees) => degrees * Math.PI / 180d;

    private static string? BuildStreetAddress(IReadOnlyDictionary<string, string> tags)
    {
        var street = GetTag(tags, "addr:street");
        var number = GetTag(tags, "addr:housenumber");
        return string.Join(' ', new[] { street, number }.Where(value => !string.IsNullOrWhiteSpace(value)));
    }

    private static string? GetTag(IReadOnlyDictionary<string, string> tags, string key)
    {
        return tags.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value)
            ? value.Trim()
            : null;
    }

    /// <summary>
    /// OSM'de kullanılmayan / terk edilmiş işletmeleri (disused:, abandoned: vb.) ele.
    /// </summary>
    private static bool IsPermanentlyClosedOsmFeature(IReadOnlyDictionary<string, string> tags)
    {
        foreach (var (key, value) in tags)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                continue;
            }

            var k = key.ToLowerInvariant();

            if (k.StartsWith("disused:", StringComparison.Ordinal) ||
                k.StartsWith("abandoned:", StringComparison.Ordinal) ||
                k.StartsWith("demolished:", StringComparison.Ordinal))
            {
                return true;
            }

            if (k == "lifecycle:status")
            {
                var v = value.Trim();
                if (v.Equals("disused", StringComparison.OrdinalIgnoreCase) ||
                    v.Equals("abandoned", StringComparison.OrdinalIgnoreCase) ||
                    v.Equals("demolished", StringComparison.OrdinalIgnoreCase) ||
                    v.Equals("removed", StringComparison.OrdinalIgnoreCase) ||
                    v.Equals("vacant", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            if (k == "operational_status")
            {
                var v = value.ToLowerInvariant();
                if (v.Contains("closed", StringComparison.Ordinal) ||
                    v.Contains("defunct", StringComparison.Ordinal) ||
                    v.Contains("abandoned", StringComparison.Ordinal))
                {
                    return true;
                }
            }

            if (k == "business:status" && value.Equals("closed", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private sealed record BoundingBox(double South, double West, double North, double East);

    private sealed record OverpassResponse(List<OverpassElement> Elements);

    private sealed record OverpassElement(
        long Id,
        string Type,
        double? Lat,
        double? Lon,
        OverpassCenter? Center,
        Dictionary<string, string>? Tags);

    private sealed record OverpassCenter(double Lat, double Lon);
}
