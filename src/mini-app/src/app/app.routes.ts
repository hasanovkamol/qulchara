import { Routes } from '@angular/router';
import { authGuard, roleGuard } from './core/guards/auth.guard';

export const routes: Routes = [
  {
    path: '',
    loadComponent: () => import('./features/home/home.component').then(m => m.HomeComponent)
  },
  {
    path: 'broker',
    loadComponent: () => import('./features/broker/broker-dashboard/broker-dashboard.component').then(m => m.BrokerDashboardComponent),
    canActivate: [authGuard, roleGuard],
    data: { roles: ['Broker', 'Admin', 'SuperAdmin'] }
  },
  {
    path: 'admin',
    loadComponent: () => import('./features/admin/admin-dashboard/admin-dashboard.component').then(m => m.AdminDashboardComponent),
    canActivate: [authGuard, roleGuard],
    data: { roles: ['Admin', 'SuperAdmin'] }
  },
  {
    path: 'superadmin',
    loadComponent: () => import('./features/super-admin/super-admin-dashboard/super-admin-dashboard.component').then(m => m.SuperAdminDashboardComponent),
    canActivate: [authGuard, roleGuard],
    data: { roles: ['SuperAdmin'] }
  },
  { path: '**', redirectTo: '' }
];
