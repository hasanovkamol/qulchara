namespace OpenBudget.Domain.Enums;

public enum BotState
{
    Default = 0,
    WaitingForVote = 1,
    WaitingForConfirmation = 2,
    WaitingForAdminId = 3,
    WaitingForBrokerRequestInfo = 4,
    WaitingForBrokerIdentifier = 5,
    WaitingForQrUrl = 6
}
