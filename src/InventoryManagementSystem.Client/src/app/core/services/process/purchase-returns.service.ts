import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { CreatePurchaseReturnRequest, PurchaseReturnFilter, PurchaseReturnModel, ReturnableItemsFilter, ReturnableReceiptItemModel, ReturnPage, ReturnResponse } from '../../models/process/purchase-return.model';

@Injectable({
  providedIn: 'root',
})
export class PurchaseReturnsService {
  constructor(private http: HttpClient) {}

  getPaged(filter: PurchaseReturnFilter): Observable<ReturnResponse<ReturnPage<PurchaseReturnModel>>> {
    const url = `${environment.main_url}/process/purchase-returns`;
    return this.http.get<ReturnResponse<ReturnPage<PurchaseReturnModel>>>(url, {
      params: this.params(filter)
    });
  }

  getById(id: string): Observable<ReturnResponse<PurchaseReturnModel>> {
    const url = `${environment.main_url}/process/purchase-returns/${id}`;
    return this.http.get<ReturnResponse<PurchaseReturnModel>>(url);
  }

  getReturnableItems(filter: ReturnableItemsFilter): Observable<ReturnResponse<ReturnPage<ReturnableReceiptItemModel>>> {
    const url = `${environment.main_url}/process/purchase-returns/returnable-items`;
    return this.http.get<ReturnResponse<ReturnPage<ReturnableReceiptItemModel>>>(url, {
      params: this.params(filter)
    });
  }

  create(request: CreatePurchaseReturnRequest): Observable<ReturnResponse<PurchaseReturnModel>> {
    const url = `${environment.main_url}/process/purchase-returns`;
    return this.http.post<ReturnResponse<PurchaseReturnModel>>(url, request);
  }

  post(id: string): Observable<ReturnResponse<PurchaseReturnModel>> {
    const url = `${environment.main_url}/process/purchase-returns/${id}/post`;
    return this.http.post<ReturnResponse<PurchaseReturnModel>>(url, {});
  }

  private params(filter: object): HttpParams {
    let params = new HttpParams();
    for (const [key, value] of Object.entries(filter)) {
      if (value !== undefined && value !== null && value !== '')
        params = params.set(key, String(value));
    }
    return params;
  }
}
