using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenBudget.Application.Services;

namespace OpenBudget.Bot.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class VoteController : ControllerBase
{
    private readonly IVoteService _voteService;

    public VoteController(IVoteService voteService)
    {
        _voteService = voteService;
    }

    [HttpGet("broker")]
    [Authorize(Roles = "Broker")]
    public async Task<IActionResult> GetBrokerVotes([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await _voteService.GetBrokerVotesPagedAsync(userId, page, pageSize);
        return Ok(result);
    }

    [HttpPost("confirm")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ConfirmVote([FromBody] ConfirmVoteRequest request)
    {
        var adminId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await _voteService.ConfirmVoteAsync(adminId, request.Last3Digits, request.TargetUtcTime, TimeSpan.FromHours(request.TimeWindowHours));
        
        if (result.Success) return Ok(new { message = result.Message });
        return BadRequest(new { message = result.Message });
    }
}

public class ConfirmVoteRequest
{
    public string Last3Digits { get; set; } = null!;
    public DateTime TargetUtcTime { get; set; }
    public int TimeWindowHours { get; set; } = 1;
}
