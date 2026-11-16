# Tasks: Retry short-lived failures of service-to-service calls

**Inputs**: [spec.md](spec.md), [plan.md](plan.md), [research.md](research.md).\
**Precondition**: `master` includes specs 001 to 005 and the registration-class renaming (it does).\
**Conventions**: as in specs 001 to 005 (tests before code, `Method_WhenCondition_ShouldOutcome`, repeated literals as class-level `private const`; NUnit + Moq + FluentAssertions; `FakeTimeProvider` for time). `BE` = `backend/`.\
**Commit message prefix: `Retry: `** (one line, for example `Retry: add the shared HTTP retry pipeline`).\
**One commit per phase**, made only when the owner approves, with the ticked `tasks.md` included. Stage files by explicit path. Before staging an `appsettings.json`, check that `git diff` shows only the intended lines. Run `backend/CheckWarnings.ps1` before reporting a phase.

## Phase 0 - Setup
- [ ] **T600** Branch `006-retry-resilience` from `master` (done when the spec was written); confirm the spec files are committed in their own commit before any code.

## Phase 1 - Shared library
Tests first, in the new `BE/Infrastructure/LetsTalk.Server.Resilience.Tests` (a stub `HttpMessageHandler` that answers from a queue, and `FakeTimeProvider`):
- [x] **T601** (25 test cases in `HttpRetryPipelineTests` (statuses as test cases) with a stub handler and `FakeTimeProvider`; extra cases: zero retries tries once, jitter keeps every delay within the limit, a connection error's message text (it can hold the address) never reaches the reason; run 3 times without a failure) `HttpRetryPipelineTests`: `Send_WhenFirstAnswerIsTransient_ShouldRepeatAndReturnTheLaterAnswer` (each of 408, 429, 502, 503, 504, as test cases); `Send_WhenConnectionFails_ShouldRepeat`; `Send_WhenAttemptTimesOut_ShouldRepeat`; `Send_WhenAnswerIsNotTransient_ShouldNotRepeat` (400, 401, 403, 404, 500); `Send_WhenAllAttemptsFail_ShouldReturnTheLastAnswerAfterTheConfiguredRepeats`; `Send_WhenRepeating_ShouldSendTheSameBodyAndHeaders` (POST with `StringContent` and an `Authorization` header); `Send_WhenAnswerHasRetryAfter_ShouldWaitThatLongButNotMoreThanMaxDelay`; `Send_WhenPausing_ShouldGrowTheDelayWithEachRepeat`; `Send_WhenCancelled_ShouldNotRepeatAndShouldStop`; `Send_WhenTotalTimeoutPasses_ShouldStopRepeating`; `Send_WhenRepeating_ShouldCallOnRetryWithAttemptAndReasonOnly` (no address). (FR-001 to FR-005, FR-010)
- [x] **T602** (2 tests: the handler built with the default settings repeats a 503, and it disposes the inner handler; counted in the 25 of the project) [P] `RetryingHttpHandlerTests`: `SendAsync_ShouldUseThePipelineOfHttpRetryPipeline` (one transient then success through an `HttpClient` with the handler). (FR-008)
- [x] **T603** (moved to Phase 3 as T627: `GrpcRetryServiceConfig` lives in `Authentication.Client` so the Lambdas do not carry the gRPC libraries; plan D1 and D3 changed) `GrpcRetryServiceConfigTests`. (FR-007)
- [x] **T604** (30 new tests in `Configuration.Tests` (`HttpRetrySettingsTests`, `GrpcRetrySettingsTests`: defaults and validator); the project now has 295) [P] In `Configuration.Tests`: `HttpRetrySettingsTests` and `GrpcRetrySettingsTests` (defaults as in plan D1), and the validators' tests (negative or zero delay, `MaxDelay` below `Delay`, `AttemptTimeout` above `TotalTimeout`, `MaxAttempts` outside 1 to 5, negative `MaxRetryAttempts`; each message names the key). (FR-009, AS5.1)

Implementation:
- [x] **T605** (both projects added to `LetsTalk.Server.sln` by hand, because `dotnet sln add` rewrote the whole file; `Polly.Core` 8.9.1) Create `LetsTalk.Server.Resilience` (package `Polly.Core`) and `.Tests`; add both to `LetsTalk.Server.sln` (by hand if `dotnet sln add` refuses, as in spec 004): the two settings classes, `RetryEvent`, `HttpRetryPipeline`, `RetryingHttpHandler`. (T601, T602; plan D1)
- [x] **T606** (validators `HttpRetrySettingsValidator` (`Resilience:Http`) and `GrpcRetrySettingsValidator` (`Resilience:Grpc`) and the project reference from `Configuration` to `Resilience`; `AddValidatedSettings` is called where each client is registered, in Phases 2 and 3) The two validators in `Configuration/Validation`, registered with `AddValidatedSettings`; project reference from `Configuration` to `Resilience`. (T604)
- [x] **T607** (2026-11-16: solution builds with 0 errors; `dotnet test` of the solution passes in all 19 test projects; `CheckWarnings.ps1` 0 warnings in the 12 changed files) `dotnet build BE/LetsTalk.Server.sln`, `dotnet test` of the new and `Configuration.Tests` projects, `backend/CheckWarnings.ps1`. **Checkpoint 1.** Propose the commit message and file list; wait for approval.

## Phase 2 - HTTP clients of the services
Tests first:
- [ ] **T610** [P] `ImageProcessing.Service` registration test: `AddImageProcessingServices_ShouldRetryTheApiCallbackClient` (resolve `IHttpClientFactory`, replace the primary handler with a stub that fails once with 503; the client succeeds after the repeat; the settings are read from `Resilience:Http`). (US1)
- [ ] **T611** [P] `LinkPreview` registration tests: the API callback client repeats (`LinkPreviewApiClient`), the download client repeats, and the two clients are different registrations (AS2.3, FR-006). (US1, US2)
- [ ] **T612** [P] `LinkPreviewRequestConsumerTests` (or the existing test class): `Consume_ShouldSendTheReportThroughTheApiCallbackClient`, not through the download client. (FR-006)

Implementation:
- [ ] **T613** `ImageProcessing.Service`: package `Microsoft.Extensions.Http.Resilience`; `AddResilienceHandler` on the existing client; settings bound from `Resilience:Http`; the `Resilience:Http` section in its `appsettings.json` (plan D6). (T610)
- [ ] **T614** `LinkPreview`: same package; the new named client `LinkPreviewApiClient` with the handler; the handler on `HttpClientService`; `LinkPreviewRequestConsumer` creates its client through `IHttpClientFactory.CreateClient("LinkPreviewApiClient")` (the `IHttpClientService` dependency of the consumer is replaced by `IHttpClientFactory`, and the existing consumer test updated); the section in its `appsettings.json`. (T611, T612)
- [ ] **T615** `ShippedSettingsTests` and its csproj: link the LinkPreview `appsettings.json`, add `HttpRetrySettings` and `GrpcRetrySettings` with nested section paths, and the "lists every key" assertion for the new sections. (D6)
- [ ] **T616** `dotnet build`, `dotnet test` of the touched projects, `CheckWarnings.ps1`. **Checkpoint 2.** Propose the commit; wait.

## Phase 3 - gRPC client
Tests first:
- [ ] **T620** `Authentication.Client.Tests` (the project exists, with `AuthenticationClient` tests): with a `GrpcChannel` over a stub handler that answers `Unavailable` twice and then succeeds, `ValidateJwtTokenAsync_WhenUnavailableTwice_ShouldSucceedOnTheThirdAttempt`; the same for `RevokeJwtTokenAsync`, `GenerateJwtTokenAsync` (access and background kinds), `EndSessionAsync`. (AS3.1, FR-007)
- [ ] **T621** [P] `CreateSessionAsync_WhenUnavailable_ShouldTryOnce`, `RefreshSessionAsync_WhenUnavailable_ShouldTryOnce`, `GenerateLoginCodeAsync_WhenUnavailable_ShouldTryOnce`, `ValidateLoginCodeAsync_WhenUnavailable_ShouldTryOnce` (the stub counts the calls). (AS3.2)
- [ ] **T622** [P] `ValidateJwtTokenAsync_WhenServiceRefuses_ShouldNotRepeat` (status `Unauthenticated`, and `InvalidArgument`); `..._WhenAllAttemptsUnavailable_ShouldThrowAfterTheConfiguredAttempts`. (AS3.3, AS1.2 for gRPC)
- [ ] **T623** [P] Registration test: `AddAuthenticationClientServices_ShouldApplyRetrySettingsFromConfiguration`. (D3)

Implementation:
- [ ] **T627** (tests first, written before T624) `GrpcRetryServiceConfigTests` in `Authentication.Client.Tests` (the project exists already): `Create_ShouldListOnlyTheFourReadOnlyMethods` (names come from the generated descriptors; the test fails if a method name no longer exists), `Create_ShouldRetryOnlyUnavailable`, `Create_ShouldApplyTheSettings`, `Create_WhenMaxAttemptsIsOne_ShouldAddNoRetryPolicy` (the gRPC client refuses a policy of one attempt). (FR-007)
- [ ] **T624** `GrpcRetryServiceConfig` in `Authentication.Client` and `AddAuthenticationClientServices`: bind `Resilience:Grpc`, `ConfigureChannel` with `GrpcRetryServiceConfig.Create` on both clients; project reference to `Resilience`; the `Resilience:Grpc` section in the API's `appsettings.json`. (T620 to T623)
- [ ] **T625** Prove repeated `EndSession` is harmless on the server side (a test in the Authentication service's test project: ending the same refresh token twice does not throw); if it does throw, drop `EndSession` from the list in `GrpcRetryServiceConfig`, update FR-007, AS3.1 and research.md first. (research table)
- [ ] **T626** `dotnet build`, `dotnet test` of the touched projects, `CheckWarnings.ps1`. **Checkpoint 3.** Propose the commit; wait.

## Phase 4 - Lambdas
Tests first (in `Resilience.Tests`; the Lambda projects have no test project and stay thin):
- [ ] **T630** [P] `RetryingHttpHandler` default-construction test: `new RetryingHttpHandler(stub, new HttpRetrySettings(), null)` repeats a 503 (AS4.1). Covered by T602; add here only if T602 did not use the default settings.
- [ ] **T631** [P] A test that both `FakeHttpClientService` classes return a client that repeats (the Lambda projects are referenced from a small test, or the factory method is made `public static` in `Resilience` and tested there; choose at T632 and say which).

Implementation:
- [ ] **T632** In `ImageProcessing.Lambda` and `LinkPreview.Lambda`: reference `LetsTalk.Server.Resilience`; `FakeHttpClientService` returns the retrying client; pass `context.Logger` for the retry warning; no configuration file. (T630, T631; plan D5)
- [ ] **T633** `dotnet build BE/LetsTalk.Server.sln`, `dotnet test BE/LetsTalk.Server.sln`, `CheckWarnings.ps1`. **Checkpoint 4.** Propose the commit; wait.

## Phase 5 - Documents and verification
- [ ] **T640** `CLAUDE.md`: one sentence in the Infrastructure bullet (where the retry rules live and that gRPC uses the channel's service config). Constitution unchanged unless the owner wants the rule there.
- [ ] **T641** Write `deployment.md` if the Lambdas need a redeploy note (they do: the new code only takes effect after the Lambda packages are redeployed) and the three `Resilience` sections.
- [ ] **T642** The owner runs `quickstart.md`; record results in its Result column.
- [ ] **T643** Final `dotnet test BE/LetsTalk.Server.sln`. **Checkpoint 5.** Propose the commit; wait.

The quickstart lives in [quickstart.md](quickstart.md).
