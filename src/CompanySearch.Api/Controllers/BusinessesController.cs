using System.Text;
using CompanySearch.Application.Businesses.Queries.GetBusinessById;
using CompanySearch.Application.Businesses.Queries.GetBusinesses;
using CompanySearch.Application.Common.Models;
using CompanySearch.Application.Exports.Queries.ExportBusinesses;
using CompanySearch.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace CompanySearch.Api.Controllers;

[ApiController]
[Route("api/businesses")]
public sealed class BusinessesController(ISender sender) : ControllerBase
{
    [HttpGet]
    public Task<PagedResult<BusinessListItemDto>> GetBusinesses(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        [FromQuery] string? searchTerm = null,
        [FromQuery] bool? hasWebsite = null,
        [FromQuery] int? minScore = null,
        [FromQuery] int? maxScore = null,
        [FromQuery] string? sortBy = null,
        [FromQuery] double? refLatitude = null,
        [FromQuery] double? refLongitude = null,
        [FromQuery] string? priority = null,
        CancellationToken cancellationToken = default)
    {
        var sort = sortBy?.Trim().ToLowerInvariant() switch
        {
            "distance" => BusinessListSortBy.DistanceFromReference,
            "newest" => BusinessListSortBy.Newest,
            _ => BusinessListSortBy.LeadScore
        };

        var priorityFilter = ParsePriority(priority);

        return sender.Send(
            new GetBusinessesQuery(page, pageSize, searchTerm, hasWebsite, minScore, maxScore, sort, refLatitude, refLongitude, priorityFilter),
            cancellationToken);
    }

    private static LeadPriority? ParsePriority(string? raw) =>
        raw?.Trim().ToLowerInvariant() switch
        {
            "low" => LeadPriority.Low,
            "medium" => LeadPriority.Medium,
            "high" => LeadPriority.High,
            "strategic" => LeadPriority.Strategic,
            _ => null
        };

    [HttpGet("{businessId:guid}")]
    public Task<BusinessDetailDto> GetBusiness(Guid businessId, CancellationToken cancellationToken)
    {
        return sender.Send(new GetBusinessByIdQuery(businessId), cancellationToken);
    }

    [HttpGet("export")]
    public async Task<FileContentResult> ExportBusinesses(
        [FromQuery] string? searchTerm,
        [FromQuery] bool? hasWebsite,
        [FromQuery] int? minScore,
        [FromQuery] int? maxScore,
        [FromQuery] string? priority,
        CancellationToken cancellationToken)
    {
        var csv = await sender.Send(
            new ExportBusinessesQuery(searchTerm, hasWebsite, minScore, maxScore, ParsePriority(priority)),
            cancellationToken);
        return File(Encoding.UTF8.GetBytes(csv), "text/csv", "businesses.csv");
    }
}
