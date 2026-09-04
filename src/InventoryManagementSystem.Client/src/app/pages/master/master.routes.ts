import { Routes } from '@angular/router';

export default [
  {
    path: 'categories',
    loadComponent: () => import('./categories/categories').then(m => m.Categories)
  },
  {
    path: 'products',
    loadComponent: () => import('./products/products').then(m => m.Products)
  },
  {
    path: 'suppliers',
    loadComponent: () => import('./suppliers/suppliers').then(m => m.Suppliers)
  }
] as Routes;
