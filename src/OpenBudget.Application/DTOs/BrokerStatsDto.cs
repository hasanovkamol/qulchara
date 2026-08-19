namespace OpenBudget.Application.DTOs;

public class BrokerStatsDto
{
    public int TotalVotes { get; set; }
    public int ConfirmedVotes { get; set; }
    public int PendingVotes { get; set; }
    public int RejectedVotes { get; set; }
}
