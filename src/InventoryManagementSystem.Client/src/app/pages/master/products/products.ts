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
import { CardModule } from 'primeng/card';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { DatePickerModule } from 'primeng/datepicker';
import { DialogModule } from 'primeng/dialog';
import { DividerModule } from 'primeng/divider';
import { FieldsetModule } from 'primeng/fieldset';
import { IconFieldModule } from 'primeng/iconfield';
import { InputIconModule } from 'primeng/inputicon';
import { InputTextModule } from 'primeng/inputtext';
import { MessageModule } from 'primeng/message';
import { SelectModule } from 'primeng/select';
import { SplitButtonModule } from 'primeng/splitbutton';
import { Table, TableModule } from 'primeng/table';
import { TagModule } from 'primeng/tag';
import { TextareaModule } from 'primeng/textarea';
import { ToastModule } from 'primeng/toast';
import { ToggleSwitch, ToggleSwitchModule } from 'primeng/toggleswitch';

@Component({
  selector: 'app-products',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    ReactiveFormsModule,
    ToggleSwitchModule,
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
    SelectModule,
    TextareaModule,
    FieldsetModule,
    DividerModule,
    DatePickerModule,
    CardModule,
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
  detailModalVisible: boolean = false;
  isEdit: boolean = false;
  isLoading: boolean = false;
  isSubmitting: boolean = false;
  buttonChange: boolean = false;

  // Filter state
  selectedCategoryId: number | null = null;
  selectedCategory: CategoryModel | null = null;
  selectedStatus: string | null = null;
  statusOptions = [
    { label: 'All Statuses', value: null },
    { label: 'In Stock', value: 'In Stock' },
    { label: 'Low Stock', value: 'Low Stock' },
    { label: 'Out of Stock', value: 'Out of Stock' },
  ];

  // get categoryFilterOptions(): any[] {
  //   return [{ id: null, name: 'All Categories' }, ...this.categories];
  // }

  // onCategoryFilterChange(): void {
  //   if (this.selectedCategoryId == null || this.selectedCategoryId === 0) {
  //     this.filteredProducts = [...this.products];
  //   } else {
  //     this.filteredProducts = this.products.filter(
  //       p => Number(p.categoryId) === Number(this.selectedCategoryId)
  //     );
  //   }
  //   this.cdr.detectChanges();
  // }

  private formBuilder = inject(FormBuilder);
  public productForm = this.formBuilder.group({
    productId: [0 as any],
    productCode: [''],
    name: ['', Validators.required],
    description: [''],
    categoryId: [null as any, Validators.required],
    unitPrice: [0, [Validators.required, Validators.min(0)]],
    costPrice: [0],
    quantityInStock: [0, [Validators.required, Validators.min(0)]],
    reorderLevel: [10, [Validators.required, Validators.min(1)]],
    unitOfMeasure: ['PCS', Validators.required],
    isActive: [true],
    createdOn: [null as any],
    createdBy: [null as any],
    updatedOn: [null as any],
    updatedBy: [null as any],
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
        // this.onCategoryFilterChange();
        this.isLoading = false;
        this.cdr.detectChanges();
      },
      error: err => {
        this.loggerService.error('Product API error', err);
        this.isLoading = false;
      },
    });
  }

  view(): void {
    this.filteredProducts = this.products.filter(p => {
      const matchCat = !this.selectedCategory || p.categoryId === this.selectedCategory.id;
      const matchStatus = !this.selectedStatus || p.status === this.selectedStatus;
      return matchCat && matchStatus;
    });
    this.buttonChange = true;
  }

  resetState(): void {
    this.selectedCategoryId = null;
    this.selectedCategory = null;
    this.selectedStatus = null;
    this.filteredProducts = [...this.products];
    this.buttonChange = false;
  }

  create(): void {
    this.isEdit = false;
    this.productForm.reset();
    this.errorMessage.set([]);
    this.productForm.controls.productId.setValue(0);
    this.productForm.controls.isActive.setValue(true);
    this.productForm.controls.unitOfMeasure.setValue('PCS');
    this.productForm.controls.reorderLevel.setValue(10);
    this.modalVisible = true;
  }

  update(): void {
    this.isEdit = true;
    this.productForm.reset();

    if (this.selectedProduct != null && this.selectedProduct !== undefined) {
      this.productForm.patchValue({
        productId: this.selectedProduct.productId,
        productCode: this.selectedProduct.productCode,
        name: this.selectedProduct.name,
        description: this.selectedProduct.description,
        categoryId: this.selectedProduct.categoryId,
        unitPrice: this.selectedProduct.unitPrice,
        costPrice: this.selectedProduct.costPrice,
        quantityInStock: this.selectedProduct.quantityInStock,
        reorderLevel: this.selectedProduct.reorderLevel,
        unitOfMeasure: this.selectedProduct.unitOfMeasure,
        isActive: (this.selectedProduct as any).isActive ?? true,
        createdOn: this.selectedProduct.createdAt as any,
      });
      this.modalVisible = true;
      this.selectedProduct = null as any;
      this.errorMessage.set([]);
    } else {
      this.messageService.add({
        key: 'globalMessage',
        severity: 'warn',
        summary: 'Warning',
        detail: 'Please Select Product',
      });
    }
  }

  delete(): void {
    if (this.selectedProduct != null) {
      this.confirmationService.confirm({
        message: 'Are You Sure Want To Delete?',
        header: 'Delete Confirmation',
        icon: 'pi pi-info-circle',
        accept: () => {
          this.productService.delete(this.selectedProduct.productId).subscribe(res => {
            this.messageService.add({
              key: 'globalMessage',
              severity: 'success',
              summary: 'Confirmed',
              detail: res.message ?? 'Product deleted successfully.',
            });
            this.loadData();
            this.selectedProduct = null as any;
            this.cdr.detectChanges();
          });
        },
        reject: () => {
          this.selectedProduct = null as any;
        },
        key: 'positionDialog',
      });
    } else {
      this.messageService.add({
        key: 'globalMessage',
        severity: 'warn',
        summary: 'Warning',
        detail: 'Please Select Product',
      });
    }
  }

  openDetail(product: ProductModel): void {
    this.selectedProduct = product;
    this.detailModalVisible = true;
  }

  onSubmit(): void {
    if (!this.productForm.valid) {
      Object.keys(this.productForm.controls).forEach(field => {
        this.productForm.get(field)?.markAsDirty({ onlySelf: true });
      });
      return;
    }

    this.isSubmitting = true;
    const model = this.productForm.value as ProductModel;

    if (!this.isEdit) {
      model.productId = 0;
      model.createdAt = new Date();
      (model as any).createdBy = this.shareService.getUserName() ?? '';
      this.isSubmitting = true;

      this.productService.create(model).subscribe({
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
      (model as any).updatedAt = new Date();
      (model as any).updatedBy = this.shareService.getUserName() ?? '';

      this.productService.update(model).subscribe({
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
    this.exportService.excelAll('Products', this.tblProducts);
  }

  getStatusSeverity(status: string): 'success' | 'warn' | 'danger' | 'info' {
    switch (status) {
      case 'In Stock': return 'success';
      case 'Low Stock': return 'warn';
      case 'Out of Stock': return 'danger';
      default: return 'info';
    }
  }

  getCategoryName(categoryId: number): string {
    return this.categories.find(c => c.id === categoryId)?.name ?? '—';
  }
}
