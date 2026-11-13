# Research: defaults in code, values in `appsettings.json`, validation after binding

Checked on 2026-11-13 against the code on `master` (commit `6b999b6b`). Nothing here is a decision; the decisions are in [spec.md](spec.md) ("Decisions to confirm").

## 1. The proposed approach: pros and cons

Proposal (owner): keep every default value in **both** `appsettings.json` and the settings class, so the class is a fallback when someone removes a key; add an `AuthCookie` section; **validate** every settings class after binding, with sensible minimum and maximum values.

### Defaults in the class **and** in the JSON
| | |
|---|---|
| **Pro** | A removed key no longer means "0". Today a missing `MessagesPerPage` gives a page size of 0, a missing `Cloudflare:TokenTtl` gives a cache lifetime of 0. |
| **Pro** | The JSON stays the one readable list of what can be tuned; the class makes the app start with a smaller file (a new project, a test host). |
| **Pro** | The newer classes (`JwtSettings`, `AuthCookieSettings`, `KeepAliveSettings`) already work this way, so the codebase becomes uniform. |
| **Con** | Two copies of one number can drift (class says 120, file says 90, and the file wins, so nobody sees the class value). **Mitigation in the plan (D2):** a test reads each shipped `appsettings.json` and checks that every key equals the class default. |
| **Con** | A key that is removed on purpose, to mean "off", now means "default". For the cache lifetimes this is a real change (see 2), so the plan keeps `0` valid and documents it. |
| **Con** | Classes shared by several hosts (`CachingSettings`, `ImageConstraints`) carry the defaults of every host, although each host reads a part. This is harmless (a host ignores the rest) and the defaults are validated once for all. |

### Validation after binding
| | |
|---|---|
| **Pro** | A wrong value stops the service at the start with the key and the rule in the message, instead of a wrong behavior later (an access token that never expires, a page size of 0, a cookie the browser drops). |
| **Pro** | The Authentication service signs tokens with `JwtSettings:Key`. HMAC-SHA256 needs at least 256 bits; today any non-empty key starts the service. |
| **Con** | A limit that is too tight blocks a legitimate change. Mitigation: wide limits, chosen below, and every shipped value and every Development override is a test case. |
| **Con** | More code than `Configure<T>`. Mitigation: one small helper and one `IValidateOptions<T>` per class, each testable without a host. |
| **Con** | Validation of a shared class runs in every host that binds it, including fields the host does not use. Harmless while the defaults are valid. |

**Verdict**: sound, with four conditions that the spec and plan take over: (1) limits are checked against what the code does with the value (section 2), (2) an automatic drift check between class and JSON, (3) one mechanism for all classes, including the existing `AuthCookie` validation, (4) error messages never contain a secret value.

## 2. What the code does with each value (so the limits are right)

| Setting | Used by | What the code does | Consequence for validation |
|---|---|---|---|
| `Caching:MessagesCache…`, `ChatCache…`, `AccountCache…`, `ProfileCache…`, `ImagesCache…` (seconds) | `*CacheServiceBase`, `ImageStorageRedisCacheService` | `0` = cache **off**; a **negative** value = cache **on, never expires**; positive = lifetime | "Must be positive" would be **wrong**: `0` and negatives are valid, documented switches. The range needs a lower bound that keeps them. |
| `Caching:LoginCodeCacheLifeTimeInSeconds` | `LoginCodeCacheServiceBase` | used directly as `TimeSpan.FromSeconds(value)` for an expiry | must be positive; `0` or negative would store a code that expires at once or never |
| `Caching:ImageSizeThresholdInBytes` | `ImageStorageRedisCacheService` | an image is cached only when `Length < threshold` | `0` = nothing is cached; negative is meaningless |
| `Messaging:MessagesPerPage` | `MessageController` | page size passed to the query | positive; an upper bound protects the database |
| `Security:AntiSpamTokenLifeTimeInSeconds` | both request validators | `abs(now - tokenTime) < value` | positive (0 rejects every request) |
| `Rtc:MaxVideoDurationInSeconds` | `GetCallSettingsQueryHandler` | copied into the call-settings response; no client reads it | **removed** by the owner's decision (spec FR-015), not validated |
| `Cloudflare:TokenTtl` | `IceServerConfigurationCacheServiceBase` | cache lifetime of the ICE configuration | positive; the upper bound must be checked against Cloudflare's own limit for TURN credentials before it is fixed (task T500) |
| `ImageConstraints:*MaxWidth/Height` | `ImageValidationService`, both launchers | an upload larger than the limit is rejected; a preview is resized to the limit | positive; WebP, the only accepted format, cannot exceed 16383 pixels per side, so a larger limit has no meaning |
| `FileStorage:ImageFolder` | `FileStoragePathProvider` | `Path.Combine(BasePath, ImageFolder, filename)` | must be a relative folder name; an absolute path or `..` would leave `BasePath` |
| `FileStorage:BasePath` | same | `null` throws a `NullReferenceException` at the first upload | must be set where local files are used (FileStorage in `Local` mode, ImageProcessing, ExtractImageInfo) |
| `JwtSettings:*` | Authentication only | `Key` signs and checks every JWT; lifetimes decide expiry | key at least 32 bytes; lifetimes positive and ordered (idle ≤ absolute). `appsettings.Development.json` of Authentication sets access and background lifetimes to **1 day**, so the upper bounds must allow it |
| `AuthCookie:*` | API, FileStorage.Service, Notifications | validated today by hand-written code (`AuthCookieSettingsRegistration`) | the same rules, moved to the common mechanism |

