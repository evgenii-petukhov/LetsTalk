import { inject } from '@angular/core';
import {
    ActivatedRouteSnapshot,
    createUrlTreeFromSnapshot,
} from '@angular/router';
import { SessionService } from '../services/session.service';
import { StoreService } from '../services/store.service';

// https://blog.herodevs.com/functional-router-guards-in-angular-15-open-the-door-to-happier-code-4a53bb60f78a
export const completeProfileGuard = async (next: ActivatedRouteSnapshot) => {
    const storeService = inject(StoreService);
    const sessionService = inject(SessionService);

    try {
        const account = await storeService.getLoggedInUser();
        if (account) {
            return !!account?.firstName && !!account?.lastName
                ? true
                : createUrlTreeFromSnapshot(next, ['/', 'profile']);
        } else {
            sessionService.onSessionLost();
            return createUrlTreeFromSnapshot(next, ['/', 'sign-in']);
        }
    } catch {
        sessionService.onSessionLost();
        return createUrlTreeFromSnapshot(next, ['/', 'sign-in']);
    }
};
