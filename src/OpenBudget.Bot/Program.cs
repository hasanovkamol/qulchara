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
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"),
        sqlOptions => sqlOptions.EnableRetryOnFailure(
            maxRetryCount: 10,
            maxRetryDelay: TimeSpan.FromSeconds(5),
            errorNumbersToAdd: null)));

// Repositories
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IVoteRepository, VoteRepository>();
builder.Services.AddScoped<ITelegramGroupRepository, TelegramGroupRepository>();
builder.Services.AddScoped<IBotSettingRepository, BotSettingRepository>();

// Services
builder.Services.AddScoped<IVoteService, VoteService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<ITelegramGroupService, TelegramGroupService>();
builder.Services.AddScoped<IBotSettingService, BotSettingService>();
builder.Services.AddSingleton<ITokenService, TokenService>();
builder.Services.AddSingleton<INotificationService, ErrorNotificationService>(); // Includes both Error and Main Bot notifications logic
builder.Services.AddSingleton<IQrCodeService, OpenBudget.Application.Services.QrCodeService>();
builder.Services.AddSingleton<OpenBudget.Bot.Services.IDocumentationService, OpenBudget.Bot.Services.DocumentationService>();

// Handlers
builder.Services.AddScoped<OpenBudget.Bot.Handlers.UpdateHandler>();
builder.Services.AddScoped<OpenBudget.Bot.Handlers.GroupMemberHandler>();
builder.Services.AddScoped<OpenBudget.Bot.Handlers.GuestHandler>();
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

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    try
    {
        dbContext.Database.ExecuteSqlRaw(@"
            IF EXISTS (SELECT * FROM sys.tables WHERE name = 'Users')
            BEGIN
                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = '__EFMigrationsHistory')
                BEGIN
                    CREATE TABLE [__EFMigrationsHistory] (
                        [MigrationId] nvarchar(150) NOT NULL,
                        [ProductVersion] nvarchar(32) NOT NULL,
                        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
                    );
                END;

                IF NOT EXISTS (SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260820034820_InitialCreate')
                BEGIN
                    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
                    VALUES (N'20260820034820_InitialCreate', N'10.0.11');
                END;
            END;");
    }
    catch
    {
    }

    dbContext.Database.Migrate();
}

app.Run();
