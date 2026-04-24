using CompanySearch.Application.Abstractions.Common;
using CompanySearch.Application.Abstractions.Jobs;
using CompanySearch.Application.Abstractions.Persistence;
using CompanySearch.Application.Abstractions.Websites;
using CompanySearch.Application.Businesses.Queries.GetBusinessById;
using CompanySearch.Application.Common.Exceptions;
using CompanySearch.Application.Common.Services;
using MediatR;

namespace CompanySearch.Application.Analysis.Commands.AnalyzeBusiness;

public sealed record AnalyzeBusinessCommand(Guid BusinessId, bool GenerateEmailAfterAnalysis = false) : IRequest<WebsiteAnalysisDto>;

public sealed class AnalyzeBusinessCommandHandler(
    IBusinessRepository businessRepository,
    IWebsiteAnalysisRepository websiteAnalysisRepository,
    IWebsiteCrawlerService websiteCrawlerService,
    IWebsiteScoringService websiteScoringService,
    ILeadScoringService leadScoringService,
    IUnitOfWork unitOfWork,
    IJobScheduler jobScheduler,
    IDateTimeProvider dateTimeProvider)
    : IRequestHandler<AnalyzeBusinessCommand, WebsiteAnalysisDto>
{
    public async Task<WebsiteAnalysisDto> Handle(AnalyzeBusinessCommand request, CancellationToken cancellationToken)
    {
        var business = await businessRepository.GetByIdAsync(request.BusinessId, includeDetails: false, cancellationToken)
            ?? throw new NotFoundException($"Business '{request.BusinessId}' was not found.");

        if (string.IsNullOrWhiteSpace(business.Website))
        {
            throw new BusinessRuleException("This business does not have a website to analyze.");
        }

        var snapshot = await websiteCrawlerService.CrawlAsync(new Uri(business.Website), cancellationToken);
        var analysis = websiteScoringService.Create(business.Id, snapshot);

        await websiteAnalysisRepository.AddAsync(analysis, cancellationToken);

        var (leadScore, priority) = leadScoringService.Calculate(business, analysis);
        business.UpdateLead(leadScore, priority, dateTimeProvider.UtcNow);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        if (request.GenerateEmailAfterAnalysis)
        {
            jobScheduler.EnqueueEmailGeneration(business.Id);
        }

        return GetBusinessByIdQueryHandler.MapAnalysis(analysis);
    }
}
