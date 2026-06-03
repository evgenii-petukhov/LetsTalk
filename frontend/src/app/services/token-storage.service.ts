import { Injectable } from '@angular/core';

const TOKEN_KEY = 'auth-token';

@Injectable({
    providedIn: 'root',
})
export class TokenStorageService {
    saveToken(token: string) {
        window.localStorage.setItem(TOKEN_KEY, token);
    }

    getToken(): string {
        return localStorage.getItem(TOKEN_KEY);
    }

    isLoggedIn(): boolean {
        return !!this.getToken();
    }
}
