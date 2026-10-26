# Implementation Plan: Short-lived sessions with silent renewal

**Spec**: [spec.md](spec.md) | **Builds on**: [001 plan](../001-httponly-cookie-auth/plan.md)
**Precondition**: spec 001 merged (cookie session, `AuthCookieSettings`, `JwtMiddleware` cookie resolution, `RevokeAsync`, credentialed CORS helper).

## Summary
Split the session into a short **access token** (JWT, default 15 min, cookie `auth-token`) and a rotating opaque **refresh token** (cookie `auth-refresh`, `Path=/api/Authentication`, HttpOnly). The Authentication service owns a refresh-token store (hashed, rotation, reuse detection, session chain). The API exposes `POST api/Authentication/refresh`. The Angular interceptor renews once on 401 and retries. Cookie-authenticated, state-changing requests are checked against the allowed origins. Background work receives a separate, longer-lived JWT instead of the user's access token.

## Technical Context
Same stack as spec 001. New server state: refresh records in the existing token cache backend (Redis or `IMemoryCache`, selected by the existing switch used by `RedisCacheTokenService` / `MemoryCacheTokenService`). No database-provider change, so constitution principle 3 is not affected.

## Constitution Check
| Principle | Status |
|---|---|
| 1 Security first | Pass. Refresh token HttpOnly, path-scoped, hashed at rest, rotated; reuse ends the chain; Origin check added. |
| 2 Generated code | Pass. Regenerate `angular.nswag` and `csharp.nswag` after adding `refresh`; never `nextjs.nswag`. |
| 3 Provider-neutral persistence | Pass. Uses the token cache abstraction only; both implementations (Redis, memory). |
| 4 Test-first | Pass (tasks order). |
| 5 Front-end conventions | Pass. |
| 6 Backward compatibility | Pass. Explicit-credential path remains (back-end callers, frontend-next). Browsers re-sign-in once (the cookie from 001 becomes a short token). |
| 7 Spec-driven | Pass. |

## Design Decisions

### D1. Configuration (FR-001, FR-003, FR-012, FR-013)
- **Model**: extend `JwtSettings` in `Infrastructure/Configuration/LetsTalk.Server.Configuration/Models/JwtSettings.cs` (now only `Key`).
  - Config section: `JwtSettings`, bound in the Authentication service (`AuthenticationServiceRegistration.cs:30`).
  - New settings with code defaults:
    - `AccessTokenLifetime` = 15 min
      - How long the access JWT (cookie `auth-token`) is valid. After this the request gets 401 and the app renews silently (FR-001, FR-004).
      - Also the `Expires` of the `auth-token` cookie (D4).
    - `BackgroundTokenLifetime` = 1 h
      - How long the separate JWT given to background work (image resize, link preview) is valid. It must outlive the queue delay (FR-012, SC-005).
      - It is not the user's access token and is never set as a cookie.
    - `RefreshIdleLifetime` = 30 d
      - How long a refresh token stays valid without being used. The period restarts on every rotation (sliding), so an active user is not signed out (FR-003, AS1.2, AS1.3).
      - Also the `Expires` of the `auth-refresh` cookie, capped by what remains of `RefreshAbsoluteLifetime` (D4).
    - `RefreshAbsoluteLifetime` = 90 d
      - Hard limit on a whole session chain, counted from the sign-in (`chainStartedAt`, D3). Activity does not extend it; when it is reached the user signs in again (FR-003).
    - `RefreshReuseGrace` = 30 s
      - After a refresh token is used, it is still accepted for this long and returns a new access token only (no second rotation). This keeps two tabs renewing at the same moment working (FR-005, AS1.4).
      - A used token presented after this period ends the whole chain (FR-006, D3).
- **Single owner**: only the Authentication service reads these values.
  - The API gets the resulting expiry times back from the gRPC calls (D3) and sets the cookie `Expires` from them (D4).
  - All expiry decisions are therefore made on one host (clock-skew risk).
- **Overrides**:
  - Development: `appsettings.Development.json` of the Authentication service sets long values (for example 1 day) so `frontend-next` can use a pasted credential (FR-013).
  - Production: supplied by the deployment (environment variables).
