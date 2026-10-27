import { inject, Injectable, Injector } from '@angular/core';
import {
    HttpInterceptor,
    HttpHandler,
    HttpRequest,
    HttpEvent,
} from '@angular/common/http';
import { Router } from '@angular/router';
import { Observable, catchError, throwError } from 'rxjs';
import { environment } from '../../environments/environment';
import { SessionService } from '../services/session.service';

const SIGN_IN_URLS = ['/sign-in', '/login-by-email'];

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
            catchError((e) => {
                if (e.status === 401) {
                    this.handleUnauthorized();
                }

                return throwError(() => e);
            }),
        );
    }

    private handleUnauthorized(): void {
        this.injector.get(SessionService).onSessionLost();

        const router = this.injector.get(Router);
        if (!SIGN_IN_URLS.some((url) => router.url.startsWith(url))) {
            router.navigate(['/sign-in']);
        }
    }
}
