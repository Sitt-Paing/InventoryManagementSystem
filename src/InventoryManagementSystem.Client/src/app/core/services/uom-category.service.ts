import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { RootModel } from '../models/root.model';
import { UomCategoryModel } from '../models/uom-category.model';

@Injectable({
  providedIn: 'root'
})
export class UomCategoryService {
  constructor(private http: HttpClient) {}

  get(): Observable<RootModel> {
    const url = `${environment.main_url}/uom-categories`;
    return this.http.get<RootModel>(url);
  }

  getById(id: number): Observable<RootModel> {
    const url = `${environment.main_url}/uom-categories/${id}`;
    return this.http.get<RootModel>(url);
  }

  create(payload: Partial<UomCategoryModel>): Observable<RootModel> {
    const url = `${environment.main_url}/uom-categories`;
    return this.http.post<RootModel>(url, payload);
  }

  update(payload: Partial<UomCategoryModel>): Observable<RootModel> {
    const url = `${environment.main_url}/uom-categories/${payload.id}`;
    return this.http.put<RootModel>(url, payload);
  }

  delete(id: number): Observable<RootModel> {
    const url = `${environment.main_url}/uom-categories/${id}`;
    return this.http.delete<RootModel>(url);
  }
}
