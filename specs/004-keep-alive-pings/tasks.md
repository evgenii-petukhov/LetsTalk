# Tasks: Keep the free-tier data stores awake

**Inputs**: [spec.md](spec.md), [plan.md](plan.md).\
**Precondition**: `specs/003-patch-partial-updates` is merged (it is).\
**Conventions**: as in specs 001 to 003 (tests before code, `Method_WhenCondition_ShouldOutcome`, repeated literals as class-level `private const`; NUnit + Moq + FluentAssertions). `BE` = `backend/`.\
**Commit message prefix: `Keep-alive: `** (one line, for example `Keep-alive: add the hosted service and the store targets`).\
**One commit per phase**, made only when the owner approves, with the ticked `tasks.md` included. Stage files by explicit path. Do not edit any `appsettings.json` (the API one holds the owner's uncommitted changes).

## Phase 0 - Setup
- [x] **T400** (branch created from `master` at `a64bf5fe` after a fast-forward pull; the working tree was clean) Branch `004-keep-alive-pings` from the updated `master`; confirm nothing unrelated is staged.

## Phase 1 - Keep-alive library
Tests first, in the new `BE/Infrastructure/LetsTalk.Server.KeepAlive.Tests`:
- [x] **T410** (3 tests: defaults, binding, missing section) `KeepAliveSettingsTests` (defaults: enabled, one day, 30 seconds; binding from configuration) [P]. (FR-006)
- [x] **T411** (9 tests with `FakeTimeProvider` (package `Microsoft.Extensions.TimeProvider.Testing`) and a recording target; the tests call `ExecuteAsync` through a small subclass instead of `StartAsync`, because .NET 10 starts `ExecuteAsync` asynchronously and the startup timer would otherwise be registered after the test has already moved the clock; run 3 times to check for flakiness) `KeepAliveServiceTests`, with a fake `TimeProvider` and mocked `IKeepAliveTarget`s: `ExecuteAsync_WhenStarted_ShouldPingAllTargetsAfterTheStartupDelay`; `..._WhenIntervalPasses_ShouldPingAgain`; `..._WhenOneTargetFails_ShouldStillPingTheOthers`; `..._WhenTargetFails_ShouldLogAWarningWithTheNameAndKeepRunning`; `..._WhenTargetSucceeds_ShouldLogOnlyAtDebug`; `..._WhenTargetHangs_ShouldCancelThePingAfterTheTimeout`; `..._WhenDisabled_ShouldNotPing`; `..._WhenNoTargets_ShouldNotPing`; `..._WhenStopped_ShouldEndQuickly`. (FR-002, FR-004, FR-005, FR-006, FR-008, AS3.1, AS3.2)
- [x] **T412** (3 tests: settings bound and hosted service registered; resolves with no target; a registered target reaches the service) `KeepAliveRegistrationTests`: `AddKeepAlive_ShouldBindSettingsAndRegisterTheHostedService`. (FR-006, FR-007) [P]

Implementation:
- [x] **T413** (new projects `LetsTalk.Server.KeepAlive` and `LetsTalk.Server.KeepAlive.Tests`; added to the solution by editing `LetsTalk.Server.sln` because `dotnet sln add` refused ("Solution folder Infrastructure already contains a project with the filename"); `KeepAliveService` logs through `[LoggerMessage]` and pings the targets one after another, so a hung store delays the next one by at most the 30 second timeout) Create `LetsTalk.Server.KeepAlive` (`IKeepAliveTarget`, `KeepAliveSettings`, `KeepAliveService`, `AddKeepAlive`) and the test project; add both to `LetsTalk.Server.sln` by hand. (T410 to T412; plan D1, D2, D4)
- [x] **T414** (2026-11-12: 15 tests pass; `backend/CheckWarnings.ps1` 0 warnings in the changed files) `dotnet test BE/Infrastructure/LetsTalk.Server.KeepAlive.Tests`. **Checkpoint 1.**

## Phase 2 - Store targets and wiring
Tests first:
- [x] **T420** (new project `Persistence/LetsTalk.Server.Persistence.Redis.Tests`, added to the solution by hand; 8 tests with a mocked `IDatabase`: the key, the value (UTC time), the expiry `EX 3600`, the read back, no other key touched, missing or different value throws, Redis failure propagates, cancellation ends the wait, and the registration; no real-Redis integration test because the test run has no Redis server, so quickstart Q1 and Q3 cover the real call) `RedisKeepAliveTargetTests` in a Redis test project (create `LetsTalk.Server.Persistence.Redis.Tests` if none exists, same pattern as the Cors tests), with a mocked `IDatabase`: `KeepAliveAsync_ShouldSetTheKeepAliveKeyWithAnExpiryAndReadItBack`; `KeepAliveAsync_WhenValueIsMissing_ShouldThrow`; `KeepAliveAsync_WhenRedisThrows_ShouldPropagate`; `Name_ShouldBeRedis`. A real-server integration test is added only if a Redis can be started in the test run; otherwise Q1 and Q3 of the quickstart cover the real call. (FR-001, FR-003) [P]
- [x] **T421** (5 tests in `MongoDB.Services.Tests`: the `ping` command on `admin`, failure propagates, name, registration, and an integration test against a real `mongod` started by Mongo2Go that the command is accepted) `MongoDBKeepAliveTargetTests` in `LetsTalk.Server.Persistence.MongoDB.Services.Tests` or a repository test project (whichever exists next to the repository; mock `IMongoClient`/`IMongoDatabase`): `KeepAliveAsync_ShouldRunThePingCommandOnTheAdminDatabase`; `KeepAliveAsync_WhenMongoThrows_ShouldPropagate`; `Name_ShouldBeMongoDB`. A real-server integration test in the style of `ChatMongoDBServiceIntegrationTests` checks that the command is accepted. (FR-001, FR-003) [P]
- [x] **T422** (4 tests in `API.Tests` (`KeepAliveRegistrationTests`): with the memory cache and MySql no target is registered; with the Redis cache only the Redis target; the hosted service is registered in both cases; the MongoDB mode is covered by T421 only, because `AddApiServices` runs the MongoDB migrations against a live server in that mode) Registration tests: `AddRedisCache_ShouldRegisterTheRedisTarget`; `AddMongoDBRepository_ShouldRegisterTheMongoTarget`; an API-level test that `AddApiServices` with `CachingMode=memory` and `DatabaseMode=MySql` resolves no targets (AS4.1, AS4.2). Use the existing API test project for the last one. [P]

Implementation:
- [x] **T423** (`RedisKeepAliveTarget` (`SET` with a one hour expiry, then `GET` and compare), registered by `AddRedisCache` together with `TimeProvider.System`; the Redis client has no cancellation, so the waits use `WaitAsync(cancellationToken)`) `RedisKeepAliveTarget` in `LetsTalk.Server.Persistence.Redis` (`SET` with expiry, then `GET`), registered in `AddRedisCache`. (T420; plan D3, D5)
- [x] **T424** (`MongoDBKeepAliveTarget` (`ping` on `admin` through `BsonDocumentCommand`), registered by `AddMongoDBRepository`) `MongoDBKeepAliveTarget` in `LetsTalk.Server.Persistence.MongoDB.Repository`, registered in `AddMongoDBRepository`. (T421; plan D3, D5)
- [x] **T425** (`AddKeepAlive(configuration)` in `ApiServiceRegistration.cs` and a project reference to `LetsTalk.Server.KeepAlive` in the API project) Call `AddKeepAlive(configuration)` in `BE/Applications/LetsTalk.Server.API/ApiServiceRegistration.cs` only. (T422; FR-007)
- [x] **T426** (2026-11-12: `dotnet build` of the solution 0 errors (5 older warnings); `dotnet test` of the solution passes in every project (Redis.Tests 8, MongoDB.Services.Tests 123, API.Tests 23, KeepAlive.Tests 15); `backend/CheckWarnings.ps1` 0 warnings in the changed files) `dotnet build BE/LetsTalk.Server.sln`, `dotnet test BE/LetsTalk.Server.sln`, `backend/CheckWarnings.ps1` for the changed files. **Checkpoint 2.**

## Phase 3 - Deployment notes and verification
- [ ] **T430** Write `deployment.md`: what runs where, the three settings (`KeepAlive__Enabled`, `KeepAlive__Interval`, `KeepAlive__StartupDelay` as environment variables), that nothing else needs configuring, what to do if the API is down longer than the limits (restart it or use the provider console), and how to read the result (the provider's warning email stops). (plan D2)
- [ ] **T431** Write `quickstart.md` with the table below, run it and record the results.
- [ ] **T432** Add one sentence about the keep-alive to the Persistence/back end paragraph of `README.md` or `CLAUDE.md` only if a natural place exists; otherwise skip.
- [ ] **T433** Final `dotnet test BE/LetsTalk.Server.sln`. **Checkpoint 3.** Propose the commit message and file list; wait for approval.

### Quickstart table (for T431)
| # | Steps | Expected | Covers | Result |
|---|---|---|---|---|
| Q1 | Run the API with `Features:CachingMode=redis`, `Features:DatabaseMode=MongoDB`, a local Redis and MongoDB, log level `Debug` for the keep-alive, and `KeepAlive__StartupDelay=00:00:05` | After about 5 seconds the log shows one debug line per store; `redis-cli GET keepalive:letstalk` returns a timestamp and `TTL keepalive:letstalk` is at most 3600 | AS1.2, AS2.2, FR-002, FR-005 | |
| Q2 | Same, with `KeepAlive__Interval=00:01:00` | A new pair of lines every minute | AS1.1, AS2.1 | |
| Q3 | Stop the local Redis while the API runs | A warning that names Redis at the next round; the MongoDB line still appears; the API keeps answering | AS3.1, AS3.2 | |
| Q4 | Start with `Features:CachingMode=memory` and `Features:DatabaseMode=MySql` | No ping lines; one information line saying there is nothing to keep alive | AS4.1, AS4.2 | |
| Q5 | Start with `KeepAlive__Enabled=false` | One information line, no pings | AS4.3, FR-006 | |
| Q6 | Stop the API with Ctrl+C during the startup delay | Shuts down at once | FR-008 | |
| Q7 | After the release: leave the app idle and watch the provider emails and consoles | No inactivity warning from either provider; the cluster is not paused | SC-001 | |

## Dependencies
T410 to T412 can run in parallel; T413 needs them; T420 to T422 can run in parallel after T413; T423 to T425 need their tests; Phase 3 needs Phase 2.
