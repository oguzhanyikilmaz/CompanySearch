using System.Diagnostics;
using System.Net;
using HtmlAgilityPack;
using CompanySearch.Application.Abstractions.Websites;
using CompanySearch.Domain.ValueObjects;
using CompanySearch.Infrastructure.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CompanySearch.Infrastructure.Websites;

public sealed class WebsiteCrawlerService(
    IHttpClientFactory httpClientFactory,
    IOptions<WebsiteAuditOptions> options,
    ILogger<WebsiteCrawlerService> logger)
    : IWebsiteCrawlerService
{
    public const string HttpClientName = "website-crawler";

    private readonly WebsiteAuditOptions _options = options.Value;

    public async Task<WebsiteCrawlSnapshot> CrawlAsync(Uri websiteUri, CancellationToken cancellationToken)
    {
        var client = httpClientFactory.CreateClient(HttpClientName);
        var request = new HttpRequestMessage(HttpMethod.Get, websiteUri);
        request.Headers.UserAgent.ParseAdd("CompanySearchBot/1.0");

        try
        {
            var stopwatch = Stopwatch.StartNew();
            using var response = await client.SendAsync(request, cancellationToken);
            stopwatch.Stop();

            var html = await response.Content.ReadAsStringAsync(cancellationToken);
            var finalUri = response.RequestMessage?.RequestUri ?? websiteUri;
            var document = new HtmlDocument();
            document.LoadHtml(html);

            var title = document.DocumentNode.SelectSingleNode("//title")?.InnerText?.Trim();
            var metaDescription = document.DocumentNode.SelectSingleNode("//meta[@name='description' or @name='Description']")?.GetAttributeValue("content", null)?.Trim();
            var h1Tags = SelectTextNodes(document, "//h1");
            var h2Tags = SelectTextNodes(document, "//h2");

            var imageUrls = document.DocumentNode.SelectNodes("//img[@src]")?
                .Select(node => ResolveUri(finalUri, node.GetAttributeValue("src", string.Empty)))
                .Where(uri => uri is not null)
                .Cast<Uri>()
                .DistinctBy(uri => uri.ToString(), StringComparer.OrdinalIgnoreCase)
                .Take(_options.MaxImagesToInspect)
                .ToArray() ?? [];

            var imagesWithoutAltCount = document.DocumentNode.SelectNodes("//img[not(@alt) or @alt='']")?.Count ?? 0;
            var internalLinks = document.DocumentNode.SelectNodes("//a[@href]")?
                .Select(node => ResolveUri(finalUri, node.GetAttributeValue("href", string.Empty)))
                .Where(uri => uri is not null && string.Equals(uri.Host, finalUri.Host, StringComparison.OrdinalIgnoreCase))
                .Select(uri => RemoveFragment(uri!))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Take(_options.MaxInternalLinksToCheck)
                .ToArray() ?? [];

            var brokenLinks = await FindBrokenLinksAsync(client, internalLinks, cancellationToken);
            var largestImageBytes = await GetLargestImageAsync(client, imageUrls, cancellationToken);
            var missingSecurityHeaders = GetMissingSecurityHeaders(response);

            return new WebsiteCrawlSnapshot
            {
                FinalUrl = finalUri.ToString(),
                StatusCode = (int)response.StatusCode,
                Title = title,
                MetaDescription = metaDescription,
                H1Tags = h1Tags,
                H2Tags = h2Tags,
                InternalLinks = internalLinks.ToList(),
                BrokenLinks = brokenLinks,
                ResponseTimeMs = (int)stopwatch.ElapsedMilliseconds,
                ImageCount = document.DocumentNode.SelectNodes("//img")?.Count ?? 0,
                ImagesWithoutAltCount = imagesWithoutAltCount,
                LargestImageBytes = largestImageBytes,
                JavaScriptFileCount = document.DocumentNode.SelectNodes("//script[@src]")?.Count ?? 0,
                StylesheetFileCount = document.DocumentNode.SelectNodes("//link[@rel='stylesheet']")?.Count ?? 0,
                HasViewportMeta = document.DocumentNode.SelectSingleNode("//meta[@name='viewport']") is not null,
                UsesHttps = string.Equals(finalUri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase),
                MissingSecurityHeaders = missingSecurityHeaders,
                HasConsoleErrorHints = html.Contains("console.error", StringComparison.OrdinalIgnoreCase) ||
                                       html.Contains("window.onerror", StringComparison.OrdinalIgnoreCase) ||
                                       html.Contains("UnhandledPromiseRejection", StringComparison.OrdinalIgnoreCase),
                HasDuplicateHeadings = HasDuplicates(h1Tags) || HasDuplicates(h2Tags),
                RawHtmlSample = CreateSample(html)
            };
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Website crawl failed for {Website}", websiteUri);

            return new WebsiteCrawlSnapshot
            {
                FinalUrl = websiteUri.ToString(),
                StatusCode = (int)HttpStatusCode.BadGateway,
                UsesHttps = string.Equals(websiteUri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase),
                MissingSecurityHeaders =
                [
                    "Content-Security-Policy",
                    "Strict-Transport-Security",
                    "X-Content-Type-Options",
                    "X-Frame-Options",
                    "Referrer-Policy"
                ],
                RawHtmlSample = exception.Message
            };
        }
    }

    private async Task<List<string>> FindBrokenLinksAsync(HttpClient client, IEnumerable<string> links, CancellationToken cancellationToken)
    {
        var brokenLinks = new List<string>();

        foreach (var link in links)
        {
            try
            {
                using var headRequest = new HttpRequestMessage(HttpMethod.Head, link);
                using var headResponse = await client.SendAsync(headRequest, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
                if ((int)headResponse.StatusCode >= 400)
                {
                    brokenLinks.Add(link);
                }
            }
            catch
            {
                brokenLinks.Add(link);
            }
        }

        return brokenLinks;
    }

    private async Task<long> GetLargestImageAsync(HttpClient client, IEnumerable<Uri> images, CancellationToken cancellationToken)
    {
        long largest = 0;

        foreach (var image in images)
        {
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Head, image);
                using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

                if (response.Content.Headers.ContentLength.HasValue)
                {
                    largest = Math.Max(largest, response.Content.Headers.ContentLength.Value);
                }
            }
            catch (Exception exception)
            {
                logger.LogDebug(exception, "Image inspection failed for {ImageUrl}", image);
            }
        }

        return largest;
    }

    private static List<string> GetMissingSecurityHeaders(HttpResponseMessage response)
    {
        var requiredHeaders = new[]
        {
            "Content-Security-Policy",
            "Strict-Transport-Security",
            "X-Content-Type-Options",
            "X-Frame-Options",
            "Referrer-Policy"
        };

        return requiredHeaders
            .Where(header => !response.Headers.Contains(header) && !response.Content.Headers.Contains(header))
            .ToList();
    }

    private static List<string> SelectTextNodes(HtmlDocument document, string xpath)
    {
        return document.DocumentNode.SelectNodes(xpath)?
            .Select(node => HtmlEntity.DeEntitize(node.InnerText).Trim())
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .ToList() ?? [];
    }

    private static Uri? ResolveUri(Uri baseUri, string href)
    {
        if (string.IsNullOrWhiteSpace(href) ||
            href.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase) ||
            href.StartsWith("tel:", StringComparison.OrdinalIgnoreCase) ||
            href.StartsWith('#'))
        {
            return null;
        }

        return Uri.TryCreate(baseUri, href, out var uri) &&
               (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)
            ? uri
            : null;
    }

    private static string RemoveFragment(Uri uri)
    {
        return $"{uri.Scheme}://{uri.Host}{uri.AbsolutePath}".TrimEnd('/');
    }

    private static bool HasDuplicates(IEnumerable<string> values)
    {
        return values
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .GroupBy(value => value.Trim(), StringComparer.OrdinalIgnoreCase)
            .Any(group => group.Count() > 1);
    }

    private string CreateSample(string html)
    {
        var normalized = string.Join(' ', html
            .Split(['\r', '\n', '\t'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));

        return normalized.Length <= _options.HtmlSampleLength
            ? normalized
            : normalized[.._options.HtmlSampleLength];
    }
}
