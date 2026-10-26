# Contract: authentication of SignalR and gRPC-web

## SignalR (`chatnotifications{{suffix}}.../messagehub`)
- Browser: connects with `withCredentials: true`; the cookie travels on the WebSocket upgrade request. The client no longer calls `AuthorizeAsync` and no longer reads a token.
- Server (`NotificationHub.OnConnectedAsync`):
  1. Read cookie `auth-token` from `Context.GetHttpContext()`.
  2. If present, validate through `IAuthenticationClient.ValidateJwtTokenAsync`; on success `AddConnectionId(accountId, ConnectionId)`.
  3. If absent or invalid, do not register the connection and abort it (`Context.Abort()`), so the client sees a failed connection and returns to sign-in.
- `AuthorizeAsync(string token)` stays for the explicit-credential path (frontend-next, tools). Behavior unchanged.
- Reconnect: a new handshake carries the cookie again; nothing to re-send.
- A revoked session cannot reconnect (validation fails).

## File storage gRPC-web (`chatfilestorage{{suffix}}...`)
- Browser: `new FileUploadGrpcEndpointClient(url, null, { withCredentials: true })`; no `authorization` metadata is set.
- Server (`JwtInterceptor`): credential = metadata `authorization` if present and non-empty (explicit path), else cookie `auth-token` from `context.GetHttpContext().Request.Cookies`. Both absent -> `UnauthorizedAccessException` as today. When both are present, the cookie wins (consistent with HTTP).
- Response signing (`ISignPackageService`) unchanged.

## Credential handed to background work
`ApiController.GetToken()` returns the credential resolved by `JwtMiddleware` (stored in `HttpContext.Items["Token"]`), not the raw `Authorization` header. The value placed into `ImageResizeRequest.Token` / `LinkPreviewRequest.Token` is the bare JWT; consumers keep sending `Authorization: Bearer <token>` to the API.
