# Tasks: Partial message updates use PATCH

**Inputs**: [spec.md](spec.md), [plan.md](plan.md).\
**Precondition**: `specs/002-short-lived-token-refresh` is merged (it is: no open tasks).\
**Conventions**: as in specs 001 and 002 (tests before code, `Method_WhenCondition_ShouldOutcome`). `BE` = `backend/`, `FE` = `frontend/src/app/`.\
**Commit message prefix: `PATCH verbs: `** (one line, for example `PATCH verbs: use PATCH for partial message updates`).\
**One commit per phase**, made only when the owner approves, with the ticked `tasks.md` included. Stage files by explicit path.

## Phase 0 - Setup
- [x] **T300** (branch created from `master`; the spec files were committed first as `540d388c`) Branch `003-patch-partial-updates` from the updated `master`; confirm the working tree is clean and nothing unrelated is staged.

## Phase 1 - API
Tests first:
- [x] **T310** (new `MessageControllerTests`, 4 cases; first run failed for the right reason: the actions were still `HttpPut`) `MessageControllerTests` in `BE/Applications/LetsTalk.Server.API.Tests` (reflection on the action attributes): `MarkAsReadAsync_ShouldBePatchWithTemplateMarkAsRead`; `SetLinkPreviewAsync_ShouldBePatchWithTemplateSetLinkPreview`; `SetImagePreviewAsync_ShouldBePatchWithTemplateSetImagePreview`; `MessageController_ShouldHaveNoPutActions`. Run them and see them fail. (FR-001, FR-002)
- [x] **T311** (no new tests needed: `OriginCheckMiddlewareTests` already runs `PATCH` among the state-changing methods, both refused and, via the shared logic, allowed; the middleware treats only GET/HEAD/OPTIONS/TRACE as safe) [P] `OriginCheckMiddlewareTests` in `BE/API/LetsTalk.Server.API.Middleware.Tests`: add `InvokeAsync_WhenCookieAndPatchAndForeignOrigin_ShouldReturn403` and `..._WhenCookieAndPatchAndAllowedOrigin_ShouldContinue` if no existing case uses `PATCH`; fix the middleware only if they fail. (NFR-001, plan D2)

Implementation:
- [x] **T312** Replace the three `[HttpPut(...)]` with `[HttpPatch(...)]` in `BE/Applications/LetsTalk.Server.API/Controllers/MessageController.cs`. (T310; FR-001, FR-002)
- [x] **T313** (2026-11-11: API.Tests 19 pass, Middleware.Tests 44 pass, `dotnet build` of the solution 0 errors) `dotnet test` for `LetsTalk.Server.API.Tests` and `LetsTalk.Server.API.Middleware.Tests`; `dotnet build BE/LetsTalk.Server.sln`. **Checkpoint 1.**

## Phase 2 - Clients
- [ ] **T320** Owner regenerates `nswag/angular.nswag` and `nswag/csharp.nswag` (the API must be running to serve the OpenAPI document). **Not** `nextjs.nswag`. (T312; FR-003)
- [ ] **T321** Verify the diffs: in `FE/api-client/api-client.ts` and `BE/API/LetsTalk.Server.API.Client/ApiClient.cs` only the verb of `markAsRead`, `setLinkPreview`, `setImagePreview` changed (`"PUT"` to `"PATCH"` / `HttpMethod("PATCH")`), and in the `.nswag` files only the stored spec snapshot. Confirm no `PUT` remains under `/api/Message`. (D4; SC-001)
- [ ] **T322** `dotnet build BE/LetsTalk.Server.sln` and `dotnet test BE/LetsTalk.Server.sln`; `npm run test:run` in `frontend/`; `npm run lint` in `frontend/` (check that it only touches the generated file or nothing). **Checkpoint 2.**

## Phase 3 - Deployment notes and verification
- [ ] **T330** Write `deployment.md` (release order from the spec's Breaking change section; which Lambdas to redeploy; what a failed preview looks like in the logs: HTTP 405 from `SetLinkPreview`/`SetImagePreview`). (D6)
- [ ] **T331** Write `quickstart.md` with the table below, run it in both modes and record the result in the table. (SC-002)
- [ ] **T332** Add one sentence to the API paragraph of `README.md`/`CLAUDE.md` only if either describes these endpoints as `PUT` (grep first); otherwise skip.
- [ ] **T333** Final `dotnet test BE/LetsTalk.Server.sln`. **Checkpoint 3.** Propose the commit message and file list; wait for approval.

### Quickstart table (for T331)
| # | Steps | Expected | Covers |
|---|---|---|---|
| Q1 | Open `/swagger` | No `PUT` under `/api/Message`; the three operations show `PATCH` | AS1.1, SC-001 |
| Q2 | `curl -X PUT` a signed-in request to `/api/Message/MarkAsRead` | 405 | AS1.2, FR-002 |
| Q3 | Open a chat in one tab and receive a message from another account | Message arrives and is marked as read; the request in DevTools is `PATCH` | AS2.1 |
| Q4 | Local-service mode: send an image and a link | Both previews arrive; no 405 in the services' logs | AS3.1, AS3.2 |
| Q5 | Cloud-function mode (after redeploying both functions): send an image and a link | Both previews arrive; no 405 in CloudWatch | AS3.1, AS3.2 |

## Dependencies
T310 and T311 can run in parallel; T312 needs T310; T320 needs T312 and a running API; Phase 3 needs Phase 2.
