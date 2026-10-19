import { ApiClient } from "./api-client";

export const TOKEN_KEY = "auth-token";

export const getToken = () => window.localStorage.getItem(TOKEN_KEY);

const authFetch = (url: RequestInfo, init?: RequestInit) => {
    const headers = new Headers(init?.headers);
    const token = getToken();
    if (token) headers.set("Authorization", `Bearer ${token}`);
    return window.fetch(url, { ...init, headers });
};

export const api = new ApiClient("/api/proxy", { fetch: authFetch });
