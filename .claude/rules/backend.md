# Rules — ShowRoom Backend (v1)

## QUICK HARD RULES (backend, must always pass)
- Modulith boundaries are mandatory: `Features -> Domain`, `Features -> Persistence`, `Persistence -> Domain` (Domain is pure); reverse directions are forbidden. No repository pattern — feature handlers use the module DbContext directly (an outbox producer injects `IDbContextOutbox<TContext>` and works on `outbox.DbContext`, still the same module context — see §3.3.2).
- Minimal APIs only. MVC Controllers/attributes are forbidden. Every endpoint must declare `.WithName("<Feature>")` + `.WithSummary("<human-readable sentence>")` (e.g. `.WithSummary("Gets a product by its public id.")`) and its `.Produces<...>`/`.ProducesProblem(...)` response contract — an endpoint without a summary is incomplete (see §2.2).
- MediatR is forbidden. AutoMapper is forbidden. Use Scrutor for DI.
- Machine-to-machine (module-to-module / service-to-service) communication MUST use AMQP messaging (RabbitMQ via **Wolverine**, request/reply `IMessageBus.InvokeAsync`), never HTTP. A module reads another module's data only through that module's `*.Contracts` message contract over the bus — never via HTTP, a shared DB, or internal access. Wolverine is allowed ONLY as the messaging transport; features still use the `IQueryHandler`/`ICommandHandler` + Scrutor convention (Wolverine must NOT be used as an in-process mediator for feature handlers — MediatR-style usage stays forbidden).
- Guaranteed cross-service delivery of an IntegrationEvent uses Wolverine's **transactional outbox** on a **single DbContext** (business schema + a dedicated `wolverine` message-store schema in the SAME database): the handler publishes via `IDbContextOutbox<TContext>` and `SaveChangesAndFlushMessagesAsync`, so the envelope commits atomically with the business change — never a fire-and-forget publish a crash could lose, and never a second DbContext for the store (see §3.3.2).
- Each feature must use VSA + REPR:
  - Request (or Command/Query)
  - Endpoint
  - Handler
  - Response (if needed)
  - Assembler `To()/From()` when mapping is required
- No direct cross-module DB/internal access. Use explicit API/event contracts.
- No connection string or credential in source code. Runtime persistence reads the connection string from `IConfiguration` (`DatabaseModule.AddDatabase` → `GetConnectionString(...)`, fed by Aspire/env); `IDesignTimeDbContextFactory` builds an `IConfiguration` from env vars (+ optional `appsettings*.json`) and any local fallback is password-less. A committed connection string with a password (in `.cs` or a tracked `appsettings*.json`) blocks the change (see global §6).
- Domain model must remain persistence-framework agnostic.
- Breaking API/event changes require versioning (`/v2`, new event version), never silent destructive edits.
- Primary identifiers in domain entities and aggregates must always be implemented as StronglyTypedId value types; primitive `Guid`/`string` identifiers are forbidden in the domain model.
- Public-facing URLs and resource identifiers returned by APIs must use `PublicId`; technical IDs stay internal unless a contract explicitly requires them.
- Response/Summary DTOs and `*.Contracts` messages must not expose persistence/audit technical fields (`IAuditable` `CreatedAt`/`UpdatedAt`, row versions, soft-delete flags); a timestamp is exposed only as an explicit business concept (e.g. `OrderDate`), never the raw audit column (see §1.6.1).
- Paginated list endpoints must return the shared generic `ShowRoom.BuildingBlocks.Application.Pagination.PagedResult<T>` (built via `IQueryable.ToPagedResultAsync(...)`), never a per-feature `Get<Entity>Response` wrapper re-declaring `Items`/`Page`/`PageSize`/`TotalItems`/`TotalPages`. Only the per-item `<Entity>SummaryResponse` is feature-owned (see §1.6.1).
- Status-like concepts with low cardinality must never remain primitive strings/ints in the domain model; implement them as a ValueObject or `Ardalis.SmartEnum`, and prefer a ValueObject when only a very small closed set of values exists.
- Prefer `Result`/`Error` over exceptions for expected, handleable outcomes (validation, not-found, conflict, domain-rule violations). Domain value-object and aggregate factories that validate input must return `Result<T>` and never throw for invalid input. Reserve exceptions for truly exceptional cases (invariant guards, corrupt persisted data, unrecoverable states).
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
- `Features -> Persistence` ✅
- `Persistence -> Domain` ✅
- `Domain -> Persistence` ❌
- `Domain -> Features` ❌
- `Persistence -> Features` ❌

