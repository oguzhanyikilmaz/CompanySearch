using CompanySearch.Api.Contracts.Analysis;
using CompanySearch.Application.Analysis.Commands.AnalyzeBusiness;
using CompanySearch.Application.Businesses.Queries.GetBusinessById;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace CompanySearch.Api.Controllers;

[ApiController]
[Route("api")]
public sealed class AnalysisController(ISender sender) : ControllerBase
{
    [HttpPost("analyze/{businessId:guid}")]
    public Task<WebsiteAnalysisDto> AnalyzeBusiness(Guid businessId, [FromBody] AnalyzeBusinessRequest? request, CancellationToken cancellationToken)
    {
        return sender.Send(new AnalyzeBusinessCommand(businessId, request?.GenerateEmailAfterAnalysis ?? false), cancellationToken);
    }
}
