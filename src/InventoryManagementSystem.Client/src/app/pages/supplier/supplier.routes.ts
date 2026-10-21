import { Routes } from '@angular/router';

export default [
  {
    path: '',
    redirectTo: 'dashboard',
    pathMatch: 'full'
  },
  {
    path: 'dashboard',
    loadComponent: () => import('./dashboard/dashboard').then(m => m.SupplierDashboard)
  },
  {
    path: 'purchase-orders',
    loadComponent: () => import('./documents/supplier-documents').then(m => m.SupplierDocuments),
    data: { kind: 'purchase-orders', titleKey: 'NAV.PURCHASE_ORDERS' }
  },
  {
    path: 'goods-receipts',
    loadComponent: () => import('./documents/supplier-documents').then(m => m.SupplierDocuments),
    data: { kind: 'goods-receipts', titleKey: 'NAV.GOOD_RECEIPTS' }
  },
  {
    path: 'purchase-returns',
    loadComponent: () => import('./documents/supplier-documents').then(m => m.SupplierDocuments),
    data: { kind: 'purchase-returns', titleKey: 'NAV.PURCHASE_RETURN' }
  }
] as Routes;
