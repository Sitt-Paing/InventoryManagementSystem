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
    path: 'suppliers',
    loadComponent: () => import('./suppliers/suppliers').then(m => m.Suppliers)
  },
  {
    path: 'supplier-accounts',
    redirectTo: 'suppliers',
    pathMatch: 'full'
  },
  {
    path: 'employee',
    loadComponent: () => import('../dashboard/dashboard').then(m => m.DashboardComponent)
  }
] as Routes;
