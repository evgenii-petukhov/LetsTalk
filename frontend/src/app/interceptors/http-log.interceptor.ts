import { inject, Injectable } from '@angular/core';
import {
    HttpInterceptor,
    HttpHandler,
    HttpRequest,
    HttpEvent,
    HttpResponse,
} from '@angular/common/http';
import { Observable, filter, tap } from 'rxjs';
import { RequestLoggingService } from '../services/request-logging.service';

@Injectable()
export class HttpLogInterceptor implements HttpInterceptor {
    private readonly requestLoggingService = inject(RequestLoggingService);

    intercept<T>(
        request: HttpRequest<T>,
        next: HttpHandler,
    ): Observable<HttpEvent<T>> {
        const t0 = performance.now();
        return next.handle(request).pipe(
            filter((response) => response instanceof HttpResponse),
            tap((response: HttpResponse<T>) => {
                const downloaded =
                    response.body && response.body instanceof Blob
                        ? response.body.size
                        : 0;
                const uploaded =
                    request.body && typeof request.body === 'string'
                        ? request.body.length
                        : 0;
                this.requestLoggingService.log(
                    request.method,
                    request.url,
                    performance.now() - t0,
                    downloaded,
                    uploaded,
                );
            }),
        );
    }
}
