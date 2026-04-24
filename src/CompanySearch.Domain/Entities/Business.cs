using CompanySearch.Domain.Common;
using CompanySearch.Domain.Enums;

namespace CompanySearch.Domain.Entities;

public sealed class Business : AuditableEntity
{
    private Business()
    {
    }

    private Business(
        string externalId,
        string name,
        string address,
        double latitude,
        double longitude,
        BusinessSourceType source,
        string? phone,
        string? email,
        string? website)
    {
        ExternalId = externalId;
        Name = name;
        Address = address;
        Latitude = latitude;
        Longitude = longitude;
        Source = source;
        Phone = Normalize(phone);
        Email = Normalize(email);
        Website = NormalizeWebsite(website);
    }

    public string ExternalId { get; private set; } = string.Empty;

    public string Name { get; private set; } = string.Empty;

    public string Address { get; private set; } = string.Empty;

    public double Latitude { get; private set; }

    public double Longitude { get; private set; }

    public string? Phone { get; private set; }

    public string? Email { get; private set; }

    public string? Website { get; private set; }

    public BusinessSourceType Source { get; private set; }

    public int LeadScore { get; private set; }

    public LeadPriority Priority { get; private set; } = LeadPriority.Medium;

    public List<string> Tags { get; private set; } = [];

    public ICollection<WebsiteAnalysis> Analyses { get; private set; } = new List<WebsiteAnalysis>();

    public ICollection<SalesEmail> Emails { get; private set; } = new List<SalesEmail>();

    public static Business Create(
        string externalId,
        string name,
        string address,
        double latitude,
        double longitude,
        BusinessSourceType source,
        string? phone,
        string? email,
        string? website)
    {
        if (string.IsNullOrWhiteSpace(externalId))
        {
            throw new ArgumentException("External identifier is required.", nameof(externalId));
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Business name is required.", nameof(name));
        }

        return new Business(
            externalId.Trim(),
            name.Trim(),
            string.IsNullOrWhiteSpace(address) ? "Unknown address" : address.Trim(),
            latitude,
            longitude,
            source,
            phone,
            email,
            website);
    }

    public void UpdateDetails(
        string? address,
        double latitude,
        double longitude,
        string? phone,
        string? email,
        string? website,
        DateTime timestampUtc)
    {
        Address = string.IsNullOrWhiteSpace(address) ? Address : address.Trim();
        Latitude = latitude;
        Longitude = longitude;
        Phone = FirstNonEmpty(phone, Phone);
        Email = FirstNonEmpty(email, Email);
        Website = NormalizeWebsite(FirstNonEmpty(website, Website));
        Touch(timestampUtc);
    }

    public void UpdateLead(int leadScore, LeadPriority priority, DateTime timestampUtc)
    {
        LeadScore = Math.Clamp(leadScore, 0, 100);
        Priority = priority;
        Touch(timestampUtc);
    }

    public void AddTag(string tag, DateTime timestampUtc)
    {
        if (string.IsNullOrWhiteSpace(tag))
        {
            return;
        }

        if (Tags.Any(existing => string.Equals(existing, tag.Trim(), StringComparison.OrdinalIgnoreCase)))
        {
            return;
        }

        Tags.Add(tag.Trim());
        Touch(timestampUtc);
    }

    private static string? FirstNonEmpty(string? candidate, string? fallback)
    {
        return string.IsNullOrWhiteSpace(candidate) ? fallback : candidate.Trim();
    }

    private static string? Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static string? NormalizeWebsite(string? website)
    {
        var normalized = Normalize(website);
        if (normalized is null)
        {
            return null;
        }

        if (normalized.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            normalized.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            return normalized;
        }

        return $"https://{normalized}";
    }
}
