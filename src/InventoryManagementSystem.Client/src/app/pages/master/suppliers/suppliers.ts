import { ChangeDetectorRef, Component, inject, OnInit, ViewChild } from '@angular/core';
import { ToastModule } from "primeng/toast";
import { ConfirmDialogModule } from "primeng/confirmdialog";
import { SplitButtonModule } from "primeng/splitbutton";
import { IconFieldModule } from "primeng/iconfield";
import { InputIconModule } from "primeng/inputicon";
import { Table, TableModule } from "primeng/table";
import { SuppliersService } from '../../../core/services/suppliers.service';
import { ConfirmationService, MenuItem, MessageService } from 'primeng/api';
import { SuppliersModel } from '../../../core/models/suppliers.model';
import { InputTextModule } from 'primeng/inputtext';
import { CommonModule, DatePipe } from '@angular/common';
import { Tag } from "primeng/tag";
import { DialogModule } from "primeng/dialog";
import { FormBuilder, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';
import { ToggleSwitchModule } from "primeng/toggleswitch";
import { ButtonModule } from "primeng/button";
import { ExportService } from '../../../core/services/export.service';

@Component({
  selector: 'app-suppliers',
  imports: [
    ToastModule,
    ReactiveFormsModule,
    FormsModule,
    CommonModule,
    ConfirmDialogModule,
    SplitButtonModule,
    IconFieldModule,
    InputIconModule,
    InputTextModule,
    TableModule,
    Tag,
    DialogModule,
    ToggleSwitchModule,
    ButtonModule
  ],
  providers: [ConfirmationService, MessageService, DatePipe, ExportService],
  templateUrl: './suppliers.html',
  styleUrl: './suppliers.scss',
})
export class Suppliers implements OnInit {
  @ViewChild(Table) tblSupplier!: Table;
  items: MenuItem[] = [];
  isLoading: boolean = false;
  suppliers: SuppliersModel[] = [];
  selectedSupplier: SuppliersModel | null = null;
  isEdit: boolean = false;
  modalVisible: boolean = false;
  isSubmitting: boolean = false;

  private formBuilder = inject(FormBuilder);
  public supplierForm = this.formBuilder.group({
    id: [0],
    supplierCode: ['', Validators.required],
    companyName: ['', Validators.required],
    contactPerson: [''],
    phone: [''],
    email: [''],
    address: [''],
    paymentTerms: ['', Validators.required],
    creditLimit: [0],
    status: [true],
    createdOn: [null as any],
    createdBy: [null as any],
    updatedOn: [null as any],
    updatedBy: [null as any],
    deletedOn: [null as any],
    deletedBy: [null as any],
  })

  constructor(
    private suppliersService: SuppliersService,
    private confirmationService: ConfirmationService,
    private dataPipe: DatePipe,
    private messageService: MessageService,
    private cdr: ChangeDetectorRef,
    private exportService: ExportService,
  ) {
    this.items = [
      {
        label: 'Edit',
        icon: 'pi pi-pencil',
        command: () => this.update()
      },
      {
        label: 'Delete',
        icon: 'pi pi-trash',
        command: () => this.delete()
      },
      {
        label: 'Excel',
        icon: 'pi pi-file-excel',
        command: () => this.excel()
      }
    ];
  }

  ngOnInit(): void {
    this.loadData();
  }

  loadData(): void {
    this.isLoading = true;
    this.suppliersService.get().subscribe({
      next: (res) => {
        this.suppliers = res.data as SuppliersModel[];
        this.isLoading = false;
        this.cdr.detectChanges();
      },
      error: (err) => {
        this.isLoading = false;
        this.cdr.detectChanges();
      }
    })
  }

  onDialogHide(): void {
    this.modalVisible = false;
    this.selectedSupplier = null;
    this.isEdit = false;
  }

  create(): void {
    this.isEdit = false;
    this.modalVisible = true;
    this.selectedSupplier = null;
    this.supplierForm.reset();
  }

  onSubmit(): void {
    this.isSubmitting = true;
    if (this.supplierForm.valid) {
      let model = this.supplierForm.value as SuppliersModel;
      if (!this.isEdit) {
        this.suppliersService.create(model).subscribe({
          next: (res) => {
            if (res.success) {
              this.isSubmitting = false;
              this.modalVisible = false;
              this.messageService.add({ severity: 'success', summary: 'Success', detail: 'Supplier created successfully.' });
              this.supplierForm.reset();
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
              detail: err.error?.message ? err.error.message.toString() : 'An error occurred while creating the supplier.'
            })
          },
        })
        this.isSubmitting = false;
      }
      else {
        this.suppliersService.update(model).subscribe({
          next: (res) => {
            if (res.success) {
              this.isSubmitting = false;
              this.modalVisible = false;
              this.isEdit = false;
              this.supplierForm.reset();
              this.messageService.add({
                key: 'globalMessage',
                severity: 'success',
                summary: 'warnning',
                detail: 'Update Successfully.'
              })
              this.loadData();
              this.cdr.detectChanges();
            }
          },
          error: (_) => {
            this.isSubmitting = false;
          },
        })
      }
    }
    else {
      Object.keys(this.supplierForm.controls).forEach(field => {
        const control = this.supplierForm.get(field);
        control?.markAsDirty({ onlySelf: true });
      });
      this.isSubmitting = false;
    }
  }

  update(): void {
    this.isEdit = true;
    this.supplierForm.reset();
    this.modalVisible = true;
    if (this.selectedSupplier) {
      this.supplierForm.patchValue({
        id: this.selectedSupplier.id,
        supplierCode: this.selectedSupplier.supplierCode,
        companyName: this.selectedSupplier.companyName,
        contactPerson: this.selectedSupplier.contactPerson,
        phone: this.selectedSupplier.phone,
        email: this.selectedSupplier.email,
        address: this.selectedSupplier.address,
        paymentTerms: this.selectedSupplier.paymentTerms,
        creditLimit: this.selectedSupplier.creditLimit,
        status: this.selectedSupplier.status,
      });
    }
    else {
      this.messageService.add({
        key: 'globalMessage',
        severity: 'warn',
        summary: 'warnning',
        detail: 'Please select supplier.'
      })
    }
  }

  delete(): void {
    if (this.selectedSupplier != null) {
      const supplierId = this.selectedSupplier.id;
      this.confirmationService.confirm({
        message: 'Are you sure to delete this supplier?',
        header: 'Delete Confirmation',
        icon: 'pi pi-info-circle',
        key: 'positionDialog',
        accept: () => {
          this.suppliersService.delete(supplierId).subscribe({
            next: (res) => {
              this.selectedSupplier = null;
              this.messageService.add({
                key: 'globalMessage',
                severity: 'success',
                summary: 'Confirmed',
                detail: res.message ?? 'Supplier was deleted successfully.',
              });
              this.loadData();
              this.cdr.detectChanges();
            }
          });
        },
        reject: () => {
          this.selectedSupplier = null as any;
        },
      })
    }
    else {
      this.messageService.add({
        key: 'globalMessage',
        severity: 'warn',
        summary: 'Warning',
        detail: 'Please Select a Supplier',
      });
    }
  }

  excel(): void {
    this.exportService.excelAll('Suppliers', this.tblSupplier);
  }
}
