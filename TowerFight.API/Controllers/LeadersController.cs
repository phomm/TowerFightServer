using TowerFight.BusinessLogic.Models;
using TowerFight.BusinessLogic.Services;
using Microsoft.AspNetCore.Mvc;

namespace TowerFight.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LeadersController(ILeadersService LeadersService) : ControllerBase
{
    private readonly ILeadersService _LeadersService = LeadersService;

    [HttpGet("", Name = "GetLeaders")]
    [ProducesResponseType(typeof(IEnumerable<Leader>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetLeadersAsync(CancellationToken cancellationToken)
    {
        return Ok(await _LeadersService.GetLeadersAsync(cancellationToken));
    }
    
    
}