import { CommonModule, CurrencyPipe, DatePipe } from '@angular/common';
import { ChangeDetectorRef, Component, inject, OnInit, signal, ViewChild } from '@angular/core';
import { FormBuilder, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';
import { CategoryModel } from '../../../core/models/category.model';
import { ExportColumnModel } from '../../../core/models/export-column.model';
import { ProductModel } from '../../../core/models/product.model';
import { CategoryService } from '../../../core/services/category.service';
import { ExportService } from '../../../core/services/export.service';
import { LoggerService } from '../../../core/services/logger.service';
import { ProductService } from '../../../core/services/product.service';
import { SharedService } from '../../../core/services/shared.service';
import { ConfirmationService, MenuItem, MessageService } from 'primeng/api';
import { ButtonModule } from 'primeng/button';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { DialogModule } from 'primeng/dialog';
import { IconFieldModule } from 'primeng/iconfield';
import { InputIconModule } from 'primeng/inputicon';
import { InputTextModule } from 'primeng/inputtext';
import { MessageModule } from 'primeng/message';
import { SelectModule } from 'primeng/select';
import { SplitButtonModule } from 'primeng/splitbutton';
import { Table, TableModule } from 'primeng/table';
import { TagModule } from 'primeng/tag';
import { ToastModule } from 'primeng/toast';
import { ToggleSwitchModule } from 'primeng/toggleswitch';
import { TextareaModule } from 'primeng/textarea';
import { Barcode } from "../../../shared/components/barcode/barcode";

@Component({
  selector: 'app-products',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    ReactiveFormsModule,
    SplitButtonModule,
    TagModule,
    DialogModule,
    ConfirmDialogModule,
    MessageModule,
    IconFieldModule,
    InputTextModule,
    SelectModule,
    ButtonModule,
    TableModule,
    ToastModule,
    InputIconModule,
    ToggleSwitchModule,
    TextareaModule,
    Barcode
  ],
  providers: [DatePipe, CurrencyPipe, ConfirmationService, ExportService, MessageService],
  templateUrl: './products.html',
  styleUrl: './products.scss',
})
export class Products implements OnInit {
  @ViewChild(Table) tblProducts!: Table;

  products: ProductModel[] = [];
  filteredProducts: ProductModel[] = [];
  categories: CategoryModel[] = [];
  selectedProduct!: ProductModel;
  errorMessage = signal<any[]>([]);

  items!: MenuItem[];
  modalVisible: boolean = false;
  barcodeModalVisible: boolean = false;
  selectedBarcodeValue: string | null = null;
  selectedProductForBarcode: ProductModel | null = null;
  isEdit: boolean = false;
  isLoading: boolean = false;
  isSubmitting: boolean = false;

  // Category filter select box state
  selectedCategoryId: number | null = null;

  // Dynamic Sticker Size (mm)
  stickerWidthMm: number = 50;
  stickerHeightMm: number = 30;
  stickerPresets = [
    { label: '50mm × 30mm (Standard)', width: 50, height: 30 },
    { label: '40mm × 30mm (Small)', width: 40, height: 30 },
    { label: '60mm × 40mm (Medium)', width: 60, height: 40 },
    { label: '70mm × 50mm (Large)', width: 70, height: 50 },
    { label: '80mm × 50mm (Shelf/Bin)', width: 80, height: 50 },
    { label: '100mm × 50mm (Shipping/Box)', width: 100, height: 50 },
  ];

  onStickerPresetChange(preset: { label: string; width: number; height: number }): void {
    if (preset) {
      this.stickerWidthMm = preset.width;
      this.stickerHeightMm = preset.height;
    }
  }

