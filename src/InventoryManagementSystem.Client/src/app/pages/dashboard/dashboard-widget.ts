import { Component, computed, inject, input, signal } from '@angular/core';
import { DecimalPipe } from '@angular/common';
import {
  DashboardDailyUsage, DashboardFacts, DashboardProductStock, DashboardWidget
} from '../../core/models/process/dashboard.model';
import { TranslatePipe } from '../../core/pipes/translate.pipe';
import { ChartModule } from 'primeng/chart';
import type { ChartData, ChartOptions, Plugin, ScriptableContext } from 'chart.js';
import { SharedService } from '../../core/services/shared.service';
import { TranslationService } from '../../core/services/translation.service';

type DashboardRow = DashboardProductStock | DashboardDailyUsage;

@Component({
  selector: 'app-dashboard-widget',
  standalone: true,
  imports: [DecimalPipe, TranslatePipe, ChartModule],
  templateUrl: './dashboard-widget.html',
  styleUrl: './dashboard-widget.scss'
})
export class DashboardWidgetComponent {
  private readonly theme = inject(SharedService);
  private readonly translation = inject(TranslationService);
  private readonly colors = ['#10b981', '#3b82f6', '#8b5cf6', '#f59e0b', '#ec4899', '#06b6d4', '#f97316', '#6366f1'];
  readonly widget = input.required<DashboardWidget>();
  readonly facts = input.required<DashboardFacts>();
  private readonly productColors = computed(() => {
    const ids = new Set([
      ...this.facts().dailyProductUsage.map(row => row.productId),
      ...this.facts().productStocks.map(row => row.productId)
    ]);
    return new Map([...ids].map((id, index) => [id, this.colors[index % this.colors.length]]));
  });
  readonly page = signal(0);
  readonly pageSize = 10;
  private readonly duplicateNames = computed(() => {
    const seen = new Set<string>();
    const duplicates = new Set<string>();
    for (const product of this.facts().productStocks) {
      if (seen.has(product.productName)) duplicates.add(product.productName);
      seen.add(product.productName);
    }
    return duplicates;
  });

  readonly rows = computed<DashboardRow[]>(() => {
    const widget = this.widget();
    const facts = this.facts();
    const source = widget.datasetKey === 'productStocks' ? facts.productStocks
      : widget.datasetKey === 'dailyProductUsage' ? facts.dailyProductUsage : [];
    const selected = new Set(widget.productIds);
    return source.filter(row => selected.size === 0 || selected.has(row.productId));
  });

  readonly visibleRows = computed(() => this.rows().slice(
    this.page() * this.pageSize, (this.page() + 1) * this.pageSize
  ));
  readonly pageCount = computed(() => Math.max(1, Math.ceil(this.rows().length / this.pageSize)));
  readonly metric = computed(() => {
    switch (this.widget().datasetKey) {
      case 'activeProductCount': return this.facts().activeProductCount;
      case 'openPurchaseOrderCount': return this.facts().openPurchaseOrderCount;
      default: return null;
    }
  });
  readonly unit = computed(() => this.displayUnit(this.rows()[0]));

