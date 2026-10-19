import { CommonModule, formatDate } from '@angular/common';
import { ChangeDetectionStrategy, ChangeDetectorRef, Component, DestroyRef, inject, OnInit } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormBuilder, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';
import { HttpErrorResponse } from '@angular/common/http';
import { ConfirmationService, MenuItem, MessageService } from 'primeng/api';
import { SplitButtonModule } from 'primeng/splitbutton';
import { ButtonModule } from 'primeng/button';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { DatePickerModule } from 'primeng/datepicker';
import { DialogModule } from 'primeng/dialog';
import { InputNumberModule } from 'primeng/inputnumber';
import { InputTextModule } from 'primeng/inputtext';
import { TextareaModule } from 'primeng/textarea';
import { PaginatorModule, PaginatorState } from 'primeng/paginator';
import { IconFieldModule } from 'primeng/iconfield';
import { InputIconModule } from 'primeng/inputicon';
import { SelectModule } from 'primeng/select';
import { TableLazyLoadEvent, TableModule } from 'primeng/table';
import { TagModule } from 'primeng/tag';
import { ToastModule } from 'primeng/toast';
import { SuppliersModel } from '../../../core/models/master/suppliers.model';
import { CreatePurchaseReturnRequest, PurchaseReturnModel, PurchaseReturnStatus, ReturnableReceiptItemModel, ReturnDraftLine } from '../../../core/models/process/purchase-return.model';
import { SuppliersService } from '../../../core/services/master/suppliers.service';
import { PurchaseReturnsService } from '../../../core/services/process/purchase-returns.service';

