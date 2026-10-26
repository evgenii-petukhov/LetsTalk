# Tasks: Short-lived sessions with silent renewal

**Inputs**: [spec.md](spec.md), [plan.md](plan.md). **Precondition**: all of `specs/001-httponly-cookie-auth/tasks.md` is done and merged.
**Conventions**: as in spec 001 (`[P]` parallel, tests before code, `Method_WhenCondition_ShouldOutcome`). `BE` = `backend/`, `FE` = `frontend/src/app/`. **Commit message prefix: `Token refresh: `** (one line, for example `Token refresh: rotate refresh tokens with reuse detection`). **One commit per phase**, made only when the owner approves, with the ticked `tasks.md` included.

## Phase 0 - Setup
- [ ] **T100** Branch `002-short-lived-token-refresh` from `001-httponly-cookie-auth`. Re-check that uncommitted `Program.cs`/`appsettings.json` changes are not staged (constitution 1).
- [ ] **T101** [P] Extend `JwtSettings` (`AccessTokenLifetime`, `BackgroundTokenLifetime`, `RefreshIdleLifetime`, `RefreshAbsoluteLifetime`, `RefreshReuseGrace`) and `AuthCookieSettings` (`RefreshName`, `RefreshPath`); defaults per plan D1; test binding in `LetsTalk.Server.Configuration.Tests`; add values (no secrets) to Authentication/API `appsettings.json` and long lifetimes to `appsettings.Development.json`. (FR-001, FR-003, FR-013)

## Phase 1 - Authentication service
Tests first, in `BE/Applications/LetsTalk.Server.Authentication.Tests`:
- [ ] **T110** `JwtStorageService`: `Generate_WhenLifetimeGiven_ShouldSetExpiry`; access vs background lifetime. (FR-001, FR-012)
- [ ] **T111** [P] `MemoryRefreshSessionService` tests: `IssueAsync_ShouldStoreOnlyHash`; `RotateAsync_WhenUnused_ShouldIssueNewPair`; `RotateAsync_WhenUsedWithinGrace_ShouldIssueAccessOnly`; `RotateAsync_WhenUsedAfterGrace_ShouldEndChainAndFail`; `RotateAsync_WhenIdleExpired_ShouldFail`; `RotateAsync_WhenAbsoluteLifetimeReached_ShouldFail`; `RotateAsync_WhenConcurrent_ShouldCreateSingleSuccessor`; `EndByRefreshTokenAsync_ShouldInvalidateChain`. (FR-003, FR-005, FR-006, FR-007, NFR-002)
- [ ] **T112** [P] Same for `RedisRefreshSessionService` (mocked `IDatabase`), including the atomic `usedAt` marker.
- [ ] **T113** [P] gRPC service tests: `CreateSession`, `RefreshSession`, `EndSession`, `GenerateJwtToken_WhenKindBackground_ShouldUseBackgroundLifetime`.

Implementation:
- [ ] **T114** Update both `.proto` copies (`kind` enum; `CreateSession`, `RefreshSession`, `EndSession`). (depends T101)
- [ ] **T115** `JwtStorageService.Generate(accountId, lifetime)` and callers (`MemoryCacheTokenService`, `RedisCacheTokenService`). (T110)
- [ ] **T116** `IRefreshSessionService` + `MemoryRefreshSessionService` + `RedisRefreshSessionService`, DI registration next to the existing token-cache switch in `AuthenticationServiceRegistration.cs`. (T111, T112)
- [ ] **T117** gRPC service methods. (T113, T114, T116)
- [ ] **T118** `IAuthenticationClient`/`AuthenticationClient`: `CreateSessionAsync`, `RefreshSessionAsync`, `EndSessionAsync`, `GenerateBackgroundTokenAsync`. (T114)
- [ ] **T119** `dotnet test BE/Applications/LetsTalk.Server.Authentication.Tests`. Checkpoint 1.

