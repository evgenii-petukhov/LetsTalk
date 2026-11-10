# Tasks: Short-lived sessions with silent renewal

**Inputs**: [spec.md](spec.md), [plan.md](plan.md).\
**Precondition**: all of `specs/001-httponly-cookie-auth/tasks.md` is done and merged.\
**Conventions**: as in spec 001 (`[P]` parallel, tests before code, `Method_WhenCondition_ShouldOutcome`). `BE` = `backend/`, `FE` = `frontend/src/app/`.\
**Commit message prefix: `Token refresh: `** (one line, for example `Token refresh: rotate refresh tokens with reuse detection`).\
**One commit per phase**, made only when the owner approves, with the ticked `tasks.md` included.

## Phase 0 - Setup
- [x] **T100** (branch created from `master` at `b1ac411a`; the working tree was clean, nothing staged) Branch `002-short-lived-token-refresh` from the updated `master`. Re-check that uncommitted `Program.cs`/`appsettings.json` changes are not staged (constitution 1).
- [x] **T101** (defaults live in code, as in spec 001 T002, so no `appsettings.json` is edited: the API one holds the owner's secrets and never reads these values, and the Authentication one holds the signing key. New tests: `JwtSettingsTests`; `AuthCookieSettingsTests` extended; `AuthCookieSettingsRegistrationTests` extended, because `AddAuthCookieSettings` now also rejects an empty or duplicate `RefreshName` and a `RefreshPath` that is not an absolute path; `Configuration.Tests`: 40 pass) [P] Extend `JwtSettings` (`AccessTokenLifetime`, `BackgroundTokenLifetime`, `RefreshIdleLifetime`, `RefreshAbsoluteLifetime`, `RefreshReuseGrace`) and `AuthCookieSettings` (`RefreshName`, `RefreshPath`); defaults per plan D1; test binding in `LetsTalk.Server.Configuration.Tests`; long lifetimes (1 day for the access and background tokens) in the Authentication service's `appsettings.Development.json`. (FR-001, FR-003, FR-013)

## Phase 1 - Authentication service
Tests first, in `BE/Applications/LetsTalk.Server.Authentication.Tests`:
- [x] **T110** (new `JwtStorageServiceTests`; the access vs background difference is only the lifetime argument, chosen in the gRPC service, see T113) `JwtStorageService`: `Generate_WhenLifetimeGiven_ShouldSetExpiry`; access vs background lifetime. (FR-001, FR-012)
- [x] **T111** (one shared scenario base class `RefreshSessionServiceTestsBase` runs the same tests against both stores; the memory store is checked through a recording `IMemoryCache`; extra cases: sliding idle period, another chain untouched, unknown token, warning log without token values) [P] `MemoryRefreshSessionService` tests: `IssueAsync_ShouldStoreOnlyHash`; `RotateAsync_WhenUnused_ShouldIssueNewPair`; `RotateAsync_WhenUsedWithinGrace_ShouldIssueAccessOnly`; `RotateAsync_WhenUsedAfterGrace_ShouldEndChainAndFail`; `RotateAsync_WhenIdleExpired_ShouldFail`; `RotateAsync_WhenAbsoluteLifetimeReached_ShouldFail`; `RotateAsync_WhenConcurrent_ShouldCreateSingleSuccessor`; `EndByRefreshTokenAsync_ShouldInvalidateChain`. (FR-003, FR-005, FR-006, FR-007, NFR-002)
- [x] **T112** (`RedisRefreshSessionServiceTests` runs the shared scenarios on a dictionary-backed fake of the few `IDatabase` commands used, with an atomic `SET NX`; extra: the used marker is written with `When.NotExists`, every key gets an expiry) [P] Same for `RedisRefreshSessionService` (mocked `IDatabase`), including the atomic `usedAt` marker.
- [x] **T113** (also `GenerateJwtToken` with the default kind and `RefreshSession_WhenRefreshTokenRejected_ShouldThrowUnauthenticated`) [P] gRPC service tests: `CreateSession`, `RefreshSession`, `EndSession`, `GenerateJwtToken_WhenKindBackground_ShouldUseBackgroundLifetime`.

Implementation:
- [x] **T114** (both copies identical; `TokenKind` enum `ACCESS|BACKGROUND`; one `SessionResponse` message for `CreateSession` and `RefreshSession`, with an empty `refresh_token` and expiry 0 for the access-only case; no `protoc` step, `Grpc.Tools` generates on build) Update both `.proto` copies (`kind` enum; `CreateSession`, `RefreshSession`, `EndSession`). (depends T101)
- [x] **T115** (`IJwtCacheService.GenerateAsync(accountId, lifetime)` as well; the existing Redis token test was updated) `JwtStorageService.Generate(accountId, lifetime)` and callers (`MemoryCacheTokenService`, `RedisCacheTokenService`). (T110)
- [x] **T116** (deviations from plan D3, now recorded there: the rules live once in `RefreshSessionServiceBase` and each store only supplies primitives; the record holds `AccountId`, `ChainId`, `ChainStartedAt`, `ExpiresAt`; the used marker is a separate key, set atomically, instead of `usedAt`/`replacedByHash` fields; memory mode uses one static lock instead of a lock per hash; `TimeProvider` is injected for testing; registered scoped like the token services) `IRefreshSessionService` + `MemoryRefreshSessionService` + `RedisRefreshSessionService`, DI registration next to the existing token-cache switch in `AuthenticationServiceRegistration.cs`. (T111, T112)
- [x] **T117** (`RefreshSession` throws `RpcException(Unauthenticated)` when the token is rejected) gRPC service methods. (T113, T114, T116)
- [x] **T118** (new project `Infrastructure/Authentication/LetsTalk.Server.Authentication.Client.Tests`, added to the solution by hand, 8 tests; `AuthSession` record in the Abstractions project; `RefreshSessionAsync` returns null only for `Unauthenticated`, an outage throws) `IAuthenticationClient`/`AuthenticationClient`: `CreateSessionAsync`, `RefreshSessionAsync`, `EndSessionAsync`, `GenerateBackgroundTokenAsync`. (T114)
- [x] **T119** (2026-11-09: Authentication.Tests 57 pass, Authentication.Client.Tests 8 pass, `dotnet build` of the solution 0 errors) `dotnet test BE/Applications/LetsTalk.Server.Authentication.Tests`. Checkpoint 1.

## Phase 2 - API
Tests first:
- [x] **T120** (also `RefreshAsync_WhenOnlyAccessTokenIssued_ShouldKeepTheRefreshCookie`, `..._WhenNoRefreshCookie_...`, `..._WhenAuthenticationServiceUnavailable_ShouldThrowServiceUnavailableAndKeepCookies` and logout cases for refresh-cookie-only; new `RefreshSessionCommandHandlerTests`; `EmailLoginCommandHandlerTests` and `LogoutCommandHandlerTests` updated; the old `ApiControllerTests` were deleted together with `GetToken()`, see T127) `AuthenticationController` tests (project from spec 001 T022): `EmailLoginAsync_WhenSuccess_ShouldSetAccessAndRefreshCookies` (refresh cookie `Path` scoped, HttpOnly); `RefreshAsync_WhenValid_ShouldSetNewCookiesAndReturnNoContent`; `RefreshAsync_WhenInvalid_ShouldClearCookiesAndReturn401`; `RefreshAsync_WhenOnlyAuthorizationHeader_ShouldReturn401`; `LogoutAsync_ShouldEndSessionAndClearBothCookies`. (FR-002, FR-004, FR-007)
- [x] **T121** (plus cases for the refresh cookie alone and for a foreign `Referer`, safe methods, letter case; the refresh cookie counts as a browser session, see plan D6) [P] `OriginCheckMiddlewareTests` in `API.Middleware.Tests`: `InvokeAsync_WhenCookieAndPostAndForeignOrigin_ShouldReturn403`; `..._WhenCookieAndPostAndAllowedOrigin_ShouldContinue`; `..._WhenCookieAndNoOriginButAllowedReferer_ShouldContinue`; `..._WhenCookieAndGet_ShouldContinue`; `..._WhenHeaderCredentialAndForeignOrigin_ShouldContinue`; `..._WhenCookieAndNoOriginNoReferer_ShouldReturn403`. (FR-010)
- [x] **T122** (extended the existing `JwtMiddlewareTests`; a rejected cookie or header sets no credential source) [P] `JwtMiddlewareTests`: sets `Items["CredentialSource"]` to `Cookie` or `Header`. 
- [x] **T123** (plus `Handle_WhenNoImageAndNoNewLink_ShouldNotRequestBackgroundToken` and `Handle_WhenBackgroundTokenCannotBeIssued_ShouldNotCreateTheMessage`) [P] `CreateMessageCommandHandlerTests`: `Handle_WhenImageOrLink_ShouldPassBackgroundTokenToLaunchers` (not the user's access token). (FR-012, AS5.1)

Implementation:
- [x] **T124** (new `refresh` endpoint, `RefreshSessionCommand` and handler, `SessionResult` replaces `EmailLoginResult`, `LogoutCommand(Token, RefreshToken)` ends the chain first and then revokes the token; an Authentication outage on `refresh` answers 503 and keeps the cookies; new `AppendRefreshCookie`/`DeleteRefreshCookie` and `RpcExceptionExtensions` shared with `JwtMiddleware`) `AuthenticationController`: login with `CreateSession`, new `refresh`, extended `logout`. (T118, T120)
- [x] **T125** (`CredentialSources` holds the key and the two values) `JwtMiddleware` credential source flag. (T122)
- [x] **T126** (deviation: registered inside `UseJwtMiddleware()` instead of `Program.cs`, which holds the owner's uncommitted changes and must not be modified; one place, so it cannot be forgotten) `OriginCheckMiddleware` + extension, register in `Program.cs` after `UseJwtMiddleware` and before `MapControllers`; add to the pipeline tests if any. (T121, T125)
- [x] **T127** (the handler requests the token before saving anything and only when an image or a new link needs it; `CreateMessageCommand.Token`, `ApiController.GetToken()` and the `MessageController` line are removed, and with them the `GetToken_*` tests) `CreateMessageCommandHandler`: mint background token through `IAuthenticationClient`; remove the now unused `Token` use from `MessageController`/`ApiController.GetToken()` if no callers remain. (T118, T123)
- [x] **T128** (2026-11-09: Middleware.Tests 41, Core.Tests 311, API.Tests 15 pass; `dotnet build` of the solution 0 errors; `backend/CheckWarnings.ps1` 0 warnings in the changed files) `dotnet test` for API.Core.Tests, API.Middleware.Tests, API tests. Checkpoint 2.

## Phase 3 - Notifications
- [x] **T130** (plus `..._WhenCookieAndNoOrigin_ShouldAbort`, allowed origin with a trailing slash or other letter case, and `..._WhenNoCookieAndForeignOrigin_ShouldNotAbort` for the explicit-credential path; the existing cookie tests now send an allowed `Origin`) `NotificationHub` tests: `OnConnectedAsync_WhenCookieAndForeignOrigin_ShouldAbort`; `..._WhenCookieAndAllowedOrigin_ShouldRegister`; `..._WhenExpiredAccessCookie_ShouldAbort`. (FR-011, AS3.2)
- [x] **T131** (the hub uses the shared `AllowedOrigins` helper, which also replaced the list the API's `OriginCheckMiddleware` read on its own: new class in `Infrastructure/LetsTalk.Server.Cors` with `AllowedOriginsTests`, registered by `AddCredentialedCors`, and the Middleware project now references the Cors project; the check runs before the token is validated, so a foreign page cannot even probe the session) Implement the origin check in `NotificationHub.OnConnectedAsync` using `Cors:AllowedOrigins`. (T130)
- [x] **T132** (2026-11-10: Notifications.Tests 21, Cors.Tests 15 and Middleware.Tests 41 pass; `dotnet build` of the solution 0 errors; `backend/CheckWarnings.ps1` 0 warnings in the changed files) `dotnet test` Notifications.Tests. Checkpoint 3.

## Phase 4 - Clients
- [x] **T140** (done by the owner on 2026-11-10; verified: `refresh()` exists in `api-client.ts` and `RefreshAsync` in `ApiClient.cs`, they contain only added lines (47 and 73), and the two `.nswag` files differ only in the stored spec snapshot; `nextjs.nswag` was not run) Regenerate `FE/api-client/api-client.ts` (`nswag/angular.nswag`) and `BE/API/LetsTalk.Server.API.Client/ApiClient.cs` (`nswag/csharp.nswag`). **Not** `nextjs.nswag`. Confirm `refresh()` exists. (T124)
- [x] **T141** (2026-11-10: `dotnet build` of the solution 0 errors, 5 older warnings in files this spec does not touch; `backend/CheckWarnings.ps1` 0 warnings in the changed files) `dotnet build BE/LetsTalk.Server.sln`. Checkpoint 4.

## Phase 5 - Angular
Tests first:
- [x] **T150** (renewal and expiry are in `SessionService`: `renew()` returns `false` after a rejected refresh token, having cleared the state and opened the sign-in page through the new `onSessionExpired()`; `ApiService.refreshSession()` wraps the generated `refresh()`; also specs for `ensureAccess` and `onSessionExpired`) `FE/services/session.service.spec.ts`: `renew_WhenCalledConcurrently_ShouldCallApiOnce`; `renew_WhenRefreshFails_ShouldClearStateAndGoToSignIn`. (FR-004, FR-008)
- [x] **T151** (spec rewritten; the parallel case runs against the real `SessionService`, so it checks that the API is called once; the three authentication calls `refresh`, `email-login` and `logout` are never renewed) [P] `FE/interceptors/auth.interceptor.spec.ts`: `intercept_When401_ShouldRenewAndRetryOnce`; `..._WhenRetryAlso401_ShouldGoToSignIn`; `..._WhenRequestIsRefreshOrLogin_ShouldNotRenew`; `..._WhenParallel401s_ShouldRenewOnce`. (AS1.1, AS1.4)
- [x] **T152** (upload and download, a rejected retry, a rejected refresh token, a server outage and other errors) [P] `FE/services/file-storage.service.spec.ts`: `UNAUTHENTICATED` -> renew + retry once. (AS1.1)
- [x] **T153** (`ConstantRetryPolicy` gets a hook that runs before every reconnect attempt; extra case: a session that has ended neither starts nor keeps retrying) [P] `FE/services/signalr.service.spec.ts`: `start_WhenAccessMayHaveExpired_ShouldRenewFirst`; `reconnect_ShouldRenewBeforeRetry`. (FR-009, AS3.2)

Implementation:
- [x] **T154** (plus `ensureAccess()`, which renews only when the access token may have expired, using the new `environment.session` settings in both environment files) `SessionService.renew()` with a shared in-flight observable. (T150, T140)
- [x] **T155** (rewritten: `renew()` then one retry; a second 401 calls `onSessionExpired()`) `auth.interceptor.ts` renew-and-retry. (T151, T154)
- [x] **T156** (backend first: `JwtInterceptor` throws `RpcException(Unauthenticated)`, three "throws" tests updated, file storage tests 10 pass; the gRPC status is compared as the number 16, because importing the `grpc-web` enum added a CommonJS build warning) [P] `file-storage.service.ts` retry on `UNAUTHENTICATED`. (T152) Also, first: `BE/Applications/LetsTalk.Server.FileStorage.Service/GrpcInterceptors/JwtInterceptor.cs` must throw `RpcException(StatusCode.Unauthenticated)` instead of `UnauthorizedAccessException` (update the four "throws" tests in `JwtInterceptorTests` first), otherwise the browser sees `Unknown`.
- [x] **T157** (`setUpConnection()` calls `ensureAccess()` first and returns `false` when the session is over, so the retry timer is not created or is cleared) [P] `signalr.service.ts` renewal before (re)start and in the reconnect policy. (T153)
- [x] **T158** (2026-11-10: vitest 63 files / 771 tests pass; `ng lint` still reports the 17 older errors, none new in the changed files; `npm run build` succeeds without warnings) `npm run test:run`, `npm run lint`, `npm run build`. Checkpoint 5.

## Phase 6 - Verification
- [x] **T160** (written for the setup of this repository: the temporary short lifetimes go into the Authentication `appsettings.Development.json` and `environment.ts`, because the settings files are read after environment variables; R6 checks the file service with a gRPC-web `curl`; R7 uses `generate-login-code`, which changes nothing; found while writing it: the notifications reconnect needs the extra rule in the retry policy, see plan D7) Add manual steps to `specs/002-short-lived-token-refresh/quickstart.md` (create it) with these checks, using a short test lifetime (access 1 min, grace 10 s):
  - R1 sign in -> cookies: `auth-token` (Path `/`), `auth-refresh` (Path `/api/Authentication`, HttpOnly); (FR-002)
  - R2 wait past access lifetime, click around -> works, new cookies visible; (AS1.1)
  - R3 two tabs, wait, act in both at once -> both work; (AS1.4)
  - R4 leave the notifications connection open past expiry -> messages still arrive; stop/start the notifications service -> reconnects after renewal; (AS3.1, AS3.2)
  - R5 copy the refresh cookie, rotate in the app, replay the copy after the grace period -> session ended, sign-in required; (AS2.2, SC-003)
  - R6 replay a copied access token after expiry -> 401 on API, file storage, notifications; (SC-001)
  - R7 `curl -X POST` with the cookie and `Origin: https://evil.example` -> 403; same with the allowed origin -> success; with `Authorization` header and foreign origin -> success; (SC-004, AS4.3)
  - R8 send an image, hold the consumer for > access lifetime, then release -> preview arrives; (SC-005)
  - R9 sign out -> refresh with the old refresh cookie fails. (FR-007)
- [ ] **T161** Run R1-R9; record results in the PR.
- [ ] **T162** `dotnet test BE/LetsTalk.Server.sln` green; `/code-review` on the branch.
- [ ] **T163** Deployment notes: Redis recommended for persistent sessions; `Cors:AllowedOrigins` required on API and Notifications; lifetimes as configuration; users re-sign-in once. Update `CLAUDE.md` auth paragraph (from 001) to mention refresh.

## Dependencies
```
T101 -> T114 -> T116 -> T117 -> T118 -> T124 -> T140 -> T154 -> T155..T157
T110-T113 -> T115-T117 -> T119
T120-T123 -> T124-T127 -> T128
T125 -> T126
T130 -> T131 (needs T101)
```
Phases 2 and 3 are independent after T118; frontend tests (T150-T153) can start once the `refresh` contract is fixed.

## Traceability
| Requirement | Tasks |
|---|---|
| FR-001 short access lifetime | T101, T110, T115 |
| FR-002 refresh credential, scoped | T101, T120, T124, R1 |
| FR-003 idle + absolute lifetimes | T101, T111, T116 |
| FR-004 auto renew + retry once | T120, T150, T151, T154, T155 |
| FR-005 rotation + grace | T111, T112, T116 |
| FR-006 reuse ends the session | T111, T116, R5 |
| FR-007 logout kills both | T111, T120, T124, R9 |
| FR-008 shared renewal | T150, T151, T154 |
| FR-009 renew before reconnect | T153, T157, R4 |
| FR-010 origin check for cookie requests | T121, T122, T125, T126, R7 |
| FR-011 origin check for the hub | T130, T131 |
| FR-012 background credential | T110, T113, T123, T127, R8 |
| FR-013 configurable/dev lifetimes | T101 |
| NFR-001 renewal latency | T161 (observe) |
| NFR-002 hashed storage | T111 |
| NFR-003 no secrets | T100, T101 |
| SC-001..SC-005 | R6, T161, R5, R7, R8 |
