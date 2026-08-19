namespace OpenBudget.Application.DTOs;

public class GlobalStatsDto
{
    public int TotalBrokers { get; set; }
    public int TotalAdmins { get; set; }
    public int TotalVotes { get; set; }
    public int ConfirmedVotes { get; set; }
    public int PendingVotes { get; set; }
    public int RejectedVotes { get; set; }
}
