# Implementation Plan: Defaults and validation for the back-end settings

**Spec**: [spec.md](spec.md) | **Research**: [research.md](research.md)\
**Status**: Decisions D-1 to D-7 confirmed by the owner on 2026-11-13 (D-5 widened: all required secrets are validated, spec FR-014).

## Summary
One small mechanism in `LetsTalk.Server.Configuration`: an extension `AddValidatedSettings<TSettings, TValidator>(configuration, sectionName)` that binds the section, registers one `IValidateOptions<TSettings>` and turns on `ValidateOnStart`. One validator class per settings class holds its rules and limits. The property defaults move into the classes. The shipped JSON keeps the values; a test keeps the two in step. `AuthCookie` moves to the same mechanism.

## Technical Context
Same stack as specs 001 to 004 (.NET 10, NUnit + Moq + FluentAssertions). All settings classes are in `Infrastructure/Configuration/LetsTalk.Server.Configuration/Models/`; their tests in `LetsTalk.Server.Configuration.Tests`. The project already has `AddLetsTalkEnvironmentVariables` (secrets spec work) and references `Microsoft.Extensions.Options` through the registration code.

## Constitution Check
| Principle | Status |
|---|---|
| 1 Security first | Pass. No secret added; the key is never printed (D6); the JSON gains only non-secret values. |
| 2 Generated code | Not affected. |
| 3 Provider-neutral persistence | Not affected (no persistence code). |
| 4 Test-first | Pass (tasks order; every rule has a failing test first). |
| 5 Front-end conventions | Not affected. |
| 6 Backward compatibility | Pass with one stated change: an **absent** tunable key now means its shipped value, not 0 (spec NFR-003). Every shipped value and Development override is a test case (FR-012). |
| 7 Spec-driven | Pass once D-1 to D-7 are confirmed. |

## Design Decisions

### D1. The mechanism (FR-003 to FR-006)
In `LetsTalk.Server.Configuration`:
```csharp
public static IServiceCollection AddValidatedSettings<TSettings, TValidator>(
    this IServiceCollection services, IConfiguration configuration, string sectionName)
    where TSettings : class
    where TValidator : class, IValidateOptions<TSettings>
```
It calls `services.AddOptions<TSettings>().Bind(configuration.GetSection(sectionName)).ValidateOnStart()` and `services.TryAddEnumerable(ServiceDescriptor.Singleton<IValidateOptions<TSettings>, TValidator>())`, so registering one class twice (the API registers `Aws` in two modes) does not repeat a failure. Each validator returns `ValidateOptionsResult.Fail(errors)` with **all** failed rules, each prefixed with `"{Section}:{Key}"`, so the framework message lists every problem (AS2.2). A helper `SettingsRules` (small, internal) offers `Range(int value, int min, int max)`, `Range(TimeSpan ...)` and builds the message `"Messaging:MessagesPerPage must be between 1 and 100, but is 0."`, so the wording is the same everywhere.
- A value of the wrong type fails at binding (the binder throws a clear `InvalidOperationException` naming the key); that already stops the start (AS2.3). A test pins this.
- `ValidateOnStart` must stop the start before requests are served, in web and generic hosts. This is proved, not assumed, in T500 with a real host.
- The existing `services.Configure<T>(GetSection(...))` calls of the listed classes are replaced by `AddValidatedSettings`. `IOptions<T>` consumers do not change.

### D2. Defaults in the classes, values in the JSON (FR-001, FR-002, D-7)
Property initializers equal to the shipped values (`public int MessagesPerPage { get; set; } = 20;`). Defaults are written as named constants only where the validator also needs them (for example `public const int DefaultMessagesPerPage = 20`), to avoid a second literal. **Drift test** `ShippedSettingsTests` in `LetsTalk.Server.Configuration.Tests`: the test project links the shipped `appsettings.json` of the API, Authentication, FileStorage.Service, ImageProcessing.Service and ExtractImageInfo as content (`<Content Include=... Link=...>`), builds a configuration from each, binds each in-scope section into a **new** instance, and asserts equality with `new TSettings()` for every property that appears in the file (`BeEquivalentTo`). A key in the file that the class does not know is ignored by the binder, so the test also asserts that every JSON key of an in-scope section is a property of the class (catches a misspelled key, such as `MessagePerPage`, that would silently do nothing today).

