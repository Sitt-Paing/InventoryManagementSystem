import { HttpClient, HttpHeaders } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { RootModel } from '../models/root.model';
import { StockTransactionModel } from '../models/stock-transaction.model';

const jsonHeaders = new HttpHeaders({ 'Content-Type': 'application/json' });

@Injectable({
  providedIn: 'root'
})
export class StockTransactionService {
  private http = inject(HttpClient);

  get(): Observable<RootModel> {
    const url = `${environment.main_url}/StockTransactions`;
    return this.http.get<RootModel>(url);
  }

  getById(id: number | string): Observable<RootModel> {
    const url = `${environment.main_url}/StockTransactions/${id}`;
    return this.http.get<RootModel>(url);
  }

  create(model: Partial<StockTransactionModel>): Observable<RootModel> {
    const url = `${environment.main_url}/StockTransactions`;
    return this.http.post<RootModel>(url, JSON.stringify(model), { headers: jsonHeaders });
  }

  delete(id: number | string): Observable<RootModel> {
    const url = `${environment.main_url}/StockTransactions/${id}`;
    return this.http.delete<RootModel>(url);
  }

  // Legacy compat: used by StockTransactionsComponent and Dashboard
  getByCB(): Observable<{ data: StockTransactionModel[] }> {
    return new Observable(observer => {
      this.get().subscribe({
        next: res => observer.next({ data: (res.data || []) as StockTransactionModel[] }),
        error: err => observer.next({ data: [] }),
        complete: () => observer.complete()
      });
    });
  }

  // Legacy compat: used by StockTransactionsComponent save
  save(model: Partial<StockTransactionModel>): Observable<RootModel> {
    return this.create(model);
  }
}
