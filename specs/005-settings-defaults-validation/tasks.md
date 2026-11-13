# Tasks: Defaults and validation for the back-end settings

**Inputs**: [spec.md](spec.md), [plan.md](plan.md), [research.md](research.md).\
**Precondition**: the owner has confirmed D-1 to D-7 of the spec (T500). Nothing below starts before that.\
**Conventions**: as in specs 001 to 004 (tests before code, `Method_WhenCondition_ShouldOutcome`, repeated literals as class-level `private const`; NUnit + Moq + FluentAssertions). `BE` = `backend/`.\
**Commit message prefix: `Settings: `** (one line, for example `Settings: add the validation mechanism and move AuthCookie to it`).\
**One commit per phase**, made only when the owner approves, with the ticked `tasks.md` included. Stage files by explicit path; no attribution line. The `appsettings.json` files may be edited: they hold no secrets in the repository, so before staging one, check that `git diff` shows only the intended lines.

## Phase 0 - Decisions and facts
- [x] **T500** (owner confirmed D-1 to D-7 on 2026-11-13; spec and tasks updated; answers a, b and c are in `research.md` section 5: proven for both host types; an empty number is a binding error; Cloudflare bound 172800) Prove three technical points with a throw-away test or console run and record the answers in `research.md`: (a) `ValidateOnStart` stops the start before requests are served in a web host (`WebApplication`) and in a generic host (`Host.CreateDefaultBuilder`); (b) what an empty environment value does to an `int` property (default kept or binding error); (c) Cloudflare's documented maximum lifetime of a TURN credential (fix the upper bound of `Cloudflare:TokenTtl`).
- [x] **T501** (not done: the work stays on `master`, as for the earlier work in this series, and the working tree was clean) Branch `005-settings-defaults-validation` from the updated `master`; confirm nothing unrelated is staged.

## Phase 1 - The mechanism, with `AuthCookie` as its first user
Tests first, in `BE/Infrastructure/Configuration/LetsTalk.Server.Configuration.Tests`:
- [x] **T510** (7 test cases) `SettingsRulesTests` [P]: the range helpers accept the bounds, reject one below and one above, and produce `"Section:Key must be between 1 and 100, but is 0."`; a `TimeSpan` variant. (FR-004)
- [x] **T511** (10 tests, including a started generic host and a started `WebApplication`, with a control that the log line appears when the values are valid) `AddValidatedSettingsTests` [P]: valid section binds and resolves; invalid value makes `IOptions<T>.Value` throw `OptionsValidationException` whose message lists **every** failed rule (AS2.2); a non-numeric value fails with the key named (AS2.3); an absent section yields the class defaults. (FR-003, FR-004)
- [x] **T512** (the 12 old cases rewritten as 24 validator cases, plus a new case for a backslash in the domain, a list-all-failures case and 2 registration tests; the old `AuthCookieSettingsRegistrationTests.cs` is deleted) `AuthCookieSettingsValidatorTests`: the 12 cases of `AuthCookieSettingsRegistrationTests` rewritten against the validator (same inputs, same expected failure); one test that `AddAuthCookieSettings` on an invalid section makes the start fail. (AS4.3, FR-005)

