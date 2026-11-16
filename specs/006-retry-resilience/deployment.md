# Deployment notes: retry of short-lived failures

Applies to the release of `specs/006-retry-resilience`. No secret, connection string, database or API contract change.

## What changes
A call between services that fails for a reason that usually passes within seconds is now repeated before it is reported as failed.

| Where | Calls that repeat | Repeated when | Not repeated |
|---|---|---|---|
| ImageProcessing service | the report of a finished image to the API | connection lost or refused, an attempt that takes longer than 30 s, answers 408, 429, 502, 503, 504 | any other answer (400, 401, 403, 404, 500 ...) |
| LinkPreview service | the report of a finished preview to the API, and the download of the page | the same | the same |
| Image and link Lambdas | the same calls | the same, with the default numbers | the same |
| API, FileStorage service, Notifications (calls to the Authentication service) | validating, revoking and issuing a token, ending a session | the Authentication service cannot be reached (gRPC status `Unavailable`) | starting a session, renewing a session, generating a login code, checking a login code (always one attempt), and any refusal by the service |

The pauses double (1 s, 2 s, 4 s, at most 10 s) with a random variation. An answer that states how long to wait (`Retry-After`) is obeyed up to 10 s. There is no circuit breaker or rate limit.

One effect to know: a repeated image or link report may reach the API twice, so a chat member can get the notification twice. The stored data is the same either way.

## Order of the release
1. Deploy the services (ImageProcessing, LinkPreview, API, FileStorage, Notifications). They may go in any order: the change is local to each caller.
2. **Redeploy both Lambda packages** (`LetsTalk.Server.ImageProcessing.Lambda`, `LetsTalk.Server.LinkPreview.Lambda`). They carry their own copy of the code, and nothing changes for them until their packages are replaced. The Lambda packages now include `Polly.Core.dll`.

## Settings
All optional, with defaults in the code; the shipped `appsettings.json` of each service lists them. Change them in the **deployed** `appsettings.json`, or with an environment variable that has the `LetsTalk_` prefix (such a variable wins over the file), for example `LetsTalk_Resilience__Http__MaxRetryAttempts=5`.

```json
"Resilience": {
  "Http": {
    "MaxRetryAttempts": 3,
    "Delay": "00:00:01",
    "MaxDelay": "00:00:10",
    "AttemptTimeout": "00:00:30",
    "TotalTimeout": "00:01:40"
  }
}
```
(`Resilience:Http` in the ImageProcessing and LinkPreview services.)

```json
"Resilience": {
  "Grpc": {
    "MaxAttempts": 4,
    "InitialBackoff": "00:00:01",
    "MaxBackoff": "00:00:10"
  }
}
```
(`Resilience:Grpc` in the API, FileStorage and Notifications services.)

| Setting | Default | Allowed | Meaning |
|---|---|---|---|
| `Http:MaxRetryAttempts` | 3 | 0 to 10 | repeats after the first attempt; `0` switches the repeat off |
| `Http:Delay` | 1 s | 100 ms to 1 min | the first pause; doubles each time |
| `Http:MaxDelay` | 10 s | the `Delay` to 5 min | the longest pause |
| `Http:AttemptTimeout` | 30 s | 1 s to 10 min | the longest wait for one attempt |
| `Http:TotalTimeout` | 100 s | the `AttemptTimeout` to 30 min | the longest wait for the whole call (equal to the time a call could take before this release) |
| `Grpc:MaxAttempts` | 4 | 1 to 5 | all attempts including the first; `1` switches the repeat off |
| `Grpc:InitialBackoff` | 1 s | 100 ms to 1 min | the first pause |
| `Grpc:MaxBackoff` | 10 s | the `InitialBackoff` to 5 min | the longest pause |

A value outside its range stops the service at its start, and the message names the key (as in spec 005). The Lambdas read no configuration; to change their numbers, change the defaults in `HttpRetrySettings` and redeploy.

## What you will see in the log
- **Every repeat** of an HTTP call: a warning `Retry <client>: attempt <n> after <reason>, next try in <s> s`. The client is `ImageResizeRequestConsumer`, `LinkPreviewApiClient` or `HttpClientService`; the reason is `HTTP 503` or an exception type. The Lambda log has the same text with the Lambda's name. Polly's own telemetry adds two more warnings per repeat; none of them shows an address, a token or a message text.
- **gRPC repeats** are not logged by the library. Turn on the `Grpc.Net.Client` category at debug level to see them.
- **A call that still fails** after the last attempt ends as before: the consumer fails, the Lambda fails, the user's request gets the error.

## Limits
- Repeats lengthen the time a failing call takes, up to `TotalTimeout` (100 s) for HTTP. A consumer that waits on a dead API therefore holds its message up to that long.
- A repeat does not help when the other side is down for longer than the total time. The message broker's own redelivery was left out of this release (see `research.md`).

## Rollback
Set `Resilience:Http:MaxRetryAttempts` to `0` and `Resilience:Grpc:MaxAttempts` to `1` in the deployed `appsettings.json` of each service and restart it, or deploy the previous build. For the Lambdas, redeploy the previous package. Nothing needs to be cleaned up.
