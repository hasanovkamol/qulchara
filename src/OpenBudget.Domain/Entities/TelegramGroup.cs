using System;

namespace OpenBudget.Domain.Entities;

public class TelegramGroup
{
    public int Id { get; set; }
    public long ChatId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Username { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastActiveAt { get; set; }
    public string? InitiativeCode { get; set; }
}
