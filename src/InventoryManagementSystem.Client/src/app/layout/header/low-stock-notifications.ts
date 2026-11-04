import { DatePipe, DecimalPipe } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { Component, Input, OnChanges, OnDestroy, ViewChild, inject, signal } from '@angular/core';
import { Popover, PopoverModule } from 'primeng/popover';
import { Subscription } from 'rxjs';
import { environment } from '../../../environments/environment';
import { TranslatePipe } from '../../core/pipes/translate.pipe';

interface LowStockNotification {
  id: string;
  productName: string;
  currentQuantity: number;
  reorderLevel: number;
  createdOn: string | null;
}

@Component({
  selector: 'app-low-stock-notifications',
  imports: [DatePipe, DecimalPipe, PopoverModule, TranslatePipe],
  templateUrl: './low-stock-notifications.html',
  styleUrl: './low-stock-notifications.scss'
})
export class LowStockNotifications implements OnChanges, OnDestroy {
  @Input({ required: true }) companyId!: number;
  @ViewChild(Popover) private popover?: Popover;

  private readonly http = inject(HttpClient);
  private request?: Subscription;

  readonly notifications = signal<LowStockNotification[]>([]);
  readonly loading = signal(false);
  readonly failed = signal(false);

  ngOnChanges(): void {
    this.clear();
    this.popover?.hide();
  }

  ngOnDestroy(): void {
    this.request?.unsubscribe();
  }

  load(): void {
    this.clear();
    this.loading.set(true);
    this.request = this.http.get<LowStockNotification[]>(
      `${environment.main_url}/process/low-stock-notifications`
    ).subscribe({
      next: notifications => {
        this.notifications.set(notifications);
        this.loading.set(false);
      },
      error: () => {
        this.failed.set(true);
        this.loading.set(false);
      }
    });
  }

  clear(): void {
    this.request?.unsubscribe();
    this.notifications.set([]);
    this.loading.set(false);
    this.failed.set(false);
  }
}
