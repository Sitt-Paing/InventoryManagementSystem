import { Routes } from '@angular/router';

export default [
  {
    path: 'stock-transactions',
    loadComponent : () => import('./stock-transactions/stock-transactions').then(m => m.StockTransactionsComponent),
  },
  {
    path: 'purchase-orders',
    loadComponent: () => import('./purchase-orders/purchase-orders').then(m => m.PurchaseOrdersComponent),
  }
] as Routes;
