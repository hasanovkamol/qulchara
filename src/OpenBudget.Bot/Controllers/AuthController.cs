using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using OpenBudget.Application.DTOs;
using OpenBudget.Application.Helpers;
using OpenBudget.Application.Services;
using OpenBudget.Domain.Interfaces;

namespace OpenBudget.Bot.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly IUserService _userService;
    private readonly ITokenService _tokenService;

    public AuthController(IConfiguration configuration, IUserService userService, ITokenService tokenService)
    {
        _configuration = configuration;
        _userService = userService;
        _tokenService = tokenService;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] TelegramAuthRequest request)
    {
        var botToken = _configuration["TelegramBot:MainBotToken"];
        if (string.IsNullOrEmpty(botToken) || !TelegramAuthHelper.ValidateInitData(request.InitData, botToken))
        {
            return Unauthorized(new { message = "Invalid Telegram initialization data." });
        }

        var tgUser = TelegramAuthHelper.ParseInitData(request.InitData);
        if (tgUser == null)
        {
            return BadRequest(new { message = "Could not parse user data." });
        }

        // Mini app orqali kelgan odam DB da bo'lishi kerak, yo'q bo'lsa register qilamiz.
        // Aslida faqat guruhdagilar broker bo'lishi kerak, lekin agar mini app dan kirsa
        // uni broker qilib qo'shib yuboramiz yoki xato beramiz.
        // Hozir registerUser metodidan foydalanamiz, bu mavjud bo'lsa qaytaradi.
        var user = await _userService.RegisterUserAsync(
            tgUser.Id, 
            tgUser.Username, 
            $"{tgUser.First_name} {tgUser.Last_name}".Trim()
        );

        var token = _tokenService.GenerateToken(user);

        return Ok(new AuthResponse
        {
            Token = token,
            Role = user.Role.ToString()
        });
    }
}
