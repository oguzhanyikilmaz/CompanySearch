namespace CompanySearch.Infrastructure.Options;

public sealed class WebsiteAuditOptions
{
    public const string SectionName = "WebsiteAudit";

    public int MaxInternalLinksToCheck { get; set; } = 12;

    public int MaxImagesToInspect { get; set; } = 6;

    public int RequestTimeoutSeconds { get; set; } = 20;

    public int HtmlSampleLength { get; set; } = 800;
}
