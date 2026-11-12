# Implementation Plan: Keep the free-tier data stores awake

**Spec**: [spec.md](spec.md)

## Summary
A small, provider-neutral keep-alive: a contract `IKeepAliveTarget` (a name and a `KeepAliveAsync`), one hosted service that pings every registered target on a timer, and one target per hosted store. The Redis target is registered together with the Redis cache, the MongoDB target together with the MongoDB repositories, so the set of targets always matches the configured modes. Only the API registers the hosted service.

## Technical Context
Same stack as specs 001 to 003. Redis is reached through `StackExchange.Redis` (`RedisConnection`), MongoDB through `IMongoClient`. What each provider counts as activity is in the spec (section "What the providers count as activity"). No persistence-provider behavior changes, so constitution principle 3 holds through the abstraction (D1).

## Constitution Check
| Principle | Status |
|---|---|
| 1 Security first | Pass. No secret added; logs name the store only. |
| 2 Generated code | Not affected (no API change). |
| 3 Provider-neutral persistence | Pass. Application code sees only `IKeepAliveTarget`; each provider supplies its own target. MySQL and Cosmos DB need none (nothing to keep alive in the free-tier sense); the spec says so (AS4.2). |
| 4 Test-first | Pass (tasks order). |
| 5 Front-end conventions | Not affected. |
| 6 Backward compatibility | Pass. Additive; defaults keep existing deployments working and the feature can be switched off. |
| 7 Spec-driven | Pass. |

## Design Decisions

### D1. Where the code lives
- New project `Infrastructure/LetsTalk.Server.KeepAlive` (net10.0, like `LetsTalk.Server.Cors`) with `IKeepAliveTarget`, `KeepAliveSettings`, `KeepAliveService` and `AddKeepAlive()`; new test project `LetsTalk.Server.KeepAlive.Tests`. Both are added to `LetsTalk.Server.sln` by hand, as in spec 002 T118.
- `LetsTalk.Server.Persistence.Redis` and `LetsTalk.Server.Persistence.MongoDB.Repository` reference it and contain one target each. The API references neither store directly for this.

### D2. Settings (FR-002, FR-006)
`KeepAliveSettings`, section `KeepAlive`, defaults in code, bound in the API only:
- `Enabled` = `true`
- `Interval` = 1 day (`TimeSpan`)
- `StartupDelay` = 30 seconds (lets the stores connect, avoids a ping racing the start-up)

No `appsettings.json` is edited (the API one holds the owner's secrets, as in specs 001 and 002). The deployment notes show how to override with environment variables.

### D3. The signal (FR-001, FR-003)
- **Redis**: a tiny write and read through `RedisConnection.Connection.GetDatabase()`: `StringSetAsync("keepalive:letstalk", <UTC timestamp, ISO 8601>, expiry: 1 hour)`, then `StringGetAsync` of the same key; a missing or different value throws, so the warning shows a real failure. This follows the provider's own recommendation (`SET ... EX`, then `GET`). The key is a constant, outside the key names used by the caches, and expires on its own, so no application data is touched and nothing piles up.
- **MongoDB**: the `ping` admin command (`new BsonDocument("ping", 1)` on the `admin` database) through the `IMongoClient` singleton. Atlas defines inactivity as zero connections, so a connection that runs a command is enough. No collection is touched and nothing is written.
- If the providers turn out not to count these signals (spec, "Still unproven"), only these two methods change.

### D4. The hosted service (FR-002, FR-004, FR-005, FR-008)
`KeepAliveService : BackgroundService`:
1. Wait `StartupDelay`, then run a round, then wait `Interval` on a `PeriodicTimer` built from the injected `TimeProvider`, and repeat until the stopping token is cancelled.
2. A round pings every target independently: each call has its own try/catch, a failure logs `Warning` with `target.Name` and the exception message, a success logs `Debug`. One failure never skips the other target (AS3.2). A hang is bounded by a per-ping timeout (the `Interval` is far longer, so a 30-second `CancellationTokenSource` per ping is enough).
3. `OperationCanceledException` from the stopping token ends the loop quietly.
4. When `Enabled` is false, or no target is registered, the service returns immediately and logs once at `Information`.

### D5. Registration (FR-001, FR-007, AS4.1, AS4.2)
- `AddRedisCache()` (already called only when `Features:CachingMode` is `redis`, in `CoreServicesRegistration.cs`) also registers `RedisKeepAliveTarget`. `AddMongoDBRepository()` (called only for `DatabaseMode` `MongoDB`) registers `MongoDBKeepAliveTarget`.
- Authentication and FileStorage also call `AddRedisCache()`, so they get the target registered but no hosted service, hence no ping (FR-007). `AddKeepAlive(configuration)` is called only in `ApiServiceRegistration.cs`.
- Targets are registered with `TryAddEnumerable`, so the hosted service receives `IEnumerable<IKeepAliveTarget>`.

### D6. Time
`TimeProvider` is injected so tests drive the schedule without waiting (the project already does this in spec 002 T116).

## Risks
- **Not testable quickly (spec)**: the 14 and 30 day windows cannot be shortened. Mitigation: the Redis signal follows the provider's own recommendation; the provider's warning email is the check; a change is local to D3.
- **API down**: out of scope, noted in `deployment.md`.
- **Free-tier terms**: one ping per day is far from abuse, but the provider's terms are the operator's to check.
