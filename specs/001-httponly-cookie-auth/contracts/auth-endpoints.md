# Contract: authentication endpoints (API)

Base path `api/Authentication`. The OpenAPI document is generated from the controller; regenerate `nswag/angular.nswag` and `nswag/csharp.nswag` after the change. **Do not regenerate `nswag/nextjs.nswag`.**

## POST `api/Authentication/email-login` (changed)
- Request: unchanged (`EmailLoginRequest`: `email`, `code`, `antiSpamToken`).
- Success `200`: body `LoginResponseDto { success: true }`. The `token` property is **removed** from the DTO.
  - Header `Set-Cookie`: session cookie, see `cookie-and-cors.md`.
- Failure: unchanged (`400` validation, or `200` with `success: false` as today). No cookie is set.
- Replaces any existing session cookie of that browser.

## POST `api/Authentication/logout` (new)
- Auth: the session cookie or the `Authorization` header. If no valid credential is presented, the call still returns `204` (idempotent) and clears the cookie.
- Effect:
  1. Resolve the credential presented (cookie first, then header).
  2. If valid, revoke it in the Authentication service (denylist until its natural expiry).
  3. Respond with an expired `Set-Cookie` of the same name, domain and path.
- Success: `204 No Content`.
- Anonymous-allowed (`[AllowAnonymous]`) so a browser with an expired session can still clear its cookie.

## Credential resolution order (all HTTP entry points)
1. Cookie `auth-token`.
2. `Authorization` header (last space-separated part, as today).
If both are present and differ, the cookie wins (FR-004).

### Invalid credentials (FR-013)
- **Cookie present but invalid** (expired, revoked, unknown, malformed): treated as *no credential*. The request continues anonymously, the response carries an expired `Set-Cookie` that removes it, and protected endpoints answer `401` through the `[Authorize]` filter. Public endpoints (`email-login`, `generate-login-code`, `logout`) work normally.
- **Header credential present but invalid** (no cookie): unchanged, `401` (explicit callers want a hard failure).
- If a valid cookie and an invalid header are both present, the cookie is used.

## Session probe (existing endpoint, no change)
The profile call behind `StoreService.getLoggedInUser()` -> `ApiService.getProfile()` (`frontend/src/app/services/store.service.ts:124`, `api.service.ts`). `200` = signed in; `401` = not signed in. No new endpoint is needed. Note that `getLoggedInUser()` returns the NgRx-cached profile if present, so the probe must bypass that cache once at startup and after sign-in (FR-006, FR-014).

## Authentication gRPC (internal, `authentication.proto`) (changed)
Add to `JwtTokenGrpcService`:
```proto
rpc RevokeJwtToken (RevokeJwtTokenRequest) returns (RevokeJwtTokenResponse);
message RevokeJwtTokenRequest { string token = 1; }
message RevokeJwtTokenResponse {}
```
- `ValidateJwtToken` semantics: returns no account id for a revoked token.
- The `.proto` exists in two places and both must be updated identically:
  `Applications/LetsTalk.Server.Authentication/Protos/authentication.proto` and
  `Infrastructure/Authentication/LetsTalk.Server.Authentication.Client/Protos/authentication.proto`.
- `IAuthenticationClient` gets `RevokeJwtTokenAsync(string token)`.
