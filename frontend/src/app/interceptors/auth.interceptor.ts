import { inject, Injectable, Injector } from '@angular/core';
import {
    HttpInterceptor,
    HttpHandler,
    HttpRequest,
    HttpEvent,
} from '@angular/common/http';
import { Observable, catchError, from, switchMap, throwError } from 'rxjs';
import { environment } from '../../environments/environment';
import { SessionService } from '../services/session.service';

// A 401 from these calls is their own answer (an unknown refresh token, a wrong code), not an expired access token
const OWN_AUTHENTICATION_URLS = [
    `${environment.services.api.url}/api/Authentication/refresh`,
    `${environment.services.api.url}/api/Authentication/email-login`,
    `${environment.services.api.url}/api/Authentication/logout`,
];

@Injectable()
export class AuthInterceptor implements HttpInterceptor {
    // SessionService depends on HttpClient, so it is resolved lazily to avoid a circular dependency
    private readonly injector = inject(Injector);

    intercept<T>(
        request: HttpRequest<T>,
        next: HttpHandler,
    ): Observable<HttpEvent<T>> {
        // The session is an HttpOnly cookie: only requests to our API carry it
        const authRequest = request.url.startsWith(environment.services.api.url)
            ? request.clone({ withCredentials: true })
            : request;

        return next.handle(authRequest).pipe(
            catchError((error) =>
                error.status === 401 && this.canBeRenewed(authRequest)
                    ? this.renewAndRetry(authRequest, next, error)
                    : throwError(() => error),
            ),
        );
    }

    private canBeRenewed<T>(request: HttpRequest<T>): boolean {
        return (
            request.url.startsWith(environment.services.api.url) &&
            !OWN_AUTHENTICATION_URLS.some((url) => request.url.startsWith(url))
        );
    }

    // The access token lives only minutes: renew it with the refresh cookie and send the request again,
    // once. SessionService shares one renewal among all requests that fail at the same time.
    private renewAndRetry<T>(
        request: HttpRequest<T>,
        next: HttpHandler,
        unauthorized: unknown,
    ): Observable<HttpEvent<T>> {
        const sessionService = this.injector.get(SessionService);

        return from(sessionService.renew()).pipe(
            switchMap((renewed) =>
                // false: the refresh token was rejected, SessionService has already ended the session
                renewed
                    ? next.handle(request).pipe(
                          catchError((error) => {
                              if (error.status === 401) {
                                  sessionService.onSessionExpired();
                              }

                              return throwError(() => error);
                          }),
                      )
                    : throwError(() => unauthorized),
            ),
        );
    }
}