The `Domain` layer is pure: it depends on nothing in `Persistence`/`Features`. `Persistence` references `Domain` to map the aggregate directly via EF Core `IEntityTypeConfiguration<TAggregate>` (no POCO). **There is NO repository abstraction** (no `I<X>Repository`): feature command/query handlers depend on the module `DbContext` directly (vertical slice), and the `DbContext` is the unit of work (`Add`/`AnyAsync`/`SaveChangesAsync`/`FirstOrDefaultAsync`). This is why `Features -> Persistence` is allowed.

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

#### 1.6.1 Dual-ID pattern: StronglyTypedId (technical) + PublicId (public)
**Every aggregate exposed via HTTP API must have both a technical `StronglyTypedId` and a `PublicId`.**

**Technical ID (`StronglyTypedId`):**
- Used internally within the domain and persistence layers only
- Each entity has its own dedicated `StronglyTypedId` (e.g., `InformationRequestId`, `ContentNodeId`)
- Example: `public InformationRequestId Id { get; private set; }`
- Never exposed in HTTP responses or API contracts

**Public ID (`PublicId`):**
- A shared `record` type with semantic prefix + guid structure (e.g., `inf_abc123def456...`)
- Used exclusively in HTTP API responses and external contracts
- Set by the domain when the aggregate is created
- Example: `public PublicId PublicId { get; private set; }`

**Complete example pattern:**
```csharp
public class InformationRequest
{
    public InformationRequestId Id { get; private set; }          // Technical ID (internal only)
    public PublicId PublicId { get; private set; }                // Public ID (exposed in APIs)

    public static InformationRequest Create(/* ... */)
    {
        var id = InformationRequestId.New();
        var publicId = PublicId.Create("inf").Value;  // Generate public ID with prefix
        return new InformationRequest { Id = id, PublicId = publicId };
    }
}
```

**API/DTO rules:**
- Response DTOs must return `PublicId`, never the technical `Id`
- Assemblers map between technical ID (internal) ↔ PublicId (external)
- Clients identify resources exclusively by `PublicId`
- **Response DTOs must not expose persistence/audit technical fields.** The `IAuditable` timestamps (`CreatedAt`/`UpdatedAt`), row versions/concurrency tokens, soft-delete flags, and any other persistence bookkeeping stay internal to the domain/persistence layers and never appear in an HTTP response or a cross-module `*.Contracts` message. Assemblers must not copy them onto a Response/Summary. Expose a date/time in a contract **only** when it is a genuine, explicitly named business concept (e.g. an `OrderDate` the business reasons about) — never the raw audit column, and never named `CreatedAt`/`UpdatedAt`.
- **Paginated lists use the shared generic wrapper, not a per-feature record.** A list feature exposes only its per-item `<Entity>SummaryResponse`; the page envelope is the shared `ShowRoom.BuildingBlocks.Application.Pagination.PagedResult<T>` (`Items`, `Page`, `PageSize`, `TotalItems`, `TotalPages`, `HasPrevious`/`HasNext`). Handlers return `Result<PagedResult<<Entity>SummaryResponse>>` — obtained directly from `IQueryable.ToPagedResultAsync(page, pageSize, <Assembler>.ToSummary, ct)` — and the endpoint declares `.Produces<PagedResult<<Entity>SummaryResponse>>(200)`. Do NOT re-declare a `Get<Entity>Response(IReadOnlyCollection<...> <Entities>, int Page, …)` wrapper; that duplicates the envelope and diverges the JSON shape across modules.

**Rationale:** Decouples internal domain architecture from public API contracts; enables ID rotation/security hardening without breaking client contracts; provides semantic context via prefixes.

Implementation notes for this repo:
- Technical ids use **Meziantou.Framework.StronglyTypedId** and live in the module's `Domain`. Write ONLY the declaration — Meziantou generates the constructor, `Value`, `FromGuid`, `Parse`/`TryParse`, equality, comparison and the System.Text.Json converter. Do not hand-write any members:
  ```csharp
  [StronglyTypedId(typeof(Guid))]
  public partial struct CustomerId : IComparable { }   // ": IComparable" makes it generate IComparable<T> + operators
  ```
  Create a fresh id with the generated factory: `CustomerId.FromGuid(Guid.CreateVersion7())` (the conceptual example above shows `Id.New()`; there is no hand-written `New()` — use `FromGuid`).
