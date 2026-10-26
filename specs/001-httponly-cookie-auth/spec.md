# Feature Spec: Keep the sign-in session out of reach of page scripts

**Feature folder**: `specs/001-httponly-cookie-auth`\
**Status**: Clarified, ready for planning\
**Created**: 2026-10-25\
**Follow-up**: `specs/002-short-lived-token-refresh` (shorter sessions, silent renewal)

## Problem
After signing in, the web app stores the user's session credential in the browser storage that any script on the page can read. If an attacker gets a script to run on the page (for example through an injected piece of content), they can copy the credential and use it from anywhere. The credential is also valid for a full year and cannot be cancelled: signing out only forgets it on the user's device, and the server still accepts it.

## Goal
A signed-in user's session credential is never readable by page scripts, and signing out really ends the session.

## User Stories

### US1 - Sign in without exposing the credential (P1)
As a user, I sign in with my email and the code I receive, and I land in my chats as before.
- **AS1.1** Given a valid email and code, when I submit them, then I am taken to my chats and my requests are authenticated.
- **AS1.2** Given I am signed in, when a script on the page inspects everything it can read (browser storage, cookies visible to scripts, responses of later requests), then the session credential is not among it.
- **AS1.3** Given I enter a wrong code, then I stay on the sign-in page and no session is created.
- **AS1.4** Given my browser still holds an expired or revoked session, when I request a login code and sign in, then both work and the stale session is replaced.
- **AS1.5** Given my browser refuses to keep the session (cookies blocked), when I sign in, then I see a message explaining that sign-in cannot be completed, not a redirect loop.

### US2 - Stay signed in across reloads and tabs (P1)
- **AS2.1** Given I am signed in, when I reload the page or open a second tab, then I am still signed in and see my chats.
- **AS2.2** Given I am not signed in, when I open a protected page, then I am sent to the sign-in page.
- **AS2.3** Given I am signed in, when I open the sign-in page, then I am sent to my chats.

### US3 - Real-time messages and file transfers keep working (P1)
- **AS3.1** Given I am signed in, when another user sends me a message, then it appears without reloading.
- **AS3.2** Given I am signed in, when I upload and later view an image or avatar, then both work.
- **AS3.3** Given the live connection drops and reconnects, then I keep receiving messages without signing in again.
- **AS3.4** Given I send an image or a link in a message, when it is processed in the background, then its preview appears as before.

### US4 - Sign out really ends the session (P1)
- **AS4.1** Given I am signed in, when I choose "log out", then I am taken to the sign-in page and cannot reach protected pages.
- **AS4.2** Given I signed out, when anyone replays the credential I had before, then every service rejects it.
- **AS4.3** Given my session is rejected by the server at any time, then the app returns me to the sign-in page instead of showing a broken screen.

### US5 - Existing users and the experimental new front end are not left behind (P2)
- **AS5.1** Given I was signed in before the update, when I open the app, then any old credential kept in browser storage is removed and I am asked to sign in once.
- **AS5.2** Given the experimental front end (`frontend-next`) sends a credential in its current way, then it keeps working unchanged.

