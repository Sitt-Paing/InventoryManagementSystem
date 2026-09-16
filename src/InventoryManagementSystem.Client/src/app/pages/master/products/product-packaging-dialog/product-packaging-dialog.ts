import { CommonModule } from '@angular/common';
import { ChangeDetectorRef, Component, EventEmitter, inject, Input, OnChanges, Output, SimpleChanges } from '@angular/core';
import { FormBuilder, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';
import { ConfirmationService, MessageService } from 'primeng/api';
import { ButtonModule } from 'primeng/button';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { DialogModule } from 'primeng/dialog';
import { IconFieldModule } from 'primeng/iconfield';
import { InputIconModule } from 'primeng/inputicon';
import { InputTextModule } from 'primeng/inputtext';
import { SelectModule } from 'primeng/select';
import { TableModule } from 'primeng/table';
import { TagModule } from 'primeng/tag';
import { ToggleSwitchModule } from 'primeng/toggleswitch';
import { ProductModel } from '../../../../core/models/product.model';
import { ProductUomConversionModel } from '../../../../core/models/product-uom-conversion.model';
import { UnitOfMeasureModel } from '../../../../core/models/unit-of-measure.model';
import { ProductUomConversionService } from '../../../../core/services/product-uom-conversion.service';
import { Barcode } from '../../../../shared/components/barcode/barcode';

@Component({
  selector: 'app-product-packaging-dialog',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    ReactiveFormsModule,
    DialogModule,
    ConfirmDialogModule,
    TableModule,
    TagModule,
    ButtonModule,
    InputTextModule,
    IconFieldModule,
    InputIconModule,
    SelectModule,
    ToggleSwitchModule,
    Barcode,
  ],
  templateUrl: './product-packaging-dialog.html',
})
export class ProductPackagingDialog implements OnChanges {
  @Input() visible: boolean = false;
  @Input() product: ProductModel | null = null;
  @Input() availableUoms: UnitOfMeasureModel[] = [];

  @Output() visibleChange = new EventEmitter<boolean>();
  @Output() printBarcode = new EventEmitter<{ product: ProductModel; barcode: string; label: string }>();

  conversions: ProductUomConversionModel[] = [];
  isLoading: boolean = false;

  // Inner Add/Edit Dialog
  editModalVisible: boolean = false;
  isEdit: boolean = false;
  isSubmitting: boolean = false;

  private formBuilder = inject(FormBuilder);

  public packagingForm = this.formBuilder.group({
    id: [0],
    fromUomId: [null as any, Validators.required],
    toUomId: [null as any, Validators.required],
    conversionFactor: [1, [Validators.required, Validators.min(0.0001)]],
    barcode: [''],
    isDefaultPurchase: [false],
    isDefaultSale: [false],
    isActive: [true],
  });

  constructor(
    private conversionService: ProductUomConversionService,
    private messageService: MessageService,
    private confirmationService: ConfirmationService,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['visible'] && this.visible && this.product?.id) {
      this.loadConversions();
    }
  }

  loadConversions(): void {
    if (!this.product?.id) return;
    this.isLoading = true;
    this.conversionService.getByProductId(this.product.id).subscribe({
      next: (res) => {
        this.isLoading = false;
        this.conversions = (res.data || []) as ProductUomConversionModel[];
        this.cdr.detectChanges();
      },
      error: () => {
        this.isLoading = false;
        this.cdr.detectChanges();
      },
    });
  }

  createPackaging(): void {
    this.isEdit = false;
    this.packagingForm.reset({
      id: 0,
      fromUomId: null,
      toUomId: null,
      conversionFactor: 1,
      barcode: '',
      isDefaultPurchase: false,
      isDefaultSale: false,
      isActive: true,
    });
    this.editModalVisible = true;
  }

  editPackaging(conv: ProductUomConversionModel): void {
    this.isEdit = true;
    this.packagingForm.patchValue({
      id: conv.id,
      fromUomId: conv.fromUomId,
      toUomId: conv.toUomId,
      conversionFactor: conv.conversionFactor,
      barcode: conv.barcode || '',
      isDefaultPurchase: conv.isDefaultPurchase,
      isDefaultSale: conv.isDefaultSale,
      isActive: conv.isActive,
    });
    this.editModalVisible = true;
  }

  savePackaging(): void {
    if (this.packagingForm.invalid || !this.product?.id) {
      this.packagingForm.markAllAsTouched();
      return;
    }

    this.isSubmitting = true;
    const formVal = this.packagingForm.value;
    const payload: Partial<ProductUomConversionModel> = {
      id: formVal.id || 0,
      productId: this.product.id,
      fromUomId: formVal.fromUomId!,
      toUomId: formVal.toUomId!,
      conversionFactor: Number(formVal.conversionFactor),
      barcode: formVal.barcode?.trim() || null,
      isDefaultPurchase: formVal.isDefaultPurchase ?? false,
      isDefaultSale: formVal.isDefaultSale ?? false,
      isActive: formVal.isActive ?? true,
    };

    const action = this.isEdit
      ? this.conversionService.update(payload)
      : this.conversionService.create(payload);

    action.subscribe({
      next: (res) => {
        this.isSubmitting = false;
        if (res.success) {
          this.editModalVisible = false;
          this.loadConversions();
          this.messageService.add({
            key: 'globalMessage',
            severity: 'success',
            summary: 'Success',
            detail: `Packaging conversion ${this.isEdit ? 'updated' : 'created'} successfully.`,
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

  deletePackaging(conv: ProductUomConversionModel): void {
    this.confirmationService.confirm({
      key: 'positionDialog',
      message: `Are you sure you want to delete packaging unit (${conv.fromUomCode || 'Unit'})?`,
      header: 'Delete Confirmation',
      icon: 'pi pi-info-circle',
      accept: () => {
        this.conversionService.delete(conv.id).subscribe({
          next: (res) => {
            if (res.success) {
              this.loadConversions();
              this.messageService.add({
                key: 'globalMessage',
                severity: 'success',
                summary: 'Success',
                detail: 'Packaging conversion deleted successfully.',
              });
            }
          },
          error: (err) => {
            this.messageService.add({
              key: 'globalMessage',
              severity: 'error',
              summary: 'Error',
              detail: err.error?.message || 'Failed to delete conversion.',
            });
          },
        });
      },
    });
  }

  onPrintBarcode(conv: ProductUomConversionModel): void {
    if (!conv.barcode || !this.product) {
      this.messageService.add({
        key: 'globalMessage',
        severity: 'warn',
        summary: 'No Barcode',
        detail: 'This packaging unit does not have a barcode set.',
      });
      return;
    }

    this.printBarcode.emit({
      product: {
        ...this.product,
        name: `${this.product.name} [${conv.fromUomCode || 'Unit'} × ${conv.conversionFactor}]`,
        barcode: conv.barcode,
      },
      barcode: conv.barcode,
      label: `${conv.fromUomCode} (${conv.conversionFactor})`,
    });
  }

  close(): void {
    this.visible = false;
    this.visibleChange.emit(false);
  }
}
