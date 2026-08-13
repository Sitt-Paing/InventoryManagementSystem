import { HttpClient, HttpHeaders } from '@angular/common/http';
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

  get(): Observable<RootModel> {
    const url = `${environment.main_url}/products`;
    return this.http.get<RootModel>(url);
  }

  getById(id: string | number): Observable<RootModel> {
    const url = `${environment.main_url}/products/${id}`;
    return this.http.get<RootModel>(url);
  }

  create(model: ProductModel): Observable<RootModel> {
    const url = `${environment.main_url}/products`;
    return this.http.post<RootModel>(url, JSON.stringify(model));
  }

  update(model: ProductModel): Observable<RootModel> {
    const url = `${environment.main_url}/products/${model.productId}`;
    return this.http.put<RootModel>(url, JSON.stringify(model));
  }

  delete(id: string | number): Observable<RootModel> {
    const url = `${environment.main_url}/products/${id}`;
    return this.http.delete<RootModel>(url);
  }

  // Legacy compat: used by ProductsComponent save
  save(model: Partial<ProductModel>): Observable<RootModel> {
    const isEdit = model.productId && model.productId !== 0 && model.productId !== '0';
    return isEdit ? this.update(model as ProductModel) : this.create(model as ProductModel);
  }
}
