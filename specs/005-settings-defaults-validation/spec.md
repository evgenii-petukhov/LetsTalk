# Feature Spec: Defaults and validation for the back-end settings

**Feature folder**: `specs/005-settings-defaults-validation`\
**Status**: Decisions D-1 to D-7 confirmed by the owner on 2026-11-13, with two changes: D-5 is widened (every required secret is validated, see below) and the cache switches `0` and `-1` are documented in the model. Written after the owner's request and the check in [research.md](research.md).\
**Created**: 2026-11-13\
**Depends on**: none (the secrets and `LetsTalk_` environment variable work is already on `master`)

## Problem
The back-end services read their tunable values (page size, cache lifetimes, image limits, token lifetimes, cookie names) from `appsettings.json`. Two weaknesses follow:
- **A removed or misspelled key silently becomes zero.** Most settings classes have no default, so a missing `Messaging:MessagesPerPage` gives a page size of 0, a missing `Cloudflare:TokenTtl` a cache lifetime of 0, a missing `ImageConstraints` section rejects every upload. Nothing says so at the start.
- **A wrong value is accepted and fails later, somewhere else.** Only `AuthCookie` is checked when the service starts. A negative token lifetime, an image limit of 0, or a signing key of three characters starts the service normally. The Authentication service signs every token with `JwtSettings:Key`, and any non-empty text is accepted today.

Three years of production use have not changed the shipped values, so the files are mostly a list of constants; with the secrets now outside them, an operator or a new project should be able to start the services with a short file, and a bad value should be refused loudly.

## Goal
Every settings class carries its shipped values as defaults, and every settings class is checked after binding. A service whose configuration is invalid does not start and says which key is wrong and what is allowed. The shipped `appsettings.json` files still list the values, so they stay visible.

## User Stories

### US1 - A removed setting keeps its normal value (P1)
As an operator, when I delete a tunable key from `appsettings.json`, the service behaves as it does with the shipped value.
- **AS1.1** Given `Messaging:MessagesPerPage` is absent, then the page size is 20.
- **AS1.2** Given a whole section (for example `Caching`) is absent, then every value of it is the shipped one.
- **AS1.3** Given a key is present, then its value wins over the default, as today.

### US2 - A wrong value stops the service at the start (P1)
- **AS2.1** Given a value outside its allowed range (for example `Messaging:MessagesPerPage` = 0 or 100000), then the service does not start and the message names the section and key, the value and the allowed range.
- **AS2.2** Given several values are wrong, then one message lists all of them, so the operator fixes them in one pass.
- **AS2.3** Given a value does not have the right type (text where a number is expected), then the service does not start and says which key.
- **AS2.4** Given two values depend on each other (the absolute session limit is shorter than the idle limit; the avatar limit is larger than the picture limit), then the service does not start and says which rule is broken.

### US3 - A weak or missing signing key stops the Authentication service (P1)
- **AS3.1** Given `JwtSettings:Key` is absent, empty or shorter than 256 bits, then the Authentication service does not start.
- **AS3.2** Given the message, then it states the required length and never contains the key.

### US4 - The cookie settings are visible and agree across services (P2)
- **AS4.1** Given the API, FileStorage and Notifications services, each lists the `AuthCookie` section with the shipped values.
- **AS4.2** Given the three sections differ, then an automatic test fails, because the services must agree on the cookie names and path.
- **AS4.3** Given the cookie rules that exist today (known SameSite value, `Secure` with `SameSite=None`, refresh name different from the name, absolute refresh path, host-name domain), then they still stop the service at the start, with the same meaning.

### US5 - Overriding still works the same way (P2)
- **AS5.1** Given a value is set in the deployed `appsettings.json`, in a `LetsTalk_`-prefixed environment variable, or in `appsettings.Development.json`, then it overrides the default and is validated like any other.
- **AS5.2** Given the Development files shipped today (`AccessTokenLifetime` and `BackgroundTokenLifetime` of one day in Authentication), then they are valid.

