# Quickstart: verify the HttpOnly cookie session

Run the back end with `RunAll.ps1` and the Angular app with `ng serve --host 127.0.0.1`; open `https://127.0.0.1:4200` (`ng serve --ssl`; plain `http://` also works if the CORS origin and `AuthCookie:Secure` are adjusted). Use DevTools (Application and Network tabs).

| # | Steps | Expected | Covers |
|---|---|---|---|
| Q1 | Sign in with email + code | Landed on chats; cookie `auth-token` is `HttpOnly`, `SameSite=Strict`; `localStorage`/`sessionStorage` have no `auth-token`; `document.cookie` does not list it; the `email-login` response body has no `token` | AS1.1, AS1.2, FR-001, FR-002, SC-001 |
| Q2 | Enter a wrong code | Stay on sign-in; no `Set-Cookie` | AS1.3 |
| Q3 | Reload; open a second tab | Still signed in | AS2.1 |
| Q4 | Signed out: open `/chats`; signed in: open `/sign-in` | Redirect to sign-in / to chats | AS2.2, AS2.3, FR-006 |
| Q5 | Send a message from another account | Appears live; WS request to notifications carries the cookie | AS3.1, FR-010 |
| Q6 | Send an image and a link | Previews arrive (background services used the explicit credential) | AS3.4, FR-012 |
| Q7 | Upload/view an avatar or image | Works; gRPC-web request has no `authorization` header, cookie sent | AS3.2, FR-003 |
| Q8 | Stop and restart the notifications service | Messages resume after reconnect without re-login | AS3.3 |
| Q9 | Copy the cookie value, click "log out", replay the value as `Authorization: Bearer` against the API, file storage and notifications | All reject (401 / not registered) | AS4.1, AS4.2, FR-005, SC-002 |
| Q10 | Sign in, then in DevTools delete the cookie, click a chat | Returned to sign-in, no broken screen | AS4.3, FR-008 |
| Q11 | Before updating: `localStorage['auth-token']='x'`; after loading the new build | Entry removed; sign-in page shown | AS5.1, FR-007 |
| Q12 | `frontend-next`: set `localStorage['auth-token']` to a valid JWT; load chats | Works as before | AS5.2, FR-004, SC-004 |
| Q14 | Block cookies for the site in the browser settings, then sign in | Message that the session could not be kept; no redirect loop | AS1.5, FR-014 |
| Q15 | Sign in, log out in another tab or revoke the token (stale cookie remains), then request a login code and sign in again; also call `POST logout` with the stale cookie | Each succeeds; the stale `auth-token` is replaced or removed by `Set-Cookie`; a protected call with the stale cookie returns 401 | AS1.4, FR-013 |
| Q13 | Request the API from another origin with credentials (e.g. `curl -H "Origin: https://evil.example" -i`) | No `Access-Control-Allow-Origin` for that origin | FR-009 |
