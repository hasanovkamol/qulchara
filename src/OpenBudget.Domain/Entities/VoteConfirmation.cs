using System;
using OpenBudget.Domain.Enums;

namespace OpenBudget.Domain.Entities;

public class VoteConfirmation
{
    public long Id { get; set; }
    public string LastNDigits { get; set; } = null!;
    public DateTime TargetTime { get; set; }
    public int AdminId { get; set; }
    public VoteConfirmationStatus Status { get; set; } = VoteConfirmationStatus.Pending;
    public long? MatchedVoteId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public User? Admin { get; set; }
    public Vote? MatchedVote { get; set; }
}