## Functional Requirements
- **FR-001** The session credential MUST NOT be readable by scripts running in the browser page at any point after sign-in.
- **FR-002** A successful sign-in MUST establish a session in the browser without the sign-in response exposing the credential to the page.
- **FR-003** The browser MUST send the session automatically with requests to the API, the live-notification service and the file service.
- **FR-004** The services MUST still accept a credential sent in the existing explicit way (request header / call metadata), so that `frontend-next` is unaffected. When both are present, the browser session wins.
- **FR-005** The system MUST provide a sign-out action that ends the session in the browser and invalidates the credential on the server side, for all services.
- **FR-006** The app MUST determine "signed in or not" by asking the server, not by looking for a stored credential, and MUST use that for route protection.
- **FR-007** On startup the app MUST delete any legacy credential kept in browser storage by earlier versions.
- **FR-008** When a service answers "not authenticated", the app MUST clear its local state and return to the sign-in page.
- **FR-009** Only configured front-end origins MAY make credentialed requests to the services. "Any origin" MUST NOT be allowed together with credentials.
- **FR-010** The live-notification connection MUST be tied to the signed-in account without the page handing over the credential.
- **FR-011** Session handling MUST work in local development and in production, with its settings (allowed origins, cookie scope, secure flag) coming from configuration.
- **FR-013** A browser session that is no longer valid (expired, revoked, damaged) MUST be treated as "not signed in": public actions (sign in, request a login code, sign out) MUST still work, protected actions MUST be refused as "not authenticated", and the stale session MUST be removed from the browser by the server, because page scripts cannot remove it.
- **FR-014** After signing in, the app MUST confirm with the server that the session is active, and if the browser does not keep it (for example cookies are blocked), MUST tell the user instead of looping between pages.
- **FR-012** The API MUST be able to hand the signed-in user's credential on to background work (image resizing, link previews) regardless of whether the request arrived with the browser session or with the explicit credential, so those features keep working for cookie-based sessions.

## Non-Functional Requirements
- **NFR-001** The session cookie is HttpOnly, restricted to secure connections outside development, and not sent on cross-site requests.
- **NFR-002** Sign-out and rejected-session handling complete in under 1 second on a normal connection.
- **NFR-003** No new secrets are committed; Swagger/developer tooling remains Development-only.

## Edge Cases
- Browser blocks or clears cookies: the user sees the sign-in page and a clear message, not a loop of redirects.
- Session rejected while a live connection is open: the app closes the connection and the user returns to sign-in (the server does not push a disconnect on sign-out; the connection ends when the app closes it or the page unloads).
- Stale session present when signing in or out: the public action succeeds and the server clears the stale session (FR-013).
- Two tabs, sign out in one: the other tab returns to sign-in on its next request.
- Background services keep calling the API with the explicit credential taken from the message they consume.
- Credential presented in both ways with different values: the browser session wins (FR-004).
- Signing in again while an old session exists: the old session is replaced.
- Users who signed in on another device keep that device's session until it is signed out or expires (see Out of scope).

## Success Criteria
- **SC-001** After sign-in, no credential appears in `localStorage`, `sessionStorage` or any script-readable cookie.
- **SC-002** Replaying a pre-sign-out credential against each service returns "not authenticated".
- **SC-003** All acceptance scenarios US1-US5 pass in the quickstart run.
- **SC-004** `frontend-next` works with a manually supplied credential exactly as before.

## Out of Scope
- Changes to `frontend-next/` (development use only).
- Shorter credential lifetime and silent renewal -> spec 002.
- Protection against cross-site request forgery beyond the cookie restrictions in NFR-001 -> spec 002.
- A "sign out of all devices" action.
- Changes to the email-code sign-in method itself.

## Key Entities
- **Session credential**: proves the account that signed in; issued by the Authentication service; today valid one year.
- **Account**: the signed-in user's identity.

## Clarifications (resolved 2026-10-25)
- **Hosts**: front end `letstalk.petukhov.fyi`; API `chatapi{{suffix}}.petukhov.fyi`; authentication `chatauth{{suffix}}.petukhov.fyi`; file storage `chatfilestorage{{suffix}}.petukhov.fyi`; notifications `chatnotifications{{suffix}}.petukhov.fyi`. `{{suffix}}` is a placeholder replaced at deployment time with the suffix of that deployment (for example `2`). All are subdomains of one registrable domain (`petukhov.fyi`), so they form one site and a session scoped to the parent domain is sent to all of them.
- **Forced re-sign-in**: accepted. Every existing user signs in once after the release.
- **Sign-out scope**: only the device/browser where the user signs out.
- **Other callers**: besides the browser app, back-end services call the API on behalf of a user with the explicit credential (`LetsTalk.Server.API.Client.ApiClient`, used by the image-processing and link-preview consumers; the credential travels inside the broker message). They are not browsers and keep the explicit way (FR-004, FR-012).
- **frontend-next**: development use only, work in progress; not deployed against production.

## Open Questions
None.
