using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using OpenBudget.Application.Services;
using OpenBudget.Domain.Interfaces;
using OpenBudget.Infrastructure.Data;
using OpenBudget.Infrastructure.Repositories;
using OpenBudget.Bot.Services;
using Telegram.Bot;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// DbContext
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Repositories
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IVoteRepository, VoteRepository>();

// Services
builder.Services.AddScoped<IVoteService, VoteService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddSingleton<ITokenService, TokenService>();
builder.Services.AddSingleton<INotificationService, ErrorNotificationService>(); // Includes both Error and Main Bot notifications logic

// Handlers
builder.Services.AddScoped<OpenBudget.Bot.Handlers.UpdateHandler>();
builder.Services.AddScoped<OpenBudget.Bot.Handlers.GroupMemberHandler>();
builder.Services.AddScoped<OpenBudget.Bot.Handlers.BrokerHandler>();
builder.Services.AddScoped<OpenBudget.Bot.Handlers.AdminHandler>();
builder.Services.AddScoped<OpenBudget.Bot.Handlers.SuperAdminHandler>();

// Telegram Bot
builder.Services.AddSingleton<ITelegramBotClient>(sp =>
{
    var token = builder.Configuration["TelegramBot:MainBotToken"]!;
    return new TelegramBotClient(token);
});

builder.Services.AddHostedService<BotService>();

// CORS for Mini App
var allowedOrigins = builder.Configuration.GetSection("MiniApp:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
builder.Services.AddCors(options =>
{
    options.AddPolicy("MiniAppPolicy", policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// Authentication
var jwtSecret = builder.Configuration["Jwt:SecretKey"]!;
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret))
        };
    });

builder.Services.AddControllers();

var app = builder.Build();

app.UseMiddleware<OpenBudget.Bot.Middlewares.ExceptionMiddleware>();
app.UseCors("MiniAppPolicy");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
