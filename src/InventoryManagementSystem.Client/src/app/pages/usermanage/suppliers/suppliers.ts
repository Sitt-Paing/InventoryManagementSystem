import { ChangeDetectorRef, Component, DestroyRef, inject, OnInit, ViewChild } from '@angular/core';
import { ToastModule } from "primeng/toast";
import { ConfirmDialogModule } from "primeng/confirmdialog";
import { SplitButtonModule } from "primeng/splitbutton";
import { IconFieldModule } from "primeng/iconfield";
import { InputIconModule } from "primeng/inputicon";
import { Table, TableModule } from "primeng/table";
import { ConfirmationService, MenuItem, MessageService } from 'primeng/api';
import { SuppliersModel } from '../../../core/models/master/suppliers.model';
import { InputTextModule } from 'primeng/inputtext';
import { PasswordModule } from 'primeng/password';
import { CommonModule, DatePipe } from '@angular/common';
import { Tag } from "primeng/tag";
import { DialogModule } from "primeng/dialog";
import { FormBuilder, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';
import { ToggleSwitchModule } from "primeng/toggleswitch";
import { ButtonModule } from "primeng/button";
import { ExportService } from '../../../core/services/export.service';
import { SuppliersService } from '../../../core/services/master/suppliers.service';
import { SupplierAccountsService } from '../../../core/services/usermanage/supplier-accounts.service';
import { AuthService } from '../../../core/services/auth.service';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { SupplierAccountModel } from '../../../core/models/usermanage/supplier-account.model';

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
    PasswordModule,
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

  createLoginAccount: boolean = false;
  savedSupplierId: number | null = null;
  formError: string = '';
  accountsVisible: boolean = false;
  isAccountsLoading: boolean = false;
  isDeletingAccount: boolean = false;
  accountsError: string = '';
  accountSupplier: SuppliersModel | null = null;
  supplierAccounts: SupplierAccountModel[] = [];
  selectedAccount: SupplierAccountModel | null = null;
  private destroyRef = inject(DestroyRef);
  private formBuilder = inject(FormBuilder);
  public accountForm = this.formBuilder.nonNullable.group({
    userName: ['', [Validators.required, Validators.maxLength(50)]],
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required, Validators.minLength(6)]],
  });
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
    private supplierAccountsService: SupplierAccountsService,
    private authService: AuthService,
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
        label: 'Login Accounts',
        icon: 'pi pi-user',
        command: () => this.openAccounts(),
        visible: this.canCreateAccount
      },
      {
        label: 'Excel',
        icon: 'pi pi-file-excel',
        command: () => this.excel()
      }
    ];
  }

  get canCreateAccount(): boolean {
    const roles = this.authService.getUserRoles().map(role => role.toLowerCase());
    return !roles.includes('supplier') && roles.some(role =>
      ['superadmin', 'administrator', 'companyadmin'].includes(role));
  }

  ngOnInit(): void {
    this.loadData();
  }

  openAccounts(): void {
    if (!this.canCreateAccount) return;
    if (!this.selectedSupplier) {
      this.messageService.add({ key: 'globalMessage', severity: 'warn', summary: 'Warning', detail: 'Please select supplier.' });
      return;
    }
    this.accountSupplier = this.selectedSupplier;
    this.supplierAccounts = [];
    this.selectedAccount = null;
    this.accountsVisible = true;
    this.loadAccounts();
  }

  loadAccounts(): void {
    if (!this.accountSupplier) return;
    this.isAccountsLoading = true;
    this.accountsError = '';
    this.supplierAccountsService.get(this.accountSupplier.id).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (res) => {
        this.isAccountsLoading = false;
        this.supplierAccounts = res.success ? res.data as SupplierAccountModel[] : [];
        if (!res.success) this.accountsError = res.message || 'Unable to load login accounts.';
        this.cdr.markForCheck();
      },
      error: (err) => {
        this.isAccountsLoading = false;
        this.accountsError = this.getErrorMessage(err);
        this.cdr.markForCheck();
      }
    });
  }

  deleteAccount(): void {
    if (!this.selectedAccount || !this.canCreateAccount || this.isDeletingAccount) return;
    const account = this.selectedAccount;
    this.confirmationService.confirm({
      key: 'positionDialog',
      header: 'Delete Login Account',
      message: `Delete login account '${account.userName}'? The supplier and purchase documents will remain.`,
      icon: 'pi pi-exclamation-triangle',
      accept: () => {
        this.isDeletingAccount = true;
        this.accountsError = '';
        this.supplierAccountsService.delete(account.userId).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
          next: (res) => {
            this.isDeletingAccount = false;
            if (res.success) {
              this.selectedAccount = null;
              this.messageService.add({ key: 'globalMessage', severity: 'success', summary: 'Success', detail: res.message });
              this.loadAccounts();
            } else {
              this.accountsError = res.message || 'Unable to delete login account.';
            }
            this.cdr.markForCheck();
          },
          error: (err) => {
            this.isDeletingAccount = false;
            this.accountsError = this.getErrorMessage(err);
            this.cdr.markForCheck();
          }
        });
      }
    });
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
    if (this.savedSupplierId !== null) this.loadData();
    this.modalVisible = false;
    this.selectedSupplier = null;
    this.isEdit = false;
    this.savedSupplierId = null;
    this.createLoginAccount = false;
    this.accountForm.reset();
    this.formError = '';
  }

  create(): void {
    this.isEdit = false;
    this.modalVisible = true;
    this.selectedSupplier = null;
    this.supplierForm.reset({ id: 0, status: true, creditLimit: 0 });
    this.savedSupplierId = null;
    this.createLoginAccount = false;
    this.accountForm.reset();
    this.formError = '';
  }

  onSubmit(): void {
    if (this.isSubmitting) return;
    this.formError = '';
    if (this.supplierForm.invalid || (this.createLoginAccount && this.accountForm.invalid)) {
      this.supplierForm.markAllAsTouched();
      Object.values(this.supplierForm.controls).forEach(control => control.markAsDirty());
      if (this.createLoginAccount) this.accountForm.markAllAsTouched();
      return;
    }
    if (this.createLoginAccount && (!this.canCreateAccount || !this.supplierForm.controls.status.value)) {
      this.formError = 'Only administrators can create login accounts for active suppliers.';
      return;
    }
    this.isSubmitting = true;
    if (this.savedSupplierId !== null) {
      this.saveAccount(this.savedSupplierId);
      return;
    }
    const model = this.supplierForm.getRawValue() as SuppliersModel;
    const request = this.isEdit ? this.suppliersService.update(model) : this.suppliersService.create(model);
    request.pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (res) => {
        if (!res.success) {
          this.showError(res.message || 'Unable to save supplier.');
          return;
        }
        if (this.createLoginAccount) {
          this.savedSupplierId = (res.data as SuppliersModel).id;
          this.saveAccount(this.savedSupplierId);
        } else {
          this.completeSave('Supplier saved successfully.');
        }
      },
      error: (err) => this.showError(this.getErrorMessage(err))
    });
  }

  private saveAccount(supplierId: number): void {
    this.supplierAccountsService.create({ supplierId, ...this.accountForm.getRawValue() })
      .pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
        next: (res) => {
          if (res.success) {
            this.completeSave('Supplier and login account saved successfully.');
          } else {
            this.showError('Supplier saved. Login account not created: ' + (res.message || 'Please try again.'));
          }
        },
        error: (err) => this.showError('Supplier saved. Login account not created: ' + this.getErrorMessage(err))
      });
  }

  private completeSave(message: string): void {
    this.isSubmitting = false;
    this.modalVisible = false;
    this.messageService.add({ key: 'globalMessage', severity: 'success', summary: 'Success', detail: message });
    this.accountForm.reset();
    this.savedSupplierId = null;
    this.loadData();
    this.cdr.markForCheck();
  }

  private showError(message: string): void {
    this.isSubmitting = false;
    this.formError = message;
    this.cdr.markForCheck();
  }

  private getErrorMessage(err: any): string {
    const body = err.error;
    const errors: string[] = Array.isArray(body?.data) ? body.data
      : Object.values(body?.errors || {}).flat() as string[];
    return errors.length ? errors.join(' ')
      : body?.message || body?.detail || 'Unable to save supplier.';
  }

  onAccountChange(): void {
    this.accountForm.reset({ email: this.supplierForm.controls.email.value || '' });
    this.formError = '';
  }

  update(): void {
    if (!this.selectedSupplier) {
      this.messageService.add({ key: 'globalMessage', severity: 'warn', summary: 'Warning', detail: 'Please select supplier.' });
      return;
    }
    this.isEdit = true;
    this.savedSupplierId = null;
    this.createLoginAccount = false;
    this.accountForm.reset();
    this.formError = '';
    this.supplierForm.reset();
    this.supplierForm.patchValue(this.selectedSupplier as any);
    this.modalVisible = true;
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
