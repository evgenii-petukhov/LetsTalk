# Feature Spec: Short-lived sessions with silent renewal

**Feature folder**: `specs/002-short-lived-token-refresh`\
**Status**: Clarified (assumptions confirmed 2026-10-25), ready for implementation after spec 001\
**Created**: 2026-10-25\
**Depends on**: `specs/001-httponly-cookie-auth` (cookie session, logout, revocation)

## Problem
After spec 001 the session credential cannot be read by page scripts, but it is still valid for a full year. Anyone who obtains it by other means (a lost device, a leaked log, a copied cookie) can use it for a year, and the only way to end it is for the owner to sign out on that device. Also, once a page is compromised, an attacker can make requests as the user from the page for as long as the credential lives. Cookie-based sessions also open the door to cross-site request forgery, which spec 001 only mitigates through browser cookie restrictions.

## Goal
A stolen or leaked session stops working quickly, the user is not asked to sign in again while they are actively using the app, and requests that carry the browser session can only come from our own web app.

## User Stories

### US1 - Stay signed in while using the app (P1)
As a user, I keep using the app for days without being asked to sign in, even though each credential only lasts minutes.
- **AS1.1** Given I am signed in and the short credential expires while I am using the app, when I do anything (open a chat, send a message, upload a file), then the action succeeds without me noticing.
- **AS1.2** Given I leave the app closed for less than the maximum idle period and reopen it, then I am still signed in.
- **AS1.3** Given I leave the app closed longer than the maximum idle period, then I am asked to sign in.
- **AS1.4** Given two tabs are open and both need renewal at the same moment, then both keep working and I am not signed out.

### US2 - A stolen session dies quickly (P1)
- **AS2.1** Given someone copies the short credential, when its lifetime passes, then it is rejected everywhere.
- **AS2.2** Given someone copies the renewal credential and uses it after the legitimate app already used it, then the whole session is ended for both parties and the user must sign in again.
- **AS2.3** Given I sign out, then the renewal credential is also unusable.

### US3 - Live connection survives renewals (P1)
- **AS3.1** Given the live connection to notifications is open for hours, when the short credential expires, then messages keep arriving.
- **AS3.2** Given the live connection drops after the short credential has expired, when the app reconnects, then it renews the session first and reconnects successfully.

### US4 - Only our web app can act with the browser session (P2)
- **AS4.1** Given a page on another website makes a request that carries my browser session and changes data, then the request is rejected.
- **AS4.2** Given the request comes from our web app, then it is accepted as before.
- **AS4.3** Given a back-end service or the experimental front end calls with an explicit credential and no browser session, then the origin check does not apply.

### US5 - Background work keeps working (P1)
- **AS5.1** Given I send an image or a link and the processing finishes several minutes later, then the preview still appears, although my short credential has expired by then.

## Functional Requirements
- **FR-001** The credential used on every request MUST expire within a configurable short period (default 15 minutes).
- **FR-002** Signing in MUST also give the browser a separate renewal credential that is not readable by page scripts and is sent only to the sign-in/renewal/sign-out endpoints.
- **FR-003** The renewal credential MUST have a configurable maximum idle period (default 30 days) that restarts each time it is used, and a configurable absolute maximum lifetime for a session (default 90 days).
- **FR-004** The app MUST renew the session automatically when a request is rejected as not authenticated, retry the original request once, and only then return the user to sign-in.
- **FR-005** Each renewal MUST replace the renewal credential with a new one (the old one stops working after a short grace period of 30 seconds, so concurrent tabs do not fail).
- **FR-006** Presenting a renewal credential that was already replaced (outside the grace period) MUST end the entire session and be recorded.
- **FR-007** Signing out MUST invalidate both the short credential and the renewal credential.
- **FR-008** The app MUST share a single renewal attempt among all concurrent requests in one tab, so a burst of rejected requests causes one renewal, not many.
- **FR-009** When the live connection is re-established after the short credential has expired, the app MUST renew first.
- **FR-010** Requests that change data and carry the browser session MUST be accepted only when their origin is one of the configured web-app origins. Requests with an explicit credential and no browser session are exempt.
- **FR-011** The live-notification connection request that carries the browser session MUST also be accepted only from configured origins.
- **FR-012** Background work started on the user's behalf MUST receive a credential that outlives the queue delay (configurable, default 1 hour) and is not the user's browser session.
- **FR-013** Lifetimes MUST be configuration values, and a development environment MUST be able to extend them (for example to use the experimental front end with a pasted credential).

## Non-Functional Requirements
- **NFR-001** A renewal completes in under 500 ms at the 95th percentile under normal load and does not require user interaction.
- **NFR-002** Renewal credentials are stored on the server only in a form that cannot be turned back into the credential (hashed).
- **NFR-003** No secrets are committed; developer tooling stays Development-only (constitution 1).

## Edge Cases
- Browser is offline during renewal: the app keeps the current page, retries when back online, and signs the user out only on an explicit rejection.
- The renewal credential is missing (cleared cookies): the app returns to sign-in.
- Clock differences between servers: lifetimes are checked on the server that issues and validates them.
- The session reaches its absolute maximum while the user is active: they are asked to sign in once; the app shows the sign-in page, not an error.
- A user signs in on two devices: each has its own session chain; signing out on one does not affect the other (as in spec 001).
- Reuse detection ends only the affected chain.

## Success Criteria
- **SC-001** Replaying a copied short credential after its lifetime returns "not authenticated" on every service.
- **SC-002** A user active over 3 hours (more than 10 renewals) is never sent to sign-in.
- **SC-003** Replaying a replaced renewal credential outside the grace period ends that session and a later renewal fails.
- **SC-004** A cross-site data-changing request carrying the browser session is rejected; the same request from the web app origin succeeds.
- **SC-005** Image/link previews complete when processing takes up to 30 minutes.

## Out of Scope
- "Sign out of all devices" and a device/session list.
- Multi-factor authentication, changes to the email-code sign-in.
- Content-security-policy and other page hardening.
- Persisting sessions beyond what the existing token cache (Redis or memory) provides; with the in-memory mode sessions end when the Authentication service restarts (users sign in again).

## Key Entities
- **Short credential**: proves the account on each request; minutes of validity.
- **Renewal credential**: used only to obtain a new short credential; one active per session chain.
- **Session chain**: all renewal credentials descended from one sign-in; ended together on reuse or sign-out.

## Assumptions (confirmed by the owner on 2026-10-25)
1. Short credential 15 min; renewal idle period 30 days; absolute session maximum 90 days; grace period 30 s; background-work credential 1 hour.
2. Session chains live in the same store type as the token cache (Redis or memory, chosen by the existing switch). A restart in memory mode signs everyone out.
3. The experimental front end keeps working with a pasted credential; in Development lifetimes can be extended (FR-013).
4. Reuse of a renewal credential ends only that chain.

## Open Questions
None.