### D3. Limits (FR-007, FR-008)
Constants live in the validator class (`public const int MinMessagesPerPage = 1` …), so tests and `deployment.md` quote the same numbers. Limits are those of the spec table. For the five cache lifetimes the rule is `value >= -1 && value <= 604800` (spec FR-008); a value below -1 is rejected because the code treats every negative number alike and `-1` is the one documented spelling. `Cloudflare:TokenTtl` upper bound is 172800 (T500: the official page names none; see research section 5).

### D4. Where each class is registered
| Host | Classes (validated) |
|---|---|
| API | `MessagingSettings`, `SecuritySettings`, `CloudflareSettings`, `CachingSettings`, `ImageConstraints`, `AuthCookieSettings` |
| Authentication | `JwtSettings`, `CachingSettings` |
| FileStorage.Service | `FileStorageSettings`, `ImageConstraints`, `CachingSettings`, `AuthCookieSettings` |
| ImageProcessing.Service | `FileStorageSettings` |
| ExtractImageInfo | `FileStorageSettings` |
| Notifications | `AuthCookieSettings` |

The registrations are in `CoreServicesRegistration.cs`, `ApiServiceRegistration.cs`, `AuthenticationServiceRegistration.cs`, `FileStorageServiceRegistration.cs`, `ImageProcessingServiceRegistration.cs`, `ImageInfoToolServiceRegistration.cs`, `NotificationsServicesRegistration.cs`. `ImageConstraints` and `CachingSettings` are one class each, so each host validates all properties (research, section 1). `CloudflareSettings` is validated for `TokenTtl` only: the two credentials stay unchecked here (D-5).

### D5. `AuthCookie` (FR-005, FR-010, D-3)
`AuthCookieSettingsValidator : IValidateOptions<AuthCookieSettings>` takes over the rules of `GetErrors` unchanged (same wording, same conditions). `AddAuthCookieSettings(configuration)` stays as the public name and becomes a one-line call to `AddValidatedSettings<AuthCookieSettings, AuthCookieSettingsValidator>(configuration, "AuthCookie")`, so the three callers do not change. The 12 tests of `AuthCookieSettingsRegistrationTests` move to a validator test (call `Validate(null, settings)` and assert on the result) plus one registration test (an invalid section makes the host start fail). The three `appsettings.json` files get the section with the six defaults. A test `AuthCookieSectionsAgreeTests` reads the three files and asserts that the sections are equal (AS4.2).

