using CompanySearch.Domain.Entities;
using CompanySearch.Domain.Enums;

namespace CompanySearch.Application.Common.Services;

public interface ILeadScoringService
{
    (int Score, LeadPriority Priority) Calculate(Business business, WebsiteAnalysis analysis);
}
