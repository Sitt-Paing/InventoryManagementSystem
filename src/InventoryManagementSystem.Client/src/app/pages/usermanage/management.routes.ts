import { Routes } from '@angular/router';

export default [
  {
    path: 'admin',
    loadComponent: () => import('../dashboard/dashboard').then(m => m.DashboardComponent)
  },
  {
    path: 'company',
    loadComponent: () => import('./company/company').then(m => m.CompanyComponent)
  },
  {
    path: 'employee',
    loadComponent: () => import('../dashboard/dashboard').then(m => m.DashboardComponent)
  }
] as Routes;
