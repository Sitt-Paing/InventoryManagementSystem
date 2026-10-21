import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { RootModel } from '../../models/root.model';
import { CreateSupplierAccountModel } from '../../models/usermanage/supplier-account.model';

@Injectable({ providedIn: 'root' })
export class SupplierAccountsService {
  constructor(private http: HttpClient) {}

  get(supplierId: number): Observable<RootModel> {
    const url = `${environment.main_url}/usermanage/supplier-accounts`;
    return this.http.get<RootModel>(url, { params: { supplierId } });
  }

  delete(userId: string): Observable<RootModel> {
    const url = `${environment.main_url}/usermanage/supplier-accounts/${encodeURIComponent(userId)}`;
    return this.http.delete<RootModel>(url);
  }

  create(model: CreateSupplierAccountModel): Observable<RootModel> {
    const url = `${environment.main_url}/usermanage/supplier-accounts`;
    return this.http.post<RootModel>(url, model);
  }
}
