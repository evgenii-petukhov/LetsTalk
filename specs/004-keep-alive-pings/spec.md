# Feature Spec: Keep the free-tier data stores awake

**Feature folder**: `specs/004-keep-alive-pings`\
**Status**: Reworked 2026-11-12: every store has its own `Enabled` and its own `Interval`, and a store is pinged only when it is listed in the settings (decisions of the owner, after the first implementation); the rest as confirmed on 2026-11-11 (a lightweight keep-alive signal run by the API only: a tiny write and read for Redis, a ping for MongoDB; changed after checking the providers' documentation)\
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
- **AS1.1** Given the API is running and nobody uses the app, when the cache's interval (default one day) passes, then the cache has received a ping.
- **AS1.2** Given the API was restarted, then a ping is sent shortly after the start, and the schedule continues.

### US2 - The document database is not paused while the app is up (P1)
As the operator, I never have to resume the cluster by hand because of a quiet period.
- **AS2.1** Given the API is running and nobody uses the app, when the document database's interval (default one day) passes, then it has received a ping.
- **AS2.2** Given the API was restarted, then a ping is sent shortly after the start.

### US3 - A failing store does not harm the app or hide the problem (P1)
- **AS3.1** Given a store is unreachable at the moment of the ping, then the app keeps serving requests, the failure is logged as a warning that names the store, and the next ping happens on schedule.
- **AS3.2** Given one store fails or hangs, then the other store is still pinged, on time.

### US4 - Only configured stores are pinged (P2)
- **AS4.1** Given the app runs with the in-memory cache instead of the hosted one, then no cache ping is attempted.
- **AS4.2** Given the app uses another database provider (MySQL, Cosmos DB), then no document-database ping is attempted.
- **AS4.3** Given the operator switches off every store, or lists none, in configuration, then nothing is pinged.

### US5 - Each store is treated separately (P2)
As the operator, I can switch a store off, or give it its own interval, without touching the other. For example, after moving the cache to a paid plan I stop pinging it and keep pinging the free document database.
- **AS5.1** Given the keep-alive is switched off for one store, then only the other store is pinged.
- **AS5.2** Given the two stores have different intervals, then each is pinged on its own schedule.
- **AS5.3** Given a store is not listed in the keep-alive settings, then it is not pinged (the keep-alive is opt-in for each store) and an information line at the start says so; a listed store that leaves out its interval is pinged once a day, and the other store's settings never change it.
- **AS5.4** Given the configuration names a store that is not in use (for example the cache while the in-memory cache is selected, or a misspelled name), then nothing is done for that entry and nothing is logged about it. A misspelled name is still visible: the real store is then not listed, and the log says so (AS5.3).
- **AS5.5** Given a store's interval is zero or negative, then that store is not pinged, a warning says why, and the other store is not affected.

## Functional Requirements
- **FR-001** While the API runs, it MUST send a lightweight keep-alive signal ("ping" in this document) to each configured hosted store: the cache when the hosted cache is selected, the document database when it is the selected database provider.
- **FR-002** A ping MUST be sent to each store shortly after the API starts and then at that store's own configurable interval (default one day for each store). There is no interval shared by the stores.
- **FR-003** A ping MUST NOT read, write or change any application data. The only data it may write is one dedicated key with a short expiry in the cache; it writes nothing to the document database.
- **FR-004** A failed ping MUST be logged as a warning with the store name and MUST NOT stop the schedule, stop the other store's ping or stop the API. A slow or hung store MUST NOT delay another store's ping.
- **FR-005** A successful ping MUST be logged at the information level, one line per store per interval (owner's decision, 2026-11-12), so the operator can see in the production log that the keep-alive works. With the default of one day this is one line per store per day, so the log is not filled with noise.
- **FR-006** The keep-alive MUST be switchable off for each store, and the interval MUST be configurable for each store, with defaults in code for what a listed store leaves out (enabled, one day; no secret or connection string is involved). A store is pinged only when it is listed in the settings (opt-in). There is no switch for the whole keep-alive: it is off when every store is off or not listed. A listed store does not inherit anything from another store or from a shared value.
- **FR-009** Settings are given per store, under the store's name as the logs show it (`Redis`, `MongoDB`); names are compared ignoring letter case. One value stays shared by all stores because it is not a property of a store: the wait after the start of the API (default 30 seconds).
- **FR-010** A store whose interval is zero or negative MUST be reported as a warning at the start and MUST NOT stop the API or any other store. A configuration entry that names no store in use is ignored without a message (owner's decision, 2026-11-12: the warning was noise whenever a store is switched off by its mode, and a misspelled name already shows as an unlisted store).
- **FR-011** At the start, the log MUST show one information line for each store that will be pinged, with its interval, so the operator can see what runs without waiting for the first ping.
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
- The settings are read once, at the start of the API; a change needs a restart.
- Both stores are pinged after the same wait at the start, but then each follows its own interval, so their pings drift apart or together depending on the intervals; that is harmless.

## Success Criteria
- **SC-001** After the release, neither provider sends an inactivity warning during a period with no user traffic.
- **SC-002** The quickstart shows, for each configured store, its interval at the start and a ping in the API log at the information level, a clear warning when a store is unreachable, and the effect of switching one store off or giving it another interval.

## Out of scope
- Creating or restoring a deleted database from code (considered, rejected for now).
- Keep-alive in the Authentication and FileStorage services.
- Alerting when a ping fails (a warning in the log is enough for now).
- Changing the settings while the API runs.
- A separate wait at the start for each store.
- Other free-tier services (event broker, storage).
