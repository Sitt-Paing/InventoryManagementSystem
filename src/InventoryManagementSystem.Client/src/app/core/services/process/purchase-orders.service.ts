import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { RootModel } from '../../models/root.model';
import { PurchaseOrderFilterModel, PurchaseOrderModel } from '../../models/process/purchase-order.model';

@Injectable({
  providedIn: 'root',
})
export class PurchaseOrdersService {
  constructor(private http: HttpClient) {}

  getPaged(filter?: PurchaseOrderFilterModel): Observable<RootModel> {
    let params = new HttpParams();
    if (filter?.q && filter.q.trim()) {
      params = params.set('q', filter.q.trim());
    }
    if (filter?.startDate) {
      params = params.set('startDate', filter.startDate);
    }
    if (filter?.endDate) {
      params = params.set('endDate', filter.endDate);
    }
    if (filter?.supplierId && filter.supplierId > 0) {
      params = params.set('supplierId', filter.supplierId.toString());
    }
    if (filter?.warehouseId && filter.warehouseId > 0) {
      params = params.set('warehouseId', filter.warehouseId.toString());
    }
    if (filter?.status !== undefined && filter?.status !== null) {
      params = params.set('status', filter.status.toString());
    }
    if (filter?.sortField) {
      params = params.set('sortField', filter.sortField);
    }
    if (filter?.order !== undefined && filter?.order !== null) {
      params = params.set('order', filter.order.toString());
    }
    if (filter?.pageNumber && filter.pageNumber > 0) {
      params = params.set('pageNumber', filter.pageNumber.toString());
    }
    if (filter?.pageSize && filter.pageSize > 0) {
      params = params.set('pageSize', filter.pageSize.toString());
    }

    const url = `${environment.main_url}/process/purchase-orders`;
    return this.http.get<RootModel>(url, { params });
  }

  getById(id: string): Observable<RootModel> {
    const url = `${environment.main_url}/process/purchase-orders/${id}`;
    return this.http.get<RootModel>(url);
  }

  create(model: PurchaseOrderModel): Observable<RootModel> {
    const url = `${environment.main_url}/process/purchase-orders`;
    return this.http.post<RootModel>(url, JSON.stringify(model));
  }

  update(model: PurchaseOrderModel): Observable<RootModel> {
    const url = `${environment.main_url}/process/purchase-orders/${model.id}`;
    return this.http.put<RootModel>(url, JSON.stringify(model));
  }

  delete(id: string): Observable<RootModel> {
    const url = `${environment.main_url}/process/purchase-orders/${id}`;
    return this.http.delete<RootModel>(url);
  }

  export(filter?: PurchaseOrderFilterModel): Observable<Blob> {
    let params = new HttpParams();
    if (filter?.q && filter.q.trim()) {
      params = params.set('q', filter.q.trim());
    }
    if (filter?.startDate) {
      params = params.set('startDate', filter.startDate);
    }
    if (filter?.endDate) {
      params = params.set('endDate', filter.endDate);
    }
    if (filter?.supplierId && filter.supplierId > 0) {
      params = params.set('supplierId', filter.supplierId.toString());
    }
    if (filter?.warehouseId && filter.warehouseId > 0) {
      params = params.set('warehouseId', filter.warehouseId.toString());
    }
    if (filter?.status !== undefined && filter?.status !== null) {
      params = params.set('status', filter.status.toString());
    }

    const url = `${environment.main_url}/process/purchase-orders/export`;
    return this.http.get(url, { params, responseType: 'blob' });
  }
}
