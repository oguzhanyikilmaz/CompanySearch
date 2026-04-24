using CompanySearch.Domain.Common;
using CompanySearch.Domain.Enums;

namespace CompanySearch.Domain.Entities;

public sealed class SearchJob : AuditableEntity
{
    private SearchJob()
    {
    }

    private SearchJob(
        string locationQuery,
        double latitude,
        double longitude,
        double radiusKm,
        BusinessSourceType source,
        bool autoAnalyzeWebsites,
        bool autoGenerateEmails)
    {
        LocationQuery = locationQuery;
        Latitude = latitude;
        Longitude = longitude;
        RadiusKm = radiusKm;
        Source = source;
        AutoAnalyzeWebsites = autoAnalyzeWebsites;
        AutoGenerateEmails = autoGenerateEmails;
        Status = SearchRequestStatus.Queued;
    }

    public string LocationQuery { get; private set; } = string.Empty;

    public double Latitude { get; private set; }

    public double Longitude { get; private set; }

    public double RadiusKm { get; private set; }

    public BusinessSourceType Source { get; private set; }

    public SearchRequestStatus Status { get; private set; }

    public int BusinessesDiscovered { get; private set; }

    public bool AutoAnalyzeWebsites { get; private set; }

    public bool AutoGenerateEmails { get; private set; }

    public string? ErrorMessage { get; private set; }

    public static SearchJob Create(
        string locationQuery,
        double latitude,
        double longitude,
        double radiusKm,
        BusinessSourceType source,
        bool autoAnalyzeWebsites,
        bool autoGenerateEmails)
    {
        if (radiusKm <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(radiusKm), "Radius must be greater than zero.");
        }

        return new SearchJob(
            string.IsNullOrWhiteSpace(locationQuery) ? $"{latitude:F5},{longitude:F5}" : locationQuery.Trim(),
            latitude,
            longitude,
            radiusKm,
            source,
            autoAnalyzeWebsites,
            autoGenerateEmails);
    }

    public void MarkRunning(DateTime timestampUtc)
    {
        Status = SearchRequestStatus.Running;
        ErrorMessage = null;
        Touch(timestampUtc);
    }

    public void MarkCompleted(int businessesDiscovered, DateTime timestampUtc)
    {
        BusinessesDiscovered = Math.Max(0, businessesDiscovered);
        Status = SearchRequestStatus.Completed;
        ErrorMessage = null;
        Touch(timestampUtc);
    }

    public void MarkFailed(string errorMessage, DateTime timestampUtc)
    {
        Status = SearchRequestStatus.Failed;
        ErrorMessage = string.IsNullOrWhiteSpace(errorMessage) ? "Unknown search error." : errorMessage.Trim();
        Touch(timestampUtc);
    }
}
