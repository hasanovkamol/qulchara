using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenBudget.Application.Services;
using OpenBudget.Domain.Enums;

namespace OpenBudget.Bot.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UserController : ControllerBase
{
    private readonly IUserService _userService;

    public UserController(IUserService userService)
    {
        _userService = userService;
    }

    [HttpGet("me")]
    public async Task<IActionResult> GetMe()
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
        {
            return Unauthorized();
        }

        var user = await _userService.GetByIdAsync(userId);
        if (user == null) return NotFound();

        return Ok(new
        {
            user.Id,
            user.TelegramId,
            user.Username,
            user.FullName,
            Role = user.Role.ToString()
        });
    }

    [HttpGet("list")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> GetUsers()
    {
        var users = await _userService.GetAllUsersAsync();
        return Ok(users);
    }

    [HttpPost("assign-role")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> AssignRole([FromBody] AssignRoleRequest request)
    {
        var assignerRoleStr = User.FindFirstValue(ClaimTypes.Role);
        if (!Enum.TryParse(assignerRoleStr, out UserRole assignerRole))
        {
            return Unauthorized();
        }

        if (!Enum.TryParse(request.NewRole, out UserRole newRole))
        {
            return BadRequest(new { message = "Invalid role specified." });
        }

        var result = await _userService.AssignRoleAsync(request.TargetUserId, newRole, assignerRole);
        
        if (result.Success) return Ok(new { message = result.Message });
        return BadRequest(new { message = result.Message });
    }
}

public class AssignRoleRequest
{
    public int TargetUserId { get; set; }
    public string NewRole { get; set; } = null!;
}
