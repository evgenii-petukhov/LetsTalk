import { afterEach, beforeEach, describe, expect, it, vi, type MockedObject } from 'vitest';
import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { Store } from '@ngrx/store';
import { environment } from '../../environments/environment';
import { SessionService } from './session.service';
import { ApiService } from './api.service';
import { sessionActions } from '../state/session/session.actions';

describe('SessionService', () => {
    let service: SessionService;
    let apiService: MockedObject<ApiService>;
    let store: { dispatch: ReturnType<typeof vi.fn> };
    let router: { url: string; navigate: ReturnType<typeof vi.fn> };

    beforeEach(() => {
        vi.clearAllMocks();
        apiService = {
            getProfile: vi.fn().mockName('ApiService.getProfile'),
            logout: vi.fn().mockName('ApiService.logout'),
            refreshSession: vi.fn().mockName('ApiService.refreshSession'),
        } as unknown as MockedObject<ApiService>;

        store = { dispatch: vi.fn().mockName('Store.dispatch') };
        router = { url: '/messenger', navigate: vi.fn().mockName('Router.navigate') };

        TestBed.configureTestingModule({
            providers: [
                SessionService,
                { provide: ApiService, useValue: apiService },
                { provide: Store, useValue: store },
                { provide: Router, useValue: router },
            ],
        });
        service = TestBed.inject(SessionService);
    });

    describe('isSignedIn', () => {
        it('should be true and cached when the probe succeeds', async () => {
            apiService.getProfile.mockResolvedValue({});

            const first = await service.isSignedIn();
            const second = await service.isSignedIn();

            expect(first).toBe(true);
            expect(second).toBe(true);
            expect(apiService.getProfile).toHaveBeenCalledTimes(1);
        });

        it('should be false when the probe is rejected', async () => {
            apiService.getProfile.mockRejectedValue({ status: 401 });

            const result = await service.isSignedIn();

            expect(result).toBe(false);
        });

        it('should probe through ApiService.getProfile, not the cached NgRx profile', async () => {
            apiService.getProfile.mockResolvedValue({});

            await service.isSignedIn();

            expect(apiService.getProfile).toHaveBeenCalled();
        });

        it('should share one probe among concurrent callers', async () => {
            apiService.getProfile.mockResolvedValue({});

            const results = await Promise.all([
                service.isSignedIn(),
                service.isSignedIn(),
                service.isSignedIn(),
            ]);

            expect(results).toEqual([true, true, true]);
            expect(apiService.getProfile).toHaveBeenCalledTimes(1);
        });
    });

    describe('a probe that fails for a reason other than 401', () => {
        it('should keep the previous state: an outage is not a sign-out', async () => {
            apiService.getProfile.mockResolvedValue({});
            await service.isSignedIn();
            apiService.getProfile.mockRejectedValue({ status: 503 });

            const result = await service.refresh();

            expect(result).toBe(true);
            expect(await service.isSignedIn()).toBe(true);
        });

        it('should not cache the failure when nothing is known yet', async () => {
            apiService.getProfile.mockRejectedValueOnce({ status: 503 });
            apiService.getProfile.mockResolvedValue({});

            const first = await service.isSignedIn();
            const second = await service.isSignedIn();

            expect(first).toBe(false);
            expect(second).toBe(true);
            expect(apiService.getProfile).toHaveBeenCalledTimes(2);
        });
    });

    describe('refresh', () => {
        it('should bypass the cached result and probe again', async () => {
            apiService.getProfile.mockResolvedValue({});
            await service.isSignedIn();
            apiService.getProfile.mockRejectedValue({ status: 401 });

            const result = await service.refresh();

            expect(result).toBe(false);
            expect(apiService.getProfile).toHaveBeenCalledTimes(2);
            expect(await service.isSignedIn()).toBe(false);
        });
    });

    describe('sessionLost', () => {
        it('should be read-only for subscribers, without a way to emit from outside', () => {
            expect('next' in service.sessionLost).toBe(false);
        });
    });

    describe('onSessionLost', () => {
        it('should dispatch the action that resets the store', () => {
            service.onSessionLost();

            expect(store.dispatch).toHaveBeenCalledWith(sessionActions.ended());
        });

        it('should invalidate the cached state and notify subscribers', async () => {
            apiService.getProfile.mockResolvedValue({});
            await service.isSignedIn();
            const listener = vi.fn();
            service.sessionLost.subscribe(listener);

            service.onSessionLost();

            expect(listener).toHaveBeenCalledTimes(1);
            expect(await service.isSignedIn()).toBe(false);
            expect(apiService.getProfile).toHaveBeenCalledTimes(1);
        });
    });

    describe('renew', () => {
        it('should call the API once for concurrent callers', async () => {
            apiService.refreshSession.mockResolvedValue();

            const results = await Promise.all([
                service.renew(),
                service.renew(),
                service.renew(),
            ]);

            expect(results).toEqual([true, true, true]);
            expect(apiService.refreshSession).toHaveBeenCalledTimes(1);
        });

        it('should keep the session when the refresh succeeds', async () => {
            apiService.refreshSession.mockResolvedValue();

            const result = await service.renew();

            expect(result).toBe(true);
            expect(store.dispatch).not.toHaveBeenCalled();
            expect(router.navigate).not.toHaveBeenCalled();
            expect(await service.isSignedIn()).toBe(true);
        });

        it('should clear the state and go to sign-in when the refresh token is rejected', async () => {
            apiService.refreshSession.mockRejectedValue({ status: 401 });

            const result = await service.renew();

            expect(result).toBe(false);
            expect(store.dispatch).toHaveBeenCalledWith(sessionActions.ended());
            expect(router.navigate).toHaveBeenCalledWith(['/sign-in']);
            expect(await service.isSignedIn()).toBe(false);
        });

        it('should reject and keep the session when the server cannot answer', async () => {
            apiService.getProfile.mockResolvedValue({});
            await service.isSignedIn();
            apiService.refreshSession.mockRejectedValue({ status: 503 });

            await expect(service.renew()).rejects.toEqual({ status: 503 });

            expect(store.dispatch).not.toHaveBeenCalled();
            expect(router.navigate).not.toHaveBeenCalled();
            expect(await service.isSignedIn()).toBe(true);
        });

        it('should allow a new attempt after a failure', async () => {
            apiService.refreshSession.mockRejectedValueOnce({ status: 503 });
            apiService.refreshSession.mockResolvedValue();

            await expect(service.renew()).rejects.toBeTruthy();
            const result = await service.renew();

            expect(result).toBe(true);
            expect(apiService.refreshSession).toHaveBeenCalledTimes(2);
        });
    });

    describe('onSessionExpired', () => {
        it('should drop the session and go to sign-in', () => {
            service.onSessionExpired();

            expect(store.dispatch).toHaveBeenCalledWith(sessionActions.ended());
            expect(router.navigate).toHaveBeenCalledWith(['/sign-in']);
        });

        it.each(['/sign-in', '/login-by-email'])(
            'should not navigate again when the user is already on %s',
            (url) => {
                router.url = url;

                service.onSessionExpired();

                expect(store.dispatch).toHaveBeenCalledWith(sessionActions.ended());
                expect(router.navigate).not.toHaveBeenCalled();
            },
        );
    });

    describe('ensureAccess', () => {
        const lifetimeInMilliseconds = environment.session.accessTokenLifetimeInSeconds * 1000;
        const marginInMilliseconds = environment.session.renewalMarginInSeconds * 1000;
        let now: number;

        beforeEach(() => {
            now = 1_800_000_000_000;
            vi.spyOn(Date, 'now').mockImplementation(() => now);
        });

        afterEach(() => {
            vi.restoreAllMocks();
        });

        it('should not renew while the access token is surely valid', async () => {
            apiService.getProfile.mockResolvedValue({});
            await service.isSignedIn();
            now += lifetimeInMilliseconds - marginInMilliseconds - 1;

            const result = await service.ensureAccess();

            expect(result).toBe(true);
            expect(apiService.refreshSession).not.toHaveBeenCalled();
        });

        it('should renew when the access token may have expired', async () => {
            apiService.getProfile.mockResolvedValue({});
            apiService.refreshSession.mockResolvedValue();
            await service.isSignedIn();
            now += lifetimeInMilliseconds - marginInMilliseconds;

            const result = await service.ensureAccess();

            expect(result).toBe(true);
            expect(apiService.refreshSession).toHaveBeenCalledTimes(1);
        });

        it('should count a renewal as proof that the access token is valid', async () => {
            apiService.refreshSession.mockResolvedValue();
            await service.renew();

            await service.ensureAccess();

            expect(apiService.refreshSession).toHaveBeenCalledTimes(1);
        });

        it('should renew when nothing is known about the access token yet', async () => {
            apiService.refreshSession.mockResolvedValue();

            await service.ensureAccess();

            expect(apiService.refreshSession).toHaveBeenCalledTimes(1);
        });

        it('should return false when the refresh token is rejected', async () => {
            apiService.refreshSession.mockRejectedValue({ status: 401 });

            const result = await service.ensureAccess();

            expect(result).toBe(false);
        });

        it('should return true and not throw when the server cannot answer', async () => {
            apiService.refreshSession.mockRejectedValue({ status: 503 });

            const result = await service.ensureAccess();

            expect(result).toBe(true);
        });
    });

    describe('logout', () => {
        it('should call the API, clear local storage and notify subscribers', async () => {
            apiService.logout.mockResolvedValue();
            const listener = vi.fn();
            service.sessionLost.subscribe(listener);

            await service.logout();

            expect(apiService.logout).toHaveBeenCalledTimes(1);
            expect(window.localStorage.clear).toHaveBeenCalled();
            expect(listener).toHaveBeenCalledTimes(1);
            expect(await service.isSignedIn()).toBe(false);
        });

        it('should keep the state and rethrow when the API call fails', async () => {
            apiService.getProfile.mockResolvedValue({});
            await service.isSignedIn();
            apiService.logout.mockRejectedValue(new Error('revoke failed'));
            const listener = vi.fn();
            service.sessionLost.subscribe(listener);

            await expect(service.logout()).rejects.toThrow('revoke failed');

            expect(listener).not.toHaveBeenCalled();
            expect(await service.isSignedIn()).toBe(true);
        });
    });
});
