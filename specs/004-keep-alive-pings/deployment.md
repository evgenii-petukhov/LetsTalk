# Deployment notes: keep-alive for the free-tier data stores

Applies to the release of `specs/004-keep-alive-pings`. No secret, connection string or database change.

## What changes
While the API runs, it sends each configured hosted store a small signal 30 seconds after the start and then once a day for each store (each store has its own interval):
- **Cache (Redis Cloud, `Features:CachingMode=redis`)**: it writes the key `keepalive:letstalk` with the current UTC time and an expiry of one hour, then reads it back. The key therefore exists for at most an hour after each run and then disappears by itself. No other key is touched.
- **Document database (MongoDB Atlas, `Features:DatabaseMode=MongoDB`)**: it runs the `ping` command on the `admin` database. Nothing is written.

Only the API does this. The Authentication and FileStorage services share the same Redis database, so they send nothing. If a store is not configured (in-memory cache, MySQL, Cosmos DB), nothing is sent for it.

## Settings
All optional, with defaults in the code. The API's `appsettings.json` now ships a `KeepAlive` section with an explicit entry for each store (enabled, one day), so the values are visible in the file. Change them in the **deployed** `appsettings.json` of the **API**. The API reads that file after the environment variables, so a `KeepAlive__*` variable for a key that exists in the file is ignored; the file is the place to change them.

```json
"KeepAlive": {
  "StartupDelay": "00:00:30",
  "Targets": {
    "Redis":   { "Enabled": true, "Interval": "1.00:00:00" },
    "MongoDB": { "Enabled": true, "Interval": "1.00:00:00" }
  }
}
```

| Setting | Default | Meaning |
|---|---|---|
| `KeepAlive:StartupDelay` | `00:00:30` | wait after the start before the first signal, shared by all stores |
| `KeepAlive:Targets:<store>:Enabled` | `true` | `false` switches the keep-alive off for that store |
| `KeepAlive:Targets:<store>:Interval` | `1.00:00:00` (one day) | time between signals for that store, format `d.hh:mm:ss`; must be positive |

`<store>` is `Redis` or `MongoDB` (letter case does not matter). **A store is signalled only when it is listed under `Targets`**: a store without an entry is not signalled, and the start-up log says so. A listed store that leaves out a value takes the default above; nothing is inherited from the other store. There is no switch for the whole keep-alive: it is off when every store is off or not listed, and a missing `KeepAlive` section means no keep-alive at all.

The limits to stay under are 14 days (Redis Cloud free plan) and 30 days (MongoDB Atlas free M0 cluster), so keep each interval well below its store's limit.

## What you will see in the log
- **Normal run**: nothing. A successful signal is logged at the debug level only, and production runs at `Information`.
- **At the start**: one information line per store that will run, for example `Keep-alive for Redis every 1.00:00:00`, and one for each store that is switched off or not listed.
- **Failure**: a warning `Keep-alive signal to Redis failed: ...` (or `MongoDB`), once per signal. The next one runs on that store's schedule and the other store is not affected. The warning does not stop the API.
- **Configuration mistakes** (warnings at the start): a `Targets` entry that names no store in use, for example a misspelled name or `Redis` while the in-memory cache is selected; a store whose interval is zero or negative, which is then not signalled.
- **Nothing to do**: one information line at the start when no store is enabled with a valid interval or no hosted store is configured.
- To see every signal while checking a release, set `Serilog__MinimumLevel__Override__LetsTalk.Server.KeepAlive` to `Debug` on the API.

## How to know it works
- **Redis Cloud**: the inactivity warning email stops. Before the release you got one; after the release you should not.
- **MongoDB Atlas**: the cluster is not paused. Its Metrics tab shows a connection every day.
- Both providers publish their rules in their own documentation (Redis Cloud support articles on inactivity of free databases, Atlas "Free cluster limitations"); they were checked on 2026-11-11 and can change. The Redis support pages could not be opened by the tool used for that check, so read them once.

## Limits
- It works only while the API is running. If the API is down for longer than the provider's limit (14 days for Redis Cloud), restart it, or open the provider console, before the limit is reached. A paused Atlas cluster is resumed from the Atlas console.
- Several API instances each send a signal. That is harmless.
- Redis Cloud deletes an inactive free database without a backup. Keep the connection string and the recreation steps at hand (creating a database from code was considered and left out of this spec).

## Rollback
Set `Enabled` to `false` for both stores in the deployed `appsettings.json` and restart the API, or deploy the previous build. Nothing needs to be cleaned up: the Redis key expires by itself.
