# Rules — ShowRoom Backend (v1)

## QUICK HARD RULES (backend, must always pass)
- Modulith boundaries are mandatory: `Features -> Domain`, `Domain -> Persistence`; all other directions are forbidden.
- Minimal APIs only. MVC Controllers/attributes are forbidden.
- MediatR is forbidden. AutoMapper is forbidden. Use Scrutor for DI.
- Each feature must use VSA + REPR:
  - Request (or Command/Query)
  - Endpoint
  - Handler
  - Response (if needed)
  - Assembler `To()/From()` when mapping is required
- No direct cross-module DB/internal access. Use explicit API/event contracts.
- Domain model must remain persistence-framework agnostic.
- Breaking API/event changes require versioning (`/v2`, new event version), never silent destructive edits.
- Primary identifiers in domain entities and aggregates must always be implemented as StronglyTypedId value types; primitive `Guid`/`string` identifiers are forbidden in the domain model.
- Public-facing URLs and resource identifiers returned by APIs must use `PublicId`; technical IDs stay internal unless a contract explicitly requires them.
- Status-like concepts with low cardinality must never remain primitive strings/ints in the domain model; implement them as a ValueObject or `Ardalis.SmartEnum`, and prefer a ValueObject when only a very small closed set of values exists.
- Tests are mandatory: AAA (`sut` naming), assembler tests, architecture tests green.

---

## 0) Absolute priority (backend)
If rules conflict, apply in this order:
1. Module/layer architecture boundaries
2. VSA + REPR feature structure
3. API/event contract integrity and versioning
4. Test quality and architecture guards
5. Secondary implementation details

---

## 1) Backend architecture (mandatory)

### 1.1 Modulith + Vertical Slice
Each business domain is an isolated module (e.g., Catalog, Ordering, Payments), organized into vertical slices.

### 1.2 Required layers per module
- `*.Persistence.*`
- `*.Domain.*`
- `*.Features.*`

### 1.3 Allowed dependencies
- `Features -> Domain` ✅
- `Domain -> Persistence` ✅
- `Features -> Persistence` ❌
- `Domain -> Features` ❌
- `Persistence -> Domain|Features` ❌

### 1.5 Module namespace conventions
Each module must organize its types according to the following namespace pattern:
- `ShowRoom.Modules.<Module>.Domain.*`
- `ShowRoom.Modules.<Module>.Features.*`
- `ShowRoom.Modules.<Module>.Persistence.*`

All types belonging to a module must reside in the `ShowRoom.Modules.<Module>.*` namespace.  
These conventions are enforced by `ShowRoom.Architecture.Tests` (ArchUnitNET) and must remain green.

### 1.6 MVC exclusion (architectural constraint)
MVC is structurally forbidden in this backend:
- No `[ApiController]`, `[FromBody]`, `[FromRoute]`, `[FromServices]`, `[HttpGet]`, etc. ❌
- No `ControllerBase` or `Controller` subclasses ❌
- No `IActionResult` / `ActionResult<T>` return types ❌

All HTTP surfaces must use **Minimal APIs** (`app.MapGet/MapPost/…`).  
This rule applies to every module, including new ones.

---

### 1.4 Module contracts
`*.Contracts` assemblies:
- must not depend on implementation (`Domain/Features/Persistence`) ❌
- may depend only on:
  - `System.*`
  - `Microsoft.*`
  - `ShowRoom.BuildingBlocks`
  - `ShowRoom.SharedKernel`
- expected namespace: `ShowRoom.Modules.<Module>.Contracts.*`

---

## 2) Feature style (VSA + REPR)

### 2.1 Minimum feature structure
Each backend feature must include:
- `Request` (or `Command/Query`)
- `Endpoint` (Minimal API)
- `Handler`
- `Response` (if needed)
- `Assembler` with `To()` / `From()` when mapping is required

### 2.2 Endpoints
- Always **Minimal APIs**
- Never MVC controllers / attributes:
  - `[ApiController]`, `[FromBody]`, `[FromServices]`, etc. ❌

### 2.3 Forbidden patterns
- MediatR ❌
- AutoMapper ❌
- Manual reflection-based DI registration when avoidable ❌

### 2.4 DI
- Use **Scrutor** for registration of handlers/components.

