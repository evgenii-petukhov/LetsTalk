---
description: "Use when creating or modifying persistence service-layer unit tests for a database provider. Covers NUnit, Moq, FluentAssertions, provider parity, and test-project conventions."
applyTo: "backend/Persistence/**/LetsTalk.Server.Persistence.*.Services.Tests/**"
---

# Persistence Service Test Guidelines

- Before adding tests, inspect the service implementation and its agnostic service interface. Use `backend/Persistence/EntityFramework/LetsTalk.Server.Persistence.EntityFramework.Services.Tests` and `backend/Persistence/MongoDB/LetsTalk.Server.Persistence.MongoDB.Services.Tests` as reference test projects, choosing the closest match for project, fixture, naming, and package conventions. Verify package versions from the neighboring test project instead of guessing.
- Keep these tests focused on the service layer. Mock repository abstractions and `IMapper`; do not construct database clients, containers, contexts, or connect to a database. Put provider/repository integration tests in a separate test project with explicit infrastructure requirements.
- Use NUnit for test fixtures and cases, Moq for dependency setup and interaction verification, and FluentAssertions for assertions, matching neighboring test style.
- Cover public service operations and overloads. Prioritize meaningful service-owned behavior: branching, orchestration, mapping decisions, ordering/filtering, fallback behavior, and error handling. Give direct pass-through methods a concise delegation/result test rather than repetitive input permutations.
- For each behavior, compare with the corresponding agnostic contract and existing provider implementations/tests. Preserve cross-provider behavior unless a difference is intentional; do not copy another provider's behavior blindly when the implementation or contract differs.
- Verify calls when they are part of the behavior, including cancellation-token forwarding and preventing downstream calls on early-return paths. Avoid asserting incidental call order or internal implementation details that are not observable contract.
- Use deterministic in-memory test data and keep each test focused on one outcome. Prefer descriptive names such as `Method_WhenCondition_ShouldOutcome`.
- Extract recurring test values, such as repeated string and integer literals (e.g., entity IDs, chat IDs, account IDs, page sizes), into class-level `private const` fields instead of repeatedly redefining local constants or variables inside individual test methods.
- Test null, empty, and exception cases when they represent a meaningful contract or branch. Do not encode accidental failures (for example, a null dereference) as expected behavior without confirming that contract; surface questionable behavior for clarification or a separate production fix.
- If tests expose a production defect or require a contract decision, keep that change separate from test-project setup unless the task explicitly includes fixing it.