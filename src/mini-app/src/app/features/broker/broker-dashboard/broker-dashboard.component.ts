import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ApiService } from '../../../core/services/api.service';
import { BrokerStats } from '../../../core/models/stats.model';
import { PaginatedResult, Vote, VoteStatus } from '../../../core/models/vote.model';

@Component({
  selector: 'app-broker-dashboard',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="p-4 pb-20 max-w-lg mx-auto">
      <h1 class="text-2xl font-bold text-gray-800 mb-6">Mening ovozlarim</h1>
      
      <!-- Stats -->
      <div class="grid grid-cols-2 gap-3 mb-6">
        <div class="bg-white p-4 rounded-xl shadow-sm border border-gray-100">
          <div class="text-sm text-gray-500 mb-1">Jami</div>
          <div class="text-2xl font-bold text-gray-800">{{ stats()?.totalVotes || 0 }}</div>
        </div>
        <div class="bg-green-50 p-4 rounded-xl shadow-sm border border-green-100">
          <div class="text-sm text-green-600 mb-1">Tasdiqlangan</div>
          <div class="text-2xl font-bold text-green-700">{{ stats()?.confirmedVotes || 0 }}</div>
        </div>
        <div class="bg-orange-50 p-4 rounded-xl shadow-sm border border-orange-100">
          <div class="text-sm text-orange-600 mb-1">Kutilmoqda</div>
          <div class="text-2xl font-bold text-orange-700">{{ stats()?.pendingVotes || 0 }}</div>
        </div>
        <div class="bg-red-50 p-4 rounded-xl shadow-sm border border-red-100">
          <div class="text-sm text-red-600 mb-1">Rad etilgan</div>
          <div class="text-2xl font-bold text-red-700">{{ stats()?.rejectedVotes || 0 }}</div>
        </div>
      </div>

      <!-- Votes List -->
      <div class="space-y-3">
        <h2 class="text-lg font-semibold text-gray-800 mb-2">Ovozlar tarixi</h2>
        
        <div *ngIf="loading()" class="flex justify-center py-4">
          <div class="animate-spin rounded-full h-6 w-6 border-b-2 border-blue-600"></div>
        </div>

        <div *ngFor="let vote of votes()?.items" class="bg-white p-4 rounded-xl shadow-sm border border-gray-100 flex items-center justify-between">
          <div>
            <div class="font-medium text-gray-900">{{ vote.phoneNumber }}</div>
            <div class="text-xs text-gray-500">{{ vote.votedAt | date:'HH:mm dd.MM.yyyy' }}</div>
          </div>
          <div>
            <span class="px-2 py-1 text-xs font-medium rounded-full" [ngClass]="getStatusClass(vote.status)">
              {{ getStatusText(vote.status) }}
            </span>
          </div>
        </div>

        <div *ngIf="!loading() && votes()?.items?.length === 0" class="text-center text-gray-500 py-4">
          Hozircha ovozlar yo'q
        </div>

        <!-- Pagination Controls -->
        <div class="flex justify-between items-center mt-4" *ngIf="votes() && votes()!.totalCount > 0">
          <button (click)="changePage(-1)" [disabled]="page() === 1" class="px-4 py-2 bg-white border border-gray-200 rounded-lg text-sm disabled:opacity-50">
            Oldingi
          </button>
          <span class="text-sm text-gray-600">
            {{ page() }} / {{ Math.ceil(votes()!.totalCount / pageSize) }}
          </span>
          <button (click)="changePage(1)" [disabled]="page() >= Math.ceil(votes()!.totalCount / pageSize)" class="px-4 py-2 bg-white border border-gray-200 rounded-lg text-sm disabled:opacity-50">
            Keyingi
          </button>
        </div>
      </div>
    </div>
  `
})
export class BrokerDashboardComponent implements OnInit {
  apiService = inject(ApiService);
  
  stats = signal<BrokerStats | null>(null);
  votes = signal<PaginatedResult<Vote> | null>(null);
  loading = signal<boolean>(false);
  page = signal<number>(1);
  pageSize = 10;
  Math = Math;

  ngOnInit() {
    this.loadData();
  }

  loadData() {
    this.apiService.getBrokerStats().subscribe(res => this.stats.set(res));
    this.loadVotes();
  }

  loadVotes() {
    this.loading.set(true);
    this.apiService.getBrokerVotes(this.page(), this.pageSize).subscribe({
      next: (res) => {
        this.votes.set(res);
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });
  }

  changePage(delta: number) {
    const newPage = this.page() + delta;
    this.page.set(newPage);
    this.loadVotes();
  }

  getStatusClass(status: VoteStatus): string {
    switch(status) {
      case VoteStatus.Pending: return 'bg-orange-100 text-orange-700';
      case VoteStatus.Confirmed: return 'bg-green-100 text-green-700';
      case VoteStatus.Rejected: return 'bg-red-100 text-red-700';
      default: return 'bg-gray-100 text-gray-700';
    }
  }

  getStatusText(status: VoteStatus): string {
    switch(status) {
      case VoteStatus.Pending: return 'Kutilmoqda';
      case VoteStatus.Confirmed: return 'Tasdiqlandi';
      case VoteStatus.Rejected: return 'Rad etildi';
      default: return 'Noma\'lum';
    }
  }
}