### 2.5 CommandHandler standard (mandatory)
- Use the Acquisition command handler style as reference for observability and reliability.
- Every command handler must create/use a request identifier (`requestId`) and propagate it in log scope (`BeginModuleScope`).
- Every command handler must emit `SetCommonTags(module, feature, requestId)` and add feature-specific activity tags.
- Use explicit `LogInformation`/`LogWarning` messages for start, validation failures, domain failures, and success.
- For create endpoints returning a new resource identifier, the command handler response must be `Result<string>` containing the created `PublicId`.

---

## 3) API and event contracts

### 3.1 API-first backend definition
Before implementation, define:
- route + HTTP verb
- Request/Response schema
- success and error status codes
- uniform error format

### 3.2 Versioning rules
- Stable endpoint versions (`/v1/...`)
- Breaking API changes => new version (`/v2/...`)
- Prefer additive changes over destructive changes

### 3.3 Event evolution
- Domain Events: internal to module/domain
- Integration Events: external/module-to-module/system contracts
- Breaking event changes => create a new event version + temporary coexistence

---

## 4) Persistence and ACL (backend)

### 4.1 Persistence boundaries
- Domain must not depend on ORM/DB details.
- Persistence mapping must stay inside `Persistence`.
- A module persists only its own data.
- No direct access to other module tables/collections.

### 4.2 ACL placement
For external integrations, place in `Persistence` (or `AntiCorruption/Adapters`):
- external DTOs
- external clients/gateways
- external -> internal mappers
- external error handling

Goal: protect domain language from unstable external contracts.

---

## 5) Backend test requirements (mandatory)

### 5.1 Project structure
- Every module must have its own test project under `tests/ShowRoom.Modules.<Module>.Tests/`.
- The test project should be modeled after the Acquisition module pattern when practical.
- Keep integration and persistence coverage close to the module being tested.
- For modules exposing HTTP endpoints, add a dedicated `<Module>BusinessWebFactory` and `<Module>DatabaseConfiguration` to guarantee full endpoint isolation.

### 5.2 Test style
- AAA mandatory: `Arrange -> Act -> Assert`
- SUT variable must be named `sut`
- Endpoint tests and Assembler tests are the top test priority and must exist for each HTTP feature.

### 5.3 Assembler and domain tests
Location:
`tests/ShowRoom.Modules.<Module>.Tests/Features/<Feature>/Assemblers/`

Requirements:
- Bogus faker per domain entity used when helpful
- Use domain factory methods (e.g., `Lead.Create()`), not direct constructors
- `To()` tests:
  - nominal case
  - edge cases (empty GUID, null, etc.)
- `From()` tests:
  - all properties mapped
  - empty/multiple collections
  - computed properties (displayName, labels, ...)

### 5.4 Architecture tests
- Maintain `ShowRoom.Architecture.Tests` (ArchUnitNET/xUnit v3)
- Any PR violating dependency rules must be blocked.

### 5.5 Targeted test strategy
- TDD priority on:
  - critical business rules
  - REPR contracts/behavior
  - sensitive mappings/conversions
- BDD scenarios recommended for critical API journeys.

---

## 6) Backend observability baseline

### 6.1 Essential signals
At minimum, backend changes should preserve:
- structured logs
- key metrics
- traces for critical flows (when available)

### 6.2 Structured logging
Include, when relevant:
- timestamp
- severity
- module
- feature
- correlation/request identifier
- explicit message

### 6.3 Health checks
- Expose at least a health endpoint (e.g., `/health`)
- Support progressive distinction:
  - **liveness** (process alive)
  - **readiness** (ready to receive traffic/dependencies available)

---

## 7) Backend generation conventions

When generating backend code:
1. Respect module/layer dependencies first.
2. Generate full VSA + REPR feature structure.
3. Use Minimal API endpoint style only.
4. Add assembler for mapping needs.
5. Add unit + assembler tests.
6. Do not introduce forbidden dependencies.
7. Use explicit ubiquitous business language.

---

## 8) Backend PR checklist (must pass)
- [ ] Module/layer architecture respected
- [ ] Dependencies conform to architecture tests
- [ ] Full VSA + REPR feature structure
- [ ] Minimal API endpoint (no MVC)
- [ ] No MediatR / AutoMapper
- [ ] Scrutor-based DI
- [ ] Assembler `To/From` present when needed
- [ ] AAA tests complete
- [ ] Assembler tests complete
- [ ] API/event contracts coherent and versioned when required
- [ ] Observability baseline preserved (logs/metrics/traces where relevant)
- [ ] Health endpoint/liveness-readiness considered