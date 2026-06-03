import { inject, Injectable } from '@angular/core';
import {
    HttpInterceptor,
    HttpHandler,
    HttpRequest,
    HttpEvent,
} from '@angular/common/http';
import { TokenStorageService } from '../services/token-storage.service';
import { Observable, catchError, throwError } from 'rxjs';

const TOKEN_HEADER_KEY = 'Authorization';

@Injectable()
export class AuthInterceptor implements HttpInterceptor {
    private readonly tokenStorageService = inject(TokenStorageService);

    intercept<T>(
        request: HttpRequest<T>,
        next: HttpHandler,
    ): Observable<HttpEvent<T>> {
        let authRequest = request;
        const token = this.tokenStorageService.getToken();
        if (token != null) {
            authRequest = request.clone({
                headers: request.headers.set(
                    TOKEN_HEADER_KEY,
                    'Bearer ' + token,
                ),
            });
        }
        return next.handle(authRequest).pipe(
            catchError((e) => {
                console.log(e);
                if (e.status === 401) {
                    window.localStorage.clear();
                    window.location.reload();
                }

                return throwError(() => e);
            }),
        );
    }
}