## Functional Requirements
- **FR-001** Every settings class that holds a tunable value MUST carry the shipped value of each such property as its default. Secrets and deployment-specific values (keys, tokens, passwords, addresses, paths) get **no** default.
- **FR-002** The shipped `appsettings.json` files MUST keep listing the tunable values (owner's decision), and a test MUST fail when a listed value differs from the class default, so the two copies cannot drift.
- **FR-003** Every settings class listed in "Settings in scope" MUST be validated once, when the service starts, after binding and after all configuration sources (files, environment) are applied.
- **FR-004** A failed validation MUST stop the start, name the section and key of every failed rule in one message, and state the allowed range or rule (FR-007).
- **FR-005** The same mechanism MUST be used for every settings class, including `AuthCookie`, which today has its own code.
- **FR-006** A service MUST validate only the classes it binds. Classes shared by several services are validated in each of them.
- **FR-007** The allowed values are those of the table in "Settings in scope". They are constants in the validator, documented in `deployment.md`.
- **FR-008** The cache lifetimes `Messages`, `Chat`, `Account`, `Profile` and `Images` MUST keep their present meaning: `0` switches that cache off and a negative value means it never expires (see research, section 2). Validation MUST NOT reject these two values.
- **FR-009** `JwtSettings:Key` MUST be at least 32 characters (256 bits) and consist of ASCII characters only, because the signing code reads the key as ASCII and would turn any other character into `?` (owner's decision, 2026-11-13). Its value MUST NOT appear in any message or log.
- **FR-010** The `AuthCookie` section MUST be added, with the shipped defaults, to `appsettings.json` of the API, FileStorage.Service and Notifications.
- **FR-011** `FileStorage:ImageFolder` MUST be validated in every host that binds it, including the `ExtractImageInfo` tool (the owner plans to remove that project later, so this is the minimum; its `appsettings.json` already lists the key, which the first check of 2026-11-13 missed), and MUST be a relative folder name that stays inside `FileStorage:BasePath` (no root, no `..`).
- **FR-013** The meaning of the special cache values (`0` = off, `-1` = never expires, any other negative value is rejected) MUST be documented on the properties of the model (XML comments), not only in this spec.
- **FR-014** Required secrets MUST be validated as non-empty text and get **no** default and **no** entry in any `appsettings.json` (owner's decision): `SignPackage:Pepper`, `Cloudflare:TurnTokenId` and `Cloudflare:ApiToken`, `LinkPreview:SecretKey`, `Aws:AccessKey`, `SecretKey`, `Region` and `BucketName`, and `EmailService:SenderEmail`, `SenderName`, `Server`, `UserName`, `Password` with `Port` between 1 and 65535. Each is validated only in a service that registers the class, and the registration is already limited to the mode that needs it (for example `Aws` and `LinkPreview` in the API only when the link-preview mode is `aws`). `FileStorage:BasePath` is validated the same way (D-6).
- **FR-015** The setting `Rtc:MaxVideoDurationInSeconds` MUST be removed (owner's decision, 2026-11-13: it was added for a future need that will not come, and no client reads it): the `Rtc` section of the API's `appsettings.json`, the settings class `RtcSettings`, its registration, the property `MaxVideoDurationInSeconds` of the call-settings response and its handler code, and their tests. An `Rtc` section left in a deployed file is ignored. All three generated API clients are regenerated (owner's decision D-8): the C# `ApiClient.cs` (`nswag/csharp.nswag`), the Angular `api-client.ts` (`nswag/angular.nswag`) and the Next.js `api-client.ts` (`nswag/nextjs.nswag`). This spec therefore allows the one run of `nextjs.nswag` and the change to that single generated file in `frontend-next/` that CLAUDE.md otherwise forbids; nothing else in `frontend-next/` is touched.
- **FR-012** A shipped value, and each value in a shipped Development file, MUST pass its own validation (backward compatibility, constitution 6).

## Non-Functional Requirements
- **NFR-001** No secret is added to a file or a log (constitution 1). Messages show a value only when it is not a secret.
- **NFR-002** Validation runs once at the start and adds no cost per request.
- **NFR-003** The change is additive for a correct configuration: with the shipped files, every service behaves as before. The one deliberate change is that an **absent** tunable key now means its shipped value instead of 0 (FR-001); an **explicit** `0` keeps its meaning where the code gives it one (FR-008).

## Settings in scope

Defaults equal the values now shipped. Ranges are proposals (D-1). "JSON" says where the shipped file lists the value.

| Section: key | Default | Allowed | Read by | JSON |
|---|---|---|---|---|
| `Messaging:MessagesPerPage` | 20 | 1 to 100 | API | API (exists) |
| `Security:AntiSpamTokenLifeTimeInSeconds` | 60 | 1 to 3600 | API | API (exists) |
| `Cloudflare:TokenTtl` | 86400 | 60 to 172800 (Cloudflare documents no maximum; 172800 is the figure of two third-party references, research section 5) | API | API (exists) |
| `Caching:MessagesCacheLifeTimeInSeconds` | 120 | −1 to 604800 (0 = off, −1 = never expires) | API | API (exists) |
| `Caching:ChatCacheLifeTimeInSeconds` | 30 | same | API | API (exists) |
| `Caching:AccountCacheLifeTimeInSeconds` | 30 | same | API | API (exists) |
| `Caching:ProfileCacheLifeTimeInSeconds` | 30 | same | API | API (exists) |
| `Caching:LoginCodeCacheLifeTimeInSeconds` | 120 | 1 to 3600 | Authentication | Authentication (exists) |
| `Caching:ImagesCacheLifeTimeInSeconds` | 86400 | −1 to 604800 (0 = off, −1 = never expires) | FileStorage | FileStorage (exists) |
| `Caching:ImageSizeThresholdInBytes` | 102400 | 0 to 10485760 (0 = nothing is cached) | FileStorage | FileStorage (exists) |
| `ImageConstraints:ImagePreviewMaxWidth`, `…Height` | 512 | 1 to 16383 | API | API (exists) |
| `ImageConstraints:AvatarMaxWidth`, `…Height` | 512 | 1 to 16383, and at most the picture limit | FileStorage | FileStorage (exists) |
| `ImageConstraints:PictureMaxWidth`, `…Height` | 4000 | 1 to 16383 | FileStorage | FileStorage (exists) |
| `FileStorage:ImageFolder` | `Images` | relative folder name, no `..`, no invalid file-name characters | FileStorage, ImageProcessing, ExtractImageInfo | all three (exists) |
| `FileStorage:BasePath` | none (deployment value) | required where local files are used (D-6) | same three | not listed (environment) |
| `AuthCookie:*` | as `AuthCookieSettings` today | rules of `AuthCookieSettingsRegistration` today | API, FileStorage, Notifications | all three (**new**, FR-010) |
| `SignPackage:Pepper` | none (secret) | non-empty | API (and every host that signs packages) | not listed |
| `Cloudflare:TurnTokenId`, `Cloudflare:ApiToken` | none (secrets) | non-empty | API | not listed |
| `LinkPreview:SecretKey` | none (secret) | non-empty | API (mode `aws`), LinkPreview | not listed |
| `Aws:AccessKey`, `SecretKey`, `Region`, `BucketName` | none (secrets, deployment values) | non-empty | API (modes `aws`), FileStorage.Service (Amazon storage) | not listed |
| `EmailService:SenderEmail`, `SenderName`, `Server`, `UserName`, `Password` | none | non-empty | EmailService | not listed |
| `EmailService:Port` | none | 1 to 65535 | EmailService | not listed |
| `JwtSettings:Key` | none (secret) | at least 32 characters, ASCII only, never shown | Authentication | not listed (environment) |
| `JwtSettings:AccessTokenLifetime` | 15 min | 1 min to 7 days | Authentication | Authentication (**new**, D-4) |
| `JwtSettings:BackgroundTokenLifetime` | 1 hour | 1 min to 7 days | Authentication | Authentication (**new**, D-4) |
| `JwtSettings:RefreshIdleLifetime` | 30 days | 1 hour to 365 days | Authentication | Authentication (**new**, D-4) |
| `JwtSettings:RefreshAbsoluteLifetime` | 90 days | at least the idle lifetime, at most 365 days | Authentication | Authentication (**new**, D-4) |
| `JwtSettings:RefreshReuseGrace` | 30 s | 0 to 5 min | Authentication | Authentication (**new**, D-4) |

## Decisions to confirm
Each has a recommendation; the plan follows it unless the owner decides otherwise.
- **D-1 Ranges.** The ranges above are wide on purpose: they exist to catch typos and unit mistakes (a lifetime given in milliseconds, a page size of a million), not to second-guess tuning. *Confirmed: as listed.* Confirmed by the owner.
- **D-2 Cache switches.** `0` and a negative value stay valid for the five cache lifetimes (FR-008), because the code gives them meaning. *Recommended.* Alternative: make them positive and add a separate on/off key, which changes the configuration format and is not proposed.
- **D-3 One mechanism for all** (FR-005): the existing `AuthCookie` check is moved to it, with the same rules and messages; its tests are adapted. *Recommended.* Alternative: leave `AuthCookie` as it is and add the new mechanism beside it (two styles).
- **D-4 `JwtSettings` lifetimes in the Authentication JSON.** The class has defaults but the file does not list them. *Recommended: list them (not the key), so the "both places" rule holds.*
- **D-5 Required secrets.** Confirmed and widened: all required secrets of FR-014 are validated, not only `JwtSettings:Key`. Validation only, no defaults, nothing added to any JSON.
- **D-6 `FileStorage:BasePath`.** Confirmed: required (non-empty) in FileStorage.Service when `Features:FileStorage` is `Local`, and always in ImageProcessing.Service and ExtractImageInfo; no default and no JSON entry.
- **D-8 The Next.js client.** Confirmed by the owner: it is regenerated together with the other two (FR-015).
- **D-7 Drift test** (FR-002): a test reads the shipped `appsettings.json` files and compares each tunable key with the class default. *Recommended;* it is what makes keeping both copies safe.

## Edge Cases
- A key set to an empty text through an environment variable (`LetsTalk_Messaging__MessagesPerPage=`) may bind as "not set" (the default stays) or fail as a number, depending on the binder; the plan proves which and the quickstart records it (T500).
- `Caching` is one class for three services. Authentication does not use the five lifetimes of the API, but they are validated there too; the defaults are valid, so only an explicit wrong value in Authentication's own file would stop it.
- An existing deployment whose `appsettings.json` still holds the old values keeps working: every old value is inside its range (FR-012). One whose file omits a key now gets the default instead of 0 (NFR-003).
- The cache lifetime check also applies to `Development` files; no Development file sets one today.
- `AllowedHosts`, Kestrel, logging and the like are not settings classes and are not validated here.

## Success Criteria
- **SC-001** With every tunable key removed from the API's, Authentication's and FileStorage's `appsettings.json`, each service starts and behaves as with the shipped files (quickstart Q1).
- **SC-002** For each rule in the table there is a unit test that fails before the change; each service refuses to start for at least one wrong value, with the message of AS2.1 (quickstart Q2 to Q6).
- **SC-003** The Authentication service refuses a 31-byte key and accepts a 32-byte key; neither message nor log contains it (Q7).
- **SC-004** The drift test and the cookie-agreement test pass, and fail when a shipped value or a cookie section is changed (Q8).
- **SC-005** The whole solution builds and all tests pass.

## Out of scope
- Rotating keys, changing git history, and the secrets in `deployment` files.
- Secrets with a value that is optional in some mode (none are known); `AutoMapper:LicenseKey`, `MediatR:LicenseKey` and `ApplicationInsights:ConnectionString` (the last is already checked by the telemetry mode).
- Validating `Features:*` modes (an unknown mode still falls back silently, except telemetry); proposed as a later spec.
- Defaults for addresses, names and modes: `ConnectionStrings`, `Kafka`, `Topics`, `Queues`, `ApplicationUrls`, `Kestrel`, `Cors`, database names (research, section 3).
- `KeepAlive` (already has defaults and its own checks).
- Reloading settings while a service runs; changes still need a restart.
- Moving topic and queue names into shared constants.
