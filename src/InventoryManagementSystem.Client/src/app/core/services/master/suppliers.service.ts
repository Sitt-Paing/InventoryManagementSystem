import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { RootModel } from '../../models/root.model';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { SuppliersModel } from '../../models/suppliers.model';


@Injectable({
  providedIn: 'root',
})
export class SuppliersService {
  constructor(private http: HttpClient) {}

  get(): Observable<RootModel>{
    const url = `${environment.main_url}/master/suppliers`;
    return this.http.get<RootModel>(url);
  }

  getById(id: number): Observable<RootModel> {
    const url = `${environment.main_url}/master/suppliers/${id}`;
    return this.http.get<RootModel>(url);
  }

  create(model: SuppliersModel): Observable<RootModel> {
    const url = `${environment.main_url}/master/suppliers`;
    return this.http.post<RootModel>(url, JSON.stringify(model));
  }

  update(model: SuppliersModel): Observable<RootModel> {
    const url = `${environment.main_url}/master/suppliers/${model.id}`;
    return this.http.put<RootModel>(url, JSON.stringify(model));
  }

  delete(id: number): Observable<RootModel> {
    const url = `${environment.main_url}/master/suppliers/${id}`;
    return this.http.delete<RootModel>(url);
  }
}
