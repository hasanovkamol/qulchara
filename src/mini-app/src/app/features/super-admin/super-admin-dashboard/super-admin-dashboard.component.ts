import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ApiService } from '../../../core/services/api.service';
import { GlobalStats } from '../../../core/models/stats.model';
import { User } from '../../../core/models/user.model';
import { FormsModule } from '@angular/forms';

@Component({
  selector: 'app-super-admin-dashboard',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="p-4 pb-20 max-w-lg mx-auto">
      <h1 class="text-2xl font-bold text-gray-800 mb-6">Super Admin Panel</h1>
      
      <!-- Stats -->
      <div class="grid grid-cols-2 gap-3 mb-6">
        <div class="bg-white p-4 rounded-xl shadow-sm border border-gray-100">
          <div class="text-sm text-gray-500 mb-1">Jami Ovozlar</div>
          <div class="text-2xl font-bold text-gray-800">{{ stats()?.totalVotes || 0 }}</div>
        </div>
        <div class="bg-blue-50 p-4 rounded-xl shadow-sm border border-blue-100">
          <div class="text-sm text-blue-600 mb-1">Brokerlar</div>
          <div class="text-2xl font-bold text-blue-700">{{ stats()?.totalBrokers || 0 }}</div>
        </div>
      </div>

      <!-- Users List -->
      <div class="space-y-3">
        <h2 class="text-lg font-semibold text-gray-800 mb-2">Foydalanuvchilar Boshqaruvi</h2>
        
        <div *ngIf="loading()" class="flex justify-center py-4">
          <div class="animate-spin rounded-full h-6 w-6 border-b-2 border-blue-600"></div>
        </div>

        <div *ngFor="let user of users()" class="bg-white p-4 rounded-xl shadow-sm border border-gray-100 flex flex-col gap-2">
          <div class="flex items-center justify-between">
            <div>
              <div class="font-medium text-gray-900">{{ user.fullName || user.username || 'Ismsiz' }}</div>
              <div class="text-xs text-gray-500">ID: {{ user.id }} | Role: {{ user.role }}</div>
            </div>
          </div>
          <div class="flex gap-2" *ngIf="user.role !== 'SuperAdmin'">
            <select #roleSelect class="bg-gray-50 border border-gray-300 text-gray-900 text-sm rounded-lg focus:ring-blue-500 focus:border-blue-500 block w-full p-2.5">
              <option value="Broker" [selected]="user.role === 'Broker'">Broker</option>
              <option value="Admin" [selected]="user.role === 'Admin'">Admin</option>
            </select>
            <button (click)="assignRole(user.id, roleSelect.value)" class="text-white bg-blue-600 hover:bg-blue-700 focus:ring-4 focus:ring-blue-300 font-medium rounded-lg text-sm px-4 py-2">
              Saqlash
            </button>
          </div>
        </div>
      </div>
    </div>
  `
})
export class SuperAdminDashboardComponent implements OnInit {
  apiService = inject(ApiService);
  
  stats = signal<GlobalStats | null>(null);
  users = signal<User[]>([]);
  loading = signal<boolean>(false);

  ngOnInit() {
    this.loadData();
  }

  loadData() {
    this.apiService.getGlobalStats().subscribe(res => this.stats.set(res));
    this.loadUsers();
  }

  loadUsers() {
    this.loading.set(true);
    this.apiService.getUsers().subscribe({
      next: (res) => {
        this.users.set(res);
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });
  }

  assignRole(userId: number, role: string) {
    this.apiService.assignRole(userId, role).subscribe({
      next: () => {
        alert('Rol saqlandi!');
        this.loadUsers();
      },
      error: (err) => {
        alert(err.error?.message || 'Xatolik yuz berdi');
      }
    });
  }
}
