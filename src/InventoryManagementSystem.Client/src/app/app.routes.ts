import { Routes } from '@angular/router';
import { AppLayout } from './layout/app-layout';

export const routes: Routes = [
  {
    path: '',
    component: AppLayout,
    children: [
      { path: '', redirectTo: 'dashboard', pathMatch: 'full' },
      {
        path: 'dashboard',
        loadComponent: () => import('./pages/dashboard/dashboard').then(m => m.DashboardComponent)
      },
      {
        path: 'products',
        loadComponent: () => import('./pages/products/products').then(m => m.ProductsComponent)
      },
      {
        path: 'categories',
        loadComponent: () => import('./pages/categories/categories').then(m => m.CategoriesComponent)
      },
      {
        path: 'stock-transactions',
        loadComponent: () => import('./pages/stock-transactions/stock-transactions').then(m => m.StockTransactionsComponent)
      }
    ]
  },
  { path: '**', redirectTo: '' }
];
