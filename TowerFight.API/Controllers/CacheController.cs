using TowerFight.BusinessLogic.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using TowerFight.API.Bootstrap;

namespace TowerFight.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CacheController(ICacheService cacheService) : ControllerBase
{
    private readonly ICacheService _cacheService = cacheService;

    [HttpDelete("clear", Name = "ClearCache")]
    [Authorize(Policies.Admin)]
    public async Task<IActionResult> ClearCacheAsync(CancellationToken cancellationToken)
    {
        await _cacheService.ClearCache(cancellationToken);
        return Ok();
    }

    [HttpPut("pushToDb", Name = "PushToDb")]
    [Authorize(Policies.Admin)]
    public async Task<IActionResult> PushToDbAsync(CancellationToken cancellationToken)
    {
        await _cacheService.PushToDb(cancellationToken);
        return Ok();
    }
}