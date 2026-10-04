import { HttpClient, HttpHeaders } from "@angular/common/http";
import { Injectable } from "@angular/core";
import { Observable } from "rxjs";
import { environment } from "../../../../environments/environment";
import { CategoryModel } from "../../models/master/category.model";
import { RootModel } from "../../models/root.model";

@Injectable({
  providedIn: 'root'
})
export class CategoryService {
  constructor(private http: HttpClient) {}

  get(): Observable<RootModel> {
    const url = `${environment.main_url}/master/categories`;
    return this.http.get<RootModel>(url);
  }

  getById(id: number): Observable<RootModel> {
    const url = `${environment.main_url}/master/categories/${id}`;
    return this.http.get<RootModel>(url);
  }

  create(model: CategoryModel): Observable<RootModel> {
    const url = `${environment.main_url}/master/categories`;
    return this.http.post<RootModel>(url, JSON.stringify(model));
  }

  update(model: CategoryModel): Observable<RootModel> {
    const url = `${environment.main_url}/master/categories/${model.id}`;
    return this.http.put<RootModel>(url, JSON.stringify(model));
  }

  delete(id: number): Observable<RootModel> {
    const url = `${environment.main_url}/master/categories/${id}`;
    return this.http.delete<RootModel>(url);
  }
}