  private formBuilder = inject(FormBuilder);
  public productForm = this.formBuilder.group({
    id: [null as string | null],
    name: ['', Validators.required],
    categoryId: [null as number | null, Validators.required],
    sku: [''],
    barcode: [''],
    brand: [''],
    unit: [''],
    costPrice: [0, [Validators.required, Validators.min(0)]],
    sellingPrice: [0, [Validators.required, Validators.min(0)]],
    currentStock: [0, [Validators.required, Validators.min(0)]],
    reorderLevel: [10, [Validators.required, Validators.min(0)]],
    reorderQuantity: [0, [Validators.required, Validators.min(0)]],
    tax: [0, [Validators.required, Validators.min(0)]],
    status: [true],
    description: [''],
  });

  constructor(
    private shareService: SharedService,
    private productService: ProductService,
    private categoryService: CategoryService,
    private messageService: MessageService,
    private confirmationService: ConfirmationService,
    private loggerService: LoggerService,
    private datePipe: DatePipe,
    private exportService: ExportService,
    private cdr: ChangeDetectorRef
  ) {
    this.items = [
      {
        label: 'Update',
        icon: 'pi pi-pencil',
        command: () => this.update(),
      },
      {
        label: 'Delete',
        icon: 'pi pi-trash',
        command: () => this.delete(),
      },
      {
        label: 'Excel',
        icon: 'pi pi-file-excel',
        command: () => this.excel(),
      },
    ];
  }

  ngOnInit(): void {
    this.loadMasterData();
    this.loadData();
  }

  loadMasterData(): void {
    this.categoryService.get().subscribe({
      next: res => {
        this.categories = (res.data || []) as CategoryModel[];
        this.cdr.detectChanges();
      },
    });
  }

  loadData(): void {
    this.isLoading = true;
    this.productService.get().subscribe({
      next: res => {
        this.products = (res.data || []) as ProductModel[];
        this.onCategoryFilterChange();
        this.isLoading = false;
        this.cdr.detectChanges();
      },
      error: err => {
        this.loggerService.error('Product API error', err);
        this.isLoading = false;
      },
    });
  }

  onCategoryFilterChange(): void {
    if (this.selectedCategoryId == null || this.selectedCategoryId === 0) {
      this.filteredProducts = [...this.products];
    } else {
      this.filteredProducts = this.products.filter(
        p => Number(p.categoryId) === Number(this.selectedCategoryId)
      );
    }
    this.cdr.detectChanges();
  }

  create(): void {
    this.isEdit = false;
    this.productForm.reset();
    this.errorMessage.set([]);
    this.productForm.patchValue({
      id: null,
      name: '',
      categoryId: this.selectedCategoryId && this.selectedCategoryId > 0 ? this.selectedCategoryId : null,
      sku: '',
      barcode: '',
      brand: '',
      unit: '',
      costPrice: 0,
      sellingPrice: 0,
      currentStock: 0,
      reorderLevel: 10,
      reorderQuantity: 0,
      tax: 0,
      status: true,
      description: '',
    });
    this.modalVisible = true;
  }

  update(): void {
    this.isEdit = true;
    this.productForm.reset();

    if (this.selectedProduct != null) {
      const prodId = this.selectedProduct.id ? String(this.selectedProduct.id) : null;
      this.productForm.patchValue({
        id: prodId,
        name: this.selectedProduct.name,
        categoryId: Number(this.selectedProduct.categoryId),
        sku: this.selectedProduct.sku || '',
        barcode: this.selectedProduct.barcode || '',
        brand: this.selectedProduct.brand || '',
        unit: this.selectedProduct.unit || '',
        costPrice: this.selectedProduct.costPrice ?? 0,
        sellingPrice: this.selectedProduct.sellingPrice ?? 0,
        currentStock: this.selectedProduct.currentStock ?? 0,
        reorderLevel: this.selectedProduct.reorderLevel ?? 0,
        reorderQuantity: this.selectedProduct.reorderQuantity ?? 0,
        tax: this.selectedProduct.tax ?? 0,
        status: this.selectedProduct.status !== undefined ? Boolean(this.selectedProduct.status) : true,
        description: this.selectedProduct.description || '',
      });
      this.modalVisible = true;
      this.errorMessage.set([]);
    } else {
      this.messageService.add({
        key: 'globalMessage',
        severity: 'warn',
        summary: 'Warning',
        detail: 'Please Select a Product',
      });
    }
  }

