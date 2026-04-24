using CompanySearch.Domain.Common;
using CompanySearch.Domain.ValueObjects;

namespace CompanySearch.Domain.Entities;

public sealed class WebsiteAnalysis : AuditableEntity
{
    private WebsiteAnalysis()
    {
    }

    private WebsiteAnalysis(
        Guid businessId,
        int score,
        string summary,
        WebsiteCrawlSnapshot snapshot,
        IReadOnlyCollection<WebsiteIssue> issues)
    {
        BusinessId = businessId;
        Score = Math.Clamp(score, 0, 100);
        Summary = summary;
        Snapshot = snapshot;
        Issues = issues.ToList();
    }

    public Guid BusinessId { get; private set; }

    public int Score { get; private set; }

    public string Summary { get; private set; } = string.Empty;

    public WebsiteCrawlSnapshot Snapshot { get; private set; } = WebsiteCrawlSnapshot.Empty;

    public List<WebsiteIssue> Issues { get; private set; } = [];

    public Business? Business { get; private set; }

    public static WebsiteAnalysis Create(
        Guid businessId,
        int score,
        string summary,
        WebsiteCrawlSnapshot snapshot,
        IReadOnlyCollection<WebsiteIssue> issues)
    {
        return new WebsiteAnalysis(
            businessId,
            score,
            string.IsNullOrWhiteSpace(summary) ? "Website analysis completed." : summary.Trim(),
            snapshot,
            issues);
    }
}
