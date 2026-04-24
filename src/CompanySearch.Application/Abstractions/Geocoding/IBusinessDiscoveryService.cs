using CompanySearch.Application.Common.Models;
using CompanySearch.Domain.Enums;

namespace CompanySearch.Application.Abstractions.Geocoding;

public interface IBusinessDiscoveryService
{
    Task<IReadOnlyCollection<BusinessSearchCandidate>> SearchAsync(
        GeoCoordinate center,
        double radiusKm,
        BusinessSourceType source,
        CancellationToken cancellationToken);
}
