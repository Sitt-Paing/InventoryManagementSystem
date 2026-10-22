import { Component, computed, effect, inject, signal, untracked } from '@angular/core';
import { AiDashboardData } from '../../core/models/ai-dashboard.model';
import { LanguageCode } from '../../core/i18n/languages';
import { TranslatePipe } from '../../core/pipes/translate.pipe';
import { AiDashboardService } from '../../core/services/ai-dashboard.service';
import { TranslationService } from '../../core/services/translation.service';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [TranslatePipe],
  templateUrl: './dashboard.html',
  styleUrl: './dashboard.scss'
})
export class DashboardComponent {
  readonly translation = inject(TranslationService);
  private readonly api = inject(AiDashboardService);
  readonly dashboard = signal<AiDashboardData | null>(null);
  readonly loading = signal(false);
  readonly errorKey = signal('');
  private readonly retryVersion = signal(0);
  // Component-lifetime cache, only for the immutable demo snapshot.
  private readonly cache = new Map<LanguageCode, AiDashboardData>();

  private readonly locale = computed(() =>
    this.translation.currentLanguage() === 'mm' ? 'my-MM' : 'en-US'
  );
  private readonly numberFormatter = computed(() =>
    new Intl.NumberFormat(this.locale(), { maximumFractionDigits: 1 })
  );
  private readonly dateFormatter = computed(() =>
    new Intl.DateTimeFormat(this.locale(), { day: 'numeric', month: 'short', year: 'numeric' })
  );
  readonly rows = computed(() => {
    const data = this.dashboard();
    if (!data) return [];
    const copy = new Map(data.content.products.map(product => [product.id, product]));
    return data.facts.products.map(product => ({ ...product, text: copy.get(product.id)! }));
  });
  readonly riskCount = computed(() => this.rows().filter(product => product.risk === 'RISK').length);

  constructor() {
    effect(onCleanup => {
      const language = this.translation.currentLanguage();
      this.retryVersion();
      // Track only language and explicit retry, not signals changed by the request.
      untracked(() => {
        this.dashboard.set(null);
        this.errorKey.set('');
        const cached = this.cache.get(language);
        if (cached) {
          this.dashboard.set(cached);
          this.loading.set(false);
          return;
        }
        this.loading.set(true);
        const request = this.api.generate(language).subscribe({
          next: response => {
            // An old-language response must never replace the selected language.
            if (this.translation.currentLanguage() !== language) return;
            this.loading.set(false);
            if (!response.success || !response.data || response.data.language !== language) {
              this.errorKey.set('DASHBOARD.ERROR');
              return;
            }
            this.cache.set(language, response.data);
            this.dashboard.set(response.data);
          },
          error: error => {
            if (this.translation.currentLanguage() !== language) return;
            this.loading.set(false);
            this.errorKey.set(error.status === 429 ? 'DASHBOARD.RATE_LIMIT' : 'DASHBOARD.ERROR');
          }
        });
        onCleanup(() => request.unsubscribe());
      });
    });
  }

  retry(): void {
    this.retryVersion.update(value => value + 1);
  }

  formatNumber(value: number): string {
    return this.numberFormatter().format(value);
  }

  formatDate(value: string): string {
    // Date-only API values are local calendar dates, not UTC timestamps.
    const [year, month, day] = value.split('-').map(Number);
    return this.dateFormatter().format(new Date(year, month - 1, day));
  }
}
