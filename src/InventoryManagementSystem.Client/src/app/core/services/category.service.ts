import { HttpClient, HttpHeaders } from "@angular/common/http";
import { Injectable } from "@angular/core";
import { Observable } from "rxjs";
import { environment } from "../../../environments/environment";
import { RootModel } from "../models/root.model";
import { CategoryModel } from "../models/category.model";

@Injectable({
  providedIn: 'root'
})
export class CategoryService {
  constructor(private http: HttpClient) { }

  get(): Observable<RootModel> {
    let url: string = `${environment.main_url}/categories`;
    return this.http.get<RootModel>(url);
  }

  getById(id: number): Observable<RootModel> {
    let url: string = `${environment.main_url}/categories/${id}`;
    return this.http.get<RootModel>(url);
  }

  create(provider: CategoryModel): Observable<RootModel> {
    let url: string = `${environment.main_url}/categories`;
    return this.http.post<RootModel>(url, JSON.stringify(provider));
  }

  update(provider: CategoryModel): Observable<RootModel> {
    let url: string = `${environment.main_url}/categories/${provider.id}`;
    return this.http.put<RootModel>(url, JSON.stringify(provider));
  }

  delete(id: number): Observable<RootModel> {
    let url: string = `${environment.main_url}/categories/${id}`;
    return this.http.delete<RootModel>(url);
  }
}