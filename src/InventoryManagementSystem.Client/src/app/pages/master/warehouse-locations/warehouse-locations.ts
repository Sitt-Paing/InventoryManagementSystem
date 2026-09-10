import { ChangeDetectorRef, Component, inject, OnInit, ViewChild } from '@angular/core';
import { ToastModule } from 'primeng/toast';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { SplitButtonModule } from 'primeng/splitbutton';
import { IconFieldModule } from 'primeng/iconfield';
import { InputIconModule } from 'primeng/inputicon';
import { Table, TableModule } from 'primeng/table';
import { WarehouseLocationService } from '../../../core/services/warehouse-location.service';
import { WarehouseService } from '../../../core/services/warehouse.service';
import { ConfirmationService, MenuItem, MessageService } from 'primeng/api';
import { WarehouseLocationModel } from '../../../core/models/warehouse-location.model';
import { WarehouseModel } from '../../../core/models/warehouse.model';
import { InputTextModule } from 'primeng/inputtext';
import { CommonModule, DatePipe, DecimalPipe } from '@angular/common';
import { Tag } from 'primeng/tag';
import { DialogModule } from 'primeng/dialog';
import { FormBuilder, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';
import { ToggleSwitchModule } from 'primeng/toggleswitch';
import { ButtonModule } from 'primeng/button';
import { SelectModule } from 'primeng/select';
import { ExportService } from '../../../core/services/export.service';
import { Barcode } from '../../../shared/components/barcode/barcode';

@Component({
  selector: 'app-warehouse-locations',
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
    Barcode,
  ],
  providers: [ConfirmationService, MessageService, DatePipe, DecimalPipe, ExportService],
  templateUrl: './warehouse-locations.html',
  styleUrl: './warehouse-locations.scss',
})
export class WarehouseLocations implements OnInit {
  @ViewChild(Table) tblLocations!: Table;
  items: MenuItem[] = [];
  isLoading: boolean = false;
  locations: WarehouseLocationModel[] = [];
  warehouses: WarehouseModel[] = [];
  selectedLocation: WarehouseLocationModel | null = null;
  selectedWarehouseFilter: number | null = null;
  isEdit: boolean = false;
  modalVisible: boolean = false;
  isSubmitting: boolean = false;
  barcodeModalVisible: boolean = false;
  selectedBarcodeValue: string | null = null;

  private formBuilder = inject(FormBuilder);
  public locationForm = this.formBuilder.group({
    id: [0],
    warehouseId: [null as number | null, Validators.required],
    locationCode: ['', Validators.required],
    zone: [''],
    rack: [''],
    bin: [''],
    barcode: [''],
    capacity: [null as number | null],
    status: [true],
    createdOn: [null as any],
    createdBy: [null as any],
    updatedOn: [null as any],
    updatedBy: [null as any],
    deletedOn: [null as any],
    deletedBy: [null as any],
  });

