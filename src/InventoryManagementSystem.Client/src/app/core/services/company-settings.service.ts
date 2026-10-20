import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { RootModel } from '../models/root.model';
import { environment } from '../../../environments/environment';

@Injectable({
  providedIn: 'root',
})
export class CompanySettingsService {
  constructor(private http: HttpClient) {}

  get(): Observable<RootModel> {
    const url = `${environment.main_url}/Auth/companies`;
    return this.http.get<RootModel>(url);
  }
}
