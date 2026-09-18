import { Component, OnInit } from '@angular/core';
import { CommonModule, DatePipe, DecimalPipe } from '@angular/common';
import { RouterModule } from '@angular/router';
import { SharedService } from '../../core/services/shared.service';
import { StockTransactionService } from '../../core/services/stock-transaction.service';
import { ProductModel } from '../../core/models/product.model';
import { StockTransactionModel } from '../../core/models/stock-transaction.model';

import { CardModule } from 'primeng/card';
import { ButtonModule } from 'primeng/button';
import { TableModule } from 'primeng/table';
import { TagModule } from 'primeng/tag';
import { ProgressBarModule } from 'primeng/progressbar';
import { ProductService } from '../../core/services/master/product.service';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    CardModule,
    ButtonModule,
    TableModule,
    TagModule,
    ProgressBarModule
  ],
  providers: [DatePipe, DecimalPipe],
  templateUrl: './dashboard.html',
  styleUrl: './dashboard.scss'
})
export class DashboardComponent implements OnInit {
  products: ProductModel[] = [];
  recentTransactions: StockTransactionModel[] = [];
  
  totalProductsCount: number = 0;
  lowStockCount: number = 0;
  outOfStockCount: number = 0;
  totalValuation: number = 0;

  constructor(
    public sharedService: SharedService,
    private productService: ProductService,
    private transactionService: StockTransactionService
  ) {}

  ngOnInit(): void {
    this.loadData();
  }

  loadData(): void {
    this.productService.get().subscribe({
      next: (res) => {
        this.products = res.data || [];
        this.totalProductsCount = this.products.length;
        this.lowStockCount = this.products.filter(p => (p.currentStock || 0) <= (p.reorderLevel || 0) && (p.currentStock || 0) > 0).length;
        this.outOfStockCount = this.products.filter(p => (p.currentStock || 0) <= 0).length;
        this.totalValuation = this.products.reduce((acc, p) => acc + ((p.sellingPrice || 0) * (p.currentStock || 0)), 0);
      },
      error: () => {}
    });

    this.transactionService.get().subscribe({
      next: (res) => {
        const txns = (res.data || []) as StockTransactionModel[];
        this.recentTransactions = txns.slice(0, 5);
      },
      error: () => {}
    });
  }

  getTxnSeverity(type: string): 'success' | 'danger' | 'warn' {
    switch (type) {
      case 'IN': return 'success';
      case 'OUT': return 'danger';
      default: return 'warn';
    }
  }
}
