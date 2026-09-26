import { ChangeDetectorRef, Component, inject, OnInit, ViewChild } from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';
import { FormBuilder, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';
import { Table, TableModule } from 'primeng/table';
import { ToastModule } from 'primeng/toast';
import { ButtonModule } from 'primeng/button';
import { DialogModule } from 'primeng/dialog';
import { Tag } from 'primeng/tag';
import { IconFieldModule } from 'primeng/iconfield';
import { InputIconModule } from 'primeng/inputicon';
import { InputTextModule } from 'primeng/inputtext';
import { PasswordModule } from 'primeng/password';
import { MessageService } from 'primeng/api';

import { CompanyModel, RegisterCompanyPayload } from '../../../core/models/master/company.model';
import { CompanyService } from '../../../core/services/master/company.service';

@Component({
  selector: 'app-company',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    ReactiveFormsModule,
    ToastModule,
    TableModule,
    ButtonModule,
    DialogModule,
    Tag,
    IconFieldModule,
    InputIconModule,
    InputTextModule,
    PasswordModule,
    DatePipe
  ],
  providers: [MessageService, DatePipe],
  templateUrl: './company.html',
  styleUrl: './company.scss',
})
export class CompanyComponent implements OnInit {
  @ViewChild('tblCompanies') tblCompanies!: Table;

  isLoading: boolean = false;
  isSubmitting: boolean = false;
  modalVisible: boolean = false;

  companies: CompanyModel[] = [];
  filteredCompanies: CompanyModel[] = [];
  selectedCompany: CompanyModel | null = null;
  searchKeyword: string = '';

  private formBuilder = inject(FormBuilder);
  private companyService = inject(CompanyService);
  private messageService = inject(MessageService);
  private cdr = inject(ChangeDetectorRef);

  public companyForm = this.formBuilder.group({
    companyName: ['', [Validators.required, Validators.maxLength(200)]],
    contactPerson: [''],
    companyEmail: ['', [Validators.email]],
    phone: [''],
    address: [''],
    adminUserName: ['', [Validators.required, Validators.maxLength(50)]],
    adminEmail: ['', [Validators.required, Validators.email]],
    adminPassword: ['', [Validators.required, Validators.minLength(6)]]
  });

  ngOnInit(): void {
    this.loadCompanies();
  }

  loadCompanies(): void {
    this.isLoading = true;
    this.companyService.get().subscribe({
      next: (res) => {
        this.companies = (res.data || []) as CompanyModel[];
        this.applyFilter();
        this.isLoading = false;
        this.cdr.markForCheck();
      },
      error: (err) => {
        this.isLoading = false;
        this.messageService.add({
          key: 'globalMessage',
          severity: 'error',
          summary: 'Error',
          detail: err.error?.message || 'Failed to load companies.'
        });
        this.cdr.markForCheck();
      }
    });
  }

  onSearch(): void {
    this.applyFilter();
  }

  applyFilter(): void {
    if (!this.searchKeyword.trim()) {
      this.filteredCompanies = [...this.companies];
    } else {
      const q = this.searchKeyword.toLowerCase().trim();
      this.filteredCompanies = this.companies.filter(c =>
        c.companyName?.toLowerCase().includes(q) ||
        c.contactPerson?.toLowerCase().includes(q) ||
        c.email?.toLowerCase().includes(q) ||
        c.phone?.toLowerCase().includes(q)
      );
    }
  }

  openRegisterModal(): void {
    this.companyForm.reset({
      companyName: '',
      contactPerson: '',
      companyEmail: '',
      phone: '',
      address: '',
      adminUserName: '',
      adminEmail: '',
      adminPassword: ''
    });
    this.modalVisible = true;
  }

  onDialogHide(): void {
    this.modalVisible = false;
    this.companyForm.reset();
  }

  onSubmit(): void {
    if (this.companyForm.invalid) {
      this.companyForm.markAllAsTouched();
      return;
    }

    const val = this.companyForm.value;
    const payload: RegisterCompanyPayload = {
      companyName: val.companyName!.trim(),
      contactPerson: val.contactPerson?.trim() || undefined,
      companyEmail: val.companyEmail?.trim() || undefined,
      phone: val.phone?.trim() || undefined,
      address: val.address?.trim() || undefined,
      adminUserName: val.adminUserName!.trim(),
      adminEmail: val.adminEmail!.trim(),
      adminPassword: val.adminPassword!,
      role: 'CompanyAdmin'
    };

    this.isSubmitting = true;
    this.companyService.registerWithAdmin(payload).subscribe({
      next: (res) => {
        this.isSubmitting = false;
        this.modalVisible = false;
        this.messageService.add({
          key: 'globalMessage',
          severity: 'success',
          summary: 'Registered',
          detail: res.message || 'Client Company & Admin registered successfully.'
        });
        this.loadCompanies();
      },
      error: (err) => {
        this.isSubmitting = false;
        this.messageService.add({
          key: 'globalMessage',
          severity: 'error',
          summary: 'Registration Failed',
          detail: err.error?.message || 'Failed to register company and admin.'
        });
        this.cdr.markForCheck();
      }
    });
  }

  toggleCompanyStatus(company: CompanyModel): void {
    if (!company.id) return;

    this.companyService.toggleStatus(company.id).subscribe({
      next: (res) => {
        company.isActive = !company.isActive;
        this.messageService.add({
          key: 'globalMessage',
          severity: 'success',
          summary: 'Status Updated',
          detail: res.message || `Company status updated.`
        });
        this.cdr.markForCheck();
      },
      error: (err) => {
        this.messageService.add({
          key: 'globalMessage',
          severity: 'error',
          summary: 'Error',
          detail: err.error?.message || 'Failed to update company status.'
        });
      }
    });
  }
}
