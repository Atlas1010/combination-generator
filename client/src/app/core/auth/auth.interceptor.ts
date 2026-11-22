import { HttpErrorResponse, HttpInterceptorFn, HttpRequest } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, from, switchMap, throwError } from 'rxjs';

import { AuthToken } from './auth-token';

const withToken = (request: HttpRequest<unknown>, token: string) =>
  request.clone({ setHeaders: { Authorization: `Bearer ${token}` } });

// Attaches the bearer token and retries once after a 401.
export const authInterceptor: HttpInterceptorFn = (request, next) => {
  if (!request.url.startsWith('/api/')) {
    return next(request);
  }

  const auth = inject(AuthToken);
  return from(auth.get()).pipe(
    switchMap((token) => next(withToken(request, token))),
    catchError((error: unknown) => {
      if (!(error instanceof HttpErrorResponse) || error.status !== 401) {
        return throwError(() => error);
      }

      auth.invalidate();
      return from(auth.get()).pipe(switchMap((token) => next(withToken(request, token))));
    }),
  );
};
