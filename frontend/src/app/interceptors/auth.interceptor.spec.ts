import { beforeEach, describe, expect, it, vi, type MockedObject } from 'vitest';
import {
    HttpClient,
    provideHttpClient,
    withInterceptorsFromDi,
} from '@angular/common/http';
import {
    HttpTestingController,
    provideHttpClientTesting,
} from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { environment } from '../../environments/environment';
import { authInterceptorProvider } from '../providers/auth-interceptor-provider';
import { SessionService } from '../services/session.service';

describe('AuthInterceptor', () => {
    const apiUrl = `${environment.services.api.url}/api/Chat`;
    const otherUrl = 'https://example.com/image.png';

    let http: HttpClient;
    let httpTesting: HttpTestingController;
    let sessionService: MockedObject<SessionService>;
    let router: { url: string; navigate: ReturnType<typeof vi.fn> };

    beforeEach(() => {
        sessionService = {
            onSessionLost: vi.fn().mockName('SessionService.onSessionLost'),
        } as unknown as MockedObject<SessionService>;
        router = { url: '/messenger', navigate: vi.fn() };

        TestBed.configureTestingModule({
            providers: [
                provideHttpClient(withInterceptorsFromDi()),
                provideHttpClientTesting(),
                authInterceptorProvider,
                { provide: SessionService, useValue: sessionService },
                { provide: Router, useValue: router },
            ],
        });
        http = TestBed.inject(HttpClient);
        httpTesting = TestBed.inject(HttpTestingController);
    });

    it('should send credentials with requests to the API', () => {
        http.get(apiUrl).subscribe();

        const req = httpTesting.expectOne(apiUrl);
        expect(req.request.withCredentials).toBe(true);
    });

    it('should not send credentials with requests to other origins', () => {
        http.get(otherUrl).subscribe();

        const req = httpTesting.expectOne(otherUrl);
        expect(req.request.withCredentials).toBe(false);
    });

    it('should not add an Authorization header', () => {
        http.get(apiUrl).subscribe();

        const req = httpTesting.expectOne(apiUrl);
        expect(req.request.headers.has('Authorization')).toBe(false);
    });

    it('should drop the session and go to sign-in on 401', () => {
        http.get(apiUrl).subscribe({ error: () => undefined });

        httpTesting
            .expectOne(apiUrl)
            .flush(null, { status: 401, statusText: 'Unauthorized' });

        expect(sessionService.onSessionLost).toHaveBeenCalledTimes(1);
        expect(router.navigate).toHaveBeenCalledWith(['/sign-in']);
    });

    it('should not navigate again when the user is already on a sign-in page', () => {
        router.url = '/login-by-email';
        http.get(apiUrl).subscribe({ error: () => undefined });

        httpTesting
            .expectOne(apiUrl)
            .flush(null, { status: 401, statusText: 'Unauthorized' });

        expect(sessionService.onSessionLost).toHaveBeenCalledTimes(1);
        expect(router.navigate).not.toHaveBeenCalled();
    });

    it('should pass other errors through without dropping the session', () => {
        const onError = vi.fn();
        http.get(apiUrl).subscribe({ error: onError });

        httpTesting
            .expectOne(apiUrl)
            .flush(null, { status: 500, statusText: 'Server Error' });

        expect(onError).toHaveBeenCalled();
        expect(sessionService.onSessionLost).not.toHaveBeenCalled();
        expect(router.navigate).not.toHaveBeenCalled();
    });
});
