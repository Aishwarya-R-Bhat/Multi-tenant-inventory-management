import { HttpErrorResponse, HttpInterceptorFn, HttpRequest } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, switchMap, throwError } from 'rxjs';
import { AuthService } from '../auth/auth.service';

/** Adds the bearer token. On a 401 it refreshes once (shared between parallel requests) and retries the request. */
export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const auth = inject(AuthService);
  const router = inject(Router);

  if (req.url.includes('/auth/')) return next(req);

  const withToken = (r: HttpRequest<unknown>) =>
    auth.accessToken ? r.clone({ setHeaders: { Authorization: `Bearer ${auth.accessToken}` } }) : r;

  return next(withToken(req)).pipe(
    catchError((err) => {
      if (err instanceof HttpErrorResponse && err.status === 401 && auth.hasRefreshToken) {
        return auth.refresh().pipe(
          switchMap(() => next(withToken(req))),
          catchError(() => {
            auth.logout();
            void router.navigate(['/login']);
            return throwError(() => err);
          }),
        );
      }
      return throwError(() => err);
    }),
  );
};