  delete(): void {
    if (this.selectedProduct != null) {
      const prodId = this.selectedProduct.id ? String(this.selectedProduct.id) : '';
      this.confirmationService.confirm({
        message: 'Are you sure you want to delete this product?',
        header: 'Delete Confirmation',
        icon: 'pi pi-info-circle',
        key: 'positionDialog',
        accept: () => {
          this.productService.delete(prodId).subscribe({
            next: res => {
              this.messageService.add({
                key: 'globalMessage',
                severity: 'success',
                summary: 'Confirmed',
                detail: res.message ?? 'Product deleted successfully.',
              });
              this.loadData();
              this.selectedProduct = null as any;
              this.cdr.detectChanges();
            },
          });
        },
        reject: () => {
          this.selectedProduct = null as any;
        },
      });
    } else {
      this.messageService.add({
        key: 'globalMessage',
        severity: 'warn',
        summary: 'Warning',
        detail: 'Please Select a Product',
      });
    }
  }

  onSubmit(): void {
    if (!this.productForm.valid) {
      Object.keys(this.productForm.controls).forEach(field => {
        this.productForm.get(field)?.markAsDirty({ onlySelf: true });
      });
      return;
    }

    this.isSubmitting = true;
    const formVal = this.productForm.value;
    const payload = {
      name: formVal.name,
      categoryId: formVal.categoryId,
      sku: formVal.sku,
      barcode: formVal.barcode,
      brand: formVal.brand,
      unit: formVal.unit,
      costPrice: formVal.costPrice,
      sellingPrice: formVal.sellingPrice,
      currentStock: formVal.currentStock,
      reorderLevel: formVal.reorderLevel,
      reorderQuantity: formVal.reorderQuantity,
      tax: formVal.tax,
      status: formVal.status ?? true,
      description: formVal.description,
    };

    if (!this.isEdit) {
      this.productService.create(payload).subscribe({
        next: res => {
          if (res.success) {
            this.modalVisible = false;
            this.loadData();
            this.messageService.add({
              key: 'globalMessage',
              severity: 'info',
              summary: 'Success',
              detail: res.message ? res.message.toString() : 'Product created successfully',
            });
            this.cdr.detectChanges();
          }
          this.isSubmitting = false;
        },
        error: () => { this.isSubmitting = false; },
      });
    } else {
      const updatePayload = {
        ...payload,
        id: formVal.id,
      };

      this.productService.update(updatePayload).subscribe({
        next: res => {
          if (res.success) {
            this.modalVisible = false;
            this.loadData();
            this.selectedProduct = null as any;
            this.messageService.add({
              key: 'globalMessage',
              severity: 'info',
              summary: 'Success',
              detail: res.message ? res.message.toString() : 'Product updated successfully',
            });
            this.cdr.detectChanges();
          }
          this.isSubmitting = false;
        },
        error: () => { this.isSubmitting = false; },
      });
    }
  }

  ViewBarcode(product: ProductModel): void {
    this.selectedProductForBarcode = product;
    this.selectedBarcodeValue = product.barcode || product.sku || null;
    this.barcodeModalVisible = true;
  }

  onDialogHide(): void {
    this.selectedProduct = null as any;
    this.modalVisible = false;
  }

  onBarcodeDialogHide(): void {
    this.selectedBarcodeValue = null;
    this.selectedProductForBarcode = null;
    this.barcodeModalVisible = false;
  }

