# Plan: Retry short-lived failures of service-to-service calls

**Spec**: [spec.md](spec.md) | **Research**: [research.md](research.md)

## Design decisions
- **D1 - One new project, `backend/Infrastructure/LetsTalk.Server.Resilience`** (and `.Tests`). It depends only on `Polly.Core`, so the Lambdas can reference it. It holds:
  - `HttpRetrySettings` (`MaxRetryAttempts` 3, `Delay` 1 s, `MaxDelay` 10 s, `AttemptTimeout` 30 s, `TotalTimeout` 100 s) and `GrpcRetrySettings` (`MaxAttempts` 4, `InitialBackoff` 1 s, `MaxBackoff` 10 s). Defaults are carried by the classes (spec 005). They sit here, not in `LetsTalk.Server.Configuration`, so the Lambdas do not pull in the configuration project and its ASP.NET reference; their validators live in `LetsTalk.Server.Configuration/Validation` as usual (one `IValidateOptions` each, registered with `AddValidatedSettings`).
  - `HttpRetryPipeline.Configure(ResiliencePipelineBuilder<HttpResponseMessage>, HttpRetrySettings, Action<RetryEvent>? onRetry)`: the one definition of FR-001 to FR-004 (the total timeout outermost, then retry, then the attempt timeout inside it). The retry predicate is public (`HttpRetryPipeline.ShouldRetry`) and tested on its own.
  - `RetryingHttpHandler : DelegatingHandler`, running a pipeline built by `Configure`. For callers without DI (the Lambdas) and for tests.
  - `RetryEvent(Attempt, Reason, Delay)`, what a repeat tells the log.
  - Not here: the gRPC `ServiceConfig` builder. It needs `Grpc.Net.Client`, which the Lambdas must not carry, so it lives in `LetsTalk.Server.Authentication.Client` (D3).
- **D2 - HTTP clients in the services (A).** `AddResilienceHandler("retry", b => HttpRetryPipeline.Configure(b, settings, log))` on:
  1. `ImageResizeRequestConsumer` (existing named client, the API callback),
  2. a **new** named client for the link-preview API callback (`LinkPreviewApiClient`), used by `LinkPreviewRequestConsumer`,
  3. `HttpClientService` (the page download), used by `DownloadService` only.
  The consumer gets its client from `IHttpClientFactory`. Today `LinkPreviewRequestConsumer` calls `IHttpClientService.GetHttpClient()` for both jobs; it will use the new named client for the API call (plan note in T631). Packages: `Microsoft.Extensions.Http.Resilience` in `ImageProcessing.Service` and `LinkPreview` (the latter also for the `Utility` code path unchanged).
- **D3 - gRPC (B).** A new `GrpcRetryServiceConfig.Create(GrpcRetrySettings)` in `LetsTalk.Server.Authentication.Client` builds the `ServiceConfig` of FR-007 from the generated service descriptors (never from typed strings), listing only the four safe methods. `AddAuthenticationClientServices` gets `ConfigureChannel(o => o.ServiceConfig = GrpcRetryServiceConfig.Create(settings))` on both gRPC clients. No resilience handler on gRPC clients (research: retries would multiply). The settings are read from `Resilience:Grpc`.
- **D4 - Repeat of the two result reports is accepted** (spec FR-005, A2): the handlers replace the preview, so the data stays right; the cost is a possible second notification. A request id or an idempotency key was considered and rejected as a change of the API contract (FR-011).
- **D5 - Lambdas (D).** In both Lambdas, `FakeHttpClientService.GetHttpClient()` returns `new HttpClient(new RetryingHttpHandler(new HttpClientHandler(), new HttpRetrySettings(), onRetry))`, so the report and the download both repeat. `onRetry` writes to `context.Logger`. The Lambdas do not read configuration; they use the defaults (AS4.1). The Lambdas' `ProcessImageAsync` and `GenerateAsync` pass their logger in.
- **D6 - Settings sections.** `Resilience:Http` and `Resilience:Grpc` in the `appsettings.json` of `LetsTalk.Server.ImageProcessing.Service`, `LetsTalk.Server.LinkPreview` (HTTP only) and `LetsTalk.Server.API` (gRPC only). They list every key with the default value. `ShippedSettingsTests` gains the two section types and the LinkPreview file (which it does not link yet); the test must accept a nested section path (`Resilience:Http`).
- **D7 - Logging.** The retry callback logs through a `[LoggerMessage]` method (`Retry {Target}: attempt {Attempt} after {Reason}`), where `Target` is the client name and `Reason` the exception type or `HTTP {status}`. No address, no headers (FR-010). gRPC retries are not logged by the library; the `Grpc.Net.Client` category logs them at debug level, which is documented in the quickstart and enough for this feature.
- **D8 - Breaker and rate limiter are left out** (FR-006, research row A).
- **D9 - Cancellation** passes through: the Polly pipeline sees the caller's token and stops without a retry (FR-004).

## Constitution check
- 1 Security first: no secrets are added; the log line carries neither address nor tokens (FR-010).
- 2 Generated code: `ApiClient.cs` and the gRPC stubs are not touched.
- 3 Provider-neutral persistence and registration: no persistence change. The `AddResilienceHandler` lines stay in each service's own registration class (`<Feature>ServiceCollectionExtensions`), because the pipeline itself is shared through `HttpRetryPipeline`; no new registration class is needed.
- 4 Test-first: every task group lists its tests before the code.
- 5 Front end untouched. 6 No contract change (FR-011). 7 This folder is committed before the code.
- Deviation: the two settings classes live in the new `Resilience` project, not in `LetsTalk.Server.Configuration` (D1, reason: Lambdas). Their validators stay in `Configuration`.

## Files
New: `Infrastructure/LetsTalk.Server.Resilience/` (`HttpRetrySettings.cs`, `GrpcRetrySettings.cs`, `HttpRetryPipeline.cs`, `RetryingHttpHandler.cs`, `RetryEvent.cs`), `Infrastructure/LetsTalk.Server.Resilience.Tests/`, `GrpcRetryServiceConfig.cs` in `Authentication.Client`, `Configuration/Validation/HttpRetrySettingsValidator.cs`, `GrpcRetrySettingsValidator.cs` (and tests).
Changed: `ImageProcessingServiceCollectionExtensions.cs`, `LinkPreviewServiceCollectionExtensions.cs`, `LinkPreviewRequestConsumer.cs`, `AuthenticationClientServiceCollectionExtensions.cs`, the two `FakeHttpClientService.cs` and Lambda classes, the three `appsettings.json` files, `ShippedSettingsTests.cs` and its csproj links, `LetsTalk.Server.sln`, four csproj files (package or project references), `CLAUDE.md` (one sentence in the Infrastructure bullet).
