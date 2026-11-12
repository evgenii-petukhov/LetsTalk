# Quickstart: verify the keep-alive

Needs a local Redis and a local MongoDB (the repo's Docker setup provides Redis; start MongoDB the same way or use any local instance).

Before the first row, set up the API as for a normal local run with the hosted stores:
- `Features:CachingMode` is `redis` and `Features:DatabaseMode` is `MongoDB`, with the connection strings of the local Redis (`localhost:6379`) and MongoDB (`mongodb://localhost:27017`).

The keep-alive settings come from the `KeepAlive` section of `backend/Applications/LetsTalk.Server.API/appsettings.json`, which lists `Redis` and `MongoDB` with an interval of one day each. For the run, change values in that file (or in `appsettings.Development.json`, which the API reads after it and which therefore wins), as the table says, and **revert every change afterwards; do not commit it**. Rows Q2 to Q11 are independent: keep `StartupDelay` at `"00:00:05"` from Q1 (except in Q6), undo the previous row's change, and apply only the change the row names (Q3 also applies Q2's). Start the API from `backend/Applications/LetsTalk.Server.API` with `dotnet run`; the log goes to the console. Serilog writes text values in quotes, so the line `Keep-alive for Redis every 1.00:00:00` in the table appears as `Keep-alive for "Redis" every 1.00:00:00`; match on the words, not on the quotes.

| # | Change in the file, then run | Expected | Covers | Result |
|---|---|---|---|---|
| Q1 | Change `KeepAlive:StartupDelay` to `"00:00:05"` | (1) At the start: `Keep-alive for Redis every 1.00:00:00` and `Keep-alive for MongoDB every 1.00:00:00`. (2) About 5 seconds after the start: `Keep-alive signal sent to Redis` and `... to MongoDB` at information level. (3) Redis: `redis-cli GET keepalive:letstalk` returns a UTC timestamp, `redis-cli TTL keepalive:letstalk` is at most 3600, and no other key was added. MongoDB leaves nothing behind (a `ping` writes nothing). | AS1.2, AS2.2, FR-002, FR-005, FR-011 | Passed (owner) |
| Q2 | Set `Interval` to `"00:01:00"` for both `Redis` and `MongoDB` under `KeepAlive:Targets` | A new pair of lines every minute; the key's timestamp changes each time | AS1.1, AS2.1 | Passed (owner) |
| Q3 | As Q2; while the API runs, stop Redis (`docker stop redis`) and wait for the next signal | A warning `Keep-alive signal to Redis failed: ...`; the MongoDB line still appears; the API still answers `GET /swagger` | AS3.1, AS3.2 | Passed (owner) |
| Q4 | Set `Features` to `"CachingMode": "memory"` and `"DatabaseMode": "MySql"` | One information line `Keep-alive has nothing to do ...`; no signal lines and no warning about the `Redis` and `MongoDB` entries | AS4.1, AS4.2, AS5.4 | Passed (owner) |
| Q5 | Set `"Enabled": false` for both `Redis` and `MongoDB` under `KeepAlive:Targets` | Both stores reported as switched off, then the `nothing to do` line; no signal lines | AS4.3, FR-006 | Passed (owner) |
| Q6 | Set `"StartupDelay": "00:05:00"`, run, then press Ctrl+C during the wait | The API shuts down at once | FR-008 | Passed (owner) |
| Q7 | Set `"Enabled": false` for `Redis` only | `Keep-alive for Redis is switched off`; only MongoDB is signalled | AS5.1 | Passed (owner) |
| Q8 | Give `Redis` the interval `00:00:20` and `MongoDB` the interval `00:01:00` | Redis is signalled about every 20 seconds, MongoDB about every minute | AS5.2 | Passed (owner) |
| Q9 | Rename the `Redis` entry to a misspelled name, for example `Redsi` | `Keep-alive for Redis is not listed in the settings, so it is not signalled`; no warning about `Redsi`; MongoDB is signalled | AS5.3, AS5.4 | Passed (owner) |
| Q10 | Set `"Interval": "00:00:00"` for `MongoDB` | A warning that the interval is not positive; MongoDB is not signalled; Redis still is | AS5.5, FR-010 | Passed (owner) |
| Q11 | Remove the `MongoDB` entry from `KeepAlive:Targets` (in `appsettings.json` for the run, reverted afterwards) | `Keep-alive for MongoDB is not listed in the settings, so it is not signalled`; only Redis is signalled | AS5.3 | Passed (owner) |
| Q12 | After the release: leave the app idle and watch the provider emails and consoles for a few weeks | No inactivity warning from Redis Cloud; the Atlas cluster is not paused and shows daily connections | SC-001 | Skipped (owner) |

Record the date and outcome in the Result column when you run each row. Q12 can only be filled in weeks after the release.
