import { inject } from '@angular/core';
import {
    ActivatedRouteSnapshot,
    createUrlTreeFromSnapshot,
} from '@angular/router';
import { SessionService } from '../services/session.service';

// https://blog.herodevs.com/functional-router-guards-in-angular-15-open-the-door-to-happier-code-4a53bb60f78a
export const anonymousOnlyGuard = async (next: ActivatedRouteSnapshot) =>
    (await inject(SessionService).isSignedIn())
        ? createUrlTreeFromSnapshot(next, ['/', 'chats'])
        : true;