- `PublicId`, `PublicIdFactory`, `PublicIdErrors` live in `ShowRoom.BuildingBlocks.Domain.PublicIds`. The domain sets the `PublicId` in the aggregate's factory via `PublicIdFactory.For<Aggregate>()` (or `PublicId.Create("<prefix>")`), taking `.Value` from the returned `Result<PublicId>`.
- Persistence maps the domain aggregate directly (no POCO): EF `IEntityTypeConfiguration<TAggregate>` with value converters for the technical id, `PublicId` (`HasConversion(p => p.Value, s => PublicId.Parse(s))`), and value objects.

---

### 1.7 Module organization (mandatory)
A module is organized by **DDD + VSA** — NO purely technical folders (no `Observability/`, no `Messaging/` bucket), NO scattered composition classes at the module root. The folders are exactly: `Domain/`, `Features/`, `Persistence/`, plus a single composition file.

- **`<Module>Module.cs`** — the ONLY root-level file: the composition root AND the module's identity. It holds:
  - the naming/routing constants (`ModuleName`, `Tag`, `RouteSegment`, `BaseRoute` = `"/" + RouteSegment`, `TelemetrySourceName`) and `BuildApiBasePath(ApiVersion?)`;
  - the module `ActivitySource`: `internal static readonly ActivitySource ActivitySource = new(TelemetrySourceName)`;
  - `Add<Module>Module(IHostApplicationBuilder, string)` → a private `AddInfrastructure()` (DbContext via `Persistence.AddDatabase`, messaging discovery — see §3.4, `TimeProvider`/`IDateTimeProvider`, `AddValidatorsFromAssembly(includeInternalTypes: true)`) + `AddApplicationHandlersFromAssembly(typeof(<Module>Module).Assembly)`. Services are ALWAYS registered; the feature flag gates routing/middleware only.
  - `Register<Module>Module(WebApplication, string)` → gates on `IsModuleEnabled`, then `UseDatabase()` (migration pipeline);
  - `Map<Module>Module(IEndpointRouteBuilder)` → `MapGroup(BaseRoute).WithTags(Tag)` + each feature endpoint's sub-path (e.g. `/{publicId}`).
- **`Domain/`** — DDD core (aggregate, entities, value objects, ids, errors, domain events).
- **`Features/<Slice>/`** — one folder per vertical slice, fully self-contained: Request/Command/Query, Endpoint, Handler, Response, Assembler, Validator, AND the slice's messaging (message handler + its `IWolverineExtension` routing + any anti-corruption port — see §3.4).
- **`Persistence/`** — the module's data mechanics (DbContext, `DatabaseModule` = `AddDatabase`/`UseDatabase`, EF `Configurations/`, `Migrations/`, design-time factory).
- `InternalsVisibleTo` for the module's test assemblies lives in the `.csproj` (`<InternalsVisibleTo Include="..." />`), NOT an `AssemblyInfo.cs`.

Do NOT reintroduce `Observability/`, `Messaging/`, `ApplicationModule`, `InfrastructureModule`, `ValidatorInstaller`, `<Module>Conventions`, or `<Module>Telemetry`: conventions/telemetry/wiring fold into `<Module>Module.cs`; messaging folds into the relevant slice. Feature handlers reference `<Module>Module.ModuleName` and open spans from `<Module>Module.ActivitySource`.

Feature-flag gating: module enablement is read from `FeatureManagement:<ModuleName>` via `IsModuleEnabled` (BuildingBlocks). The host keeps a `ModulesRegistry` (record with `implicit operator string`) and wires `Add<Module>Module` (always — services/routes stay consistent) and `Map<Module>Module` (always). Only `Register<Module>Module` (module middleware) is gated by `IsModuleEnabled`. Do NOT gate `Map` on `builder.Configuration` before `Build()` — under `WebApplicationFactory` the flag is not reliably present at that point, which breaks endpoint tests.

Handlers: implement `ShowRoom.BuildingBlocks.Application.IQueryHandler<TQuery, TResponse>` (`HandleAsync`) or `ICommandHandler<TCommand, TResponse>` (`Handle`), where `TResponse` is the `Result<T>`. Register with `AddApplicationHandlersFromAssembly` (Scrutor, `AsImplementedInterfaces`); endpoints inject the handler INTERFACE, never the concrete type.

