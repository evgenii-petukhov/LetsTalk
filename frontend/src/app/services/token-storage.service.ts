import { Injectable } from '@angular/core';

const LEGACY_TOKEN_KEY = 'auth-token';

// The session credential is an HttpOnly cookie now and is not readable by scripts.
// Earlier versions kept the token in localStorage, so it is removed on startup.
@Injectable({
    providedIn: 'root',
})
export class TokenStorageService {
    clearLegacyToken(): void {
        try {
            window.localStorage.removeItem(LEGACY_TOKEN_KEY);
        } catch {
            // localStorage can be blocked by the browser; there is nothing to clean up then
        }
    }
}