- **No secrets**: no secret values are committed.

### D2. Token generation takes a lifetime (FR-001, FR-012)
- `IJwtStorageService.Generate(accountId)` (`Services/Cache/Token/JwtStorageService.cs:60`) replaces `AddYears(1)` with `Generate(accountId, TimeSpan lifetime)`. Callers pass `AccessTokenLifetime` or `BackgroundTokenLifetime`.
- `authentication.proto` (two copies, see 001 D3): `GenerateJwtTokenRequest` gains `kind` enum `ACCESS | BACKGROUND`.

### D3. Refresh store and session chain (FR-002..FR-007, NFR-002)
New abstraction `IRefreshSessionService` in `Applications/LetsTalk.Server.Authentication/Abstractions`, with `RedisRefreshSessionService` and `MemoryRefreshSessionService` selected by the same switch as the token cache.
- Refresh token: 256-bit random, URL-safe; only `SHA-256(token)` is stored.
- Record (key = hash): `accountId`, `chainId`, `createdAt`, `chainStartedAt`, `usedAt?`, `replacedByHash?`; TTL = min(idle lifetime, absolute remaining).
- Chain index: `chain:<chainId>` -> current hash, so a chain can be ended in one operation.
- `IssueAsync(accountId)`: new chain, returns `{refreshToken, accessToken, expiries}`.
- `RotateAsync(refreshToken)`:
  1. Unknown/expired -> fail.
  2. Unused -> mark `usedAt`, create successor (same `chainId`, sliding TTL, cap at absolute lifetime), return new access + new refresh.
  3. Used and `now - usedAt <= RefreshReuseGrace` -> return a new **access token only** (no rotation; the browser already holds the successor from the first response via the shared cookie jar).
  4. Used after the grace period -> revoke the whole chain (`EndChainAsync`) and fail; log the event at Warning level with `chainId` (no token values).
- `EndChainAsync(chainId)` and `EndByRefreshTokenAsync(token)` used by logout.
- `gRPC` additions (both proto copies): `CreateSession(accountId) -> (accessToken, refreshToken, expiries)`, `RefreshSession(refreshToken) -> (accessToken, refreshToken?, expiries)`, `EndSession(refreshToken)`.
- Concurrency: Redis uses a Lua script / `StringSet(When.NotExists)` on the `usedAt` marker so two simultaneous rotations cannot both create successors; memory implementation uses a lock per hash.

### D4. API endpoints (FR-002, FR-004, FR-005, FR-007)
- `email-login`: calls `CreateSession`; sets both cookies (access: `Path=/`, refresh: `Path=/api/Authentication`, same Domain/Secure/SameSite=Strict, `Expires` = respective lifetime).
- `POST api/Authentication/refresh` (`[AllowAnonymous]`): relies on spec 001 FR-013 (an expired or invalid access cookie must not turn this call into a 401 before it runs); reads the refresh cookie only (never the header); on success sets new cookies, returns `204`; on failure clears both cookies and returns `401`.
- `logout` (from 001): additionally calls `EndSession` with the refresh cookie and clears both cookies.
- Cookie names/paths in `AuthCookieSettings` (add `RefreshName`, `RefreshPath`).

### D5. Background credential (FR-012, US5)
`CreateMessageCommandHandler.cs:98,107` passes `request.Token` to the launchers. Replace with a token from `IAuthenticationClient.GenerateBackgroundTokenAsync(accountId)` (kind `BACKGROUND`, lifetime 1 h). `MessageController` no longer needs `GetToken()` for this (keep method until no callers, then remove). Consumers and Lambdas are unchanged (still `Authorization: Bearer <token from message>`).

