using CompanySearch.Domain.ValueObjects;

namespace CompanySearch.Application.Abstractions.Websites;

public interface IWebsiteCrawlerService
{
    Task<WebsiteCrawlSnapshot> CrawlAsync(Uri websiteUri, CancellationToken cancellationToken);
}
