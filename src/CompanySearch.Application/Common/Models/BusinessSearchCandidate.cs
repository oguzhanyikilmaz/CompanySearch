using CompanySearch.Domain.Enums;

namespace CompanySearch.Application.Common.Models;

public sealed record BusinessSearchCandidate(
    string ExternalId,
    string Name,
    string Address,
    double Latitude,
    double Longitude,
    string? Phone,
    string? Email,
    string? Website,
    BusinessSourceType Source);