@Component({
  selector: 'app-purchase-return',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    ReactiveFormsModule,
    ButtonModule,
    SplitButtonModule,
    ConfirmDialogModule,
    DatePickerModule,
    DialogModule,
    InputNumberModule,
    InputTextModule,
    TextareaModule,
    PaginatorModule,
    SelectModule,
    TableModule,
    TagModule,
    ToastModule,
    IconFieldModule,
    InputIconModule,
  ],
  providers: [ConfirmationService, MessageService],
  templateUrl: './purchase-return.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PurchaseReturn implements OnInit {
  private readonly destroyRef = inject(DestroyRef);
  private readonly formBuilder = inject(FormBuilder);
  private numberRequest: number = 0;
  private listRequest: number = 0;
  private sourceRequest: number = 0;
  private detailRequest: number = 0;
  private pendingCreate: CreatePurchaseReturnRequest | null = null;

  readonly status = PurchaseReturnStatus;
  readonly statusOptions = [{
    label: 'Draft',
    value: PurchaseReturnStatus.Draft,
  }, {
    label: 'Posted',
    value: PurchaseReturnStatus.Posted,
  }];
  items: MenuItem[] = [
    { label: 'Edit', icon: 'pi pi-pencil', command: () => this.edit() },
    { label: 'Delete', icon: 'pi pi-trash', command: () => this.delete() },
  ];
  returns: PurchaseReturnModel[] = [];
  selectedPurchaseReturn: PurchaseReturnModel | null = null;
  suppliers: SuppliersModel[] = [];
  isLoading: boolean = false;
  isSuppliersLoading: boolean = false;
  listError: string = '';
  first: number = 0;
  rows: number = 20;
  totalRecords: number = 0;
  sortField: string = 'createdOn';
  sortOrder: number = -1;
  searchKeyword: string = '';
  filterSupplier: number | null = null;
  filterStatus: PurchaseReturnStatus | null = null;
  startDate: Date | null = null;
  endDate: Date | null = null;

  editId: string | null = null;
  formReturnNo: string = '';
  numberError: string = '';
  isNumberLoading: boolean = false;
  deletingId: string | null = null;

  // Create / Edit Dialog
  formModalVisible: boolean = false;
  isSubmitting: boolean = false;
  saveError: string = '';
  isSourceLoading: boolean = false;
  sourceError: string = '';
  sources: ReturnableReceiptItemModel[] = [];
  lines: ReturnDraftLine[] = [];
  sourceKeyword: string = '';
  sourceStartDate: Date | null = null;
  sourceEndDate: Date | null = null;
  sourceFirst: number = 0;
  sourceRows: number = 10;
  sourceTotal: number = 0;

  // Detail Dialog
  detailModalVisible: boolean = false;
  isDetailLoading: boolean = false;
  detailError: string = '';
  detail: PurchaseReturnModel | null = null;
  postingId: string | null = null;

  readonly purchaseReturnForm = this.formBuilder.group({
    supplierId: [null as number | null, [Validators.required, Validators.min(1)]],
    returnDate: [new Date() as Date | null, Validators.required],
    returnReason: ['', Validators.maxLength(500)],
  });
  constructor(
    private purchaseReturnService: PurchaseReturnsService,
    private supplierService: SuppliersService,
    private messageService: MessageService,
    private confirmationService: ConfirmationService,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.loadMasterData();
  }

  loadData(): void {
    this.selectedPurchaseReturn = null;
    const request = ++this.listRequest;
    if (!this.validRange(this.startDate, this.endDate)) {
      this.listError = 'Start date cannot be after end date.';
      this.isLoading = false;
      return;
    }
    this.isLoading = true;
    this.listError = '';
    this.purchaseReturnService.getPaged({
      q: this.searchKeyword.trim(),
      supplierId: this.filterSupplier ?? undefined,
      status: this.filterStatus ?? undefined,
      startDate: this.date(this.startDate),
      endDate: this.date(this.endDate),
      sortField: this.sortField,
      order: this.sortOrder,
      pageNumber: Math.floor(this.first / this.rows) + 1,
      pageSize: this.rows,
    })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (res) => {
          if (request !== this.listRequest) {
            return;
          }
          this.returns = res.data.items;
          this.totalRecords = res.data.totalRecords;
          this.isLoading = false;
          if (this.first > 0 && this.first >= this.totalRecords) {
            this.first = 0;
            this.loadData();
          }
          this.cdr.markForCheck();
        },
        error: (err) => {
          if (request !== this.listRequest) {
            return;
          }
          this.isLoading = false;
          this.listError = this.errorMessage(err);
          this.returns = [];
          this.totalRecords = 0;
          this.cdr.markForCheck();
        },
      });
  }

  loadMasterData(): void {
    this.isSuppliersLoading = true;
    this.supplierService.get()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (res) => {
          this.suppliers = Array.isArray(res.data) ? res.data : res.data?.items ?? [];
          this.isSuppliersLoading = false;
          this.cdr.markForCheck();
        },
        error: (err) => {
          this.isSuppliersLoading = false;
          this.notify('error', 'Unable to load suppliers', this.errorMessage(err));
          this.cdr.markForCheck();
        },
      });
  }

  create(isEdit: boolean = false): void {
    const numberRequest = ++this.numberRequest;
    this.isNumberLoading = false;
    ++this.sourceRequest;
    this.editId = null;
    this.formReturnNo = '';
    this.numberError = '';
    this.purchaseReturnForm.enable();
    this.purchaseReturnForm.reset({
      supplierId: null,
      returnDate: new Date(),
      returnReason: '',
    });
    this.pendingCreate = null;
    this.lines = [];
    this.sources = [];
    this.sourceTotal = 0;
    this.sourceFirst = 0;
    this.sourceKeyword = '';
    this.sourceStartDate = null;
    this.sourceEndDate = null;
    this.saveError = '';
    this.sourceError = '';
    this.formModalVisible = true;
    if (!isEdit) {
      const year = new Date().getFullYear();
      this.isNumberLoading = true;
      this.purchaseReturnService.getLastNumber(year)
        .pipe(takeUntilDestroyed(this.destroyRef))
        .subscribe({
          next: (res) => {
            if (numberRequest !== this.numberRequest) {
              return;
            }
            const lastNumber = res.data ? Number(res.data.slice(`PR-${year}-`.length)) : 0;
            const nextNumber = lastNumber + 1;
            this.isNumberLoading = false;
            if (!Number.isSafeInteger(nextNumber) || nextNumber <= 0) {
              this.numberError = 'Unable to generate a return number. Close and reopen Create Return.';
              this.saveError = this.numberError;
            } else {
              this.formReturnNo = `PR-${year}-${String(nextNumber).padStart(8, '0')}`;
            }
            this.cdr.markForCheck();
          },
          error: (err) => {
            if (numberRequest !== this.numberRequest) {
              return;
            }
            this.isNumberLoading = false;
            this.numberError = err.status === 404
              ? 'Return-number API is unavailable. Rebuild and restart the API, then reopen Create Return.'
              : this.errorMessage(err);
            this.saveError = this.numberError;
            this.cdr.markForCheck();
          },
        });
    }
  }

  onSubmit(): void {
    if (this.isSubmitting || this.isNumberLoading) {
      return;
    }
    this.purchaseReturnForm.markAllAsTouched();
    if (!this.pendingCreate) {
      if (!this.formReturnNo || this.purchaseReturnForm.invalid || !this.lines.length || this.lines.some((line) => !this.validQuantity(line))) {
        this.saveError = 'Fill in the required fields and add at least one item with a valid quantity.';
        return;
      }
      const value = this.purchaseReturnForm.getRawValue();
      this.pendingCreate = {
        returnNo: this.formReturnNo,
        supplierId: value.supplierId!,
        returnDate: `${this.date(value.returnDate)}T00:00:00`,
        returnReason: value.returnReason?.trim() || null,
        idempotencyKey: crypto.randomUUID(),
        items: this.lines.map((line) => ({
          goodReceiptItemId: line.goodReceiptItemId,
          returnQuantity: line.returnQuantity,
        })),
      };
    }
    this.isSubmitting = true;
    this.purchaseReturnForm.disable();
    this.saveError = '';
    const { idempotencyKey, returnNo, ...draft } = this.pendingCreate;
    const saveRequest = this.editId
      ? this.purchaseReturnService.update(this.editId, { id: this.editId, ...draft })
      : this.purchaseReturnService.create(this.pendingCreate);
    saveRequest
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (res) => {
          this.isSubmitting = false;
          if (!res.success || !res.data) {
            this.saveError = 'Save was not confirmed. Retry the same draft.';
            this.cdr.markForCheck();
            return;
          }
          this.pendingCreate = null;
          this.formModalVisible = false;
          ++this.sourceRequest;
          this.notify('success', this.editId ? 'Draft updated' : 'Draft saved', res.data.returnNo);
          this.first = 0;
          this.loadData();
          ++this.detailRequest;
          this.isDetailLoading = false;
          this.detail = res.data;
          this.detailError = '';
          this.detailModalVisible = true;
          this.cdr.markForCheck();
        },
        error: (err: HttpErrorResponse) => {
          this.isSubmitting = false;
          if (err.status === 400 || err.status === 422) {
            this.pendingCreate = null;
            this.purchaseReturnForm.enable();
          }
          this.saveError = this.errorMessage(err);
          if (this.pendingCreate) {
            this.saveError += ' Save result is unconfirmed. Retry uses the same request and key.';
          }
          this.cdr.markForCheck();
        },
      });
  }

  edit(row: PurchaseReturnModel | null = this.selectedPurchaseReturn): void {
    if (!row) {
      this.notify('warn', 'Select a return', 'Please select a purchase return first.');
      return;
    }
    if (row.status !== this.status.Draft) {
      this.notify('error', 'Cannot update return', 'Posted purchase returns cannot be updated.');
      return;
    }
    if (this.isSubmitting) {
      return;
    }
    this.detailModalVisible = false;
    const request = ++this.detailRequest;
    this.create(true);
    this.editId = row.id;
    this.formReturnNo = row.returnNo;
    this.isSubmitting = true;
    this.purchaseReturnForm.disable();
    this.purchaseReturnService.getById(row.id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (res) => {
          if (request !== this.detailRequest) {
            return;
          }
          this.isSubmitting = false;
          if (res.data.status !== this.status.Draft) {
            this.formModalVisible = false;
            this.notify('warn', 'Unable to edit', 'Only draft purchase returns can be updated.');
            this.loadData();
          } else {
            this.formReturnNo = res.data.returnNo;
            this.purchaseReturnForm.enable();
            this.purchaseReturnForm.reset({
              supplierId: res.data.supplierId,
              returnDate: new Date(res.data.returnDate),
              returnReason: res.data.returnReason || '',
            });
            this.lines = res.data.items.map((item) => ({
              ...item,
              receiptDate: '',
              returnedQuantity: item.receivedQuantity - item.returnableQuantity,
            }));
            this.loadSources();
          }
          this.cdr.markForCheck();
        },
        error: (err) => {
          if (request !== this.detailRequest) {
            return;
          }
          this.isSubmitting = false;
          this.formModalVisible = false;
          this.notify('error', 'Unable to load draft', this.errorMessage(err));
          this.cdr.markForCheck();
        },
      });
  }

  delete(row: PurchaseReturnModel | null = this.selectedPurchaseReturn): void {
    if (!row) {
      this.notify('warn', 'Select a return', 'Please select a purchase return first.');
      return;
    }
    if (row.status !== this.status.Draft) {
      this.notify('error', 'Cannot delete return', 'Posted purchase returns cannot be deleted.');
      return;
    }
    if (this.deletingId) {
      return;
    }
    this.confirmationService.confirm({
      header: 'Delete purchase return?',
      message: `Delete draft ${row.returnNo}?`,
      icon: 'pi pi-exclamation-triangle',
      acceptLabel: 'Delete',
      rejectLabel: 'Cancel',
      accept: () => {
        this.deletingId = row.id;
        this.purchaseReturnService.delete(row.id)
          .pipe(takeUntilDestroyed(this.destroyRef))
          .subscribe({
            next: () => {
              this.deletingId = null;
              if (this.detail?.id === row.id) {
                this.detailModalVisible = false;
                this.detail = null;
              }
              this.notify('success', 'Draft deleted', row.returnNo);
              this.loadData();
              this.cdr.markForCheck();
            },
            error: (err) => {
              this.deletingId = null;
              this.notify('error', 'Unable to delete draft', this.errorMessage(err));
              this.loadData();
              this.cdr.markForCheck();
            },
          });
      },
    });
  }

  viewDetail(row: PurchaseReturnModel): void {
    const request = ++this.detailRequest;
    this.detail = null;
    this.detailError = '';
    this.detailModalVisible = true;
    this.isDetailLoading = true;
    this.purchaseReturnService.getById(row.id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (res) => {
          if (request !== this.detailRequest) {
            return;
          }
          this.detail = res.data;
          this.isDetailLoading = false;
          this.cdr.markForCheck();
        },
        error: (err) => {
          if (request !== this.detailRequest) {
            return;
          }
          this.detailError = this.errorMessage(err);
          this.isDetailLoading = false;
          this.cdr.markForCheck();
        },
      });
  }

  confirmPost(row: PurchaseReturnModel): void {
    if (this.postingId || row.status !== PurchaseReturnStatus.Draft) {
      return;
    }
    this.confirmationService.confirm({
      header: 'Post purchase return?',
      message: `${row.returnNo}: posting deducts warehouse stock. This return cannot be edited afterwards.`,
      icon: 'pi pi-exclamation-triangle',
      acceptLabel: 'Post Return',
      rejectLabel: 'Cancel',
      accept: () => this.postReturn(row),
    });
  }

  onLazyLoad(event: TableLazyLoadEvent): void {
    this.first = event.first ?? 0;
    this.rows = event.rows ?? 20;
    this.sortField = Array.isArray(event.sortField) ? event.sortField[0] : event.sortField || 'createdOn';
    this.sortOrder = event.sortOrder ?? -1;
    this.loadData();
  }

  applyFilters(): void {
    this.first = 0;
    this.loadData();
  }

  clearFilters(): void {
    this.searchKeyword = '';
    this.filterSupplier = null;
    this.filterStatus = null;
    this.startDate = null;
    this.endDate = null;
    this.applyFilters();
  }

  closeCreate(): void {
    ++this.numberRequest;
    this.isNumberLoading = false;
    ++this.sourceRequest;
    this.isSourceLoading = false;
    if (this.pendingCreate) {
      this.notify('warn', 'Check the return list', 'The previous save result was not confirmed. Check the list before creating another draft.');
      this.loadData();
    }
    this.pendingCreate = null;
  }

  onSupplierChange(): void {
    this.lines = [];
    this.sources = [];
    this.sourceTotal = 0;
    this.sourceFirst = 0;
    this.loadSources();
  }

  loadSources(): void {
    const request = ++this.sourceRequest;
    const supplierId = this.purchaseReturnForm.controls.supplierId.value;
    this.sourceError = '';
    this.isSourceLoading = false;
    if (!supplierId) {
      this.sources = [];
      this.sourceTotal = 0;
      return;
    }
    if (!this.validRange(this.sourceStartDate, this.sourceEndDate)) {
      this.sourceError = 'Receipt start date cannot be after end date.';
      return;
    }
    this.isSourceLoading = true;
    this.purchaseReturnService.getReturnableItems({
      supplierId,
      q: this.sourceKeyword.trim(),
      startDate: this.date(this.sourceStartDate),
      endDate: this.date(this.sourceEndDate),
      pageNumber: Math.floor(this.sourceFirst / this.sourceRows) + 1,
      pageSize: this.sourceRows,
    })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (res) => {
          if (request !== this.sourceRequest || !this.formModalVisible) {
            return;
          }
          this.sources = res.data.items;
          this.sourceTotal = res.data.totalRecords;
          this.isSourceLoading = false;
          this.cdr.markForCheck();
        },
        error: (err) => {
          if (request !== this.sourceRequest || !this.formModalVisible) {
            return;
          }
          this.sources = [];
          this.sourceTotal = 0;
          this.isSourceLoading = false;
          this.sourceError = this.errorMessage(err);
          this.cdr.markForCheck();
        },
      });
  }

  searchSources(): void {
    this.sourceFirst = 0;
    this.loadSources();
  }

  sourcePage(event: PaginatorState): void {
    this.sourceFirst = event.first ?? 0;
    this.sourceRows = event.rows ?? 10;
    this.loadSources();
  }

  addLine(source: ReturnableReceiptItemModel): void {
    if (this.createLocked || this.isSelected(source.goodReceiptItemId)) {
      return;
    }
    this.lines = [...this.lines, {
      ...source,
      returnQuantity: Math.min(1, source.returnableQuantity),
    }];
  }

  removeLine(id: number): void {
    if (!this.createLocked) {
      this.lines = this.lines.filter((line) => line.goodReceiptItemId !== id);
    }
  }

  isSelected(id: number): boolean {
    return this.lines.some((line) => line.goodReceiptItemId === id);
  }

  validQuantity(line: ReturnDraftLine): boolean {
    return Number.isFinite(line.returnQuantity) && line.returnQuantity > 0
      && line.returnQuantity <= line.returnableQuantity
      && Math.abs(line.returnQuantity * 10000 - Math.round(line.returnQuantity * 10000)) < 0.00001;
  }

  get createLocked(): boolean {
    return this.isSubmitting || this.pendingCreate !== null;
  }

  private postReturn(row: PurchaseReturnModel): void {
    if (this.postingId) {
      return;
    }
    this.postingId = row.id;
    this.purchaseReturnService.post(row.id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (res) => {
          this.postingId = null;
          this.notify('success', 'Return posted', res.data.returnNo);
          if (this.detail?.id === row.id) {
            this.detail = res.data;
          }
          this.loadData();
          this.cdr.markForCheck();
        },
        error: (err) => {
          this.postingId = null;
          this.notify('error', 'Posting was not confirmed', this.errorMessage(err));
          this.loadData();
          if (this.detail?.id === row.id) {
            this.viewDetail(row);
          }
          this.cdr.markForCheck();
        },
      });
  }

  private date(value: Date | null): string | undefined {
    return value ? formatDate(value, 'yyyy-MM-dd', 'en-US') : undefined;
  }

  private validRange(start: Date | null, end: Date | null): boolean {
    return !start || !end || this.date(start)! <= this.date(end)!;
  }

  private notify(severity: 'success' | 'error' | 'warn', summary: string, detail: string): void {
    this.messageService.add({
      severity,
      summary,
      detail,
      life: 6500,
    });
  }

  private errorMessage(error: unknown): string {
    const response = error as HttpErrorResponse;
    const body = response.error;
    const validation = body?.errors;
    if (validation && typeof validation === 'object') {
      const messages = Object.values(validation).flat().filter((value) => typeof value === 'string');
      if (messages.length) {
        return messages.join(' ');
      }
    }
    return body?.detail || body?.message || body?.title || (response.status === 0 ? 'Unable to reach the server.' : 'The request could not be completed.');
  }
}
