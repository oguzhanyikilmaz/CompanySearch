using CompanySearch.Application.Abstractions.Persistence;
using CompanySearch.Application.Common.Models;
using CompanySearch.Domain.Entities;
using CompanySearch.Domain.Enums;
using MediatR;

namespace CompanySearch.Application.Businesses.Queries.GetBusinesses;

public sealed record GetBusinessesQuery(
    int Page = 1,
    int PageSize = 25,
    string? SearchTerm = null,
    bool? HasWebsite = null,
    int? MinScore = null,
    int? MaxScore = null,
    BusinessListSortBy SortBy = BusinessListSortBy.LeadScore,
    double? ReferenceLatitude = null,
    double? ReferenceLongitude = null,
    LeadPriority? Priority = null) : IRequest<PagedResult<BusinessListItemDto>>;

public sealed record BusinessListItemDto(
    Guid Id,
    string Name,
    string Address,
    double Latitude,
    double Longitude,
    string? Phone,
    string? Email,
    string? Website,
    string Source,
    int LeadScore,
    string Priority,
    int? LatestScore,
    string? LatestEmailStatus,
    DateTime CreatedAtUtc);

public sealed class GetBusinessesQueryHandler(IBusinessRepository businessRepository)
    : IRequestHandler<GetBusinessesQuery, PagedResult<BusinessListItemDto>>
{
    public async Task<PagedResult<BusinessListItemDto>> Handle(GetBusinessesQuery request, CancellationToken cancellationToken)
    {
        var filters = new BusinessSearchFilters(
            Math.Max(1, request.Page),
            Math.Clamp(request.PageSize, 1, 200),
            request.SearchTerm,
            request.HasWebsite,
            request.MinScore,
            request.MaxScore,
            request.SortBy,
            request.ReferenceLatitude,
            request.ReferenceLongitude,
            request.Priority);

        var results = await businessRepository.ListAsync(filters, cancellationToken);
        return new PagedResult<BusinessListItemDto>(
            results.Items.Select(Map).ToArray(),
            results.Page,
            results.PageSize,
            results.TotalCount);
    }

    private static BusinessListItemDto Map(Business business)
    {
        var latestAnalysis = business.Analyses.OrderByDescending(analysis => analysis.CreatedAtUtc).FirstOrDefault();
        var latestEmail = business.Emails.OrderByDescending(email => email.CreatedAtUtc).FirstOrDefault();

        return new BusinessListItemDto(
            business.Id,
            business.Name,
            business.Address,
            business.Latitude,
            business.Longitude,
            business.Phone,
            business.Email,
            business.Website,
            business.Source.ToString(),
            business.LeadScore,
            business.Priority.ToString(),
            latestAnalysis?.Score,
            latestEmail?.SentStatus.ToString(),
            business.CreatedAtUtc);
    }
}
