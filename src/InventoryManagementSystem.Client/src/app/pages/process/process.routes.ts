import { Routes } from '@angular/router';

export default [
  {
    path: 'stock-transactions',
    loadComponent: () => import('../stock-transactions/stock-transactions').then(m => m.StockTransactionsComponent)
  }
] as Routes;
