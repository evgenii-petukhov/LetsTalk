# Deployment notes: HttpOnly cookie session

Applies to the release of `specs/001-httponly-cookie-auth`. Verified on the hosting on 2026-10-27 (quickstart Q1-Q8, Q10-Q12, Q14).

## What changes for users
- Every user signs in once after the release. The old token in `localStorage` is removed on startup, and the application no longer uses it.
- The session is an HttpOnly cookie (`auth-token`, `SameSite=Strict`, `Secure`). Logging out revokes the token on the server.

## Settings to add to the deployed `appsettings.json` (or as environment variables)

`{{domain}}`, `{{frontend}}` and `{{suffix}}` below are placeholders for the registrable domain, the web app's sub-domain and the deployment suffix. `{{domain}}` and `{{suffix}}` are substituted into `frontend/src/environments/environment.prod.ts` by the deployment command line, so the domain is not fixed anywhere in the repository. Use one value for the front end and for all services; if any `{{` remains in the built front end, the build is not ready to deploy.

Add these sections to the existing file; do not commit them to the repository. The same values as environment variables use a double underscore, for example `Cors__AllowedOrigins__0`. Do not set the same key in both places: the deployed `appsettings.json` is read last and wins.

**API**
```json
"AuthCookie": {
  "Domain": "{{domain}}"
},
"Cors": {
  "AllowedOrigins": [ "https://{{frontend}}.{{domain}}" ]
}
```

**Notifications**
```json
"Cors": {
  "AllowedOrigins": [ "https://{{frontend}}.{{domain}}" ]
}
```

**FileStorage**
```json
"Cors": {
  "AllowedOrigins": [ "https://{{frontend}}.{{domain}}" ]
}
```

**Authentication**: nothing to add (not called by the browser).

Notes:
- `AllowedOrigins` is the exact origin of the web app: scheme and host, no trailing slash. Add more entries for more front ends.
- `AuthCookie:Domain` is needed only on the API (it sets and clears the cookie). With the parent domain `{{domain}}`, one cookie is sent to all `chat*{{suffix}}` hosts. `Secure=true`, `SameSite=Strict` and the name `auth-token` are the defaults.
- Without `Cors:AllowedOrigins`, the API, Notifications and FileStorage refuse to start outside the Development environment (`Cors:AllowedOrigins must contain at least one origin ...`). This is intentional.
- Set `AuthCookie:Domain` before the first rollout. If it is changed later, a cookie issued without it stays as a separate host-only cookie, so users must sign out or clear it once.
- Do not run the services with `ASPNETCORE_ENVIRONMENT=Development` on the hosting to avoid the settings above.

## Hosts and reverse proxy
- The web app and all services must be under one registrable domain (`{{domain}}`) and use `https`; a cookie is bound to the site and the scheme, so a service on plain `http` would not receive it.
- If Nginx sits in front, it must pass `Set-Cookie` through unchanged and must not add its own `Access-Control-Allow-Origin` header; the services send the correct CORS headers.
- The Notifications hub uses WebSockets (`wss://`), so the proxy has to allow the upgrade, as before.

## Unchanged
- Back-end callers (image processing, link preview consumers and Lambdas) still send `Authorization: Bearer <token>`.
- `frontend-next` is untouched and keeps sending its explicit token.
- Token revocation is kept in the Authentication service's cache: shared in Redis mode, per process in memory mode.

## Quick checks after the rollout
```
# the cookie carries the parent domain
curl -si -X POST https://<api-host>/api/Authentication/logout | grep -i set-cookie
# the services allow the web app origin with credentials
curl -si -X OPTIONS https://<api-host>/api/Chat -H "Origin: https://{{frontend}}.{{domain}}" -H "Access-Control-Request-Method: GET" | grep -i access-control-allow
```
The first command should show `domain={{domain}}`; the second should show the origin and `access-control-allow-credentials: true`.
