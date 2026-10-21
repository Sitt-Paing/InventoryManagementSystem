import { CommonModule } from '@angular/common';
import { ChangeDetectorRef, Component, DestroyRef, inject, OnInit } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { Subscription } from 'rxjs';
import { ButtonModule } from 'primeng/button';
import { DatePickerModule } from 'primeng/datepicker';
import { AuthService } from '../../../core/services/auth.service';
import { SupplierDocumentsService } from '../../../core/services/supplier/supplier-documents.service';
import { SupplierDashboardModel } from '../../../core/models/supplier/supplier-document.model';
import { TranslatePipe } from '../../../core/pipes/translate.pipe';

@Component({
  selector: 'app-supplier-dashboard',
  imports: [CommonModule, FormsModule, RouterLink, ButtonModule, DatePickerModule, TranslatePipe],
  templateUrl: './dashboard.html',
})
export class SupplierDashboard implements OnInit {
  summary: SupplierDashboardModel | null = null;
  startDate: Date | null = null;
  endDate: Date | null = null;
  isLoading: boolean = false;
  errorMessage: string = '';
  private destroyRef = inject(DestroyRef);
  private request?: Subscription;

  constructor(
    public authService: AuthService,
    private documentsService: SupplierDocumentsService,
    private cdr: ChangeDetectorRef,
  ) {}

  ngOnInit(): void {
    this.loadData();
  }

  loadData(): void {
    this.request?.unsubscribe();
    this.errorMessage = '';
    this.summary = null;
    if (this.startDate && this.endDate && this.startDate > this.endDate) {
      this.errorMessage = 'Start date must not be after end date.';
      this.isLoading = false;
      return;
    }
    this.isLoading = true;
    this.request = this.documentsService.getDashboard(this.formatDate(this.startDate), this.formatDate(this.endDate))
      .pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
        next: (res) => {
          this.isLoading = false;
          if (res.success) this.summary = res.data;
          else this.errorMessage = res.message || 'Unable to load dashboard.';
          this.cdr.markForCheck();
        },
        error: (err) => {
          this.isLoading = false;
          this.errorMessage = err.error?.message || 'Unable to load dashboard.';
          this.cdr.markForCheck();
        }
      });
  }

  clearFilters(): void {
    this.startDate = null;
    this.endDate = null;
    this.loadData();
  }

  private formatDate(date: Date | null): string | null {
    if (!date) return null;
    return `${date.getFullYear()}-${String(date.getMonth() + 1).padStart(2, '0')}-${String(date.getDate()).padStart(2, '0')}`;
  }
}
