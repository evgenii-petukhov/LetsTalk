# Quickstart: verify the defaults and the validation

Needs the local set-up of the README (the `LetsTalk_` secrets are set, MySQL and Redis run). Each row changes one value, starts **one** service from its folder with `dotnet run`, and reads the first lines of the console. Revert every change afterwards; **do not commit these edits**. Rows are independent: undo the previous change before the next.

The message of a refused start has the form `Messaging:MessagesPerPage must be between 1 and 100, but is 0.` (wording of the plan, D1). Match on the key and the rule, not on the full text.

| # | Change, then start | Expected | Covers | Result |
|---|---|---|---|---|
| Q1a | API. Before the run, clear the Redis cache (`redis-cli FLUSHDB` on the development database) or set `Features:CachingMode` to `memory`, because the first page of messages is cached and an old entry would hide the page size. In the API `appsettings.json`, delete the sections `Messaging`, `Caching`, `Security`, `ImageConstraints` and `Cloudflare` completely; start the API; open a chat | The API starts. Messages come in pages of 20 (the first request after the cache was cleared); a second request for the same chat is served from the cache | US1, SC-001 | |
| Q1b | Authentication. Delete the `Caching` section of its `appsettings.json`; start the service | The service starts | US1, SC-001 | |
| Q1c | FileStorage.Service. Delete the `Caching`, `ImageConstraints` and `FileStorage` sections of its `appsettings.json` (`FileStorage:BasePath` comes from the environment variable, so it stays set); start the service | The service starts | US1, SC-001 | |
| Q2 | API: `Messaging:MessagesPerPage` = `0` | The API does not start; the message names `Messaging:MessagesPerPage` and `1 to 100` | AS2.1 | |
| Q3 | API: `Messaging:MessagesPerPage` = `0` **and** `Security:AntiSpamTokenLifeTimeInSeconds` = `-5` **and** `Caching:ChatCacheLifeTimeInSeconds` = `-7` | The API **does not start**, and the one error message lists all three keys (`Messaging:MessagesPerPage`, `Security:AntiSpamTokenLifeTimeInSeconds`, `Caching:ChatCacheLifeTimeInSeconds`) | AS2.2 | |
| Q4 | API: `Messaging:MessagesPerPage` = `"twenty"` | The API does not start; the message names the key | AS2.3 | |
| Q5 | API: `Caching:ChatCacheLifeTimeInSeconds` = `0`, start, then `-1`, start | Both start; with `0` the chat is read from the database on every request, with `-1` the cache entry has no expiry (check with `redis-cli TTL`) | FR-008 | |
| Q6 | FileStorage.Service: `ImageConstraints:AvatarMaxWidth` = `5000` (picture limit stays 4000) | Does not start; the message names the avatar and picture limits | AS2.4 | |
| Q7 | Authentication: set `LetsTalk_JwtSettings__Key` to 31 characters, start; then to 32 characters, start | 31: does not start, the message says "at least 32 bytes" and does not contain the key. 32: starts | AS3.1, AS3.2, SC-003 | |
| Q8 | In the Notifications `appsettings.json` add `"AuthCookie": { "Name": "other" }`; run the unit tests of `LetsTalk.Server.Configuration.Tests` | The cookie-agreement test fails and names the file. Revert: it passes. Also change one value in a shipped `Caching` section: the drift test fails | AS4.2, SC-004 | |
| Q9 | API: `AuthCookie:SameSite` = `None` and `AuthCookie:Secure` = `false` | Does not start (as before the change) | AS4.3 | |
| Q10 | Authentication: set `LetsTalk_JwtSettings__AccessTokenLifetime` to `1.00:00:00` (the Development value) | Starts | AS5.1, AS5.2 | |
| Q11 | Set `LetsTalk_Messaging__MessagesPerPage=` (empty) in a fresh PowerShell window and start the API | The API **does not start**; the message names `Messaging:MessagesPerPage` (an empty number is a binding error, research section 5) | Edge case | |
| Q12 | Start every service with the shipped files and use the Angular app: sign in, send a message and an image, scroll up, start a call | Everything works as before the change | NFR-003 | |
| Q13 | Call `GET api/…` for the call settings after the change (or open a video call in the Angular app) | The response has `iceServerConfiguration` and no `maxVideoDurationInSeconds`; the call still starts | FR-015 | |

Record the date and outcome in the Result column when you run each row.
