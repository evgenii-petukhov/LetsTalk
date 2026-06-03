import { HTTP_INTERCEPTORS } from '@angular/common/http';
import { HttpLogInterceptor } from '../interceptors/http-log.interceptor';

export const httpLogInterceptorProvider = [
    { provide: HTTP_INTERCEPTORS, useClass: HttpLogInterceptor, multi: true },
];
