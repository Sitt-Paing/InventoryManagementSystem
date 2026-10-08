import { ChangeDetectorRef, Component, DestroyRef, inject, OnInit, ViewChild } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { CommonModule, DatePipe } from '@angular/common';
import { FormBuilder, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';
import { exhaustMap, forkJoin, Subject, take, takeUntil, takeWhile, timer } from 'rxjs';
import { ConfirmationService, MenuItem, MessageService } from 'primeng/api';
import { Table, TableLazyLoadEvent, TableModule } from 'primeng/table';
import { ToastModule } from 'primeng/toast';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { SplitButtonModule } from 'primeng/splitbutton';
import { IconFieldModule } from 'primeng/iconfield';
import { InputIconModule } from 'primeng/inputicon';
import { InputTextModule } from 'primeng/inputtext';
import { ButtonModule } from 'primeng/button';
import { DialogModule } from 'primeng/dialog';
import { Tag } from 'primeng/tag';
import { ToggleSwitchModule } from 'primeng/toggleswitch';
import { SelectModule } from 'primeng/select';
import { DatePickerModule } from 'primeng/datepicker';

import { PurchaseOrderModel, PurchaseOrderSaveModel, PurchaseOrderItemModel, PurchaseOrderStatus, PURCHASE_ORDER_STATUS_OPTIONS } from '../../../core/models/process/purchase-order.model';
import { PurchaseOrderEmailPreviewModel } from '../../../core/models/process/purchase-order-email-preview.model';
import { SuppliersModel } from '../../../core/models/master/suppliers.model';
import { WarehouseModel } from '../../../core/models/master/warehouse.model';
import { ProductModel } from '../../../core/models/master/product.model';
import { UnitOfMeasureModel } from '../../../core/models/master/unit-of-measure.model';

import { PurchaseOrdersService } from '../../../core/services/process/purchase-orders.service';
import { SuppliersService } from '../../../core/services/master/suppliers.service';
import { WarehouseService } from '../../../core/services/master/warehouse.service';
import { ProductService } from '../../../core/services/master/product.service';
import { UnitOfMeasureService } from '../../../core/services/master/unit-of-measure.service';
import { ExportService } from '../../../core/services/export.service';
import { PurchaseOrderPrintDialog } from './purchase-order-print-dialog/purchase-order-print-dialog';

@Component({
  selector: 'app-purchase-orders',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    ReactiveFormsModule,
    ToastModule,
    ConfirmDialogModule,
    SplitButtonModule,
    IconFieldModule,
    InputIconModule,
    InputTextModule,
    TableModule,
    Tag,
    DialogModule,
    ToggleSwitchModule,
    ButtonModule,
    SelectModule,
    DatePickerModule,
    PurchaseOrderPrintDialog
  ],
  providers: [ConfirmationService, MessageService, DatePipe, ExportService],
  templateUrl: './purchase-orders.html',
  styleUrl: './purchase-orders.scss',
})
export class PurchaseOrdersComponent implements OnInit {
  @ViewChild(Table) tblPurchaseOrders!: Table;

  items: MenuItem[] = [];
  isLoading: boolean = false;
  isSubmitting: boolean = false;
  isEdit: boolean = false;
  private createRequestKey: string = '';
  cancelModalVisible: boolean = false;
  cancellationReason: string = '';
  isCancelling: boolean = false;
  private cancelOrderId: string | null = null;
  modalVisible: boolean = false;
  detailModalVisible: boolean = false;
  printModalVisible: boolean = false;
  emailModalVisible: boolean = false;
  isLoadingEmailPreview: boolean = false;
  isSendingEmail: boolean = false;
  emailPreview: PurchaseOrderEmailPreviewModel | null = null;
  emailOrder: PurchaseOrderModel | null = null;
  emailError: string = '';
  emailStatus: string = '';
  acceptedEmailId: string | null = null;
  private emailRequestKeys = new Map<string, string>();
  private emailStatusClosed = new Subject<void>();
  private emailPreviewRequestId: number = 0;
  private destroyRef = inject(DestroyRef);

  purchaseOrders: PurchaseOrderModel[] = [];
  selectedPurchaseOrder: PurchaseOrderModel | null = null;
  detailOrder: PurchaseOrderModel | null = null;
  printOrderData: PurchaseOrderModel | null = null;

