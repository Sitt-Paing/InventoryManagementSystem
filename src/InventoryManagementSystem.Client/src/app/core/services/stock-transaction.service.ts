import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { RootModel } from '../models/root.model';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { StockTransactionModel } from '../models/stock-transaction.model';

@Injectable({
  providedIn: 'root',
})
export class StockTransactionService {
  constructor(private http: HttpClient) {}

  get(filter?: {
    transactionType?: string | null;
    date?: string | null;
    startDate?: string | null;
    endDate?: string | null;
    productId?: string | null;
    warehouseId?: number | null;
  }): Observable<RootModel> {
    let params = new HttpParams();
    if (filter?.transactionType) {
      params = params.set('transactionType', filter.transactionType);
    }
    if (filter?.date) {
      params = params.set('date', filter.date);
    }
    if (filter?.startDate) {
      params = params.set('startDate', filter.startDate);
    }
    if (filter?.endDate) {
      params = params.set('endDate', filter.endDate);
    }
    if (filter?.productId) {
      params = params.set('productId', filter.productId);
    }
    if (filter?.warehouseId && filter.warehouseId > 0) {
      params = params.set('warehouseId', filter.warehouseId.toString());
    }
    const url = `${environment.main_url}/process/stock-transactions`;
    return this.http.get<RootModel>(url, { params });
  }

  getById(id: number | string): Observable<RootModel> {
    const url = `${environment.main_url}/process/stock-transactions/${id}`;
    return this.http.get<RootModel>(url);
  }

  create(model: StockTransactionModel): Observable<RootModel> {
    const url = `${environment.main_url}/process/stock-transactions`;
    return this.http.post<RootModel>(url, JSON.stringify(model));
  }

  update(model: StockTransactionModel): Observable<RootModel> {
    const url = `${environment.main_url}/process/stock-transactions/${model.id}`;
    return this.http.put<RootModel>(url, JSON.stringify(model));
  }

  delete(id: number | string): Observable<RootModel> {
    const url = `${environment.main_url}/process/stock-transactions/${id}`;
    return this.http.delete<RootModel>(url);
  }

  getWarehouseBalance(productId: string, warehouseId: number): Observable<RootModel> {
    const url = `${environment.main_url}/process/stock-transactions/warehouse-balance?productId=${productId}&warehouseId=${warehouseId}`;
    return this.http.get<RootModel>(url);
  }
}