Note: modules using Aspire's `AddNpgsqlDbContext` register infrastructure on `IHostApplicationBuilder` (not `IServiceCollection`), which is the intended deviation from a plain `AddDatabase(IServiceCollection, IConfiguration)` registration.

---

### 1.8 Domain primitives (mandatory)
Domain entities/aggregates derive from the shared primitives in `ShowRoom.BuildingBlocks.Domain.Primitives`:
- `Entity<TId>` / `Entity` — identity + `DomainEvents` with `RaiseDomainEvent`/`RemoveDomainEvent`/`ClearDomainEvents` (implements `IHasDomainEvents`).
- `AggregateRoot<TId>` / `AggregateRoot`.
- `AggregateRootWithPublicId<TId>` — adds `PublicId` (`protected set`) + `CreatePublicId(prefix)`; the derived aggregate declares its own strongly-typed `Id` (e.g. `public CustomerId Id { get; private set; }`).
- `EntityWithPublicId`, plus the `DomainEvent` base (`abstract record …(Guid Id)`). Integration events are NOT raised on the aggregate — they are published explicitly by the producing handler through the outbox (§3.3.2); the aggregate carries only domain events.
- Abstractions in `ShowRoom.BuildingBlocks.Domain.Abstractions`: `IAuditable` (marker) and `IStatefulEntity<TStatus> where TStatus : SmartEnum<TStatus>`.

An HTTP-exposed aggregate is `public sealed class <Aggregate> : AggregateRootWithPublicId<<Aggregate>Id>, IAuditable`, with a private id-only constructor (`private <Aggregate>(<Aggregate>Id id) : base() => Id = id;`), a private full constructor chaining `: this(id)`, private setters, and static factories (`Create` sets `PublicId` via `PublicIdFactory.For<Aggregate>().Value`; `Restore` rehydrates). The module `DbContext` must call `modelBuilder.Ignore<DomainEvent>();` so the event collection is never persisted (schema unaffected).

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
- **Every endpoint must declare an OpenAPI summary via `.WithSummary(...)`** — a short, human-readable sentence describing what it does (e.g. `.WithSummary("Gets a product by its public id.")`, `.WithSummary("Creates a new order and returns its public id.")`). It must also declare a stable operation id via `.WithName("<Feature>")`. Both feed the OpenAPI document / Scalar UI, so an endpoint without a `.WithSummary` is incomplete. Keep summaries consistent across modules (`Gets a <entity> by its public id.`, `Lists <entities> (paginated)…`, `Creates a new <entity> and returns its public id.`).
- Declare the response contract with `.Produces<T>(200)` / `.Produces<string>(201)` and the error shapes (`.ProducesProblem(400)`, `.ProducesValidationProblem()`, `.ProducesProblem(404)`…) so the generated OpenAPI is accurate.

### 2.3 Forbidden patterns
- MediatR ❌
- AutoMapper ❌
- Manual reflection-based DI registration when avoidable ❌

### 2.4 DI
- Use **Scrutor** for registration of handlers/components.

### 2.5 Handler observability standard (mandatory)
- Apply a single, consistent handler style for observability and reliability across **every** handler — command handlers, query handlers, AND Wolverine message handlers.
- Every handler must derive a `requestId` from the ambient trace (`Activity.Current?.TraceId.ToString() ?? Guid.NewGuid().ToString("N")`) and propagate it in the log scope (`BeginModuleScope(module, feature, requestId)`).
- Every handler must **open its own span** from the module's `ActivitySource` (`using var activity = <Module>Module.ActivitySource.StartActivity("<module>.<snake_case_feature>");`) and enrich it with `SetCommonTags(module, feature, requestId)` + feature-specific business tags. The module `ActivitySource` name (`<Module>Module.TelemetrySourceName`) must be registered with OpenTelemetry (`AddSource`) in the hosting service.
- Every failure branch must mark the span (`activity?.SetStatus(ActivityStatusCode.Error, "<reason>")`); the success path adds result tags (e.g. the created `public_id`, result counts).
- Use explicit `LogInformation`/`LogWarning` messages for start, validation failures, domain failures, and success.
- For create endpoints returning a new resource identifier, the command handler response must be `Result<PublicId>` containing the created `PublicId` (serialised as its string value over HTTP); never expose the technical id.

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

