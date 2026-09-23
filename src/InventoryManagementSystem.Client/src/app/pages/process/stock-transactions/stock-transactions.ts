import { ChangeDetectorRef, Component, inject, OnInit, ViewChild } from '@angular/core';
import { ToastModule } from 'primeng/toast';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { SplitButtonModule } from 'primeng/splitbutton';
import { IconFieldModule } from 'primeng/iconfield';
import { InputIconModule } from 'primeng/inputicon';
import { Table, TableLazyLoadEvent, TableModule } from 'primeng/table';
import { ConfirmationService, MenuItem, MessageService } from 'primeng/api';
import { InputTextModule } from 'primeng/inputtext';
import { CommonModule, DatePipe } from '@angular/common';
import { debounceTime, distinctUntilChanged, Subject } from 'rxjs';
import { Tag } from 'primeng/tag';
import { DialogModule } from 'primeng/dialog';
import { FormBuilder, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';
import { ButtonModule } from 'primeng/button';
import { SelectModule } from 'primeng/select';
import { SelectButtonModule } from 'primeng/selectbutton';
import { DatePickerModule } from 'primeng/datepicker';
import { TextareaModule } from 'primeng/textarea';

import { StockTransactionModel } from '../../../core/models/process/stock-transaction.model';
import { ProductModel } from '../../../core/models/master/product.model';
import { WarehouseModel } from '../../../core/models/master/warehouse.model';
import { WarehouseLocationModel } from '../../../core/models/master/warehouse-location.model';

import { ProductService } from '../../../core/services/master/product.service';
import { WarehouseService } from '../../../core/services/master/warehouse.service';
import { WarehouseLocationService } from '../../../core/services/master/warehouse-location.service';
import { ExportService } from '../../../core/services/export.service';
import { StockTransactionService } from '../../../core/services/process/stock-transaction.service';

@Component({
  selector: 'app-stock-transactions',
  standalone: true,
  imports: [
    ToastModule,
    ReactiveFormsModule,
    FormsModule,
    CommonModule,
    ConfirmDialogModule,
    SplitButtonModule,
    IconFieldModule,
    InputIconModule,
    InputTextModule,
    TableModule,
    Tag,
    DialogModule,
    ButtonModule,
    SelectModule,
    SelectButtonModule,
    DatePickerModule,
    TextareaModule
  ],
  providers: [ConfirmationService, MessageService, DatePipe, ExportService],
  templateUrl: './stock-transactions.html',
  styleUrl: './stock-transactions.scss'
})
export class StockTransactionsComponent implements OnInit {
  @ViewChild(Table) tblStockTransactions!: Table;
  items: MenuItem[] = [];
  isLoading: boolean = false;
  stockTransactions: StockTransactionModel[] = [];
  selectedStockTransaction: StockTransactionModel | null = null;
  isEdit: boolean = false;
  modalVisible: boolean = false;
  detailModalVisible: boolean = false;
  isSubmitting: boolean = false;

  // Server-side search, sorting, and pagination
  totalRecords: number = 0;
  pageNumber: number = 1;
  pageSize: number = 20;
  sortField: string = 'createdOn';
  sortOrder: number = -1;
  searchKeyword: string = '';

  products: ProductModel[] = [];
  warehouses: WarehouseModel[] = [];
  allLocations: WarehouseLocationModel[] = [];
  filteredLocations: WarehouseLocationModel[] = [];
  filteredToLocations: WarehouseLocationModel[] = [];
  selectedProductForForm: ProductModel | null = null;
  warehouseStock: number | null = null;
  productWarehouseStocks: { warehouseId: number; warehouseName: string; quantity: number }[] = [];
  warehouseOptionsWithStock: any[] = [];

  // Filter toolbar state
  selectedType: string = '_';
  selectedWarehouseFilter: number | null = null;
  sDate: Date = new Date();
  eDate: Date = new Date();

  movementTypeOptions = [
    { label: 'All', value: '_' },
    { label: 'IN', value: 'IN' },
    { label: 'OUT', value: 'OUT' },
    { label: 'TRANSFER', value: 'TRANSFER' },
    { label: 'ADJUSTMENT', value: 'ADJUSTMENT' }
  ];

  formTypeOptions = [
    { label: 'Stock Intake (IN)', value: 'IN' },
    { label: 'Stock Issue / Dispatch (OUT)', value: 'OUT' },
    { label: 'Stock Transfer (TRANSFER)', value: 'TRANSFER' },
    { label: 'Stock Adjustment (ADJUSTMENT)', value: 'ADJUSTMENT' }
  ];

  private formBuilder = inject(FormBuilder);
  public stockTransactionForm = this.formBuilder.group({
    id: [0],
    productId: ['', Validators.required],
    warehouseId: [0, [Validators.required, Validators.min(1)]],
    warehouseLocationId: [0, [Validators.required, Validators.min(1)]],
    toWarehouseId: [null as number | null],
    toWarehouseLocationId: [null as number | null],
    transactionType: ['IN', Validators.required],
    quantity: [1, [Validators.required, Validators.min(0.01)]],
    transactionDate: [new Date(), Validators.required],
    referenceNo: [null as any],
    note: [''],
    createdOn: [null as any],
    createdBy: [null as any],
    updatedOn: [null as any],
    updatedBy: [null as any],
    deletedOn: [null as any],
    deletedBy: [null as any]
  });

  constructor(
    private stockTransactionService: StockTransactionService,
    private productService: ProductService,
    private warehouseService: WarehouseService,
    private warehouseLocationService: WarehouseLocationService,
    private confirmationService: ConfirmationService,
    private datePipe: DatePipe,
    private messageService: MessageService,
    private cdr: ChangeDetectorRef,
    private exportService: ExportService,
  ) {
    this.items = [
      {
        label: 'Edit',
        icon: 'pi pi-pencil',
        command: () => this.update()
      },
      {
        label: 'Delete',
        icon: 'pi pi-trash',
        command: () => this.delete()
      },
      {
        label: 'Excel',
        icon: 'pi pi-file-excel',
        command: () => this.excel()
      }
    ];
  }

  ngOnInit(): void {
    this.setupTransactionTypeListener();
    this.loadProducts();
    this.loadWarehouses();
    this.loadLocations();
    this.loadData();
  }

  onSearch(): void {
    this.pageNumber = 1;
    if (this.tblStockTransactions) {
      this.tblStockTransactions.first = 0;
    }
    this.loadData();
  }

  onLazyLoad(event: TableLazyLoadEvent): void {
    if (event.first !== undefined && event.rows) {
      this.pageNumber = Math.floor(event.first / event.rows) + 1;
      this.pageSize = event.rows;
    }
    if (event.sortField) {
      this.sortField = Array.isArray(event.sortField) ? event.sortField[0] : event.sortField;
      this.sortOrder = event.sortOrder ?? -1;
    }
    this.loadData();
  }

  setupTransactionTypeListener(): void {
    this.stockTransactionForm.get('transactionType')?.valueChanges.subscribe(type => {
      const toWhControl = this.stockTransactionForm.get('toWarehouseId');
      const toLocControl = this.stockTransactionForm.get('toWarehouseLocationId');
      if (type === 'TRANSFER') {
        toWhControl?.setValidators([Validators.required, Validators.min(1)]);
        toLocControl?.setValidators([Validators.required, Validators.min(1)]);
      } else {
        toWhControl?.clearValidators();
        toLocControl?.clearValidators();
        toWhControl?.setValue(null);
        toLocControl?.setValue(null);
        this.filteredToLocations = [];
      }
      toWhControl?.updateValueAndValidity();
      toLocControl?.updateValueAndValidity();
    });
  }

  loadData(): void {
    this.isLoading = true;
    const sdate = this.datePipe.transform(this.sDate || new Date(), 'yyyy-MM-dd') ?? '';
    const edate = this.datePipe.transform(this.eDate || new Date(), 'yyyy-MM-dd') ?? '';
    const type = this.selectedType === '_' ? null : this.selectedType;

    this.stockTransactionService.get({
      transactionType: type,
      startDate: sdate,
      endDate: edate,
      warehouseId: this.selectedWarehouseFilter,
      q: this.searchKeyword,
      sortField: this.sortField,
      order: this.sortOrder,
      pageNumber: this.pageNumber,
      pageSize: this.pageSize
    }).subscribe({
      next: (res) => {
        if (res.data) {
          this.stockTransactions = (res.data.items || []) as StockTransactionModel[];
          this.totalRecords = res.data.totalRecords ?? 0;
        } else {
          this.stockTransactions = [];
          this.totalRecords = 0;
        }
        this.isLoading = false;
        this.cdr.detectChanges();
      },
      error: () => {
        this.stockTransactions = [];
        this.totalRecords = 0;
        this.isLoading = false;
        this.cdr.detectChanges();
      }
    });
  }
 

  onStartDateChange(date: Date): void {
    this.sDate = date;
    if (!this.sDate || this.eDate < this.sDate) {
      this.eDate = new Date(this.sDate);
    }
  }

  onEndDateChange(date: Date): void {
    this.eDate = date;
    if (!this.eDate || this.sDate > this.eDate) {
      this.sDate = new Date(this.eDate);
    }
  }

  loadProducts(): void {
    this.productService.get().subscribe({
      next: (res) => {
        this.products = (res.data || []) as ProductModel[];
      },
      error: () => {
        this.products = [];
      }
    });
  }

  loadWarehouses(): void {
    this.warehouseService.get().subscribe({
      next: (res) => {
        this.warehouses = (res.data || []) as WarehouseModel[];
        this.buildWarehouseOptionsWithStock();
      },
      error: () => {
        this.warehouses = [];
        this.buildWarehouseOptionsWithStock();
      }
    });
  }

  loadProductWarehouseStocks(productId: string): void {
    if (!productId) {
      this.productWarehouseStocks = [];
      this.buildWarehouseOptionsWithStock();
      return;
    }
    this.stockTransactionService.getProductWarehouseStocks(productId).subscribe({
      next: (res) => {
        this.productWarehouseStocks = (res.data || []) as { warehouseId: number; warehouseName: string; quantity: number }[];
        this.buildWarehouseOptionsWithStock();
        this.cdr.detectChanges();
      },
      error: () => {
        this.productWarehouseStocks = [];
        this.buildWarehouseOptionsWithStock();
      }
    });
  }

  get calculatedTotalStock(): number {
    if (this.productWarehouseStocks && this.productWarehouseStocks.length > 0) {
      return this.productWarehouseStocks.reduce((sum, item) => sum + (Number(item.quantity) || 0), 0);
    }
    return this.selectedProductForForm ? Number(this.selectedProductForForm.currentStock) || 0 : 0;
  }

  buildWarehouseOptionsWithStock(): void {
    this.warehouseOptionsWithStock = this.warehouses.map(w => {
      const match = this.productWarehouseStocks.find(s => s.warehouseId === w.id);
      const stock = match ? Number(match.quantity) : 0;
      return {
        ...w,
        stock: stock,
        label: `${w.name} (Stock: ${stock})`
      };
    });
  }

  loadLocations(): void {
    this.warehouseLocationService.get().subscribe({
      next: (res) => {
        this.allLocations = (res.data || []) as WarehouseLocationModel[];
        this.updateFilteredLocations(Number(this.stockTransactionForm.get('warehouseId')?.value));
      },
      error: () => {
        this.allLocations = [];
      }
    });
  }

  onWarehouseChange(event: any): void {
    const warehouseId = event?.value ?? null;
    this.updateFilteredLocations(warehouseId);
    this.stockTransactionForm.patchValue({ warehouseLocationId: 0 });
    this.loadWarehouseBalance();
  }

  updateFilteredLocations(warehouseId: number | null): void {
    if (warehouseId && warehouseId > 0) {
      this.filteredLocations = this.allLocations.filter(loc => loc.warehouseId === warehouseId);
    } else {
      this.filteredLocations = [...this.allLocations];
    }
  }

  onToWarehouseChange(event: any): void {
    const warehouseId = event?.value ?? null;
    this.updateFilteredToLocations(warehouseId);
    this.stockTransactionForm.patchValue({ toWarehouseLocationId: null });
  }

  updateFilteredToLocations(warehouseId: number | null): void {
    if (warehouseId && warehouseId > 0) {
      this.filteredToLocations = this.allLocations.filter(loc => loc.warehouseId === warehouseId);
    } else {
      this.filteredToLocations = [...this.allLocations];
    }
  }

  onProductChange(event: any): void {
    const productId = event?.value ?? null;
    if (!productId) {
      this.selectedProductForForm = null;
      this.warehouseStock = null;
      this.productWarehouseStocks = [];
      this.buildWarehouseOptionsWithStock();
      return;
    }
    this.selectedProductForForm = this.products.find(p => p.id === productId) || null;
    this.loadProductWarehouseStocks(productId);
    this.loadWarehouseBalance();
  }

  loadWarehouseBalance(): void {
    const productId = this.stockTransactionForm.get('productId')?.value;
    const warehouseId = Number(this.stockTransactionForm.get('warehouseId')?.value);
    if (productId && warehouseId > 0) {
      this.stockTransactionService.getWarehouseBalance(productId, warehouseId).subscribe({
        next: (res) => {
          this.warehouseStock = res.data != null ? Number(res.data) : 0;
          this.cdr.detectChanges();
        },
        error: () => {
          this.warehouseStock = null;
        }
      });
    } else {
      this.warehouseStock = null;
    }
  }

  onDialogHide(): void {
    this.modalVisible = false;
    this.selectedStockTransaction = null;
    this.selectedProductForForm = null;
    this.isEdit = false;
    this.productWarehouseStocks = [];
    this.buildWarehouseOptionsWithStock();
  }

  create(): void {
    this.isEdit = false;
    this.modalVisible = true;
    this.selectedStockTransaction = null;
    this.selectedProductForForm = null;
    this.warehouseStock = null;
    this.productWarehouseStocks = [];
    this.buildWarehouseOptionsWithStock();
    this.filteredToLocations = [];

    this.stockTransactionForm.reset({
      id: 0,
      productId: '',
      warehouseId: 0,
      warehouseLocationId: 0,
      toWarehouseId: null,
      toWarehouseLocationId: null,
      transactionType: '',
      quantity: 1,
      transactionDate: new Date(),
      referenceNo: Math.floor(100000 + Math.random() * 900000),
      note: ''
    });
  }

  onSubmit(): void {
    this.isSubmitting = true;
    if (this.stockTransactionForm.valid) {
      const formValue = this.stockTransactionForm.value;
      const isTransfer = formValue.transactionType === 'TRANSFER';

      if (isTransfer && Number(formValue.warehouseId) === Number(formValue.toWarehouseId)) {
        this.isSubmitting = false;
        this.messageService.add({
          key: 'globalMessage',
          severity: 'error',
          summary: 'Validation Error',
          detail: 'Source warehouse and Destination warehouse cannot be the same.'
        });
        return;
      }

      const quantity = Number(formValue.quantity);
      if ((formValue.transactionType === 'OUT' || isTransfer) && this.warehouseStock !== null && quantity > this.warehouseStock) {
        this.isSubmitting = false;
        this.messageService.add({
          key: 'globalMessage',
          severity: 'error',
          summary: 'Insufficient Warehouse Stock',
          detail: `Selected warehouse only has ${this.warehouseStock} available.`
        });
        return;
      }

      const model: StockTransactionModel = {
        id: this.isEdit ? Number(formValue.id) : 0,
        productId: formValue.productId!,
        warehouseId: Number(formValue.warehouseId),
        warehouseLocationId: Number(formValue.warehouseLocationId),
        toWarehouseId: isTransfer && formValue.toWarehouseId ? Number(formValue.toWarehouseId) : null,
        toWarehouseLocationId: isTransfer && formValue.toWarehouseLocationId ? Number(formValue.toWarehouseLocationId) : null,
        transactionType: formValue.transactionType as any,
        quantity: Number(formValue.quantity),
        transactionDate: this.datePipe.transform(formValue.transactionDate || new Date(), 'yyyy-MM-ddTHH:mm:ss')!,
        referenceNo: formValue.referenceNo ? Number(formValue.referenceNo) : null,
        note: formValue.note || null
      };

      if (!this.isEdit) {
        this.stockTransactionService.create(model).subscribe({
          next: (res) => {
            if (res.success) {
              this.isSubmitting = false;
              this.modalVisible = false;
              this.messageService.add({
                key: 'globalMessage',
                severity: 'success',
                summary: 'Success',
                detail: 'Stock transaction created successfully.'
              });
              this.stockTransactionForm.reset();
              this.loadData();
              this.loadProducts();
              this.cdr.detectChanges();
            }
          },
          error: (err) => {
            this.isSubmitting = false;
            this.messageService.add({
              key: 'globalMessage',
              severity: 'error',
              summary: 'Error',
              detail: err.error?.message ? err.error.message.toString() : 'An error occurred while creating the stock transaction.'
            });
            this.cdr.detectChanges();
          }
        });
      } else {
        this.stockTransactionService.update(model).subscribe({
          next: (res) => {
            if (res.success) {
              this.isSubmitting = false;
              this.modalVisible = false;
              this.isEdit = false;
              this.stockTransactionForm.reset();
              this.messageService.add({
                key: 'globalMessage',
                severity: 'success',
                summary: 'Success',
                detail: 'Stock transaction updated successfully.'
              });
              this.loadData();
              this.loadProducts();
              this.cdr.detectChanges();
            }
          },
          error: (err) => {
            this.isSubmitting = false;
            this.messageService.add({
              key: 'globalMessage',
              severity: 'error',
              summary: 'Error',
              detail: err.error?.message ? err.error.message.toString() : 'An error occurred while updating the stock transaction.'
            });
            this.cdr.detectChanges();
          }
        });
      }
    } else {
      Object.keys(this.stockTransactionForm.controls).forEach(field => {
        const control = this.stockTransactionForm.get(field);
        control?.markAsDirty({ onlySelf: true });
      });
      this.isSubmitting = false;
    }
  }

  update(): void {
    if (this.selectedStockTransaction) {
      this.isEdit = true;
      this.stockTransactionForm.reset();
      this.modalVisible = true;
      const txn = this.selectedStockTransaction;
      this.updateFilteredLocations(txn.warehouseId);
      if (txn.toWarehouseId) {
        this.updateFilteredToLocations(txn.toWarehouseId);
      } else {
        this.filteredToLocations = [];
      }
      this.selectedProductForForm = this.products.find(p => p.id === txn.productId) || null;
      if (txn.productId) {
        this.loadProductWarehouseStocks(txn.productId);
      } else {
        this.productWarehouseStocks = [];
        this.buildWarehouseOptionsWithStock();
      }

      this.stockTransactionForm.patchValue({
        id: txn.id,
        productId: txn.productId,
        warehouseId: txn.warehouseId,
        warehouseLocationId: txn.warehouseLocationId,
        toWarehouseId: txn.toWarehouseId ?? null,
        toWarehouseLocationId: txn.toWarehouseLocationId ?? null,
        transactionType: txn.transactionType,
        quantity: txn.quantity,
        transactionDate: txn.transactionDate ? new Date(txn.transactionDate) : new Date(),
        referenceNo: txn.referenceNo,
        note: txn.note || txn.notes || ''
      });
      this.loadWarehouseBalance();
    } else {
      this.messageService.add({
        key: 'globalMessage',
        severity: 'warn',
        summary: 'Warning',
        detail: 'Please select a stock transaction.'
      });
    }
  }

  delete(): void {
    if (this.selectedStockTransaction != null) {
      const txnId = this.selectedStockTransaction.id!;
      this.confirmationService.confirm({
        message: `Are you sure to delete transaction #${txnId}? Stock balances will be reverted accordingly.`,
        header: 'Delete Confirmation',
        icon: 'pi pi-info-circle',
        key: 'positionDialog',
        accept: () => {
          this.stockTransactionService.delete(txnId).subscribe({
            next: (res) => {
              this.selectedStockTransaction = null;
              this.messageService.add({
                key: 'globalMessage',
                severity: 'success',
                summary: 'Confirmed',
                detail: res.message ?? 'Stock transaction was deleted successfully.'
              });
              this.loadData();
              this.loadProducts();
              this.cdr.detectChanges();
            },
            error: (err) => {
              this.messageService.add({
                key: 'globalMessage',
                severity: 'error',
                summary: 'Error',
                detail: err.error?.message ?? 'An error occurred while deleting transaction.'
              });
              this.cdr.detectChanges();
            }
          });
        },
        reject: () => {
          this.selectedStockTransaction = null as any;
        }
      });
    } else {
      this.messageService.add({
        key: 'globalMessage',
        severity: 'warn',
        summary: 'Warning',
        detail: 'Please select a stock transaction.'
      });
    }
  }

  view(): void {
    this.pageNumber = 1;
    if (this.tblStockTransactions) {
      this.tblStockTransactions.first = 0;
    }
    this.loadData();
  }

  resetState(): void {
    this.selectedType = '_';
    this.selectedWarehouseFilter = null;
    this.sDate = new Date();
    this.eDate = new Date();
    this.searchKeyword = '';
    this.pageNumber = 1;
    this.sortField = 'createdOn';
    this.sortOrder = -1;
    if (this.tblStockTransactions) {
      this.tblStockTransactions.first = 0;
    }
    this.loadData();
  }

  openDetail(txn: StockTransactionModel): void {
    this.selectedStockTransaction = txn;
    this.detailModalVisible = true;
  }

  editRow(txn: StockTransactionModel): void {
    this.selectedStockTransaction = txn;
    this.update();
  }

  deleteRow(txn: StockTransactionModel): void {
    this.selectedStockTransaction = txn;
    this.delete();
  }

  getWarehouseName(warehouseId: number): string {
    const wh = this.warehouses.find(w => w.id === warehouseId);
    return wh ? wh.name : `WH #${warehouseId}`;
  }

  getLocationName(locationId: number): string {
    const loc = this.allLocations.find(l => l.id === locationId);
    return loc ? `${loc.locationCode}` : `LOC #${locationId}`;
  }

  getTxnSeverity(type: string): 'success' | 'danger' | 'warn' | 'info' {
    switch (type) {
      case 'IN': return 'success';
      case 'OUT': return 'danger';
      case 'TRANSFER': return 'info';
      case 'ADJUSTMENT': return 'warn';
      default: return 'warn';
    }
  }

  excel(): void {
    this.exportService.excelAll('Stock_Transactions', this.tblStockTransactions);
  }
}