  displayUnit(row: DashboardRow | undefined): string {
    return row?.baseUnitSymbol?.trim() || '—';
  }
  readonly isCircular = computed(() => this.widget().type === 'pie_chart' || this.widget().type === 'doughnut_chart');
  readonly isTrend = computed(() => this.widget().type === 'line_chart' || this.widget().type === 'area_chart');
  readonly isChart = computed(() => this.widget().type !== 'metric_card' && this.widget().type !== 'table');
  readonly numericRows = computed(() => this.rows().map(row => ({
    row,
    label: 'date' in row ? `${row.date} · ${this.productLabel(row)}` : this.productLabel(row),
    value: 'availableQuantity' in row ? row.availableQuantity : row.issuedQuantity
  })));
  readonly circularSegments = computed(() => {
    const products = new Map<string, { productId: string; label: string; value: number; color: string }>();
    for (const row of this.rows()) {
      const current = products.get(row.productId);
      const value = 'availableQuantity' in row ? row.availableQuantity : row.issuedQuantity;
      products.set(row.productId, {
        productId: row.productId, label: this.productLabel(row),
        value: (current?.value ?? 0) + value, color: this.productColor(row.productId)
      });
    }
    const segments = [...products.values()].sort((a, b) => b.value - a.value);
    const total = segments.reduce((sum, segment) => sum + segment.value, 0);
    return segments.map(segment => ({ ...segment, percent: total > 0 ? segment.value / total * 100 : 0 }));
  });
  readonly circularTotal = computed(() => this.circularSegments().reduce((sum, segment) => sum + segment.value, 0));
  readonly circularData = computed<ChartData<'pie' | 'doughnut'>>(() => ({
    labels: this.circularSegments().map(segment => segment.label),
    datasets: [{
      label: this.quantityLabel(), data: this.circularSegments().map(segment => segment.value),
      backgroundColor: this.circularSegments().map(segment => segment.color),
      borderColor: this.theme.isDarkMode() ? '#0f172a' : '#ffffff', borderWidth: 3,
      hoverOffset: 7, borderRadius: this.widget().type === 'doughnut_chart' ? 5 : 0
    }]
  }));
  readonly circularOptions = computed<ChartOptions<'pie' | 'doughnut'>>(() => {
    const dark = this.theme.isDarkMode();
    const format = new Intl.NumberFormat(this.translation.currentLanguage() === 'mm' ? 'my-MM' : 'en-GB', {
      maximumFractionDigits: 4
    });
    return {
      responsive: true, maintainAspectRatio: false, animation: false,
      cutout: this.widget().type === 'doughnut_chart' ? '76%' : 0,
      layout: { padding: 10 },
      plugins: {
        legend: { display: false },
        tooltip: {
          backgroundColor: dark ? '#1e293b' : '#0f172a', padding: 12, cornerRadius: 8,
          callbacks: { label: item => {
            const segment = this.circularSegments()[item.dataIndex];
            return `${format.format(segment.value)} ${this.unit()} · ${format.format(segment.percent)}%`;
          } }
        }
      }
    };
  });
  readonly icon = computed(() => this.isCircular() ? 'pi pi-chart-pie'
    : this.widget().datasetKey === 'activeProductCount' ? 'pi pi-box'
    : this.widget().datasetKey === 'openPurchaseOrderCount' ? 'pi pi-shopping-bag'
    : this.widget().datasetKey === 'productStocks' ? 'pi pi-chart-bar' : 'pi pi-chart-line');

  readonly barData = computed<ChartData<'bar'>>(() => {
    if (this.widget().datasetKey === 'productStocks') {
      const rows = [...this.numericRows()].sort((a, b) => b.value - a.value);
      return {
        labels: rows.map(point => point.label),
        datasets: [{
          label: this.quantityLabel(), data: rows.map(point => point.value),
          backgroundColor: this.theme.isDarkMode() ? '#34d399bb' : '#10b981cc',
          hoverBackgroundColor: '#059669', borderRadius: 5, borderSkipped: false,
          maxBarThickness: 22, categoryPercentage: .65, barPercentage: .75
        }]
      };
    }
    const rows = this.rows().filter((row): row is DashboardDailyUsage => 'date' in row);
    const dates = [...new Set(rows.map(row => row.date))].sort();
    const groups = new Map<string, DashboardDailyUsage[]>();
    for (const row of rows) {
      const group = groups.get(row.productId) ?? [];
      group.push(row);
      groups.set(row.productId, group);
    }
    return {
      labels: dates,
      datasets: [...groups.values()].map(observations => {
        const quantities = new Map(observations.map(row => [row.date, row.issuedQuantity]));
        return {
          label: this.productLabel(observations[0]), data: dates.map(date => quantities.get(date) ?? null),
          backgroundColor: this.productColor(observations[0].productId), borderRadius: 4,
          borderSkipped: false, maxBarThickness: 28
        };
      })
    };
  });

