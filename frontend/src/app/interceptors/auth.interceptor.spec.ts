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
import { Store } from '@ngrx/store';
import { environment } from '../../environments/environment';
import { authInterceptorProvider } from '../providers/auth-interceptor-provider';
import { ApiService } from '../services/api.service';
import { SessionService } from '../services/session.service';

const unauthorized = { status: 401, statusText: 'Unauthorized' };

describe('AuthInterceptor', () => {
    const apiUrl = `${environment.services.api.url}/api/Chat`;
    const otherUrl = 'https://example.com/image.png';

    let http: HttpClient;
    let httpTesting: HttpTestingController;
    let sessionService: MockedObject<SessionService>;

    beforeEach(() => {
        sessionService = {
            renew: vi.fn().mockName('SessionService.renew'),
            onSessionExpired: vi.fn().mockName('SessionService.onSessionExpired'),
        } as unknown as MockedObject<SessionService>;

        TestBed.configureTestingModule({
            providers: [
                provideHttpClient(withInterceptorsFromDi()),
                provideHttpClientTesting(),
                authInterceptorProvider,
                { provide: SessionService, useValue: sessionService },
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

    it('should renew the session and retry the request once on 401', async () => {
        sessionService.renew.mockResolvedValue(true);
        const onNext = vi.fn();
        http.get(apiUrl).subscribe(onNext);

        httpTesting.expectOne(apiUrl).flush(null, unauthorized);
        const retry = await vi.waitFor(() => httpTesting.expectOne(apiUrl));
        retry.flush({ ok: true });

        expect(sessionService.renew).toHaveBeenCalledTimes(1);
        expect(retry.request.withCredentials).toBe(true);
        expect(onNext).toHaveBeenCalledWith({ ok: true });
        expect(sessionService.onSessionExpired).not.toHaveBeenCalled();
        httpTesting.verify();
    });

    it('should go to sign-in when the retried request is rejected as well', async () => {
        sessionService.renew.mockResolvedValue(true);
        const onError = vi.fn();
        http.get(apiUrl).subscribe({ error: onError });

        httpTesting.expectOne(apiUrl).flush(null, unauthorized);
        const retry = await vi.waitFor(() => httpTesting.expectOne(apiUrl));
        retry.flush(null, unauthorized);

        expect(sessionService.renew).toHaveBeenCalledTimes(1);
        expect(sessionService.onSessionExpired).toHaveBeenCalledTimes(1);
        expect(onError).toHaveBeenCalledWith(expect.objectContaining({ status: 401 }));
        httpTesting.verify();
    });

    it('should pass the 401 through without retrying when the renewal is rejected', async () => {
        sessionService.renew.mockResolvedValue(false);
        const onError = vi.fn();
        http.get(apiUrl).subscribe({ error: onError });

        httpTesting.expectOne(apiUrl).flush(null, unauthorized);
        await vi.waitFor(() => expect(onError).toHaveBeenCalled());

        // SessionService.renew already dropped the session and went to sign-in
        expect(onError).toHaveBeenCalledWith(expect.objectContaining({ status: 401 }));
        expect(sessionService.onSessionExpired).not.toHaveBeenCalled();
        httpTesting.verify();
    });

    it('should pass the renewal error through without retrying when the server cannot answer', async () => {
        const outage = { status: 503 };
        sessionService.renew.mockRejectedValue(outage);
        const onError = vi.fn();
        http.get(apiUrl).subscribe({ error: onError });

        httpTesting.expectOne(apiUrl).flush(null, unauthorized);
        await vi.waitFor(() => expect(onError).toHaveBeenCalled());

        expect(onError).toHaveBeenCalledWith(outage);
        expect(sessionService.onSessionExpired).not.toHaveBeenCalled();
        httpTesting.verify();
    });

    it.each(['refresh', 'email-login', 'logout'])(
        'should not renew when the %s request itself is rejected',
        async (endpoint) => {
            const url = `${environment.services.api.url}/api/Authentication/${endpoint}`;
            const onError = vi.fn();
            http.post(url, null).subscribe({ error: onError });

            httpTesting.expectOne(url).flush(null, unauthorized);

            expect(onError).toHaveBeenCalledWith(expect.objectContaining({ status: 401 }));
            expect(sessionService.renew).not.toHaveBeenCalled();
            expect(sessionService.onSessionExpired).not.toHaveBeenCalled();
            httpTesting.verify();
        },
    );

    it('should not renew for a 401 from another origin', () => {
        const onError = vi.fn();
        http.get(otherUrl).subscribe({ error: onError });

        httpTesting.expectOne(otherUrl).flush(null, unauthorized);

        expect(onError).toHaveBeenCalled();
        expect(sessionService.renew).not.toHaveBeenCalled();
    });

    it('should pass other errors through without renewing', () => {
        const onError = vi.fn();
        http.get(apiUrl).subscribe({ error: onError });

        httpTesting
            .expectOne(apiUrl)
            .flush(null, { status: 500, statusText: 'Server Error' });

        expect(onError).toHaveBeenCalled();
        expect(sessionService.renew).not.toHaveBeenCalled();
        expect(sessionService.onSessionExpired).not.toHaveBeenCalled();
    });
});

describe('AuthInterceptor with the real SessionService', () => {
    const apiUrl = `${environment.services.api.url}/api/Chat`;

    let http: HttpClient;
    let httpTesting: HttpTestingController;
    let apiService: { refreshSession: ReturnType<typeof vi.fn> };

    beforeEach(() => {
        apiService = { refreshSession: vi.fn().mockName('ApiService.refreshSession') };

        TestBed.configureTestingModule({
            providers: [
                provideHttpClient(withInterceptorsFromDi()),
                provideHttpClientTesting(),
                authInterceptorProvider,
                SessionService,
                { provide: ApiService, useValue: apiService },
                { provide: Store, useValue: { dispatch: vi.fn() } },
                { provide: Router, useValue: { url: '/messenger', navigate: vi.fn() } },
            ],
        });
        http = TestBed.inject(HttpClient);
        httpTesting = TestBed.inject(HttpTestingController);
    });

    it('should renew only once when several requests are rejected together', async () => {
        apiService.refreshSession.mockResolvedValue(undefined);
        const first = vi.fn();
        const second = vi.fn();
        http.get(apiUrl).subscribe(first);
        http.get(apiUrl).subscribe(second);

        for (const request of httpTesting.match(apiUrl)) {
            request.flush(null, unauthorized);
        }
        const retries = await vi.waitFor(() => {
            const pending = httpTesting.match(apiUrl);
            expect(pending).toHaveLength(2);
            return pending;
        });
        retries.forEach((retry) => retry.flush({ ok: true }));

        expect(apiService.refreshSession).toHaveBeenCalledTimes(1);
        expect(first).toHaveBeenCalledWith({ ok: true });
        expect(second).toHaveBeenCalledWith({ ok: true });
        httpTesting.verify();
    });
});
