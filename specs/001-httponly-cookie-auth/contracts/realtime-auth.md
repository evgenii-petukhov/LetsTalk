# Contract: authentication of SignalR and gRPC-web

## SignalR (`chatnotifications{{suffix}}.../messagehub`)
- Browser: connects with `withCredentials: true`; the cookie travels on the WebSocket upgrade request. The client no longer calls `AuthorizeAsync` and no longer reads a token.
- Server (`NotificationHub.OnConnectedAsync`):
  1. Read cookie `auth-token` from `Context.GetHttpContext()`.
  2. If present, validate through `IAuthenticationClient.ValidateJwtTokenAsync`; on success `AddConnectionId(accountId, ConnectionId)`.
  3. If the cookie is present but invalid (including a failed validation call), do not register the connection and abort it (`Context.Abort()`), so the client sees a failed connection and returns to sign-in.
  4. If there is no cookie, leave the connection open but unregistered: a client using the explicit credential registers it afterwards with `AuthorizeAsync(token)` (FR-004). The Angular app does not connect without a session (the guards send it to sign-in first).
- `AuthorizeAsync(string token)` stays for the explicit-credential path (frontend-next, tools). Behavior unchanged.
- Reconnect: a new handshake carries the cookie again; nothing to re-send.
- A revoked session cannot reconnect (validation fails).

## File storage gRPC-web (`chatfilestorage{{suffix}}...`)
- Browser: `new FileUploadGrpcEndpointClient(url, null, { withCredentials: true })`; no `authorization` metadata is set.
- Server (`JwtInterceptor`): credential = cookie `auth-token` from `context.GetHttpContext().Request.Cookies` if present, else metadata `authorization` (explicit path). When both are present, the cookie wins (consistent with HTTP). Both absent, or the credential invalid -> `UnauthorizedAccessException` as today. (Spec 002 changes the failure to a gRPC `Unauthenticated` status so the browser can tell it apart.)
- Response signing (`ISignPackageService`) unchanged.

## Credential handed to background work
`ApiController.GetToken()` returns the credential resolved by `JwtMiddleware` (stored in `HttpContext.Items["Token"]`), not the raw `Authorization` header. The value placed into `ImageResizeRequest.Token` / `LinkPreviewRequest.Token` is the bare JWT; consumers keep sending `Authorization: Bearer <token>` to the API.
