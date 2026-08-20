using System;
using OpenBudget.Domain.Enums;

namespace OpenBudget.Application.DTOs;

public class VoteDto
{
    public long Id { get; set; }
    public string PhoneNumber { get; set; } = null!;
    public int? BrokerId { get; set; }
    public string? BrokerName { get; set; }
    public VoteStatus Status { get; set; }
    public DateTime VotedAt { get; set; }
    public DateTime? ConfirmedAt { get; set; }
    public string? RejectReason { get; set; }
}