  printSticker(): void {
    const stickerEl = document.getElementById('product-barcode-sticker');
    if (!stickerEl) return;

    const printWindow = window.open('', '_blank', 'width=500,height=400');
    if (!printWindow) {
      window.print();
      return;
    }

    const wMm = this.stickerWidthMm || 50;
    const hMm = this.stickerHeightMm || 30;

    printWindow.document.open();
    printWindow.document.write(`
      <!DOCTYPE html>
      <html>
        <head>
          <title>Print Product Sticker - ${this.selectedProductForBarcode?.name || 'Product'}</title>
          <style>
            @page {
              size: ${wMm}mm ${hMm}mm;
              margin: 0mm;
            }
            * {
              box-sizing: border-box;
              -webkit-print-color-adjust: exact !important;
              print-color-adjust: exact !important;
            }
            html, body {
              margin: 0 !important;
              padding: 0 !important;
              width: ${wMm}mm;
              height: ${hMm}mm;
              font-family: system-ui, -apple-system, sans-serif;
              background: #fff;
              overflow: hidden;
            }
            .sticker-card {
              width: ${wMm}mm;
              height: ${hMm}mm;
              padding: 1.5mm 2mm;
              margin: 0;
              display: flex;
              flex-direction: column;
              justify-content: space-between;
              overflow: hidden;
              background: #fff;
            }
            .top-left-info {
              text-align: left;
              border-bottom: 0.8px solid #333;
              padding-bottom: 1mm;
              margin-bottom: 1mm;
            }
            .title {
              font-size: 10pt;
              font-weight: 900;
              color: #000;
              line-height: 1.1;
              white-space: nowrap;
              overflow: hidden;
              text-overflow: ellipsis;
            }
            .subtitle {
              font-size: 8.5pt;
              font-weight: 800;
              color: #059669;
              line-height: 1.1;
            }
            .barcode-svg-wrapper {
              display: flex;
              justify-content: center;
              align-items: center;
              flex: 1;
              width: 100%;
              min-height: 0;
              overflow: hidden;
            }
            svg {
              width: 100% !important;
              height: 100% !important;
              max-height: 100%;
              display: block;
            }
          </style>
        </head>
        <body>
          <div class="sticker-card">
            ${stickerEl.innerHTML}
          </div>
          <script>
            window.onload = function() {
              window.focus();
              window.print();
              window.onafterprint = function() { window.close(); };
            };
          </script>
        </body>
      </html>
    `);
    printWindow.document.close();
  }

  excel(): void {
    const exportColumn: ExportColumnModel[] = [
      { key: 'Sku', value: 'SKU' },
      { key: 'Name', value: 'Product Name' },
      { key: 'Category', value: 'Category' },
      { key: 'Brand', value: 'Brand' },
      { key: 'Unit', value: 'Unit' },
      { key: 'Barcode', value: 'Barcode' },
      { key: 'CostPrice', value: 'Cost Price' },
      { key: 'SellingPrice', value: 'Selling Price' },
      { key: 'CurrentStock', value: 'Current Stock' },
      { key: 'ReorderLevel', value: 'Reorder Level' },
      { key: 'ReorderQuantity', value: 'Reorder Quantity' },
      { key: 'Tax', value: 'Tax' },
      { key: 'Status', value: 'Status' },
      { key: 'Description', value: 'Description' },
      { key: 'CreatedOn', value: 'Created On' },
      { key: 'CreatedBy', value: 'Created By' },
      { key: 'UpdatedOn', value: 'Updated On' },
      { key: 'UpdatedBy', value: 'Updated By' },
    ];

    this.productService
      .excel(
        this.selectedCategoryId,
        undefined,
        'CreatedOn',
        -1,
        exportColumn
      )
      .subscribe({
        next: (res) => this.exportService.excel_blob('Product List', res),
        error: (err) => {
          console.error('Export failed', err);
          this.exportService.excelAll('Products', this.tblProducts);
        }
      });
  }

  getCategoryName(categoryId: number | string): string {
    return this.categories.find(c => Number(c.id) === Number(categoryId))?.name ?? '—';
  }
}

