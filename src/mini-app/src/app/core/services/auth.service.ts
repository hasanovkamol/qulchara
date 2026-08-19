import { Injectable, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { Observable, tap } from 'rxjs';
import { AuthResponse, UserRole } from '../models/user.model';
import { TelegramService } from './telegram.service';

@Injectable({
  providedIn: 'root'
})
export class AuthService
{
  private apiUrl = 'http://localhost:5000/api/auth';

  public currentRole = signal<string | null>(null);
  public isAuthenticated = signal<boolean>(false);

  constructor(
    private http: HttpClient,
    private router: Router,
    private telegramService: TelegramService
  )
  {
    this.checkInitialAuth();
  }

  private checkInitialAuth()
  {
    const token = localStorage.getItem('token');
    const role = localStorage.getItem('role');
    if (token && role)
    {
      this.isAuthenticated.set(true);
      this.currentRole.set(role);
    }
  }

  loginWithTelegram(): Observable<AuthResponse>
  {
    const initData = this.telegramService.getInitData();
    return this.http.post<AuthResponse>(`${this.apiUrl}/login`, { initData }).pipe(
      tap(res =>
      {
        localStorage.setItem('token', res.token);
        localStorage.setItem('role', res.role);
        this.isAuthenticated.set(true);
        this.currentRole.set(res.role);
      })
    );
  }

  logout()
  {
    localStorage.removeItem('token');
    localStorage.removeItem('role');
    this.isAuthenticated.set(false);
    this.currentRole.set(null);
    this.router.navigate(['/']);
  }
}
