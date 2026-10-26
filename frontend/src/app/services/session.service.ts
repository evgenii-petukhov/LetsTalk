import { inject, Injectable } from '@angular/core';
import { Store } from '@ngrx/store';
import { Subject } from 'rxjs';
import { sessionActions } from '../state/session/session.actions';
import { ApiService } from './api.service';

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
    private signedIn: boolean | null = null;
    private probe: Promise<boolean> | null = null;

    isSignedIn(): Promise<boolean> {
        return this.signedIn === null
            ? this.refresh()
            : Promise.resolve(this.signedIn);
    }

    // Asks the server again, whatever was cached; concurrent callers share one request
    refresh(): Promise<boolean> {
        this.probe ??= this.apiService.getProfile().then(
            () => this.finishProbe(true),
            () => this.finishProbe(false),
        );
        return this.probe;
    }

    onSessionLost(): void {
        this.signedIn = false;
        this.store.dispatch(sessionActions.ended());
        this.sessionLostSubject.next();
    }

    async logout(): Promise<void> {
        await this.apiService.logout();
        window.localStorage.clear();
        this.onSessionLost();
    }

    reloadPage(): void {
        window.location.reload();
    }

    private finishProbe(result: boolean): boolean {
        this.signedIn = result;
        this.probe = null;
        return result;
    }
}
