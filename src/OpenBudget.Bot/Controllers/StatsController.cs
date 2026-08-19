using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenBudget.Application.Services;

namespace OpenBudget.Bot.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class StatsController : ControllerBase
{
    private readonly IVoteService _voteService;
    private readonly IUserService _userService;

    public StatsController(IVoteService voteService, IUserService userService)
    {
        _voteService = voteService;
        _userService = userService;
    }

    [HttpGet("broker")]
    [Authorize(Roles = "Broker,Admin,SuperAdmin")]
    public async Task<IActionResult> GetBrokerStats([FromQuery] int? brokerId = null)
    {
        var currentUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var role = User.FindFirstValue(ClaimTypes.Role);

        int targetId = currentUserId;

        // Admin/SuperAdmin can view other broker's stats
        if (brokerId.HasValue && (role == "Admin" || role == "SuperAdmin"))
        {
            targetId = brokerId.Value;
        }

        var stats = await _voteService.GetBrokerStatsAsync(targetId);
        return Ok(stats);
    }

    [HttpGet("global")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> GetGlobalStats()
    {
        var stats = await _userService.GetGlobalStatsAsync();
        return Ok(stats);
    }
}
