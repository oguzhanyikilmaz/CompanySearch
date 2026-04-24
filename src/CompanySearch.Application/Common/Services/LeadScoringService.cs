using CompanySearch.Domain.Entities;
using CompanySearch.Domain.Enums;

namespace CompanySearch.Application.Common.Services;

public sealed class LeadScoringService : ILeadScoringService
{
    public (int Score, LeadPriority Priority) Calculate(Business business, WebsiteAnalysis analysis)
    {
        var opportunityScore = 100 - analysis.Score;

        if (!string.IsNullOrWhiteSpace(business.Email))
        {
            opportunityScore += 12;
        }

        if (!string.IsNullOrWhiteSpace(business.Phone))
        {
            opportunityScore += 8;
        }

        if (!string.IsNullOrWhiteSpace(business.Website))
        {
            opportunityScore += 5;
        }

        var score = Math.Clamp(opportunityScore, 0, 100);
        var priority = score switch
        {
            >= 80 => LeadPriority.Strategic,
            >= 60 => LeadPriority.High,
            >= 35 => LeadPriority.Medium,
            _ => LeadPriority.Low
        };

        return (score, priority);
    }
}
