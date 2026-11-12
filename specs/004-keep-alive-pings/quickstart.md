# Quickstart: verify the keep-alive

Needs a local Redis and a local MongoDB (the repo's Docker setup provides Redis; start MongoDB the same way or use any local instance).

All settings for the run go into one place: `backend/Applications/LetsTalk.Server.API/appsettings.Development.json`, which the API reads last. Add the blocks below to it for the run, and **revert the file afterwards (`git checkout backend/Applications/LetsTalk.Server.API/appsettings.Development.json`); do not commit it**.

```json
{
  "Features": { "CachingMode": "redis", "DatabaseMode": "MongoDB" },
  "ConnectionStrings": { "Redis": "localhost:6379", "MongoDB": "mongodb://localhost:27017" },
  "KeepAlive": { "StartupDelay": "00:00:05" },
  "Serilog": { "MinimumLevel": { "Override": { "LetsTalk.Server.KeepAlive": "Debug" } } }
}
```

(Keep the `Cors` and `Logging` blocks that are already in the file.) The `Serilog` override makes the successful signals visible; without it they are not logged. Start the API from `backend/Applications/LetsTalk.Server.API` with `dotnet run`, and change one value in the file between rows, as the table says.

| # | Change in the file, then run | Expected | Covers | Result |
|---|---|---|---|---|
| Q1 | The block above as it is | About 5 seconds after the start the log shows `Keep-alive signal sent to Redis` and `... to MongoDB` at debug level; `redis-cli GET keepalive:letstalk` returns a UTC timestamp and `redis-cli TTL keepalive:letstalk` is at most 3600; no other key was added | AS1.2, AS2.2, FR-002, FR-005 | |
| Q2 | Add `"Interval": "00:01:00"` to `KeepAlive` | A new pair of lines every minute; the key's timestamp changes each time | AS1.1, AS2.1 | |
| Q3 | As Q2; while the API runs, stop Redis (`docker stop redis`) and wait for the next round | A warning `Keep-alive signal to Redis failed: ...`; the MongoDB line still appears; the API still answers `GET /swagger` | AS3.1, AS3.2 | |
| Q4 | Set `Features` to `"CachingMode": "memory"` and `"DatabaseMode": "MySql"` | One information line `Keep-alive does nothing: enabled is True, 0 store(s) are configured`; no signal lines | AS4.1, AS4.2 | |
| Q5 | Add `"Enabled": false` to `KeepAlive` | One information line saying it is disabled; no signal lines | AS4.3, FR-006 | |
| Q6 | Set `"StartupDelay": "00:05:00"`, run, then press Ctrl+C during the wait | The API shuts down at once | FR-008 | |
| Q7 | After the release: leave the app idle and watch the provider emails and consoles for a few weeks | No inactivity warning from Redis Cloud; the Atlas cluster is not paused and shows daily connections | SC-001 | |

Record the date and outcome in the Result column when you run each row. Q7 can only be filled in weeks after the release.
