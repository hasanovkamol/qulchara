import { Component, inject, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';
import { UserRole } from '../../core/models/user.model';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-home',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="min-h-screen flex items-center justify-center bg-gray-50 p-4">
      <div class="bg-white rounded-2xl shadow-xl p-8 max-w-sm w-full text-center">
        <h1 class="text-2xl font-bold text-gray-800 mb-4">OpenBudget</h1>
        <p class="text-gray-600 mb-8">Telegram orqali avtorizatsiya qilinmoqda...</p>
        
        <div *ngIf="error" class="bg-red-50 text-red-600 p-3 rounded-lg mb-4 text-sm">
          {{ error }}
        </div>

        <div *ngIf="!error" class="animate-spin rounded-full h-10 w-10 border-b-2 border-blue-600 mx-auto"></div>
      </div>
    </div>
  `
})
export class HomeComponent implements OnInit {
  authService = inject(AuthService);
  router = inject(Router);
  error: string | null = null;

  ngOnInit() {
    this.authService.loginWithTelegram().subscribe({
      next: (res) => {
        if (res.role === 'Broker') {
          this.router.navigate(['/broker']);
        } else if (res.role === 'Admin') {
          this.router.navigate(['/admin']);
        } else if (res.role === 'SuperAdmin') {
          this.router.navigate(['/superadmin']);
        } else {
          this.error = "Noma'lum rol.";
        }
      },
      error: (err) => {
        console.error('Login error', err);
        this.error = "Avtorizatsiyadan o'tishda xatolik. Faqat Telegram ichidan kiring.";
      }
    });
  }
}
