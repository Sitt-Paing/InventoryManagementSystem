import { HttpClient } from '@angular/common/http';
import { inject, Injectable, signal } from '@angular/core';
import { Observable, of } from 'rxjs';
import { catchError, map, tap } from 'rxjs/operators';
import { environment } from '../../../environments/environment';
import { RootModel } from '../models/root.model';
import { AuthResponseModel, LoginModel } from '../models/auth.model';

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private http = inject(HttpClient);
  
  // Storage Keys (Flat keys only in localStorage, no userId, no AUTH_USER object)
  private readonly USERNAME_KEY = 'userName';
  private readonly EMAIL_KEY = 'email';
  private readonly ROLES_KEY = 'userRoles';
  private readonly THEME_KEY = 'theme';
  private readonly REMEMBER_ME_KEY = 'rememberMe';

  public userNameSignal = signal<string>(this.getStoredUserName());
  public userRolesSignal = signal<string[]>(this.getStoredUserRoles());

  /**
   * Login user and receive HttpOnly cookies + user profile
   */
  login(model: LoginModel): Observable<RootModel> {
    const url = `${environment.main_url}/Auth/login`;
    return this.http.post<RootModel>(url, model, { withCredentials: true }).pipe(
      tap(res => {
        if (res.success && res.data) {
          const dto = res.data as any;
          this.storeFlatUserData({
            userName: dto.userName ?? '',
            email: dto.email ?? '',
            roles: dto.roles ?? [],
            theme: dto.theme ?? model.theme,
            rememberMe: dto.rememberMe ?? model.rememberMe
          });
        }
      })
    );
  }

  /**
   * Refresh session using HttpOnly refresh token cookie
   */
  refreshToken(): Observable<RootModel> {
    const url = `${environment.main_url}/Auth/refresh-token`;
    return this.http.post<RootModel>(url, {}, { withCredentials: true }).pipe(
      tap(res => {
        if (res.success && res.data) {
          const dto = res.data as any;
          if (dto.userName) {
            this.storeFlatUserData({
              userName: dto.userName,
              email: dto.email ?? this.getEmail() ?? '',
              roles: dto.roles ?? this.getUserRoles()
            });
          }
        }
      }),
      catchError(err => {
        this.clearLocalUser();
        return of({ success: false, message: 'Session expired.' } as RootModel);
      })
    );
  }

  /**
   * Logout from backend (clears HttpOnly cookies) and local storage
   */
  logout(): Observable<RootModel> {
    const url = `${environment.main_url}/Auth/logout`;
    return this.http.post<RootModel>(url, {}, { withCredentials: true }).pipe(
      tap(() => this.clearLocalUser()),
      catchError(() => {
        this.clearLocalUser();
        return of({ success: true, message: 'Logged out.' } as RootModel);
      })
    );
  }

  /**
   * Fetch current authenticated profile from server
   */
  getProfile(): Observable<AuthResponseModel | null> {
    const url = `${environment.main_url}/Auth/me`;
    return this.http.get<RootModel>(url, { withCredentials: true }).pipe(
      map(res => {
        if (res.success && res.data) {
          const dto = res.data as any;
          this.storeFlatUserData({
            userName: dto.userName ?? '',
            email: dto.email ?? '',
            roles: dto.roles ?? []
          });
          return dto as AuthResponseModel;
        }
        return null;
      }),
      catchError(() => of(null))
    );
  }

  /**
   * Fetch CSRF token from server
   */
  getCsrfToken(): Observable<string | null> {
    const url = `${environment.main_url}/Auth/csrf-token`;
    return this.http.get<RootModel>(url, { withCredentials: true }).pipe(
      map(res => (res.success && res.data?.csrfToken ? (res.data.csrfToken as string) : null)),
      catchError(() => of(null))
    );
  }

  isAuthenticated(): boolean {
    return !!this.getUserName() && this.getUserName() !== 'Guest';
  }

  getUserName(): string {
    return this.userNameSignal();
  }

  getUserInitial(): string {
    const name = this.getUserName();
    return (name && name.length > 0 ? name.charAt(0) : 'U').toUpperCase();
  }

  getEmail(): string | null {
    if (typeof localStorage === 'undefined') return null;
    return localStorage.getItem(this.EMAIL_KEY);
  }

  getUserRoles(): string[] {
    return this.userRolesSignal();
  }

  getTheme(): string {
    if (typeof localStorage === 'undefined') return 'light';
    return localStorage.getItem(this.THEME_KEY) ?? 'light';
  }

  getRememberMe(): boolean {
    if (typeof localStorage === 'undefined') return false;
    return localStorage.getItem(this.REMEMBER_ME_KEY) === 'true';
  }

  /**
   * Read cookie value by name from document.cookie (used for XSRF-TOKEN)
   */
  public getCookie(name: string): string | null {
    if (typeof document === 'undefined') return null;
    const nameEQ = `${name}=`;
    const ca = document.cookie.split(';');
    for (let i = 0; i < ca.length; i++) {
      let c = ca[i];
      while (c.charAt(0) === ' ') c = c.substring(1, c.length);
      if (c.indexOf(nameEQ) === 0) return decodeURIComponent(c.substring(nameEQ.length, c.length));
    }
    return null;
  }

  private storeFlatUserData(data: {
    userName?: string;
    email?: string;
    roles?: string[];
    theme?: string;
    rememberMe?: boolean;
  }): void {
    if (typeof localStorage === 'undefined') return;

    if (data.userName) {
      localStorage.setItem(this.USERNAME_KEY, data.userName);
      this.userNameSignal.set(data.userName);
    }
    if (data.email) {
      localStorage.setItem(this.EMAIL_KEY, data.email);
    }
    if (data.roles) {
      localStorage.setItem(this.ROLES_KEY, JSON.stringify(data.roles));
      this.userRolesSignal.set(data.roles);
    }
    if (data.theme) {
      localStorage.setItem(this.THEME_KEY, data.theme);
    }
    if (data.rememberMe !== undefined) {
      localStorage.setItem(this.REMEMBER_ME_KEY, String(data.rememberMe));
    }

    // Clean up any legacy/unwanted keys
    localStorage.removeItem('userId');
    localStorage.removeItem('AUTH_USER');
    localStorage.removeItem('ACCESS_TOKEN');
    localStorage.removeItem('REFRESH_TOKEN');
  }

  private clearLocalUser(): void {
    if (typeof localStorage === 'undefined') return;

    localStorage.removeItem(this.USERNAME_KEY);
    localStorage.removeItem(this.EMAIL_KEY);
    localStorage.removeItem(this.ROLES_KEY);
    localStorage.removeItem(this.REMEMBER_ME_KEY);
    localStorage.removeItem('userId');
    localStorage.removeItem('AUTH_USER');
    localStorage.removeItem('ACCESS_TOKEN');
    localStorage.removeItem('REFRESH_TOKEN');

    this.userNameSignal.set('Guest');
    this.userRolesSignal.set([]);
  }

  private getStoredUserName(): string {
    if (typeof localStorage === 'undefined') return 'Guest';
    return localStorage.getItem(this.USERNAME_KEY) ?? 'Guest';
  }

  private getStoredUserRoles(): string[] {
    if (typeof localStorage === 'undefined') return [];
    const raw = localStorage.getItem(this.ROLES_KEY);
    if (!raw) return [];
    try {
      return JSON.parse(raw);
    } catch {
      return [raw];
    }
  }
}


