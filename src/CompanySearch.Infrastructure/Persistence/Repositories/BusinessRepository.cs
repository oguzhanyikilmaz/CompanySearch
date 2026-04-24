using CompanySearch.Application.Abstractions.Persistence;
using CompanySearch.Application.Common.Models;
using CompanySearch.Domain.Entities;
using CompanySearch.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CompanySearch.Infrastructure.Persistence.Repositories;

public sealed class BusinessRepository(ApplicationDbContext dbContext) : IBusinessRepository
{
    public Task AddAsync(Business business, CancellationToken cancellationToken)
    {
        return dbContext.Businesses.AddAsync(business, cancellationToken).AsTask();
    }

    public Task<Business?> GetByIdAsync(Guid id, bool includeDetails, CancellationToken cancellationToken)
    {
        return BuildQuery(includeDetails)
            .FirstOrDefaultAsync(business => business.Id == id, cancellationToken);
    }

    public async Task<Dictionary<string, Business>> GetByExternalIdsAsync(
        BusinessSourceType source,
        IReadOnlyCollection<string> externalIds,
        CancellationToken cancellationToken)
    {
        return await dbContext.Businesses
            .Where(business => business.Source == source && externalIds.Contains(business.ExternalId))
            .ToDictionaryAsync(business => business.ExternalId, StringComparer.OrdinalIgnoreCase, cancellationToken);
    }

    public async Task<IReadOnlyCollection<Business>> GetByIdsAsync(
        IReadOnlyCollection<Guid> businessIds,
        bool includeDetails,
        CancellationToken cancellationToken)
    {
        return await BuildQuery(includeDetails)
            .Where(business => businessIds.Contains(business.Id))
            .ToListAsync(cancellationToken);
    }

    public async Task<PagedResult<Business>> ListAsync(BusinessSearchFilters filters, CancellationToken cancellationToken)
    {
        var query = BuildQuery(includeDetails: true);

        if (!string.IsNullOrWhiteSpace(filters.SearchTerm))
        {
            var term = filters.SearchTerm.Trim().ToLowerInvariant();
            query = query.Where(business =>
                business.Name.ToLower().Contains(term) ||
                business.Address.ToLower().Contains(term) ||
                (business.Website != null && business.Website.ToLower().Contains(term)));
        }

        if (filters.HasWebsite.HasValue)
        {
            query = filters.HasWebsite.Value
                ? query.Where(business => business.Website != null)
                : query.Where(business => business.Website == null);
        }

        if (filters.MinScore.HasValue)
        {
            query = query.Where(business =>
                business.Analyses
                    .OrderByDescending(analysis => analysis.CreatedAtUtc)
                    .Select(analysis => (int?)analysis.Score)
                    .FirstOrDefault() >= filters.MinScore.Value);
        }

        if (filters.MaxScore.HasValue)
        {
            query = query.Where(business =>
                business.Analyses
                    .OrderByDescending(analysis => analysis.CreatedAtUtc)
                    .Select(analysis => (int?)analysis.Score)
                    .FirstOrDefault() <= filters.MaxScore.Value);
        }

        if (filters.Priority.HasValue)
        {
            query = query.Where(business => business.Priority == filters.Priority.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        query = ApplySort(query, filters);

        var items = await query
            .Skip((filters.Page - 1) * filters.PageSize)
            .Take(filters.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<Business>(items, filters.Page, filters.PageSize, totalCount);
    }

    private static IQueryable<Business> ApplySort(IQueryable<Business> query, BusinessSearchFilters filters)
    {
        if (filters.SortBy == BusinessListSortBy.DistanceFromReference &&
            filters.ReferenceLatitude is { } refLat &&
            filters.ReferenceLongitude is { } refLng)
        {
            return query
                .OrderBy(business =>
                    (business.Latitude - refLat) * (business.Latitude - refLat) +
                    (business.Longitude - refLng) * (business.Longitude - refLng))
                .ThenByDescending(business => business.LeadScore);
        }

        if (filters.SortBy == BusinessListSortBy.Newest)
        {
            return query.OrderByDescending(business => business.CreatedAtUtc);
        }

        return query
            .OrderByDescending(business => business.LeadScore)
            .ThenByDescending(business => business.CreatedAtUtc);
    }

    private IQueryable<Business> BuildQuery(bool includeDetails)
    {
        var query = dbContext.Businesses.AsQueryable();

        if (!includeDetails)
        {
            return query;
        }

        return query
            .Include(business => business.Analyses)
            .Include(business => business.Emails);
    }
}
