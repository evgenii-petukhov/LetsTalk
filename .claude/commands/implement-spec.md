---
description: Implement a feature spec from specs/ task by task (Spec-Driven Development)
argument-hint: <spec number or folder, e.g. 001> [phase or task range, e.g. "Phase 0-1" or "T020-T029"]
---

Implement the spec identified by `$ARGUMENTS` (the first word is the spec number or folder name under `specs/`; an optional rest says which phase or tasks to do; if there is no rest, start at the first unticked task).

1. Read, in this order: `.specify/memory/constitution.md`, then in the spec folder `spec.md`, `plan.md`, `research.md`, every file in `contracts/`, `tasks.md`, `quickstart.md`. If the spec folder is missing or `spec.md` still contains `[NEEDS CLARIFICATION]`, stop and tell me.
2. Check `git branch --show-current`. It should be the spec's branch (same name as the folder). If not, tell me and wait.
3. Work through `tasks.md` in order, phase by phase. For each group, write the tests before the implementation. Tick the checkbox in `tasks.md` as soon as a task is done and verified. Run the tests that exist for the area you changed.
4. Stop and report at every "Checkpoint" line (after running the tests it names), at the end of every phase (also phases without a checkpoint), and at the end of the requested range. Report which tasks are done, which tests ran and their results, and anything that deviates from the spec. Do not continue past a stop without my reply.
5. Each phase is one commit. At every phase stop, propose the commit: the one-line message (with the prefix below) and the explicit list of files to stage, including the ticked `tasks.md`. Do not run `git add` or `git commit` until I say so. After I approve, stage exactly that list by path, commit, and only then start the next phase.
6. If the code and the spec disagree, fix the spec (and plan/tasks) first and tell me what changed and why.

Guardrails for this repository:
- My uncommitted changes in `backend/Applications/LetsTalk.Server.API/Program.cs` and `appsettings.json` contain secrets. Never stage, modify, or reformat them. Stage files by explicit path only; never `git add -A` or `git add .`. If a task needs a change in those files, make the change only in other files or tell me what to add locally.
- Commit only when I ask. Commit messages are one line and start with the prefix given under "Conventions" in the spec's `tasks.md` (spec 001: `Http-only cookie: `, spec 002: `Token refresh: `).
- Do not touch `frontend-next/`; never run `nswag/nextjs.nswag`.
- Do not hand-edit generated files (`api-client.ts`, `ApiClient.cs`, `*_pb.js`, `.proto` outputs). Regenerate NSwag clients with the matching `nswag/*.nswag`. If `protoc` is needed for the grpc-web client, tell me exactly what to run; I run it in WSL.
- Do not implement any other spec than the one requested.
