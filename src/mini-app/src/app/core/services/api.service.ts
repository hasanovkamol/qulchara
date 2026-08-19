import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { PaginatedResult, Vote } from '../models/vote.model';
import { BrokerStats, GlobalStats } from '../models/stats.model';
import { User } from '../models/user.model';
import { ConfigService } from './config.service';

@Injectable({
  providedIn: 'root'
})
export class ApiService {
  constructor(private http: HttpClient, private config: ConfigService) {}

  private get baseUrl(): string {
    return this.config.apiUrl;
  }

  // Vote Endpoints
  getBrokerVotes(page: number = 1, pageSize: number = 10): Observable<PaginatedResult<Vote>> {
    return this.http.get<PaginatedResult<Vote>>(`${this.baseUrl}/vote/broker?page=${page}&pageSize=${pageSize}`);
  }

  confirmVote(last3Digits: string, targetUtcTime: string, timeWindowHours: number = 1): Observable<{message: string}> {
    return this.http.post<{message: string}>(`${this.baseUrl}/vote/confirm`, {
      last3Digits, targetUtcTime, timeWindowHours
    });
  }

  // User Endpoints
  getMe(): Observable<User> {
    return this.http.get<User>(`${this.baseUrl}/user/me`);
  }

  getUsers(): Observable<User[]> {
    return this.http.get<User[]>(`${this.baseUrl}/user/list`);
  }

  assignRole(targetUserId: number, newRole: string): Observable<{message: string}> {
    return this.http.post<{message: string}>(`${this.baseUrl}/user/assign-role`, {
      targetUserId, newRole
    });
  }

  // Stats Endpoints
  getBrokerStats(brokerId?: number): Observable<BrokerStats> {
    const url = brokerId ? `${this.baseUrl}/stats/broker?brokerId=${brokerId}` : `${this.baseUrl}/stats/broker`;
    return this.http.get<BrokerStats>(url);
  }

  getGlobalStats(): Observable<GlobalStats> {
    return this.http.get<GlobalStats>(`${this.baseUrl}/stats/global`);
  }
}

