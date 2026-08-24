import { CommonModule, CurrencyPipe, DatePipe } from '@angular/common';
import { ChangeDetectorRef, Component, inject, OnInit, signal, ViewChild } from '@angular/core';
import { FormBuilder, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';
import { CategoryModel } from '../../../core/models/category.model';
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
  isEdit: boolean = false;
  isLoading: boolean = false;
  isSubmitting: boolean = false;

  // Category filter select box state
  selectedCategoryId: number | null = null;

  private formBuilder = inject(FormBuilder);
  public productForm = this.formBuilder.group({
    id: [null as string | null],
    name: ['', Validators.required],
    categoryId: [null as number | null, Validators.required],
    sku: [''],
    barcode: [''],
    unitPrice: [0, [Validators.required, Validators.min(0)]],
    currentStock: [0, [Validators.required, Validators.min(0)]],
    reorderLevel: [10, [Validators.required, Validators.min(1)]],
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
      unitPrice: 0,
      currentStock: 0,
      reorderLevel: 10,
      categoryId: this.selectedCategoryId && this.selectedCategoryId > 0 ? this.selectedCategoryId : null
    });
    this.modalVisible = true;
  }

  update(): void {
    this.isEdit = true;
    this.productForm.reset();

    if (this.selectedProduct != null) {
      const prodId = this.selectedProduct.id ? String(this.selectedProduct.id) : (this.selectedProduct.productId ? String(this.selectedProduct.productId) : null);
      this.productForm.patchValue({
        id: prodId,
        name: this.selectedProduct.name,
        categoryId: Number(this.selectedProduct.categoryId),
        sku: this.selectedProduct.sku || this.selectedProduct.productCode || '',
        barcode: this.selectedProduct.barcode || '',
        unitPrice: this.selectedProduct.unitPrice,
        currentStock: this.selectedProduct.currentStock ?? this.selectedProduct.quantityInStock ?? 0,
        reorderLevel: this.selectedProduct.reorderLevel,
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
      const prodId = this.selectedProduct.id ? String(this.selectedProduct.id) : (this.selectedProduct.productId ? String(this.selectedProduct.productId) : '');
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

    if (!this.isEdit) {
      const payload = {
        name: formVal.name,
        categoryId: formVal.categoryId,
        sku: formVal.sku,
        barcode: formVal.barcode,
        unitPrice: formVal.unitPrice,
        currentStock: formVal.currentStock,
        reorderLevel: formVal.reorderLevel,
      };

      

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
      const payload = {
        id: formVal.id,
        name: formVal.name,
        categoryId: formVal.categoryId,
        sku: formVal.sku,
        barcode: formVal.barcode,
        unitPrice: formVal.unitPrice,
        currentStock: formVal.currentStock,
        reorderLevel: formVal.reorderLevel,
      };

      this.productService.update(payload).subscribe({
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

  onDialogHide(): void {
    this.selectedProduct = null as any;
    this.modalVisible = false;
  }

  excel(): void {
    this.productService.exportExcel(this.selectedCategoryId, 'Pyidaungsu').subscribe({
      next: (blob: Blob) => {
        const url = window.URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = `Products_${new Date().toISOString().slice(0, 10)}.xlsx`;
        document.body.appendChild(a);
        a.click();
        document.body.removeChild(a);
        window.URL.revokeObjectURL(url);
      },
      error: () => {
        // Fallback to client-side table export if backend call fails
        this.exportService.excelAll('Products', this.tblProducts);
      }
    });
  }

  getCategoryName(categoryId: number | string): string {
    return this.categories.find(c => Number(c.id) === Number(categoryId))?.name ?? '—';
  }
}