  // Server-side Pagination & Filter state
  totalRecords: number = 0;
  pageNumber: number = 1;
  pageSize: number = 20;
  sortField: string = 'createdOn';
  sortOrder: number = -1;
  searchKeyword: string = '';
  filterOrderDate: Date | null = null;
  filterStatus: PurchaseOrderStatus | null = null;
  statusOptions = PURCHASE_ORDER_STATUS_OPTIONS;
  statusFilterOptions = [
    { label: 'All Status', value: null },
    ...PURCHASE_ORDER_STATUS_OPTIONS
  ];
  PurchaseOrderStatus = PurchaseOrderStatus;

  // Dropdown master lists
  suppliers: SuppliersModel[] = [];
  warehouses: WarehouseModel[] = [];
  products: ProductModel[] = [];
  uoms: UnitOfMeasureModel[] = [];

  // Line items state for the dialog form
  formItems: PurchaseOrderItemModel[] = [];
  formTotalAmount: number = 0;

  private formBuilder = inject(FormBuilder);
  public purchaseOrderForm = this.formBuilder.group({
    id: [null as string | null],
    purchaseOrderNo: [''],
    supplierId: [null as number | null, [Validators.required, Validators.min(1)]],
    warehouseId: [null as number | null, [Validators.required, Validators.min(1)]],
    orderDate: [new Date(), Validators.required],
    expectedDate: [new Date(), Validators.required],
    status: [PurchaseOrderStatus.Pending, Validators.required],
  });

