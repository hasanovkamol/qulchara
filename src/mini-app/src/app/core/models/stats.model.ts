export interface BrokerStats {
  totalVotes: number;
  confirmedVotes: number;
  pendingVotes: number;
  rejectedVotes: number;
}

export interface GlobalStats {
  totalBrokers: number;
  totalAdmins: number;
  totalVotes: number;
  confirmedVotes: number;
  pendingVotes: number;
  rejectedVotes: number;
}
