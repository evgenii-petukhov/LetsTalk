# Deployment notes: keep-alive for the free-tier data stores

Applies to the release of `specs/004-keep-alive-pings`. No secret, connection string or database change.

## What changes
While the API runs, it sends each configured hosted store a small signal 30 seconds after the start and then once a day:
- **Cache (Redis Cloud, `Features:CachingMode=redis`)**: it writes the key `keepalive:letstalk` with the current UTC time and an expiry of one hour, then reads it back. The key therefore exists for at most an hour after each run and then disappears by itself. No other key is touched.
- **Document database (MongoDB Atlas, `Features:DatabaseMode=MongoDB`)**: it runs the `ping` command on the `admin` database. Nothing is written.

Only the API does this. The Authentication and FileStorage services share the same Redis database, so they send nothing. If a store is not configured (in-memory cache, MySQL, Cosmos DB), nothing is sent for it.

## Settings
All optional, with defaults in the code. Set them either in a `KeepAlive` section of the deployed `appsettings.json` of the **API**, or as environment variables of the API (a double underscore replaces the colon, as in the table). Use one of the two, not both: the API reads the file after the environment variables, so a value in the file wins.

| Variable | Default | Meaning |
|---|---|---|
| `KeepAlive__Enabled` | `true` | `false` switches the keep-alive off |
| `KeepAlive__Interval` | `1.00:00:00` (one day) | time between rounds, format `d.hh:mm:ss` |
| `KeepAlive__StartupDelay` | `00:00:30` | wait after the start before the first round |

The limits to stay under are 14 days (Redis Cloud free plan) and 30 days (MongoDB Atlas free M0 cluster), so keep the interval well below the smaller one.

## What you will see in the log
- **Normal run**: nothing. A successful signal is logged at the debug level only, and production runs at `Information`.
- **Failure**: a warning `Keep-alive signal to Redis failed: ...` (or `MongoDB`), once per round. The next round runs on schedule and the other store is still signalled. The warning does not stop the API.
- **Nothing to do**: one information line at the start when the keep-alive is disabled or no hosted store is configured.
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
Set `KeepAlive__Enabled=false` and restart the API, or deploy the previous build. Nothing needs to be cleaned up: the Redis key expires by itself.
