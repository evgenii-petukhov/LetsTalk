# Feature Spec: Retry short-lived failures of service-to-service calls

**Feature folder**: `specs/006-retry-resilience`\
**Status**: Draft for the owner's review (2026-11-13). Options were compared in `research.md`; the owner chose: retry for the HTTP clients, the gRPC client's own retry for gRPC, and a small shared retry in the two Lambdas.\
**Created**: 2026-11-13\
**Depends on**: none

## Problem
Services call each other over the network, and a call fails at the first error:
- The image-processing and link-preview consumers, and the two Lambdas that do the same work, report their result to the API with one HTTP request. The link-preview code also downloads the page it previews with one HTTP request.
- The API calls the Authentication service over gRPC for every sign-in, token check and session step, each with one attempt.

A failure that would pass a moment later (the server is busy and answers "try later", a connection drops, a restart takes a few seconds, a request is slow) is treated as final. For the HTTP callbacks the work is lost: the image or link preview is computed and then never reaches the chat. For gRPC the user's request fails with an error.

## Goal
A call that fails for a reason that usually passes within seconds is repeated a few times, with growing pauses, before it is reported as failed. A call that fails for any other reason fails at once, as today, and a call is never repeated when repeating it could do harm.

## User Stories

### US1 - A busy or briefly unavailable API does not lose a preview (P1)
As a user who sends an image or a link, I get the preview even if the API was busy or restarting for a few seconds when the preview was ready.
- **AS1.1** Given the API answers "busy" or "unavailable" to the first attempt of a result report and accepts the second, then the preview appears and no error is logged beyond a warning that a retry happened.
- **AS1.2** Given the API stays unavailable for every attempt, then after the last attempt the failure is reported as it is today (the consumer fails; the Lambda fails), and the log shows how many attempts were made.
- **AS1.3** Given the API rejects the report because it is wrong (a bad request, no permission, a missing item, a server bug), then it is not repeated.

### US2 - A flaky link download is repeated (P2)
- **AS2.1** Given the page to preview does not answer in time, drops the connection or answers "busy" once, then the download is repeated and the preview is made from the later answer.
- **AS2.2** Given the page answers "not found" or "forbidden", then it is not repeated.
- **AS2.3** Given one page keeps failing, then the download of other pages is not slowed (a failing site never blocks the others).

### US3 - A briefly unreachable Authentication service does not fail the user's request (P1)
- **AS3.1** Given the Authentication service is unreachable for the first attempt of a read-only call (checking a token, revoking a token, issuing a token, ending a session) and reachable for the second, then the user's request succeeds.
- **AS3.2** Given a call that changes state in a way a repeat could not undo (starting a session, renewing a session, generating a login code, checking a login code), then it is never repeated, and the failure is reported as today.
- **AS3.3** Given the Authentication service refuses a call (the token is invalid, the code is wrong), then it is not repeated.

### US4 - The Lambdas behave like the services (P1)
- **AS4.1** Given the image or link Lambda cannot reach the API on the first attempt, then it repeats under the same rules and with the same default numbers as the services (US1, US2), without any configuration file.

### US5 - The operator can tune and see it (P2)
- **AS5.1** Given a service's settings list different numbers (how many repeats, the first pause, the longest wait for one attempt and for the whole call), then those numbers apply to that service; a wrong number (zero or negative pause, a negative count) stops the service at start with a clear message (as spec 005).
- **AS5.2** Given a call is repeated, then a warning names the target (the client or the call), the attempt number and the reason, without the address query, tokens or message content.

## Functional Requirements
- **FR-001** A call over HTTP MUST be repeated, up to the configured number of times, when it fails with a lost or refused connection, with no answer within the time allowed for one attempt, or with the answers "request timeout", "too many requests", "bad gateway", "service unavailable" or "gateway timeout". No other answer is repeated.
- **FR-002** Pauses between repeats MUST grow with each repeat and carry a random variation, so many callers do not repeat in step. When the answer states how long to wait, that wait is used instead, but never longer than a configured maximum pause.
- **FR-003** Every attempt MUST have a time limit, and the whole call (all attempts and pauses) MUST have a limit as well. The defaults equal the time an HTTP call may take today (100 seconds for the whole call), so nothing that works today starts to fail.
- **FR-004** A cancelled call (a shutdown, a cancelled message) MUST NOT be repeated and MUST stop at once.
- **FR-005** A repeated HTTP call MUST send the same request again (same body and headers). The two result reports repeat even though they change data, because the API accepts a second report for the same message (it replaces the preview and, for a link, falls back to the existing preview); the only effect of a repeat is that a chat member may receive the notification twice (decision D4 in `plan.md`).
- **FR-006** The link download and the report to the API MUST NOT share one client, and no failure state of one target (a pause, a counter) may be shared with another target. There is no circuit breaker or call-rate limit in this feature.
- **FR-007** A call over gRPC MUST be repeated only when the Authentication service cannot be reached (status "unavailable"), only for the read-only calls named in AS3.1, up to the configured number of attempts with growing pauses. All other calls MUST get exactly one attempt. A refusal from the service is never repeated.
- **FR-008** The HTTP rules (FR-001 to FR-005) MUST be written once and used by the services and by the Lambdas, so the two cannot drift apart.
- **FR-009** The numbers MUST be settings of the service that makes the call, with defaults carried by the settings classes and listed in the shipped `appsettings.json` of that service (spec 005); a Lambda uses the defaults. Validation follows spec 005.
- **FR-010** Each repeat MUST be logged as a warning (AS5.2). The message MUST NOT contain the request address, query, headers, body or tokens.
- **FR-011** The API's own contract (routes, request and response bodies, status codes) and the Angular and Next.js clients MUST NOT change.

## Success Criteria
- **SC-001** With the API stopped for less than the total retry time while an image is processed, the preview still arrives after the API is back (quickstart Q1).
- **SC-002** With the Authentication service stopped for the same time, a token check succeeds after it is back, and a sign-in that fails during the outage is not repeated (Q4, Q5).
- **SC-003** The tests prove each rule of FR-001, FR-002, FR-004, FR-005 and FR-007 without a real network.

## Out of Scope
- Retry of the message broker, the database, Redis, S3, Cosmos DB and SignalR; the broker-level redelivery of a whole message (decided against in `research.md`).
- A circuit breaker, a rate limiter, a fallback value.
- Other gRPC services (there is none called from code today besides Authentication).
- Changing what the API does when a report arrives twice.

## Assumptions (the owner confirms or changes these at review)
- A1: "unavailable" for gRPC and the answer list in FR-001 cover the failures the owner saw (a busy server, a poor connection).
- A2: Reporting twice may notify a chat member twice; that is acceptable (FR-005).
- A3: Default numbers: 3 repeats, first pause 1 second, longest pause 10 seconds, 30 seconds per attempt, 100 seconds in total; gRPC 4 attempts, first pause 1 second, longest pause 10 seconds.
