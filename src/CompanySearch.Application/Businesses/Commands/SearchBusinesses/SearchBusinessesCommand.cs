using CompanySearch.Application.Abstractions.Common;
using CompanySearch.Application.Abstractions.Geocoding;
using CompanySearch.Application.Abstractions.Jobs;
using CompanySearch.Application.Abstractions.Persistence;
using CompanySearch.Application.Common.Exceptions;
using CompanySearch.Application.Common.Models;
using CompanySearch.Domain.Entities;
using CompanySearch.Domain.Enums;
using MediatR;

namespace CompanySearch.Application.Businesses.Commands.SearchBusinesses;

public sealed record SearchBusinessesCommand(
    string? Location,
    double? Latitude,
    double? Longitude,
    double RadiusKm,
    BusinessSourceType Source = BusinessSourceType.OpenStreetMap,
    bool AutoAnalyzeWebsites = true,
    bool AutoGenerateEmails = true) : IRequest<SearchJobDto>;

public sealed record SearchJobDto(
    Guid Id,
    string LocationQuery,
    double Latitude,
    double Longitude,
    double RadiusKm,
    string Source,
    string Status,
    bool AutoAnalyzeWebsites,
    bool AutoGenerateEmails,
    int BusinessesDiscovered);

public sealed class SearchBusinessesCommandHandler(
    IGeocodingService geocodingService,
    ISearchJobRepository searchJobRepository,
    IUnitOfWork unitOfWork,
    IJobScheduler jobScheduler,
    IDateTimeProvider dateTimeProvider)
    : IRequestHandler<SearchBusinessesCommand, SearchJobDto>
{
    public async Task<SearchJobDto> Handle(SearchBusinessesCommand request, CancellationToken cancellationToken)
    {
        if (request.RadiusKm <= 0)
        {
            throw new BusinessRuleException("Radius must be greater than zero.");
        }

        GeoCoordinate coordinate;
        if (request.Latitude.HasValue && request.Longitude.HasValue)
        {
            coordinate = new GeoCoordinate(request.Latitude.Value, request.Longitude.Value);
        }
        else if (!string.IsNullOrWhiteSpace(request.Location))
        {
            coordinate = await geocodingService.ResolveAsync(request.Location, cancellationToken);
        }
        else
        {
            throw new BusinessRuleException("Either a location string or latitude/longitude pair must be provided.");
        }

        var searchJob = SearchJob.Create(
            request.Location ?? $"{coordinate.Latitude:F5},{coordinate.Longitude:F5}",
            coordinate.Latitude,
            coordinate.Longitude,
            request.RadiusKm,
            request.Source,
            request.AutoAnalyzeWebsites,
            request.AutoGenerateEmails);

        searchJob.Touch(dateTimeProvider.UtcNow);

        await searchJobRepository.AddAsync(searchJob, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        jobScheduler.EnqueueBusinessImport(searchJob.Id);

        return ToDto(searchJob);
    }

    private static SearchJobDto ToDto(SearchJob searchJob)
    {
        return new SearchJobDto(
            searchJob.Id,
            searchJob.LocationQuery,
            searchJob.Latitude,
            searchJob.Longitude,
            searchJob.RadiusKm,
            searchJob.Source.ToString(),
            searchJob.Status.ToString(),
            searchJob.AutoAnalyzeWebsites,
            searchJob.AutoGenerateEmails,
            searchJob.BusinessesDiscovered);
    }
}
