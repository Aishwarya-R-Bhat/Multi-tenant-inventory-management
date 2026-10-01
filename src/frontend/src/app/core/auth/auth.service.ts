import { HttpClient } from '@angular/common/http';
import { computed, inject, Injectable, signal } from '@angular/core';
import { Observable, finalize, map, shareReplay, tap, throwError } from 'rxjs';
import { environment } from '../../../environments/environment';
import { AuthResponse, LoginRequest, RegisterTenantRequest } from './auth.models';
import { decodeUser } from './jwt';

const STORAGE_KEY = 'inventory.session';

interface Session {
  accessToken: string;
  refreshToken: string;
}

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly api = `${environment.apiBaseUrl}/auth`;

  private readonly session = signal<Session | null>(this.load());
  private refreshing$?: Observable<AuthResponse>;

  readonly user = computed(() => {
    const s = this.session();
    return s ? decodeUser(s.accessToken) : null;
  });
  readonly isAuthenticated = computed(() => this.user() !== null);

  get accessToken(): string | null {
    return this.session()?.accessToken ?? null;
  }

  get hasRefreshToken(): boolean {
    return !!this.session()?.refreshToken;
  }

  hasPermission(permission: string): boolean {
    return this.user()?.permissions.includes(permission) ?? false;
  }

  login(request: LoginRequest): Observable<void> {
    return this.http.post<AuthResponse>(`${this.api}/login`, request).pipe(
      tap((r) => this.store(r)),
      map(() => undefined),
    );
  }

  registerTenant(request: RegisterTenantRequest): Observable<void> {
    return this.http.post<AuthResponse>(`${this.api}/register-tenant`, request).pipe(
      tap((r) => this.store(r)),
      map(() => undefined),
    );
  }

  /** Several requests can fail with 401 at once. They share one refresh call, because a refresh token works only once. */
  refresh(): Observable<AuthResponse> {
    if (!this.refreshing$) {
      const refreshToken = this.session()?.refreshToken;
      if (!refreshToken) return throwError(() => new Error('No refresh token'));

      this.refreshing$ = this.http.post<AuthResponse>(`${this.api}/refresh`, { refreshToken }).pipe(
        tap((r) => this.store(r)),
        finalize(() => (this.refreshing$ = undefined)),
        shareReplay(1),
      );
    }
    return this.refreshing$;
  }

  logout(): void {
    this.session.set(null);
    this.persist(null);
  }

  private store(r: AuthResponse): void {
    const session = { accessToken: r.accessToken, refreshToken: r.refreshToken };
    this.session.set(session);
    this.persist(session);
  }

  private load(): Session | null {
    try {
      const raw = localStorage.getItem(STORAGE_KEY);
      return raw ? (JSON.parse(raw) as Session) : null;
    } catch {
      return null;
    }
  }

  private persist(session: Session | null): void {
    try {
      if (session) localStorage.setItem(STORAGE_KEY, JSON.stringify(session));
      else localStorage.removeItem(STORAGE_KEY);
    } catch {
      // Storage unavailable: the session then lasts only until the page closes.
    }
  }
}
