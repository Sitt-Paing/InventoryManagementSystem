import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { ProductUomConversionModel } from '../../models/product-uom-conversion.model';
import { RootModel } from '../../models/root.model';


@Injectable({
  providedIn: 'root'
})
export class ProductUomConversionService {
  constructor(private http: HttpClient) {}

  getByProductId(productId: string): Observable<RootModel> {
    const url = `${environment.main_url}/master/product-uom-conversions/product/${productId}`;
    return this.http.get<RootModel>(url);
  }

  create(payload: Partial<ProductUomConversionModel>): Observable<RootModel> {
    const url = `${environment.main_url}/master/product-uom-conversions`;
    return this.http.post<RootModel>(url, payload);
  }

  update(payload: Partial<ProductUomConversionModel>): Observable<RootModel> {
    const url = `${environment.main_url}/master/product-uom-conversions/${payload.id}`;
    return this.http.put<RootModel>(url, payload);
  }

  delete(id: number): Observable<RootModel> {
    const url = `${environment.main_url}/master/product-uom-conversions/${id}`;
    return this.http.delete<RootModel>(url);
  }
}
