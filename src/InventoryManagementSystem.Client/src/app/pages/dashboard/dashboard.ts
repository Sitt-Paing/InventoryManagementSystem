import { Component, computed, effect, inject, signal, untracked } from '@angular/core';
import { DatePipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { FormsModule } from '@angular/forms';
import { takeUntilDestroyed, toSignal } from '@angular/core/rxjs-interop';
import { catchError, distinctUntilChanged, interval, map, of } from 'rxjs';
import { DashboardFacts, DashboardGeneration, DashboardWidget } from '../../core/models/process/dashboard.model';
import { DashboardService } from '../../core/services/process/dashboard.service';
import { TranslationService } from '../../core/services/translation.service';
import { AuthService } from '../../core/services/auth.service';
import { TranslatePipe } from '../../core/pipes/translate.pipe';
import { ButtonModule } from 'primeng/button';
import { ProgressBarModule } from 'primeng/progressbar';
import { DashboardWidgetComponent } from './dashboard-widget';
import { DatePickerModule } from 'primeng/datepicker';

interface DashboardLoad {
  dashboard: DashboardGeneration | null;
  facts: DashboardFacts | null;
  status: number | null;
}

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [DatePipe, FormsModule, TranslatePipe, ButtonModule, DatePickerModule, ProgressBarModule, DashboardWidgetComponent],
  templateUrl: './dashboard.html',
  styleUrl: './dashboard.scss'
})
export class DashboardComponent {
  private readonly dashboardService = inject(DashboardService);
  private readonly translationService = inject(TranslationService);
  private readonly auth = inject(AuthService);
  readonly language = this.translationService.currentLanguage;
  private readonly scope = toSignal(this.auth.currentUser$.pipe(
    map(user => user?.companyId ? `${user.userId}:${user.companyId}` : ''),
    distinctUntilChanged()
  ));
  private readonly refreshVersion = signal(0);
  private readonly retryAt = signal(0);
  private readonly clock = signal(Date.now());

  readonly dashboard = signal<DashboardGeneration | null>(null);
  readonly facts = signal<DashboardFacts | null>(null);
  readonly loading = signal(false);
  readonly errorStatus = signal<number | null>(null);
  readonly hasError = signal(false);
  readonly invalidRange = signal(false);
  readonly retrySeconds = computed(() => Math.max(0, Math.ceil((this.retryAt() - this.clock()) / 1000)));
  readonly today = this.formatDate(new Date());
  readonly maxDate = new Date();
  readonly range = signal({ startDate: `${this.today.slice(0, 7)}-01`, endDate: this.today });
  draftStart: Date | null = new Date(`${this.range().startDate}T00:00:00`);
  draftEnd: Date | null = new Date(`${this.range().endDate}T00:00:00`);

  readonly standardWidgets = computed<DashboardWidget[]>(() => {
    this.translationService.currentLanguage();
    const text = (key: string) => this.translationService.instant(key);
    const label = (field: string, key: string) => ({ field, text: text(key) });
    const widgets: DashboardWidget[] = [
      {
        id: 'standard-products', type: 'metric_card', datasetKey: 'activeProductCount',
        title: text('DASHBOARD.ACTIVE_PRODUCTS'), description: '', productIds: [], labels: []
      },
      {
        id: 'standard-orders', type: 'metric_card', datasetKey: 'openPurchaseOrderCount',
        title: text('DASHBOARD.OPEN_ORDERS'), description: '', productIds: [], labels: []
      },
      {
        id: 'standard-stock', type: 'table', datasetKey: 'productStocks',
        title: text('PRODUCTS.STOCK_QUANTITY'), description: '', productIds: [],
        labels: [label('productName', 'COMMON.NAME'), label('baseUnit', 'PRODUCTS.UNIT'),
          label('availableQuantity', 'COMMON.QUANTITY'), label('reorderLevel', 'DASHBOARD.REORDER_LEVEL')]
      },
      {
        id: 'standard-usage', type: 'table', datasetKey: 'dailyProductUsage',
        title: text('DASHBOARD.RECORDED_USAGE'), description: '', productIds: [],
        labels: [label('date', 'COMMON.DATE'), label('productName', 'COMMON.NAME'),
          label('baseUnit', 'PRODUCTS.UNIT'), label('issuedQuantity', 'COMMON.QUANTITY')]
      }
    ];
    const facts = this.facts();
    if (facts?.productStocks.length && facts.productStocks.every(row => row.baseUnit.trim()) &&
      new Set(facts.productStocks.map(row => row.baseUnit.trim().toUpperCase())).size === 1) {
      widgets.splice(2, 0, {
        id: 'standard-stock-chart', type: 'bar_chart', datasetKey: 'productStocks',
        title: text('DASHBOARD.STOCK_OVERVIEW'), description: text('DASHBOARD.STOCK_DESCRIPTION'), productIds: [],
        labels: [label('productName', 'COMMON.NAME'), label('availableQuantity', 'COMMON.QUANTITY')]
      });
      if (facts.productStocks.length >= 2 && facts.productStocks.length <= 8 &&
        facts.productStocks.every(row => row.availableQuantity >= 0) &&
        facts.productStocks.some(row => row.availableQuantity > 0)) {
        widgets.splice(3, 0, {
          id: 'standard-stock-share', type: 'doughnut_chart', datasetKey: 'productStocks',
          title: text('DASHBOARD.STOCK_SHARE'), description: text('DASHBOARD.STOCK_SHARE_DESCRIPTION'), productIds: [],
          labels: [label('productName', 'COMMON.NAME'), label('availableQuantity', 'COMMON.QUANTITY')]
        });
      }
    }
    const usageGroups = new Map<string, Set<string>>();
    for (const row of facts?.dailyProductUsage ?? []) {
      const dates = usageGroups.get(row.productId) ?? new Set<string>();
      dates.add(row.date);
      usageGroups.set(row.productId, dates);
    }
    const eligibleIds = [...usageGroups].filter(([, dates]) => dates.size >= 2).map(([id]) => id);
    const selectedUsage = facts?.dailyProductUsage.filter(row => eligibleIds.includes(row.productId)) ?? [];
    if (selectedUsage.length && selectedUsage.every(row => row.baseUnit.trim()) &&
      new Set(selectedUsage.map(row => row.baseUnit.trim().toUpperCase())).size === 1) {
      widgets.splice(3, 0, {
        id: 'standard-usage-chart', type: 'area_chart', datasetKey: 'dailyProductUsage',
        title: text('DASHBOARD.USAGE_OVERVIEW'), description: text('DASHBOARD.USAGE_DESCRIPTION'), productIds: eligibleIds,
        labels: [label('date', 'COMMON.DATE'), label('issuedQuantity', 'COMMON.QUANTITY')]
      });
    }
    return widgets;
  });

