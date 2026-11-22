# Research: Retry options

## Where calls are made today
| Place | Call | Registration |
|---|---|---|
| `ImageResizeRequestConsumer` | `ApiClient.SetImagePreviewAsync` (PATCH, spec 003) | named client `ImageResizeRequestConsumer` (`AddHttpClient`; it had a debug certificate bypass, removed later, see D10 in `plan.md`) |
| `LinkPreviewRequestConsumer` | `ApiClient.SetLinkPreviewAsync` (PATCH) | named client `HttpClientService`, **shared** with `DownloadService` (GET of arbitrary web pages) |
| `ImageProcessingLambda`, `LinkPreviewLambda` | the same two PATCH calls, and the page download | no DI: `new HttpClient()` in `FakeHttpClientService` |
| `AuthenticationClient` (used by the API) | 8 gRPC calls | `AddGrpcClient<JwtTokenGrpcServiceClient>` and `<LoginCodeGrpcServiceClient>` |

Nothing in the solution references Polly or `Microsoft.Extensions.Http.Resilience`; MassTransit consumers have no retry configured.

## Options
| | Option | Covers | Verdict |
|---|---|---|---|
| A | `Microsoft.Extensions.Http.Resilience` (`AddResilienceHandler`; Polly v8 inside) | named HTTP clients with DI | **Chosen** for the three HTTP clients, with a hand-built pipeline (retry + timeouts) instead of `AddStandardResilienceHandler`: the standard one adds a circuit breaker and a rate limiter. A breaker on a client that downloads arbitrary sites would let one dead site block all of them; in a Lambda it would live for one invocation only. |
| B | gRPC `ServiceConfig` with `RetryPolicy` (built into `Grpc.Net.Client`, no new package) | gRPC clients | **Chosen** for gRPC. Per-method policies, `Unavailable` only, retry throttling available. Not combined with A on the same client (retries would multiply). |
| C | MassTransit `UseMessageRetry`/`UseDelayedRedelivery` | whole consumer | Not chosen now: it reruns the image resize or link download, and it does not help the Lambdas or gRPC. Can be added later if messages are still lost. |
| D | Polly v8 `ResiliencePipeline` used by hand | anything, including Lambdas (no DI) | **Chosen** for the Lambdas, as a delegating handler built from the same pipeline definition as A. |

Other libraries (Refit with Polly, Flurl, `Microsoft.Extensions.Resilience`) wrap Polly and add nothing needed here.

## Safety of repeating each call
| Call | Effect on the server | Repeat? |
|---|---|---|
| `SetImagePreview` (API) | replaces the stored preview, then publishes a notification to each chat member and clears the chat's message cache | yes; a repeat may notify twice |
| `SetLinkPreview` (API) | stores the preview; when it exists already, falls back to the existing preview by URL (a `catch` in the handler); then notifies and clears the cache | yes; may notify twice |
| link page download (GET) | none | yes |
| gRPC `ValidateJwtToken` | reads the cache | yes |
| gRPC `RevokeJwtToken` | removes a token from the cache | yes (a second revoke is a no-op) |
| gRPC `GenerateJwtToken` | stores a new token; a repeat after a lost answer leaves one unused token that expires | yes |
| gRPC `EndSession` | ends the session of a refresh token | yes (to be proven by a test, task T625) |
| gRPC `CreateSession` | creates a refresh session; a repeat after a lost answer leaves an orphan session | no |
| gRPC `RefreshSession` | rotates the refresh token; a repeat after a lost answer is a reuse of the replaced token, which ends the whole chain (spec 002) | no |
| gRPC `GenerateLoginCode` | a repeat returns "code already created" (`isCodeCreated = false`), and the API would then not send the e-mail | no |
| gRPC `ValidateLoginCode` | a successful check may consume the code, so a repeat after a lost answer says "invalid" | no |

gRPC retries only on status `Unavailable`, where the request did not reach the server or the server refused it before handling, which is the safe moment even for the "no" rows; the "no" rows stay one-shot because the cost of a wrong guess is a locked-out user.

## Technical notes
- `AddResilienceHandler("name", builder => ...)` receives a `ResiliencePipelineBuilder<HttpResponseMessage>` from `Polly.Core`, so one function `Configure(builder, settings, onRetry)` can serve both the DI clients and the Lambda's handler.
- Retry resends the same `HttpRequestMessage`. NSwag's `ApiClient` builds a `ByteArrayContent` (re-readable), so a PATCH body is safe to resend; the test of task T601 proves it for a request body.
- gRPC retry: `GrpcChannelOptions.ServiceConfig` with `MethodConfig { Names = { new MethodName { Service, Method } }, RetryPolicy = new RetryPolicy { MaxAttempts (at most 5), InitialBackoff, MaxBackoff, BackoffMultiplier, RetryableStatusCodes = { StatusCode.Unavailable } } }`. `AddGrpcClient(...).ConfigureChannel(o => o.ServiceConfig = ...)`. The gRPC client library retries only with HTTP/2 and buffers the request, which is fine for these small messages.
- The existing `HttpClient` default timeout is 100 seconds; the defaults of FR-003 keep that.
- Delay with `Retry-After`: Polly's `DelayGenerator` can return the header's value; capped by the maximum pause.
- Polly accepts a `TimeProvider`, so tests use `FakeTimeProvider` (package `Microsoft.Extensions.TimeProvider.Testing`, already used by the keep-alive tests) and never wait.