### D6. The JWT key (FR-009)
`JwtSettingsValidator`: `key.Length >= 32` and every character ASCII (`char.IsAscii`), otherwise `"JwtSettings:Key must be at least 32 bytes (256 bits) of ASCII characters; the configured key is missing, shorter, or has other characters."` — the message never includes the key or its length. The key must be ASCII only (owner's decision, 2026-11-13): `JwtStorageService` reads it with `Encoding.ASCII.GetBytes`, which turns every other character into `?`, so a UTF-8 byte count would let a weak key pass; for ASCII, characters and bytes are the same number. Lifetime rules: `AccessTokenLifetime` and `BackgroundTokenLifetime` between 1 minute and 7 days (the Development file sets 1 day), `RefreshIdleLifetime` between 1 hour and 365 days, `RefreshAbsoluteLifetime` between the idle lifetime and 365 days, `RefreshReuseGrace` between 0 and 5 minutes. The null check in `JwtStorageService` that runs after the key is first used (found earlier) becomes unreachable; it stays, because removing a guard is not part of this spec.
Note: length is a minimum, not a strength test; a 32-character key such as `aaaa…` passes. The deployment notes tell the operator to generate a random key (for example 32 random bytes, base64 encoded).

### D7. `FileStorage` rules (FR-011, D-6)
`FileStorageSettingsValidator`: `ImageFolder` not empty, not rooted (`Path.IsPathRooted`), no `..` segment, none of `Path.GetInvalidFileNameChars()` except the directory separators (a nested folder `a/b` is allowed). `BasePath` required where D-6 says: the check needs to know the host, so the validator takes a small flag from the registration (`requireBasePath`), true for ImageProcessing and ExtractImageInfo, and for FileStorage.Service only when `Features:FileStorage` is `Local`. If this makes the mechanism awkward, T543 moves the `BasePath` rule into a second, host-specific validator instead.

### D8. Behavior change and its guard
Because the defaults replace "0 when absent", tests that construct a settings class with `new X()` and rely on zeros would change. A search found `new SecuritySettings` in tests (and `new RtcSettings`, which is removed, D10); all set the property they use, so they are unaffected. Task T526 repeats the search for every class touched and fixes any that depend on zero.

### D9. Required secrets (FR-014, D-5)
Validators with no defaults: `SignPackageSettingsValidator` (`Pepper`), `LinkPreviewSettingsValidator` (`SecretKey`), `AwsSettingsValidator` (four keys), `EmailServiceSettingsValidator` (five texts and the port range), and the two Cloudflare credentials in `CloudflareSettingsValidator`. A helper `SettingsRules.NotEmpty(value, key)` gives `"Aws:SecretKey must be set."` and never prints a value. They run only where the class is registered, and those registrations are tied to the mode (`aws`, the EmailService host), so a local run with `LinkPreviewMode=local` needs no `LinkPreview` secret. The exception found in phase 5: `AddAmazonFileServices` is called by every file-storage host whatever the mode, so it validates `Aws` only when `Features:FileStorage` is `AmazonS3` (`FileStorageMode.Of` reads the mode like `FeaturesSettingsService`; absent or unknown means `Local`). The API's `Cloudflare` registration is unconditional, so the API now needs both Cloudflare credentials to start (it already needed the section, owner's note of 2026-11-13). `Aws:BucketName` is required for every use of the class, although the link-preview lambda does not read it; the four values come from one account, so one rule is simpler than two classes.

### D10. Removing `Rtc:MaxVideoDurationInSeconds` (FR-015)
Only the API reads it: `GetCallSettingsQueryHandler` copies it into `CallSettingsDto.MaxVideoDurationInSeconds`, which no front-end code reads (only the generated clients mention it). The change is a contract change (constitution 6), allowed because no client uses the field: the response keeps `iceServerConfiguration` and loses one number. Steps: (1) tests first: `GetCallSettingsQueryHandlerTests` keeps its ICE-configuration cases (empty and whitespace configuration is a bad request) and loses every case about the duration; (2) remove `RtcSettings`, the `Configure<RtcSettings>` line in `CoreServicesRegistration.cs`, the handler's options parameter and the DTO property; (3) remove the `Rtc` section from the API `appsettings.json`; (4) regenerate all three clients: `ApiClient.cs` with `nswag/csharp.nswag`, the Angular client with `nswag/angular.nswag` and the Next.js client with `nswag/nextjs.nswag` (D-8; this spec allows that run), and review each diff, which must contain only the removed property (constitution 2); if a diff holds more, the generated file was out of date for another reason, and that is reported before it is kept. NSwag reads the running API's OpenAPI document, so the owner may need to run the API, or tell me how the configs are run.

## Risks
- **Limits too tight for production.** Mitigation: wide limits; FR-012; the owner confirms D-1 and compares them with the production files before release (`deployment.md`).
- **`ValidateOnStart` does not stop the start early in some host type.** Mitigation: T500 tests web and generic hosts; if not, the mechanism resolves `IOptions<T>.Value` once at the end of registration in those hosts.
- **Drift test brittle** (paths to files). Mitigation: the files are linked by the project file, so a moved file breaks the build, not the test run.
- **Shared classes validate unused fields.** Accepted (research).
- **Validator messages leak a value.** Mitigation: only non-secret numbers and names are printed; a test asserts the key text is absent from the failure message.
