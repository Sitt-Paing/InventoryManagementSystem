import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { SharedService } from '../../core/services/shared.service';
import { ProductService } from '../../core/services/product.service';
import { StockTransactionService } from '../../core/services/stock-transaction.service';
import { ProductModel } from '../../core/models/product.model';
import { StockTransactionModel } from '../../core/models/stock-transaction.model';

import { CardModule } from 'primeng/card';
import { ButtonModule } from 'primeng/button';
import { TableModule } from 'primeng/table';
import { TagModule } from 'primeng/tag';
import { ProgressBarModule } from 'primeng/progressbar';

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
    this.productService.getByCB().subscribe(res => {
      this.products = res.data;
      this.totalProductsCount = this.products.length;
      this.lowStockCount = this.products.filter(p => p.status === 'Low Stock').length;
      this.outOfStockCount = this.products.filter(p => p.status === 'Out of Stock').length;
      this.totalValuation = this.products.reduce((acc, p) => acc + (p.unitPrice * p.quantityInStock), 0);
    });

    this.transactionService.getByCB().subscribe(res => {
      this.recentTransactions = res.data.slice(0, 5);
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
