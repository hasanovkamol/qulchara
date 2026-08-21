namespace OpenBudget.Domain.Entities;

public class BotCommand
{
    public int Id { get; set; }
    public string CommandText { get; set; } = null!;
    public string Description { get; set; } = null!;
    public string AllowedRoles { get; set; } = "All";
    public bool IsActive { get; set; } = true;
}