### 3.3.1 Domain event dispatch (mandatory)
- Aggregates **raise** domain events through the `Entity` base (`RaiseDomainEvent`), from intention-revealing behaviour methods — never from persistence or handlers.
- Domain events are **dispatched by an EF Core `SaveChangesInterceptor`** (`ShowRoom.BuildingBlocks.Persistence.DomainEventDispatchInterceptor`), **never** by the `DbContext` (no `SaveChanges` override, no event logic in the context) and **never** by the command handler. The DbContext stays free of event concerns.
- The interceptor runs in **`SavedChangesAsync` (AFTER commit)**, collects + clears events from the change tracker via `IHasDomainEvents`, and invokes `IDomainEventHandler<T>` resolved from DI. It is **best-effort** (a handler throwing is logged, not rethrown — the transaction is already committed) and **must not write to the DbContext**. Wire it via `services.AddDomainEventDispatch()` + `options.AddInterceptors(sp.GetRequiredService<DomainEventDispatchInterceptor>())` in `DatabaseModule.AddDatabase`; handlers are discovered by `AddApplicationHandlersFromAssembly` (Scrutor scan of `IDomainEventHandler<>`). The interceptor is a **singleton** that dispatches in its own `IServiceScopeFactory` scope (the outbox integration builds `DbContextOptions` from the root provider — see §3.3.2). A factory that repoints a context carrying the interceptor must keep it attached; a module using the outbox instead binds its test DbContext through configuration (no repoint), so the production `AddDatabase` attachment stands unchanged.
- This gives **at-most-once, in-process** semantics — fine for internal reactions. Anything requiring **guaranteed cross-service delivery** is an **Integration Event** published through Wolverine's transactional **outbox** (see §3.3.2, which DOES write to the DB, in the same transaction); the "never write to the DbContext" rule scopes to the domain-event interceptor, not the outbox.

### 3.3.2 Integration event publication — transactional outbox (mandatory for guaranteed delivery)
Cross-service *IntegrationEvents* that require **at-least-once** delivery (never fire-and-forget, never a raw `PublishAsync` that a crash could lose) go through Wolverine's **transactional outbox**. Keep a **single DbContext** — the separation from the business tables is by **schema**, not by a second context:
- **Contract:** the event is a pure record in the producing module's `*.Contracts` assembly (e.g. `CustomerRegisteredIntegrationEvent` in `ShowRoom.Modules.Customer.Contracts`), exposing `PublicId` (never the technical id) and dates only as explicit business concepts. Its durable queue name is a `const` in the module's `*MessagingContract`.
- **Single DbContext + dedicated schema:** the module keeps ONE DbContext (business tables in its own schema); the Wolverine message store lives in a dedicated `wolverine` schema of the SAME database, configured ONCE centrally by `ConfigureShowRoomMessaging` via `PersistMessagesWithPostgresql(connectionString, schema)`, gated by `Messaging:UseTransactionalOutbox`. The envelope tables are auto-provisioned and are **NOT** part of the module's EF model/migrations.
- **Same database, mandatory:** the outbox writes envelopes through the module DbContext's own connection (that is what makes the write atomic), so the message store and the module MUST resolve the SAME connection string (`Messaging:MessageStoreConnectionName`, default `showroom`). The DbContext is registered with `services.AddDbContextWithOptionalOutbox<TContext>(configuration, configureOptions)` (BuildingBlocks) which switches to `AddDbContextWithWolverineIntegration` when the outbox is on (exposing `IDbContextOutbox<TContext>`) and plain `AddDbContext` otherwise. The `DomainEventDispatchInterceptor` is a **singleton** (the outbox integration builds `DbContextOptions` from the root provider) that dispatches in its own scope.
- **Atomic publish in the handler:** the command handler injects `IDbContextOutbox<TContext>`, does its data work on `outbox.DbContext`, calls `outbox.PublishAsync(@event, deliveryOptions)`, then `outbox.SaveChangesAndFlushMessagesAsync(ct)` — the business row and the outgoing envelope commit in ONE transaction (all-or-nothing). This is the sanctioned DbContext write for messaging. Headers are still mandatory (§3.4) via `DeliveryOptions`.
- **Durable delivery routing:** the slice's `IWolverineExtension` declares `PublishMessage<TEvent>().ToRabbitQueue(<queue>).UseDurableOutbox()` so the durable sending agent retries the stored envelope until acknowledged (surviving process/broker outages).
- **Proof (broker-free):** the module's integration tests run with external transports stubbed (`DisableAllExternalWolverineTransports`) and a dedicated test asserts the event is published via Wolverine message tracking (`services.TrackActivity().ExecuteAndWaitAsync(...)` → `tracked.Sent.SingleMessage<TEvent>()`), so no real broker is needed (see §5).

