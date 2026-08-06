# Rules — ShowRoom (Global Entry Point)

## Purpose
This file is the global rules entry point for ShowRoom.

It defines **cross-cutting rules** that apply to all contributions (backend, frontend, cloud), and references specialized instruction files:
- `.claude/rules/backend.md`
- `.claude/rules/frontend.md`
- `.claude/rules/cloud.md`

When rules overlap, apply precedence from this file first, then specialized files.

---

## 0) Absolute priority (global)
If multiple rules conflict, apply in this order:
1. Architecture and module boundaries
2. API/event contract integrity and versioning
3. Security and operational reliability
4. Testability and quality gates
5. Code style/conventions

---

## 1) Architecture and boundaries (cross-cutting)
- Respect strict module boundaries and explicit contracts.
- No hidden coupling between modules (code, data, or runtime dependencies).
- Prefer explicit interfaces/contracts over implicit internal access.
- Breaking changes must be versioned (API and events), never silent destructive changes.
- **Machine-to-machine communication (module-to-module and service-to-service) must use asynchronous AMQP messaging (RabbitMQ via Wolverine), never HTTP.** A module obtains another module's data only through that module's explicit message contract over the bus (request/reply via `IMessageBus.InvokeAsync`), never via HTTP calls, a shared database, or internal type access. HTTP is reserved for the outside world calling into the system.
- The primary identifier must always be a StronglyTypedId.
- Only PublicId should be exposed over HTTP.
- Binary or low cardinality statuses should be implemented as ValueObject or SmartEnum, with a preference for ValueObject when there are only two values.
- Do not use MVC decorations (e.g., [FromServices]) in handlers/endpoints; prefer patterns compatible with Blazor/.NET without MVC dependencies.

---

## 2) API-first and contract discipline
- Define and validate contracts before implementation when possible.
- Keep endpoints/events stable and readable.
- Prefer additive evolution over breaking mutation.
- Public-facing URLs and resource identifiers returned by APIs should use `PublicId` whenever the entity exposes one; technical IDs stay internal unless a contract explicitly requires them.
- Use uniform error structures and explicit status handling.

---

## 3) Documentation rules
- Keep **one** `README.md` at repository root.
- Do not create README files in subfolders unless explicitly requested.
- Update existing root documentation instead of duplicating.
- Keep API documentation/OpenAPI aligned with implemented behavior.

---

## 4) Testability and quality baseline
- Tests are mandatory on critical behavior and architecture constraints.
- Every module must have its own dedicated test project.
- Use clear Arrange/Act/Assert structure for automated tests.
- Prioritize testing where business/operational risk is highest.
- Keep architecture tests as enforcement guardrails when present.
- For modules exposing HTTP endpoints, include a complete isolation structure: a dedicated `<Module>BusinessWebFactory` and `<Module>DatabaseConfiguration`.

---

## 5) Observability and operability baseline
- Changes should remain diagnosable in production-like environments.
- Prefer structured logs and explicit failure signals.
- Preserve/extend health and readiness semantics when applicable.
- Avoid introducing opaque behavior that reduces diagnosability.
- Use explicit observability extensions (e.g., `BeginModuleScope`) in relevant handlers.
- Message-based (AMQP/Wolverine) flows must be observable end-to-end: register the Wolverine OpenTelemetry ActivitySource and meter, rely on trace-context propagation across the broker so producer and consumer spans belong to a single distributed trace, and enrich handler spans/logs with module/feature/correlation tags.
- Keep observability nomenclature (routes, logs, tags, business names) consistent across modules to ensure uniformity.

---

## 6) Security and robustness baseline
- Never hardcode secrets or sensitive environment values.
- Validate untrusted inputs at system boundaries.
- Keep failure handling explicit (timeouts/retries/fallbacks where relevant).
- Favor short critical transactions and decoupled side effects when appropriate.
- Prefer `Result`/`Error` over exceptions for expected, handleable outcomes; reserve exceptions for truly exceptional or unrecoverable cases.

---

## 7) Delivery and change safety
- Keep changes traceable and reversible.
- Avoid unnecessary complexity in Foundation phase.
- Prefer simple, reliable defaults over premature optimization.
- Ensure compatibility with CI/CD and automated validation paths.

---

## 8) Database Migration Guidelines
- Never use direct SQL scripts for database migrations. Always use EF Core and its migrations (`dotnet ef migrations add`).

---

## 9) Specialized instruction files (mandatory scope routing)

### Backend scope
For backend/domain/API/persistence work, apply:
- `.claude/rules/backend.md`
- `.claude/rules/tests.md`

### Frontend scope
For frontend/UI/client orchestration work, apply:
- `.claude/rules/frontend.md`
- Centralize frontend-specific content in `.claude/rules/frontend.md`, not in `src/frontends/ShowRoom.Web/`.
- Implement a step-by-step guided setup for frontend development. At each validated step, add a design rule to `.claude/rules/frontend.md` to ensure clarity and adherence to best practices.

### Cloud/DevOps scope
For cloud/runtime/deployment/observability platform work, apply:
- `.claude/rules/cloud.md`

If a task spans multiple scopes, combine relevant files while preserving the global priority order.

---

## 10) Pull Request checklist (global)
- [ ] Architecture/module boundaries respected
- [ ] API/event contract integrity preserved (or properly versioned)
- [ ] Security and configuration hygiene preserved
- [ ] Tests updated for critical behavior
- [ ] Observability/operability impact considered
- [ ] Documentation updated when behavior/contracts changed