  constructor(
    private purchaseOrdersService: PurchaseOrdersService,
    private suppliersService: SuppliersService,
    private warehouseService: WarehouseService,
    private productService: ProductService,
    private uomService: UnitOfMeasureService,
    private confirmationService: ConfirmationService,
    private messageService: MessageService,
    private datePipe: DatePipe,
    private cdr: ChangeDetectorRef,
    private exportService: ExportService
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
        label: 'Cancel Order',
        icon: 'pi pi-ban',
        command: () => this.openCancellation()
      },
      {
        label: 'Email Preview',
        icon: 'pi pi-envelope',
        command: () => this.previewEmail()
      },
      {
        label: 'Excel',
        icon: 'pi pi-file-excel',
        command: () => this.excel()
      }
    ];
  }

  ngOnInit(): void {
    this.loadDropdownData();
  }

  loadData(): void {
    this.isLoading = true;
    const todayStr = this.datePipe.transform(new Date(), 'yyyy-MM-dd') ?? undefined;
    const orderDateStr = this.filterOrderDate
      ? (this.datePipe.transform(this.filterOrderDate, 'yyyy-MM-dd') ?? undefined)
      : todayStr;

    this.purchaseOrdersService.getPaged({
      q: this.searchKeyword,
      orderDate: orderDateStr,
      status: this.filterStatus,
      sortField: this.sortField,
      order: this.sortOrder,
      pageNumber: this.pageNumber,
      pageSize: this.pageSize
    }).subscribe({
      next: (res) => {
        if (res.data) {
          this.purchaseOrders = (res.data.items || []) as PurchaseOrderModel[];
          this.totalRecords = res.data.totalRecords ?? 0;
        } else {
          this.purchaseOrders = [];
          this.totalRecords = 0;
        }
        this.isLoading = false;
        this.cdr.detectChanges();
      },
      error: () => {
        this.purchaseOrders = [];
        this.totalRecords = 0;
        this.isLoading = false;
        this.cdr.detectChanges();
      }
    });
  }

  loadDropdownData(): void {
    forkJoin({
      suppliers: this.suppliersService.get(),
      warehouses: this.warehouseService.get(),
      products: this.productService.get(),
      uoms: this.uomService.get()
    }).subscribe({
      next: (res) => {
        this.suppliers = (res.suppliers?.data || []) as SuppliersModel[];
        this.warehouses = (res.warehouses?.data || []) as WarehouseModel[];
        this.products = (res.products?.data?.items || []) as ProductModel[];
        this.uoms = (res.uoms?.data || []) as UnitOfMeasureModel[];
        this.cdr.markForCheck();
      }
    });
  }

  onDialogHide(): void {
    this.modalVisible = false;
    this.selectedPurchaseOrder = null;
    this.isEdit = false;
    this.formItems = [];
  }

  create(): void {
    this.isEdit = false;
    this.createRequestKey = crypto.randomUUID();
    this.selectedPurchaseOrder = null;
    this.purchaseOrderForm.reset({
      id: null,
      purchaseOrderNo: '',
      supplierId: null,
      warehouseId: null,
      orderDate: new Date(),
      expectedDate: new Date(Date.now() + 7 * 24 * 60 * 60 * 1000), // Default 7 days later
      status: PurchaseOrderStatus.Pending
    });
    this.formItems = [this.createEmptyItem()];
    this.recalculateTotalAmount();
    this.modalVisible = true;
  }

  createEmptyItem(): PurchaseOrderItemModel {
    return {
      productId: '',
      quantity: 1,
      unitPrice: 0,
      uomId: 0,
      receivedQuantity: 0,
      subTotal: 0
    };
  }

  onSubmit(): void {
    if (this.isSubmitting) return;
    if (this.purchaseOrderForm.invalid) {
      Object.keys(this.purchaseOrderForm.controls).forEach(field => {
        const control = this.purchaseOrderForm.get(field);
        control?.markAsDirty({ onlySelf: true });
      });
      return;
    }

    // Validate line items
    const invalidItem = this.formItems.find(i => !i.productId || !Number.isFinite(i.quantity) || i.quantity <= 0 || !i.uomId || !Number.isFinite(i.unitPrice) || i.unitPrice < 0);
    if (this.formItems.length === 0 || invalidItem) {
      this.messageService.add({
        key: 'globalMessage',
        severity: 'error',
        summary: 'Validation Error',
        detail: 'Please ensure all items have a Product, valid Quantity, and UOM selected.'
      });
      return;
    }

    this.isSubmitting = true;
    const formVal = this.purchaseOrderForm.value;

    const payload: PurchaseOrderSaveModel = {
      id: formVal.id ?? undefined,
      supplierId: formVal.supplierId!,
      warehouseId: formVal.warehouseId!,
      orderDate: formVal.orderDate ? (this.datePipe.transform(formVal.orderDate, 'yyyy-MM-ddTHH:mm:ss') ?? new Date().toISOString()) : new Date().toISOString(),
      expectedDate: formVal.expectedDate ? (this.datePipe.transform(formVal.expectedDate, 'yyyy-MM-ddTHH:mm:ss') ?? new Date().toISOString()) : new Date().toISOString(),
      idempotencyKey: this.isEdit ? undefined : this.createRequestKey,
      items: this.formItems.map(i => ({
        id: i.id,
        productId: i.productId,
        quantity: i.quantity,
        unitPrice: i.unitPrice,
        uomId: i.uomId,
      }))
    };

    if (!this.isEdit) {
      this.purchaseOrdersService.create(payload).subscribe({
        next: (res) => {
          this.isSubmitting = false;
          if (res.success) {
            this.modalVisible = false;
            this.messageService.add({
              key: 'globalMessage',
              severity: 'success',
              summary: 'Success',
              detail: 'Purchase order created successfully.'
            });
            this.loadData();
          }
        },
        error: (err) => {
          this.isSubmitting = false;
          this.messageService.add({
            key: 'globalMessage',
            severity: 'error',
            summary: 'Error',
            detail: err.error?.message ?? 'Failed to create purchase order.'
          });
        }
      });
    } else {
      this.purchaseOrdersService.update(payload).subscribe({
        next: (res) => {
          this.isSubmitting = false;
          if (res.success) {
            this.modalVisible = false;
            this.messageService.add({
              key: 'globalMessage',
              severity: 'success',
              summary: 'Success',
              detail: 'Purchase order updated successfully.'
            });
            this.loadData();
          }
        },
        error: (err) => {
          this.isSubmitting = false;
          this.messageService.add({
            key: 'globalMessage',
            severity: 'error',
            summary: 'Error',
            detail: err.error?.message ?? 'Failed to update purchase order.'
          });
        }
      });
    }
  }

  update(): void {
    if (!this.selectedPurchaseOrder) {
      this.messageService.add({
        key: 'globalMessage',
        severity: 'warn',
        summary: 'Warning',
        detail: 'Please select a purchase order.'
      });
      return;
    }

    this.isEdit = true;
    const po = this.selectedPurchaseOrder;
    this.purchaseOrderForm.patchValue({
      id: po.id,
      purchaseOrderNo: po.purchaseOrderNo,
      supplierId: po.supplierId,
      warehouseId: po.warehouseId,
      orderDate: po.orderDate ? new Date(po.orderDate) : new Date(),
      expectedDate: po.expectedDate ? new Date(po.expectedDate) : new Date(),
      status: po.status
    });

    this.formItems = (po.items || []).map(i => ({
      id: i.id,
      purchaseOrderId: i.purchaseOrderId,
      productId: i.productId,
      productName: i.productName,
      productSku: i.productSku,
      quantity: i.quantity,
      unitPrice: i.unitPrice,
      uomId: i.uomId,
      uomName: i.uomName,
      subTotal: i.quantity * i.unitPrice
    }));

    if (this.formItems.length === 0) {
      this.formItems = [this.createEmptyItem()];
    }

    this.recalculateTotalAmount();
    this.modalVisible = true;
  }

  delete(): void {
    if (!this.selectedPurchaseOrder) {
      this.messageService.add({
        key: 'globalMessage',
        severity: 'warn',
        summary: 'Warning',
        detail: 'Please select a purchase order to delete.'
      });
      return;
    }

    const orderId = this.selectedPurchaseOrder.id!;
    const poNo = this.selectedPurchaseOrder.purchaseOrderNo;

    this.confirmationService.confirm({
      message: `Are you sure you want to delete purchase order "${poNo}"?`,
      header: 'Delete Confirmation',
      icon: 'pi pi-info-circle',
      key: 'positionDialog',
      accept: () => {
        this.purchaseOrdersService.delete(orderId).subscribe({
          next: (res) => {
            this.selectedPurchaseOrder = null;
            this.messageService.add({
              key: 'globalMessage',
              severity: 'success',
              summary: 'Deleted',
              detail: res.message ?? 'Purchase order deleted successfully.'
            });
            this.loadData();
          },
          error: (err) => {
            this.messageService.add({
              key: 'globalMessage',
              severity: 'error',
              summary: 'Error',
              detail: err.error?.message ?? 'Failed to delete purchase order.'
            });
          }
        });
      }
    });
  }

  excel(): void {
    const todayStr = this.datePipe.transform(new Date(), 'yyyy-MM-dd') ?? undefined;
    const orderDateStr = this.filterOrderDate
      ? (this.datePipe.transform(this.filterOrderDate, 'yyyy-MM-dd') ?? undefined)
      : todayStr;

    this.purchaseOrdersService.export({
      q: this.searchKeyword,
      orderDate: orderDateStr,
      status: this.filterStatus
    }).subscribe({
      next: (blob) => {
        this.exportService.excel_blob('Purchase_Orders', blob);
      },
      error: () => {
        // Fallback to client-side table export if API export fails
        this.exportService.excelAll('Purchase_Orders', this.tblPurchaseOrders);
      }
    });
  }

  onLazyLoad(event: TableLazyLoadEvent): void {
    this.pageNumber = Math.floor((event.first ?? 0) / (event.rows ?? 20)) + 1;
    this.pageSize = event.rows ?? 20;
    if (event.sortField) {
      this.sortField = Array.isArray(event.sortField) ? event.sortField[0] : event.sortField;
      this.sortOrder = event.sortOrder ?? -1;
    }
    this.loadData();
  }

  onSearch(keyword?: string): void {
    if (keyword !== undefined) {
      this.searchKeyword = keyword;
    }
    this.pageNumber = 1;
    if (this.tblPurchaseOrders) {
      this.tblPurchaseOrders.first = 0;
    }
    this.loadData();
  }

  onDateChange(): void {
    this.pageNumber = 1;
    if (this.tblPurchaseOrders) {
      this.tblPurchaseOrders.first = 0;
    }
    this.loadData();
  }

  onStatusChange(): void {
    this.pageNumber = 1;
    if (this.tblPurchaseOrders) {
      this.tblPurchaseOrders.first = 0;
    }
    this.loadData();
  }

  clearSearch(): void {
    this.searchKeyword = '';
    this.onSearch();
  }

  viewDetails(order?: PurchaseOrderModel): void {
    const target = order ?? this.selectedPurchaseOrder;
    if (!target) {
      this.messageService.add({
        key: 'globalMessage',
        severity: 'warn',
        summary: 'Warning',
        detail: 'Please select a purchase order.'
      });
      return;
    }

    this.detailOrder = target;
    this.detailModalVisible = true;
  }

  previewEmail(order?: PurchaseOrderModel): void {
    if (this.isSendingEmail || this.isLoadingEmailPreview) {
      return;
    }

    const target = order ?? this.selectedPurchaseOrder;
    if (!target?.id) {
      this.messageService.add({
        key: 'globalMessage',
        severity: 'warn',
        summary: 'Warning',
        detail: 'Please select a purchase order.'
      });
      return;
    }

    const requestId = ++this.emailPreviewRequestId;
    this.emailOrder = target;
    this.emailStatus = '';
    this.acceptedEmailId = null;
    this.emailPreview = null;
    this.emailError = '';
    this.emailModalVisible = true;
    this.isLoadingEmailPreview = true;

    this.purchaseOrdersService.getEmailPreview(target.id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (res) => {
          if (requestId !== this.emailPreviewRequestId) return;
          this.isLoadingEmailPreview = false;
          if (res.success && res.data) {
            this.emailPreview = res.data;
          } else {
            this.emailError = res.message || 'Unable to load the email preview.';
          }
          this.cdr.markForCheck();
        },
        error: (err) => {
          if (requestId !== this.emailPreviewRequestId) return;
          this.isLoadingEmailPreview = false;
          this.emailError = err.error?.message || 'Unable to load the email preview.';
          this.cdr.markForCheck();
        }
      });
  }

  sendOrderEmail(): void {
    if (this.isSendingEmail || this.isLoadingEmailPreview || this.acceptedEmailId || !this.emailPreview || !this.emailOrder?.id) {
      return;
    }

    this.isSendingEmail = true;
    this.emailError = '';
    const orderId = this.emailOrder.id;
    const requestKey = this.emailRequestKeys.get(orderId) ?? crypto.randomUUID();
    this.emailRequestKeys.set(orderId, requestKey);
    this.purchaseOrdersService.sendEmail(orderId, requestKey)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (res) => {
          this.isSendingEmail = false;
          if (res.success) {
            this.emailRequestKeys.delete(orderId);
            this.acceptedEmailId = res.data.emailId;
            this.emailStatus = 'Pending';
            this.watchEmailStatus(orderId, res.data.emailId);
            this.messageService.add({
              key: 'globalMessage',
              severity: 'success',
              summary: 'Success',
              detail: 'Email queued. Sending will continue in the background.'
            });
          } else {
            this.emailError = res.message || 'Failed to send the purchase order email.';
          }
          this.cdr.markForCheck();
        },
        error: (err) => {
          this.isSendingEmail = false;
          this.emailError = err.error?.message || 'Failed to send the purchase order email.';
          this.cdr.markForCheck();
        }
      });
  }

  onEmailDialogHide(): void {
    this.emailStatusClosed.next();
    this.emailStatus = '';
    this.acceptedEmailId = null;
    ++this.emailPreviewRequestId;
    this.isLoadingEmailPreview = false;
    this.emailPreview = null;
    this.emailOrder = null;
    this.emailError = '';
  }

  private watchEmailStatus(orderId: string, emailId: string): void {
    this.emailStatusClosed.next();
    timer(0, 10000).pipe(
      exhaustMap(() => this.purchaseOrdersService.getEmailStatus(orderId, emailId)),
      take(13),
      takeWhile(res => res.data.status === 'Pending' || res.data.status === 'Sending', true),
      takeUntil(this.emailStatusClosed),
      takeUntilDestroyed(this.destroyRef)
    ).subscribe({
      next: (res) => {
        this.emailStatus = res.data.status;
        if (res.data.status === 'Failed') {
          this.emailError = 'Email sending failed. Review the delivery result before creating another send request.';
        }
        this.cdr.markForCheck();
      },
      error: () => {
        this.emailError = 'Unable to check status. Your queued email request is retained.';
        this.cdr.markForCheck();
      },
      complete: () => {
        if (this.emailModalVisible && (this.emailStatus === 'Pending' || this.emailStatus === 'Sending')) {
          this.emailError = 'Automatic status checks stopped. Your email is still queued; use Check Status to check again.';
          this.cdr.markForCheck();
        }
      }
    });
  }

  checkEmailStatus(): void {
    if (!this.emailOrder?.id || !this.acceptedEmailId) return;
    this.emailError = '';
    this.purchaseOrdersService.getEmailStatus(this.emailOrder.id, this.acceptedEmailId)
      .pipe(takeUntil(this.emailStatusClosed), takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: res => {
          this.emailStatus = res.data.status;
          if (res.data.status === 'Failed') this.emailError = 'Email sending failed. Review delivery before resending.';
          this.cdr.markForCheck();
        },
        error: () => {
          this.emailError = 'Unable to check email status.';
          this.cdr.markForCheck();
        }
      });
  }

  printOrder(order?: PurchaseOrderModel): void {
    const po = order ?? this.detailOrder ?? this.selectedPurchaseOrder;
    if (!po) {
      this.messageService.add({
        key: 'globalMessage',
        severity: 'warn',
        summary: 'Warning',
        detail: 'Please select a purchase order to print.'
      });
      return;
    }

    this.printOrderData = po;
    this.printModalVisible = true;
  }

  addItem(): void {
    this.formItems.push(this.createEmptyItem());
    this.recalculateTotalAmount();
  }

  removeItem(index: number): void {
    if (this.formItems.length > 1) {
      this.formItems.splice(index, 1);
      this.recalculateTotalAmount();
    } else {
      this.messageService.add({
        key: 'globalMessage',
        severity: 'warn',
        summary: 'Warning',
        detail: 'A purchase order must contain at least one item.'
      });
    }
  }

  onProductSelected(item: PurchaseOrderItemModel): void {
    const prod = this.products.find(p => p.id === item.productId);
    if (prod) {
      item.productId = prod.id!;
      item.productName = prod.name;
      item.productSku = prod.sku ?? '';
      item.unitPrice = prod.costPrice ?? 0;
      if (prod.baseUomId) {
        item.uomId = prod.baseUomId;
      }
      this.calculateSubTotal(item);
    }
  }

  calculateSubTotal(item: PurchaseOrderItemModel): void {
    item.subTotal = (item.quantity || 0) * (item.unitPrice || 0);
    this.recalculateTotalAmount();
  }

  recalculateTotalAmount(): void {
    this.formTotalAmount = this.formItems.reduce((acc, curr) => acc + ((curr.quantity || 0) * (curr.unitPrice || 0)), 0);
  }

  getTotalAmount(): number {
    return this.formTotalAmount;
  }

  getStatusSeverity(status: PurchaseOrderStatus): 'warn' | 'info' | 'success' | 'danger' | 'secondary' {
    switch (status) {
      case PurchaseOrderStatus.Pending:
        return 'warn';
      case PurchaseOrderStatus.PartiallyReceived:
        return 'info';
      case PurchaseOrderStatus.Completed:
        return 'success';
      case PurchaseOrderStatus.Cancelled:
        return 'danger';
      default:
        return 'secondary';
    }
  }

  getStatusIcon(status: PurchaseOrderStatus): string {
    switch (status) {
      case PurchaseOrderStatus.Pending:
        return 'pi pi-clock';
      case PurchaseOrderStatus.PartiallyReceived:
        return 'pi pi-truck';
      case PurchaseOrderStatus.Completed:
        return 'pi pi-check-circle';
      case PurchaseOrderStatus.Cancelled:
        return 'pi pi-times-circle';
      default:
        return 'pi pi-info-circle';
    }
  }

  getStatusLabel(status: PurchaseOrderStatus): string {
    switch (status) {
      case PurchaseOrderStatus.Pending:
        return 'Pending';
      case PurchaseOrderStatus.PartiallyReceived:
        return 'Partially Received';
      case PurchaseOrderStatus.Completed:
        return 'Completed';
      case PurchaseOrderStatus.Cancelled:
        return 'Cancelled';
      default:
        return 'Unknown';
    }
  }

  openCancellation(): void {
    if (!this.selectedPurchaseOrder?.id || this.selectedPurchaseOrder.status !== PurchaseOrderStatus.Pending) {
      this.messageService.add({ key: 'globalMessage', severity: 'warn', summary: 'Cancel Order', detail: 'Select an unreceived, pending purchase order.' });
      return;
    }
    this.cancelOrderId = this.selectedPurchaseOrder.id;
    this.cancellationReason = '';
    this.cancelModalVisible = true;
  }

  cancelOrder(): void {
    const reason = this.cancellationReason.trim();
    if (!this.cancelOrderId || this.isCancelling || !reason || reason.length > 500) return;
    this.isCancelling = true;
    this.purchaseOrdersService.cancel(this.cancelOrderId, reason).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => {
        this.isCancelling = false;
        this.cancelModalVisible = false;
        this.selectedPurchaseOrder = null;
        this.loadData();
        this.messageService.add({ key: 'globalMessage', severity: 'success', summary: 'Cancelled', detail: 'Purchase order cancelled.' });
        this.cdr.markForCheck();
      },
      error: (err) => {
        this.isCancelling = false;
        this.messageService.add({ key: 'globalMessage', severity: 'error', summary: 'Error', detail: err.error?.message || 'Unable to cancel purchase order.' });
        this.cdr.markForCheck();
      }
    });
  }
}
