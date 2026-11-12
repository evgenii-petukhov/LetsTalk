# Implementation Plan: Keep the free-tier data stores awake

**Spec**: [spec.md](spec.md)

## Summary
A small, provider-neutral keep-alive: a contract `IKeepAliveTarget` (a name and a `KeepAliveAsync`), one hosted service that runs one independent schedule for every registered target, and one target per hosted store. Each store has its own settings (a switch and an interval); there is no interval shared by the stores. The Redis target is registered together with the Redis cache, the MongoDB target together with the MongoDB repositories, so the set of targets always matches the configured modes. Only the API registers the hosted service.

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
| 6 Backward compatibility | Pass. Additive; the shipped `appsettings.json` lists both stores, and each can be switched off. |
| 7 Spec-driven | Pass. |

## Design Decisions

### D1. Where the code lives
- New project `Infrastructure/LetsTalk.Server.KeepAlive` (net10.0, like `LetsTalk.Server.Cors`) with `IKeepAliveTarget`, `KeepAliveSettings`, `KeepAliveService` and `AddKeepAlive()`; new test project `LetsTalk.Server.KeepAlive.Tests`. Both are added to `LetsTalk.Server.sln` by hand, as in spec 002 T118.
- `LetsTalk.Server.Persistence.Redis` and `LetsTalk.Server.Persistence.MongoDB.Repository` reference it and contain one target each. The API references neither store directly for this.

### D2. Settings (FR-002, FR-006, FR-009, FR-010)
`KeepAliveSettings`, section `KeepAlive`, defaults in code, bound in the API only:
- `StartupDelay` = 30 seconds: one wait after the start for all stores (lets them connect, avoids a ping racing the start-up). It is not a property of a store, so it stays shared (spec FR-009).
- `Targets`: `Dictionary<string, KeepAliveTargetSettings>` with `StringComparer.OrdinalIgnoreCase`, initialised in the class so the binder fills the same instance and keeps the comparer. The key is the target's `Name` (`Redis`, `MongoDB`).

`KeepAliveTargetSettings` (one per store, **no inheritance**):
- `Enabled` = `true`
- `Interval` = 1 day (`TimeSpan`)

A store with no entry in `Targets` is **not pinged** (opt-in, owner's decision); an information line says so. A listed store that leaves out a value gets that value's default from `KeepAliveTargetSettings` (enabled, one day). The former section-level `Interval` is removed. The settings of this spec have not been released yet (the branch is not merged), so there is nothing to migrate.

Example (a store is pinged only when it is listed; here each has its own explicit interval):
```json
"KeepAlive": {
  "StartupDelay": "00:00:30",
  "Targets": {
    "Redis":   { "Enabled": true, "Interval": "12:00:00" },
    "MongoDB": { "Enabled": true, "Interval": "1.00:00:00" }
  }
}
```
To switch one store off, set its `Enabled` to `false`; its `Interval` is then ignored. There is no switch for the whole keep-alive (spec FR-006): it is off when every store is off, and the service then logs that it has nothing to do.

The API's `appsettings.json` gets a `KeepAlive` section with the values of the example above (`Redis` and `MongoDB`, both enabled, interval one day), so the intervals are visible and each store's is explicit. The file is committed and holds no secrets (the owner adds them by hand for a run and removes them afterwards), so editing it is normal. When the section is missing, or a store is not listed, that store is not pinged; the code defaults apply only to what a listed store leaves out. Because the API reads `appsettings.json` after the environment variables, the values in the file win over `KeepAlive__*` variables; the deployment notes say so.

### D3. The signal (FR-001, FR-003)
- **Redis**: a tiny write and read through `RedisConnection.Connection.GetDatabase()`: `StringSetAsync("keepalive:letstalk", <UTC timestamp, ISO 8601>, expiry: 1 hour)`, then `StringGetAsync` of the same key; a missing or different value throws, so the warning shows a real failure. This follows the provider's own recommendation (`SET ... EX`, then `GET`). The key is a constant, outside the key names used by the caches, and expires on its own, so no application data is touched and nothing piles up.
- **MongoDB**: the `ping` admin command (`new BsonDocument("ping", 1)` on the `admin` database) through the `IMongoClient` singleton. Atlas defines inactivity as zero connections, so a connection that runs a command is enough. No collection is touched and nothing is written.
- If the providers turn out not to count these signals (spec, "Still unproven"), only these two methods change.

### D4. The hosted service (FR-002, FR-004, FR-005, FR-008, FR-010, FR-011)
`KeepAliveService : BackgroundService`:
1. Resolve each registered target's settings: `Targets[target.Name]`. Skip a target with no entry (information line: not listed). Skip a target whose `Enabled` is false (information line). Skip a target whose `Interval` is zero or negative and log a warning (a `PeriodicTimer` would throw on it). Everything left is *runnable*.
2. For every key in `Targets` that matches no registered target (ignoring case), log a warning (AS5.4). This also fires for `Redis` while the in-memory cache is selected, which is correct: that entry has no effect.
3. If nothing is runnable (every store disabled or skipped, or no store registered), log once at `Information` and return.
4. Log one information line per runnable target with its interval (FR-011).
5. Wait `StartupDelay` once for all stores (`Task.Delay` with the injected `TimeProvider`). Then run one loop per runnable target and wait for all of them (`Task.WhenAll`). Each loop: ping, then wait for the target's own `PeriodicTimer(interval, timeProvider)`, until the stopping token is cancelled.
6. A ping has its own try/catch: a failure logs `Warning` with `target.Name` and the exception message, a success logs `Debug`. A hang is bounded by a per-ping timeout (30 seconds, a `CancellationTokenSource` built from the `TimeProvider`). Because every store has its own loop, a failing or hung store cannot delay or skip another (AS3.2, FR-004).
7. `OperationCanceledException` from the stopping token ends the loops quietly (FR-008).

### D5. Registration (FR-001, FR-007, AS4.1, AS4.2)
- `AddRedisCache()` (already called only when `Features:CachingMode` is `redis`, in `CoreServicesRegistration.cs`) also registers `RedisKeepAliveTarget`. `AddMongoDBRepository()` (called only for `DatabaseMode` `MongoDB`) registers `MongoDBKeepAliveTarget`.
- Authentication and FileStorage also call `AddRedisCache()`, so they get the target registered but no hosted service, hence no ping (FR-007). `AddKeepAlive(configuration)` is called only in `ApiServiceRegistration.cs`.
- Targets are registered with `AddSingleton<IKeepAliveTarget, ...>()` (each registration function runs once per service), so the hosted service receives `IEnumerable<IKeepAliveTarget>`.

### D6. Time
`TimeProvider` is injected so tests drive the schedule without waiting (the project already does this in spec 002 T116).

## Risks
- **Not testable quickly (spec)**: the 14 and 30 day windows cannot be shortened. Mitigation: the Redis signal follows the provider's own recommendation; the provider's warning email is the check; a change is local to D3.
- **API down**: out of scope, noted in `deployment.md`.
- **More configuration to get wrong**: a misspelled store name or a bad interval would silently do nothing. Mitigation: both are reported as warnings at the start (D4 steps 1 and 2), and each runnable store is listed with its interval (step 4).
- **Free-tier terms**: one ping per day is far from abuse, but the provider's terms are the operator's to check.
