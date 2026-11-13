# Deployment notes: defaults and validation of settings

Applies to the release of `specs/005-settings-defaults-validation`. No secret, connection string or database change. The limits below are the ones in the validators.

## What changes
- Every tunable setting has its shipped value as a default in the code. The `appsettings.json` files still list those values.
- A service now **checks its settings when it starts**. A wrong value stops the start, and the message names the section, the key and the allowed range. All wrong values are listed in one message.
- The Authentication service refuses a `JwtSettings:Key` shorter than 32 bytes (256 bits) or containing a character that is not ASCII.
- **Required secrets are checked.** A service does not start when one of its required values is empty (they have no default and are not in any committed file): API: `SignPackage:Pepper`, `Cloudflare:TurnTokenId`, `Cloudflare:ApiToken`; FileStorage.Service: `SignPackage:Pepper`, and `FileStorage:BasePath` when `Features:FileStorage` is `Local` or absent; ImageProcessing.Service and ExtractImageInfo: `FileStorage:BasePath`; EmailService: `EmailService:SenderEmail`, `SenderName`, `Server`, `Port`, `UserName`, `Password`. `Aws:AccessKey`, `SecretKey`, `Region` and `BucketName` are required where Amazon is used: in the API when `Features:LinkPreviewMode` or `Features:ImageProcessingMode` is `aws`, and in the file-storage hosts when `Features:FileStorage` is `AmazonS3`. `LinkPreview:SecretKey` is optional and not checked: without it the link-preview service has no fallback for a page that answers 403.
- One behavior change for a **correct** configuration: a tunable key that is **absent** now means its shipped value; before, it meant 0. An explicit `0` is unchanged.
- New sections in the shipped files: `AuthCookie` (API, FileStorage.Service, Notifications), `JwtSettings` lifetimes (Authentication).

## Before you deploy
1. Compare the production `appsettings.json` of each service with the table below. Every value that is there must be inside its allowed range, otherwise that service will not start after the release. The shipped values all are.
2. Check the production `JwtSettings:Key`: 32 or more characters, ASCII only. A key shorter than that is accepted today and refused after the release. If it is shorter, **rotate it first** (new tokens are signed with the new key; existing sessions are signed out), or the Authentication service will not start. Generate a key from random bytes, for example 32 random bytes encoded as base64.
3. Check that each service has its required secrets (list above); a missing one now stops that service at the start.
4. If a production file still holds an old value for `AuthCookie`, make the API, FileStorage and Notifications agree on it: all three must carry the same values.

- The setting `Rtc:MaxVideoDurationInSeconds` is gone, together with the matching field of the call-settings response; no client read it. An `Rtc` section that is still in a deployed `appsettings.json` is ignored.

## Allowed values
| Setting | Allowed |
|---|---|
| `Messaging:MessagesPerPage` | 1 to 100 |
| `Security:AntiSpamTokenLifeTimeInSeconds` | 1 to 3600 |
| `Cloudflare:TokenTtl` | 60 to 172800 |
| `Caching:Messages/Chat/Account/Profile/ImagesCacheLifeTimeInSeconds` | −1 to 604800 (`0` = off, `−1` = never expires) |
| `Caching:LoginCodeCacheLifeTimeInSeconds` | 1 to 3600 |
| `Caching:ImageSizeThresholdInBytes` | 0 to 10485760 |
| `ImageConstraints:*` | 1 to 16383; avatar limits not above picture limits |
| `FileStorage:ImageFolder` | relative folder name, no `..` |
| `FileStorage:BasePath` | required where local files are used |
| `SignPackage:Pepper`, `Cloudflare:TurnTokenId`, `Cloudflare:ApiToken`, `Aws:*`, `EmailService:*` | not empty (`EmailService:Port` 1 to 65535) |
| `JwtSettings:Key` | at least 32 characters, ASCII only |
| `JwtSettings:AccessTokenLifetime`, `BackgroundTokenLifetime` | 1 minute to 7 days |
| `JwtSettings:RefreshIdleLifetime` | 1 hour to 365 days |
| `JwtSettings:RefreshAbsoluteLifetime` | idle lifetime to 365 days |
| `JwtSettings:RefreshReuseGrace` | 0 to 5 minutes |
| `AuthCookie:*` | as before this release |

## What you will see
- **A refused start**: the service exits with `OptionsValidationException`; the message lists each failed rule, for example `Messaging:MessagesPerPage must be between 1 and 100, but is 0.` Fix the value and start again. The message never contains a key or a password.
- **Normal start**: nothing new in the log.

## Rollback
Deploy the previous build. No data changed. If only one value is refused, correct it in `appsettings.json` or the environment variable; no rollback is needed.
