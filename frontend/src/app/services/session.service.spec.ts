import { beforeEach, describe, expect, it, vi, type MockedObject } from 'vitest';
import { TestBed } from '@angular/core/testing';
import { Store } from '@ngrx/store';
import { SessionService } from './session.service';
import { ApiService } from './api.service';
import { sessionActions } from '../state/session/session.actions';

describe('SessionService', () => {
    let service: SessionService;
    let apiService: MockedObject<ApiService>;
    let store: { dispatch: ReturnType<typeof vi.fn> };

    beforeEach(() => {
        vi.clearAllMocks();
        apiService = {
            getProfile: vi.fn().mockName('ApiService.getProfile'),
            logout: vi.fn().mockName('ApiService.logout'),
        } as unknown as MockedObject<ApiService>;

        store = { dispatch: vi.fn().mockName('Store.dispatch') };

        TestBed.configureTestingModule({
            providers: [
                SessionService,
                { provide: ApiService, useValue: apiService },
                { provide: Store, useValue: store },
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
