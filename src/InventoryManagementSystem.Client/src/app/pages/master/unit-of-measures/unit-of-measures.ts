import { CommonModule, DatePipe } from '@angular/common';
import { ChangeDetectorRef, Component, inject, OnInit, signal, ViewChild } from '@angular/core';
import { FormBuilder, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';
import { UnitOfMeasureModel } from '../../../core/models/unit-of-measure.model';
import { UomCategoryModel } from '../../../core/models/uom-category.model';
import { ExportService } from '../../../core/services/export.service';
import { LoggerService } from '../../../core/services/logger.service';
import { UnitOfMeasureService } from '../../../core/services/unit-of-measure.service';
import { UomCategoryService } from '../../../core/services/uom-category.service';
import { ConfirmationService, MenuItem, MessageService } from 'primeng/api';
import { ButtonModule } from 'primeng/button';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { DialogModule } from 'primeng/dialog';
import { IconFieldModule } from 'primeng/iconfield';
import { InputIconModule } from 'primeng/inputicon';
import { InputTextModule } from 'primeng/inputtext';
import { SelectModule } from 'primeng/select';
import { SplitButtonModule } from 'primeng/splitbutton';
import { Table, TableModule } from 'primeng/table';
import { TagModule } from 'primeng/tag';
import { ToastModule } from 'primeng/toast';
import { ToggleSwitchModule } from 'primeng/toggleswitch';
import { UomCategoryDialog } from './uom-category-dialog/uom-category-dialog';

@Component({
  selector: 'app-unit-of-measures',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    ReactiveFormsModule,
    ToggleSwitchModule,
    SplitButtonModule,
    SelectModule,
    TagModule,
    DialogModule,
    ConfirmDialogModule,
    IconFieldModule,
    InputTextModule,
    ButtonModule,
    TableModule,
    ToastModule,
    InputIconModule,
    UomCategoryDialog,
  ],
  providers: [DatePipe, ConfirmationService, ExportService, MessageService],
  templateUrl: './unit-of-measures.html',
  styleUrl: './unit-of-measures.scss',
})
export class UnitOfMeasures implements OnInit {
  @ViewChild('tblUom') tblUom!: Table;

  unitOfMeasures: UnitOfMeasureModel[] = [];
  categories: UomCategoryModel[] = [];
  selectedUom!: UnitOfMeasureModel;
  selectedCategoryFilter: number | null = null;

  items!: MenuItem[];
  modalVisible: boolean = false;
  isEdit: boolean = false;
  isLoading: boolean = false;
  isSubmitting: boolean = false;

  // Category Manager Modal state
  categoryModalVisible: boolean = false;

  private formBuilder = inject(FormBuilder);

  public uomForm = this.formBuilder.group({
    id: [0],
    categoryId: [null as any, Validators.required],
    code: ['', [Validators.required, Validators.maxLength(20)]],
    name: ['', [Validators.required, Validators.maxLength(150)]],
    symbol: [''],
    decimalPlaces: [0, [Validators.required, Validators.min(0), Validators.max(4)]],
    isActive: [true],
  });

  decimalOptions = [
    { label: '0 (Discrete / Count e.g. Pcs, Box)', value: 0 },
    { label: '1 (1 Decimal place)', value: 1 },
    { label: '2 (2 Decimal places e.g. 1.25 Kg)', value: 2 },
    { label: '3 (3 Decimal places e.g. 1.234 Kg)', value: 3 },
    { label: '4 (4 High precision)', value: 4 },
  ];

