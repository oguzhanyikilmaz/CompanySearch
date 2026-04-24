using CompanySearch.Domain.Enums;

namespace CompanySearch.Application.Common.Models;

public sealed record BusinessSearchFilters(
    int Page,
    int PageSize,
    string? SearchTerm,
    bool? HasWebsite,
    int? MinScore,
    int? MaxScore,
    BusinessListSortBy SortBy = BusinessListSortBy.LeadScore,
    double? ReferenceLatitude = null,
    double? ReferenceLongitude = null,
    LeadPriority? Priority = null);
