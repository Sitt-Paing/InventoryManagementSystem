import { Routes } from '@angular/router';
import { authGuard } from './core/guards/auth.guard';

export const routes: Routes = [
  {
    path: 'auth',
    loadChildren: () => import('./pages/auth/auth.routes')
  },
  {
    path: '',
    loadComponent: () => import('./layout/app-layout').then((m) => m.AppLayout),
    children: [
      {
        path: '',
        redirectTo: 'dashboard',
        pathMatch: 'full'
      },
      {
        path: 'dashboard',
        loadComponent: () => import('./pages/dashboard/dashboard').then((m) => m.DashboardComponent),
        canActivate: [authGuard]
      },
      {
        path: 'master',
        loadChildren: () => import('./pages/master/master.routes').then(m => m.default),
        canActivate: [authGuard]
      },
      // Backward compatibility redirects
      { path: 'products', redirectTo: 'master/products', pathMatch: 'full' },
      { path: 'categories', redirectTo: 'master/categories', pathMatch: 'full' },
      { path: 'stock-transactions', redirectTo: 'process/stock-transactions', pathMatch: 'full' }
    ]
  },
  {
    path: '**',
    redirectTo: 'auth/login'
  }
];
