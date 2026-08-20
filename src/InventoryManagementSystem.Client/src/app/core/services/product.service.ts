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
    const id = model.id || model.productId;
    const url = `${environment.main_url}/products/${id}`;
    return this.http.put<RootModel>(url, model);
  }

  delete(id: string | number): Observable<RootModel> {
    const url = `${environment.main_url}/products/${id}`;
    return this.http.delete<RootModel>(url);
  }

  save(model: Partial<ProductModel>): Observable<RootModel> {
    const id = model.id || model.productId;
    const isEdit = id && id !== '0';
    return isEdit ? this.update(model) : this.create(model);
  }
}
