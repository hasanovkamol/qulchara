export enum VoteStatus {
  Pending = 0,
  Confirmed = 1,
  Rejected = 2
}

export interface Vote {
  id: number;
  phoneNumber: string;
  status: VoteStatus;
  votedAt: string;
}

export interface PaginatedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
}
