using CompanySearch.Domain.Enums;

namespace CompanySearch.Domain.ValueObjects;

public sealed record WebsiteIssue(
    AnalysisCategory Category,
    IssueSeverity Severity,
    string Code,
    string Title,
    string Description,
    string Recommendation,
    int Penalty,
    string? Evidence = null,
    string? TitleTr = null,
    string? DescriptionTr = null,
    string? RecommendationTr = null);
