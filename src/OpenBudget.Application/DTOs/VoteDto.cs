using System;
using OpenBudget.Domain.Enums;

namespace OpenBudget.Application.DTOs;

public class VoteDto
{
    public long Id { get; set; }
    public string PhoneNumber { get; set; } = null!;
    public VoteStatus Status { get; set; }
    public DateTime VotedAt { get; set; }
}
