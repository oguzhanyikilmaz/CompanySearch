using System.Text;
using CompanySearch.Application.Abstractions.Persistence;
using CompanySearch.Application.Common.Models;
using CompanySearch.Domain.Enums;
using MediatR;

namespace CompanySearch.Application.Exports.Queries.ExportBusinesses;

public sealed record ExportBusinessesQuery(
    string? SearchTerm = null,
    bool? HasWebsite = null,
    int? MinScore = null,
    int? MaxScore = null,
    LeadPriority? Priority = null) : IRequest<string>;

public sealed class ExportBusinessesQueryHandler(IBusinessRepository businessRepository)
    : IRequestHandler<ExportBusinessesQuery, string>
{
    public async Task<string> Handle(ExportBusinessesQuery request, CancellationToken cancellationToken)
    {
        var results = await businessRepository.ListAsync(
            new BusinessSearchFilters(
                1,
                5_000,
                request.SearchTerm,
                request.HasWebsite,
                request.MinScore,
                request.MaxScore,
                BusinessListSortBy.LeadScore,
                null,
                null,
                request.Priority),
            cancellationToken);

        var builder = new StringBuilder();
        builder.AppendLine("Id,Name,Address,Phone,Email,Website,Source,LeadScore,Priority,LatestScore");

        foreach (var business in results.Items)
        {
            var latestAnalysis = business.Analyses.OrderByDescending(analysis => analysis.CreatedAtUtc).FirstOrDefault();
            builder.AppendLine(string.Join(",",
                Escape(business.Id.ToString()),
                Escape(business.Name),
                Escape(business.Address),
                Escape(business.Phone),
                Escape(business.Email),
                Escape(business.Website),
                Escape(business.Source.ToString()),
                Escape(business.LeadScore.ToString()),
                Escape(business.Priority.ToString()),
                Escape(latestAnalysis?.Score.ToString())));
        }

        return builder.ToString();
    }

    private static string Escape(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        return $"\"{value.Replace("\"", "\"\"")}\"";
    }
}
