# Quickstart: verify short-lived sessions with silent renewal

Run the back end with `RunAll.ps1` and the Angular app with `npm start`; open `https://127.0.0.1:4200` and use DevTools (the Application and Network tabs). The checks R1-R9 map to the acceptance scenarios of [spec.md](spec.md). How the pieces fit together is drawn in [token-refresh-flow.md](token-refresh-flow.md).

## Setup: shorter lifetimes, only for this run

The defaults (a 15-minute access token and a 30-second grace period) are too slow to test. Change two files **temporarily and do not commit them**; undo them afterwards with `git checkout -- <file>`.

1. `backend/Applications/LetsTalk.Server.Authentication/appsettings.Development.json`: the `JwtSettings` section becomes

   ```json
   "JwtSettings": {
     "AccessTokenLifetime": "00:01:00",
     "BackgroundTokenLifetime": "1.00:00:00",
     "RefreshReuseGrace": "00:00:10"
   }
   ```

   The access token then lives 1 minute and a replaced refresh token still works for 10 seconds. The background token keeps its long Development lifetime. This file is read after the environment variables, so a variable would not override it.

2. `frontend/src/environments/environment.ts`: set `session.accessTokenLifetimeInSeconds` to `60` and `session.renewalMarginInSeconds` to `10`. The app assumes the lifetime of the back end, see plan D7.

Restart the Authentication service and the Angular dev server. Use the in-memory token cache or Redis; with the in-memory cache, restarting the Authentication service ends every session.

For the `curl` checks, `-k` accepts the development certificate. `<ACCESS>` and `<REFRESH>` are the values of the cookies `auth-token` and `auth-refresh` that you copy from DevTools (Application > Cookies; HttpOnly cookies are listed there).

| # | Steps | Expected | Covers |
|---|---|---|---|
| R1 | Sign in with email and code. Look at the cookies and at the `email-login` response. Then open any chat and look at the cookies sent with the `GET /api/Chat` request. | Two cookies. `auth-token`: `Path=/`, `HttpOnly`, `Secure`, `SameSite=Strict`, expires in about 1 minute. `auth-refresh`: `Path=/api/Authentication`, `HttpOnly`, `Secure`, `SameSite=Strict`, expires in about 30 days. The response body has no token. `localStorage` is empty. The `GET /api/Chat` request carries `auth-token` but **not** `auth-refresh`. | FR-002, SC-001 |
| R2 | Stay signed in, wait more than a minute, then click a chat or send a message. Watch the Network tab. | The action works without a sign-in page. You see `401`, then `POST /api/Authentication/refresh` answered `204`, then the same request again answered `200`. Both cookies have new values, and the `auth-refresh` expiry has moved to about 30 days from now. | AS1.1, FR-004, FR-005 |
| R3 | Open the app in two tabs. Wait more than a minute with both idle. Then act in both tabs within a few seconds. | Both tabs keep working and neither goes to the sign-in page. One `refresh` returns new cookies; the other either does the same or returns only a new `auth-token` (the refresh token was reused within the 10 seconds). The Authentication service log has no "already replaced" warning. | AS1.4, FR-005, FR-008 |
| R4 | Leave the app open for more than a minute and let a second account send you messages. Then stop the Notifications service, wait a few seconds, and start it again. | Messages keep arriving while the connection stays open (it is checked only when it is opened). After the restart the app reconnects on its own: in the Network tab the WebSocket is opened again and, before it, a `refresh` or a `401`-then-`refresh` renews the session. Messages arrive again. | AS3.1, AS3.2, FR-009 |
| R5 | Sign in. Copy `auth-refresh` as `<OLD>`. Wait for the app to refresh (R2), so the cookie changes. Wait at least 10 seconds more. Then replay the old value (below). | The replay returns `401` and clears both cookies in the response. The Authentication log has a warning "A refresh token that was already replaced was presented...", with a chain id and no token value. In the browser the next click sends you to the sign-in page, because the whole chain is gone. | AS2.2, FR-006, SC-003 |
| R6 | Sign in. Copy `auth-token` as `<ACCESS>`. Wait more than a minute. Then call the API and the file service with it (below). | Both answer "not authenticated": the API with `401`, the file service with `grpc-status: 16`. The notifications hub refuses an expired credential as well, see R4. | AS2.1, SC-001 |
| R7 | With valid cookies and without, send the requests below with a foreign and with an allowed origin. | Requests 1, 2 and 5 return `403`. Requests 3 and 4 return `400`: they passed the origin check and then failed validation of the empty body, which is the proof. No data is changed. | FR-010, SC-004, AS4.1-AS4.3 |
| R8 | Stop the image processing and the link preview consumers. Send a message with an image and a message with a new link. Wait more than a minute. Start the consumers. | The previews arrive. Before this change the job carried the user's access token and failed with `401` after it expired. | AS5.1, FR-012, SC-005 |
| R9 | Sign in. Copy `auth-refresh` as `<OLD>`. Click "log out". Then try to renew with the copy (below). | Both cookies are removed from the browser. The renewal returns `401` and the app shows the sign-in page. | AS2.3, FR-007 |

