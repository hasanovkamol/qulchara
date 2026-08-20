using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using OpenBudget.Application.DTOs;
using OpenBudget.Application.Helpers;
using OpenBudget.Application.Validators;
using OpenBudget.Domain.Entities;
using OpenBudget.Domain.Enums;
using OpenBudget.Domain.Interfaces;

namespace OpenBudget.Application.Services;

public class VoteService : IVoteService
{
    private readonly IVoteRepository _voteRepository;
    private readonly IUserRepository _userRepository;
    private readonly INotificationService _notificationService;

    public VoteService(
        IVoteRepository voteRepository, 
        IUserRepository userRepository,
        INotificationService notificationService)
    {
        _voteRepository = voteRepository;
        _userRepository = userRepository;
        _notificationService = notificationService;
    }

    public async Task<(bool Success, string Message)> AddVoteAsync(int brokerId, string rawPhoneNumber, CancellationToken cancellationToken = default)
    {
        var validation = PhoneNumberValidator.ValidateAndFormat(rawPhoneNumber);
        if (!validation.IsValid)
        {
            return (false, "Iltimos, 9 xonali raqam kiriting. Faqat raqamlar qabul qilinadi.");
        }

        var existingVote = await _voteRepository.GetByPhoneNumberAsync(validation.FormattedNumber, cancellationToken);
        if (existingVote != null)
        {
            return (false, "Bu raqam avval kiritilgan!");
        }

        var vote = new Vote
        {
            BrokerId = brokerId,
            PhoneNumber = validation.FormattedNumber,
            Status = VoteStatus.Pending,
            VotedAt = DateTimeHelper.UzbekistanNow,
            CreatedAt = DateTimeHelper.UzbekistanNow
        };

        await _voteRepository.AddAsync(vote, cancellationToken);
        return (true, $"{validation.FormattedNumber} qabul qilindi. Kutish holatida.");
    }

    public async Task<(bool Success, string Message)> ConfirmVoteAsync(int adminId, string lastNDigits, DateTime targetTime, TimeSpan timeWindow, CancellationToken cancellationToken = default)
    {
        var vote = await _voteRepository.GetPendingVoteToConfirmAsync(lastNDigits, targetTime, timeWindow, cancellationToken);
        
        if (vote == null)
        {
            return (false, "Mos keluvchi nomer topilmadi. Raqam yoki vaqtni tekshiring.");
        }

        vote.Status = VoteStatus.Confirmed;
        vote.ConfirmedAt = DateTimeHelper.UzbekistanNow;
        vote.ConfirmedByAdminId = adminId;

        await _voteRepository.UpdateAsync(vote, cancellationToken);

        var broker = await _userRepository.GetByIdAsync(vote.BrokerId, cancellationToken);
        if (broker != null)
        {
            await _notificationService.NotifyBrokerVoteConfirmedAsync(broker.TelegramId, vote.PhoneNumber, cancellationToken);
        }

        return (true, $"Tasdiqlandi: {MaskPhoneNumber(vote.PhoneNumber)}");
    }

    public async Task<PaginatedResult<VoteDto>> GetBrokerVotesPagedAsync(int brokerId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var (items, totalCount) = await _voteRepository.GetByBrokerIdPagedAsync(brokerId, page, pageSize, cancellationToken);

        var dtos = items.Select(x => new VoteDto
        {
            Id = x.Id,
            PhoneNumber = x.PhoneNumber,
            Status = x.Status,
            VotedAt = x.VotedAt
        }).ToList();

        return new PaginatedResult<VoteDto>
        {
            Items = dtos,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<PaginatedResult<VoteDto>> GetAllVotesPagedAsync(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var result = await _voteRepository.GetAllPagedAsync(page, pageSize, cancellationToken);
        var dtos = result.Items.Select(v => new VoteDto
        {
            Id = v.Id,
            BrokerId = v.BrokerId,
            BrokerName = v.Broker?.FullName ?? "Noma'lum",
            PhoneNumber = MaskPhoneNumber(v.PhoneNumber),
            Status = v.Status,
            VotedAt = v.VotedAt,
            ConfirmedAt = v.ConfirmedAt,
            RejectReason = v.RejectReason
        }).ToList();

        return new PaginatedResult<VoteDto>
        {
            Items = dtos,
            TotalCount = result.TotalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<BrokerStatsDto> GetBrokerStatsAsync(int brokerId, CancellationToken cancellationToken = default)
    {
        var pending = await _voteRepository.GetCountByBrokerAndStatusAsync(brokerId, VoteStatus.Pending, cancellationToken);
        var confirmed = await _voteRepository.GetCountByBrokerAndStatusAsync(brokerId, VoteStatus.Confirmed, cancellationToken);
        var rejected = await _voteRepository.GetCountByBrokerAndStatusAsync(brokerId, VoteStatus.Rejected, cancellationToken);

        return new BrokerStatsDto
        {
            TotalVotes = pending + confirmed + rejected,
            ConfirmedVotes = confirmed,
            PendingVotes = pending,
            RejectedVotes = rejected
        };
    }

    private string MaskPhoneNumber(string phoneNumber)
    {
        if (phoneNumber.Length < 4) return phoneNumber;
        var last3 = phoneNumber.Substring(phoneNumber.Length - 3);
        return "+998***" + last3;
    }
}
