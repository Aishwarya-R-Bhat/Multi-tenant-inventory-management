import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { catchError, throwError } from 'rxjs';
import { toApiError } from './api-error';

/** Outermost interceptor: those below it see a raw HttpErrorResponse, callers above it get an ApiError. */
export const errorInterceptor: HttpInterceptorFn = (req, next) =>
  next(req).pipe(
    catchError((err) => throwError(() => (err instanceof HttpErrorResponse ? toApiError(err) : err))),
  );
