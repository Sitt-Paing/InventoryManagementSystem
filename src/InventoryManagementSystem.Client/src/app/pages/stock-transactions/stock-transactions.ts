import { Component, OnInit, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { StockTransactionModel } from '../../core/models/stock-transaction.model';
import { ProductModel } from '../../core/models/product.model';
import { StockTransactionService } from '../../core/services/stock-transaction.service';
import { ProductService } from '../../core/services/product.service';
import { ExportService } from '../../core/services/export.service';
import { SharedService } from '../../core/services/shared.service';

import { Table, TableModule } from 'primeng/table';
import { ButtonModule } from 'primeng/button';
import { SelectModule } from 'primeng/select';
import { InputTextModule } from 'primeng/inputtext';
import { TextareaModule } from 'primeng/textarea';
import { IconFieldModule } from 'primeng/iconfield';
import { InputIconModule } from 'primeng/inputicon';
import { DialogModule } from 'primeng/dialog';
import { TagModule } from 'primeng/tag';
import { FieldsetModule } from 'primeng/fieldset';
import { DatePickerModule } from 'primeng/datepicker';

@Component({
  selector: 'app-stock-transactions',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    ReactiveFormsModule,
    TableModule,
    ButtonModule,
    SelectModule,
    InputTextModule,
    TextareaModule,
    IconFieldModule,
    InputIconModule,
    DialogModule,
    TagModule,
    FieldsetModule,
    DatePickerModule
  ],
  templateUrl: './stock-transactions.html',
  styleUrl: './stock-transactions.scss'
})
export class StockTransactionsComponent implements OnInit {
  @ViewChild('tblTxn') tblTxn!: Table;

  transactions: StockTransactionModel[] = [];
  filteredTransactions: StockTransactionModel[] = [];
  products: ProductModel[] = [];

  // Filter Toolbar state (ES_HR style)
  selectedType: string | null = null;
  filterDate: Date = new Date();
  isLoading: boolean = false;

  typeOptions = [
    { label: 'All Movement Types', value: null },
    { label: 'Stock Intake (IN)', value: 'IN' },
    { label: 'Sales Dispatch (OUT)', value: 'OUT' },
    { label: 'Adjustment', value: 'ADJUSTMENT' }
  ];

  // Modals state
  detailModalVisible: boolean = false;
  formModalVisible: boolean = false;
  selectedTxn: StockTransactionModel | null = null;
  dialogHeader: string = '';

  txnForm!: FormGroup;

  constructor(
    private transactionService: StockTransactionService,
    private productService: ProductService,
    private exportService: ExportService,
    public sharedService: SharedService,
    private fb: FormBuilder
  ) {
    this.initForm();
  }

  ngOnInit(): void {
    this.loadProducts();
    this.loadTransactions();
  }

  initForm(): void {
    this.txnForm = this.fb.group({
      productId: [null, Validators.required],
      transactionType: ['IN', Validators.required],
      quantity: [1, [Validators.required, Validators.min(1)]],
      unitPrice: [0, [Validators.required, Validators.min(0)]],
      referenceNo: [''],
      notes: ['']
    });
  }

  loadProducts(): void {
    this.productService.getByCB().subscribe(res => {
      this.products = res.data;
    });
  }

  loadTransactions(): void {
    this.isLoading = true;
    this.transactionService.getByCB().subscribe(res => {
      this.transactions = res.data;
      this.filteredTransactions = [...this.transactions];
      this.isLoading = false;
    });
  }

  // ES_HR Pattern: Filter view
  view(): void {
    this.filteredTransactions = this.transactions.filter(t => {
      return !this.selectedType || t.transactionType === this.selectedType;
    });
  }

  resetState(): void {
    this.selectedType = null;
    this.filterDate = new Date();
    this.filteredTransactions = [...this.transactions];
  }

  openDetail(txn: StockTransactionModel): void {
    this.selectedTxn = txn;
    this.dialogHeader = `Stock Transaction: ${txn.transactionCode} (${txn.transactionType})`;
    this.detailModalVisible = true;
  }

  openCreate(): void {
    this.dialogHeader = 'Record Stock Movement / Transaction';
    this.txnForm.reset({
      transactionType: 'IN',
      quantity: 1,
      unitPrice: 0,
      referenceNo: `REF-${Math.floor(1000 + Math.random() * 9000)}`
    });
    this.formModalVisible = true;
  }

  onProductSelect(productId: number): void {
    const prd = this.products.find(p => p.productId === productId);
    if (prd) {
      this.txnForm.patchValue({ unitPrice: prd.unitPrice });
    }
  }

  saveTransaction(): void {
    if (this.txnForm.invalid) {
      this.txnForm.markAllAsTouched();
      return;
    }
    const val = this.txnForm.value;
    const prd = this.products.find(p => p.productId === val.productId);
    val.productName = prd ? prd.name : 'Unknown Product';
    val.productCode = prd ? prd.productCode : 'PRD-000';

    this.transactionService.save(val).subscribe(() => {
      this.formModalVisible = false;
      this.loadTransactions();
    });
  }

  excel(): void {
    this.exportService.excel('Stock_Transactions', this.tblTxn);
  }

  getTxnSeverity(type: string): 'success' | 'danger' | 'warn' {
    switch (type) {
      case 'IN': return 'success';
      case 'OUT': return 'danger';
      default: return 'warn';
    }
  }
}
