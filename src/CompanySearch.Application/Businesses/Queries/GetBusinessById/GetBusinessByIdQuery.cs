using CompanySearch.Application.Abstractions.Persistence;
using CompanySearch.Application.Common.Exceptions;
using CompanySearch.Domain.Entities;
using CompanySearch.Domain.ValueObjects;
using MediatR;

namespace CompanySearch.Application.Businesses.Queries.GetBusinessById;

public sealed record GetBusinessByIdQuery(Guid BusinessId) : IRequest<BusinessDetailDto>;

public sealed record BusinessDetailDto(
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
    IReadOnlyCollection<string> Tags,
    WebsiteAnalysisDto? LatestAnalysis,
    SalesEmailDto? LatestEmail,
    DateTime CreatedAtUtc);

public sealed record WebsiteAnalysisDto(
    Guid Id,
    int Score,
    string Summary,
    DateTime CreatedAtUtc,
    WebsiteSnapshotDto Snapshot,
    IReadOnlyCollection<WebsiteIssueDto> Issues);

public sealed record WebsiteSnapshotDto(
    string? FinalUrl,
    int StatusCode,
    string? Title,
    string? MetaDescription,
    IReadOnlyCollection<string> H1Tags,
    IReadOnlyCollection<string> H2Tags,
    IReadOnlyCollection<string> InternalLinks,
    IReadOnlyCollection<string> BrokenLinks,
    int ResponseTimeMs,
    int ImageCount,
    int ImagesWithoutAltCount,
    int JavaScriptFileCount,
    int StylesheetFileCount,
    bool HasViewportMeta,
    bool UsesHttps,
    IReadOnlyCollection<string> MissingSecurityHeaders);

public sealed record WebsiteIssueDto(
    string Category,
    string Severity,
    string Code,
    string Title,
    string Description,
    string Recommendation,
    int Penalty,
    string? Evidence,
    string? TitleTr,
    string? DescriptionTr,
    string? RecommendationTr);

public sealed record SalesEmailDto(
    Guid Id,
    string Subject,
    string Body,
    string? RecipientEmail,
    string SentStatus,
    string? GeneratedByModel,
    int RetryCount,
    string? LastError,
    DateTime CreatedAtUtc,
    DateTime? SentAtUtc);

public sealed class GetBusinessByIdQueryHandler(IBusinessRepository businessRepository)
    : IRequestHandler<GetBusinessByIdQuery, BusinessDetailDto>
{
    public async Task<BusinessDetailDto> Handle(GetBusinessByIdQuery request, CancellationToken cancellationToken)
    {
        var business = await businessRepository.GetByIdAsync(request.BusinessId, includeDetails: true, cancellationToken)
            ?? throw new NotFoundException($"Business '{request.BusinessId}' was not found.");

        var latestAnalysis = business.Analyses.OrderByDescending(analysis => analysis.CreatedAtUtc).FirstOrDefault();
        var latestEmail = business.Emails.OrderByDescending(email => email.CreatedAtUtc).FirstOrDefault();

        return new BusinessDetailDto(
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
            business.Tags.AsReadOnly(),
            latestAnalysis is null ? null : MapAnalysis(latestAnalysis),
            latestEmail is null ? null : MapEmail(latestEmail),
            business.CreatedAtUtc);
    }

    public static WebsiteAnalysisDto MapAnalysis(WebsiteAnalysis analysis)
    {
        return new WebsiteAnalysisDto(
            analysis.Id,
            analysis.Score,
            analysis.Summary,
            analysis.CreatedAtUtc,
            MapSnapshot(analysis.Snapshot),
            analysis.Issues.Select(MapIssue).ToArray());
    }

    public static SalesEmailDto MapEmail(SalesEmail email)
    {
        return new SalesEmailDto(
            email.Id,
            email.Subject,
            email.Body,
            email.RecipientEmail,
            email.SentStatus.ToString(),
            email.GeneratedByModel,
            email.RetryCount,
            email.LastError,
            email.CreatedAtUtc,
            email.SentAtUtc);
    }

    private static WebsiteSnapshotDto MapSnapshot(WebsiteCrawlSnapshot snapshot)
    {
        return new WebsiteSnapshotDto(
            snapshot.FinalUrl,
            snapshot.StatusCode,
            snapshot.Title,
            snapshot.MetaDescription,
            snapshot.H1Tags,
            snapshot.H2Tags,
            snapshot.InternalLinks,
            snapshot.BrokenLinks,
            snapshot.ResponseTimeMs,
            snapshot.ImageCount,
            snapshot.ImagesWithoutAltCount,
            snapshot.JavaScriptFileCount,
            snapshot.StylesheetFileCount,
            snapshot.HasViewportMeta,
            snapshot.UsesHttps,
            snapshot.MissingSecurityHeaders);
    }

    private static WebsiteIssueDto MapIssue(WebsiteIssue issue)
    {
        return new WebsiteIssueDto(
            issue.Category.ToString(),
            issue.Severity.ToString(),
            issue.Code,
            issue.Title,
            issue.Description,
            issue.Recommendation,
            issue.Penalty,
            issue.Evidence,
            issue.TitleTr,
            issue.DescriptionTr,
            issue.RecommendationTr);
    }
}