  constructor(
    private locationService: WarehouseLocationService,
    private warehouseService: WarehouseService,
    private confirmationService: ConfirmationService,
    private datePipe: DatePipe,
    private messageService: MessageService,
    private cdr: ChangeDetectorRef,
    private exportService: ExportService,
  ) {
    this.items = [
      {
        label: 'Edit',
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
    this.loadWarehouses();
    this.loadData();
  }

  loadWarehouses(): void {
    this.warehouseService.get().subscribe({
      next: (res) => {
        this.warehouses = (res.data as WarehouseModel[]) ?? [];
        this.cdr.detectChanges();
      },
    });
  }

  loadData(): void {
    this.isLoading = true;
    const filterId = this.selectedWarehouseFilter ? Number(this.selectedWarehouseFilter) : undefined;
    this.locationService.get(filterId).subscribe({
      next: (res) => {
        this.locations = (res.data as WarehouseLocationModel[]) ?? [];
        this.isLoading = false;
        this.cdr.detectChanges();
      },
      error: (err) => {
        this.isLoading = false;
        this.cdr.detectChanges();
      },
    });
  }

  onWarehouseFilterChange(): void {
    this.loadData();
  }

  onDialogHide(): void {
    this.modalVisible = false;
    this.selectedLocation = null;
    this.isEdit = false;
  }

  create(): void {
    this.isEdit = false;
    this.selectedLocation = null;
    this.locationForm.reset({
      id: 0,
      warehouseId: this.selectedWarehouseFilter ?? null,
      locationCode: '',
      zone: '',
      rack: '',
      bin: '',
      barcode: '',
      capacity: null,
      status: true,
    });
    this.modalVisible = true;
  }

  onSubmit(): void {
    this.isSubmitting = true;
    if (this.locationForm.valid) {
      const model = this.locationForm.value as WarehouseLocationModel;
      model.warehouseId = Number(model.warehouseId);

      if (!this.isEdit) {
        this.locationService.create(model).subscribe({
          next: (res) => {
            this.isSubmitting = false;
            if (res.success) {
              this.modalVisible = false;
              this.messageService.add({
                key: 'globalMessage',
                severity: 'success',
                summary: 'Success',
                detail: 'Warehouse location created successfully.',
              });
              this.locationForm.reset();
              this.loadData();
              this.cdr.detectChanges();
            }
          },
          error: (err) => {
            this.isSubmitting = false;
            this.messageService.add({
              key: 'globalMessage',
              severity: 'error',
              summary: 'Error',
              detail: err.error?.message ? err.error.message.toString() : 'An error occurred while creating the location.',
            });
          },
        });
      } else {
        this.locationService.update(model).subscribe({
          next: (res) => {
            this.isSubmitting = false;
            if (res.success) {
              this.modalVisible = false;
              this.isEdit = false;
              this.selectedLocation = null;
              this.locationForm.reset();
              this.messageService.add({
                key: 'globalMessage',
                severity: 'success',
                summary: 'Success',
                detail: 'Warehouse location updated successfully.',
              });
              this.loadData();
              this.cdr.detectChanges();
            }
          },
          error: (err) => {
            this.isSubmitting = false;
            this.messageService.add({
              key: 'globalMessage',
              severity: 'error',
              summary: 'Error',
              detail: err.error?.message ? err.error.message.toString() : 'An error occurred while updating the location.',
            });
          },
        });
      }
    } else {
      Object.keys(this.locationForm.controls).forEach((field) => {
        const control = this.locationForm.get(field);
        control?.markAsDirty({ onlySelf: true });
      });
      this.isSubmitting = false;
    }
  }

  update(): void {
    if (this.selectedLocation) {
      this.isEdit = true;
      this.locationForm.reset();
      this.locationForm.patchValue({
        id: this.selectedLocation.id,
        warehouseId: this.selectedLocation.warehouseId,
        locationCode: this.selectedLocation.locationCode,
        zone: this.selectedLocation.zone,
        rack: this.selectedLocation.rack,
        bin: this.selectedLocation.bin,
        barcode: this.selectedLocation.barcode,
        capacity: this.selectedLocation.capacity,
        status: this.selectedLocation.status,
      });
      this.modalVisible = true;
    } else {
      this.messageService.add({
        key: 'globalMessage',
        severity: 'warn',
        summary: 'Warning',
        detail: 'Please select a warehouse location.',
      });
    }
  }

  delete(): void {
    if (this.selectedLocation != null) {
      const locationId = this.selectedLocation.id;
      this.confirmationService.confirm({
        message: `Are you sure you want to delete location "${this.selectedLocation.locationCode}"?`,
        header: 'Delete Confirmation',
        icon: 'pi pi-info-circle',
        key: 'positionDialog',
        accept: () => {
          this.locationService.delete(locationId).subscribe({
            next: (res) => {
              this.selectedLocation = null;
              this.messageService.add({
                key: 'globalMessage',
                severity: 'success',
                summary: 'Confirmed',
                detail: res.message ?? 'Warehouse location was deleted successfully.',
              });
              this.loadData();
              this.cdr.detectChanges();
            },
            error: (err) => {
              this.messageService.add({
                key: 'globalMessage',
                severity: 'error',
                summary: 'Error',
                detail: err.error?.message ? err.error.message.toString() : 'Failed to delete location.',
              });
            },
          });
        },
        reject: () => {
          this.selectedLocation = null;
        },
      });
    } else {
      this.messageService.add({
        key: 'globalMessage',
        severity: 'warn',
        summary: 'Warning',
        detail: 'Please select a warehouse location.',
      });
    }
  }

  viewBarcode(location: WarehouseLocationModel): void {
    this.selectedBarcodeValue = location.barcode || location.locationCode || null;
    this.barcodeModalVisible = true;
  }

  onBarcodeDialogHide(): void {
    this.selectedBarcodeValue = null;
    this.barcodeModalVisible = false;
  }

  excel(): void {
    this.exportService.excelAll('WarehouseLocations', this.tblLocations);
  }
}
