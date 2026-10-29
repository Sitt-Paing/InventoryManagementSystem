import { ChangeDetectorRef, Component, DestroyRef, inject, OnDestroy, OnInit, ViewChild } from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormBuilder, FormsModule, ReactiveFormsModule } from '@angular/forms';
import { Subscription } from 'rxjs';
import { ButtonModule } from 'primeng/button';
import { DatePickerModule } from 'primeng/datepicker';
import { IconFieldModule } from 'primeng/iconfield';
import { InputIconModule } from 'primeng/inputicon';
import { InputTextModule } from 'primeng/inputtext';
import { SelectModule } from 'primeng/select';
import { Table, TableLazyLoadEvent, TableModule } from 'primeng/table';
import { ToggleSwitchModule } from 'primeng/toggleswitch';
import { ToastModule } from 'primeng/toast';
import { MessageService } from 'primeng/api';
import { StockBalanceModel } from '../../../core/models/report/stock-balance.model';
import { WarehouseModel } from '../../../core/models/master/warehouse.model';
import { CategoryModel } from '../../../core/models/master/category.model';
import { StockBalanceService } from '../../../core/services/report/stock-balance.service';
import { WarehouseService } from '../../../core/services/master/warehouse.service';
import { CategoryService } from '../../../core/services/master/category.service';
import { ExportService } from '../../../core/services/export.service';

@Component({
  selector: 'app-stock-balance',
  imports: [
    ToastModule,
    ReactiveFormsModule,
    FormsModule,
    CommonModule,
    ButtonModule,
    DatePickerModule,
    IconFieldModule,
    InputIconModule,
    InputTextModule,
    SelectModule,
    TableModule,
    ToggleSwitchModule
  ],
  providers: [MessageService, DatePipe, ExportService],
  templateUrl: './stock-balance.html',
  styleUrl: './stock-balance.scss',
})
export class StockBalance implements OnInit, OnDestroy {
  @ViewChild(Table) tblStockBalance!: Table;
  isLoading: boolean = false;
  stockBalances: StockBalanceModel[] = [];
  selectedStockBalance: StockBalanceModel | null = null;
  warehouses: WarehouseModel[] = [];
  categories: CategoryModel[] = [];
  first: number = 0;
  rows: number = 20;
  totalRecords: number = 0;
  sortField: string = 'productName';
  sortOrder: number = 1;
  listError: string = '';
  masterError: string = '';
  private destroyRef = inject(DestroyRef);
  private formBuilder = inject(FormBuilder);
  private listRequest?: Subscription;
  public stockBalanceForm = this.formBuilder.group({
    search: [''],
    startDate: [null as Date | null],
    endDate: [null as Date | null],
    warehouseId: [null as number | null],
    categoryId: [null as number | null],
    showZeroStock: [true],
  })

  constructor(
    private stockBalanceService: StockBalanceService,
    private warehouseService: WarehouseService,
    private categoryService: CategoryService,
    private dataPipe: DatePipe,
    private messageService: MessageService,
    private cdr: ChangeDetectorRef,
    private exportService: ExportService,
  ) {}

  ngOnInit(): void {
    this.loadMasters();
  }

  ngOnDestroy(): void {
    this.listRequest?.unsubscribe();
  }

  loadMasters(): void {
    this.warehouseService.get().pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (res) => {
        this.warehouses = res.success ? res.data as WarehouseModel[] : [];
        if (!res.success) this.masterError = res.message || 'Unable to load warehouses.';
        this.cdr.detectChanges();
      },
      error: () => {
        this.masterError = 'Unable to load warehouses. Refresh the page to retry.';
        this.cdr.detectChanges();
      }
    });
    this.categoryService.get().pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (res) => {
        this.categories = res.success ? res.data as CategoryModel[] : [];
        if (!res.success) this.masterError = res.message || 'Unable to load categories.';
        this.cdr.detectChanges();
      },
      error: () => {
        this.masterError = 'Unable to load categories. Refresh the page to retry.';
        this.cdr.detectChanges();
      }
    });
  }

  loadData(): void {
    this.listRequest?.unsubscribe();
    this.selectedStockBalance = null;
    this.stockBalances = [];
    this.totalRecords = 0;
    const filter = this.stockBalanceForm.getRawValue();
    const defaultDate = !filter.startDate && !filter.endDate ? this.getToday() : null;
    if (filter.startDate && filter.endDate && filter.startDate > filter.endDate) {
      this.listError = 'Start date must not be after end date.';
      this.isLoading = false;
      return;
    }
    this.isLoading = true;
    this.listError = '';
    this.listRequest = this.stockBalanceService.get({
      search: filter.search?.trim() || '',
      startDate: this.dataPipe.transform(filter.startDate ?? defaultDate, 'yyyy-MM-dd'),
      endDate: this.dataPipe.transform(filter.endDate ?? defaultDate, 'yyyy-MM-dd'),
      warehouseId: filter.warehouseId,
      categoryId: filter.categoryId,
      showZeroStock: filter.showZeroStock ?? true,
      pageNumber: Math.floor(this.first / this.rows) + 1,
      pageSize: this.rows,
      sortField: this.sortField,
      order: this.sortOrder,
    }).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (res) => {
        this.isLoading = false;
        if (res.success) {
          this.stockBalances = res.data.items as StockBalanceModel[];
          this.totalRecords = res.data.totalRecords;
        } else {
          this.listError = res.message || 'Unable to load stock balances.';
        }
        this.cdr.detectChanges();
      },
      error: (err) => {
        this.isLoading = false;
        this.listError = err.error?.message || 'Unable to load stock balances.';
        this.cdr.detectChanges();
      }
    });
  }

  onLazyLoad(event: TableLazyLoadEvent): void {
    this.first = event.first ?? 0;
    this.rows = event.rows ?? 20;
    this.sortField = typeof event.sortField === 'string' ? event.sortField : 'productName';
    this.sortOrder = event.sortOrder ?? 1;
    this.loadData();
  }

  applyFilters(): void {
    this.first = 0;
    this.loadData();
  }

  clearFilters(): void {
    this.stockBalanceForm.reset({
      search: '',
      startDate: null,
      endDate: null,
      warehouseId: null,
      categoryId: null,
      showZeroStock: true,
    });
    this.first = 0;
    this.loadData();
  }

  excel(): void {
    if (this.isLoading || this.stockBalances.length === 0) {
      this.messageService.add({ key: 'globalMessage', severity: 'warn', summary: 'Warning', detail: 'Please load stock balances before exporting.' });
      return;
    }
    this.exportService.excel('Stock Balance - Current Page', this.tblStockBalance);
  }

  private getToday(): Date {
    const today = new Date();
    return new Date(today.getFullYear(), today.getMonth(), today.getDate());
  }
}
