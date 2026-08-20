using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using OpenBudget.Application.DTOs;
using OpenBudget.Application.Helpers;
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

    public async Task<User> RegisterUserAsync(long telegramId, string? username, string? fullName, UserRole role = UserRole.Guest, CancellationToken cancellationToken = default)
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
            Role = role,
            CreatedAt = DateTimeHelper.UzbekistanNow
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
        targetUser.UpdatedAt = DateTimeHelper.UzbekistanNow;
        await _userRepository.UpdateAsync(targetUser, cancellationToken);

        return (true, "Rol muvaffaqiyatli o'zgartirildi.");
    }

    public async Task<(bool Success, string Message)> AssignRoleByIdentifierAsync(string identifier, UserRole newRole, UserRole assignerRole, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(identifier))
        {
            return (false, "Foydalanuvchi ma'lumoti kiritilmadi.");
        }

        identifier = identifier.Trim();
        User? targetUser = null;

        if (long.TryParse(identifier, out long telegramId))
        {
            targetUser = await _userRepository.GetByTelegramIdAsync(telegramId, cancellationToken);
        }
        else
        {
            var username = identifier.StartsWith("@") ? identifier.Substring(1) : identifier;
            targetUser = await _userRepository.GetByUsernameAsync(username, cancellationToken);
        }

        if (targetUser == null)
        {
            return (false, "Foydalanuvchi topilmadi. U botga start bosgan bo'lishi kerak.");
        }

        return await AssignRoleAsync(targetUser.Id, newRole, assignerRole, cancellationToken);
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
            targetUser.UpdatedAt = DateTimeHelper.UzbekistanNow;
            await _userRepository.UpdateAsync(targetUser, cancellationToken);
        }
    }

    public async Task PromoteToBrokerAsync(int userId, CancellationToken cancellationToken = default)
    {
        var targetUser = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (targetUser != null)
        {
            targetUser.Role = UserRole.Broker;
            targetUser.IsActive = true;
            targetUser.UpdatedAt = DateTimeHelper.UzbekistanNow;
            await _userRepository.UpdateAsync(targetUser, cancellationToken);
        }
    }

    public async Task<(bool Success, string Message, User? TargetUser)> PromoteBrokerByIdentifierAsync(string identifier, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(identifier))
        {
            return (false, "Foydalanuvchi ma'lumoti kiritilmadi.", null);
        }

        identifier = identifier.Trim();
        User? user = null;

        if (long.TryParse(identifier, out long telegramId))
        {
            user = await _userRepository.GetByTelegramIdAsync(telegramId, cancellationToken);
            if (user == null)
            {
                user = new User
                {
                    TelegramId = telegramId,
                    Role = UserRole.Broker,
                    IsActive = true,
                    CreatedAt = DateTimeHelper.UzbekistanNow
                };
                await _userRepository.AddAsync(user, cancellationToken);
                return (true, $"Yangi foydalanuvchi (ID: {telegramId}) yaratildi va Broker sifatida qo'shildi.", user);
            }
        }
        else
        {
            var username = identifier.StartsWith("@") ? identifier.Substring(1) : identifier;
            user = await _userRepository.GetByUsernameAsync(username, cancellationToken);
            if (user == null)
            {
                return (false, $"@{username} nomli foydalanuvchi bazada topilmadi. U avval botga kamida bir marta /start yuborgan bo'lishi kerak.", null);
            }
        }

        user.Role = UserRole.Broker;
        user.IsActive = true;
        user.UpdatedAt = DateTimeHelper.UzbekistanNow;
        await _userRepository.UpdateAsync(user, cancellationToken);

        var name = user.FullName ?? (string.IsNullOrEmpty(user.Username) ? user.TelegramId.ToString() : $"@{user.Username}");
        return (true, $"Foydalanuvchi {name} Broker sifatida faollashtirildi.", user);
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
        targetUser.UpdatedAt = DateTimeHelper.UzbekistanNow;

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
