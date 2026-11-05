using Microsoft.AspNetCore.Mvc;
using TowerFight.API.Models;
using TowerFight.BusinessLogic.Services;

namespace TowerFight.API.Controllers;

/// <summary>Receives Banzai log entries.</summary>
[ApiController]
[Route("api/banzai-log")]
public sealed class BanzaiLogController(
    IBanzaiLogService banzaiLogService,
    ILogger<BanzaiLogController> logger) : ControllerBase
{
    /// <summary>Inserts a Banzai log entry.</summary>
    [HttpPost(Name = "InsertBanzaiLog")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> InsertAsync(
        [FromBody] InsertBanzaiLogRequest request,
        CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Received Banzai log request for {Guid} with {EntryLength} characters.",
            request.Guid,
            request.Entry.Length);

        await banzaiLogService.InsertAsync(request.Guid, request.Entry, cancellationToken);

        logger.LogInformation("Banzai log request for {Guid} completed successfully.", request.Guid);
        return Ok();
    }
}
