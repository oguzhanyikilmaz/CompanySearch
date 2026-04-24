using CompanySearch.Domain.Entities;
using CompanySearch.Domain.ValueObjects;

namespace CompanySearch.Application.Common.Services;

public interface IWebsiteScoringService
{
    WebsiteAnalysis Create(Guid businessId, WebsiteCrawlSnapshot snapshot);
}
