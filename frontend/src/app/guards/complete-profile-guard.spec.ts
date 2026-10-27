import { beforeEach, describe, expect, it, vi, type MockedObject } from 'vitest';
import { TestBed } from '@angular/core/testing';
import { ActivatedRouteSnapshot } from '@angular/router';
import { completeProfileGuard } from './complete-profile-guard';
import { StoreService } from '../services/store.service';
import { SessionService } from '../services/session.service';

vi.mock('@angular/router', async (importOriginal) => ({
    ...(await importOriginal<typeof import('@angular/router')>()),
    createUrlTreeFromSnapshot: vi.fn((_snapshot, commands: string[]) => ({
        redirectTo: commands,
    })),
}));

describe('completeProfileGuard', () => {
    let storeService: MockedObject<StoreService>;
    let sessionService: MockedObject<SessionService>;
    const route = {} as ActivatedRouteSnapshot;

    beforeEach(() => {
        vi.clearAllMocks();
        storeService = {
            getLoggedInUser: vi.fn().mockName('StoreService.getLoggedInUser'),
        } as unknown as MockedObject<StoreService>;
        sessionService = {
            onSessionLost: vi.fn().mockName('SessionService.onSessionLost'),
        } as unknown as MockedObject<SessionService>;
        TestBed.configureTestingModule({
            providers: [
                { provide: StoreService, useValue: storeService },
                { provide: SessionService, useValue: sessionService },
            ],
        });
    });

    it('should allow navigation when the profile is complete', async () => {
        storeService.getLoggedInUser.mockResolvedValue({
            firstName: 'Ann',
            lastName: 'Lee',
        } as never);

        const result = await TestBed.runInInjectionContext(() =>
            completeProfileGuard(route),
        );

        expect(result).toBe(true);
    });

    it('should redirect to the profile when the name is missing', async () => {
        storeService.getLoggedInUser.mockResolvedValue({
            firstName: 'Ann',
        } as never);

        const result = await TestBed.runInInjectionContext(() =>
            completeProfileGuard(route),
        );

        expect(result).toEqual({ redirectTo: ['/', 'profile'] });
    });

    it('should drop the session and redirect to sign-in when the profile cannot be loaded', async () => {
        storeService.getLoggedInUser.mockRejectedValue({ status: 401 });

        const result = await TestBed.runInInjectionContext(() =>
            completeProfileGuard(route),
        );

        expect(result).toEqual({ redirectTo: ['/', 'sign-in'] });
        expect(sessionService.onSessionLost).toHaveBeenCalled();
        expect(window.localStorage.clear).not.toHaveBeenCalled();
    });
});
