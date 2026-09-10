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
  },
  {
    path: 'warehouses',
    loadComponent: () => import('./warehouses/warehouses').then(m => m.Warehouses)
  },
  {
    path: 'warehouse-locations',
    loadComponent: () => import('./warehouse-locations/warehouse-locations').then(m => m.WarehouseLocations)
  }
] as Routes;
