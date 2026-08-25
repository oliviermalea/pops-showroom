# ShowRoom

> .NET 10 proof of concept demonstrating a **modulith → distributed** architecture: isolated bounded
> contexts, **machine-to-machine communication over AMQP messaging** (never HTTP), **.NET Aspire**
> orchestration and end-to-end **OpenTelemetry** observability.

The guiding business scenario: **a customer consults their profile, their order history and a product's
details** — each capability living in its own bounded context.

> ℹ️ **Maintenance** — this README is the single root documentation of the repository. It must be **kept
> up to date continuously**: any change to the topology, a service or an API contract has to be
> reflected here (see `.claude/rules/global.md` §3).

---

## Contents

- [Minimum Viable Architecture](#minimum-viable-architecture)
- [Topology](#topology)
- [Technical stack](#technical-stack)
- [Solution layout](#solution-layout)
- [Getting started](#getting-started)
- [Available APIs](#available-apis)
- [Blazor front end (ShowRoom.Web)](#blazor-front-end-showroomweb)
- [Messaging & observability](#messaging--observability)
- [Tests](#tests)

---

## Minimum Viable Architecture

The smallest set of decisions that keeps this system **evolvable**: enough structure to extract a module
into its own service later, not so much ceremony that delivery slows down today. Two properties make it
"viable" rather than aspirational:

- every rule below is **enforced somewhere** — an architecture test, a guardrail test, or a measurement.
  A principle nobody enforces is a wish;
- every rule is **reversible in one place**. Nothing here requires rewriting the system to change course.

The authoritative rules live in [`.claude/rules/`](.claude/rules); what follows is the architecture they
encode.

### 1. One module per bounded context, one schema per module

A business domain is an isolated module (`Customer`, `Order`, `Product`). Modules share a single
PostgreSQL database but never a schema: `customers`, `orders`, `products`. The separation is **logical,
not physical** — which is exactly what makes extraction cheap: `Customer` already moved into its own
service without a data migration.

No module reads another module's tables. Ever.

### 2. Vertical Slice + REPR inside a module

A module is organised by **feature**, not by technical layer. Each slice holds everything the feature
needs, side by side:

```text
Features/CreateCustomer/
  CreateCustomerCommand.cs         # Request
  CreateCustomerEndpoint.cs        # Endpoint (Minimal API)
  CreateCustomerCommandHandler.cs  # Handler
  CreateCustomerAssembler.cs       # Response mapping
  CreateCustomerValidator.cs
  CreateCustomerMessaging.cs       # this slice's Wolverine routing
```

The three layers a module does keep are dependency-ordered and enforced by architecture tests:
`Features → Domain`, `Features → Persistence`, `Persistence → Domain`. **`Domain` depends on nothing.**

### 3. Minimal APIs only

No MVC controllers, no `[ApiController]`, no `IActionResult`. Every endpoint declares its operation id, a
human-readable summary and its response contract, so the generated OpenAPI document is accurate rather
than decorative.

### 4. No repository pattern

Feature handlers use the module `DbContext` directly — it *is* the unit of work. A repository layered
over an ORM that already is one adds indirection without adding a boundary. The boundary that matters is
the module, and it is enforced elsewhere.

### 5. A rich domain, not an anemic one

- Behaviour lives on aggregates, through intention-revealing methods that raise domain events.
- Low-cardinality states are **value objects or SmartEnums**, never bare `string`/`int` —
  `CustomerStatus` is a value object, `Currency` is an ISO 4217 SmartEnum.
- Factories that validate input return `Result<T>`; they never throw for invalid input.
- Aggregates derive from shared primitives (`AggregateRootWithPublicId<TId>`), so identity, public id and
  domain events are not re-implemented per module.

### 6. Dual identity: technical id inside, public id outside

Every HTTP-exposed aggregate carries both a `StronglyTypedId` (internal, never serialised) and a
`PublicId` (`cus_`, `ord_`, `prd_` + 32 hex). Clients only ever see the public one. This decouples the
public contract from the storage strategy: identifiers can be rotated or hardened without breaking a
single consumer.

### 7. Manual mapping through assemblers — no AutoMapper

Mapping is explicit, co-located with its slice, and **tested**. Convention-based mappers move breakage
from compile time to runtime and hide contract drift; an assembler makes both visible. The same rule
applies on the front end (`CustomerDetailMapper`, `CreateCustomerMapper`).

Contracts must not leak persistence bookkeeping: no `CreatedAt`/`UpdatedAt` audit columns, no row
versions, no soft-delete flags. A timestamp appears only when it is a genuine business concept
(`OrderDate`).

### 8. Reasonable messaging

"Reasonable" means: asynchronous where it buys decoupling, explicit where it costs latency.

| Need | Mechanism | Guarantee |
|---|---|---|
| Read another module's data | Request/reply over AMQP (`IMessageBus.InvokeAsync`) behind an anti-corruption port | Best effort, **graceful degradation** |
| Publish a business fact | Transactional **outbox** (envelope committed with the business change) | At least once |
| Consume a business fact | Durable **inbox** + scoped retry/dead-letter policy | At least once, poison messages quarantined |
| React inside the module | Domain events dispatched by an EF `SaveChangesInterceptor` | At most once, in-process |

Three rules keep this from turning into distributed spaghetti:

- **M2M is AMQP, never HTTP.** A module reads another module's data only through that module's
  `*.Contracts` message contract. HTTP is reserved for the outside world calling into the system.
- **A read path serving an HTTP request has an explicit time budget**, named and unit-tested — never the
  implicit product of a default timeout by a retry count (see `OrderHistoryRetryPolicy`: worst case
  6.2 s, down from ~16 s).
- **Degradation is a designed outcome, not a failure.** When the Order service is unreachable, the
  customer is still returned with `ordersAvailable = false`, and the UI says exactly that.

### 9. Front end and back end are decoupled

- The front end is **the outside world**: it calls the system over HTTP, like any other client. The
  "M2M = AMQP" rule does not apply to it.
- It is a **BFF**: the Refit client and the facade run server-side, so the browser never learns an API
  URL. Moving to WebAssembly would mean exposing the APIs to the browser (CORS, auth) — an architecture
  decision, not a setting.
- The UI holds **no business rules**. It orchestrates a facade per module and formats view models;
  decisions stay in the domain.
- The **render mode is chosen per screen** (static SSR for read screens, interactive only where an
  interaction requires it), and that choice is justified by measurement.

### 10. Contracts first, evolved additively

Routes are versioned (`/api/v{version}`), evolution is additive, and a breaking change means a new
version — never a silent mutation. Paginated endpoints return the shared `PagedResult<T>` envelope so the
JSON shape never diverges between modules. Cross-module message contracts live in `*.Contracts`
assemblies that depend on nothing but the framework and the shared kernel.

### 11. Expected outcomes are results, not exceptions

Validation failures, not-found, conflicts and domain-rule violations flow as `Result`/`Error` and are
translated once into uniform RFC 7807 ProblemDetails. Exceptions are kept for the genuinely exceptional.
The front end mirrors this: a `404`/`409` is **business data** turned into an explicit outcome, not a
thrown exception — and the ProblemDetails body is **read**, so a server-side rejection lands on the form
field it names and the `traceId` reaches the screen (see
[Blazor front end](#blazor-front-end-showroomweb)). The error **code** is the contract; the message is
the server's own wording.

### 12. Observability ships with the feature

Structured logs scoped by module/feature/correlation id, spans opened from the module's own
`ActivitySource`, business metrics on the module's meter, trace context propagated across the broker so
producer and consumer belong to one distributed trace, and health/readiness endpoints. A feature that
cannot be diagnosed in production is not finished.

### 13. Configuration and secret hygiene

No connection string or credential in source code — not in runtime code, not in design-time factories.
Everything resolves from configuration, fed by Aspire, environment variables or a secret store. Local
fallbacks are password-less. Modules are gated by feature flags (`FeatureManagement:<Module>`), so a
context can be switched off without a rebuild.

### 14. Schema changes go through EF migrations

Never hand-written SQL. And any column serving a paginated `ORDER BY` carries an index, declared in the
entity configuration and shipped by a migration — the difference measured here at 200,000 rows was 41 ms
per page (sort spilling to disk) against 0.12 ms.

### 15. Guardrails, not discipline

- **Architecture tests** (ArchUnitNET) enforce module and layer boundaries at build time.
- **Model tests** pin what behaviour tests cannot see — an index is invisible until it is missing.
- The **test pyramid** is layered by cost, and each level has an admission criterion (see [Tests](#tests)).

### 16. Measure before optimising — and after

Several decisions in this repository were **reversed by measurement**: an output cache that never served
a hit, a 350 ms skeleton floor that cost 345 ms for nothing, a client timeout shorter than the backend
degradation path it was meant to observe. The numbers are kept next to the decisions they justify, so a
future change can re-run them rather than re-argue them.

---

## Topology

The system is **distributed**: the *Customer* bounded context is extracted into its own service, which
talks to the *Business* service (Order + Product) **only through RabbitMQ** (Wolverine request/reply).
The services share **one PostgreSQL database** (`showroom`); isolation between modules comes from **a
dedicated schema per module** (`customers`, `orders`, `products`) — logical separation, not physical. An
extra `wolverine` schema hosts the transactional outbox **message store** (tables managed by Wolverine,
next to the business schema — not a second DbContext).

Two messaging styles coexist:
- **synchronous request/reply** (`IMessageBus.InvokeAsync`) to read another module's data
  (`GetOrdersForCustomer`) — no delivery guarantee, with graceful degradation;
- **guaranteed publish/subscribe** through the **transactional outbox** for *IntegrationEvents*
  (`CustomerRegisteredIntegrationEvent`) — at-least-once delivery, the envelope being persisted in the
  **same transaction** as the business change (same DbContext, same connection) then delivered to
  RabbitMQ with retries.

```mermaid
flowchart LR
    client([HTTP client])
    web["ShowRoom.Web<br/>(Blazor front end)"]

    subgraph customer[ShowRoom.Customer.Api]
        cust[Customer module]
    end
    subgraph business[ShowRoom.Business.Api]
        ord[Order module]
        prod[Product module]
    end

    rabbit[[RabbitMQ<br/>messaging]]
    db[("showroom<br/>(schemas: customers / orders / products / wolverine / wolverine_business)")]

    client -->|HTTP /customers| cust
    client -->|HTTP /orders, /products| ord

    web -->|HTTP /customers/{publicId}| cust
    web -.->|HTTP (planned)| ord

    cust -->|"InvokeAsync(GetOrdersForCustomer)"| rabbit
    rabbit -->|GetOrdersForCustomer| ord
    ord -.->|OrdersForCustomerResponse| rabbit
    rabbit -.->|reply| cust

    cust -->|"outbox: CustomerRegistered (guaranteed delivery)"| rabbit
    rabbit -->|CustomerRegistered| ord

    cust ---|schemas customers + wolverine| db
    ord ---|schemas orders + wolverine_business| db
    prod ---|schema products| db
```

- **`ShowRoom.Customer.Api`** — hosts the Customer module. **Producer**: `GET
  /customers/{id}/with-orders` sends `GetOrdersForCustomer` on the bus and awaits the reply; `POST
  /customers` publishes `CustomerRegisteredIntegrationEvent` through the **transactional outbox**
  (guaranteed delivery).
- **`ShowRoom.Business.Api`** — hosts Order + Product. **Consumer**: answers `GetOrdersForCustomer` from
  its own database (never exposing an internal HTTP call) and **consumes**
  `CustomerRegisteredIntegrationEvent` (durable inbox + retry/dead-letter policy, see below).
- Because the Order handler lives in **another process**, the request genuinely crosses the broker →
  a complete, trustworthy distributed trace, with no HTTP coupling between services.
- Graceful degradation: if the Order service or the broker is unavailable, the customer is still returned
  with `ordersAvailable = false` and an empty order list.
- **`ShowRoom.Web`** — Blazor front end orchestrated by Aspire. It consumes the Customer service over
  **HTTP** (typed Refit client): the front end is the outside world calling into the system, so HTTP is
  legitimate there — the "M2M = AMQP" rule only governs service-to-service exchanges. See
  [Blazor front end](#blazor-front-end-showroomweb).

---

## Technical stack

| Area | Choice |
|---|---|
| Runtime | .NET 10, C# / Minimal APIs (no MVC) |
| Front end | Blazor Web App (render mode chosen per screen), in-house "swiss" design system (CSS variables, no CSS framework) |
| Front-end API client | **Refit** (typed interfaces) over `IHttpClientFactory` — selective approach: hand-written while the consumed surface is narrow, generated with **Refitter** when it widens |
| Orchestration | .NET Aspire (AppHost + ServiceDefaults) |
| Persistence | EF Core 10 + PostgreSQL (one `showroom` database, one schema per module) |
| M2M messaging | RabbitMQ through **Wolverine** — request/reply (`IMessageBus.InvokeAsync`) + **transactional outbox** (producer, single DbContext) + **durable inbox & retry/dead-letter** (consumer); PostgreSQL message stores per service (`wolverine`, `wolverine_business`) |
| Observability | OpenTelemetry (traces + metrics, OTLP export), Serilog (structured logs) |
| Identifiers | `StronglyTypedId` (Meziantou) internally, `PublicId` (`prefix_guid`) exposed over HTTP |
| Results | `Result`/`Error` + `ErrorCategory` (SmartEnum) → ProblemDetails, rather than exceptions |
| HTTP errors | Uniform RFC 7807; a malformed JSON body yields a clean `400` (`BadRequestExceptionHandler` + `UseExceptionHandler`), never a stack trace |
| Validation | FluentValidation; currency = `Currency` ISO 4217 **SmartEnum** (`Currency.IsValidCode` at the boundary, `Currency.FromCode` in the domain) |
| DI | Scrutor (handler scanning) |
| API versioning | Asp.Versioning (`/api/v{version}`) |
| Feature flags | Microsoft.FeatureManagement (`FeatureManagement:<Module>`) |
| API docs | OpenAPI + Scalar (UI in development) |
| Tests | xUnit v3, **bUnit** (Blazor components), **Playwright** (browser journeys), Testcontainers (PostgreSQL / RabbitMQ), ArchUnitNET, AwesomeAssertions, Bogus |

---

## Solution layout

```text
Pops-ShowRoom.slnx
├─ src/
│  ├─ aspire/
│  │  ├─ ShowRoom.AppHost              # Aspire orchestration (services, PostgreSQL, RabbitMQ)
│  │  └─ ShowRoom.ServiceDefaults      # OpenTelemetry, health checks, service discovery
│  ├─ backends/
│  │  ├─ ShowRoom.Customer.Api         # Customer service (messaging producer)
│  │  └─ ShowRoom.Business.Api         # Order + Product service (messaging consumer)
│  ├─ frontends/
│  │  └─ ShowRoom.Web                  # Blazor front end (Refit clients, facade per module)
│  ├─ modules/
│  │  ├─ ShowRoom.Modules.Customer     # Customer bounded context
│  │  ├─ ShowRoom.Modules.Order        # Order bounded context
│  │  ├─ ShowRoom.Modules.Order.Contracts   # shared AMQP messages (cross-service contract)
│  │  └─ ShowRoom.Modules.Product      # Product bounded context
│  ├─ buildingblocks/ShowRoom.BuildingBlocks  # DDD primitives, Result, PublicId, observability, pagination
│  └─ sharedkernel/ShowRoom.SharedKernel      # shared types: value objects (Email, PhoneNumber) + Currency SmartEnum (ISO 4217)
└─ tests/
   ├─ ShowRoom.Testing                        # integration harness (Testcontainers, generic factory)
   ├─ ShowRoom.Architecture.Tests             # boundary tests (ArchUnitNET)
   ├─ ShowRoom.Web.Tests                      # front end: units + bUnit components
   ├─ ShowRoom.Web.IntegrationTests           # front end: full chain over a disposable database
   ├─ ShowRoom.Web.E2ETests                   # front end: browser journeys (Playwright)
   └─ ShowRoom.Modules.<Module>.Tests / .IntegrationTests
```

---

## Getting started

**Prerequisites**: .NET 10 SDK, Docker (PostgreSQL + RabbitMQ through Testcontainers/Aspire).

### 1. Local secrets (one-off, per clone and per machine)

The PostgreSQL password is an **explicit parameter** the AppHost reads from user secrets and **never
generates** — see [the gotcha](#3-the-password-and-the-data-volume-are-bound-together) below. Set it once:

```bash
dotnet user-secrets set "Parameters:postgres-password" "<local-dev-password>" --project src/aspire/ShowRoom.AppHost
```

Missing it is not silent: the AppHost refuses to start with an explicit message naming the missing
configuration key, rather than bringing up a database nothing can connect to.

Secrets are keyed by the `UserSecretsId` declared in
[`ShowRoom.AppHost.csproj`](src/aspire/ShowRoom.AppHost/ShowRoom.AppHost.csproj), and stored **outside
the repository**, per user and per machine — a fresh clone, another machine or a reset Windows profile
starts with nothing.

List what exists (values are printed in clear):

```bash
dotnet user-secrets list --project src/aspire/ShowRoom.AppHost
```

Edit the file directly if you prefer (`<UserSecretsId>` is the folder name):

```bash
code "$env:APPDATA\Microsoft\UserSecrets\<UserSecretsId>\secrets.json"
```

Drop one key to start over:

```bash
dotnet user-secrets remove "Parameters:postgres-password" --project src/aspire/ShowRoom.AppHost
```

Expected keys: `Parameters:postgres-password`, `Parameters:messaging-password`, plus Aspire's own
(`AppHost:OtlpApiKey`, `AppHost:DashboardApiKey`, version check).

### 2. Run the stack

```bash
dotnet run --project src/aspire/ShowRoom.AppHost
```

Aspire starts PostgreSQL, RabbitMQ, both APIs and the Blazor front end (`showroom-web`,
`http://localhost:5206`), then opens the **dashboard** (traces, metrics, logs, endpoint discovery). Each
service exposes its **Scalar** UI (`/scalar`) in development.

The front end alone (home page, plus the screens that make no API call; screens consuming the Customer
service expect `http://localhost:5205`, the port pinned by the AppHost):

```bash
dotnet run --project src/frontends/ShowRoom.Web
```

> Tip: to watch the distributed trace, call `GET /api/v1/customers/{publicId}/with-orders` on the
> Customer service — a single trace crosses Customer.Api → RabbitMQ → Business.Api in the dashboard.

### 3. The password and the data volume are bound together

> ⚠️ `WithDataVolume()` persists the data directory, and PostgreSQL only honours `POSTGRES_PASSWORD`
> when `initdb` runs — that is, on an **empty** volume. It never reads it again. So a password that
> changes afterwards locks the existing volume out permanently, and the failure is **silent by nature**:
> the server starts and looks healthy from the outside, then rejects every connection
> (`password authentication failed for user "postgres"`), the Aspire health check fails, and **every
> resource declaring `WaitFor(showroomDb)` stays blocked** — both APIs, hence the front end. Nothing in
> the logs names the password as the culprit.

Declaring the parameter explicitly is what prevents a regeneration behind your back. If you **do** change
the password on purpose, drop the volume in the same move (Aspire stopped) — the migration pipeline
rebuilds the `customers` / `orders` / `products` schemas at the next start, and the Wolverine message
stores are auto-provisioned:

```bash
docker volume rm showroom.apphost-a6fd80faab-postgres-data
```

The same reasoning does **not** apply to `Parameters:messaging-password`: RabbitMQ carries no data volume
here, so changing it is inconsequential.

---

## Available APIs

All routes are versioned under `/api/v{version}` (v1 by default). Resources are identified by their
**`PublicId`** (never the technical identifier).

### ShowRoom.Customer.Api

| Verb | Route | Description | Responses |
|---|---|---|---|
| `POST` | `/api/v1/customers` | Creates a customer | `201` + `PublicId` · `400` · `409` (email already used) |
| `GET` | `/api/v1/customers?page=&pageSize=&search=` | Paginated list, optional name filter | `200` · `400` |
| `GET` | `/api/v1/customers/{publicId}` | Customer detail | `200` · `400` · `404` |
| `GET` | `/api/v1/customers/{publicId}/with-orders` | Customer + order history (fetched over AMQP) | `200` (with `ordersAvailable`) · `400` · `404` |
| `PATCH` | `/api/v1/customers/{publicId}/email` | Changes a customer's email (raises `CustomerEmailChanged`) | `204` · `400` · `404` · `409` (email taken) |
| `PUT` | `/api/v1/customers/{publicId}` | Updates the profile — **Style 1** task-based, fine-grained events (`CustomerRenamed`/`…EmailChanged`/`…PhoneChanged`) | `204` · `400` · `404` · `409` |
| `PUT` | `/api/v1/customers/{publicId}/profile` | Updates the profile — **Style 2** coarse, a single `CustomerProfileUpdated` (+ `ChangedFields`) | `204` · `400` · `404` · `409` |

### ShowRoom.Business.Api

**Orders**

| Verb | Route | Description | Responses |
|---|---|---|---|
| `POST` | `/api/v1/orders` | Creates an order (product lines) | `201` + `PublicId` · `400` |
| `GET` | `/api/v1/orders/{publicId}` | Order detail + lines | `200` · `400` · `404` |
| `GET` | `/api/v1/orders?page=&pageSize=&customerPublicId=` | Paginated list, optional customer filter | `200` · `400` |

**Products**

| Verb | Route | Description | Responses |
|---|---|---|---|
| `POST` | `/api/v1/products` | Creates a product | `201` + `PublicId` · `400` |
| `GET` | `/api/v1/products/{publicId}` | Product detail | `200` · `400` · `404` |
| `GET` | `/api/v1/products?page=&pageSize=&publicId=` | Paginated list, optional public id filter | `200` · `400` |

**Common to both services**: `GET /api/status`, `GET /health` (readiness), `GET /alive` (liveness),
`/openapi` + `/scalar` (development).

> The Business API also declares a **CORS** policy (`Cors:AllowedOrigins`, `GET` only, injected by the
> AppHost): the catalogue screens run in WebAssembly and call it straight from the browser. It is the
> only place where a browser is a first-class client of an API here.

---

## Blazor front end (ShowRoom.Web)

`src/frontends/ShowRoom.Web` — a **Blazor Web App** (.NET 10) orchestrated by Aspire (`showroom-web`)
and instrumented like the services (ServiceDefaults + Serilog → OTLP, `/health` and `/alive`).

**Available screens:**

| Route | Screen | API consumed |
|---|---|---|
| `/` | Home — topology (SVG), manifesto, pillars, modules | — |
| `/customers?page=&search=` | Paginated list + name filter | `GET /api/v1/customers` |
| `/customers/{publicId}` | Customer detail | `GET /api/v1/customers/{publicId}` |
| `/customers/{publicId}/orders` | Order history (cross-service aggregate) | `GET /api/v1/customers/{publicId}/with-orders` |
| `/customers/new` | Customer creation (form) | `POST /api/v1/customers` |
| `/catalog` | Product catalogue (grid, paginated) | `GET /api/v1/products` |
| `/catalog/{publicId}` | Product detail | `GET /api/v1/products/{publicId}` |

Together these screens cover the **4 reference screens** convention
([`frontend.md`](.claude/rules/frontend.md) §2.3): consultation, creation, grouping, navigation. The list
state (page, search) lives in the **query string**: the screen stays shareable by URL and the browser's
Back button works.

Layout (following [`.claude/rules/frontend.md`](.claude/rules/frontend.md) §11):

```text
src/frontends/ShowRoom.Web/
├─ App/                            # technical shell
│  ├─ App.razor                    # root HTML document
│  ├─ Layout/                      # MainLayout (header/nav/footer, scroll-top), ReconnectModal
│  └─ Routing/                     # Routes, Error, NotFound
├─ Features/
│  ├─ Home/                        # "/" page
│  └─ Customer/                    # Customer module
│     ├─ CustomerFacade.cs         # THE module's entry point for the UI (+ ICustomerFacade)
│     ├─ CustomerFormat.cs         # presentation rules shared across screens
│     ├─ CustomerProblemMessages.cs # API error codes → the French sentences the UI shows
│     ├─ CustomerListResult.cs     # outcome: Loaded / Unavailable
│     ├─ CustomerLookupResult.cs   # outcome: Found / InvalidPublicId / NotFound / Unavailable
│     ├─ CustomerCreationResult.cs # outcome: Created / EmailAlreadyUsed / Rejected / Unavailable
│     ├─ CustomerList/             # screen: Page.razor, CustomerTable, CustomerTableSkeleton,
│     │                            #         CustomerListView, CustomerListMapper
│     ├─ CustomerDetail/           # screen: Page.razor, CustomerDetailCard, CustomerDetailSkeleton,
│     │                            #         CustomerDetailView (view model), CustomerDetailMapper
│     ├─ CustomerOrders/           # screen: Page.razor, CustomerOrderCard, CustomerOrdersView,
│     │                            #         CustomerOrdersMapper
│     └─ CreateCustomer/           # screen: Page.razor, CreateCustomerForm (POCO),
│                                  #         CreateCustomerFormValidator, CreateCustomerMapper
├─ Infrastructure/
│  ├─ Api/
│  │  ├─ BackendApiOptions.cs      # service addresses ("BackendApi" section)
│  │  ├─ RefitRegistration.cs      # AddRefitClient + RefitSettings (System.Text.Json)
│  │  └─ Refit/
│  │     ├─ Models/PagedResponse.cs # shared pagination envelope (all modules)
│  │     └─ Customer/              # ICustomerApi + Models/ (List + Get + WithOrders + Create)
│  ├─ Problems/                    # ApiProblem + ApiProblemReader (RFC 7807 handling)
│  ├─ Validation/                  # FluentValidationValidator (plugs FluentValidation into EditForm)
│  └─ PublicIds/PublicIdFormat.cs  # boundary format check
├─ Shared/Components/              # reusable UI (ApiProblemPanel: the diagnostic block)
├─ customer.refitter               # Refitter configuration (optional generation — see below)
                                     #
src/frontends/ShowRoom.Web.Shared/   # Razor Class Library, browser-compatible
├─ Api/Problems/                     # ApiProblem + ApiProblemReader (RFC 7807)
├─ Api/Models/PagedResponse.cs       # shared pagination envelope
├─ Components/ApiProblemPanel.razor  # diagnostic block, used by both front ends
└─ PublicIds/PublicIdFormat.cs       # boundary format check
                                     #
src/frontends/ShowRoom.Web.Client/   # WebAssembly project — downloaded and run by the browser
├─ ClientAssembly.cs                 # marker the host declares to the router and the render mode
├─ Program.cs                        # WASM entry point (browser-side DI)
├─ Features/Catalog/                 # facade, outcomes, format, ProductList/ + ProductDetail/
└─ Infrastructure/Api/               # CatalogApiRegistration (called from BOTH sides), IProductApi
└─ wwwroot/
   ├─ app.css                      # swiss design system (CSS tokens)
   └─ App/Layout/shell.js          # shell behaviour in plain JS (mobile menu, back to top)
```

**API consumption — Refit, selectively.** The front end never calls a bare `HttpClient` nor a generated
client from a component: it orchestrates a **facade per module** (`ICustomerFacade`), which calls a
**Refit interface** registered on `IHttpClientFactory` (thus inheriting Aspire service discovery and the
ServiceDefaults resilience handler). Methods return `ApiResponse<T>`: a `404`/`400`/`409` is **business
data** translated into an explicit outcome (`CustomerLookupOutcome`, `CustomerCreationOutcome`), not an
exception. Every state (loading / empty / invalid / not found / unavailable / success) has its own branch
in the page.

**API errors: ProblemDetails, read rather than discarded.** Every ShowRoom API answers a failure with
RFC 7807. The front end parses it (`Infrastructure/Api/Problems/`) and normalises the **two shapes** the
backend emits — a validation failure keys `errors` by code, every other failure lists them:

| Failure | `errors` shape | Example code |
|---|---|---|
| Validation (400) | object keyed by code | `Validation.Email` |
| Business (404 / 409) | array of `{code, message, category}` | `Customer.NotFound`, `Customer.EmailAlreadyExists` |

Three properties are deliberate:

- **The code is the contract, the message is a hint.** The API answers in its own language, so the
  user-facing sentence is resolved from the CODE (`CustomerProblemMessages`), never from the server's
  `detail` — which would put backend wording in the UI and couple the screen to a string the backend may
  reword. The server text stays available in the diagnostic panel.
- **`Validation.<PropertyName>` lands on the field it names.** The backend codes its FluentValidation
  failures over command properties carrying the form's own names, so a server-side rejection appears
  under the right input instead of in a vague banner — pushed into a second `ValidationMessageStore`,
  cleared on every validation request (otherwise a stale server error would veto every later submit,
  `EditContext.Validate()` being false while any store holds a message).
- **The `traceId` is surfaced**, in a collapsed `<details>` block next to the message: it is what ties a
  user-visible failure back to its distributed trace. Nothing is rendered when the response carried no
  readable body — a transport failure shows no empty shell.

The parser never throws: an empty body, an HTML page from a reverse proxy or a truncated payload all
degrade to the bare status. A contract test in the integration suite (`ProblemDetailsContractTests`)
pins the shapes against the **real** API — unit tests only prove the reader parses what we believe is
returned.

**Hosting model and render modes.** `ShowRoom.Web` is a **Blazor Web App** (the unified .NET 8+ model,
one project, `blazor.web.js`) — not the old "ASP.NET Core hosted WebAssembly" template (removed in
.NET 8, replaced by render modes). No WebAssembly is shipped: the Refit client and the facade live
server-side, so the browser never learns that the APIs exist (**BFF** property). Switching to
`InteractiveWebAssembly` / `InteractiveAuto` would require a Client project, exposing the APIs to the
browser (CORS + auth), and would make the **first** load heavier — an architecture decision, not a
setting.

The render mode is decided **per screen**, not globally:

| Screen | Render mode | Runs where | Circuit |
|---|---|---|---|
| Home, customer list, detail, orders | **Static SSR** + `[StreamRendering]` | server | **none** |
| Customer creation | `InteractiveServer` | server | yes (validation while typing) |
| **Catalogue, product detail** | **`InteractiveWebAssembly`** | **browser** | none (no circuit either) |

Verified on the served HTML: 0 interactive-component markers on the read screens, 1 on `/customers/new`.
Interactions on static screens go through the web: a **GET form** for the filter, **links** for
pagination and "Retry" actions — enhanced navigation from `blazor.web.js` renders them without a full
reload, and URLs stay shareable.

The shell (mobile menu, back-to-top) is driven by `wwwroot/App/Layout/shell.js` in **plain JS**: turning
those into interactive components would reopen a circuit on every page and cancel the benefit of static
SSR.

**The catalogue runs in the browser (WebAssembly).** It is the third render mode of the same
application, deliberately kept as a comparison point. Three consequences are structural, not incidental:

- **A separate project is mandatory.** `ShowRoom.Web.Client` is a `Microsoft.NET.Sdk.BlazorWebAssembly`
  project: the host assembly is never sent to the browser, so a WASM component cannot live in it. The
  host declares that assembly twice — to the router and to the render mode.
- **The component runs twice.** Prerendering is on, so it executes once on the server before the runtime
  reaches the browser. Its dependencies exist in both containers through a **single** registration
  method (`AddCatalog`) called from both `Program.cs` — one place, no drift.
- **The BFF property is lost for these screens.** The browser resolves the API address itself, so it must
  be absolute (service discovery means nothing there) and `ShowRoom.Business.Api` declares **CORS** for
  the front's origin, `GET` only. The server-rendered screens keep their BFF property: the arbitration is
  per screen, and this is what it costs.

Measured on a Release publish, with trimming:

| | First load | Interaction |
|---|---|---|
| Server-rendered screen (`/customers`) | 21.6 KB | round-trip to the server |
| Catalogue (`/catalog`) | **2.9 MB Brotli** for `_framework` (9.6 MB raw) — ~2.4 MB actually fetched, only one of the three ICU files being downloaded | **local**: paginating re-issues the HTTP call alone, with no navigation |

That is roughly a hundredfold on first load, bought back on every later interaction. Verified in the
browser: no circuit marker on `/catalog` (a `"type":"webassembly"` marker instead), the products request
leaving the browser straight to `https://localhost:7106`, and paging that changes the page without
losing the JavaScript context.

> ⚠️ **Scoped CSS does not travel the same way.** A Razor Class Library's bundle is auto-imported into
> the host's `ShowRoom.Web.styles.css`; a referenced WebAssembly **project** produces its own bundle,
> which is not. It must be linked in `App.razor`, otherwise the screens render **unstyled with no error
> anywhere** — the markup even carries its scope attribute, which makes it look like a CSS mistake.

**Streaming.** Read screens declare `@attribute [StreamRendering]`: the shell leaves from the first byte
and data is streamed as soon as the API answers, instead of holding back the whole HTML response.
Measured on the orders screen with the Order service stopped (worst case, 6.3 s of degradation):

| | TTFB | Full response |
|---|---|---|
| Without streaming | **6.36 s** | 6.36 s |
| With streaming | **0.03 s** | 6.32 s |

That is a ~150× faster first byte, and a 6.4 s blank screen replaced by an immediate shell + skeleton.
On the list (warm API): TTFB 0.05 s, full response 0.40 s.

**Compression and HTTP caching.** `UseResponseCompression` (Brotli + Gzip, optimal level) is enabled for
dynamic responses. Static assets do not go through it: `MapStaticAssets` already serves them
**pre-compressed and fingerprinted** (verified: `app.css` returned as `br`, 1,988 bytes, with an ETag).

| Page | Raw size | Brotli size | Encoding |
|---|---|---|---|
| `/` | 17,967 B | **4,969 B** (−72%) | `br` |
| `/customers/new` | 8,742 B | **3,590 B** (−59%) | `br` |
| `/customers`, detail, orders | 21,575 B | 21,575 B | `identity` |

> ⚠️ **Streaming and compression are mutually exclusive.** The three `[StreamRendering]` screens are
> served as `Content-Encoding: identity` — a compressing intermediary could buffer the response and
> destroy the streaming. The trade-off is deliberate: on the orders screen's degraded path, streaming
> brings TTFB from 6.36 s down to 0.03 s, which no compression comes close to. TTFB verified unchanged
> after enabling compression (`/customers`: 0.05 s).

**HTTP caching: deliberately no output cache.** A measured decision, not an assumption:
- customer screens carry **`no-store, no-cache, must-revalidate`** — a cached list would hide the
  customer just created, and a "back" on a shared machine would replay personal data;
- the home page, the only candidate, emits an **antiforgery `Set-Cookie`** like every Razor Components
  SSR response. The output cache rightly refuses to store such a response, and forcing it would hand one
  visitor's antiforgery token to every other visitor. Measured: 5 successive requests at 29–37 ms, with
  no cache plateau whatsoever. The gain (~30 ms of server rendering) is not worth the risk. Revisit only
  behind a CDN/proxy that strips the cookie.

**Loading.** The list does not say "Loading…": it renders a **skeleton** mirroring the real table
(5 rows), streamed from the first byte. Pagination stays rendered but hidden with `visibility: hidden`,
so incoming data shifts nothing. Shimmer and fade are disabled under `prefers-reduced-motion`.

Two settings were **removed after measurement**, and the reasons are instructive:

| Removed setting | Why |
|---|---|
| 350 ms minimum skeleton display | Under SSR + streaming the skeleton ships in the first flush and the browser coalesces paints: nothing flickers. The floor delayed the full response by 345 ms (381 ms → 52 ms) for no benefit. |
| Skeleton sized after the previous page | Under static SSR every request instantiates a fresh component: an instance field always starts from its default. Sizing it on `PageSize` grew the response from 21.6 KB to 28.4 KB, paid on every request. |

Three variants were measured on `/customers` before deciding (median of 7 requests):

| Variant | TTFB | Full response | Size | Encoding |
|---|---|---|---|---|
| Streaming + 350 ms floor | 19 ms | 381 ms | 21,575 B | `identity` |
| **Streaming without floor** *(chosen)* | **38 ms** | **52 ms** | 21,575 B | `identity` |
| No streaming (plain SSR) | 35 ms | 35 ms | **3,763 B** | `br` |

Streaming only saves time when the API is slow — its TTFB stays constant regardless, while plain SSR's
TTFB tracks API latency. On the list (API at ~30 ms) both tie on time and plain SSR is 5.7× lighter;
streaming is kept to preserve a loading indicator for the day the list slows down, and for consistency
with the orders screen where the gap is decisive (TTFB 0.02 s against a 2 s full response, measured while
degraded).

**Behaviour at scale (measured with 200,026 customers).** The front end is not the limiting factor:
pagination caps the page at 20 rows, so its size and TTFB are constant whatever the volume. The list's
SQL queries are what plateau.

The list's `ORDER BY "CreatedAt" DESC` had no index: every page triggered a sequential scan then a full
sort **spilling to disk** (`external merge Disk: 9864kB`). The `IX_customers_CreatedAt` index (migration
`AddCustomerCreatedAtIndex`) fixes it:

| | Without index | With index |
|---|---|---|
| SQL page 1 | 41 ms (`Seq Scan` + disk sort) | **0.12 ms** (`Index Scan`) |
| API page 1 | 0.070 s | **0.032 s** |
| API page 5000 | 0.112 s | **0.039 s** |
| Front list page 1 (TTFB / full) | 0.032 / 0.071 s | **0.017 / 0.038 s** |
| Front list page 5000 | 0.030 / 0.107 s | **0.014 / 0.047 s** |

Two limits remain, known and not addressed at this stage:

1. **Deep `OFFSET`** — even with the index, page 5000 walks 100,020 index entries. Only *keyset*
   pagination (`WHERE "CreatedAt" < @last`) makes the cost independent of depth.
2. **`ILIKE '%…%'` search** — not indexable by a b-tree, full `Seq Scan` (~148 ms at 200 k). A **GIN +
   pg_trgm** index is the answer.

The per-page `COUNT(*)` stays modest (~35 ms at 200 k); beyond a million rows it would need an
approximate or cached count. The front-end design — streaming + skeleton — is precisely the one that
absorbs these degradations best: its TTFB stays stable however slow the query gets.

**Cross-service degradation, visible on screen.** `/customers/{publicId}/orders` consumes the aggregating
`with-orders` endpoint: the Customer service fetches the history from the Order service **over AMQP**.
When that hop fails, the backend answers `200` with `ordersAvailable = false` (graceful degradation) — a
**partial success** the screen renders as such: customer identity preserved, a `role="status"` banner
explaining that only the orders are missing, and a Retry action. It is neither an error nor a "customer
with no orders".

> ⚠️ **HTTP client time budget.** This degradation path only answers once the backend's retry budget is
> spent (**6.3 s** measured — see `OrderHistoryRetryPolicy` under [Messaging](#messaging--observability)).
> The default ServiceDefaults resilience handler cuts at **10 s per attempt**, which was shorter than the
> original backend budget (~16 s): it killed the call before the degraded answer and turned an
> explainable partial success into "service unavailable". The `ICustomerApi` client therefore replaces
> that pipeline (`RemoveAllResilienceHandlers` + `AddStandardResilienceHandler`, **12 s per attempt**,
> 1 retry) — see `RefitRegistration.cs`. The two budgets are **coupled**: any API aggregating other
> services imposes the same rule — the client budget must outlast the slowest degradation path it wants
> to observe, and must be tightened when that path is.

**Forms.** Validation is **FluentValidation** only (never DataAnnotations): a POCO model, a separate
`AbstractValidator<T>` with French end-user messages, plugged into the `EditForm` by the
`Infrastructure/Validation/FluentValidationValidator` component. It validates the whole model on submit
but replaces only the touched field's messages while typing — a field the user has not filled in yet
never shows an error prematurely. Client rules mirror the backend constraints, and the backend remains
the authority: a duplicate email can only be detected server-side (409 → dedicated message, the user
stays on the form; success → redirect to the created customer).

**Generation with Refitter (optional).** While the consumed surface stays narrow, the interface is
hand-written. When it widens, `customer.refitter` regenerates it from the service's OpenAPI document —
without touching any caller, the facade being the only boundary:

```bash
dotnet tool install --global refitter
```

Then, **with the Customer service running** (`openApiPath` points at its `/openapi/v1.json` document):

```bash
refitter --settings-file src/frontends/ShowRoom.Web/customer.refitter
```

The configuration file is provided as a starting point but **has not been executed yet**: check the
generated interface name (`multipleInterfaces: ByTag` derives it from the OpenAPI tag) before replacing
the hand-written interface.

**"Swiss" design system**: Helvetica Neue, an 8 px spacing scale, flat areas and 1 px rules, spaced
uppercase labels, no CSS framework. Tokens (`--color-*`, `--space-*`, `--font-size-*`, `--transition-*`)
are defined once in `wwwroot/app.css`; each page writes only its own **scoped** `*.razor.css` and
consumes those variables. Mobile-first: breakpoints at `1024px` / `768px`, touch targets ≥ 44 px, no
horizontal overflow.

---

## Messaging & observability

- **Contract**: `ShowRoom.Modules.Order.Contracts` — `GetOrdersForCustomer` /
  `OrdersForCustomerResponse` messages, pure records with no implementation dependency.
- **Producer** (Customer.Api): `IMessageBus.InvokeAsync<OrdersForCustomerResponse>` behind an
  anti-corruption port (`IOrderHistory`), with graceful degradation and a **bounded cold-start retry**
  (the very first message provisions the connection + reply queue; later attempts hit a warm path, and
  since the read is idempotent the retry is safe).
- **Time budget of that read path** — `OrderHistoryRetryPolicy` (slice `Features/GetCustomerWithOrders/`).
  This read serves an HTTP request: its worst case is user-visible latency, so deadlines are **explicit
  per attempt** instead of inheriting Wolverine's 5 s timeout:

  | | Deadline | Role |
  |---|---|---|
  | Attempt 1 | 2 s | warm path (answers in ms) — fails fast |
  | Backoff | 200 ms | |
  | Attempt 2 | 4 s | absorbs cold-start provisioning |
  | **Worst case** | **6.2 s** | before the degraded answer (`ordersAvailable = false`) |

  Measurements: cold start **2.9 s** with a complete history (the 2nd attempt succeeds), warm path
  **0.07 s**, degraded **6.3 s** (against ~16 s with the previous 3 × 5 s budget). A unit test
  (`OrderHistoryRetryPolicyTests`) pins that ceiling.
- **Consumer** (Business.Api): a Wolverine handler listening on the RabbitMQ queue, answering from
  `OrdersContext`.
- **Traces**: `Wolverine` + `RabbitMQ.Client.*` OpenTelemetry sources registered; Wolverine propagates
  the W3C trace context across the broker → a single distributed trace producer → broker → consumer →
  reply. Every element (HTTP → send → publish → deliver → handle → business handler → **SQL queries** via
  Npgsql instrumentation) is a span with its **duration**. Wolverine (`Wolverine*`), runtime, ASP.NET
  Core and **Npgsql** (pool/commands) metrics are exported.
- **Logs**: structured Serilog, enriched with `[Module] [Feature] [RequestId]`, `RequestId` derived from
  the `TraceId` so logs and traces correlate across both services.

### Transactional outbox (guaranteed IntegrationEvent delivery)

Where `GetOrdersForCustomer` is a best-effort **read** (request/reply, graceful degradation), an
*IntegrationEvent* such as `CustomerRegisteredIntegrationEvent` demands **at-least-once delivery**
between services. ShowRoom guarantees it with Wolverine's **transactional outbox**, without mixing
concerns — **a single DbContext**, separation happening at the **schema** level:

- **Single DbContext + dedicated schema** — `CustomersContext` carries the business tables (schema
  `customers`); Wolverine's envelope tables live in the `wolverine` schema of the **same** database.
  Atomicity comes from exactly that: the outbox writes the envelope **through the DbContext's own
  connection**, hence in the same transaction. The message store
  (`PersistMessagesWithPostgresql(showroom, "wolverine")`) is configured once in
  `ConfigureShowRoomMessaging`, driven by `Messaging:UseTransactionalOutbox`; its tables are
  auto-provisioned, **outside** the module's migrations.
- **Atomic publication** — the `CreateCustomer` handler injects `IDbContextOutbox<CustomersContext>`,
  works on `outbox.DbContext`, publishes the event, then calls `SaveChangesAndFlushMessagesAsync()`: the
  customer insert **and** the envelope are written in **one transaction** (all or nothing).
- **Durable delivery** — the RabbitMQ sending endpoint declares `UseDurableOutbox()`: a Wolverine agent
  replays the envelope until acknowledged, surviving process crashes and broker outages.
- **Broker-free tests** — integration tests disable external transports
  (`DisableAllExternalWolverineTransports`) and clear Wolverine storage
  (`ClearAllWolverineStorageAsync`); `CreateCustomerOutboxTests` uses Wolverine message tracking
  (`TrackActivity().ExecuteAndWaitAsync(...)`) to assert the event is emitted by the outbox — with no
  real RabbitMQ.

**Consumer + retry / dead-letter (Business.Api).** The Order module consumes
`CustomerRegisteredIntegrationEvent` (slice `Features/OnCustomerRegistered`):

- **Durable inbox** — `ListenToRabbitQueue(...).UseDurableInbox()`; Business.Api has **its own** message
  store (`Messaging:UsePersistentMessageStore`, schema **`wolverine_business`** — one Wolverine runtime
  never shares another service's store).
- **Retry/dead-letter policy** — a *poison* message (unprocessable, here missing its `PublicId`) raises
  `UnprocessableCustomerRegisteredException`; the policy replays it a few times with a cooldown then
  moves it to the **dead-letter** table (`wolverine_business.wolverine_dead_letters`) rather than
  replaying it forever. It is **scoped by exception type**, so the `GetOrdersForCustomer` request/reply
  is unaffected.
- **End-to-end proof** — `CustomerRegisteredConsumerE2ETests` (PostgreSQL + RabbitMQ Testcontainers, real
  broker): a well-formed message is handled (`MessageSucceeded`), a poison message ends up dead-lettered
  (`MovedToErrorQueue`).

> Domain events (in-process, at-most-once) versus integration events (cross-service, at-least-once): the
> former go through a best-effort `SaveChangesInterceptor`, the latter through this durable outbox.

### Messaging metrics (`MessagingMetrics`) — board-ready

A dedicated **`ShowRoom.Messaging`** meter (registered with `AddMeter` in both hosts), complementing —
never replacing — spans and Wolverine's own metrics. Two duration histograms (`ms`) plus a counter:

| Instrument (OTel) | Type · unit | Recorded by | Measures |
|---|---|---|---|
| `showroom.messaging.roundtrip.duration` | Histogram · `ms` | Producer (Customer.Api, `IOrderHistory` port) | full round-trip as the caller perceives it: produce → reply received (broker + network + handling) |
| `showroom.messaging.handler.duration` | Histogram · `ms` | Consumer (Business.Api, AMQP handler) | message handling time: dequeue → reply produced |
| `showroom.messaging.retries` | Counter · `{retry}` | Producer (Customer.Api, port) | number of request/reply retries (cold start / retried timeouts) |

**Tags** (for `group by` / filters on the board):

| Tag | Values | On |
|---|---|---|
| `messaging.module` | `Customer`, `Order` | roundtrip · handler · retries |
| `messaging.feature` | `GetCustomerWithOrders`, `GetOrdersForCustomer` | roundtrip · handler · retries |
| `messaging.message` | `GetOrdersForCustomer` | roundtrip · retries |
| `messaging.outcome` | `success`, `failure`, `invalid_request` | roundtrip · handler |

**Typical panels** (OTel histograms → in Prometheus, `.` becomes `_` and the unit is suffixed, hence
`showroom_messaging_roundtrip_duration_milliseconds_*`):

```promql
# p95 round-trip latency (per feature)
histogram_quantile(0.95, sum by (le, messaging_feature) (
  rate(showroom_messaging_roundtrip_duration_milliseconds_bucket[5m])))

# Pure transport latency ≈ round-trip − handler (p95), isolates the AMQP cost from business handling
histogram_quantile(0.95, sum by (le) (rate(showroom_messaging_roundtrip_duration_milliseconds_bucket[5m])))
- histogram_quantile(0.95, sum by (le) (rate(showroom_messaging_handler_duration_milliseconds_bucket[5m])))

# Round-trip failure rate (degradations / unrecovered timeouts)
sum(rate(showroom_messaging_roundtrip_duration_milliseconds_count{messaging_outcome!="success"}[5m]))
/ sum(rate(showroom_messaging_roundtrip_duration_milliseconds_count[5m]))
```

In the **Aspire dashboard** (dev): *Metrics* tab → resource → `ShowRoom.Messaging` meter; the durations
are also set as span tags (`customer.orders.roundtrip_ms`, `messaging.handler.duration_ms`), visible in
the *Traces* tab.

**Ready-made Grafana dashboard**: [`docs/observability/showroom-messaging.grafana.json`](docs/observability/showroom-messaging.grafana.json)
— importable as is (Grafana → *Import* → pick the Prometheus datasource). Panels: round-trip p50/p95/p99,
handler p50/p95/p99, transport latency (round-trip − handler), throughput by `outcome`, failure rate,
p95 round-trip per feature, **retries (cold start)**; a `feature` variable to filter.

### Business metrics (Orders)

Domain KPIs (not infrastructure timings), emitted on **each module's meter**
(`ShowRoom.Modules.<Module>`, registered with `AddMeter(<Module>.TelemetrySourceName)`), from the
`Create*` handlers — pattern: module meter + a `*Metrics` helper co-located in the slice.

| Instrument (OTel) | Module | Type | Tags | Measures |
|---|---|---|---|---|
| `showroom.orders.created` | Order | Counter · `{order}` | `order.currency` | orders created (rate = orders/s) |
| `showroom.orders.amount` | Order | Histogram | `order.currency` | order total; `sum/count` = **average basket** (€), buckets = distribution |
| `showroom.orders.items` | Order | Histogram · `{item}` | — | items per order; `sum/count` = **average basket in items** |
| `showroom.customers.registered` | Customer | Counter · `{customer}` | — | registered customers (**acquisition**) |
| `showroom.<entity>.create.rejected` | Order · Product · Customer | Counter · `{rejection}` | `reason` (`validation`/`domain`/`conflict`) | **rejected** creations — funnel quality |

```promql
# Average basket (€) per currency
sum by (order_currency) (rate(showroom_orders_amount_sum[$__rate_interval]))
/ sum by (order_currency) (rate(showroom_orders_amount_count[$__rate_interval]))

# Average basket in items
sum(rate(showroom_orders_items_sum[$__rate_interval])) / sum(rate(showroom_orders_items_count[$__rate_interval]))

# Customers registered per minute
sum(rate(showroom_customers_registered_total[$__rate_interval])) * 60

# Creation rejections by module & reason
sum by (reason) (rate(showroom_orders_create_rejected_total[$__rate_interval]))
```

**Business dashboard**: [`docs/observability/showroom-business.grafana.json`](docs/observability/showroom-business.grafana.json)
— orders created/min, average basket (€ and items), amount distribution (p50/p95), customers
registered/min, **creation rejections by reason** (all three modules), totals over the period; a
`currency` variable.

---

## Tests

```bash
dotnet test Pops-ShowRoom.slnx
```

Levels are layered by cost, and each has an **admission criterion**: a test belongs to a level only if
that level's machinery buys a signal the cheaper level cannot produce.

| Suite | Tooling | Docker | Scope |
|---|---|---|---|
| `*.Tests` | xUnit v3 | no | domain, assemblers (`To`/`From`), validators |
| `ShowRoom.Web.Tests` | xUnit v3 + **bUnit** | no | front-end units + component rendering/interactions |
| `*.IntegrationTests` | xUnit v3 + Testcontainers | PostgreSQL | endpoints on an isolated host |
| `ShowRoom.Web.IntegrationTests` | xUnit v3 + chained factories | PostgreSQL | front → API → EF → PostgreSQL, and the ProblemDetails contract |
| `ShowRoom.Web.E2ETests` | **Playwright** | PostgreSQL + RabbitMQ | browser journeys |
| `ShowRoom.Architecture.Tests` | ArchUnitNET | no | layer and module boundaries |

- **Back-end integration** (`*.IntegrationTests`): endpoints on an isolated host through a PostgreSQL
  Testcontainer; the `ShowRoom.Testing` harness (`BusinessWebFactory<TEntryPoint>`) is generic and each
  suite targets its own service.
- **Front-end units & components** (`ShowRoom.Web.Tests`) — two layers, no network and no database:
  - *units*: DTO → view model mapping, validators, facade orchestration — every HTTP status
    (200/400/404/409/5xx) and network unavailability, through a hand-written stub of the Refit interface;
  - *components* (**bUnit** on xUnit v3): screen rendering and interactions. One test = one user scenario
    (found, not found, invalid identifier, service unavailable, **partial success**), using semantic
    selectors (`role`, `aria-label`, `href`, `th[scope]`) rather than CSS classes. Injected services are
    doubled by hand — no mocking framework.
- **Front end, full chain** (`ShowRoom.Web.IntegrationTests`) — the whole chain over a database **created
  then destroyed**: two chained `WebApplicationFactory` instances (front + Customer service) wired
  together through `Server.CreateHandler()`, the second backed by a PostgreSQL Testcontainer. The front
  end renders its SSR HTML from data **actually written by the API**, seeded by the tests themselves via
  `POST /api/v1/customers`. No open port, no network, no pre-existing data — each test class gets its own
  container, destroyed at the end (a property verified by `DatabaseIsolationTests`).
- **Front end, browser journeys** (`ShowRoom.Web.E2ETests`) — **Playwright** over a few critical
  journeys: disposable PostgreSQL + RabbitMQ containers, the Customer service and the front end launched
  as **real processes** from their own build output, then Chromium. The only level that exercises the
  **interactive circuit** (creation form: validation while typing, submission, redirect, duplicate email)
  and the **shell JavaScript** (mobile menu, back to top) — out of reach for both bUnit and HTTP tests.
  7 journeys, ~16 s, stable across four consecutive runs.
- **Architecture** (`ShowRoom.Architecture.Tests`): boundaries between layers and between modules (a
  module depends only on another's `*.Contracts`, never on its implementation).
