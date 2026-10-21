import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { RootModel } from '../../models/root.model';
import { SupplierDocumentFilter, SupplierDocumentType } from '../../models/supplier/supplier-document.model';

@Injectable({ providedIn: 'root' })
export class SupplierDocumentsService {
  constructor(private http: HttpClient) {}

  get(kind: SupplierDocumentType, filter: SupplierDocumentFilter): Observable<RootModel> {
    const url = `${environment.main_url}/supplier/${kind}`;
    let params = new HttpParams()
      .set('pageNumber', filter.pageNumber).set('pageSize', filter.pageSize)
      .set('search', filter.search).set('sortField', filter.sortField).set('order', filter.order);
    if (filter.startDate) params = params.set('startDate', filter.startDate);
    if (filter.endDate) params = params.set('endDate', filter.endDate);
    return this.http.get<RootModel>(url, { params });
  }

  getById(kind: SupplierDocumentType, id: string): Observable<RootModel> {
    const url = `${environment.main_url}/supplier/${kind}/${encodeURIComponent(id)}`;
    return this.http.get<RootModel>(url);
  }

  getDashboard(startDate: string | null, endDate: string | null): Observable<RootModel> {
    const url = `${environment.main_url}/supplier/dashboard`;
    let params = new HttpParams();
    if (startDate) params = params.set('startDate', startDate);
    if (endDate) params = params.set('endDate', endDate);
    return this.http.get<RootModel>(url, { params });
  }
}
