import { Injectable } from '@angular/core';
import { Observable, of } from 'rxjs';
import { StockTransactionModel } from '../models/stock-transaction.model';

@Injectable({
  providedIn: 'root'
})
export class StockTransactionService {
  private transactions: StockTransactionModel[] = [
    {
      transactionId: 1001,
      transactionCode: 'TXN-20260812-01',
      productId: 101,
      productName: 'Wireless Ergonomic Keyboard',
      productCode: 'PRD-00101',
      transactionType: 'IN',
      quantity: 50,
      unitPrice: 55.00,
      totalAmount: 2750.00,
      referenceNo: 'PO-99241',
      notes: 'Stock intake from main vendor shipment',
      transactionDate: '2026-08-12 10:30',
      performedBy: 'Sitt Paing (Manager)',
      companyId: 'COMP001',
      branchId: 1
    },
    {
      transactionId: 1002,
      transactionCode: 'TXN-20260811-04',
      productId: 102,
      productName: 'Logitech Master MX 3S Mouse',
      productCode: 'PRD-00102',
      transactionType: 'OUT',
      quantity: 12,
      unitPrice: 99.00,
      totalAmount: 1188.00,
      referenceNo: 'SO-10842',
      notes: 'Dispatched to Corporate Client Order #10842',
      transactionDate: '2026-08-11 14:15',
      performedBy: 'Aung Kyaw (Logistics)',
      companyId: 'COMP001',
      branchId: 1
    },
    {
      transactionId: 1003,
      transactionCode: 'TXN-20260810-02',
      productId: 104,
      productName: 'Ergonomic Mesh Executive Chair',
      productCode: 'PRD-00104',
      transactionType: 'OUT',
      quantity: 15,
      unitPrice: 249.50,
      totalAmount: 3742.50,
      referenceNo: 'SO-10839',
      notes: 'Out of stock fulfilled for office setup project',
      transactionDate: '2026-08-10 16:45',
      performedBy: 'Sitt Paing (Manager)',
      companyId: 'COMP001',
      branchId: 1
    },
    {
      transactionId: 1004,
      transactionCode: 'TXN-20260809-01',
      productId: 106,
      productName: 'Thermal Receipt Paper Rolls (50-Pack)',
      productCode: 'PRD-00106',
      transactionType: 'ADJUSTMENT',
      quantity: -2,
      unitPrice: 18.00,
      totalAmount: -36.00,
      referenceNo: 'ADJ-0042',
      notes: 'Damaged paper rolls written off after audit',
      transactionDate: '2026-08-09 09:00',
      performedBy: 'Inventory Auditor',
      companyId: 'COMP001',
      branchId: 1
    }
  ];

  getByCB(companyId?: string, branchId?: number): Observable<{ data: StockTransactionModel[] }> {
    return of({ data: this.transactions });
  }

  save(txn: Partial<StockTransactionModel>): Observable<StockTransactionModel> {
    const newId = Math.max(...this.transactions.map(t => t.transactionId), 1000) + 1;
    const newTxn: StockTransactionModel = {
      transactionId: newId,
      transactionCode: `TXN-${new Date().toISOString().slice(0, 10).replace(/-/g, '')}-${newId}`,
      productId: txn.productId || 101,
      productName: txn.productName || 'Product',
      productCode: txn.productCode || 'PRD-000',
      transactionType: txn.transactionType || 'IN',
      quantity: txn.quantity || 1,
      unitPrice: txn.unitPrice || 0,
      totalAmount: (txn.quantity || 1) * (txn.unitPrice || 0),
      referenceNo: txn.referenceNo || 'REF-N/A',
      notes: txn.notes || '',
      transactionDate: new Date().toLocaleString(),
      performedBy: txn.performedBy || 'Admin User',
      companyId: txn.companyId || 'COMP001',
      branchId: txn.branchId || 1
    };
    this.transactions.unshift(newTxn);
    return of(newTxn);
  }
}
