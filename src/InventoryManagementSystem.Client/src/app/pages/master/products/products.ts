import { CommonModule, CurrencyPipe, DatePipe } from '@angular/common';
import { ChangeDetectorRef, Component, OnInit, ViewChild } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { CategoryModel } from '../../../core/models/category.model';
import { ExportColumnModel } from '../../../core/models/export-column.model';
import { ProductModel } from '../../../core/models/product.model';
import { UnitOfMeasureModel } from '../../../core/models/unit-of-measure.model';
import { CategoryService } from '../../../core/services/category.service';
import { ExportService } from '../../../core/services/export.service';
import { LoggerService } from '../../../core/services/logger.service';
import { ProductService } from '../../../core/services/product.service';
import { SharedService } from '../../../core/services/shared.service';
import { UnitOfMeasureService } from '../../../core/services/unit-of-measure.service';
import { ConfirmationService, MenuItem, MessageService } from 'primeng/api';
import { ButtonModule } from 'primeng/button';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { IconFieldModule } from 'primeng/iconfield';
import { InputIconModule } from 'primeng/inputicon';
import { InputTextModule } from 'primeng/inputtext';
import { SelectModule } from 'primeng/select';
import { SplitButtonModule } from 'primeng/splitbutton';
import { Table, TableModule } from 'primeng/table';
import { TagModule } from 'primeng/tag';
import { ToastModule } from 'primeng/toast';
import { ProductBarcodeDialog } from './product-barcode-dialog/product-barcode-dialog';
import { ProductFormDialog } from './product-form-dialog/product-form-dialog';
import { ProductPackagingDialog } from './product-packaging-dialog/product-packaging-dialog';

@Component({
  selector: 'app-products',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    SplitButtonModule,
    TagModule,
    IconFieldModule,
    InputTextModule,
    SelectModule,
    ButtonModule,
    TableModule,
    ToastModule,
    InputIconModule,
    ConfirmDialogModule,
    ProductFormDialog,
    ProductBarcodeDialog,
    ProductPackagingDialog,
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
  availableUoms: UnitOfMeasureModel[] = [];
  selectedProduct!: ProductModel;

  items!: MenuItem[];
  isLoading: boolean = false;
  selectedCategoryId: number | null = null;

  // Dialog Controls
  formModalVisible: boolean = false;
  isEdit: boolean = false;

  barcodeModalVisible: boolean = false;
  selectedBarcodeProduct: ProductModel | null = null;
  selectedBarcodeValue: string | null = null;

  packagingModalVisible: boolean = false;
  selectedPackagingProduct: ProductModel | null = null;

  constructor(
    private shareService: SharedService,
    private productService: ProductService,
    private categoryService: CategoryService,
    private uomService: UnitOfMeasureService,
    private messageService: MessageService,
    private confirmationService: ConfirmationService,
    private loggerService: LoggerService,
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
      next: (res) => {
        this.categories = (res.data || []) as CategoryModel[];
        this.cdr.detectChanges();
      },
    });

    this.uomService.get().subscribe({
      next: (res) => {
        this.availableUoms = (res.data || []) as UnitOfMeasureModel[];
        this.cdr.detectChanges();
      },
    });
  }

  loadData(): void {
    this.isLoading = true;
    this.productService.get().subscribe({
      next: (res) => {
        this.products = (res.data || []) as ProductModel[];
        this.onCategoryFilterChange();
        this.isLoading = false;
        this.cdr.detectChanges();
      },
      error: (err) => {
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
        (p) => Number(p.categoryId) === Number(this.selectedCategoryId)
      );
    }
  }

  create(): void {
    this.isEdit = false;
    this.selectedProduct = null as any;
    this.formModalVisible = true;
  }

  update(): void {
    if (!this.selectedProduct) {
      this.messageService.add({
        key: 'globalMessage',
        severity: 'warn',
        summary: 'Warning',
        detail: 'Please select a product to update.',
      });
      return;
    }
    this.isEdit = true;
    this.formModalVisible = true;
  }

  delete(): void {
    if (!this.selectedProduct) {
      this.messageService.add({
        key: 'globalMessage',
        severity: 'warn',
        summary: 'Warning',
        detail: 'Please select a product to delete.',
      });
      return;
    }

    this.confirmationService.confirm({
      key: 'positionDialog',
      message: `Are you sure you want to delete "${this.selectedProduct.name}"?`,
      header: 'Delete Confirmation',
      icon: 'pi pi-info-circle',
      accept: () => {
        this.productService.delete(this.selectedProduct.id!).subscribe({
          next: () => {
            this.messageService.add({
              key: 'globalMessage',
              severity: 'success',
              summary: 'Success',
              detail: 'Product deleted successfully',
            });
            this.loadData();
          },
          error: (err) => {
            this.messageService.add({
              key: 'globalMessage',
              severity: 'error',
              summary: 'Error',
              detail: err.error?.message || 'Failed to delete product',
            });
          },
        });
      },
    });
  }

  viewItemBarcode(product: ProductModel): void {
    this.selectedBarcodeProduct = product;
    this.selectedBarcodeValue = product.barcode || product.sku || null;
    this.barcodeModalVisible = true;
  }

  openPackagingDialog(product: ProductModel): void {
    this.selectedPackagingProduct = product;
    this.packagingModalVisible = true;
  }

  onPackagingPrintBarcode(event: { product: ProductModel; barcode: string; label: string }): void {
    this.selectedBarcodeProduct = event.product;
    this.selectedBarcodeValue = event.barcode;
    this.barcodeModalVisible = true;
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
        },
      });
  }

  getCategoryName(categoryId: number | string): string {
    return this.categories.find((c) => Number(c.id) === Number(categoryId))?.name ?? '—';
  }
}
