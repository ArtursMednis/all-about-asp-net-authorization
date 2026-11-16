import { HttpClient, HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { Injectable, inject, signal } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { catchError, firstValueFrom, throwError } from 'rxjs';

export type User = { name: string; isAdmin: boolean };

// Holds the current user for the whole app.
@Injectable({ providedIn: 'root' })
export class AuthService {
  private http = inject(HttpClient);

  readonly user = signal<User | null>(null);

  // The cookie survives page reloads, but JavaScript can't read it - so ask the API who we are.
  // Runs once at startup (see app.config.ts), before the first route is activated.
  async load() {
    try {
      this.user.set(await firstValueFrom(this.http.get<User>('/api/account/me')));
    } catch {
      this.user.set(null);
    }
  }
}

// Route guard - the SPA version of [Authorize]: anonymous users go to the login page,
// with a return URL to come back to. This is for the user's convenience only:
// the API checks the cookie on every request anyway.
export const authGuard: CanActivateFn = (_route, state) => {
  if (inject(AuthService).user()) {
    return true;
  }
  return inject(Router).createUrlTree(['/login'], { queryParams: { returnUrl: state.url } });
};

// Runs for every HttpClient request. If the API says 401 (the cookie expired or was removed),
// forget the user and go to the login page - the guard thought we were logged in, the API knows better.
export const authInterceptor: HttpInterceptorFn = (request, next) => {
  const auth = inject(AuthService);
  const router = inject(Router);

  return next(request).pipe(
    catchError((error: HttpErrorResponse) => {
      if (error.status === 401 && !request.url.startsWith('/api/account/')) {
        auth.user.set(null);
        router.navigate(['/login'], { queryParams: { returnUrl: router.url } });
      }
      return throwError(() => error);
    }),
  );
};

// Only return to paths inside this app, like Url.IsLocalUrl in tutorial 01.
export function safeReturnUrl(returnUrl: string | undefined) {
  return returnUrl?.startsWith('/') && !returnUrl.startsWith('//') ? returnUrl : '/';
}
