import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { RootModel } from '../../models/root.model';
import { StockBalanceFilterModel } from '../../models/report/stock-balance.model';

@Injectable({
  providedIn: 'root',
})
export class StockBalanceService {
  constructor(private http: HttpClient) {}

  get(filter: StockBalanceFilterModel): Observable<RootModel> {
    const url = `${environment.main_url}/report/stock-balances`;
    let params = new HttpParams()
      .set('search', filter.search)
      .set('showZeroStock', filter.showZeroStock)
      .set('pageNumber', filter.pageNumber)
      .set('pageSize', filter.pageSize)
      .set('sortField', filter.sortField)
      .set('order', filter.order);
    if (filter.startDate) params = params.set('startDate', filter.startDate);
    if (filter.endDate) params = params.set('endDate', filter.endDate);
    if (filter.warehouseId != null) params = params.set('warehouseId', filter.warehouseId);
    if (filter.categoryId != null) params = params.set('categoryId', filter.categoryId);
    return this.http.get<RootModel>(url, { params });
  }
}
