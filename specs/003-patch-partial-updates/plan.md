# Implementation Plan: Partial message updates use PATCH

**Spec**: [spec.md](spec.md)

## Summary
Change `[HttpPut]` to `[HttpPatch]` on three actions in `MessageController`, regenerate the Angular and C# API clients, and redeploy the cloud functions together with the API. No route, DTO, handler or persistence change.

## Technical Context
Same stack as specs 001 and 002. Persistence providers are not touched, so constitution principle 3 does not apply.

## Constitution Check
| Principle | Status |
|---|---|
| 1 Security first | Pass. No auth, token or origin change. `OriginCheckMiddleware` already covers every non-safe method, and `PATCH` is non-safe, so it is still checked (verify in T200). |
| 2 Generated code | Pass. Regenerate `angular.nswag` and `csharp.nswag`; never `nextjs.nswag`. |
| 3 Provider-neutral persistence | Not affected. |
| 4 Test-first | Pass (tasks order). |
| 5 Front-end conventions | Pass. Only generated code changes in `frontend/`. |
| 6 Backward compatibility | **Violation, justified.** The old verb stops working. Justification: owner decision for a simpler change; the only affected callers are in this repository, the impact is a missing preview during the deployment gap, and the migration order is in the spec. |
| 7 Spec-driven | Pass. |

## Design Decisions

### D1. Verb only
`MessageController.cs`: `MarkAsReadAsync`, `SetLinkPreviewAsync`, `SetImagePreviewAsync` get `[HttpPatch("MarkAsRead")]`, `[HttpPatch("SetLinkPreview")]`, `[HttpPatch("SetImagePreview")]`. Everything else stays (FR-001, FR-004).

### D2. Origin check applies to PATCH
`OriginCheckMiddleware` decides by "safe method" (GET, HEAD, OPTIONS, TRACE). Confirm `PATCH` falls on the checked side; add a test case if the existing tests cover only POST/PUT (NFR-001, constitution 1).

### D3. CORS
`CorsServiceCollectionExtensions` uses `AllowAnyMethod()`, so the preflight for `PATCH` already succeeds. No change.

### D4. Clients are regenerated, not edited
- `nswag/angular.nswag` rewrites `frontend/src/app/api-client/api-client.ts`: `markAsRead`, `setLinkPreview`, `setImagePreview` now send `PATCH`. Method names and signatures are unchanged, so `api.service.ts` and the specs need no edit.
- `nswag/csharp.nswag` rewrites `backend/API/LetsTalk.Server.API.Client/ApiClient.cs`. Method names are unchanged, so the four callers (`ImageResizeRequestConsumer`, `LinkPreviewRequestConsumer`, `ImageProcessingLambda`, `LinkPreviewLambda`) need no code change, only a rebuild and, for the Lambdas, a redeploy.
- `nextjs.nswag` is not run (CLAUDE.md, constitution 2).
- Check the diff of each generated file: only the three verbs may change (plus the stored spec snapshot in the `.nswag` files, as in spec 002 T140).

### D5. Test for the contract
`MessageController` has no tests. Add `MessageControllerTests` in `Applications/LetsTalk.Server.API.Tests` that reads the action attributes by reflection: the three actions carry `HttpPatchAttribute` with the same templates and no `HttpPutAttribute`. This protects against a silent revert and covers FR-001 and FR-002 without starting the host.

### D6. Deployment order
Documented in `deployment.md`: cloud functions and API released together; after the release, send one image and one link as a check.

## Risks
- The Lambdas are the only callers outside the repo build. If one is not redeployed, its previews fail with 405 and nothing retries. Mitigation: `deployment.md` and the quickstart step Q4.
- A browser tab with the old Angular build fails to mark messages as read until reloaded (accepted, spec edge cases).
