import { inject } from '@angular/core';
import {
    ActivatedRouteSnapshot,
    createUrlTreeFromSnapshot,
} from '@angular/router';
import { StoreService } from '../services/store.service';

// https://blog.herodevs.com/functional-router-guards-in-angular-15-open-the-door-to-happier-code-4a53bb60f78a
export const completeProfileGuard = async (next: ActivatedRouteSnapshot) => {
    const storeService = inject(StoreService);

    try {
        const account = await storeService.getLoggedInUser();
        if (account) {
            return !!account?.firstName && !!account?.lastName
                ? true
                : createUrlTreeFromSnapshot(next, ['/', 'profile']);
        } else {
            window.localStorage.clear();
            return createUrlTreeFromSnapshot(next, ['/', 'sign-in']);
        }
    } catch {
        window.localStorage.clear();
        return createUrlTreeFromSnapshot(next, ['/', 'sign-in']);
    }
};
