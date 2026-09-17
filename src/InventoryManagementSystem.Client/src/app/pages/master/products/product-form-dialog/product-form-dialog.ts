import { CommonModule } from '@angular/common';
import { Component, EventEmitter, inject, Input, OnChanges, Output, SimpleChanges } from '@angular/core';
import { FormBuilder, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';
import { MessageService } from 'primeng/api';
import { ButtonModule } from 'primeng/button';
import { DialogModule } from 'primeng/dialog';
import { InputTextModule } from 'primeng/inputtext';
import { SelectModule } from 'primeng/select';
import { TextareaModule } from 'primeng/textarea';
import { ToggleSwitchModule } from 'primeng/toggleswitch';
import { CategoryModel } from '../../../../core/models/category.model';
import { ProductModel } from '../../../../core/models/product.model';
import { UnitOfMeasureModel } from '../../../../core/models/unit-of-measure.model';
import { ProductService } from '../../../../core/services/master/product.service';

@Component({
  selector: 'app-product-form-dialog',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    ReactiveFormsModule,
    DialogModule,
    InputTextModule,
    SelectModule,
    ButtonModule,
    ToggleSwitchModule,
    TextareaModule,
  ],
  templateUrl: './product-form-dialog.html',
})
export class ProductFormDialog implements OnChanges {
  @Input() visible: boolean = false;
  @Input() isEdit: boolean = false;
  @Input() product: ProductModel | null = null;
  @Input() categories: CategoryModel[] = [];
  @Input() availableUoms: UnitOfMeasureModel[] = [];

  @Output() visibleChange = new EventEmitter<boolean>();
  @Output() saved = new EventEmitter<void>();

  isSubmitting: boolean = false;

  private formBuilder = inject(FormBuilder);
  private productService = inject(ProductService);
  private messageService = inject(MessageService);

  public productForm = this.formBuilder.group({
    id: [null as string | null],
    name: ['', Validators.required],
    categoryId: [null as number | null, Validators.required],
    baseUomId: [null as number | null, Validators.required],
    purchaseUomId: [null as number | null],
    saleUomId: [null as number | null],
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

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['visible'] && this.visible) {
      if (this.isEdit && this.product) {
        this.productForm.patchValue({
          id: this.product.id || null,
          name: this.product.name,
          categoryId: Number(this.product.categoryId),
          baseUomId: this.product.baseUomId ? Number(this.product.baseUomId) : (this.availableUoms.length > 0 ? this.availableUoms[0].id : null),
          purchaseUomId: this.product.purchaseUomId ? Number(this.product.purchaseUomId) : null,
          saleUomId: this.product.saleUomId ? Number(this.product.saleUomId) : null,
          sku: this.product.sku || '',
          barcode: this.product.barcode || '',
          brand: this.product.brand || '',
          unit: this.product.unit || '',
          costPrice: this.product.costPrice ?? 0,
          sellingPrice: this.product.sellingPrice ?? 0,
          currentStock: this.product.currentStock,
          reorderLevel: this.product.reorderLevel,
          reorderQuantity: this.product.reorderQuantity ?? 0,
          tax: this.product.tax ?? 0,
          status: this.product.status ?? true,
          description: this.product.description || '',
        });
      } else {
        this.productForm.reset({
          id: null,
          name: '',
          categoryId: this.categories.length > 0 ? Number(this.categories[0].id) : null,
          baseUomId: this.availableUoms.length > 0 ? this.availableUoms[0].id : null,
          purchaseUomId: null,
          saleUomId: null,
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
      }
    }
  }

  onSubmit(): void {
    if (this.productForm.invalid) {
      this.productForm.markAllAsTouched();
      return;
    }

    this.isSubmitting = true;
    const formVal = this.productForm.value;

    const payload: ProductModel = {
      name: formVal.name!,
      categoryId: formVal.categoryId!,
      baseUomId: Number(formVal.baseUomId!),
      purchaseUomId: formVal.purchaseUomId ? Number(formVal.purchaseUomId) : null,
      saleUomId: formVal.saleUomId ? Number(formVal.saleUomId) : null,
      sku: formVal.sku?.trim() || undefined,
      barcode: formVal.barcode?.trim() || undefined,
      brand: formVal.brand || undefined,
      unit: this.availableUoms.find(u => Number(u.id) === Number(formVal.baseUomId))?.code || formVal.unit || undefined,
      costPrice: Number(formVal.costPrice) || 0,
      sellingPrice: Number(formVal.sellingPrice) || 0,
      currentStock: Number(formVal.currentStock) || 0,
      reorderLevel: Number(formVal.reorderLevel) || 0,
      reorderQuantity: Number(formVal.reorderQuantity) || 0,
      tax: Number(formVal.tax) || 0,
      status: formVal.status ?? true,
      description: formVal.description || undefined,
    };

    if (this.isEdit && formVal.id) {
      payload.id = formVal.id;
      this.productService.update(payload).subscribe({
        next: (res) => {
          this.isSubmitting = false;
          if (res.success) {
            this.messageService.add({
              key: 'globalMessage',
              severity: 'info',
              summary: 'Success',
              detail: res.message ? res.message.toString() : 'Product updated successfully',
            });
            this.close();
            this.saved.emit();
          }
        },
        error: (err) => {
          this.isSubmitting = false;
          const detail = err.error?.errors
            ? Object.values(err.error.errors).flat().join(', ')
            : (err.error?.message || 'Update failed');
          this.messageService.add({
            key: 'globalMessage',
            severity: 'error',
            summary: 'Error',
            detail,
          });
        },
      });
    } else {
      this.productService.create(payload).subscribe({
        next: (res) => {
          this.isSubmitting = false;
          if (res.success) {
            this.messageService.add({
              key: 'globalMessage',
              severity: 'success',
              summary: 'Success',
              detail: res.message ? res.message.toString() : 'Product created successfully',
            });
            this.close();
            this.saved.emit();
          }
        },
        error: (err) => {
          this.isSubmitting = false;
          const detail = err.error?.errors
            ? Object.values(err.error.errors).flat().join(', ')
            : (err.error?.message || 'Creation failed');
          this.messageService.add({
            key: 'globalMessage',
            severity: 'error',
            summary: 'Error',
            detail,
          });
        },
      });
    }
  }

  close(): void {
    this.visible = false;
    this.visibleChange.emit(false);
  }
}
