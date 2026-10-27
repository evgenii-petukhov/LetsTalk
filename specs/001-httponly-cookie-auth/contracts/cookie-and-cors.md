# Contract: session cookie and CORS

## Cookie `auth-token`
| Attribute | Production | Development |
|---|---|---|
| Name | `auth-token` (configurable) | same |
| Value | the JWT | same |
| HttpOnly | yes | yes |
| Secure | yes | configurable (default yes) |
| SameSite | `Strict` | `Strict` |
| Domain | `petukhov.fyi` (configurable) | empty (host-only) |
| Path | `/` | `/` |
| Expires | the JWT's expiry (persistent cookie; spec 002 shortens it) | same |

- Set only by the API on successful `email-login`; cleared by `logout` using identical Domain/Path.
- Configuration (section `AuthCookie`, bound in the API, Notifications and File storage): `Name`, `Domain`, `Secure`, `SameSite`. Defaults live in `AuthCookieSettings`; deployments override them through configuration. `appsettings.json` is not edited. The settings are validated when the service starts (`AddAuthCookieSettings`): `Name` not empty, `SameSite` one of `None`, `Lax`, `Strict`, `None` only together with `Secure=true`, `Domain` a bare host name (no scheme, port or path). A wrong value stops the service instead of silently producing a cookie the browser drops.

## CORS (API, Notifications, File storage)
- Policy: `WithOrigins(<AllowedOrigins from configuration>)`, `AllowAnyHeader()`, `AllowAnyMethod()`, `AllowCredentials()`.
- `AllowedOrigins` (configuration, a list):
  - Production: `https://letstalk.petukhov.fyi`
  - Development: the dev front-end origin (`https://127.0.0.1:4200`, set in each service's `appsettings.Development.json`)
- `AllowAnyOrigin` MUST NOT be used with credentials.
- File storage (gRPC-web) additionally exposes the gRPC-web headers (`grpc-status`, `grpc-message`, `grpc-status-details-bin`) and allows `x-grpc-web`, `x-user-agent` request headers (`AllowAnyHeader`). The previous policy did not expose the status headers; the browser needs them to read a gRPC error code such as `UNAUTHENTICATED` (used by spec 002).
- Outside the Development environment an empty `AllowedOrigins` list fails at startup. `RunAll.ps1` therefore sets `ASPNETCORE_ENVIRONMENT=Development`; the services read their `appsettings.Development.json` (with `Cors:AllowedOrigins`) because each `Program.cs` adds `appsettings.{Environment}.json` from the executable folder, which the default lookup misses when the executables are started without a working directory.
- Notifications (SignalR) uses the same policy; SignalR needs `AllowCredentials()` for the WebSocket handshake with cookies.
- Authentication service: unchanged (called only by back-end services).

## Back-end callers (no cookie)
Services using `LetsTalk.Server.API.Client.ApiClient` keep sending `Authorization: Bearer <token>`. They are not browsers and are not subject to CORS.
