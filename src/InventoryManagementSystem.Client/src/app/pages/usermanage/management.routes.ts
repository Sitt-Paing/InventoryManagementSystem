import { Routes } from '@angular/router';

export default [
  {
    path: 'user-manage',
    children: [
      {
        path: 'admin',
        loadComponent: () => import('../dashboard/dashboard').then(m => m.DashboardComponent)
      },
      {
        path: 'company',
        loadComponent: () => import('../dashboard/dashboard').then(m => m.DashboardComponent)
      },
      {
        path: 'employee',
        loadComponent: () => import('../dashboard/dashboard').then(m => m.DashboardComponent)
      }
    ]
  }
] as Routes;