Implementation:
- [x] **T513** (`Validation/SettingsRules.cs`, `SettingsValidatorBase.cs`, `ValidatedSettingsRegistration.cs`; written before their tests in this phase, contrary to the test-first rule, and the tests were then written against the finished code) Add `SettingsRules` and `AddValidatedSettings` to `LetsTalk.Server.Configuration`. (T510, T511; plan D1)
- [x] **T514** (`AuthCookieSettingsValidator`; `AuthCookieSettingsRegistration` is now one call; messages carry the `AuthCookie:` prefix) Add `AuthCookieSettingsValidator`; make `AddAuthCookieSettings` call the mechanism; delete the old registration code and tests that T512 replaced. (T512; plan D5)
- [x] **T515** (2026-11-13: solution builds with 0 errors, all 18 test projects pass (Configuration.Tests 68), `CheckWarnings.ps1` 0 warnings; waiting for the owner's review) `dotnet build BE/LetsTalk.Server.sln`, `dotnet test BE/LetsTalk.Server.sln`. **Checkpoint 1.** Propose the commit message and file list; wait for approval.

## Phase 2 - API settings
Tests first (validator tests in `LetsTalk.Server.Configuration.Tests`, one fixture per class; a case for each bound, one below, one above, plus the default itself):
- [x] **T520** `MessagingSettingsValidatorTests`, `SecuritySettingsValidatorTests`, `CloudflareSettingsValidatorTests` (`TokenTtl` only) [P]. (US2, FR-007)
- [x] **T521** `CachingSettingsValidatorTests` [P] (the models also get XML comments on the `0` and `-1` values, FR-013): for the five lifetimes `-1`, `0`, `1`, `604800` pass and `-2`, `604801` fail (FR-008); `LoginCodeCacheLifeTimeInSeconds` 1 to 3600; `ImageSizeThresholdInBytes` 0 to 10485760. A case that all seven defaults pass.
- [x] **T522** `ImageConstraintsValidatorTests` [P]: each of the six values 1 to 16383; avatar larger than picture (width or height) fails (AS2.4).
- [x] **T523** `DefaultsTests` [P]: `new X()` equals the shipped values for every class of this phase (plan D2), and a section absent from the configuration keeps them (AS1.1, AS1.2).
- [x] **T524** (covered by `DefaultsTests` in the Configuration test project, which resolves `IOptions<T>` from a configuration without the key) `MessagesPerPage` and one cache lifetime: an API-level test that a missing key leaves the shipped value in `IOptions<T>` (US1).

Implementation:
- [x] **T525** Defaults in `MessagingSettings`, `SecuritySettings`, `CloudflareSettings` (`TokenTtl`), `CachingSettings`, `ImageConstraints`; the validators; replace the `Configure<T>` calls in `ApiServiceRegistration.cs` and `CoreServicesRegistration.cs` (API), `AuthenticationServiceRegistration.cs` (Caching) and `FileStorageServiceRegistration.cs` (Caching, ImageConstraints). (T520 to T524; plan D2 to D4)
- [x] **T527** (done 2026-11-13; the owner regenerated the three clients in NSwag Studio. C# and Angular diffs contain only the removed property. The Next.js file also picked up changes that were already stale against the API (`refresh`, `logout`, three `PUT` to `PATCH`, `LoginResponseDto.token` removed); the owner keeps it and will address `frontend-next` later. `npm run build` there was not run: `node_modules` is not installed) (tests first, plan D10, FR-015) Remove `Rtc:MaxVideoDurationInSeconds`: trim `GetCallSettingsQueryHandlerTests` to the ICE-configuration cases (they fail to compile once the property is gone, which is the signal); remove `RtcSettings`, its registration, the handler's options parameter, `CallSettingsDto.MaxVideoDurationInSeconds` and the `Rtc` section of the API `appsettings.json`; regenerate `ApiClient.cs` (`nswag/csharp.nswag`), the Angular `api-client.ts` (`nswag/angular.nswag`) and the Next.js `api-client.ts` (`nswag/nextjs.nswag`, allowed by this spec, D-8), review that each diff is only the removed property; run the Angular tests (`npm run test:run`), `npm run lint` in `frontend-next/` is not needed (generated file only) but `npm run build` there must still compile, and the C# tests.
- [x] **T526** Search the tests for `new CachingSettings`, `new ImageConstraints`, `new MessagingSettings`, `new CloudflareSettings` and any test that relies on a zero default; fix those (plan D8). `dotnet build`, `dotnet test BE/LetsTalk.Server.sln`. **Checkpoint 2.** Propose the commit message and file list; wait for approval.

## Phase 3 - Authentication
Tests first:
- [ ] **T530** `JwtSettingsValidatorTests`: key absent, empty, 31 bytes fail; 32 bytes pass; 32 ASCII characters pass; 17 two-byte characters (34 bytes) pass; 16 two-byte characters (32 bytes) pass; 31 bytes made of 16 characters fails; the failure message does not contain the key (AS3.2, SC-003). Lifetimes: each bound, one below, one above; idle larger than absolute fails (AS2.4); the shipped Development values (`1.00:00:00`) pass (AS5.2). (FR-009, FR-012)
- [ ] **T531** `AuthenticationRegistrationTests` (in the Authentication test project): the service collection built by `AddAuthenticationServices` with a short key makes the start fail, and with a valid key resolves `IOptions<JwtSettings>`. (US3)

Implementation:
- [ ] **T532** `JwtSettingsValidator`; replace `Configure<JwtSettings>` in `AuthenticationServiceRegistration.cs`; list the five lifetimes (not the key) in the Authentication `appsettings.json` (D-4). (T530, T531; plan D6)
- [ ] **T533** `dotnet build`, `dotnet test BE/LetsTalk.Server.sln`. **Checkpoint 3.** Propose the commit message and file list; wait for approval.

## Phase 4 - File storage and the remaining JSON
Tests first:
- [ ] **T540** `FileStorageSettingsValidatorTests` [P]: `ImageFolder` `Images` and `a/b` pass; empty, `/abs`, `C:\x`, `..\x`, `a/../b`, a name with `<` fail; `BasePath` empty fails only when required (D-6). (FR-011)
- [ ] **T541** `ShippedSettingsTests` (drift, plan D2): every key of every in-scope section of each linked shipped file equals the class default and is a known property; the project file links the five shipped `appsettings.json` files. (FR-002, SC-004) [P]
- [ ] **T542** `AuthCookieSectionsAgreeTests`: the `AuthCookie` sections of the API, FileStorage.Service and Notifications files are equal and equal the class defaults. (AS4.2) [P]

Implementation:
- [ ] **T543** `FileStorageSettings` default `ImageFolder = "Images"`, its validator, registration in FileStorage.Service, ImageProcessing.Service and ExtractImageInfo; add `FileStorage:ImageFolder` to the `ExtractImageInfo` `appsettings.json` (it is missing there). (T540; FR-011)
- [ ] **T544** Add the `AuthCookie` section (six defaults) to the `appsettings.json` of the API, FileStorage.Service and Notifications. (T542; FR-010)
- [ ] **T545** `dotnet build`, `dotnet test BE/LetsTalk.Server.sln`. **Checkpoint 4.** Propose the commit message and file list; wait for approval.

## Phase 5 - Required secrets (FR-014)
Tests first, in `LetsTalk.Server.Configuration.Tests` (each: empty, whitespace and null fail; a value passes; the message names the key and never the value):
- [ ] **T560** `SignPackageSettingsValidatorTests`, `CloudflareSettingsValidatorTests` (extend with `TurnTokenId`, `ApiToken`), `LinkPreviewSettingsValidatorTests`, `AwsSettingsValidatorTests` (four keys) [P]. (FR-014)
- [ ] **T561** `EmailServiceSettingsValidatorTests` [P]: five texts non-empty; `Port` 0 and 65536 fail, 25, 587 and 65535 pass. (FR-014)
- [ ] **T562** `AddValidatedSettings` called twice for one class (the API registers `Aws` in two modes) reports each failure once. (plan D1)

Implementation:
- [ ] **T563** Validators; registrations in `SignPackageServiceRegistration.cs`, `CoreServicesRegistration.cs` (Cloudflare, Aws and LinkPreview in the `aws` modes), `LinkPreviewServiceRegistration.cs`, `AmazonFileServicesRegistration.cs`, `EmailServiceRegistration.cs`. No defaults, no JSON change. (T560 to T562)
- [ ] **T564** `dotnet build`, `dotnet test BE/LetsTalk.Server.sln`. **Checkpoint 5.** Propose the commit message and file list; wait for approval.

## Phase 6 - Documentation and verification
- [ ] **T550** `deployment.md` (already drafted) updated with the final limits; `quickstart.md` run by the owner, results recorded in its Result column.
- [ ] **T551** `README.md`: a short "Settings" paragraph (defaults live in the classes, the file lists them, a wrong value stops the start); one sentence in `CLAUDE.md` (Back end section) pointing to the validators, only if a natural place exists.
- [ ] **T552** Final `dotnet build BE/LetsTalk.Server.sln`, `dotnet test BE/LetsTalk.Server.sln`, `backend/CheckWarnings.ps1` for the changed files. **Checkpoint 6.** Propose the commit message and file list; wait for approval.

## Dependencies
T500 first. T510 to T512 can run in parallel; T513 needs T510 and T511; T514 needs T512 and T513. Phases 2 to 4 need Phase 1; within them the validators are independent of each other, so T520 to T523 and T540 to T542 can run in parallel. T541 needs the defaults of Phases 2 to 4 to exist, so it is written in Phase 4 and its expectations for earlier phases are added as those phases land.
