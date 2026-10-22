import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { environment } from '../../../environments/environment';
import { LanguageCode } from '../i18n/languages';
import { AiDashboardResponse } from '../models/ai-dashboard.model';

@Injectable({ providedIn: 'root' })
export class AiDashboardService {
  private readonly http = inject(HttpClient);

  generate(language: LanguageCode) {
    return this.http.post<AiDashboardResponse>(
      `${environment.main_url}/ai-demo/dashboard`, {}, { params: { language } });
  }
}
