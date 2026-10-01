import { HttpClient } from '@angular/common/http';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { AuthService } from '../auth/auth.service';
import { fakeToken } from '../auth/fake-token';
import { authInterceptor } from './auth.interceptor';
import { errorInterceptor } from './error.interceptor';

describe('authInterceptor', () => {
  let http: HttpClient;
  let backend: HttpTestingController;
  let auth: AuthService;

  const loginResponse = (suffix: string) => ({
    accessToken: fakeToken({ sub: 'u1', tenant_slug: 'acme', n: suffix }),
    accessTokenExpiresAt: '2030-01-01T00:00:00Z',
    refreshToken: `refresh-${suffix}`,
  });

  beforeEach(() => {
    localStorage.clear();
    TestBed.configureTestingModule({
      providers: [
        provideRouter([{ path: 'login', children: [] }]),
        provideHttpClient(withInterceptors([errorInterceptor, authInterceptor])),
        provideHttpClientTesting(),
      ],
    });
    http = TestBed.inject(HttpClient);
    backend = TestBed.inject(HttpTestingController);
    auth = TestBed.inject(AuthService);
    auth.login({ tenantSlug: 'acme', email: 'a@b.com', password: 'x' }).subscribe();
    backend.expectOne('/api/v1/auth/login').flush(loginResponse('1'));
  });

  afterEach(() => backend.verify());

  it('adds the bearer token to API calls', () => {
    http.get('/api/v1/users').subscribe();
    const req = backend.expectOne('/api/v1/users');
    expect(req.request.headers.get('Authorization')).toBe(`Bearer ${auth.accessToken}`);
    req.flush([]);
  });

  it('refreshes once on 401 and retries with the new token', () => {
    let result: unknown;
    http.get('/api/v1/users').subscribe((r) => (result = r));

    backend.expectOne('/api/v1/users').flush(null, { status: 401, statusText: 'Unauthorized' });
    const refresh = backend.expectOne('/api/v1/auth/refresh');
    expect(refresh.request.body).toEqual({ refreshToken: 'refresh-1' });
    refresh.flush(loginResponse('2'));

    const retry = backend.expectOne('/api/v1/users');
    expect(retry.request.headers.get('Authorization')).toBe(`Bearer ${auth.accessToken}`);
    retry.flush(['ok']);
    expect(result).toEqual(['ok']);
  });

  it('shares one refresh call between parallel 401s', () => {
    http.get('/api/v1/a').subscribe();
    http.get('/api/v1/b').subscribe();

    backend.expectOne('/api/v1/a').flush(null, { status: 401, statusText: 'Unauthorized' });
    backend.expectOne('/api/v1/b').flush(null, { status: 401, statusText: 'Unauthorized' });

    backend.expectOne('/api/v1/auth/refresh').flush(loginResponse('2')); // exactly one refresh call
    backend.expectOne('/api/v1/a').flush([]);
    backend.expectOne('/api/v1/b').flush([]);
  });

  it('signs the user out when the refresh fails', () => {
    let failed = false;
    http.get('/api/v1/users').subscribe({ error: () => (failed = true) });

    backend.expectOne('/api/v1/users').flush(null, { status: 401, statusText: 'Unauthorized' });
    backend.expectOne('/api/v1/auth/refresh').flush(null, { status: 401, statusText: 'Unauthorized' });

    expect(failed).toBe(true);
    expect(auth.isAuthenticated()).toBe(false);
  });

  it('turns ProblemDetails into an ApiError', () => {
    let message = '';
    http.post('/api/v1/auth/login', {}).subscribe({ error: (e) => (message = e.message) });
    backend.expectOne('/api/v1/auth/login').flush({ title: 'Invalid company code, email or password.' }, { status: 401, statusText: 'Unauthorized' });
    expect(message).toBe('Invalid company code, email or password.');
  });
});
