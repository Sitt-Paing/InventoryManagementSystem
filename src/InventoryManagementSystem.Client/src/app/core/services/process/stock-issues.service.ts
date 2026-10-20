import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { StockIssueFilter, StockIssueModel, StockIssuePage, StockIssueRequest, StockIssueResponse, UpdateStockIssueRequest } from '../../models/process/stock-issue.model';

@Injectable({
  providedIn: 'root',
})
export class StockIssuesService {
  constructor(private http: HttpClient) {}

  get(filter: StockIssueFilter): Observable<StockIssueResponse<StockIssuePage>> {
    const url = `${environment.main_url}/process/stock-issues`;
    let params = new HttpParams();
    for (const [key, value] of Object.entries(filter)) {
      if (value !== undefined && value !== null && value !== '') {
        params = params.set(key, String(value));
      }
    }
    return this.http.get<StockIssueResponse<StockIssuePage>>(url, { params });
  }

  getById(id: string): Observable<StockIssueResponse<StockIssueModel>> {
    const url = `${environment.main_url}/process/stock-issues/${id}`;
    return this.http.get<StockIssueResponse<StockIssueModel>>(url);
  }

  create(model: StockIssueRequest): Observable<StockIssueResponse<StockIssueModel>> {
    const url = `${environment.main_url}/process/stock-issues`;
    return this.http.post<StockIssueResponse<StockIssueModel>>(url, model);
  }

  update(model: UpdateStockIssueRequest): Observable<StockIssueResponse<StockIssueModel>> {
    const url = `${environment.main_url}/process/stock-issues/${model.id}`;
    return this.http.put<StockIssueResponse<StockIssueModel>>(url, model);
  }

  delete(id: string): Observable<StockIssueResponse<null>> {
    const url = `${environment.main_url}/process/stock-issues/${id}`;
    return this.http.delete<StockIssueResponse<null>>(url);
  }

  post(id: string): Observable<StockIssueResponse<StockIssueModel>> {
    const url = `${environment.main_url}/process/stock-issues/${id}/post`;
    return this.http.post<StockIssueResponse<StockIssueModel>>(url, {});
  }
}
