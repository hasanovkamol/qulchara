using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using OpenBudget.Domain.Entities;
using OpenBudget.Domain.Enums;
using OpenBudget.Application.DTOs;

namespace OpenBudget.Application.Services;

public interface IUserService
{
    Task<User?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<User?> GetByTelegramIdAsync(long telegramId, CancellationToken cancellationToken = default);
    Task<User> RegisterUserAsync(long telegramId, string? username, string? fullName, UserRole role = UserRole.Guest, CancellationToken cancellationToken = default);
    Task UpdateStateAsync(int userId, BotState state, CancellationToken cancellationToken = default);
    Task<(bool Success, string Message)> AssignRoleAsync(int targetUserId, UserRole newRole, UserRole assignerRole, CancellationToken cancellationToken = default);
    Task UpdateRoleAsync(int userId, UserRole newRole, CancellationToken cancellationToken = default);
    Task PromoteToBrokerAsync(int userId, CancellationToken cancellationToken = default);
    Task<(bool Success, string Message, User? TargetUser)> PromoteBrokerByIdentifierAsync(string identifier, CancellationToken cancellationToken = default);
    Task<(bool Success, string Message, bool NewStatus)> ToggleUserBlockAsync(int targetUserId, UserRole actorRole, CancellationToken cancellationToken = default);
    Task<List<User>> GetAllUsersAsync(CancellationToken cancellationToken = default);
    Task<GlobalStatsDto> GetGlobalStatsAsync(CancellationToken cancellationToken = default);
}