### D6. Origin check (FR-010, FR-011)
- New middleware `OriginCheckMiddleware` in `API/LetsTalk.Server.API.Middleware`. Pipeline order: `UseCors` -> `UseJwtMiddleware` -> `OriginCheckMiddleware` -> controllers, because it needs the flag set by `JwtMiddleware` (`Items["CredentialSource"]="Cookie"`). It applies when the credential came from the cookie and the method is not GET/HEAD/OPTIONS: require `Origin` (or, if absent, `Referer`'s origin) to be in `Cors:AllowedOrigins`, else `403`.
- The same check is applied in `NotificationHub.OnConnectedAsync` on the cookie path (`Context.GetHttpContext().Request.Headers.Origin`), because WebSockets are not governed by CORS; failure aborts the connection.
- Header-credential requests are exempt (no ambient credential).

### D7. Front end (FR-004, FR-008, FR-009, US1, US3)
- `interceptors/auth.interceptor.ts`: on 401 for our API (excluding `refresh`, `email-login`, `logout`), call `SessionService.renew()` and retry the request once; renewals are shared with a single in-flight observable (`shareReplay`/`BehaviorSubject` lock). A failed renewal -> clear state, navigate to `/sign-in`.
- `services/session.service.ts` (from 001): add `renew()` calling the generated `refresh()`; `isSignedIn()` stays the probe, now benefiting from renewal through the interceptor.
- `services/file-storage.service.ts`: gRPC-web errors with `UNAUTHENTICATED` -> `renew()` then retry once.
- `services/signalr.service.ts`: before each (re)start of the connection call `renew()` if the last renewal is older than the access lifetime minus a safety margin or the last connect failed with auth error; `onreconnected` unchanged (no token handling). `withAutomaticReconnect` custom policy calls `renew()` before the first retry.
- Proactive renewal is not added (renew-on-401 plus the SignalR rule is enough).

### D8. Development
`appsettings.Development.json` lifetimes extended; the cookie from DevTools (Application tab) is the credential to paste into `frontend-next` (HttpOnly cookies are visible there).

## Risks
| Risk | Mitigation |
|---|---|
| Rotation race between tabs logs the user out | 30 s grace (D3.3); single in-flight renewal per tab (D7); tests with concurrent calls |
| Memory mode loses sessions on restart | Documented; Redis mode is shared and persistent |
| A WebSocket stays open after the access token expires | Acceptable by design; the connection is bound at handshake. Revocation (logout) still removes `ConnectionId` mapping on disconnect; if immediate cut-off on logout is required, add a hub notification (not in scope) |
| Background JWT with 1 h lifetime is a bearer credential | Narrow lifetime; carried only in the broker message; same trust boundary as today (previously 1 year) |
| Clock skew between API and Authentication hosts | All expiry decisions are made inside the Authentication service |
| Origin check breaks legitimate non-browser callers using cookies | None exist; header-credential callers are exempt |
| Refresh path-scoping mismatch behind a proxy | `RefreshPath` is configuration; quickstart checks the cookie is sent only to `/api/Authentication/*` |

## Project Structure (touched files)
```
backend/
  Infrastructure/Configuration/LetsTalk.Server.Configuration/Models/{JwtSettings,AuthCookieSettings}.cs
  Applications/LetsTalk.Server.Authentication/{Protos,Abstractions/IRefreshSessionService.cs,Services/Session/*,Services/Cache/Token/JwtStorageService.cs,Services/*GrpcService.cs}  (+ Tests)
  Infrastructure/Authentication/LetsTalk.Server.Authentication.{Abstractions,Client}/*
  Applications/LetsTalk.Server.API/Controllers/AuthenticationController.cs, Program.cs (middleware order)
  API/LetsTalk.Server.API.Middleware/{JwtMiddleware,OriginCheckMiddleware,JwtMiddlewareExtension}.cs (+ Tests)
  API/LetsTalk.Server.API.Core/Features/Message/Commands/CreateMessage/CreateMessageCommandHandler.cs (+ Tests)
  Applications/LetsTalk.Server.Notifications/Hubs/NotificationHub.cs (+ Tests)
frontend/src/app/{interceptors/auth.interceptor.ts,services/{session,signalr,file-storage}.service.ts,api-client/api-client.ts}
```

## Complexity Tracking
Refresh rotation with reuse detection is more machinery than a plain longer-lived cookie, but it is the minimum that satisfies FR-005/FR-006 (a stolen refresh token cannot be used silently). No constitution violations.
