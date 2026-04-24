using CompanySearch.Application.Abstractions.Common;
using CompanySearch.Application.Abstractions.Geocoding;
using CompanySearch.Application.Abstractions.Jobs;
using CompanySearch.Application.Abstractions.Persistence;
using CompanySearch.Application.Common.Exceptions;
using CompanySearch.Domain.Entities;
using MediatR;

namespace CompanySearch.Application.Businesses.Commands.ImportBusinesses;

public sealed record ImportBusinessesCommand(Guid SearchJobId) : IRequest<int>;

public sealed class ImportBusinessesCommandHandler(
    ISearchJobRepository searchJobRepository,
    IBusinessRepository businessRepository,
    IBusinessDiscoveryService businessDiscoveryService,
    IJobScheduler jobScheduler,
    IUnitOfWork unitOfWork,
    IDateTimeProvider dateTimeProvider)
    : IRequestHandler<ImportBusinessesCommand, int>
{
    public async Task<int> Handle(ImportBusinessesCommand request, CancellationToken cancellationToken)
    {
        var searchJob = await searchJobRepository.GetByIdAsync(request.SearchJobId, cancellationToken)
            ?? throw new NotFoundException($"Search job '{request.SearchJobId}' was not found.");

        try
        {
            searchJob.MarkRunning(dateTimeProvider.UtcNow);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            var candidates = (await businessDiscoveryService.SearchAsync(
                    new(searchJob.Latitude, searchJob.Longitude),
                    searchJob.RadiusKm,
                    searchJob.Source,
                    cancellationToken))
                .GroupBy(candidate => candidate.ExternalId, StringComparer.OrdinalIgnoreCase)
                .Select(group => group.First())
                .ToList();

            var externalIds = candidates.Select(candidate => candidate.ExternalId).ToArray();
            var existingBusinesses = await businessRepository.GetByExternalIdsAsync(
                searchJob.Source,
                externalIds,
                cancellationToken);

            foreach (var candidate in candidates)
            {
                if (existingBusinesses.TryGetValue(candidate.ExternalId, out var existing))
                {
                    existing.UpdateDetails(
                        candidate.Address,
                        candidate.Latitude,
                        candidate.Longitude,
                        candidate.Phone,
                        candidate.Email,
                        candidate.Website,
                        dateTimeProvider.UtcNow);
                }
                else
                {
                    var business = Business.Create(
                        candidate.ExternalId,
                        candidate.Name,
                        candidate.Address,
                        candidate.Latitude,
                        candidate.Longitude,
                        candidate.Source,
                        candidate.Phone,
                        candidate.Email,
                        candidate.Website);

                    await businessRepository.AddAsync(business, cancellationToken);
                    existingBusinesses[candidate.ExternalId] = business;
                }
            }

            await unitOfWork.SaveChangesAsync(cancellationToken);

            foreach (var business in existingBusinesses.Values)
            {
                if (searchJob.AutoAnalyzeWebsites && !string.IsNullOrWhiteSpace(business.Website))
                {
                    jobScheduler.EnqueueWebsiteAnalysis(business.Id, searchJob.AutoGenerateEmails);
                }
                else if (searchJob.AutoGenerateEmails)
                {
                    jobScheduler.EnqueueEmailGeneration(business.Id);
                }
            }

            searchJob.MarkCompleted(candidates.Count, dateTimeProvider.UtcNow);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return candidates.Count;
        }
        catch (Exception exception)
        {
            searchJob.MarkFailed(exception.Message, dateTimeProvider.UtcNow);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            throw;
        }
    }
}
