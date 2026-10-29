import { Routes } from "@angular/router";

export default [
  {
    path: '',
    redirectTo: 'stock-balance',
    pathMatch: 'full'
  },
  {
    path: 'stock-balance',
    loadComponent: () => import('./stock-balance/stock-balance').then(m => m.StockBalance)
  }
] as Routes;
