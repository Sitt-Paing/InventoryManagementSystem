import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { RootModel } from '../models/root.model';
import { environment } from '../../../environments/environment';
import { Observable } from 'rxjs';
import { WarehouseLocationModel } from '../models/warehouse-location.model';

@Injectable({
  providedIn: 'root',
})
export class WarehouseLocationService {
  constructor(private http: HttpClient) {}

  get(warehouseId?: number): Observable<RootModel> {
    let params = new HttpParams();
    if (warehouseId && warehouseId > 0) {
      params = params.set('warehouseId', warehouseId.toString());
    }
    const url = `${environment.main_url}/warehouse-locations`;
    return this.http.get<RootModel>(url, { params });
  }

  getById(id: number): Observable<RootModel> {
    const url = `${environment.main_url}/warehouse-locations/${id}`;
    return this.http.get<RootModel>(url);
  }

  create(model: WarehouseLocationModel): Observable<RootModel> {
    const url = `${environment.main_url}/warehouse-locations`;
    return this.http.post<RootModel>(url, JSON.stringify(model));
  }

  update(model: WarehouseLocationModel): Observable<RootModel> {
    const url = `${environment.main_url}/warehouse-locations/${model.id}`;
    return this.http.put<RootModel>(url, JSON.stringify(model));
  }

  delete(id: number): Observable<RootModel> {
    const url = `${environment.main_url}/warehouse-locations/${id}`;
    return this.http.delete<RootModel>(url);
  }
}
