import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { RootModel } from '../../models/root.model';
import { WarehouseModel } from '../../models/warehouse.model';

@Injectable({
  providedIn: 'root',
})
export class WarehouseService {
  constructor(private http: HttpClient) {}

  get(): Observable<RootModel> {
    const url = `${environment.main_url}/master/warehouses`;
    return this.http.get<RootModel>(url);
  }

  getById(id: number): Observable<RootModel> {
    const url = `${environment.main_url}/master/warehouses/${id}`;
    return this.http.get<RootModel>(url);
  }

  create(model: WarehouseModel): Observable<RootModel> {
    const url = `${environment.main_url}/master/warehouses`;
    return this.http.post<RootModel>(url, JSON.stringify(model));
  }

  update(model: WarehouseModel): Observable<RootModel> {
    const url = `${environment.main_url}/master/warehouses/${model.id}`;
    return this.http.put<RootModel>(url, JSON.stringify(model));
  }

  delete(id: number): Observable<RootModel> {
    const url = `${environment.main_url}/master/warehouses/${id}`;
    return this.http.delete<RootModel>(url);
  }
}
