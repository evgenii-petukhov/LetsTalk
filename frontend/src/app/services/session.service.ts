import { inject, Injectable } from '@angular/core';
import { Router } from '@angular/router';
import { Store } from '@ngrx/store';
import { Subject } from 'rxjs';
import { environment } from '../../environments/environment';
import { sessionActions } from '../state/session/session.actions';
import { ApiService } from './api.service';

const SIGN_IN_URLS = ['/sign-in', '/login-by-email'];

// "Signed in" is decided by the server (the session cookie is not readable by scripts).
// The probe calls ApiService.getProfile directly: StoreService.getLoggedInUser returns the
// NgRx-cached profile without asking the server.
@Injectable({
    providedIn: 'root',
})
export class SessionService {
    private readonly sessionLostSubject = new Subject<void>();
    // subscribers can listen, only this service can emit
    readonly sessionLost = this.sessionLostSubject.asObservable();

    private readonly apiService = inject(ApiService);
    private readonly store = inject(Store);
    private readonly router = inject(Router);
    private signedIn: boolean | null = null;
    private probe: Promise<boolean> | null = null;
    private renewal: Promise<boolean> | null = null;
    // when the server last confirmed the access token (a successful probe or renewal)
    private lastConfirmedAt: number | null = null;

    isSignedIn(): Promise<boolean> {
        return this.signedIn === null
            ? this.refresh()
            : Promise.resolve(this.signedIn);
    }

    // Asks the server again, whatever was cached; concurrent callers share one request.
    // Only a 401 means "not signed in": any other failure (a server outage, the network) keeps
    // what is known, so an outage never looks like a sign-out.
    refresh(): Promise<boolean> {
        this.probe ??= this.apiService.getProfile().then(
            () => this.finishProbe(true),
            (error) => this.finishProbe(error?.status === 401 ? false : this.signedIn),
        );
        return this.probe;
    }

    // Exchanges the refresh cookie for a new access token; concurrent callers share one request.
    // Resolves false when the server rejects the refresh token: the session is over, the state
    // is cleared and the user is on the sign-in page. Rejects when the server cannot answer, which
    // is not a sign-out.
    renew(): Promise<boolean> {
        this.renewal ??= this.apiService.refreshSession().then(
            () => {
                this.renewal = null;
                this.signedIn = true;
                this.lastConfirmedAt = Date.now();
                return true;
            },
            (error) => {
                this.renewal = null;
                if (error?.status !== 401) {
                    throw error;
                }

                this.onSessionExpired();
                return false;
            },
        );
        return this.renewal;
    }

    // For connections that are opened outside the HTTP interceptor (the live notifications): renews
    // when the access token may have expired. Resolves false only when the session is over.
    async ensureAccess(): Promise<boolean> {
        if (!this.accessMayHaveExpired()) {
            return true;
        }

        try {
            return await this.renew();
        } catch {
            // the server cannot answer: the caller tries anyway and finds out
            return true;
        }
    }

    onSessionLost(): void {
        this.signedIn = false;
        this.lastConfirmedAt = null;
        this.store.dispatch(sessionActions.ended());
        this.sessionLostSubject.next();
    }

    // The server no longer accepts the session: forget it and show the sign-in page
    onSessionExpired(): void {
        this.onSessionLost();

        if (!SIGN_IN_URLS.some((url) => this.router.url.startsWith(url))) {
            this.router.navigate(['/sign-in']);
        }
    }

    async logout(): Promise<void> {
        await this.apiService.logout();
        window.localStorage.clear();
        this.onSessionLost();
    }

    reloadPage(): void {
        window.location.reload();
    }

    private accessMayHaveExpired(): boolean {
        const { accessTokenLifetimeInSeconds, renewalMarginInSeconds } =
            environment.session;

        return (
            this.lastConfirmedAt === null ||
            Date.now() - this.lastConfirmedAt >=
                (accessTokenLifetimeInSeconds - renewalMarginInSeconds) * 1000
        );
    }

    private finishProbe(result: boolean | null): boolean {
        this.signedIn = result;
        this.probe = null;
        if (result) {
            this.lastConfirmedAt = Date.now();
        }

        return result ?? false;
    }
}
