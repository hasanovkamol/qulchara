using System;
using OpenBudget.Domain.Enums;

namespace OpenBudget.Application.DTOs;

public class VoteConfirmationDto
{
    public long Id { get; set; }
    public string LastNDigits { get; set; } = null!;
    public DateTime TargetTime { get; set; }
    public VoteConfirmationStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? BrokerName { get; set; }
    public string? PhoneNumber { get; set; }
}
