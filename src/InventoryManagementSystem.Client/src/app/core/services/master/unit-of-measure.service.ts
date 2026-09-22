import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { RootModel } from '../../models/root.model';
import { UnitOfMeasureModel } from '../../models/master/unit-of-measure.model';

@Injectable({
  providedIn: 'root'
})
export class UnitOfMeasureService {
  constructor(private http: HttpClient) {}

  get(categoryId?: number): Observable<RootModel> {
    let url = `${environment.main_url}/master/unit-of-measures`;
    let params = new HttpParams();
    if (categoryId && categoryId > 0) {
      params = params.set('categoryId', categoryId.toString());
    }
    return this.http.get<RootModel>(url, { params });
  }

  getById(id: number): Observable<RootModel> {
    const url = `${environment.main_url}/master/unit-of-measures/${id}`;
    return this.http.get<RootModel>(url);
  }

  create(payload: Partial<UnitOfMeasureModel>): Observable<RootModel> {
    const url = `${environment.main_url}/master/unit-of-measures`;
    return this.http.post<RootModel>(url, payload);
  }

  update(payload: Partial<UnitOfMeasureModel>): Observable<RootModel> {
    const url = `${environment.main_url}/master/unit-of-measures/${payload.id}`;
    return this.http.put<RootModel>(url, payload);
  }

  delete(id: number): Observable<RootModel> {
    const url = `${environment.main_url}/master/unit-of-measures/${id}`;
    return this.http.delete<RootModel>(url);
  }
}