## Commands

**R5 and R9: replay a refresh token**

```
curl -sik -X POST https://127.0.0.1:5000/api/Authentication/refresh -H "Origin: https://127.0.0.1:4200" -H "Cookie: auth-refresh=<OLD>"
```

**R6: an expired access token**

```
# API
curl -sik https://127.0.0.1:5000/api/Profile -H "Authorization: Bearer <ACCESS>"

# file storage (gRPC-web, an empty request message): look for "grpc-status: 16" in the headers
printf '\0\0\0\0\0' | curl -sik --data-binary @- https://127.0.0.1:5003/file_upload.FileUploadGrpcEndpoint/DownloadImageAsync -H "content-type: application/grpc-web+proto" -H "x-grpc-web: 1" -H "authorization: <ACCESS>"
```

**R7: the origin check** (`generate-login-code` is used because it changes nothing: an empty body makes it answer `400`)

```
# 1. cookies and a foreign origin -> 403
curl -sik -X POST https://127.0.0.1:5000/api/Authentication/generate-login-code -H "Origin: https://evil.example" -H "Content-Type: application/json" -H "Cookie: auth-token=<ACCESS>; auth-refresh=<REFRESH>" -d "{}"

# 2. cookies, no Origin and no Referer -> 403
curl -sik -X POST https://127.0.0.1:5000/api/Authentication/generate-login-code -H "Content-Type: application/json" -H "Cookie: auth-token=<ACCESS>; auth-refresh=<REFRESH>" -d "{}"

# 3. cookies and the allowed origin -> 400
curl -sik -X POST https://127.0.0.1:5000/api/Authentication/generate-login-code -H "Origin: https://127.0.0.1:4200" -H "Content-Type: application/json" -H "Cookie: auth-token=<ACCESS>; auth-refresh=<REFRESH>" -d "{}"

# 4. an explicit Authorization header, no cookies, a foreign origin -> 400 (no browser session, so the check does not apply)
curl -sik -X POST https://127.0.0.1:5000/api/Authentication/generate-login-code -H "Origin: https://evil.example" -H "Content-Type: application/json" -H "Authorization: Bearer <ACCESS>" -d "{}"

# 5. only the refresh cookie and a foreign origin -> 403 (the refresh token is not used up)
curl -sik -X POST https://127.0.0.1:5000/api/Authentication/refresh -H "Origin: https://evil.example" -H "Cookie: auth-refresh=<REFRESH>"
```

Run request 4 and the access token checks within the minute after signing in, or the access token has expired.

## After the run

- Undo the temporary settings: `git checkout -- backend/Applications/LetsTalk.Server.Authentication/appsettings.Development.json frontend/src/environments/environment.ts`.
- Record the results of R1-R9 in the pull request description.
