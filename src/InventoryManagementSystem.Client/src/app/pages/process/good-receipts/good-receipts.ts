import { ChangeDetectorRef, Component, inject, OnInit, ViewChild } from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';
import { FormBuilder, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';
import { forkJoin } from 'rxjs';
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

import { GoodReceiptModel, GoodReceiptItemModel } from '../../../core/models/process/good-receipt.model';
import { PurchaseOrderModel } from '../../../core/models/process/purchase-order.model';
import { SuppliersModel } from '../../../core/models/master/suppliers.model';
import { WarehouseModel } from '../../../core/models/master/warehouse.model';
import { ProductModel } from '../../../core/models/master/product.model';
import { UnitOfMeasureModel } from '../../../core/models/master/unit-of-measure.model';

import { GoodReceiptsService } from '../../../core/services/process/good-receipts.service';
import { PurchaseOrdersService } from '../../../core/services/process/purchase-orders.service';
import { SuppliersService } from '../../../core/services/master/suppliers.service';
import { WarehouseService } from '../../../core/services/master/warehouse.service';
import { ProductService } from '../../../core/services/master/product.service';
import { UnitOfMeasureService } from '../../../core/services/master/unit-of-measure.service';
import { ExportService } from '../../../core/services/export.service';

@Component({
  selector: 'app-good-receipts',
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
    DatePickerModule
  ],
  providers: [ConfirmationService, MessageService, DatePipe, ExportService],
  templateUrl: './good-receipts.html',
  styleUrl: './good-receipts.scss',
})
export class GoodReceiptsComponent implements OnInit {
  @ViewChild(Table) tblGoodReceipts!: Table;

  items: MenuItem[] = [];
  isLoading: boolean = false;
  isSubmitting: boolean = false;
  isEdit: boolean = false;
  modalVisible: boolean = false;
  detailModalVisible: boolean = false;

  goodReceipts: GoodReceiptModel[] = [];
  selectedGoodReceipt: GoodReceiptModel | null = null;
  detailReceipt: GoodReceiptModel | null = null;

  // Server-side Pagination & Filter state
  totalRecords: number = 0;
  pageNumber: number = 1;
  pageSize: number = 20;
  sortField: string = 'createdOn';
  sortOrder: number = -1;
  searchKeyword: string = '';
  filterReceiptDate: Date | null = null;

  // Dropdown master lists
  purchaseOrders: PurchaseOrderModel[] = [];
  suppliers: SuppliersModel[] = [];
  warehouses: WarehouseModel[] = [];
  products: ProductModel[] = [];
  uoms: UnitOfMeasureModel[] = [];

  // Line items state for the dialog form
  formItems: GoodReceiptItemModel[] = [];

  private formBuilder = inject(FormBuilder);
  public goodReceiptForm = this.formBuilder.group({
    id: [null as string | null],
    receiptNo: ['', Validators.required],
    purchaseOrderId: ['', Validators.required],
    supplierId: [null as number | null, [Validators.required, Validators.min(1)]],
    warehouseId: [null as number | null, [Validators.required, Validators.min(1)]],
    receiptDate: [new Date(), Validators.required],
    status: [true],
    receivedBy: ['', Validators.required],
    note: ['']
  });

