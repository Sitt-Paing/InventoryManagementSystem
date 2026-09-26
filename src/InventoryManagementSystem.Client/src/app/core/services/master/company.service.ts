import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { RootModel } from '../../models/root.model';
import { CompanyModel, RegisterCompanyPayload } from '../../models/master/company.model';

@Injectable({
  providedIn: 'root',
})
export class CompanyService {
  constructor(private http: HttpClient) {}

  get(): Observable<RootModel> {
    const url = `${environment.main_url}/master/companies`;
    return this.http.get<RootModel>(url);
  }

  registerWithAdmin(payload: RegisterCompanyPayload): Observable<RootModel> {
    const url = `${environment.main_url}/master/companies/register-with-admin`;
    return this.http.post<RootModel>(url, payload);
  }

  toggleStatus(id: number): Observable<RootModel> {
    const url = `${environment.main_url}/master/companies/${id}/toggle-status`;
    return this.http.put<RootModel>(url, {});
  }
}
