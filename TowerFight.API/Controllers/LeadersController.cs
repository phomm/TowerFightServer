using TowerFight.BusinessLogic.Models;
using TowerFight.BusinessLogic.Services;
using TowerFight.API.Models;
using TowerFight.API.Utilities;
using Microsoft.AspNetCore.Mvc;
using TowerFight.BusinessLogic.Data.Config;
using System.Net;

namespace TowerFight.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LeadersController(ILeadersService _LeadersService, IConfiguration configuration) : ControllerBase
{
    [HttpGet("", Name = "GetLeaders")]
    [ProducesResponseType(typeof(IEnumerable<Leader>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetLeadersAsync(CancellationToken cancellationToken)
    {
        return Ok(await _LeadersService.GetLeadersAsync(cancellationToken));
    }

    [HttpPost("", Name = "InsertHighscore")]
    [ProducesResponseType(typeof(InsertHighscoreResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> InsertHighscoreAsync(
        [FromBody] InsertHighscoreRequest request,
        CancellationToken cancellationToken)
    {
        var signingSettings = configuration.GetSection(nameof(SigningSettings)).Get<SigningSettings>()!;
        if (signingSettings.Enabled && !HighscoreHashUtility.IsValid(request))
        {
            return Problem("Invalid request. Please update your app", statusCode: (int)HttpStatusCode.BadRequest);
        }

        var leader = new Leader
        {
            Difficulty = request.Difficulty!.Value,
            Score = request.Score!.Value,
            Name = request.Name!
        };

        var result = await _LeadersService.InsertHighscoreAsync(
            leader,
            request.Guid,
            cancellationToken);

        return result.Match<IActionResult>(
            success => Created(string.Empty, new InsertHighscoreResponse(success.Guid)),
            nameError => Problem(nameError.Reason, statusCode: (int)HttpStatusCode.Conflict),
            noChanges => Accepted(string.Empty, noChanges.Reason)
            );
    }

    public record InsertHighscoreResponse(Guid Guid);
}
