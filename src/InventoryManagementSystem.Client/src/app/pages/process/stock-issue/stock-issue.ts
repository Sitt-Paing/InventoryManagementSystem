import { CommonModule, formatDate } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { ChangeDetectorRef, Component, DestroyRef, inject, OnInit, ViewChild } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormBuilder, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';
import { forkJoin } from 'rxjs';
import { ConfirmationService, MenuItem, MessageService } from 'primeng/api';
import { ButtonModule } from 'primeng/button';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { DatePickerModule } from 'primeng/datepicker';
import { DialogModule } from 'primeng/dialog';
import { IconFieldModule } from 'primeng/iconfield';
import { InputIconModule } from 'primeng/inputicon';
import { InputNumberModule } from 'primeng/inputnumber';
import { InputTextModule } from 'primeng/inputtext';
import { SelectModule } from 'primeng/select';
import { SplitButtonModule } from 'primeng/splitbutton';
import { Table, TableLazyLoadEvent, TableModule } from 'primeng/table';
import { TagModule } from 'primeng/tag';
import { TextareaModule } from 'primeng/textarea';
import { ToastModule } from 'primeng/toast';
import { BarcodeLookupResultModel } from '../../../core/models/master/barcode-lookup.model';
import { DepartmentsModel } from '../../../core/models/master/departments.model';
import { ProductModel } from '../../../core/models/master/product.model';
import { WarehouseModel } from '../../../core/models/master/warehouse.model';
import { StockIssueDraftLine, StockIssueModel, StockIssueRequest, StockIssueStatus } from '../../../core/models/process/stock-issue.model';
import { DepartmentsService } from '../../../core/services/master/departments.service';
import { ProductService } from '../../../core/services/master/product.service';
import { WarehouseService } from '../../../core/services/master/warehouse.service';
import { StockIssuesService } from '../../../core/services/process/stock-issues.service';
import { StockTransactionService } from '../../../core/services/process/stock-transaction.service';

@Component({
  selector: 'app-stock-issue',
  imports: [CommonModule, FormsModule, ReactiveFormsModule, ButtonModule, ConfirmDialogModule,
    DatePickerModule, DialogModule, IconFieldModule, InputIconModule, InputNumberModule,
    InputTextModule, SelectModule, SplitButtonModule, TableModule, TagModule, TextareaModule, ToastModule],
  providers: [ConfirmationService, MessageService],
  templateUrl: './stock-issue.html',
  styleUrl: './stock-issue.scss',
})
export class StockIssue implements OnInit {
  @ViewChild(Table) tblStockIssue!: Table;
  items: MenuItem[] = [];
  stockIssues: StockIssueModel[] = [];
  selectedStockIssue: StockIssueModel | null = null;
  isLoading: boolean = false;
  isEdit: boolean = false;
  modalVisible: boolean = false;
  isSubmitting: boolean = false;
  isMasterLoading: boolean = false;
  isDetailLoading: boolean = false;
  detailModalVisible: boolean = false;
  isScanning: boolean = false;
  postingId: string | null = null;
  detail: StockIssueModel | null = null;
  warehouses: WarehouseModel[] = [];
  departments: DepartmentsModel[] = [];
  products: ProductModel[] = [];
  lines: StockIssueDraftLine[] = [];
  selectedProductId: string | null = null;
  barcode: string = '';
  first: number = 0;
  rows: number = 20;
  totalRecords: number = 0;
  searchKeyword: string = '';
  filterWarehouse: number | null = null;
  filterDepartment: number | null = null;
  filterStatus: StockIssueStatus | null = null;
  startDate: Date | null = null;
  endDate: Date | null = null;
  listError: string = '';
  readonly status = StockIssueStatus;
  readonly statusOptions = [{ label: 'Draft', value: StockIssueStatus.Draft }, { label: 'Posted', value: StockIssueStatus.Posted }];

