import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ApiService } from '../../../core/services/api.service';
import { User } from '../../../core/models/user.model';

@Component({
  selector: 'app-admin-dashboard',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="p-4 pb-20 max-w-lg mx-auto">
      <h1 class="text-2xl font-bold text-gray-800 mb-6">Admin Panel</h1>
      
      <!-- Brokers List -->
      <div class="space-y-3">
        <h2 class="text-lg font-semibold text-gray-800 mb-2">Brokerlar Ro'yxati</h2>
        
        <div *ngIf="loading()" class="flex justify-center py-4">
          <div class="animate-spin rounded-full h-6 w-6 border-b-2 border-blue-600"></div>
        </div>

        <div *ngFor="let user of brokers()" class="bg-white p-4 rounded-xl shadow-sm border border-gray-100 flex items-center justify-between">
          <div>
            <div class="font-medium text-gray-900">{{ user.fullName || user.username || 'Ismsiz' }}</div>
            <div class="text-xs text-gray-500">ID: {{ user.id }} | {{ user.role }}</div>
          </div>
          <div>
            <button class="text-blue-600 text-sm font-medium hover:underline">Statistika</button>
          </div>
        </div>

        <div *ngIf="!loading() && brokers()?.length === 0" class="text-center text-gray-500 py-4">
          Hozircha brokerlar yo'q
        </div>
      </div>
    </div>
  `
})
export class AdminDashboardComponent implements OnInit {
  apiService = inject(ApiService);
  
  brokers = signal<User[]>([]);
  loading = signal<boolean>(false);

  ngOnInit() {
    this.loadUsers();
  }

  loadUsers() {
    this.loading.set(true);
    this.apiService.getUsers().subscribe({
      next: (res) => {
        this.brokers.set(res.filter(u => u.role === 'Broker'));
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });
  }
}
