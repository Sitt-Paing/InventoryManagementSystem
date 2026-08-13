import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { tap } from 'rxjs/operators';
import { environment } from '../../../environments/environment';
import { RootModel } from '../models/root.model';
import { AuthResponseModel, LoginModel, RefreshTokenModel } from '../models/auth.model';

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private http = inject(HttpClient);
  private readonly TOKEN_KEY = 'ACCESS_TOKEN';
  private readonly REFRESH_KEY = 'REFRESH_TOKEN';
  private readonly USER_KEY = 'AUTH_USER';

  login(model: LoginModel): Observable<RootModel> {
    const url = `${environment.main_url}/Auth/login`;
    return this.http.post<RootModel>(url, model).pipe(
      tap(res => {
        // res.data is AuthResultDto: { succeeded, accessToken, refreshToken, userName, email, roles, expiry }
        if (res.success && res.data) {
          const dto = res.data as any;
          if (dto.succeeded && dto.accessToken) {
            const userData: AuthResponseModel = {
              userId: dto.userId ?? '',
              userName: dto.userName ?? '',
              email: dto.email ?? '',
              roles: dto.roles ?? [],
              accessToken: dto.accessToken,
              refreshToken: dto.refreshToken ?? '',
              expiresAt: dto.expiry ?? ''
            };
            localStorage.setItem(this.TOKEN_KEY, userData.accessToken);
            localStorage.setItem(this.REFRESH_KEY, userData.refreshToken);
            localStorage.setItem(this.USER_KEY, JSON.stringify(userData));
          }
        }
      })
    );
  }

  refreshToken(): Observable<RootModel> {
    const url = `${environment.main_url}/Auth/refresh-token`;
    const body: RefreshTokenModel = {
      accessToken: this.getAccessToken() ?? '',
      refreshToken: this.getRefreshToken() ?? ''
    };
    return this.http.post<RootModel>(url, body).pipe(
      tap(res => {
        if (res.success && res.data) {
          const data = res.data as AuthResponseModel;
          localStorage.setItem(this.TOKEN_KEY, data.accessToken);
          localStorage.setItem(this.REFRESH_KEY, data.refreshToken);
        }
      })
    );
  }

  logout(): void {
    localStorage.removeItem(this.TOKEN_KEY);
    localStorage.removeItem(this.REFRESH_KEY);
    localStorage.removeItem(this.USER_KEY);
  }

  isAuthenticated(): boolean {
    return !!this.getAccessToken();
  }

  getAccessToken(): string | null {
    return localStorage.getItem(this.TOKEN_KEY);
  }

  getRefreshToken(): string | null {
    return localStorage.getItem(this.REFRESH_KEY);
  }

  getCurrentUser(): AuthResponseModel | null {
    const raw = localStorage.getItem(this.USER_KEY);
    return raw ? (JSON.parse(raw) as AuthResponseModel) : null;
  }

  getUserName(): string {
    return this.getCurrentUser()?.userName ?? 'Guest';
  }
}