  constructor(
    private goodReceiptsService: GoodReceiptsService,
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
    forkJoin({
      purchaseOrders: this.purchaseOrdersService.getPaged({ pageSize: 100 }),
      suppliers: this.suppliersService.get(),
      warehouses: this.warehouseService.get(),
      products: this.productService.get(),
      uoms: this.uomService.get()
    }).subscribe({
      next: (res) => {
        this.purchaseOrders = (res.purchaseOrders?.data?.items || []) as PurchaseOrderModel[];
        this.suppliers = (res.suppliers?.data || []) as SuppliersModel[];
        this.warehouses = (res.warehouses?.data || []) as WarehouseModel[];
        this.products = (res.products?.data || []) as ProductModel[];
        this.uoms = (res.uoms?.data || []) as UnitOfMeasureModel[];
        this.cdr.markForCheck();
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
    const filter = {
      q: this.searchKeyword || undefined,
      receiptDate: this.filterReceiptDate ? (this.datePipe.transform(this.filterReceiptDate, 'yyyy-MM-dd') ?? undefined) : undefined,
      sortField: this.sortField,
      order: this.sortOrder,
      pageNumber: this.pageNumber,
      pageSize: this.pageSize
    };

    this.goodReceiptsService.getPaged(filter).subscribe({
      next: (res) => {
        this.goodReceipts = (res.data?.items || []) as GoodReceiptModel[];
        this.totalRecords = res.data?.totalRecords || 0;
        this.isLoading = false;
        this.selectedGoodReceipt = null;
        this.cdr.markForCheck();
      },
      error: () => {
        this.isLoading = false;
        this.cdr.markForCheck();
      }
    });
  }

  onSearch(): void {
    this.pageNumber = 1;
    if (this.tblGoodReceipts) {
      this.tblGoodReceipts.first = 0;
    }
    this.loadData();
  }

  onDateChange(): void {
    this.pageNumber = 1;
    if (this.tblGoodReceipts) {
      this.tblGoodReceipts.first = 0;
    }
    this.loadData();
  }

  create(): void {
    this.isEdit = false;
    this.formItems = [];
    const autoReceiptNo = 'GRN-' + new Date().getTime().toString().slice(-6);

    this.goodReceiptForm.reset({
      id: null,
      receiptNo: autoReceiptNo,
      purchaseOrderId: '',
      supplierId: null,
      warehouseId: null,
      receiptDate: new Date(),
      status: true,
      receivedBy: '',
      note: ''
    });

    this.modalVisible = true;
  }

  onPurchaseOrderSelected(poId: string): void {
    if (!poId) return;

    this.purchaseOrdersService.getById(poId).subscribe({
      next: (res) => {
        const po = res.data as PurchaseOrderModel;
        if (po) {
          this.goodReceiptForm.patchValue({
            supplierId: po.supplierId,
            warehouseId: po.warehouseId
          });

          // Auto populate line items from PO
          this.formItems = (po.items || []).map((item) => ({
            purchaseOrderItemId: item.id || 0,
            productId: item.productId,
            productName: item.productName,
            productSku: item.productSku,
            uomId: item.uomId,
            uomName: item.uomName,
            receivedQuantity: Math.max(0, item.quantity - (item.receivedQuantity || 0))
          }));

          this.cdr.markForCheck();
        }
      }
    });
  }

  addItem(): void {
    this.formItems.push({
      purchaseOrderItemId: 0,
      productId: '',
      uomId: 0,
      receivedQuantity: 1
    });
  }

  removeItem(index: number): void {
    this.formItems.splice(index, 1);
  }

  onProductSelected(item: GoodReceiptItemModel): void {
    const selectedProd = this.products.find(p => p.id === item.productId);
    if (selectedProd) {
      item.productName = selectedProd.name;
      item.productSku = selectedProd.sku;
      if (selectedProd.baseUomId) {
        item.uomId = selectedProd.baseUomId;
      }
    }
  }

  update(): void {
    if (!this.selectedGoodReceipt) {
      this.messageService.add({
        key: 'globalMessage',
        severity: 'warn',
        summary: 'Warning',
        detail: 'Please select a Goods Receipt to edit.'
      });
      return;
    }

    this.isEdit = true;
    this.goodReceiptsService.getById(this.selectedGoodReceipt.id!).subscribe({
      next: (res) => {
        const receipt = res.data as GoodReceiptModel;
        if (!receipt) return;

        this.goodReceiptForm.patchValue({
          id: receipt.id,
          receiptNo: receipt.receiptNo,
          purchaseOrderId: receipt.purchaseOrderId,
          supplierId: receipt.supplierId,
          warehouseId: receipt.warehouseId,
          receiptDate: receipt.receiptDate ? new Date(receipt.receiptDate) : new Date(),
          status: receipt.status,
          receivedBy: receipt.receivedBy,
          note: receipt.note || ''
        });

        this.formItems = (receipt.items || []).map(i => ({ ...i }));
        this.modalVisible = true;
        this.cdr.markForCheck();
      }
    });
  }

  delete(): void {
    if (!this.selectedGoodReceipt) {
      this.messageService.add({
        key: 'globalMessage',
        severity: 'warn',
        summary: 'Warning',
        detail: 'Please select a Goods Receipt to delete.'
      });
      return;
    }

    this.confirmationService.confirm({
      key: 'positionDialog',
      header: 'Delete Confirmation',
      message: `Are you sure you want to delete Goods Receipt '${this.selectedGoodReceipt.receiptNo}'? This will revert received quantities and inventory.`,
      icon: 'pi pi-exclamation-triangle',
      acceptLabel: 'Yes',
      rejectLabel: 'No',
      accept: () => {
        this.goodReceiptsService.delete(this.selectedGoodReceipt!.id!).subscribe({
          next: () => {
            this.messageService.add({
              key: 'globalMessage',
              severity: 'success',
              summary: 'Success',
              detail: 'Goods Receipt deleted successfully.'
            });
            this.loadData();
          },
          error: (err) => {
            this.messageService.add({
              key: 'globalMessage',
              severity: 'error',
              summary: 'Error',
              detail: err.error?.message || 'Failed to delete Goods Receipt.'
            });
          }
        });
      }
    });
  }

  viewDetails(receipt: GoodReceiptModel): void {
    this.goodReceiptsService.getById(receipt.id!).subscribe({
      next: (res) => {
        this.detailReceipt = res.data as GoodReceiptModel;
        this.detailModalVisible = true;
        this.cdr.markForCheck();
      }
    });
  }

  onSubmit(): void {
    if (this.goodReceiptForm.invalid) {
      this.goodReceiptForm.markAllAsTouched();
      return;
    }

    if (!this.isEdit && this.formItems.length === 0) {
      this.messageService.add({
        key: 'globalMessage',
        severity: 'warn',
        summary: 'Validation',
        detail: 'At least one line item is required.'
      });
      return;
    }

    const formVal = this.goodReceiptForm.value;
    const model: GoodReceiptModel = {
      id: formVal.id || undefined,
      receiptNo: formVal.receiptNo!.trim(),
      purchaseOrderId: formVal.purchaseOrderId!,
      supplierId: Number(formVal.supplierId),
      warehouseId: Number(formVal.warehouseId),
      receiptDate: formVal.receiptDate || new Date(),
      status: !!formVal.status,
      receivedBy: formVal.receivedBy!.trim(),
      note: formVal.note?.trim() || undefined,
      items: this.formItems.map(i => ({
        id: i.id,
        purchaseOrderItemId: i.purchaseOrderItemId,
        productId: i.productId,
        uomId: Number(i.uomId),
        receivedQuantity: Number(i.receivedQuantity)
      }))
    };

    this.isSubmitting = true;
    const request$ = this.isEdit
      ? this.goodReceiptsService.update(model)
      : this.goodReceiptsService.create(model);

    request$.subscribe({
      next: () => {
        this.messageService.add({
          key: 'globalMessage',
          severity: 'success',
          summary: 'Success',
          detail: `Goods Receipt ${this.isEdit ? 'updated' : 'created'} successfully.`
        });
        this.isSubmitting = false;
        this.modalVisible = false;
        this.loadData();
      },
      error: (err) => {
        this.isSubmitting = false;
        this.messageService.add({
          key: 'globalMessage',
          severity: 'error',
          summary: 'Error',
          detail: err.error?.message || 'Operation failed.'
        });
        this.cdr.markForCheck();
      }
    });
  }

  excel(): void {
    const filter = {
      q: this.searchKeyword || undefined,
      receiptDate: this.filterReceiptDate ? (this.datePipe.transform(this.filterReceiptDate, 'yyyy-MM-dd') ?? undefined) : undefined
    };

    this.goodReceiptsService.export(filter).subscribe({
      next: (blob) => {
        const url = window.URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = `GoodsReceipts_${this.datePipe.transform(new Date(), 'yyyyMMdd_HHmmss')}.xlsx`;
        a.click();
        window.URL.revokeObjectURL(url);
      },
      error: () => {
        this.messageService.add({
          key: 'globalMessage',
          severity: 'error',
          summary: 'Error',
          detail: 'Failed to export Goods Receipts.'
        });
      }
    });
  }

  onDialogHide(): void {
    this.goodReceiptForm.reset();
    this.formItems = [];
    this.isEdit = false;
  }
}
