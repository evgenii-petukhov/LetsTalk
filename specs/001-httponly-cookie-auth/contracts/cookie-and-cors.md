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
- Configuration (API `appsettings.json`, section `AuthCookie`): `Name`, `Domain`, `Secure`, `SameSite`.

## CORS (API, Notifications, File storage)
- Policy: `WithOrigins(<AllowedOrigins from configuration>)`, `AllowAnyHeader()`, `AllowAnyMethod()`, `AllowCredentials()`.
- `AllowedOrigins` (configuration, a list):
  - Production: `https://letstalk.petukhov.fyi`
  - Development: the dev front-end origin (`http://127.0.0.1:4200`)
- `AllowAnyOrigin` MUST NOT be used with credentials.
- File storage (gRPC-web) additionally exposes the gRPC-web headers (`grpc-status`, `grpc-message`, `grpc-status-details-bin`) as it does today, and allows `x-grpc-web`, `x-user-agent` request headers.
- Notifications (SignalR) uses the same policy; SignalR needs `AllowCredentials()` for the WebSocket handshake with cookies.
- Authentication service: unchanged (called only by back-end services).

## Back-end callers (no cookie)
Services using `LetsTalk.Server.API.Client.ApiClient` keep sending `Authorization: Bearer <token>`. They are not browsers and are not subject to CORS.