### 3.3.3 Integration event consumption — durable inbox + retry/dead-letter (mandatory)
A module consuming an IntegrationEvent published by another service (the flip side of §3.3.2):
- **Contract reference:** the consuming module references ONLY the producer's `*.Contracts` assembly (never its implementation) and implements a Wolverine message handler (`public [Task] Handle(TEvent, Envelope, <deps>, …)`) in a `Features/On<Event>/` slice, with the §2.5 handler observability standard (correlation id from the `Envelope` headers, span from the module `ActivitySource`, `MessagingMetrics.RecordHandler`).
- **Durable inbox + per-service store:** the slice's `IWolverineExtension` declares `ListenToRabbitQueue(<queue>).UseDurableInbox()`; the consuming SERVICE provisions its OWN message store — `Messaging:UsePersistentMessageStore` with a **service-specific** `MessageStoreSchema` (e.g. `wolverine_business`). Two Wolverine runtimes MUST NOT share one message store schema.
- **Retry/dead-letter policy, scoped:** define a dedicated exception for an unprocessable ("poison") message; configure `options.OnException<TPoison>().RetryWithCooldown(…).Then.MoveToErrorQueue()` in the slice's extension. Scope it by exception type so request/reply handlers (e.g. `GetOrdersForCustomer`) are unaffected — a global catch-all would wrongly dead-letter replies. Dead-lettered messages land durably in `<schema>.wolverine_dead_letters` (replayable via `IMessageStore.Admin`), never retried forever.
- **Proof:** a dedicated end-to-end test backed by PostgreSQL + RabbitMQ Testcontainers (`StubExternalTransports => false`) asserts a well-formed message is handled (`tracked.MessageSucceeded`) and a poison message is dead-lettered (`tracked.MovedToErrorQueue`), using `TrackActivity().WaitForMessageToBeReceivedAt<TEvent>(host).ExecuteAndWaitAsync(...)`.

### 3.4 Machine-to-machine messaging (AMQP / Wolverine) — mandatory
Cross-module (and future cross-service) data exchange goes over **RabbitMQ via Wolverine**, never HTTP.

