import { Component, OnInit, ViewChild } from '@angular/core';
import { CommonModule, CurrencyPipe, DatePipe } from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { ProductModel } from '../../core/models/product.model';
import { CategoryModel } from '../../core/models/category.model';
import { ProductService } from '../../core/services/product.service';
import { CategoryService } from '../../core/services/category.service';
import { ExportService } from '../../core/services/export.service';
import { SharedService } from '../../core/services/shared.service';

import { Table, TableModule } from 'primeng/table';
import { ButtonModule } from 'primeng/button';
import { SelectModule } from 'primeng/select';
import { InputTextModule } from 'primeng/inputtext';
import { TextareaModule } from 'primeng/textarea';
import { IconFieldModule } from 'primeng/iconfield';
import { InputIconModule } from 'primeng/inputicon';
import { DialogModule } from 'primeng/dialog';
import { TagModule } from 'primeng/tag';
import { FieldsetModule } from 'primeng/fieldset';
import { DividerModule } from 'primeng/divider';
import { DatePickerModule } from 'primeng/datepicker';
import { CardModule } from 'primeng/card';

@Component({
  selector: 'app-products',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    ReactiveFormsModule,
    TableModule,
    ButtonModule,
    SelectModule,
    InputTextModule,
    TextareaModule,
    IconFieldModule,
    InputIconModule,
    DialogModule,
    TagModule,
    FieldsetModule,
    DividerModule,
    DatePickerModule,
    CardModule
  ],
  providers: [CurrencyPipe, DatePipe],
  templateUrl: './products.html',
  styleUrl: './products.scss'
})
export class ProductsComponent implements OnInit {
  @ViewChild('tblProducts') tblProducts!: Table;

  products: ProductModel[] = [];
  filteredProducts: ProductModel[] = [];
  categories: CategoryModel[] = [];

  // Filter Bar state (ES_HR style)
  selectedCategory: CategoryModel | null = null;
  selectedStatus: string | null = null;
  filterDate: Date = new Date();
  isLoading: boolean = false;
  buttonChange: boolean = false;

  statusOptions = [
    { label: 'All Statuses', value: null },
    { label: 'In Stock', value: 'In Stock' },
    { label: 'Low Stock', value: 'Low Stock' },
    { label: 'Out of Stock', value: 'Out of Stock' }
  ];

  // Modals state
  detailModalVisible: boolean = false;
  formModalVisible: boolean = false;
  selectedProduct: ProductModel | null = null;
  dialogHeader: string = '';
  isEditMode: boolean = false;

  productForm!: FormGroup;

  constructor(
    private productService: ProductService,
    private categoryService: CategoryService,
    private exportService: ExportService,
    public sharedService: SharedService,
    private fb: FormBuilder
  ) {
    this.initForm();
  }

  ngOnInit(): void {
    this.loadMasterData();
    this.loadProducts();
  }

  initForm(): void {
    this.productForm = this.fb.group({
      productId: [0],
      productCode: [''],
      name: ['', Validators.required],
      description: [''],
      categoryId: [null, Validators.required],
      unitPrice: [0, [Validators.required, Validators.min(0)]],
      costPrice: [0, [Validators.required, Validators.min(0)]],
      quantityInStock: [0, [Validators.required, Validators.min(0)]],
      reorderLevel: [10, [Validators.required, Validators.min(1)]],
      unitOfMeasure: ['PCS', Validators.required]
    });
  }

  loadMasterData(): void {
    this.categoryService.getByCB().subscribe(res => {
      this.categories = res.data;
    });
  }

  loadProducts(): void {
    this.isLoading = true;
    this.productService.getByCB().subscribe({
      next: res => {
        this.products = res.data;
        this.filteredProducts = [...this.products];
        this.isLoading = false;
      },
      error: () => (this.isLoading = false)
    });
  }

  // ES_HR Pattern: Filter view method
  view(): void {
    this.filteredProducts = this.products.filter(p => {
      const matchCat = !this.selectedCategory || p.categoryId === this.selectedCategory.categoryId;
      const matchStatus = !this.selectedStatus || p.status === this.selectedStatus;
      return matchCat && matchStatus;
    });
  }

  // ES_HR Pattern: Reset filters
  resetState(): void {
    this.selectedCategory = null;
    this.selectedStatus = null;
    this.filterDate = new Date();
    this.filteredProducts = [...this.products];
    this.buttonChange = false;
  }

  onCategoryChange(): void {
    this.buttonChange = true;
  }

  // ES_HR Pattern: View Detailed Slip / Modal
  openDetail(product: ProductModel): void {
    this.selectedProduct = product;
    this.dialogHeader = `${product.name} (${product.productCode}) - Category: ${product.categoryName || 'General'}`;
    this.detailModalVisible = true;
  }

  openCreate(): void {
    this.isEditMode = false;
    this.dialogHeader = 'Create New Inventory Product';
    this.productForm.reset({
      productId: 0,
      productCode: `PRD-${Math.floor(10000 + Math.random() * 90000)}`,
      unitPrice: 0,
      costPrice: 0,
      quantityInStock: 10,
      reorderLevel: 5,
      unitOfMeasure: 'PCS'
    });
    this.formModalVisible = true;
  }

  openEdit(product: ProductModel): void {
    this.isEditMode = true;
    this.dialogHeader = `Edit Product: ${product.productCode}`;
    this.productForm.patchValue({
      productId: product.productId,
      productCode: product.productCode,
      name: product.name,
      description: product.description,
      categoryId: product.categoryId,
      unitPrice: product.unitPrice,
      costPrice: product.costPrice,
      quantityInStock: product.quantityInStock,
      reorderLevel: product.reorderLevel,
      unitOfMeasure: product.unitOfMeasure
    });
    this.formModalVisible = true;
  }

  saveProduct(): void {
    if (this.productForm.invalid) {
      this.productForm.markAllAsTouched();
      return;
    }
    const val = this.productForm.value;
    const cat = this.categories.find(c => c.categoryId === val.categoryId);
    val.categoryName = cat ? cat.categoryName : 'General';

    this.productService.save(val).subscribe(saved => {
      this.formModalVisible = false;
      this.loadProducts();
    });
  }

  deleteProduct(product: ProductModel): void {
    if (confirm(`Are you sure you want to delete ${product.name}?`)) {
      this.productService.delete(product.productId).subscribe(() => {
        this.loadProducts();
      });
    }
  }

  // ES_HR Pattern: Export to Excel
  excel(): void {
    this.exportService.excel('Products_Inventory', this.tblProducts);
  }

  getStatusSeverity(status: string): 'success' | 'warn' | 'danger' | 'info' {
    switch (status) {
      case 'In Stock':
        return 'success';
      case 'Low Stock':
        return 'warn';
      case 'Out of Stock':
        return 'danger';
      default:
        return 'info';
    }
  }
}
