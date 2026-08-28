import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { RootModel } from '../models/root.model';
import { ProductModel } from '../models/product.model';

@Injectable({
  providedIn: 'root'
})
export class ProductService {
  private http = inject(HttpClient);

  get(categoryId?: number | null): Observable<RootModel> {
    let url = `${environment.main_url}/products`;
    if (categoryId != null && categoryId > 0) {
      url += `?categoryId=${categoryId}`;
    }
    return this.http.get<RootModel>(url);
  }

  getById(id: string | number): Observable<RootModel> {
    const url = `${environment.main_url}/products/${id}`;
    return this.http.get<RootModel>(url);
  }

  create(model: any): Observable<RootModel> {
    const url = `${environment.main_url}/products`;
    return this.http.post<RootModel>(url, model);
  }

  update(model: any): Observable<RootModel> {
    const id = model.id;
    const url = `${environment.main_url}/products/${id}`;
    return this.http.put<RootModel>(url, model);
  }

  delete(id: string | number): Observable<RootModel> {
    const url = `${environment.main_url}/products/${id}`;
    return this.http.delete<RootModel>(url);
  }

  save(model: Partial<ProductModel>): Observable<RootModel> {
    const id = model.id;
    const isEdit = id && id !== '0';
    return isEdit ? this.update(model) : this.create(model);
  }

  exportExcel(categoryId?: number | null, fontName: string = 'Pyidaungsu'): Observable<Blob> {
    let url = `${environment.main_url}/products/export?format=excel&fontName=${encodeURIComponent(fontName)}`;
    if (categoryId != null && categoryId > 0) {
      url += `&categoryId=${categoryId}`;
    }
    return this.http.get(url, { responseType: 'blob' });
  }

  exportCsv(categoryId?: number | null): Observable<Blob> {
    let url = `${environment.main_url}/products/export?format=csv`;
    if (categoryId != null && categoryId > 0) {
      url += `&categoryId=${categoryId}`;
    }
    return this.http.get(url, { responseType: 'blob' });
  }

  excel(
    categoryId?: number | null,
    q?: string,
    sortField?: string,
    order?: number,
    columns: import('../models/export-column.model').ExportColumnModel[] = []
  ): Observable<Blob> {
    let url: string = `${environment.main_url}/products/excel?`;
    const params: string[] = [];
    if (categoryId != null && categoryId > 0) params.push(`categoryId=${categoryId}`);
    if (q) params.push(`q=${encodeURIComponent(q)}`);
    if (sortField) params.push(`sortField=${encodeURIComponent(sortField)}`);
    if (order != null) params.push(`order=${order}`);
    url += params.join('&');

    return this.http.post(url, columns, { responseType: 'blob' });
  }
}
