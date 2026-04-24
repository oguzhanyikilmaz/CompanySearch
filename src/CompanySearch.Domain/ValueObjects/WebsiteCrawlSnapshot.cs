namespace CompanySearch.Domain.ValueObjects;

public sealed record WebsiteCrawlSnapshot
{
    public static WebsiteCrawlSnapshot Empty => new();

    public string? FinalUrl { get; init; }

    public int StatusCode { get; init; }

    public string? Title { get; init; }

    public string? MetaDescription { get; init; }

    public List<string> H1Tags { get; init; } = [];

    public List<string> H2Tags { get; init; } = [];

    public List<string> InternalLinks { get; init; } = [];

    public List<string> BrokenLinks { get; init; } = [];

    public List<string> MissingSecurityHeaders { get; init; } = [];

    public int ResponseTimeMs { get; init; }

    public int ImageCount { get; init; }

    public int ImagesWithoutAltCount { get; init; }

    public long LargestImageBytes { get; init; }

    public int JavaScriptFileCount { get; init; }

    public int StylesheetFileCount { get; init; }

    public bool HasViewportMeta { get; init; }

    public bool UsesHttps { get; init; }

    public bool HasConsoleErrorHints { get; init; }

    public bool HasDuplicateHeadings { get; init; }

    public string? RawHtmlSample { get; init; }
}
