import { CommonModule } from '@angular/common';
import { ChangeDetectorRef, Component, inject, OnInit } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { SharedService } from '../../core/services/shared.service';
import { ButtonModule } from 'primeng/button';
import { SelectModule } from 'primeng/select';
import { CompanyAccountModel } from '../../core/models/company-account.model';
import { AuthService } from '../../core/services/auth.service';
import { CompanySettingsService } from '../../core/services/company-settings.service';

@Component({
  selector: 'app-settings',
  imports: [CommonModule, ReactiveFormsModule, ButtonModule, SelectModule, RouterLink],
  templateUrl: './settings.html',
  styleUrl: '../auth/login/login.scss',
})
export class Settings implements OnInit {
  companies: CompanyAccountModel[] = [];
  isLoading: boolean = false;
  isSubmitting: boolean = false;
  errorMessage: string = '';
  private formBuilder = inject(FormBuilder);
  public settingsForm = this.formBuilder.group({
    companyId: [null as number | null, Validators.required],
  });

  constructor(
    private companySettingsService: CompanySettingsService,
    public authService: AuthService,
    public sharedService: SharedService,
    private router: Router,
    private cdr: ChangeDetectorRef,
  ) {}

  ngOnInit(): void {
    this.settingsForm.reset({ companyId: this.authService.getCompanyId() });
    if (this.authService.canSelectCompany()) this.loadData();
  }

  loadData(): void {
    this.isLoading = true;
    this.errorMessage = '';
    this.companySettingsService.get().subscribe({
      next: (res) => {
        this.companies = res.data as CompanyAccountModel[];
        this.isLoading = false;
        this.cdr.detectChanges();
      },
      error: (err) => {
        this.isLoading = false;
        this.errorMessage = err.status === 404
          ? 'Company selection is unavailable. Rebuild and restart the API, then try again.'
          : err.error?.message || err.error?.detail || 'Unable to load company accounts.';
        this.cdr.detectChanges();
      }
    });
  }

  logout(): void {
    this.authService.logout().subscribe(() => this.router.navigate(['/auth/login']));
  }

  onSubmit(): void {
    if (this.isSubmitting) return;
    if (this.settingsForm.invalid) {
      this.settingsForm.markAllAsTouched();
      return;
    }
    this.isSubmitting = true;
    this.errorMessage = '';
    this.authService.selectCompany(this.settingsForm.controls.companyId.value!).subscribe({
      next: (res) => {
        if (res.success) {
          // Reload clears cached inventory screens after changing company cookies.
          this.router.navigate(['/dashboard']).then(() => window.location.reload());
        } else {
          this.isSubmitting = false;
          this.errorMessage = res.message || 'Unable to select company account.';
          this.cdr.detectChanges();
        }
      },
      error: (err) => {
        this.isSubmitting = false;
        this.errorMessage = err.error?.message || err.error?.detail || 'Unable to select company account.';
        this.cdr.detectChanges();
      }
    });
  }
}