  readonly chartPlugins: Plugin<'bar' | 'line'>[] = [{
    id: 'inventory-stock-values',
    afterDatasetsDraw: chart => {
      if (chart.options.indexAxis !== 'y') return;
      const context = chart.ctx;
      const formatter = new Intl.NumberFormat(this.translation.currentLanguage() === 'mm' ? 'my-MM' : 'en-GB', {
        maximumFractionDigits: 4
      });
      context.save();
      context.font = '600 11px Inter, system-ui, sans-serif';
      context.fillStyle = this.theme.isDarkMode() ? '#e2e8f0' : '#334155';
      context.textBaseline = 'middle';
      chart.getDatasetMeta(0).data.forEach((bar, index) => {
        const value = chart.data.datasets[0]?.data[index];
        if (typeof value !== 'number') return;
        context.textAlign = value < 0 ? 'right' : 'left';
        context.fillText(formatter.format(value), bar.x + (value < 0 ? -8 : 8), bar.y);
      });
      context.restore();
    }
  }];

  readonly lineData = computed<ChartData<'line'>>(() => {
    const rows = this.rows().filter((row): row is DashboardDailyUsage => 'date' in row);
    const groups = new Map<string, DashboardDailyUsage[]>();
    for (const row of rows) {
      const group = groups.get(row.productId) ?? [];
      group.push(row);
      groups.set(row.productId, group);
    }
    return {
      datasets: [...groups.values()].map(observations => ({
        label: this.productLabel(observations[0]),
        data: [...observations].sort((a, b) => a.date.localeCompare(b.date)).map(row => ({
          x: Date.parse(row.date), y: row.issuedQuantity
        })),
        borderColor: this.productColor(observations[0].productId),
        backgroundColor: (context: ScriptableContext<'line'>) => {
          const color = this.productColor(observations[0].productId);
          const area = context.chart.chartArea;
          if (!area) return color + '12';
          const gradient = context.chart.ctx.createLinearGradient(0, area.top, 0, area.bottom);
          gradient.addColorStop(0, color + '25');
          gradient.addColorStop(1, color + '00');
          return gradient;
        },
        pointBackgroundColor: this.productColor(observations[0].productId),
        pointBorderColor: this.theme.isDarkMode() ? '#0f172a' : '#ffffff',
        pointBorderWidth: 2,
        pointRadius: 3,
        pointHoverRadius: 6,
        pointHitRadius: 12,
        borderWidth: 2,
        tension: 0,
        fill: this.widget().type === 'area_chart'
      }))
    };
  });

