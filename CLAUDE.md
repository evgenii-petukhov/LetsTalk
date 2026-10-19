# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Overview

LetsTalk is an instant messenger (text, images, video calls). The repo holds a .NET 10 microservice back end and two front ends:

- `backend/` — .NET solution `LetsTalk.Server.sln`
- `frontend/` — Angular 21 + NgRx SPA (the original, production front end)
- `frontend-next/` — new Next.js (React 19, TanStack Query, Zustand) front end, work in progress (chat list, chat selection, messages with upward infinite scroll)
- `nswag/` — NSwag configs (`angular.nswag`, `nextjs.nswag`, `csharp.nswag`) that generate API clients from the API's OpenAPI spec (e.g. `nextjs.nswag` writes `frontend-next/src/lib/api/api-client.ts`). Regenerate clients rather than hand-editing generated files.

## Commands

### Back end (run from `backend/`)
```
dotnet build LetsTalk.Server.sln
dotnet test LetsTalk.Server.sln
dotnet test <path-to-Tests.csproj> --filter "FullyQualifiedName~SomeTestName"   # single test / class
```
`RunAll.ps1` is a Windows dev launcher: starts Docker Desktop, the `redis` and `kafka` containers, cleans bin/obj, runs `dotnet publish -r win-x64 --self-contained false -c Debug`, and launches every service exe from its `publish` folder. Tests use NUnit + Moq + FluentAssertions.

### Angular (`frontend/`)
```
npm start                 # ng serve
npm run build             # production build
npm run build:dev         # dev build, watch mode
npm run test:run          # vitest, single run (npm test = watch)
npx vitest --run path/to/file.spec.ts   # single spec
npm run lint              # ng lint --fix + stylelint --fix (modifies files)
```

### Next.js (`frontend-next/`)
```
npm run dev | build | start | lint
```
`frontend-next/AGENTS.md` warns this is a Next.js version with breaking changes (config uses `cacheComponents` and `partialPrefetching`): read the relevant guide in `frontend-next/node_modules/next/dist/docs/` before writing Next.js code. There is no test script. The browser talks to the API through a proxy route at `src/app/api/proxy/[...path]/route.ts`.

## Back end architecture

Event-driven microservices; the event broker is switchable between Apache Kafka and AWS SNS/SQS (via MassTransit), selected by `Features:EventBrokerMode` in `appsettings.json`. Runnable services live in `backend/Applications/`:

- `LetsTalk.Server.API` — REST entry point (messages, accounts), SignalR host; Swagger is exposed
- `Authentication` — gRPC; issues/validates JWTs
- `FileStorage.Service` — gRPC; saves/serves avatars and images (AWS S3)
- `ImageProcessing.Service`, `LinkPreview` — broker consumers (call AWS Lambda; SkiaSharp for images)
- `Notifications` — broker consumer that pushes new-message events to clients via SignalR
- `EmailService`

Layout of the rest of `backend/`:
- `API/` — layered API pieces: Core (MediatR CQRS handlers), Core.Commands, Middleware, Models, Validation, Client (gRPC/HTTP clients), each with sibling `*.Tests` projects.
- `Persistence/` — pluggable database layer. `Features:DatabaseMode` (e.g. `MySql`) picks the provider: EntityFramework (MySQL, with migrations and `MigrationTool`), MongoDB, CosmosDB. Each provider has `Models`, `Repository(.Abstractions)`, and `Services(.Tests)`. Provider-neutral contracts are `Persistence.AgnosticServices.Abstractions` (`I*AgnosticService`) with models in `AgnosticServices.Models`; application code depends only on those. `LetsTalk.Server.Domain` holds entities; `Persistence.Redis` handles Redis.
- `Infrastructure/` — cross-cutting: Configuration, Authentication, FileStorage, ImageProcessing, LinkPreview, Logging, SignPackage, Telemetry, and `DependencyInjection` (shared service registration).
- `Models/` — DTOs, Kafka/broker message models, exceptions. `Shared/` — helpers.

When adding a persistence feature, implement it across all providers and keep behavior consistent with the agnostic contract.

### Persistence service tests
Follow `.github/instructions/persistence-service-tests.instructions.md` (referenced from `backend/AGENTS.md`) when touching any `Persistence/**/*.Services.Tests`: mock repositories and `IMapper` (no real DB), use NUnit/Moq/FluentAssertions, name tests `Method_WhenCondition_ShouldOutcome`, hoist repeated literals to class-level `private const`, and use the EntityFramework and MongoDB test projects as references.

## Frontend notes

The Angular app is organized by `components/`, `state/` (NgRx), `services/`, `api-client/`, `protos/` (grpc-web), with SignalR for realtime. Components use `ChangeDetectionStrategy.OnPush`.