## 3. Verification of the configuration files

Every committed `appsettings*.json` was read. Result:

**Settings the owner listed: all present and correct.** `Messaging`, `Caching` (API), `Security`, `Rtc` (now removed, FR-015), `Cloudflare:TokenTtl`, `ImageConstraints:ImagePreview*` in the API; `Caching:LoginCodeCacheLifeTimeInSeconds` in Authentication; `FileStorage:ImageFolder`, `ImageConstraints:Avatar*/Picture*`, `Caching:ImagesCacheLifeTimeInSeconds` and `ImageSizeThresholdInBytes` in FileStorage.Service; `FileStorage:ImageFolder` in ImageProcessing.Service.

**Missing from the list (found):**
1. `FileStorage:ImageFolder` in **`LetsTalk.Server.Tools.ExtractImageInfo`** (`appsettings.json`) is the same setting as in the two services and needs the same default and the same validation. (Correction made in phase 4: its `appsettings.json` already lists `ImageFolder`; only the class default and the check were missing.)
2. **`JwtSettings`** (five lifetimes) has defaults in the class but **no section in any committed `appsettings.json`**, so the "both places" rule is not met for it; the class also has no validation. Proposed: add the lifetimes to the Authentication `appsettings.json` (not the key).
3. **`FileStorage:BasePath`** has no default and cannot have one (it is a deployment path, now supplied through the environment), but its absence is a silent time bomb; it needs a required-value check (decision D-6).
4. **`AuthCookie`** is absent from all three hosts that read it (API, FileStorage.Service, Notifications). It must carry the same values in all three, otherwise one host cannot read the cookie another set. A test comparing the three sections is proposed.
5. **`KeepAlive`** already has defaults in code and an explicit section in the API JSON; its validation is hand-written in `KeepAliveService`. Out of scope, listed for completeness.
6. **Required secrets** with no check: `SignPackage:Pepper` (an empty pepper is silently accepted: `?? string.Empty`), `Cloudflare:ApiToken` and `TurnTokenId`, `LinkPreview:SecretKey`, `EmailService:*`, `Aws:*`. Which of these are required depends on the mode in use (decision D-5).

**Deliberately not given defaults (and why):** `ConnectionStrings`, `Kafka`, `Topics`, `Queues`, `ApplicationUrls`, `Kestrel`, `Cors`, `MongoDB:DatabaseName`, `CosmosDB:DatabaseName`, `Features:*`, `Serilog`, `Logging`, `AllowedHosts`. They are addresses, names and modes that differ per environment or must match across services; a code default would hide a missing value instead of failing. (`MongoDB:DatabaseName` is read with `GetValue` and bypasses the model anyway.)

**Also noticed, not part of this work:** `Features:*` modes are read as plain strings with a silent fallback (an unknown `CachingMode` means in-memory). The telemetry mode already rejects unknown values; the others could do the same later. Proposed as a follow-up, not as part of this spec.

## 4. Mechanism: how to validate

Options considered:
- **A. Keep the current style**: bind by hand at registration, throw `InvalidOperationException`. Works, eager, but every class repeats the binding code, and a test host that overrides configuration after registration is not seen by the check.
- **B. Options pattern**: `AddOptions<T>().Bind(section).ValidateOnStart()` with one `IValidateOptions<T>` per class. The framework way; validates the final configuration; collects all errors into one message; the validator is a plain class that tests call directly. **Recommended.**
- **C. DataAnnotations (`[Range]`) on the classes**: shortest, but cross-field rules (idle ≤ absolute, avatar ≤ picture), the UTF-8 byte length of the key and the cache `0 / -1` switch need custom code anyway, and the attributes put limits in the model files where they are less visible. Not chosen.

Open technical point to prove, not assume (task T500): in .NET 10 `ValidateOnStart` must fail the start **before** the web server accepts requests, in both web hosts and generic hosts. A test that starts a real host with an invalid value settles it.

## 5. Answers to the open points of task T500

Checked on 2026-11-13.

**(a) Does `ValidateOnStart` stop the start before the server accepts requests?** Yes, for both host types. Tests in `LetsTalk.Server.Configuration.Tests` (`AddValidatedSettingsTests`) start a generic host and a `WebApplication` with an invalid value: both throw `OptionsValidationException` and the "Now listening on" line is never logged. A control test with valid values starts the same hosts, and the line appears, so the first result is not caused by a faulty test set-up.

**(b) What does an empty text do for a number?** An empty value for an `int` key (`LetsTalk_Messaging__MessagesPerPage=`) is a binding `InvalidOperationException` that names the key, so the service does not start. It does not fall back to the default. A key that is absent keeps the default of the property. For a text property an empty value is just an empty text, which the `NotEmpty` rule rejects.

**(c) Maximum lifetime of a Cloudflare TURN credential.** It is not documented on the official page (`https://developers.cloudflare.com/calls/turn/generate-credentials`), which shows a `ttl` of 86400 as an example and says only that it can be adjusted. Two third-party reference files (npm packages, not Cloudflare) state 172800 seconds (48 hours) as the limit and say the API rejects larger values. This figure is therefore **unofficial**. The upper bound of `Cloudflare:TokenTtl` is set to **172800**: it covers the shipped 86400 and does not allow what the API would reject if the third-party figure is right. If Cloudflare publishes a different limit, change the constant in the validator and `deployment.md`.
