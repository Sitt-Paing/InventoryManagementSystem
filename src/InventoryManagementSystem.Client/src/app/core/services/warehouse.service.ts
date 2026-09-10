import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { RootModel } from '../models/root.model';
import { environment } from '../../../environments/environment';
import { Observable } from 'rxjs';
import { WarehouseModel } from '../models/warehouse.model';

@Injectable({
  providedIn: 'root',
})
export class WarehouseService {
  constructor(private http: HttpClient) {}

  get(): Observable<RootModel> {
    const url = `${environment.main_url}/warehouses`;
    return this.http.get<RootModel>(url);
  }

  getById(id: number): Observable<RootModel> {
    const url = `${environment.main_url}/warehouses/${id}`;
    return this.http.get<RootModel>(url);
  }

  create(model: WarehouseModel): Observable<RootModel> {
    const url = `${environment.main_url}/warehouses`;
    return this.http.post<RootModel>(url, JSON.stringify(model));
  }

  update(model: WarehouseModel): Observable<RootModel> {
    const url = `${environment.main_url}/warehouses/${model.id}`;
    return this.http.put<RootModel>(url, JSON.stringify(model));
  }

  delete(id: number): Observable<RootModel> {
    const url = `${environment.main_url}/warehouses/${id}`;
    return this.http.delete<RootModel>(url);
  }
}