  readonly chartOptions = computed<ChartOptions<'bar' | 'line'>>(() => {
    const dark = this.theme.isDarkMode();
    const isLine = this.isTrend();
    const stacked = this.widget().type === 'stacked_bar_chart';
    const stockBar = !isLine && this.widget().datasetKey === 'productStocks';
    const text = dark ? '#94a3b8' : '#64748b';
    const grid = dark ? 'rgba(148,163,184,.08)' : 'rgba(148,163,184,.12)';
    const locale = this.translation.currentLanguage() === 'mm' ? 'my-MM' : 'en-GB';
    const formatNumber = new Intl.NumberFormat(locale, { maximumFractionDigits: 4 });
    const formatDate = (value: number) => new Intl.DateTimeFormat(locale, {
      month: 'short', day: 'numeric', timeZone: 'UTC'
    }).format(new Date(value));
    const observedDates = isLine ? this.rows().filter((row): row is DashboardDailyUsage => 'date' in row)
      .map(row => Date.parse(row.date)).sort((a, b) => a - b) : [];
    const barLabels = !isLine && !stockBar ? this.barData().labels ?? [] : [];
    let stockMinimum = 0;
    let stockMaximum = 0;
    if (stockBar) {
      for (const point of this.numericRows()) {
        stockMinimum = Math.min(stockMinimum, point.value);
        stockMaximum = Math.max(stockMaximum, point.value);
      }
    }
    return {
      responsive: true, maintainAspectRatio: false, animation: false, locale,
      font: { family: "Inter, Pyidaungsu, Padauk, 'Noto Sans Myanmar', system-ui, sans-serif" },
      indexAxis: stockBar ? 'y' : 'x',
      layout: { padding: { top: 8, right: stockBar ? 55 : 8, bottom: 0, left: 0 } },
      interaction: { mode: 'nearest', axis: stockBar ? 'y' : 'x', intersect: false },
      plugins: {
        legend: { display: !stockBar, position: 'top', align: 'end', labels: {
          color: text, usePointStyle: true, boxWidth: 6, boxHeight: 6, padding: 16, font: { size: 11 }
        } },
        tooltip: {
          backgroundColor: dark ? '#1e293b' : '#0f172a',
          titleColor: '#f8fafc', bodyColor: '#e2e8f0', padding: 12, cornerRadius: 8,
          titleFont: { size: 12 }, bodyFont: { size: 11 },
          displayColors: !stockBar,
          callbacks: {
            title: items => isLine && items.length ? formatDate(items[0].parsed.x ?? 0) : items[0]?.label ?? '',
            label: item => `${item.dataset.label}: ${formatNumber.format((stockBar ? item.parsed.x : item.parsed.y) ?? 0)} ${this.unit()}`
          }
        }
      },
      scales: {
        x: isLine ? {
          type: 'linear', grid: { display: false }, border: { display: false },
          min: observedDates[0], max: observedDates[observedDates.length - 1],
          ticks: { color: text, maxTicksLimit: 5, font: { size: 11 }, callback: value => formatDate(Number(value)) }
        } : stockBar ? {
          type: 'linear', beginAtZero: true, grid: { color: grid }, border: { display: false },
          min: stockMinimum * 1.1, max: stockMaximum > 0 ? stockMaximum * 1.1 : (stockMinimum < 0 ? 0 : 1),
          ticks: { color: text, maxTicksLimit: 4, includeBounds: false, font: { size: 11 } }
        } : {
          type: 'category', stacked, grid: { display: false }, border: { display: false },
          ticks: { color: text, maxRotation: 0, autoSkip: true, maxTicksLimit: 6, font: { size: 11 },
            callback: value => formatDate(Date.parse(String(barLabels[Number(value)]))) }
        },
        y: stockBar ? {
          type: 'category', grid: { display: false }, border: { display: false },
          ticks: { color: text, font: { size: 11 }, padding: 10 }
        } : {
          type: 'linear', stacked, beginAtZero: true, border: { display: false },
          grid: { color: grid }, ticks: { color: text, maxTicksLimit: 5, font: { size: 11 }, padding: 8 }
        }
      }
    };
  });

  private productColor(productId: string): string {
    return this.productColors().get(productId) ?? this.colors[0];
  }

  private fieldLabel(field: string): string {
    return this.widget().labels.find(label => label.field === field)?.text ?? '';
  }

  private quantityLabel(): string {
    return this.fieldLabel(this.widget().datasetKey === 'productStocks' ? 'availableQuantity' : 'issuedQuantity');
  }

  productLabel(row: DashboardRow): string {
    const duplicate = this.duplicateNames().has(row.productName);
    return duplicate ? `${row.productName} · ${row.productId.slice(0, 8)}` : row.productName;
  }

  cell(row: DashboardRow, field: string): string | number {
    switch (field) {
      case 'productName': return this.productLabel(row);
      case 'baseUnit': return this.displayUnit(row);
      case 'availableQuantity': return 'availableQuantity' in row ? row.availableQuantity : '';
      case 'reorderLevel': return 'reorderLevel' in row ? row.reorderLevel : '';
      case 'date': return 'date' in row ? row.date : '';
      case 'issuedQuantity': return 'issuedQuantity' in row ? row.issuedQuantity : '';
      default: return '';
    }
  }

  isNumber(value: string | number): value is number {
    return typeof value === 'number';
  }

  rowKey(row: DashboardRow): string {
    return 'date' in row ? `${row.productId}:${row.date}` : row.productId;
  }
}
