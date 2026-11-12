# Feature Spec: Keep the free-tier data stores awake

**Feature folder**: `specs/004-keep-alive-pings`\
**Status**: Draft, decisions confirmed 2026-11-11 (a lightweight keep-alive signal run by the API only, once a day: a tiny write and read for Redis, a ping for MongoDB; changed after checking the providers' documentation)\
**Created**: 2026-11-11\
**Depends on**: none

## Problem
Production runs on free tiers of two hosted data stores, and both punish a period without traffic:
- The hosted cache (Redis Cloud, free plan) **deletes the database** after 14 days of inactivity. The provider warns by email first. The cache holds the session and token records, so a deletion signs every user out and needs a new database, a new connection string and a redeploy.
- The hosted document database (MongoDB Atlas, free M0 cluster) **pauses the cluster** after 30 days of inactivity. The app then cannot read or write messages until someone resumes it by hand in the provider's console.

A quiet period (a holiday, a demo that nobody opens, an outage of the front end) is enough to trigger either. Nothing in the app currently notices or prevents it.

## Goal
As long as the API is running, both stores receive a harmless, regular signal that counts as activity, so neither is deleted nor paused, and the operator does not have to remember to open the app.

## User Stories

### US1 - The cache is not deleted while the app is up (P1)
As the operator, I do not receive the "your database will be deleted" email while the API is running.
- **AS1.1** Given the API is running and nobody uses the app, when a day passes, then the cache has received a ping.
- **AS1.2** Given the API was restarted, then a ping is sent shortly after the start, and the daily schedule continues.

### US2 - The document database is not paused while the app is up (P1)
As the operator, I never have to resume the cluster by hand because of a quiet period.
- **AS2.1** Given the API is running and nobody uses the app, when a day passes, then the document database has received a ping.
- **AS2.2** Given the API was restarted, then a ping is sent shortly after the start.

### US3 - A failing store does not harm the app or hide the problem (P1)
- **AS3.1** Given a store is unreachable at the moment of the ping, then the app keeps serving requests, the failure is logged as a warning that names the store, and the next ping happens on schedule.
- **AS3.2** Given one store fails, then the other store is still pinged.

### US4 - Only configured stores are pinged (P2)
- **AS4.1** Given the app runs with the in-memory cache instead of the hosted one, then no cache ping is attempted.
- **AS4.2** Given the app uses another database provider (MySQL, Cosmos DB), then no document-database ping is attempted.
- **AS4.3** Given the operator disables the keep-alive in configuration, then nothing is pinged.

## Functional Requirements
- **FR-001** While the API runs, it MUST send a lightweight keep-alive signal ("ping" in this document) to each configured hosted store: the cache when the hosted cache is selected, the document database when it is the selected database provider.
- **FR-002** A ping MUST be sent shortly after the API starts and then at a configurable interval (default one day).
- **FR-003** A ping MUST NOT read, write or change any application data. The only data it may write is one dedicated key with a short expiry in the cache; it writes nothing to the document database.
- **FR-004** A failed ping MUST be logged as a warning with the store name and MUST NOT stop the schedule, stop the other store's ping or stop the API.
- **FR-005** A successful ping MUST be logged at the debug level only, so the daily log is not filled with noise.
- **FR-006** The keep-alive MUST be switchable off and its interval MUST be configurable, both with defaults in code (no secret or connection string is involved).
- **FR-007** Only the API runs the keep-alive. The other services that use the same hosted cache (Authentication, FileStorage) do not, because they share its database (owner's confirmation, 2026-11-11).
- **FR-008** The keep-alive MUST stop promptly when the API shuts down.

## Non-Functional Requirements
- **NFR-001** The load is negligible: one tiny round trip per store per interval.
- **NFR-002** No secrets are added or logged; the logs name the store, never the connection string (constitution 1).
- **NFR-003** Application code depends on provider-neutral abstractions only (constitution 3): a store plugs into the keep-alive by implementing one small contract.

## What the providers count as activity (checked 2026-11-11)
- **Redis Cloud (free plan)**: inactive means that no Redis commands reach the database for 14 days; opening the console does not count. The provider's support articles never name `PING` and recommend a scheduled write such as `SET key value EX 60`, or a `SET` followed by a `GET`. A third-party project reports that `PING` does not count on another provider (Upstash). The Redis signal is therefore a tiny write and read, not a bare `PING`. The support pages could not be opened by the tool used for this check (HTTP 403), so the owner should read them once.
- **MongoDB Atlas (free M0)**: "Atlas automatically pauses Free clusters after 30 days of inactivity where there are zero connections to the cluster." Inactivity is defined by connections only, so a `ping`, which needs a connection, counts, and nothing needs to be written.
- **Still unproven**: neither provider can be tested quickly (14 and 30 days). The proof is that the provider's inactivity warning does not appear after the release (SC-001). If it does, the signal is changed in the two store implementations only (plan D3), and the spec is updated first.

## Edge Cases
- If the API itself is down for longer than the provider's limit (14 or 30 days), nothing here helps; the deployment notes tell the operator to restart the API or use the provider's console.
- Several API instances behind one address each ping, which is harmless.
- A ping at the same moment as the start of the app, when the store is still connecting, may fail once; the next one succeeds (FR-004).

## Success Criteria
- **SC-001** After the release, neither provider sends an inactivity warning during a period with no user traffic.
- **SC-002** The quickstart shows a ping for each configured store in the API log at the debug level shortly after the start, and a clear warning when a store is unreachable.

## Out of scope
- Creating or restoring a deleted database from code (considered, rejected for now).
- Keep-alive in the Authentication and FileStorage services.
- Alerting when a ping fails (a warning in the log is enough for now).
- Other free-tier services (event broker, storage).