- **Contract placement:** request/reply message types live in the owning module's `*.Contracts` assembly (pure records, no `Domain/Features/Persistence` or messaging-framework dependency). Consumers reference only the `*.Contracts` assembly of the module they call.
- **Consumer side (data owner):** the owning module implements a Wolverine message handler (`public Task<TResponse> Handle(TRequest, <deps>, CancellationToken)`) in the matching `Features/<Slice>/` folder, answering from its own `DbContext`. That same slice declares its routing as an `IWolverineExtension` co-located with the handler (`opts.ListenToRabbitQueue(...)` + `opts.Discovery.IncludeType<THandler>()`); the core `ConfigureShowRoomMessaging` calls `DisableConventionalDiscovery()` so the modules' REPR `IQueryHandler`/`ICommandHandler` types are never scanned as message handlers.
- **Caller side:** wrap `IMessageBus.InvokeAsync<TResponse>(request)` behind a small anti-corruption port (an interface named for the business capability it exposes, e.g. `IOrderHistory` — NOT a `*Gateway`/`*Client` technical suffix) in the caller module; do not inject `IMessageBus` directly into a feature handler — keeps the handler unit-testable. The feature handler orchestrates the port.
- **Standard message headers (MANDATORY):** every message sent on the bus MUST carry the standard headers defined in `ShowRoom.BuildingBlocks.Messaging.MessageHeaders` — at minimum `ModuleName`, `FeatureName`, `MessageType`, `MessageId`, `CorrelationId`, `TraceId` (add `CausationId`, `EventType`, `TenantId`, `UserId` when the context provides them). **Producers** set them via `new DeliveryOptions().WithHeader(MessageHeaders.<Key>, value)` passed to `InvokeAsync`/`PublishAsync` (correlation/trace id derived from `Activity.Current?.TraceId`). **Consumers** inject the Wolverine `Envelope` and read `envelope.Headers[MessageHeaders.<Key>]` to derive the correlation id (used for `BeginModuleScope`) and to enrich the span with caller module/feature. **Never** hardcode header key strings — always reference the `MessageHeaders` constants.
- **Configuration is config-driven and per-module (like persistence) — MANDATORY:** the transport is configured ONCE centrally via `opts.ConfigureShowRoomMessaging(configuration)` (`ShowRoom.BuildingBlocks.Messaging`) inside the host's `UseWolverine`, reading `MessagingOptions` from the `"Messaging"` section (`Enabled`, `Transport` = `InMemory`/`RabbitMq`, `RabbitMqConnectionName`, `UseDurableLocalQueues`, `EnableRemoteInvocation`, plus the message-store fields `UseTransactionalOutbox` (producer, §3.3.2), `UsePersistentMessageStore` (consumer durable inbox + dead-letter, §3.3.3), `MessageStoreConnectionName`, and the per-service `MessageStoreSchema`). Each feature **slice** owns its routing as an `IWolverineExtension` co-located in `Features/<Slice>/`; the module composition discovers them with a Scrutor scan in `<Module>Module.AddInfrastructure` (`services.Scan(... AssignableTo<IWolverineExtension>() ... AsSingleton)`), gated by `Messaging:Enabled`. Wolverine applies those extensions automatically at bootstrap, so the host stays agnostic of module specifics. Producers declare `PublishMessage<T>().ToRabbitQueue(...)`; consumers `ListenToRabbitQueue(...)` — in the slice's `IWolverineExtension`. Extracting a module to its own service is then a deployment change, not a contract change.
- **Reactive degradation:** read/query paths must degrade gracefully when the broker/remote module is unavailable or times out (Wolverine's default 5s remote-invocation timeout) — return a partial result with an availability flag rather than failing the whole request. Write paths choose an explicit failure or outbox strategy.
- **Connection:** RabbitMQ is an Aspire resource (`AddRabbitMQ("messaging")`); `ConfigureShowRoomMessaging` connects via `UseRabbitMqUsingNamedConnection(MessagingOptions.RabbitMqConnectionName).AutoProvision()` when `Transport == RabbitMq`.
- **Tests:** stub external transports by default in the shared test harness (`services.DisableAllExternalWolverineTransports()`); prove behaviour with unit tests over the port abstraction (happy + degraded paths), and prove the request/reply round-trip with one dedicated end-to-end test backed by a RabbitMQ Testcontainer. (The transactional outbox is proven **broker-free** with Wolverine message tracking — see §3.3.2 — not a RabbitMQ Testcontainer.)

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
- The test project should follow the established module test pattern when practical.
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

### 6.4 Messaging observability (Wolverine / AMQP) — mandatory
Message-based machine-to-machine flows (see §3.4) must be observable end-to-end:
- Register Wolverine's OpenTelemetry **ActivitySource `"Wolverine"`** (`AddSource`) and **meter `"Wolverine*"`** (`AddMeter`, wildcard — the meter is `Wolverine:{ApplicationName}`) with the OTel tracing/metrics pipelines so send/handle spans and messaging metrics are exported. Centralise these names (e.g. `WolverineObservability`).
- Rely on Wolverine's automatic W3C trace-context propagation across RabbitMQ: the consumer's handling span is a child of the producer's send span — a single distributed trace across the async boundary. Do not invent a parallel correlation scheme.
- In the message handler AND the caller-side handler/port: derive the correlation id from `Activity.Current?.TraceId`, open `BeginModuleScope(module, feature, requestId)` with it, and enrich `Activity.Current` via `SetCommonTags(module, feature, requestId)` + business tags (result counts, availability, key public ids). Record failures on the span (`AddException`) on the degraded path, and keep structured start/success/degraded log messages.
- Emit aggregatable **duration metrics** for the cross-service hop via the shared `MessagingMetrics` meter (`ShowRoom.Messaging`, registered with `AddMeter`): the caller-side anti-corruption port records the request/reply **round-trip** (`showroom.messaging.roundtrip.duration`, tagged module/feature/message/outcome) and the consumer message handler records its **handling time** (`showroom.messaging.handler.duration`) plus a `messaging.handler.duration_ms` span tag. These complement — never replace — the per-element span durations already in the distributed trace and Wolverine's own metrics.

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