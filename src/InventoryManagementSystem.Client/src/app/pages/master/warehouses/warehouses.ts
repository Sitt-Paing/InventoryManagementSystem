import { ChangeDetectorRef, Component, inject, OnInit, ViewChild } from '@angular/core';
import { ToastModule } from 'primeng/toast';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { SplitButtonModule } from 'primeng/splitbutton';
import { IconFieldModule } from 'primeng/iconfield';
import { InputIconModule } from 'primeng/inputicon';
import { Table, TableModule } from 'primeng/table';
import { WarehouseService } from '../../../core/services/warehouse.service';
import { ConfirmationService, MenuItem, MessageService } from 'primeng/api';
import { WarehouseModel } from '../../../core/models/warehouse.model';
import { InputTextModule } from 'primeng/inputtext';
import { CommonModule, DatePipe, DecimalPipe } from '@angular/common';
import { Tag } from 'primeng/tag';
import { DialogModule } from 'primeng/dialog';
import { FormBuilder, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';
import { ToggleSwitchModule } from 'primeng/toggleswitch';
import { ButtonModule } from 'primeng/button';
import { ExportService } from '../../../core/services/export.service';

@Component({
  selector: 'app-warehouses',
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
  ],
  providers: [ConfirmationService, MessageService, DatePipe, DecimalPipe, ExportService],
  templateUrl: './warehouses.html',
  styleUrl: './warehouses.scss',
})
export class Warehouses implements OnInit {
  @ViewChild(Table) tblWarehouses!: Table;
  items: MenuItem[] = [];
  isLoading: boolean = false;
  warehouses: WarehouseModel[] = [];
  selectedWarehouse: WarehouseModel | null = null;
  isEdit: boolean = false;
  modalVisible: boolean = false;
  isSubmitting: boolean = false;

  private formBuilder = inject(FormBuilder);
  public warehouseForm = this.formBuilder.group({
    id: [0],
    warehouseCode: ['', Validators.required],
    name: ['', Validators.required],
    contactPerson: [''],
    phone: [''],
    email: [''],
    address: [''],
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
    this.loadData();
  }

  loadData(): void {
    this.isLoading = true;
    this.warehouseService.get().subscribe({
      next: (res) => {
        this.warehouses = (res.data as WarehouseModel[]) ?? [];
        this.isLoading = false;
        this.cdr.detectChanges();
      },
      error: (err) => {
        this.isLoading = false;
        this.cdr.detectChanges();
      },
    });
  }

  onDialogHide(): void {
    this.modalVisible = false;
    this.selectedWarehouse = null;
    this.isEdit = false;
  }

  create(): void {
    this.isEdit = false;
    this.selectedWarehouse = null;
    this.warehouseForm.reset({
      id: 0,
      warehouseCode: '',
      name: '',
      contactPerson: '',
      phone: '',
      email: '',
      address: '',
      capacity: null,
      status: true,
    });
    this.modalVisible = true;
  }

  onSubmit(): void {
    this.isSubmitting = true;
    if (this.warehouseForm.valid) {
      const model = this.warehouseForm.value as WarehouseModel;
      if (!this.isEdit) {
        this.warehouseService.create(model).subscribe({
          next: (res) => {
            this.isSubmitting = false;
            if (res.success) {
              this.modalVisible = false;
              this.messageService.add({
                key: 'globalMessage',
                severity: 'success',
                summary: 'Success',
                detail: 'Warehouse created successfully.',
              });
              this.warehouseForm.reset();
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
              detail: err.error?.message ? err.error.message.toString() : 'An error occurred while creating the warehouse.',
            });
          },
        });
      } else {
        this.warehouseService.update(model).subscribe({
          next: (res) => {
            this.isSubmitting = false;
            if (res.success) {
              this.modalVisible = false;
              this.isEdit = false;
              this.selectedWarehouse = null;
              this.warehouseForm.reset();
              this.messageService.add({
                key: 'globalMessage',
                severity: 'success',
                summary: 'Success',
                detail: 'Warehouse updated successfully.',
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
              detail: err.error?.message ? err.error.message.toString() : 'An error occurred while updating the warehouse.',
            });
          },
        });
      }
    } else {
      Object.keys(this.warehouseForm.controls).forEach((field) => {
        const control = this.warehouseForm.get(field);
        control?.markAsDirty({ onlySelf: true });
      });
      this.isSubmitting = false;
    }
  }

  update(): void {
    if (this.selectedWarehouse) {
      this.isEdit = true;
      this.warehouseForm.reset();
      this.warehouseForm.patchValue({
        id: this.selectedWarehouse.id,
        warehouseCode: this.selectedWarehouse.warehouseCode,
        name: this.selectedWarehouse.name,
        contactPerson: this.selectedWarehouse.contactPerson,
        phone: this.selectedWarehouse.phone,
        email: this.selectedWarehouse.email,
        address: this.selectedWarehouse.address,
        capacity: this.selectedWarehouse.capacity,
        status: this.selectedWarehouse.status,
      });
      this.modalVisible = true;
    } else {
      this.messageService.add({
        key: 'globalMessage',
        severity: 'warn',
        summary: 'Warning',
        detail: 'Please select a warehouse.',
      });
    }
  }

  delete(): void {
    if (this.selectedWarehouse != null) {
      const warehouseId = this.selectedWarehouse.id;
      this.confirmationService.confirm({
        message: `Are you sure you want to delete warehouse "${this.selectedWarehouse.name}"?`,
        header: 'Delete Confirmation',
        icon: 'pi pi-info-circle',
        key: 'positionDialog',
        accept: () => {
          this.warehouseService.delete(warehouseId).subscribe({
            next: (res) => {
              this.selectedWarehouse = null;
              this.messageService.add({
                key: 'globalMessage',
                severity: 'success',
                summary: 'Confirmed',
                detail: res.message ?? 'Warehouse was deleted successfully.',
              });
              this.loadData();
              this.cdr.detectChanges();
            },
            error: (err) => {
              this.messageService.add({
                key: 'globalMessage',
                severity: 'error',
                summary: 'Error',
                detail: err.error?.message ? err.error.message.toString() : 'Failed to delete warehouse.',
              });
            },
          });
        },
        reject: () => {
          this.selectedWarehouse = null;
        },
      });
    } else {
      this.messageService.add({
        key: 'globalMessage',
        severity: 'warn',
        summary: 'Warning',
        detail: 'Please select a warehouse.',
      });
    }
  }

  excel(): void {
    this.exportService.excelAll('Warehouses', this.tblWarehouses);
  }
}
