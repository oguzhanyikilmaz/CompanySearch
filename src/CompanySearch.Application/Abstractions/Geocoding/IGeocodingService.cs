using CompanySearch.Application.Common.Models;

namespace CompanySearch.Application.Abstractions.Geocoding;

public interface IGeocodingService
{
    Task<GeoCoordinate> ResolveAsync(string location, CancellationToken cancellationToken);
}
