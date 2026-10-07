import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { of } from 'rxjs';
import { environment } from '../../../environments/environment';
import { AuthService } from '../services/auth.service';
import { authInterceptor } from './auth.interceptor';

describe('CSRF request integration', () => {
  let client: HttpClient;
  let requests: HttpTestingController;
  let auth: jasmine.SpyObj<AuthService>;
  const writeUrl = `${environment.main_url}/master/products`;

  beforeEach(() => {
    auth = jasmine.createSpyObj<AuthService>('AuthService', ['getCsrfToken', 'refreshToken', 'logout']);
    auth.getCsrfToken.and.returnValue(of('current-token'));
    auth.refreshToken.and.returnValue(of({ success: true } as any));
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([authInterceptor])),
        provideHttpClientTesting(),
        { provide: AuthService, useValue: auth },
        { provide: Router, useValue: { navigate: jasmine.createSpy('navigate') } }
      ]
    });
    client = TestBed.inject(HttpClient);
    requests = TestBed.inject(HttpTestingController);
  });

  afterEach(() => requests.verify());

  it('attaches the fetched token and credentials to an API mutation', () => {
    client.post(writeUrl, {}).subscribe();
    const request = requests.expectOne(writeUrl);
    expect(request.request.headers.get('X-XSRF-TOKEN')).toBe('current-token');
    expect(request.request.withCredentials).toBeTrue();
    request.flush({});
  });

  it('does not send the mutation when token acquisition fails', () => {
    auth.getCsrfToken.and.returnValue(of(null));
    let failed = false;
    client.post(writeUrl, {}).subscribe({ error: () => { failed = true; } });
    requests.expectNone(writeUrl);
    expect(failed).toBeTrue();
  });

  it('does not fetch CSRF tokens for safe API reads', () => {
    client.get(writeUrl).subscribe();
    const request = requests.expectOne(writeUrl);
    expect(auth.getCsrfToken).not.toHaveBeenCalled();
    expect(request.request.withCredentials).toBeTrue();
    request.flush({});
  });

  it('does not attach credentials or CSRF tokens to third-party URLs', () => {
    const url = 'https://third-party.example/api/items';
    client.post(url, {}).subscribe();
    const request = requests.expectOne(url);
    expect(request.request.withCredentials).toBeFalse();
    expect(request.request.headers.has('X-XSRF-TOKEN')).toBeFalse();
    expect(auth.getCsrfToken).not.toHaveBeenCalled();
    request.flush({});
  });

  it('fetches a fresh token when retrying after session refresh', () => {
    auth.getCsrfToken.and.returnValues(of('old-token'), of('new-token'));
    client.post(writeUrl, {}).subscribe();
    requests.expectOne(writeUrl).flush({}, { status: 401, statusText: 'Unauthorized' });
    const retried = requests.expectOne(writeUrl);
    expect(retried.request.headers.get('X-XSRF-TOKEN')).toBe('new-token');
    expect(auth.refreshToken).toHaveBeenCalledTimes(1);
    retried.flush({});
  });

  it('does not retry a server-side CSRF rejection', () => {
    client.post(writeUrl, {}).subscribe({ error: () => {} });
    requests.expectOne(writeUrl).flush({}, { status: 400, statusText: 'Bad Request' });
    expect(auth.refreshToken).not.toHaveBeenCalled();
    expect(auth.getCsrfToken).toHaveBeenCalledTimes(1);
  });
});
