using System;
using System.Collections.Generic;
using OpenBudget.Domain.Enums;

namespace OpenBudget.Domain.Entities;

public class User
{
    public int Id { get; set; }
    public long TelegramId { get; set; }
    public string? Username { get; set; }
    public string? FullName { get; set; }
    public UserRole Role { get; set; } = UserRole.Broker;
    public BotState BotState { get; set; } = BotState.Default;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public ICollection<Vote> CollectedVotes { get; set; } = new List<Vote>();
    public ICollection<Vote> ConfirmedVotes { get; set; } = new List<Vote>();
}
