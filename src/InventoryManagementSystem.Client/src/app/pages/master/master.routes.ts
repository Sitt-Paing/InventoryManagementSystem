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
    redirectTo: '/user-manage/suppliers',
    pathMatch: 'full'
  },
  {
    path: 'departments',
    loadComponent: () => import('./departments/departments').then(m => m.Departments)
  },
  {
    path: 'warehouses',
    loadComponent: () => import('./warehouses/warehouses').then(m => m.Warehouses)
  },
  {
    path: 'warehouse-locations',
    loadComponent: () => import('./warehouse-locations/warehouse-locations').then(m => m.WarehouseLocations)
  },
  {
    path: 'unit-of-measures',
    loadComponent: () => import('./unit-of-measures/unit-of-measures').then(m => m.UnitOfMeasures)
  }
] as Routes;