## Phase 2 - API
Tests first:
- [ ] **T120** `AuthenticationController` tests (project from spec 001 T022): `EmailLoginAsync_WhenSuccess_ShouldSetAccessAndRefreshCookies` (refresh cookie `Path` scoped, HttpOnly); `RefreshAsync_WhenValid_ShouldSetNewCookiesAndReturnNoContent`; `RefreshAsync_WhenInvalid_ShouldClearCookiesAndReturn401`; `RefreshAsync_WhenOnlyAuthorizationHeader_ShouldReturn401`; `LogoutAsync_ShouldEndSessionAndClearBothCookies`. (FR-002, FR-004, FR-007)
- [ ] **T121** [P] `OriginCheckMiddlewareTests` in `API.Middleware.Tests`: `InvokeAsync_WhenCookieAndPostAndForeignOrigin_ShouldReturn403`; `..._WhenCookieAndPostAndAllowedOrigin_ShouldContinue`; `..._WhenCookieAndNoOriginButAllowedReferer_ShouldContinue`; `..._WhenCookieAndGet_ShouldContinue`; `..._WhenHeaderCredentialAndForeignOrigin_ShouldContinue`; `..._WhenCookieAndNoOriginNoReferer_ShouldReturn403`. (FR-010)
- [ ] **T122** [P] `JwtMiddlewareTests`: sets `Items["CredentialSource"]` to `Cookie` or `Header`. 
- [ ] **T123** [P] `CreateMessageCommandHandlerTests`: `Handle_WhenImageOrLink_ShouldPassBackgroundTokenToLaunchers` (not the user's access token). (FR-012, AS5.1)

Implementation:
- [ ] **T124** `AuthenticationController`: login with `CreateSession`, new `refresh`, extended `logout`. (T118, T120)
- [ ] **T125** `JwtMiddleware` credential source flag. (T122)
- [ ] **T126** `OriginCheckMiddleware` + extension, register in `Program.cs` after `UseJwtMiddleware` and before `MapControllers`; add to the pipeline tests if any. (T121, T125)
- [ ] **T127** `CreateMessageCommandHandler`: mint background token through `IAuthenticationClient`; remove the now unused `Token` use from `MessageController`/`ApiController.GetToken()` if no callers remain. (T118, T123)
- [ ] **T128** `dotnet test` for API.Core.Tests, API.Middleware.Tests, API tests. Checkpoint 2.

## Phase 3 - Notifications
- [ ] **T130** `NotificationHub` tests: `OnConnectedAsync_WhenCookieAndForeignOrigin_ShouldAbort`; `..._WhenCookieAndAllowedOrigin_ShouldRegister`; `..._WhenExpiredAccessCookie_ShouldAbort`. (FR-011, AS3.2)
- [ ] **T131** Implement the origin check in `NotificationHub.OnConnectedAsync` using `Cors:AllowedOrigins`. (T130)
- [ ] **T132** `dotnet test` Notifications.Tests. Checkpoint 3.

## Phase 4 - Clients
- [ ] **T140** Regenerate `FE/api-client/api-client.ts` (`nswag/angular.nswag`) and `BE/API/LetsTalk.Server.API.Client/ApiClient.cs` (`nswag/csharp.nswag`). **Not** `nextjs.nswag`. Confirm `refresh()` exists. (T124)
- [ ] **T141** `dotnet build BE/LetsTalk.Server.sln`. Checkpoint 4.

## Phase 5 - Angular
Tests first:
- [ ] **T150** `FE/services/session.service.spec.ts`: `renew_WhenCalledConcurrently_ShouldCallApiOnce`; `renew_WhenRefreshFails_ShouldClearStateAndGoToSignIn`. (FR-004, FR-008)
- [ ] **T151** [P] `FE/interceptors/auth.interceptor.spec.ts`: `intercept_When401_ShouldRenewAndRetryOnce`; `..._WhenRetryAlso401_ShouldGoToSignIn`; `..._WhenRequestIsRefreshOrLogin_ShouldNotRenew`; `..._WhenParallel401s_ShouldRenewOnce`. (AS1.1, AS1.4)
- [ ] **T152** [P] `FE/services/file-storage.service.spec.ts`: `UNAUTHENTICATED` -> renew + retry once. (AS1.1)
- [ ] **T153** [P] `FE/services/signalr.service.spec.ts`: `start_WhenAccessMayHaveExpired_ShouldRenewFirst`; `reconnect_ShouldRenewBeforeRetry`. (FR-009, AS3.2)

Implementation:
- [ ] **T154** `SessionService.renew()` with a shared in-flight observable. (T150, T140)
- [ ] **T155** `auth.interceptor.ts` renew-and-retry. (T151, T154)
- [ ] **T156** [P] `file-storage.service.ts` retry on `UNAUTHENTICATED`. (T152) Also, first: `BE/Applications/LetsTalk.Server.FileStorage.Service/GrpcInterceptors/JwtInterceptor.cs` must throw `RpcException(StatusCode.Unauthenticated)` instead of `UnauthorizedAccessException` (update the four "throws" tests in `JwtInterceptorTests` first), otherwise the browser sees `Unknown`.
- [ ] **T157** [P] `signalr.service.ts` renewal before (re)start and in the reconnect policy. (T153)
- [ ] **T158** `npm run test:run`, `npm run lint`, `npm run build`. Checkpoint 5.

## Phase 6 - Verification
- [ ] **T160** Add manual steps to `specs/002-short-lived-token-refresh/quickstart.md` (create it) with these checks, using a short test lifetime (access 1 min, grace 10 s):
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
