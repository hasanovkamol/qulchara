using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using OpenBudget.Application.DTOs;
using OpenBudget.Domain.Entities;
using OpenBudget.Domain.Enums;
using OpenBudget.Domain.Interfaces;

namespace OpenBudget.Application.Services;

public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;
    private readonly IVoteRepository _voteRepository;

    public UserService(IUserRepository userRepository, IVoteRepository voteRepository)
    {
        _userRepository = userRepository;
        _voteRepository = voteRepository;
    }

    public Task<User?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return _userRepository.GetByIdAsync(id, cancellationToken);
    }

    public Task<User?> GetByTelegramIdAsync(long telegramId, CancellationToken cancellationToken = default)
    {
        return _userRepository.GetByTelegramIdAsync(telegramId, cancellationToken);
    }

    public async Task<User> RegisterUserAsync(long telegramId, string? username, string? fullName, UserRole role = UserRole.Broker, CancellationToken cancellationToken = default)
    {
        var existingUser = await _userRepository.GetByTelegramIdAsync(telegramId, cancellationToken);
        if (existingUser != null)
        {
            return existingUser; // Already exists
        }

        var newUser = new User
        {
            TelegramId = telegramId,
            Username = username,
            FullName = fullName,
            Role = role
        };

        await _userRepository.AddAsync(newUser, cancellationToken);
        return newUser;
    }

    public async Task<(bool Success, string Message)> AssignRoleAsync(int targetUserId, UserRole newRole, UserRole assignerRole, CancellationToken cancellationToken = default)
    {
        // Rules:
        // SuperAdmin can assign Admin
        // Admin can assign Broker

        if (assignerRole == UserRole.SuperAdmin && newRole == UserRole.Admin)
        {
            // allowed
        }
        else if (assignerRole == UserRole.Admin && newRole == UserRole.Broker)
        {
            // allowed
        }
        else if (assignerRole == UserRole.SuperAdmin && newRole == UserRole.Broker)
        {
            // SuperAdmin can also assign Broker or remove roles (make Broker)
        }
        else
        {
            return (false, "Sizda bunday rol berish huquqi yo'q.");
        }

        var targetUser = await _userRepository.GetByIdAsync(targetUserId, cancellationToken);
        if (targetUser == null)
        {
            return (false, "Foydalanuvchi topilmadi.");
        }

        if (targetUser.Role == UserRole.SuperAdmin)
        {
            return (false, "SuperAdmin rolini o'zgartirib bo'lmaydi.");
        }

        targetUser.Role = newRole;
        await _userRepository.UpdateAsync(targetUser, cancellationToken);

        return (true, "Rol muvaffaqiyatli o'zgartirildi.");
    }

    public async Task UpdateStateAsync(int userId, BotState state, CancellationToken cancellationToken = default)
    {
        var targetUser = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (targetUser != null)
        {
            targetUser.BotState = state;
            await _userRepository.UpdateAsync(targetUser, cancellationToken);
        }
    }

    public async Task UpdateRoleAsync(int userId, UserRole newRole, CancellationToken cancellationToken = default)
    {
        var targetUser = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (targetUser != null)
        {
            targetUser.Role = newRole;
            await _userRepository.UpdateAsync(targetUser, cancellationToken);
        }
    }

    public async Task<(bool Success, string Message, bool NewStatus)> ToggleUserBlockAsync(int targetUserId, UserRole actorRole, CancellationToken cancellationToken = default)
    {
        var targetUser = await _userRepository.GetByIdAsync(targetUserId, cancellationToken);
        if (targetUser == null)
        {
            return (false, "Foydalanuvchi topilmadi.", false);
        }

        if (targetUser.Role == UserRole.SuperAdmin)
        {
            return (false, "SuperAdmin foydalanuvchisini bloklab bo'lmaydi.", targetUser.IsActive);
        }

        if (actorRole == UserRole.Admin && targetUser.Role == UserRole.Admin)
        {
            return (false, "Admin boshqa Adminni bloklay olmaydi.", targetUser.IsActive);
        }

        targetUser.IsActive = !targetUser.IsActive;
        targetUser.UpdatedAt = System.DateTime.UtcNow;

        await _userRepository.UpdateAsync(targetUser, cancellationToken);

        var statusText = targetUser.IsActive ? "blokdan chiqarildi" : "bloklandi";
        return (true, $"Foydalanuvchi {targetUser.FullName ?? targetUser.Username ?? targetUser.TelegramId.ToString()} {statusText}.", targetUser.IsActive);
    }

    public Task<List<User>> GetAllUsersAsync(CancellationToken cancellationToken = default)
    {
        return _userRepository.GetAllAsync(cancellationToken);
    }

    public async Task<GlobalStatsDto> GetGlobalStatsAsync(CancellationToken cancellationToken = default)
    {
        var totalBrokers = await _userRepository.GetCountByRoleAsync(UserRole.Broker, cancellationToken);
        var totalAdmins = await _userRepository.GetCountByRoleAsync(UserRole.Admin, cancellationToken);

        var totalVotes = await _voteRepository.GetTotalCountAsync(cancellationToken);
        var confirmedVotes = await _voteRepository.GetCountByStatusAsync(VoteStatus.Confirmed, cancellationToken);
        var pendingVotes = await _voteRepository.GetCountByStatusAsync(VoteStatus.Pending, cancellationToken);
        var rejectedVotes = await _voteRepository.GetCountByStatusAsync(VoteStatus.Rejected, cancellationToken);

        return new GlobalStatsDto
        {
            TotalBrokers = totalBrokers,
            TotalAdmins = totalAdmins,
            TotalVotes = totalVotes,
            ConfirmedVotes = confirmedVotes,
            PendingVotes = pendingVotes,
            RejectedVotes = rejectedVotes
        };
    }
}
