# Research: HttpOnly cookie session

Findings that back the decisions in `plan.md`. Facts were verified in the code on 2026-10-25.

## R1. Where the credential lives today
- Angular: `localStorage['auth-token']`, only through `frontend/src/app/services/token-storage.service.ts`. Elsewhere `localStorage.clear()` is used for logout and on 401.
- Sent three different ways:
  - HTTP: `Authorization: Bearer <jwt>` via `auth.interceptor.ts`.
  - gRPC-web (file storage): call metadata `authorization: <raw jwt>` in `file-storage.service.ts`.
  - SignalR: after connecting, hub method `AuthorizeAsync(token)`; repeated on reconnect.
- Issued by `EmailLoginCommandHandler` -> Authentication gRPC `GenerateJwtToken`, returned in the JSON body of `POST api/Authentication/email-login`.

## R2. Server-side validation today
- API: custom `JwtMiddleware` (no `AddJwtBearer`). Takes the last space-separated part of `Authorization`, validates through the Authentication gRPC service, sets `HttpContext.Items["AccountId"]`. A missing token is allowed through; the custom `[Authorize]` filter returns 401 when `AccountId` is missing.
- File storage: `JwtInterceptor` reads gRPC metadata `authorization`.
- Notifications: `NotificationHub.AuthorizeAsync(token)` registers `ConnectionId -> accountId`. An invalid token is silently ignored.
- Authentication: `JwtStorageService` creates/validates HS256 JWTs with a single `id` claim, lifetime 1 year. `MemoryCacheTokenService` / `RedisCacheTokenService` cache `token -> accountId` until expiry.

## R3. Logout cannot be done by deleting the cache entry (important)
`GetAccountIdAsync` on a cache miss falls back to `JwtStorageService.GetStoredTokenAsync`, which only checks the signature and expiry, then re-caches the token. So removing the cache entry does **not** revoke a token.
**Decision input**: revocation needs an explicit denylist, checked before the cache and the signature check, with a lifetime equal to the token's remaining validity (Redis key with TTL, or a memory-cache entry with the same expiry).

## R4. Back-end services also call the API with the explicit credential
- `ApiController.GetToken()` returns `HttpContext.Request.Headers.Authorization` (used in `MessageController` to put the credential into `ImageResizeRequest` / `LinkPreviewRequest`).
- `ImageResizeRequestConsumer` and `LinkPreviewRequestConsumer` build `LetsTalk.Server.API.Client.ApiClient` (NSwag C# client) and call `SetImagePreview` / `SetLinkPreview` with `Authorization: Bearer <token from message>`. The Lambdas do the same.
- Consequence: with a cookie session the `Authorization` header is empty, so `GetToken()` must return the credential the middleware resolved (cookie or header). The Bearer path must stay.

## R5. Same-site feasibility
Hosts: `{{frontend}}`, `chatapi{{suffix}}`, `chatauth{{suffix}}`, `chatfilestorage{{suffix}}`, `chatnotifications{{suffix}}` under `{{domain}}`; `{{suffix}}` and `{{domain}}` are replaced at deployment time (for example `2`; the domain may change, so it is not fixed in the repository). Same registrable domain => same *site*. A cookie with `Domain={{domain}}` (or `.{{domain}}`) and `SameSite=Strict` is sent by the browser on requests from `{{frontend}}.{{domain}}` to every `chat*{{suffix}}` host, including WebSocket handshakes.
- Requests are still *cross-origin*, so CORS must allow the front-end origin explicitly with `AllowCredentials()`. `AllowAnyOrigin` is invalid with credentials (ASP.NET Core throws at startup, browsers reject `*`).
- The Authentication service (`chatauth{{suffix}}`) is only called by back-end services, not by the browser; its CORS policy can stay unchanged unless the browser calls it (it does not: the browser uses API, file storage and notifications).
- Local dev: `127.0.0.1` with different ports is one site; `localhost` vs `127.0.0.1` are different. A `Domain` attribute is not valid for an IP address, so in Development the cookie is host-only (shared across ports). `Secure` cookies are accepted on `http://127.0.0.1`/`localhost` by modern browsers (treated as potentially trustworthy), so `Secure` can stay on if dev uses HTTPS or a browser that allows it; otherwise relax by configuration.

## R6. Library support
- `@microsoft/signalr`: `IHttpConnectionOptions.withCredentials` exists (`node_modules/@microsoft/signalr/dist/esm/IHttpConnectionOptions.d.ts:41`). With `skipNegotiation: true` + WebSockets, the browser sends cookies on the upgrade request for the host. Server reads `Context.GetHttpContext().Request.Cookies`.
- `grpc-web`: `GrpcWebClientBaseOptions.withCredentials` exists. The generated client takes `(hostname, credentials, options)` (`protos/file_upload_grpc_web_pb.js:34`), so: `new FileUploadGrpcEndpointClient(url, null, { withCredentials: true })`.
- Angular `HttpClient`: `withCredentials: true` on the request (set in the interceptor).
- Server side of gRPC-web: `context.GetHttpContext()` exposes `Request.Cookies` inside the interceptor (`ServerCallContext` extension `GetHttpContext()` from `Grpc.AspNetCore.Server`).

## R7. Alternatives considered
| Option | Verdict |
|---|---|
| Keep token in `localStorage` | Rejected: readable by any script (the problem). |
| Keep token only in memory (JS variable) | Rejected: lost on reload; needs a refresh mechanism (spec 002) and still exposes the token to scripts while the page lives. |
| HttpOnly `SameSite=Strict` cookie scoped to the parent domain | **Chosen**: not readable by scripts, works for all services on the same site, small change. |
| Reverse proxy so everything is one origin | Rejected for now: infrastructure change, not needed since hosts share a site. |
| Backend-for-frontend (server-side session) | Rejected: larger architectural change. |

## R9. Added by the analyze pass
- `JwtMiddleware.cs:24,28` throws for any invalid token, and `CustomExceptionMiddleware.cs:57` maps that to 401 on every endpoint, anonymous ones included. This was harmless with `localStorage` (the client could clear its token) but would lock users out with an HttpOnly cookie. Hence FR-013.
- `StoreService.getLoggedInUser()` (`store.service.ts:124`) returns the NgRx-cached profile when present, so it is not a valid session probe; use `ApiService.getProfile()`.
- The `frontend-next` proxy route forwards only `authorization`, `content-type` and `accept`, so cookies never reach the API through it.

## R8. Other observations (not part of this feature)
- All CORS policies are `AllowAnyOrigin`.
- Uncommitted working-tree changes enable Swagger in every environment and contain real-looking secrets in `appsettings.json`: must not be committed (constitution, principle 1).
- `JwtMiddleware` throws `UnauthorizedAccessException` for an invalid token; mapped to 401 by the custom exception handling middleware.
