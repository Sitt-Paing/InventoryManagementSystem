import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';
import {
  DashboardApiResponse,
  DashboardGenerateRequest,
  DashboardFacts,
  DashboardGeneration
} from '../../models/process/dashboard.model';

@Injectable({
  providedIn: 'root',
})
export class DashboardService {
  constructor(private http: HttpClient) {}

  getFacts(startDate: string, endDate: string): Observable<DashboardApiResponse<DashboardFacts>> {
    return this.http.get<DashboardApiResponse<DashboardFacts>>(
      `${environment.main_url}/process/dashboard/facts`,
      { params: new HttpParams().set('startDate', startDate).set('endDate', endDate) }
    );
  }

  generate(
    request: DashboardGenerateRequest
  ): Observable<DashboardApiResponse<DashboardGeneration>> {
    const url = `${environment.main_url}/process/dashboard/generate`;

    const params = new HttpParams()
      .set('startDate', request.startDate)
      .set('endDate', request.endDate)
      .set('language', request.language);

    return this.http.post<DashboardApiResponse<DashboardGeneration>>(
      url,
      null,
      { params }
    );
  }
}
