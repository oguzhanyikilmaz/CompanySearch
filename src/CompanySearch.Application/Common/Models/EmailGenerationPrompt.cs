using CompanySearch.Domain.ValueObjects;

namespace CompanySearch.Application.Common.Models;

public sealed record EmailGenerationPrompt(
    string BusinessName,
    string? Website,
    int Score,
    string Summary,
    IReadOnlyCollection<WebsiteIssue> Issues,
    string? RecipientEmail);
