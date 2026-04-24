using CompanySearch.Api.Contracts.Search;
using CompanySearch.Application.Businesses.Commands.SearchBusinesses;
using CompanySearch.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace CompanySearch.Api.Controllers;

[ApiController]
[Route("api/search")]
public sealed class SearchController(ISender sender) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(SearchJobDto), StatusCodes.Status202Accepted)]
    public async Task<IActionResult> StartSearch([FromBody] SearchBusinessesRequest request, CancellationToken cancellationToken)
    {
        var source = Enum.TryParse<BusinessSourceType>(request.Source, true, out var parsedSource)
            ? parsedSource
            : BusinessSourceType.OpenStreetMap;

        var response = await sender.Send(
            new SearchBusinessesCommand(
                request.Location,
                request.Latitude,
                request.Longitude,
                request.RadiusKm,
                source,
                request.AutoAnalyzeWebsites,
                request.AutoGenerateEmails),
            cancellationToken);

        return Accepted(response);
    }
}
