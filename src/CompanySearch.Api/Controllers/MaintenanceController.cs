using CompanySearch.Api.Contracts.Maintenance;
using CompanySearch.Application.Maintenance.Commands.PurgeAllApplicationData;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace CompanySearch.Api.Controllers;

[ApiController]
[Route("api/maintenance")]
public sealed class MaintenanceController(ISender sender) : ControllerBase
{
    /// <summary>
    /// Tüm işletmeler, web analizleri, e-postalar ve arama işlerini kalıcı olarak siler.
    /// </summary>
    [HttpPost("purge-all")]
    public async Task<IActionResult> PurgeAll([FromBody] PurgeAllDataRequest request, CancellationToken cancellationToken)
    {
        await sender.Send(new PurgeAllApplicationDataCommand(request.Confirmation), cancellationToken);
        return NoContent();
    }
}
