export enum UserRole {
  Broker = 0,
  Admin = 1,
  SuperAdmin = 2
}

export interface User {
  id: number;
  telegramId: number;
  username?: string;
  fullName?: string;
  role: string;
}

export interface AuthResponse {
  token: string;
  role: string;
}
