import { CommonModule } from '@angular/common';
import { Component, EventEmitter, inject, Input, OnChanges, Output, SimpleChanges } from '@angular/core';
import { FormBuilder, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';
import { ConfirmationService, MessageService } from 'primeng/api';
import { ButtonModule } from 'primeng/button';
import { DialogModule } from 'primeng/dialog';
import { InputTextModule } from 'primeng/inputtext';
import { TableModule } from 'primeng/table';
import { TagModule } from 'primeng/tag';
import { ToggleSwitchModule } from 'primeng/toggleswitch';
import { UomCategoryModel } from '../../../../core/models/uom-category.model';
import { UomCategoryService } from '../../../../core/services/uom-category.service';

@Component({
  selector: 'app-uom-category-dialog',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    ReactiveFormsModule,
    DialogModule,
    TableModule,
    TagModule,
    ButtonModule,
    InputTextModule,
    ToggleSwitchModule,
  ],
  templateUrl: './uom-category-dialog.html',
})
export class UomCategoryDialog implements OnChanges {
  @Input() visible: boolean = false;
  @Input() categories: UomCategoryModel[] = [];

  @Output() visibleChange = new EventEmitter<boolean>();
  @Output() categoriesChanged = new EventEmitter<void>();

  // Inner Add/Edit Category Modal
  editModalVisible: boolean = false;
  isEdit: boolean = false;
  isSubmitting: boolean = false;
  selectedCategory!: UomCategoryModel;

  private formBuilder = inject(FormBuilder);
  private uomCategoryService = inject(UomCategoryService);
  private messageService = inject(MessageService);
  private confirmationService = inject(ConfirmationService);

  public categoryForm = this.formBuilder.group({
    id: [0],
    name: ['', [Validators.required, Validators.maxLength(100)]],
    description: [''],
    isActive: [true],
  });

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['visible'] && this.visible) {
      this.categoriesChanged.emit();
    }
  }

  createCategory(): void {
    this.isEdit = false;
    this.categoryForm.reset({
      id: 0,
      name: '',
      description: '',
      isActive: true,
    });
    this.editModalVisible = true;
  }

  editCategory(cat: UomCategoryModel): void {
    this.isEdit = true;
    this.selectedCategory = cat;
    this.categoryForm.patchValue({
      id: cat.id,
      name: cat.name,
      description: cat.description || '',
      isActive: cat.isActive,
    });
    this.editModalVisible = true;
  }

  saveCategory(): void {
    if (this.categoryForm.invalid) {
      this.categoryForm.markAllAsTouched();
      return;
    }

    this.isSubmitting = true;
    const formVal = this.categoryForm.value;
    const payload: Partial<UomCategoryModel> = {
      id: formVal.id || 0,
      name: formVal.name!.trim(),
      description: formVal.description?.trim() || null,
      isActive: formVal.isActive ?? true,
    };

    const action = this.isEdit
      ? this.uomCategoryService.update(payload)
      : this.uomCategoryService.create(payload);

    action.subscribe({
      next: (res) => {
        this.isSubmitting = false;
        if (res.success) {
          this.editModalVisible = false;
          this.categoriesChanged.emit();
          this.messageService.add({
            key: 'globalMessage',
            severity: 'success',
            summary: 'Success',
            detail: `Category ${this.isEdit ? 'updated' : 'created'} successfully.`,
          });
        }
      },
      error: (err) => {
        this.isSubmitting = false;
        this.messageService.add({
          key: 'globalMessage',
          severity: 'error',
          summary: 'Error',
          detail: err.error?.message || 'Operation failed',
        });
      },
    });
  }

  deleteCategory(cat: UomCategoryModel): void {
    this.confirmationService.confirm({
      key: 'positionDialog',
      message: `Are you sure you want to delete category "${cat.name}"?`,
      header: 'Delete Category Confirmation',
      icon: 'pi pi-info-circle',
      accept: () => {
        this.uomCategoryService.delete(cat.id).subscribe({
          next: (res) => {
            if (res.success) {
              this.categoriesChanged.emit();
              this.messageService.add({
                key: 'globalMessage',
                severity: 'success',
                summary: 'Success',
                detail: 'Category deleted successfully.',
              });
            }
          },
          error: (err) => {
            this.messageService.add({
              key: 'globalMessage',
              severity: 'error',
              summary: 'Error',
              detail: err.error?.message || 'Failed to delete category.',
            });
          },
        });
      },
    });
  }

  close(): void {
    this.visible = false;
    this.visibleChange.emit(false);
  }
}
