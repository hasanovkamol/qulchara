using System;
using OpenBudget.Domain.Enums;

namespace OpenBudget.Domain.Entities;

public class Vote
{
    public long Id { get; set; }
    public int BrokerId { get; set; }
    public string PhoneNumber { get; set; } = null!;
    public VoteStatus Status { get; set; } = VoteStatus.Pending;
    public DateTime VotedAt { get; set; }
    public DateTime? ConfirmedAt { get; set; }
    public int? ConfirmedByAdminId { get; set; }
    public string? RejectReason { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public User? Broker { get; set; }
    public User? ConfirmedByAdmin { get; set; }
}
