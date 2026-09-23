import { ChangeDetectorRef, Component, inject, OnInit, ViewChild } from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';
import { FormBuilder, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';
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

import { PurchaseOrderModel, PurchaseOrderItemModel } from '../../../core/models/process/purchase-order.model';
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
  modalVisible: boolean = false;
  detailModalVisible: boolean = false;
  printModalVisible: boolean = false;

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

  // Dropdown master lists
  suppliers: SuppliersModel[] = [];
  warehouses: WarehouseModel[] = [];
  products: ProductModel[] = [];
  uoms: UnitOfMeasureModel[] = [];

  // Line items state for the dialog form
  formItems: PurchaseOrderItemModel[] = [];

  private formBuilder = inject(FormBuilder);
  public purchaseOrderForm = this.formBuilder.group({
    id: [null as string | null],
    purchaseOrderNo: ['', Validators.required],
    supplierId: [null as number | null, [Validators.required, Validators.min(1)]],
    warehouseId: [null as number | null, [Validators.required, Validators.min(1)]],
    orderDate: [new Date(), Validators.required],
    expectedDate: [new Date(), Validators.required],
    status: [true],
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
        label: 'Excel',
        icon: 'pi pi-file-excel',
        command: () => this.excel()
      }
    ];
  }

  ngOnInit(): void {
    this.loadDropdownData();
  }

  loadDropdownData(): void {
    this.suppliersService.get().subscribe({
      next: (res) => {
        this.suppliers = (res.data || []) as SuppliersModel[];
      }
    });

    this.warehouseService.get().subscribe({
      next: (res) => {
        this.warehouses = (res.data || []) as WarehouseModel[];
      }
    });

    this.productService.get().subscribe({
      next: (res) => {
        this.products = (res.data || []) as ProductModel[];
      }
    });

    this.uomService.get().subscribe({
      next: (res) => {
        this.uoms = (res.data || []) as UnitOfMeasureModel[];
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

  loadData(): void {
    this.isLoading = true;
    this.purchaseOrdersService.getPaged({
      q: this.searchKeyword,
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

  onSearch(keyword: string): void {
    this.searchKeyword = keyword;
    this.pageNumber = 1;
    if (this.tblPurchaseOrders) {
      this.tblPurchaseOrders.first = 0;
    }
    this.loadData();
  }

  create(): void {
    this.isEdit = false;
    this.selectedPurchaseOrder = null;
    this.purchaseOrderForm.reset({
      id: null,
      purchaseOrderNo: this.generatePoNumber(),
      supplierId: null,
      warehouseId: null,
      orderDate: new Date(),
      expectedDate: new Date(Date.now() + 7 * 24 * 60 * 60 * 1000), // Default 7 days later
      status: true
    });
    this.formItems = [this.createEmptyItem()];
    this.modalVisible = true;
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
      receivedQuantity: i.receivedQuantity ?? 0,
      subTotal: i.quantity * i.unitPrice
    }));

    if (this.formItems.length === 0) {
      this.formItems = [this.createEmptyItem()];
    }

    this.modalVisible = true;
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

  addItem(): void {
    this.formItems.push(this.createEmptyItem());
  }

  removeItem(index: number): void {
    if (this.formItems.length > 1) {
      this.formItems.splice(index, 1);
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
  }

  getTotalAmount(): number {
    return this.formItems.reduce((acc, curr) => acc + ((curr.quantity || 0) * (curr.unitPrice || 0)), 0);
  }

  onSubmit(): void {
    if (this.purchaseOrderForm.invalid) {
      Object.keys(this.purchaseOrderForm.controls).forEach(field => {
        const control = this.purchaseOrderForm.get(field);
        control?.markAsDirty({ onlySelf: true });
      });
      return;
    }

    // Validate line items
    const invalidItem = this.formItems.find(i => !i.productId || i.quantity <= 0 || !i.uomId);
    if (invalidItem) {
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

    const payload: PurchaseOrderModel = {
      id: formVal.id ?? undefined,
      purchaseOrderNo: formVal.purchaseOrderNo!,
      supplierId: formVal.supplierId!,
      warehouseId: formVal.warehouseId!,
      orderDate: formVal.orderDate ? (this.datePipe.transform(formVal.orderDate, 'yyyy-MM-ddTHH:mm:ss') ?? new Date().toISOString()) : new Date().toISOString(),
      expectedDate: formVal.expectedDate ? (this.datePipe.transform(formVal.expectedDate, 'yyyy-MM-ddTHH:mm:ss') ?? new Date().toISOString()) : new Date().toISOString(),
      status: formVal.status ?? true,
      items: this.formItems.map(i => ({
        id: i.id,
        productId: i.productId,
        quantity: i.quantity,
        unitPrice: i.unitPrice,
        uomId: i.uomId,
        receivedQuantity: i.receivedQuantity ?? 0
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

  onDialogHide(): void {
    this.modalVisible = false;
    this.selectedPurchaseOrder = null;
    this.isEdit = false;
    this.formItems = [];
  }

  excel(): void {
    this.purchaseOrdersService.export({
      q: this.searchKeyword
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

  private generatePoNumber(): string {
    const today = new Date();
    const dateStr = this.datePipe.transform(today, 'yyyyMMdd') ?? '';
    const rand = Math.floor(1000 + Math.random() * 9000);
    return `PO-${dateStr}-${rand}`;
  }
}
