import { beforeEach, describe, expect, it, vi, type MockedObject } from 'vitest';
import { TestBed } from '@angular/core/testing';
import { ActivatedRouteSnapshot } from '@angular/router';
import { anonymousOnlyGuard } from './anonymous-only-guard';
import { SessionService } from '../services/session.service';

vi.mock('@angular/router', async (importOriginal) => ({
    ...(await importOriginal<typeof import('@angular/router')>()),
    createUrlTreeFromSnapshot: vi.fn((_snapshot, commands: string[]) => ({
        redirectTo: commands,
    })),
}));

describe('anonymousOnlyGuard', () => {
    let sessionService: MockedObject<SessionService>;
    const route = {} as ActivatedRouteSnapshot;

    beforeEach(() => {
        sessionService = {
            isSignedIn: vi.fn().mockName('SessionService.isSignedIn'),
        } as unknown as MockedObject<SessionService>;
        TestBed.configureTestingModule({
            providers: [{ provide: SessionService, useValue: sessionService }],
        });
    });

    it('should allow navigation when the user is not signed in', async () => {
        sessionService.isSignedIn.mockResolvedValue(false);

        const result = await TestBed.runInInjectionContext(() =>
            anonymousOnlyGuard(route),
        );

        expect(result).toBe(true);
    });

    it('should redirect to the chats when the user is signed in', async () => {
        sessionService.isSignedIn.mockResolvedValue(true);

        const result = await TestBed.runInInjectionContext(() =>
            anonymousOnlyGuard(route),
        );

        expect(result).toEqual({ redirectTo: ['/', 'chats'] });
    });
});
