# Deployment notes: short-lived sessions with silent renewal

Applies to the release of `specs/002-short-lived-token-refresh`, on top of the notes of [spec 001](../001-httponly-cookie-auth/deployment.md). Use the same placeholders as there: `{{domain}}`, `{{frontend}}` and `{{suffix}}`. How the pieces work together is drawn in [token-refresh-flow.md](token-refresh-flow.md).

## What changes for users and for the operator
- The `auth-token` cookie now holds a JWT that lives 15 minutes. A second cookie, `auth-refresh`, renews it silently for as long as the user is active (30 days idle, 90 days at most per sign-in). Users do not notice.
- **Nobody has to sign in again at the release.** The access tokens issued by spec 001 stay valid until their own expiry (up to a year), because the signature check does not look at the lifetime. A browser that holds such a token has no refresh cookie, so it signs in again when that token expires. The old tokens can be stolen for as long as they live.
- To end all old sessions at once, change `JwtSettings:Key` of the Authentication service when you roll out. Every user then signs in once. Do this only if you want it: it also invalidates the background tokens that are in the queue at that moment, so previews of messages sent just before the release may fail.

## Roll out the back end and the front end together
- A front end from before this release does not know about renewal. Against the new back end it would treat the first expired access token as a sign-out, so users would be sent to the sign-in page every 15 minutes.
- The new front end against an old back end calls `POST api/Authentication/refresh`, which does not exist there.
- If you cannot deploy both at once, deploy the back end first and the front end right after it.

## Settings

**Authentication service** (all optional, the defaults are in the code): in the deployed `appsettings.json`, or as environment variables with a double underscore, for example `JwtSettings__AccessTokenLifetime`. Do not set the same key in both places: the deployed `appsettings.json` is read last and wins.
```json
"JwtSettings": {
  "AccessTokenLifetime": "00:15:00",
  "BackgroundTokenLifetime": "01:00:00",
  "RefreshIdleLifetime": "30.00:00:00",
  "RefreshAbsoluteLifetime": "90.00:00:00",
  "RefreshReuseGrace": "00:00:30"
}
```
The Development file of the repository sets the access and background lifetimes to one day; it is not read on the hosting.

**API**: `AuthCookie:RefreshName` (default `auth-refresh`) and `AuthCookie:RefreshPath` (default `/api/Authentication`) are optional. Both are checked when the service starts: the names must differ and the path must start with `/`.

**Front end** (build time): `environment.session.accessTokenLifetimeInSeconds` in `frontend/src/environments/environment.prod.ts` (default 900) must equal `JwtSettings:AccessTokenLifetime`. With a longer value the app renews late: the 401 and the notifications reconnect still renew the session, but one request fails first.

**`Cors:AllowedOrigins`** (API and Notifications, from spec 001) now does a second job: it is the list that the origin check uses. It must contain exactly the origin of the web app, `https://{{frontend}}.{{domain}}`. Without it, every state-changing request from the browser answers `403` and the notifications connection is refused. The API and Notifications services refuse to start without it outside Development.

## Hosts and reverse proxy
- **Cookie path.** The refresh cookie is sent only to the path in `AuthCookie:RefreshPath`. If a proxy publishes the API under another prefix than the one the service sees, set the path the browser sees, otherwise the cookie is never sent and every renewal fails.
- **Headers.** The proxy must pass `Origin`, `Referer` and `Set-Cookie` on unchanged. The origin check reads `Origin`, or the origin of `Referer` when `Origin` is absent.
- The WebSocket upgrade for the notifications hub is unchanged.

## Redis is recommended
- With `Features:CachingMode` set to `redis`, the refresh sessions are shared between instances and survive a restart of the Authentication service.
- With the in-memory cache, a restart of the Authentication service ends every session and users sign in again within 15 minutes. This is a change: the one-year tokens of spec 001 were checked by their signature and survived a restart. A second Authentication instance would not know the first one's sessions, so more than one instance needs Redis.
- All lifetimes are checked on the server that issues and validates the tokens, so a clock difference between hosts does not matter.

## What sign-out does not do
- Sign-out ends the refresh chain and revokes the access token that came with the request. Access tokens issued by earlier refreshes, and a background token already handed to the queue, stay valid until they expire (15 minutes and 1 hour). That is the price of short-lived tokens that are not looked up on every request.

## What to watch
- The Authentication service logs a warning, "A refresh token that was already replaced was presented...", with a chain id and an account id, never a token value. One now and then can be a double click that missed the 30-second grace period. Many for one account mean a copied token.

## Quick checks after the rollout
```
# sign-in sets two cookies (use a real code); look at both Set-Cookie headers
curl -si -X POST https://<api-host>/api/Authentication/email-login -H "Origin: https://{{frontend}}.{{domain}}" -H "Content-Type: application/json" -d '{"email":"...","code":1234,"antiSpamToken":...}' | grep -i set-cookie

# a cookie request from a foreign origin is refused
curl -si -X POST https://<api-host>/api/Authentication/refresh -H "Origin: https://evil.example" -H "Cookie: auth-refresh=x" | head -1
```
The first command should show `auth-token` with `path=/` and `auth-refresh` with `path=/api/Authentication`, both `httponly` and `secure`; the second should answer `403`. The manual checks R1-R9 are in [quickstart.md](quickstart.md).