  constructor(
    private uomService: UnitOfMeasureService,
    private uomCategoryService: UomCategoryService,
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
        label: 'Export Excel',
        icon: 'pi pi-file-excel',
        command: () => this.exportExcel(),
      },
    ];
  }

  ngOnInit(): void {
    this.loadCategories();
    this.loadUoms();
  }

  loadCategories(): void {
    this.uomCategoryService.get().subscribe({
      next: (res) => {
        if (res.success && res.data) {
          this.categories = res.data;
          this.cdr.detectChanges();
        }
      },
      error: (err) => {
        this.loggerService.error('Error loading UOM categories:', err);
      },
    });
  }

  loadUoms(): void {
    this.isLoading = true;
    const catId = this.selectedCategoryFilter ?? undefined;
    this.uomService.get(catId).subscribe({
      next: (res) => {
        this.isLoading = false;
        if (res.success && res.data) {
          this.unitOfMeasures = res.data;
          this.cdr.detectChanges();
        }
      },
      error: (err) => {
        this.isLoading = false;
        this.loggerService.error('Error loading UOMs:', err);
      },
    });
  }

  onCategoryFilterChange(): void {
    this.loadUoms();
  }

  create(): void {
    this.isEdit = false;
    this.uomForm.reset({
      id: 0,
      categoryId: this.categories.length > 0 ? this.categories[0].id : (null as any),
      code: '',
      name: '',
      symbol: '',
      decimalPlaces: 0,
      isActive: true,
    });
    this.modalVisible = true;
  }

  update(): void {
    if (!this.selectedUom) {
      this.messageService.add({
        key: 'globalMessage',
        severity: 'warn',
        summary: 'Warning',
        detail: 'Please select a unit of measure to update.',
      });
      return;
    }
    this.isEdit = true;
    this.uomForm.patchValue({
      id: this.selectedUom.id,
      categoryId: this.selectedUom.categoryId,
      code: this.selectedUom.code,
      name: this.selectedUom.name,
      symbol: this.selectedUom.symbol || '',
      decimalPlaces: this.selectedUom.decimalPlaces,
      isActive: this.selectedUom.isActive,
    });
    this.modalVisible = true;
  }

  delete(): void {
    if (!this.selectedUom) {
      this.messageService.add({
        key: 'globalMessage',
        severity: 'warn',
        summary: 'Warning',
        detail: 'Please select a unit of measure to delete.',
      });
      return;
    }

    this.confirmationService.confirm({
      key: 'positionDialog',
      message: `Are you sure you want to delete unit "${this.selectedUom.name} (${this.selectedUom.code})"?`,
      header: 'Delete Confirmation',
      icon: 'pi pi-info-circle',
      accept: () => {
        this.uomService.delete(this.selectedUom.id).subscribe({
          next: (res) => {
            if (res.success) {
              this.messageService.add({
                key: 'globalMessage',
                severity: 'success',
                summary: 'Success',
                detail: 'Unit of measure deleted successfully.',
              });
              this.loadUoms();
            }
          },
          error: (err) => {
            this.messageService.add({
              key: 'globalMessage',
              severity: 'error',
              summary: 'Error',
              detail: err.error?.message || 'Failed to delete unit of measure.',
            });
          },
        });
      },
    });
  }

  save(): void {
    if (this.uomForm.invalid) {
      this.uomForm.markAllAsTouched();
      return;
    }

    this.isSubmitting = true;
    const formVal = this.uomForm.value;

    const payload: Partial<UnitOfMeasureModel> = {
      id: formVal.id || 0,
      categoryId: formVal.categoryId!,
      code: formVal.code!.trim().toUpperCase(),
      name: formVal.name!.trim(),
      symbol: formVal.symbol?.trim() || null,
      decimalPlaces: formVal.decimalPlaces ?? 0,
      isActive: formVal.isActive ?? true,
    };

    const action = this.isEdit
      ? this.uomService.update(payload)
      : this.uomService.create(payload);

    action.subscribe({
      next: (res) => {
        this.isSubmitting = false;
        if (res.success) {
          this.messageService.add({
            key: 'globalMessage',
            severity: 'success',
            summary: 'Success',
            detail: `Unit of measure ${this.isEdit ? 'updated' : 'created'} successfully.`,
          });
          this.modalVisible = false;
          this.loadUoms();
        }
      },
      error: (err) => {
        this.isSubmitting = false;
        const detail = err.error?.errors
          ? Object.values(err.error.errors).flat().join(', ')
          : (err.error?.message || 'Operation failed');
        this.messageService.add({
          key: 'globalMessage',
          severity: 'error',
          summary: 'Error',
          detail,
        });
      },
    });
  }

  openCategoryManager(): void {
    this.categoryModalVisible = true;
  }

  exportExcel(): void {
    this.exportService.excelAll('UnitOfMeasures', this.tblUom);
  }
}