  readonly widgets = computed(() => this.dashboard()?.definition.widgets ?? this.standardWidgets());
  readonly metricWidgets = computed(() => this.widgets().filter(widget => widget.type === 'metric_card'));
  readonly detailWidgets = computed(() => this.widgets().filter(widget => widget.type !== 'metric_card'));

  constructor() {
    interval(1000).pipe(takeUntilDestroyed()).subscribe(() => this.clock.set(Date.now()));

    effect(onCleanup => {
      const scope = this.scope();
      const language = this.translationService.currentLanguage();
      const range = this.range();
      this.refreshVersion();

      // Only scope, applied dates, language and explicit refresh trigger a request.
      const subscription = untracked(() => {
        this.dashboard.set(null);
        this.facts.set(null);
        this.hasError.set(false);
        this.errorStatus.set(null);
        if (!scope) {
          this.loading.set(false);
          return null;
        }
        this.loading.set(true);

        return this.dashboardService.generate({ ...range, language }).pipe(
          map(response => {
            if (!response.success || !response.data || response.data.definition.language !== language) {
              throw new Error('Unusable dashboard response.');
            }
            return { dashboard: response.data, facts: response.data.facts, status: null } as DashboardLoad;
          }),
          catchError((error: unknown) => {
            const status = error instanceof HttpErrorResponse ? error.status : null;
            const suppliedWait = error instanceof HttpErrorResponse ? Number(error.error?.data?.retryAfterSeconds) : NaN;
            const wait = status === 429 ? (Number.isFinite(suppliedWait) && suppliedWait > 0 ? suppliedWait : 60) : 0;
            if (wait > 0) {
              this.clock.set(Date.now());
              this.retryAt.set(Date.now() + wait * 1000);
            }
            const failed: DashboardLoad = { dashboard: null, facts: null, status };
            if (status === 401 || status === 403 || status === 409) return of(failed);

            return this.dashboardService.getFacts(range.startDate, range.endDate).pipe(
              map(response => ({ ...failed, facts: response.success ? response.data : null })),
              catchError(() => of(failed))
            );
          })
        ).subscribe(result => {
          this.dashboard.set(result.dashboard);
          this.facts.set(result.facts);
          this.errorStatus.set(result.status);
          this.hasError.set(result.dashboard === null);
          this.loading.set(false);
          if (result.dashboard) this.retryAt.set(0);
        });
      });

      onCleanup(() => subscription?.unsubscribe());
    });
  }

  private applyDates(): boolean {
    const startDate = this.draftStart;
    const endDate = this.draftEnd;
    if (!(startDate instanceof Date) || !(endDate instanceof Date) ||
      !Number.isFinite(startDate.getTime()) || !Number.isFinite(endDate.getTime())) {
      this.invalidRange.set(true);
      return false;
    }
    const start = Date.UTC(startDate.getFullYear(), startDate.getMonth(), startDate.getDate());
    const end = Date.UTC(endDate.getFullYear(), endDate.getMonth(), endDate.getDate());
    const startText = this.formatDate(startDate);
    const endText = this.formatDate(endDate);
    const valid = end >= start && (end - start) / 86400000 < 90 && endText <= this.today;
    this.invalidRange.set(!valid);
    if (!valid) return false;
    const current = this.range();
    if (current.startDate !== startText || current.endDate !== endText) {
      this.range.set({ startDate: startText, endDate: endText });
    }
    return true;
  }

  refresh(): void {
    if (this.loading() || this.retrySeconds() > 0) return;
    const previousRange = this.range();
    if (!this.applyDates()) return;
    if (this.range() === previousRange) this.refreshVersion.update(version => version + 1);
  }

  private formatDate(date: Date): string {
    return `${date.getFullYear()}-${String(date.getMonth() + 1).padStart(2, '0')}-${String(date.getDate()).padStart(2, '0')}`;
  }
}
