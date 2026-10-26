# Tasks: HttpOnly cookie session

**Inputs**: [spec.md](spec.md), [plan.md](plan.md), [contracts/](contracts/), [quickstart.md](quickstart.md)\
**Conventions**: `[P]` = can run in parallel with other `[P]` tasks of the same phase (different files, no dependency). Tests come before the code they cover (constitution, principle 4). Test names follow `Method_WhenCondition_ShouldOutcome`. Paths are relative to the repo root. `BE` = `backend/`, `FE` = `frontend/src/app/`. **Commit message prefix: `Http-only cookie: `** (one line, for example `Http-only cookie: revoke tokens on logout`). **One commit per phase**, made only when the owner approves, with the ticked `tasks.md` included.

## Phase 0 - Setup and guardrails
- [x] **T000** (verified 2026-10-26: the working tree is clean, the owner's `Program.cs`/`appsettings.json` changes are in `stash@{0}` and were not touched; nothing was staged) Before any commit, make sure the uncommitted `BE/Applications/LetsTalk.Server.API/Program.cs` (Swagger outside Development) and `appsettings.json` (real-looking secrets) are NOT staged. Stage files by name only. (NFR-003, constitution 1)
- [x] **T001** (branch already existed and was checked out) Create branch `001-httponly-cookie-auth` from `master`.
- [x] **T002** [P] Add the cookie settings model, bind it, and add the development CORS origin. (FR-011, NFR-001)
  - **Model**: create `AuthCookieSettings` in `BE/Infrastructure/Configuration/LetsTalk.Server.Configuration/Models/AuthCookieSettings.cs`.
    - Config section: `AuthCookie`.
    - Defaults in code:
      - `Name = "auth-token"`
      - `Domain = ""` (empty means a host-only cookie)
      - `Secure = true`
      - `SameSite = "Strict"` (a string, because the Configuration library has no ASP.NET Core reference; the code that builds the cookie parses it into `SameSiteMode`)
  - **Binding**: add `services.Configure<AuthCookieSettings>(configuration.GetSection("AuthCookie"));` as the repo does for other settings (string literal, no shared constant), in:
    - `BE/Applications/LetsTalk.Server.API/ApiServiceRegistration.cs`, next to the `Security` line (~53)
    - `BE/Applications/LetsTalk.Server.Notifications/NotificationsServicesRegistration.cs`
    - `BE/Applications/LetsTalk.Server.FileStorage.Service/FileStorageServiceRegistration.cs`, next to the `FileStorage` line (~44)
  - **Configuration values**:
    - Do **not** edit any `appsettings.json`. The API one holds the owner's uncommitted secrets, and the class defaults make the edit unnecessary.
    - Development: in `appsettings.Development.json` of the API, Notifications and FileStorage services add `"Cors": { "AllowedOrigins": [ "https://127.0.0.1:4200" ] }`. The origin is not a secret (plan D4, D8).
    - Production: supplied by the deployment (T082).
  - **Tests**: in `LetsTalk.Server.Configuration.Tests` (create it if it does not exist, same pattern as T003):
    - `AuthCookieSettings_WhenSectionMissing_ShouldUseDefaults`
    - `AuthCookieSettings_WhenSectionPresent_ShouldBindValues`
- [x] **T003** [P] Create test project `BE/Applications/LetsTalk.Server.FileStorage.Service.Tests` modeled on `LetsTalk.Server.Notifications.Tests.csproj`; add it to `LetsTalk.Server.sln`.

## Phase 1 - Authentication service: revocation (blocks everything that logs out)
Tests first, in `BE/Applications/LetsTalk.Server.Authentication.Tests`:
- [x] **T010** Tests for `MemoryCacheTokenService.RevokeAsync` / `GetAccountIdAsync`: `GetAccountIdAsync_WhenTokenRevoked_ShouldReturnNull`; `..._WhenRevokedTokenStillHasValidSignature_ShouldNotRevalidate`; `RevokeAsync_WhenTokenInvalid_ShouldNotThrow`. (FR-005, SC-002)
- [x] **T011** [P] (`RedisConnection.Connection` was made `virtual` in `BE/Persistence/LetsTalk.Server.Persistence.Redis/RedisConnection.cs` so the class can be mocked) Same tests for `RedisCacheTokenService` (mock `IDatabase`/`RedisConnection`): denylist key written with TTL = remaining lifetime; denylist checked before the cache read. (FR-005)
- [x] **T012** [P] Test `JwtTokenGrpcService.RevokeJwtToken_WhenCalled_ShouldCallCacheService`.

Implementation:
- [x] **T013** Add `RevokeJwtToken` rpc + messages to **both** `.proto` copies (`BE/Applications/LetsTalk.Server.Authentication/Protos/authentication.proto` and `BE/Infrastructure/Authentication/LetsTalk.Server.Authentication.Client/Protos/authentication.proto`) exactly as in `contracts/auth-endpoints.md`.
  - No manual regeneration step: both projects use `Grpc.Tools` (`<Protobuf Include="Protos\authentication.proto" .../>`), which generates the server base class (`GrpcServices="Server"`) and the client (`GrpcServices="Client"`) from the `.proto` on every build.
  - Verify with `dotnet build` of `LetsTalk.Server.Authentication` and `LetsTalk.Server.Authentication.Client`: the new `RevokeJwtToken` members must appear. T012 and T016 compile only after this build.
- [x] **T014** Add `Task RevokeAsync(string token)` to `IJwtCacheService` (`BE/Applications/LetsTalk.Server.Authentication/Abstractions/IJwtCacheService.cs`); implement in `MemoryCacheTokenService` and `RedisCacheTokenService` (denylist first; positive cache entry removed; expiry from `IJwtStorageService.GetStoredTokenAsync`; invalid tokens ignored). Shared key helper in `CacheTokenServiceBase`. (depends T010, T011, T013)
- [x] **T015** Implement `JwtTokenGrpcService.RevokeJwtToken`. (depends T012, T014)
- [x] **T016** Add `RevokeJwtTokenAsync(string token)` to `IAuthenticationClient` (`BE/Infrastructure/Authentication/LetsTalk.Server.Authentication.Abstractions/IAuthenticationClient.cs`) and `AuthenticationClient` (`.../LetsTalk.Server.Authentication.Client/AuthenticationClient.cs`). (depends T013)
- [x] **T017** Run `dotnet test BE/Applications/LetsTalk.Server.Authentication.Tests`. Checkpoint 1.

## Phase 2 - API: cookie resolution, login, logout
Tests first:
- [x] **T020** (also added: `WhenCookieAndAuthServiceUnavailable_ShouldContinueAnonymouslyAndKeepCookie`, `WhenCookieInvalid_ShouldDeleteCookieWithConfiguredDomainAndPath`, `WhenCookieInvalidAndHeaderValid_ShouldUseHeaderAndDeleteCookie`) Extend `BE/API/LetsTalk.Server.API.Middleware.Tests/JwtMiddlewareTests.cs`: `InvokeAsync_WhenCookiePresent_ShouldSetAccountIdAndToken`; `..._WhenOnlyAuthorizationHeader_ShouldSetAccountIdAndToken`; `..._WhenBothPresentAndDifferent_ShouldUseCookie`; `..._WhenHeaderHasBearerPrefix_ShouldStoreBareToken`; `..._WhenHeaderTokenInvalid_ShouldThrowUnauthorized`; `..._WhenNoCredential_ShouldContinueWithoutAccountId`; **FR-013:** `..._WhenCookieInvalid_ShouldContinueAnonymouslyAndDeleteCookie`; `..._WhenCookieRpcException_ShouldContinueAnonymouslyAndDeleteCookie`; `..._WhenValidCookieAndInvalidHeader_ShouldUseCookie`. (FR-001, FR-004, FR-012, FR-013)
- [x] **T021** [P] (the expiry reaches the handler through a new `expires_at_unix_seconds` field of `GenerateJwtTokenResponse`; `GenerateJwtTokenAsync` now returns `(token, expiresAt)`, see `contracts/auth-endpoints.md`; `GenerateJwtToken_WhenCalled_ShouldReturnTokenAndExpiry` added in `Authentication.Tests`) Update `BE/API/LetsTalk.Server.API.Core.Tests/Handlers/EmailLoginCommandHandlerTests.cs` for the new result type (`EmailLoginResult` with `Token`, `ExpiresAt`; DTO without token). (FR-002)
- [x] **T022** [P] (project `LetsTalk.Server.API.Tests` created and added to the solution) Add controller tests project or tests for `AuthenticationController` (new `BE/Applications/LetsTalk.Server.API.Tests` if none exists, same csproj pattern as T003): `EmailLoginAsync_WhenSuccess_ShouldSetHttpOnlyCookieAndNotReturnToken`; `EmailLoginAsync_WhenCodeWrong_ShouldNotSetCookie`; `LogoutAsync_WhenCredentialValid_ShouldRevokeAndClearCookie`; `LogoutAsync_WhenNoCredential_ShouldReturnNoContentAndClearCookie`; `LogoutAsync_WhenCookieStale_ShouldReturnNoContent`. (FR-002, FR-005, FR-013, AS1.3, AS4.x)
- [x] **T023** [P] Test for `ApiController.GetToken` returning `HttpContext.Items["Token"]` (e.g. via `MessageController` test): `GetToken_WhenSessionCookieUsed_ShouldReturnBareToken`. (FR-012)

Implementation:
- [x] **T024** (cookie helpers in `BE/API/LetsTalk.Server.API.Middleware/AuthCookieExtensions.cs`) `BE/API/LetsTalk.Server.API.Middleware/JwtMiddleware.cs`: cookie first (name from `AuthCookieSettings`), header fallback (last part); store `Items["Token"]` (bare) and `Items["AccountId"]`. Invalid *cookie* credential: do not throw, delete the cookie (same Domain/Path), continue anonymously; invalid *header-only* credential keeps throwing (plan D1). (depends T002, T020)
- [x] **T025** `BE/Applications/LetsTalk.Server.API/Controllers/ApiController.cs`: `GetToken()` returns `Items["Token"]`. (depends T023, T024)
- [x] **T026** `EmailLoginCommandHandler` + `EmailLoginCommand`: return `EmailLoginResult` (token + expiry), remove `Token` from `BE/Models/LetsTalk.Server.Models.Dtos/LoginResponseDto.cs`. (depends T021)
- [x] **T027** `AuthenticationController.EmailLoginAsync`: append cookie per `contracts/cookie-and-cors.md`, return `LoginResponseDto { Success }`. (depends T022, T026)
- [x] **T028** `AuthenticationController.LogoutAsync` (`POST logout`, `[AllowAnonymous]`, `204`): resolve credential, `RevokeJwtTokenAsync`, expire cookie. (depends T016, T022, T024)
- [x] **T029** Run `dotnet test` for API.Middleware.Tests, API.Core.Tests, API.Tests. Checkpoint 2.

## Phase 3 - CORS with credentials
- [x] **T030** (new project `BE/Infrastructure/LetsTalk.Server.Cors.Tests`; also `..._WhenOriginsEmptyInDevelopment_ShouldNotThrow`, `..._WhenExposedHeadersProvided_ShouldExposeThem`) Tests in `LetsTalk.Server.Configuration.Tests` (or a new DI test project if the helper lives elsewhere) for the helper: `AddCredentialedCors_WhenOriginsConfigured_ShouldAllowOnlyThoseOrigins`; `..._WhenOriginsEmptyOutsideDevelopment_ShouldThrow`. (FR-009)
- [x] **T031** Add the helper `AddCredentialedCors` in the new project `BE/Infrastructure/LetsTalk.Server.Cors/CorsServiceCollectionExtensions.cs` (not in `DependencyInjection`, so the ASP.NET Core framework reference stays out of that project; referenced by API, Notifications and File storage only) (named policy "all", `WithOrigins`, any header/method, `AllowCredentials`, optional exposed headers). (depends T002, T030)
- [x] **T032** [P] API: use the helper in `BE/Applications/LetsTalk.Server.API/ApiServiceRegistration.cs:29`.
- [x] **T033** [P] Notifications (project reference to `Cors` added): `NotificationsServicesRegistration.cs:24`.
- [x] **T034** [P] File storage: `FileStorageServiceRegistration.cs:27` and replace the per-endpoint `.RequireCors(... AllowAnyOrigin())` in `FileStorage.Service/Program.cs:32` with the named policy (exposed gRPC-web headers included). (FR-009)
- [x] **T035** (also: `BE/RunAll.ps1` sets `ASPNETCORE_ENVIRONMENT=Development` and `Cors__AllowedOrigins__0=https://127.0.0.1:4200` for the services it starts: they would otherwise start as Production and fail on the empty `Cors:AllowedOrigins`, and the published executables do not find `appsettings.Development.json` because RunAll starts them without a working directory; Notifications runs on `https://127.0.0.1:5002` in Development through its `appsettings.Development.json`, which its `Program.cs` now adds after `appsettings.json`, see plan D8) Leave the `Authentication` service's CORS unchanged (not browser-facing, see research R5). Verify `dotnet build BE/LetsTalk.Server.sln`. Checkpoint 3.

## Phase 4 - Real-time and file service
- [x] **T040** (cookie missing now means "stay open, unregistered, no abort" so that `AuthorizeAsync` keeps working, see `contracts/realtime-auth.md`; tests are `..._WhenCookieMissing_ShouldNotRegisterAndNotAbort`, plus `..._WhenValidationThrowsRpcException_ShouldAbortAndNotRegister` and `AuthorizeAsync_WhenTokenInvalid_ShouldNotRegisterConnection`) Tests in `BE/Applications/LetsTalk.Server.Notifications.Tests`: `OnConnectedAsync_WhenCookieValid_ShouldRegisterConnection`; `..._WhenCookieMissing_ShouldAbortAndNotRegister`; `..._WhenCookieInvalid_ShouldAbort`; `AuthorizeAsync_WhenTokenValid_ShouldRegisterConnection` (unchanged path). (FR-010, FR-004)
- [x] **T041** [P] (also `..._WhenCredentialInvalid_ShouldThrowUnauthorized`) Tests in the new FileStorage test project (T003): `UnaryServerHandler_WhenCookiePresent_ShouldAuthenticate`; `..._WhenOnlyMetadata_ShouldAuthenticate`; `..._WhenBothPresent_ShouldPreferCookie`; `..._WhenNone_ShouldThrowUnauthorized`. (FR-003, FR-004)
- [x] **T042** `NotificationHub.OnConnectedAsync` per `contracts/realtime-auth.md`. (depends T002, T040)
- [x] **T043** `JwtInterceptor`: metadata or cookie via `context.GetHttpContext()`. (depends T002, T041)
- [x] **T044** Run `dotnet test` for Notifications.Tests and FileStorage.Service.Tests. Checkpoint 4.

## Phase 5 - Clients and contracts
- [x] **T050** (done by the owner; verified: `logout` in both clients, `LoginResponseDto` has only `success`, the only other diffs are the stored spec snapshot in the two `.nswag` files) **Owner step, then agent verification.** Regenerate the API clients from the changed API. **Do not run `nextjs.nswag`.** (depends T027, T028)
  - **Why it is not automatic:** each `.nswag` file has `fromDocument.url` = `https://127.0.0.1:5000/swagger/v1/swagger.json` and also a stored `fromDocument.json` snapshot of the spec. The snapshot has no `logout`. Which one the generator uses when both are present is unverified, so a blind run could regenerate the old client. Running the API needs Docker, Redis, Kafka and the owner's secrets.
  - **Owner:**
    - Start the API in Development (for example `RunAll.ps1`) so the URL above responds.
    - In NSwagStudio open `nswag/angular.nswag`, refresh the stored specification from the URL ("Create local copy"), then generate outputs. Repeat for `nswag/csharp.nswag`. Or run `nswag run` for each file if the CLI is on PATH.
    - Outputs: `FE/api-client/api-client.ts` and `BE/API/LetsTalk.Server.API.Client/ApiClient.cs`.
    - Tell the agent when done.
  - **Agent (after the owner's reply):** verify, do not generate by hand.
    - `logout` exists in both generated clients.
    - `LoginResponseDto` has no `token`.
    - `git diff --stat` shows only the expected files. The `.nswag` files may also change (the stored snapshot); stage them by explicit path.
    - If the check fails, report it and stop.
- [x] **T051** (solution builds with 0 errors; the full `dotnet test` run has no failures) `dotnet build BE/LetsTalk.Server.sln`; fix any consumer of `ApiClient` that referenced removed members (image/link consumers and Lambdas should compile unchanged). Checkpoint 5.

## Phase 6 - Angular front end
Tests first (vitest; update or add specs):
- [x] **T060** (spec names follow the front-end `should ...` style) `FE/services/token-storage.service.spec.ts` -> `clearLegacyToken_WhenLegacyEntryExists_ShouldRemoveIt`; remove save/get specs. (FR-007)
- [x] **T061** (the "no page reload" part cannot be observed in jsdom, where `window.location` is not redefinable) [P] New `FE/interceptors/auth.interceptor.spec.ts`: adds `withCredentials`; sends no `Authorization`; on 401 clears state and navigates to `/sign-in` (no reload loop). (FR-003, FR-008)
- [x] **T062** (also `refresh`, single-flight probe, `logout` tests; guard specs mock `createUrlTreeFromSnapshot`) [P] New `FE/services/session.service.spec.ts`: `isSignedIn_WhenProbeSucceeds_ShouldBeTrueAndCached`; `..._WhenProbe401_ShouldBeFalse`; `..._ShouldCallApiNotStoreCache` (probe uses `ApiService.getProfile()`, not the NgRx-cached `getLoggedInUser()`); invalidated on logout; `onSessionLost_ShouldStopSignalr`. New guard specs `FE/guards/*.spec.ts` (authenticated-only, anonymous-only, complete-profile). (FR-006, AS2.2, AS2.3, edge case "live connection")
- [x] **T063** (a failed probe shows `errorMessages.sessionNotKept` and stays on the page) [P] Update `FE/components/sign-in/login-by-email/login-by-email.component.spec.ts`: on success the session probe runs and then it navigates to `chats`, storing nothing; `submit_WhenProbeFailsAfterLogin_ShouldShowCookiesMessageAndStay`. (AS1.1, AS1.5, FR-001, FR-014)
- [x] **T064** (the constructor arguments are asserted through a mocked client class) [P] Update `FE/services/file-storage.service.spec.ts`: client created with `{ withCredentials: true }`; no `authorization` metadata. (FR-003)
- [x] **T065** (also: stop on `sessionLost`, session check after a failed start) [P] Update `FE/services/signalr.service.spec.ts`: `withUrl` options include `withCredentials: true`; no `AuthorizeAsync` on start or reconnect. (FR-010)
- [x] **T066** (page reload goes through `SessionService.reloadPage()` so it can be mocked; the unused `AccountListTopPanelComponent.onLogoutButtonClicked` was removed instead of updated, the template never called it) [P] Update `FE/components/sidebar/chat-sidebar/chat-list-top-panel/chat-list-top-panel.component.spec.ts` and the account list top panel spec: logout calls API `logout()`, clears state, reloads. (AS4.1)

Implementation (depends T050 and its test task):
- [x] **T070** `FE/services/token-storage.service.ts` -> `clearLegacyToken()`; call once at bootstrap (`APP_INITIALIZER` in `FE/app.module.ts`). (T060)
- [x] **T071** (no more `console.log` of every error; `httpLogInterceptor` already logs) `FE/interceptors/auth.interceptor.ts` (and `providers/auth-interceptor-provider.ts` if needed). (T061)
- [x] **T072** (adds a read-only `sessionLost` observable, `refresh()`, `logout()`, `reloadPage()`; `onSessionLost()` also dispatches `sessionActions.ended`, and the meta-reducer `resetOnSessionEnded` (new `state/session/session.actions.ts`, `state/reset-on-session-ended.meta-reducer.ts`, `storeMetaReducers` in `store-config.ts`) resets the whole store; tests: `reset-on-session-ended.meta-reducer.spec.ts`, `session-reset.spec.ts` with the real store) New `FE/services/session.service.ts` (probe via `ApiService.getProfile()`; on session loss stop the SignalR connection); update `FE/guards/authenticated-only-guard.ts`, `anonymous-only-guard.ts`, `complete-profile-guard.ts` to use it. Keep `ChangeDetectionStrategy.OnPush` components unchanged. (T062)
- [x] **T073** `login-by-email.component.ts`: drop `saveToken`; after a successful login run the probe and, if it fails, show the "browser did not keep the session" message (FR-014). Update `FE/services/api.service.ts` for the `logout` method and removed `token`. (T063)
- [x] **T074** [P] `FE/services/file-storage.service.ts` (T064). [P] `FE/services/signalr.service.ts` (T065). [P] logout handlers in both top-panel components (T066).
- [x] **T075** (CORRECTION: the local dev environment is `FE/environments/environment.ts`; `environment.hosting.dev.ts` was outdated and unused, so it was removed together with the `development-hosting` configuration in `angular.json` and the `build:dev:hosting` script. `start` is now `ng serve --host 127.0.0.1 --ssl`, matching the dev CORS origin `https://127.0.0.1:4200`. Two pre-existing `angular.json` problems of the `serve` target were fixed so that `npm start` works: it used the removed `browserTarget` key (now `buildTarget`) and had no development default, so it served an optimized build without source maps (now `defaultConfiguration: development`)) Environments: `FE/environments/environment.hosting.dev.ts` -> all URLs on `127.0.0.1`; `package.json` script `start` uses `ng serve --host 127.0.0.1` (or document the flag). (FR-011)
- [x] **T076** (results: vitest 62 files / 736 tests pass; `npm run build` succeeds; `npx ng lint` reports 17 errors, all in code not touched by this phase (`api.service.ts:203,205`, `signalr.service.spec.ts:447,452`, rtc and stub files); `npm run lint` was not run because it uses `--fix` and would rewrite unrelated files) `npm run test:run`, `npm run lint`, `npm run build` in `frontend/`. Checkpoint 6.

## Phase 7 - Verification and wrap-up
- [ ] **T080** Run the whole `quickstart.md` (Q1-Q13) against a local run (`RunAll.ps1` + `ng serve --host 127.0.0.1`). Record results in the PR description.
- [ ] **T081** `dotnet test BE/LetsTalk.Server.sln` fully green.
- [ ] **T082** Add deployment notes to the PR: set `AuthCookie:Domain=petukhov.fyi`, `AuthCookie:Secure=true`, `Cors:AllowedOrigins=https://letstalk.petukhov.fyi` on API, Notifications and FileStorage; users must sign in once.
- [ ] **T083** Update `CLAUDE.md` "Frontend notes"/auth paragraph with the cookie-session behavior (one short paragraph). Run `/code-review` on the branch before merging.

## Dependencies
```
T000 T001 -> everything
T002 -> T024 T031 T042 T043
T013 -> T014 T016 -> T028
T010-T012 -> T014 -> T015 -> T017
T020-T023 -> T024-T028 -> T029
T031 -> T032 T033 T034
T027 T028 -> T050 -> T051 -> T070..T076 -> T080
```
Parallel groups after Checkpoint 1: Phase 2, Phase 3 and Phase 4 are independent of each other except for the shared `T002`; Phase 6 can start its test tasks (T060-T066) as soon as Phase 2's contracts are agreed, and its implementation after T050.

## Traceability (requirement -> tasks)
| Requirement | Tasks |
|---|---|
| FR-001 credential unreadable by scripts | T020, T024, T027, T063, T070, Q1 |
| FR-002 session established without exposing credential | T021, T022, T026, T027 |
| FR-003 session sent to all services | T061, T064, T071, T074, T043 |
| FR-004 explicit credential still accepted; cookie wins | T020, T024, T040, T041, T042, T043, Q12 |
| FR-005 sign-out + server-side invalidation | T010-T016, T022, T028, T066, Q9 |
| FR-006 signed-in state from server | T062, T072 |
| FR-007 legacy credential removed | T060, T070, Q11 |
| FR-008 rejected session -> sign-in | T061, T071, T065, Q10 |
| FR-009 explicit CORS origins with credentials | T030-T034, Q13 |
| FR-010 realtime tied to account without handing over credential | T040, T042, T065, T074 |
| FR-011 works in dev and prod via configuration | T002, T075, T082 |
| FR-012 credential handed to background work | T020, T023, T025, Q6 |
| FR-013 stale session does not block public actions; server clears it | T020, T022, T024, Q15 |
| FR-014 confirm session after sign-in; message when browser drops it | T063, T073, Q14 |
| NFR-001 cookie attributes | T002, T022, T027 |
| NFR-002 sign-out < 1 s | T080 (observe) |
| NFR-003 no secrets / Swagger dev-only | T000, T002 |
| SC-001..SC-004 | Q1, Q9, Q12, T080 |
| AS1.4, AS1.5 | T020, T024, T063, Q14, Q15 |
| Edge: live connection when session lost | T062, T072 |