  private readonly destroyRef = inject(DestroyRef);
  private formBuilder = inject(FormBuilder);
  private listRequest: number = 0;
  public stockIssueForm = this.formBuilder.group({
    id: [''],
    issueNo: ['', Validators.required],
    issueDate: [new Date() as Date | null, Validators.required],
    warehouseId: [null as number | null, Validators.required],
    departmentId: [null as number | null, Validators.required],
    receivedBy: ['', [Validators.required, Validators.maxLength(100)]],
    note: ['', Validators.maxLength(500)],
  });

  constructor(
    private stockIssuesService: StockIssuesService,
    private warehouseService: WarehouseService,
    private departmentsService: DepartmentsService,
    private productService: ProductService,
    private stockTransactionService: StockTransactionService,
    private confirmationService: ConfirmationService,
    private messageService: MessageService,
    private cdr: ChangeDetectorRef,
  ) {
    this.items = [
      { label: 'Edit', icon: 'pi pi-pencil', command: () => this.update() },
      { label: 'Delete', icon: 'pi pi-trash', command: () => this.delete() },
      { label: 'Post', icon: 'pi pi-check', command: () => this.post() },
    ];
  }

  ngOnInit(): void {
    this.loadMasterData();
  }

  loadData(): void {
    const request = ++this.listRequest;
    this.isLoading = true;
    this.listError = '';
    this.selectedStockIssue = null;
    const startDate = this.startDate ? formatDate(this.startDate, 'yyyy-MM-dd', 'en-US') : undefined;
    const endDate = this.endDate ? formatDate(this.endDate, 'yyyy-MM-dd', 'en-US') : undefined;
    if (startDate && endDate && startDate > endDate) {
      this.isLoading = false;
      this.listError = 'Start Date cannot be after End Date.';
      return;
    }
    this.stockIssuesService.get({
      search: this.searchKeyword.trim(), warehouseId: this.filterWarehouse,
      departmentId: this.filterDepartment, status: this.filterStatus,
      startDate, endDate,
      pageNumber: Math.floor(this.first / this.rows) + 1, pageSize: this.rows,
    }).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (res) => {
        if (request !== this.listRequest) return;
        this.stockIssues = res.data.items;
        this.totalRecords = res.data.totalRecords;
        this.isLoading = false;
        if (this.first > 0 && this.first >= this.totalRecords) {
          this.first = 0;
          this.loadData();
        }
        this.cdr.markForCheck();
      },
      error: (err) => {
        if (request !== this.listRequest) return;
        this.isLoading = false;
        this.stockIssues = [];
        this.totalRecords = 0;
        this.listError = this.errorMessage(err);
        this.cdr.markForCheck();
      }
    });
  }

  loadMasterData(): void {
    this.isMasterLoading = true;
    forkJoin({ warehouses: this.warehouseService.get(), departments: this.departmentsService.get(), products: this.productService.get() })
      .pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
        next: (res) => {
          this.warehouses = (res.warehouses.data as WarehouseModel[]).filter(item => item.status);
          this.departments = (res.departments.data as DepartmentsModel[]).filter(item => item.isActive);
          this.products = res.products.data.items.filter(item => item.status);
          this.isMasterLoading = false;
          this.cdr.markForCheck();
        },
        error: (err) => {
          this.isMasterLoading = false;
          this.notify('error', this.errorMessage(err));
          this.cdr.markForCheck();
        }
      });
  }

  onLazyLoad(event: TableLazyLoadEvent): void {
    this.first = event.first ?? 0;
    this.rows = event.rows ?? 20;
    this.loadData();
  }

  applyFilters(): void {
    this.first = 0;
    this.loadData();
  }

  clearFilters(): void {
    this.searchKeyword = '';
    this.filterWarehouse = null;
    this.filterDepartment = null;
    this.filterStatus = null;
    this.startDate = null;
    this.endDate = null;
    this.applyFilters();
  }

  onDialogHide(): void {
    this.modalVisible = false;
    this.isEdit = false;
    this.lines = [];
    this.selectedProductId = null;
    this.barcode = '';
  }

  create(): void {
    this.isEdit = false;
    this.selectedStockIssue = null;
    this.lines = [];
    this.selectedProductId = null;
    this.barcode = '';
    this.stockIssueForm.reset({
      id: '', issueNo: `SI-${new Date().getFullYear()}-${crypto.randomUUID().slice(0, 8).toUpperCase()}`,
      issueDate: new Date(), warehouseId: null, departmentId: null, receivedBy: '', note: ''
    });
    this.modalVisible = true;
  }

  onSubmit(): void {
    if (this.isSubmitting) return;
    this.stockIssueForm.markAllAsTouched();
    if (this.stockIssueForm.invalid || this.lines.length === 0 || this.lines.some(line => !line.quantity || line.quantity <= 0)) {
      this.notify('warn', 'Complete the required fields and add at least one item with a positive quantity.');
      return;
    }
    const value = this.stockIssueForm.getRawValue();
    const model: StockIssueRequest = {
      issueNo: value.issueNo!, issueDate: formatDate(value.issueDate!, 'yyyy-MM-dd', 'en-US'),
      warehouseId: value.warehouseId!, departmentId: value.departmentId!, receivedBy: value.receivedBy!.trim(),
      note: value.note?.trim() || null,
      items: this.lines.map(line => ({ productId: line.productId, quantity: line.quantity! })),
    };
    const { issueNo, ...updateFields } = model;
    const request = this.isEdit
      ? this.stockIssuesService.update({ ...updateFields, id: value.id! })
      : this.stockIssuesService.create(model);
    this.isSubmitting = true;
    request.pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (res) => {
        this.isSubmitting = false;
        this.modalVisible = false;
        this.notify('success', res.message);
        this.detail = res.data;
        this.detailModalVisible = true;
        this.loadData();
        this.cdr.markForCheck();
      },
      error: (err) => {
        this.isSubmitting = false;
        this.notify('error', this.errorMessage(err));
        this.cdr.markForCheck();
      }
    });
  }

  update(): void {
    const selected = this.selectedStockIssue;
    if (!selected) { this.notify('warn', 'Please select a stock issue.'); return; }
    if (selected.status !== this.status.Draft) { this.notify('error', 'Posted stock issues cannot be updated.'); return; }
    this.stockIssuesService.getById(selected.id).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (res) => {
        const issue = res.data;
        if (issue.status !== this.status.Draft) { this.notify('error', 'Posted stock issues cannot be updated.'); return; }
        this.stockIssueForm.reset({
          id: issue.id, issueNo: issue.issueNo, issueDate: new Date(issue.issueDate), warehouseId: issue.warehouseId,
          departmentId: issue.departmentId, receivedBy: issue.receivedBy, note: issue.note || '',
        });
        this.lines = issue.items.map(item => ({ ...item, availableQuantity: null }));
        this.selectedProductId = null;
        this.barcode = '';
        this.isEdit = true;
        this.modalVisible = true;
        this.refreshBalances();
        this.cdr.markForCheck();
      },
      error: (err) => this.notify('error', this.errorMessage(err))
    });
  }

  delete(): void {
    const selected = this.selectedStockIssue;
    if (!selected) { this.notify('warn', 'Please select a stock issue.'); return; }
    if (selected.status !== this.status.Draft) { this.notify('error', 'Posted stock issues cannot be deleted.'); return; }
    this.confirmationService.confirm({
      header: 'Delete Confirmation', message: `Delete draft ${selected.issueNo}?`, icon: 'pi pi-info-circle',
      accept: () => this.stockIssuesService.delete(selected.id).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
        next: (res) => {
          this.notify('success', res.message);
          if (this.detail?.id === selected.id) this.detailModalVisible = false;
          this.loadData();
        },
        error: (err) => this.notify('error', this.errorMessage(err))
      })
    });
  }

  viewDetail(row: StockIssueModel | null = this.selectedStockIssue): void {
    if (!row) { this.notify('warn', 'Please select a stock issue.'); return; }
    this.detail = null;
    this.isDetailLoading = true;
    this.detailModalVisible = true;
    this.stockIssuesService.getById(row.id).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (res) => {
        this.detail = res.data;
        this.isDetailLoading = false;
        this.cdr.markForCheck();
      },
      error: (err) => {
        this.isDetailLoading = false;
        this.detailModalVisible = false;
        this.notify('error', this.errorMessage(err));
        this.cdr.markForCheck();
      }
    });
  }

  post(row: StockIssueModel | null = this.selectedStockIssue): void {
    if (!row) { this.notify('warn', 'Please select a stock issue.'); return; }
    if (row.status !== this.status.Draft) { this.notify('warn', 'This stock issue is already posted.'); return; }
    if (this.postingId) return;
    this.confirmationService.confirm({
      header: 'Post Stock Issue', message: `Post ${row.issueNo}? Warehouse stock will be deducted.`, icon: 'pi pi-exclamation-triangle',
      accept: () => {
        if (this.postingId) return;
        this.postingId = row.id;
        this.stockIssuesService.post(row.id).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
          next: (res) => {
            this.postingId = null;
            this.detail = res.data;
            this.detailModalVisible = true;
            this.notify('success', res.message);
            this.loadData();
            this.cdr.markForCheck();
          },
          error: (err) => {
            this.postingId = null;
            this.notify('error', this.errorMessage(err));
            this.cdr.markForCheck();
          }
        });
      }
    });
  }

  addItem(productId: string | null = this.selectedProductId): void {
    if (!productId) { this.notify('warn', 'Select a product first.'); return; }
    if (this.lines.some(line => line.productId === productId)) { this.notify('warn', 'This product is already added.'); return; }
    const product = this.products.find(product => product.id === productId);
    if (!product) { this.notify('warn', 'This product is unavailable.'); return; }
    const line: StockIssueDraftLine = {
      productId, productName: product.name, productSku: product.sku || null,
      uomName: product.baseUomName || product.unit || '-', quantity: 1, availableQuantity: null,
    };
    this.lines = [...this.lines, line];
    this.selectedProductId = null;
    this.loadBalance(line);
  }

  removeItem(productId: string): void {
    this.lines = this.lines.filter(line => line.productId !== productId);
  }

  scanBarcode(): void {
    const code = this.barcode.trim();
    if (!code || this.isScanning) return;
    this.isScanning = true;
    this.productService.barcodeLookup(code).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (res) => {
        this.isScanning = false;
        const result = res.data as BarcodeLookupResultModel;
        if (result.lookupType !== 'Product' || !result.product) {
          this.notify('warn', 'Scan a product barcode or SKU.');
        } else {
          this.addItem(result.product.productId);
          this.barcode = '';
        }
        this.cdr.markForCheck();
      },
      error: (err) => {
        this.isScanning = false;
        this.notify('error', this.errorMessage(err));
        this.cdr.markForCheck();
      }
    });
  }

  refreshBalances(): void {
    this.lines.forEach(line => this.loadBalance(line));
  }

  private loadBalance(line: StockIssueDraftLine): void {
    const warehouseId = this.stockIssueForm.controls.warehouseId.value;
    line.availableQuantity = null;
    if (!warehouseId) return;
    this.stockTransactionService.getWarehouseBalance(line.productId, warehouseId)
      .pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
        next: (res) => {
          if (warehouseId !== this.stockIssueForm.controls.warehouseId.value) return;
          line.availableQuantity = Number(res.data);
          this.cdr.markForCheck();
        },
        error: () => {
          line.availableQuantity = null;
          this.cdr.markForCheck();
        }
      });
  }

  private notify(severity: 'success' | 'error' | 'warn', detail: string): void {
    this.messageService.add({ severity, summary: severity === 'success' ? 'Success' : 'Stock Issue', detail });
    this.cdr.markForCheck();
  }

  private errorMessage(error: HttpErrorResponse): string {
    const body = error.error;
    if (Array.isArray(body?.data)) {
      const messages = body.data.map((item: { message?: string }) => item.message).filter(Boolean);
      if (messages.length) return messages.join(' ');
    }
    return body?.message || body?.detail || body?.title || 'Unable to complete the request. Please try again.';
  }

}
