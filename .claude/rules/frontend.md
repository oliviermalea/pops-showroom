# Rules — ShowRoom Frontend (v2)

## QUICK HARD RULES (must always pass)
- Frontend must be organized by business domains/features aligned with backend modules.
- Frontend must remain a presentation and local orchestration layer.
- Core business rules belong to backend domain, not UI components.
- API consumption must follow stable contracts and versioning (`/v1`, `/v2` for breaking changes).
- Mobile-first and responsive design are mandatory by default.
- Accessibility and predictable UX states are mandatory.
- Tests are mandatory for critical UI flows and mapping/orchestration logic.
- Apply ShowRoom **4 reference screens** per feature.
- API First is mandatory: mock REPR contracts to parallelize backend/frontend work.

---

## 0) Absolute priority
If rules conflict, apply in this order:
1. Domain/module alignment and boundaries
2. API contract integrity
3. UX reliability (4 reference screens, accessibility, responsiveness)
4. Test coverage and maintainability
5. Secondary conventions

---

## 1) Purpose
These instructions define how frontend code must be generated/refactored in ShowRoom to stay aligned with the backend modulith and API-first strategy.

Goal: deliver fast UX iterations while preserving modular consistency, contract stability, and production reliability.

---

## 2) Target frontend architecture

### 2.1 Domain-oriented structure
Organize frontend by domain/feature, not by technical layer only.

Recommended:
```text
src/
  modules/
    catalog/
      features/
        list-products/
        get-product-details/
      components/
      api/
      mappers/
      state/
```

### 2.2 Feature-first slices
Each frontend feature should include (when relevant):
- `View` (page/component)
- `State` (local/global orchestration)
- `Api Client` (typed contract consumption)
- `Mapper/Assembler` (API DTO <-> UI model)
- `Tests`

### 2.3 ShowRoom 4-reference-screens rule (mandatory by feature)
For each frontend feature, reason using these 4 reference screens:

1. **Consultation**: entity detail view carrying the feature (e.g., product details page).
2. **Creation/Edition**: create/edit view (edition can be postponed if complex or low-value initially).
3. **Grouping**: one grid/list with pagination and filters (can be postponed if needed).
4. **Navigation**: menu entries with their contextual links.

This is a minimum convention to frame scope and implementation effort per feature.

### 2.4 Complementary ShowRoom frontend conventions
- **API First**: mock REPR contracts to enable backend/frontend parallel development.
- **Reusable components**: minimal design system baseline (1 theme, 3 button types, navbar).
- **UX refactor cadence**: each iteration must include at least one UI/UX improvement.

These rules are intentionally minimal (not exhaustive), but mandatory as a foundation baseline.

### 2.5 Boundaries
- Do not import internal code across domains in an uncontrolled way.
- Shared code goes to explicit shared packages/folders (`shared/ui`, `shared/utils`, etc.).
- Avoid “god” shared folders that hide coupling.

---

## 3) API consumption rules

### 3.1 Contract-first consumption
Before wiring UI behavior, define:
- endpoint route and verb
- request payload shape
- response payload shape
- error payload shape and status handling

### 3.2 Versioning
- Breaking backend API changes require migration (`/v2`).
- Frontend must not silently assume backward-incompatible payload changes.

### 3.3 Typed clients
- Use explicit typed contracts/interfaces for API requests/responses.
- Never rely on `any` for core business payloads.
- Normalize/validate unknown external fields at boundaries.

### 3.4 Error handling
Handle at least:
- validation errors (`400`)
- unauthorized/forbidden (`401/403`)
- not found (`404`)
- conflict (`409`)
- unexpected server errors (`5xx`)
with consistent UI behavior.

---

## 4) UI/UX quality baseline

### 4.1 Mandatory states for async/data screens
Every data-driven screen must explicitly implement:
- loading behavior
- empty behavior
- error behavior
- success behavior

### 4.2 Responsive — non-negotiable ⚠️
Mobile traffic represents the majority of internet usage. **Every page and component must be fully responsive**, without exception.

Mandatory rules:
- **Mobile-first**: design and code for small screens first, then enhance for tablet and desktop via `@media (min-width: …)` or `@media (max-width: …)` breakpoints.
- **Breakpoints aligned** with the project convention: `768px` for mobile → tablet, `1024px` for tablet → desktop.
- **Typography scaling**: large headings (`font-size-2xl`, `font-size-3xl`) must be reduced on mobile to avoid overflow.
- **Spacing scaling**: page gaps, card padding, and section spacing must shrink on mobile using the spacing scale (`--space-*`).
- **No horizontal scroll**: content must never overflow the viewport width on any screen size.
- **Touch targets**: interactive elements must be at least `44×44px` on mobile.
- A page that is not responsive is **not Done** (see Definition of Done §9).

### 4.3 Accessibility baseline
- Semantic HTML first
- Keyboard navigability
- Labels/ARIA where needed
- Sufficient contrast
- Avoid inaccessible custom controls when native elements work

---

## 5) Testability and development strategy (Foundation)

### 5.1 Targeted TDD
Prioritize TDD on:
- critical frontend orchestration logic
- mapping/assembler transformations
- critical form logic where regressions are costly

### 5.2 BDD on critical user/API journeys
Use behavior scenarios (Given/When/Then style) for critical flows:
- onboarding, checkout, payment, permissions, etc.
- ensure expected API-visible behavior remains stable across iterations

### 5.3 ODD (Observability-Driven Development) baseline
Frontend should emit exploitable signals for critical workflows:
- structured client logs (when available)
- correlation identifiers (trace/request id propagation when available)
- minimal operational metrics on key UX flows (where tooling exists)

---

## 6) State management and side effects

### 6.1 Keep state local by default
- Prefer local component state first.
- Promote to global state only when cross-feature sharing is required.

### 6.2 Predictable side effects
- Isolate API side effects in dedicated hooks/services.
- Avoid side effects directly in render paths.
- Ensure cancellation/cleanup for async calls where relevant.

### 6.3 Business logic placement
- Frontend can orchestrate and format.
- Core domain decisions/rules must remain backend-owned.

---

## 7) Mapping / assembler guidance (frontend)

When backend DTOs differ from UI view models:
- Use dedicated mapper/assembler functions.
- Keep mapping explicit and testable.
- Avoid scattered inline transformations across components.

Mapping intent:
- `fromApi(dto) -> viewModel`
- `toApi(formModel) -> requestDto`

---

## 8) Performance baseline

- Avoid unnecessary re-renders (memoization only when justified).
- Split heavy screens/components when meaningful.
- Lazy-load large feature areas when possible.
- Keep network calls minimal and avoid duplicate fetches.
- Prefer optimistic UX only when rollback/error strategies are explicit.

---

## 9) Definition of Done (Frontend)

A frontend feature is Done only if:
- aligned with domain/feature module structure
- applies the 4 reference screens convention
- consumes API contracts explicitly and safely
- implements loading/empty/error/success behavior where applicable
- is responsive and accessibility-aware
- keeps business core logic out of UI
- includes meaningful tests for critical flows/mapping
- includes at least one UI/UX improvement in the iteration

---

## 10) Page layout and margin conventions (Blazor)

### 10.1 Margin ownership
The global layout wrapper (`.layout__content` in `MainLayout.razor.css`) is the **sole owner** of page-level vertical spacing.  
It applies `padding-block: var(--space-8)` uniformly to every page rendered via `@Body`.

### 10.2 Page component rules
- Page components must **not** add top/bottom padding or margin at their root element; set `padding: 0` explicitly to prevent drift.
- Sections inside a page may use `padding-block` for their own internal rhythm, but must not duplicate the outer layout spacing.
- Horizontal padding is handled by the `.container` utility class applied on `.layout__content`; pages must not add their own horizontal padding at the root level.

### 10.3 Header and footer — the `__inner` pattern
The `<header>` and `<footer>` span the full page width (background, border).
Their **content** must be wrapped in an `__inner` element that carries the horizontal alignment:

```css
.site-header__inner,
.site-footer__inner {
    max-width: var(--container-max);
    margin: 0 auto;
    padding-left: var(--container-pad);
    padding-right: var(--container-pad);
    width: 100%;
}
```

- Never put `padding-left/right` directly on `.site-header` or `.site-footer`.
- The `__inner` automatically inherits the `--container-pad` reductions from the global media queries.
- `position: relative` (required by the mobile dropdown menu) belongs on `__inner`, not on the
  `<header>` element itself.

### 10.4 Horizontal overflow
- Use `overflow-x: clip` on `.layout`, **never** `overflow-x: hidden` on `body`.
  `hidden` on `body` creates a new scrolling context that clips `position: fixed` elements
  (scroll-to-top, menus). `clip` on `.layout` blocks the overflow without creating that context.

### 10.5 Content overflow inside cards
Any long text or code (e.g. a `PublicId`, a title) must be constrained:
- Titles: `overflow-wrap: break-word`
- Codes/identifiers: `word-break: break-all` + `max-width: 100%`

### 10.6 Consequence
Any new Blazor page (feature or shared) must follow this contract so that all pages present identical outer spacing without per-page adjustments.

---
## 11) ShowRoom.Web project structure

The project-specific structure for `src/frontends/ShowRoom.Web` must follow this layout:

```text
/App
  DependencyInjection/
  Routing/
  Layout/

/Infrastructure
  /Api
    /Generated
      ... Kiota generated files ...
    KiotaRegistration.cs
    BackendApiOptions.cs

/Features
  /Product
    /CreateProduct
      Page.razor
      CreateProductForm.razor
      CreateProductCommand.cs
      ProductFacade.cs
    /ProductList
      Page.razor
      ProductCard.razor
      GetProductsQuery.cs
      ProductFacade.cs

  /Identity
    /Login
      Page.razor
      LoginForm.razor
      LoginFacade.cs

  /Customer
    /GetCustomerWithOrders
      Page.razor
      CustomerOrdersView.razor
      CustomerFacade.cs

/Shared
  /Components
  /Layout
```

- Keep existing screens under `Features/<Domain>/<Screen>` with their page and related components in the same subfolder.
- Keep shared UI under `Shared` only when it is reused across multiple features.
- Do not create a duplicate instructions file under `src/frontends/ShowRoom.Web`; this file in `.claude/rules` is the canonical location for frontend rules.
- Technical shell components (`App.razor`, routing, layouts, error/not-found surfaces, reconnect UI) must live under `App/` and use `App/_Imports.razor` for local shared imports.
- Feature-specific static assets should be colocated under `wwwroot/<Feature>/...` and imported from the feature that owns them.
- Reusable UI belongs in `Shared/Components` or `Shared/Layout`; domain pages and their local parts stay inside `Features/<Domain>/<Screen>`.
- Typed API clients are **Refit** interfaces under `Infrastructure/Api/Refit/<Module>/` (contracts in `.../<Module>/Models/`), registered from `Infrastructure/Api/RefitRegistration.cs`. Kiota is not used in ShowRoom.

---

## Validated Blazor frontend conventions

Each subsection below records a decision that was implemented **and verified** on a concrete screen.
The step it was validated on is named in the heading, so the context of the decision stays traceable.

### Blazor validation
- FluentValidation (`AbstractValidator<T>`), never DataAnnotations on form models.
- A `FluentValidationValidator` component (`Infrastructure/Validation/`) — the reference pattern for
  .NET 8+.
- `AddValidatorsFromAssemblyContaining<Program>(ServiceLifetime.Scoped)` in `Program.cs`.
- Error messages are written in French, addressed to the end user (the application's UI language).

### Facade
- One `<Module>Facade` per module, injected as Scoped, behind an `I<Module>Facade` interface (placed at
  the root of `Features/<Module>/`, shared by every screen of that module).
- Blazor never references the generated API client directly.
- Transformations (formatting, DTO ↔ view model mapping) live in the facade, not in the component.

### API client (validated — "customer detail" step)
- **Refit**, never Kiota, never a bare `HttpClient` inside a component. The interface lives in
  `Infrastructure/Api/Refit/<Module>/I<Module>Api.cs`, its transport contracts in `.../<Module>/Models/`,
  its registration in `Infrastructure/Api/RefitRegistration.cs`.
- Register through `AddRefitClient<T>(settings).ConfigureHttpClient(...)` so the client goes through
  `IHttpClientFactory`: it inherits Aspire service discovery and the ServiceDefaults resilience handler,
  and outgoing calls join the distributed trace.
- Methods return `ApiResponse<T>` (never a bare `T`): a `404`/`400` carries a **business outcome**,
  handled as data, not as an exception. The facade translates statuses into an outcome enum
  (`Found` / `InvalidPublicId` / `NotFound` / `Unavailable`) and degrades any transport failure to
  `Unavailable` — a screen must never surface an unhandled exception.
- **A deliberately selective approach**: the interface is hand-written while the consumed surface stays
  narrow; **Refitter** generation (`<module>.refitter` at the root of the web project,
  `refitter --settings-file ...`) takes over when it widens. The facade is the only boundary: switching
  from one to the other must touch no caller.
- Valid Refitter keys worth knowing: `multipleInterfaces` (not `generateMultipleInterfaces`),
  `optionalParameters` (not `generateOptionalParameters`), `operationNameTemplate` at the root level
  (the `naming` object only carries `useOpenApiTitle` and `interfaceName`). `useSystemTextJson`,
  `jsonSerializerOptions`, `typeForAdditionalProperties` and `generateResultTypes` do not exist in
  Refitter (they are NSwag settings): serialization is configured in the `RefitSettings` at registration.
- The format of a public identifier (`abc_` + 32 hex) is checked at the boundary
  (`Infrastructure/PublicIds/PublicIdFormat`): a precise message for the UI, and a pointless request
  avoided. The backend remains the authority on the format.

### Screen states (validated — "customer detail" step)
- A data screen exposes one branch per state: loading, empty/prompt, invalid, not found, unavailable
  (with a "Retry" action), success. Each state carries the right ARIA role (`role="status"` +
  `aria-live` for loading, `role="alert"` for errors).
- The component formats nothing: a `<Screen>Mapper.FromApi(dto)` produces a view model that is already
  presentable (a `—` placeholder for a missing value, a localized date, a boolean derived from the
  status). That mapper is unit-tested (nominal + edge cases), exactly like a backend assembler.
- A hand-written stub of the Refit interface is enough to test the facade (no mocking framework): an
  `ApiResponse<T>` is built with `new ApiResponse<T>(new HttpResponseMessage(status), content, new
  RefitSettings())`.

### Forms
- A primary action + a secondary action (where relevant) + a Cancel link.
- `disabled=isSubmitting` while submitting, an error message on network failure.
- `NavigationManager.NavigateTo` after success. Never expose technical identifiers in the form.

### Forms — validated rules ("create customer" step)
- The client-side validator mirrors the backend constraints (required, lengths, format) for immediate
  feedback, but the backend stays the authority: a rule that can only be decided server-side (email
  uniqueness) comes back as a business outcome (409) and is displayed as a form error, without leaving
  the page.
- `.Cascade(CascadeMode.Stop)` on a field chaining `NotEmpty` + format: otherwise an empty field shows
  both "required" AND "invalid format".
- The `FluentValidationValidator` validates the whole model on submit, but on an `OnFieldChanged` it
  replaces ONLY the messages of the field concerned (`messageStore.Clear(field)` then re-adding only
  that field's errors): clearing the whole store would surface errors on fields the user never touched,
  and clearing only the field while re-adding all errors would duplicate the other fields' messages.
- **CSS scope and child components**: `EditForm`, `InputText` and `ValidationMessage` render their own
  tags, which do NOT carry the page's scope attribute — so a `.form__input { ... }` rule in
  `Page.razor.css` never applies. Use `::deep` anchored on an element of the page
  (`.<page>__body ::deep .form__input`), or style globally in `app.css`. Check the actually computed
  style (field height, border) before considering a form finished.
- **A `201` response returning a bare identifier**: Refit returns the **raw** body for a `string` result
  (it does not go through the JSON serializer), so a `"cus_…"` body keeps its quotes. Unwrap them in the
  facade before any navigation, or they end up in the URL.

### CSS scope
- `Page.razor.css` is scoped per page. Design-system variables only (`--color-*`, `--space-*`,
  `--font-size-*`).

### Navigation
- On a list page, the primary action is a link in the header. A pure POCO model plus a separate
  validator.

### Cross-service degradation (validated — "customer orders" step)
- A backend that aggregates other services can answer `200` with a **partial success** (e.g.
  `ordersAvailable = false`). The screen must render that nuance: keep what is known (the customer's
  identity), show a `role="status"` banner (not `role="alert"`: the request itself did not fail)
  explaining WHICH service is missing, and offer "Retry". Never flatten this case into "unavailable" nor
  into "no data".
- The view model distinguishes the three cases explicitly: `OrdersAvailable=false` (degraded), `IsEmpty`
  (the service answered, there is nothing) and `HasOrders`. A single boolean is not enough.
- The facade **logs the partial success as `LogWarning`**: without it the degradation is invisible in
  production (the HTTP call is a 200).
- **HTTP client time budget**: a degradation path only answers once the backend's retry budget is spent.
  Measure that worst case, then configure the client to outlast it — the ServiceDefaults
  `AddStandardResilienceHandler` cuts at **10 s per attempt** by default, which kills the degraded
  response and turns it into "service unavailable". Replace the pipeline for that client
  (`RemoveAllResilienceHandlers` — an experimental API, scope the `EXTEXP0001` suppression — then
  `AddStandardResilienceHandler(options => …)`), and reduce the retries: replaying a 16 s aggregating
  call brings no new information.

### Hosting model and render modes (validated — "streaming" and "static SSR" steps)
- ShowRoom.Web is a **Blazor Web App** (the unified .NET 8+ model, single project, `blazor.web.js`).
  "Blazor hosted" no longer exists: it was the .NET 6/7 ASP.NET Core hosted WebAssembly template,
  replaced by render modes. "Blazor Hybrid" is a native shell (MAUI): out of scope here.
- The Refit client and the facade live **server-side**: the browser knows no API URL. This BFF property
  is a choice to defend — moving to `InteractiveWebAssembly`/`InteractiveAuto` requires a Client project,
  CORS, browser-side auth, and **makes the first load heavier**. WASM does not reduce load time: it
  shifts it to subsequent interactions.
- **The render mode is decided per screen.** `App.razor` sets NO `@rendermode`: the default is static
  SSR, and only the screens that need it declare `@rendermode InteractiveServer`. A read-only screen
  does not need a circuit.
- On a static screen, interactions go through the web: a **GET form** for a filter (the state lands in
  the query string, so the URL is shareable), and **links** for pagination and "Retry" actions. An
  unavailable pagination link is a `<span aria-disabled="true">`, never an inert `<a>`. Enhanced
  navigation avoids a full reload.
- **The shell (layout) must not be interactive**: an `@onclick` on the menu or the scroll-to-top button
  reopens a circuit on EVERY page and cancels the whole benefit. Those purely local behaviours belong in
  a JS module (`wwwroot/App/Layout/shell.js`) with listeners **delegated on `document`**, so they survive
  the DOM being replaced by enhanced navigation (`enhancedload`).
- Any screen whose rendering awaits a network call declares `@attribute [StreamRendering]`. Without it
  the HTML response is held back until the component completes: measured on a 6.3 s degraded path, TTFB
  was 6.36 s without streaming against 0.03 s with it. A skeleton is only worth anything if it is
  **sent** immediately: skeleton and streaming go together.
- Verify the result on the served HTML, not on intent: `curl`, then count the `"type":"server"` markers
  (0 = no interactive component, hence no circuit). And measure with
  `curl -w "TTFB=%{time_starttransfer} TOTAL=%{time_total}"`.

### Compression and HTTP caching (validated — "compression / caching" step)
- `UseResponseCompression` (Brotli + Gzip) applies to **dynamic** responses only. Do not add static
  assets to it: `MapStaticAssets` already serves them pre-compressed and fingerprinted.
- `EnableForHttps = true` is a choice to revisit the day authentication arrives: compressing under TLS
  reopens the BREACH class of attacks when a response mixes a secret with attacker-controlled input.
  Today these pages carry neither a secret nor a session token.
- **Streaming and compression are mutually exclusive**: a `[StreamRendering]` response goes out as
  `Content-Encoding: identity`. Arbitrate per page — streaming wins as soon as the data can be slow
  (TTFB 0.03 s instead of 6.36 s on a degraded path), compression wins on a fast and bulky page (−72% on
  the home page). Do not assume the two add up: verify it with `curl -H "Accept-Encoding: br" -D -`.
- A screen carrying personal business data emits **`no-store, no-cache, must-revalidate`**. Set it in a
  **path-based** middleware: a `[StreamRendering]` component has already flushed its headers by the time
  it runs.
- **Do not put an output cache on an SSR Razor Components page**: it emits an antiforgery `Set-Cookie`,
  so the output cache refuses to store it — and forcing it would hand one visitor's token to all the
  others. Measure before concluding there is a gain: a cache that never reaches a hit is misleading dead
  code, better deleted.

### Loading state (validated — "list skeleton" step)
- A data screen does not display "Loading…": it renders a **skeleton** reproducing the real structure
  (same columns, same row metrics), so that the arrival of data is a substitution rather than a layout
  jump.
- The skeleton carries `role="status"` + `aria-busy="true"` + `aria-live="polite"` + an explicit
  `aria-label`; its rows are `aria-hidden="true"` (decorative bars have nothing to announce).
- **Minimum display duration: only in interactive rendering.** When the skeleton is swapped client-side,
  a floor (~350 ms) avoids a flicker. Under **SSR + streaming** it is part of the first HTML flush and
  the browser coalesces paints: the floor no longer prevents anything and delays the full response by
  just as much (measured: 381 ms with, 52 ms without). Do not add it reflexively.
- The skeleton **announces** the arrival of a table, it does not have to equal its final height: sizing
  it on the page size cost +31% in weight (21.6 → 28.4 KB) on every request, for a fidelity that only
  matters on a slow load. A sober, constant number of rows is enough.
- Under static SSR, **an instance field does not survive from one request to the next**: every request
  instantiates a fresh component. Any "memory" between two renders (previous row count, screen state) is
  dead code there — verify before writing it.
- Peripheral elements that disappear during loading (pagination, counter) stay **rendered** and are
  hidden with `visibility: hidden` — never removed from the DOM: `display: none` frees their space and
  makes the content jump when data arrives.
- The shimmer is a `@keyframes` over a `linear-gradient` (`background-size: 200% 100%`), and the result
  panel arrives with a short fade. Both are **disabled** under `@media (prefers-reduced-motion: reduce)`.

### Paginated list (validated — "customer list" step)
- The list state (page, filters) lives in the **query string**, never in a private field:
  `[SupplyParameterFromQuery]` + `NavigationManager.NavigateTo("/x?page=2&search=…")`. The screen stays
  shareable by URL and the browser's Back button works. Do not serialize default values (`page=1`, empty
  search): the canonical URL stays clean.
- The component of a page named `Page.razor` is called `Page`: a `Page` parameter does not compile
  (CS0542). Name the property `PageNumber` and map the key with
  `[SupplyParameterFromQuery(Name = "page")]`.
- The facade **normalizes** page and pageSize (min/max bounds) before the call: a hand-edited query
  string must never produce a backend 400.
- The view model exposes **derived** navigation state (`HasPrevious`, `HasNext`, `FirstItemIndex`,
  `LastItemIndex`, `IsEmpty`) — the component writes no computation. Do not rely on the
  `hasPrevious`/`hasNext` fields returned by the API: re-derive them from the page numbers.
- A **contextual** empty state: "no items" and "no results for this filter" are two distinct messages.
- Tabular data means a real `<table>` (a `<caption>` in a visually-hidden class, `<th scope="col">`),
  wrapped in an `overflow-x: auto` container. On mobile, hide the secondary column(s) rather than forcing
  horizontal scrolling.
- Formatting shared by several screens of the same module (date, placeholder, status label) belongs in a
  single `<Module>Format`: a detail card and a list row must display the same data the same way.

### bUnit component tests (validated — "bUnit tests" step)
- **Stack**: `bunit` 2.x on **xUnit v3** + **AwesomeAssertions** (never FluentAssertions here:
  AwesomeAssertions is this repository's convention). The bUnit v2 base class is **`BunitContext`**
  (renamed from `TestContext`, which collided with xUnit v3's own `TestContext`), and rendering is done
  with `Render<TComponent>(p => p.Add(...))`.
- **One test project per front end** (`ShowRoom.Web.Tests`), whose tree mirrors the code
  (`Features/<Module>/<Screen>/`). No separate project for components.
- **No mocking framework**: injected services are doubled by hand (`StubCustomerFacade`), fed the outcome
  the facade would produce. The test then reads as "given the service answers X, the screen shows Y".
- **Stable, semantic selectors first**: `[role='alert']`, `[role='status']`, `nav[aria-label='…']`,
  `a[href='…']`, `table`/`th[scope='col']`, a field's `#id`. Those selectors describe what the user and
  assistive technologies perceive; a CSS class is used only when nothing semantic exists. Do not add a
  `data-testid` in place of a missing role: it is the role that must be added.
- **One test = one user scenario**: found / not found / invalid identifier / service unavailable /
  partial success. Cases that resemble each other must be told apart explicitly ("no orders" ≠ "Order
  service unreachable") — that is precisely what a test protects.
- **Loading states**: suspend the double with a `TaskCompletionSource` handed to the stub, assert the
  skeleton (`aria-busy`) and the reserved space, then release and await with
  `await cut.WaitForAssertionAsync(...)`.
- **The real validator is injected**, not simulated: its messages are observable behaviour.
- `[SupplyParameterFromQuery]` works under bUnit: navigate the `NavigationManager` (a fake is provided)
  to `"/screen?param=value"` BEFORE rendering. Navigation triggered by the component is asserted on
  `NavigationManager.Uri`.
- Do NOT test in bUnit what a unit test covers better (mapping, validation, facade): bUnit is for
  rendering and interactions.

### Front-end integration tests (validated — "disposable database" step)
- **Strict separation of the two levels, by project.** `ShowRoom.Web.Tests` (bUnit) knows NEITHER Docker,
  NOR `HttpClient`, NOR `WebApplicationFactory` — 1 s for the whole suite.
  `ShowRoom.Web.IntegrationTests` does NOT use bUnit. Mixing both in one project would produce a hybrid
  category: no longer a fast unit test, not yet a genuine end-to-end test.
- **Admission criterion for the integration suite**: the container must buy a signal the component tests
  cannot produce (the real route, a query string translated into SQL, round-trip serialization, pipeline
  headers). A client-side rejection or a status message is proven without a network: it stays in bUnit.
  Keep this suite **short** — a few critical scenarios, not a mirror of the component suite.
- bUnit tests double the facade: they NEVER see the real HTTP contract (route, query string,
  serialization). A second level is therefore required, otherwise a backend contract break leaves the
  suite green.
- A Blazor front end is an ASP.NET Core application: it is hosted with `WebApplicationFactory<TEntryPoint>`
  like an API. **Chaining the two hosts** is the clean way to test the full chain without a network: the
  front-end factory replaces the primary handler of its `HttpClient`s with `apiFactory.Server.CreateHandler()`,
  and the API factory carries its disposable PostgreSQL container (the `ShowRoom.Testing` harness).
  ```csharp
  services.ConfigureHttpClientDefaults(c => c.ConfigurePrimaryHttpMessageHandler(() => backendHandler));
  ```
- **Seed through the public API**, not through the DbContext: the data set goes through the real write
  path, and the test does not couple itself to the schema. It disappears with the container.
- **One fixture per test class** (`IClassFixture`) = one container per class. A test asserting "the
  database is empty" must be ALONE in its class: xUnit does not guarantee ordering within a class, and a
  seeding test would break it.
- This level is only usable because the read screens are in **static SSR**: the returned HTML contains
  the data, so it is assertable without a browser.
- **Decode the HTML before asserting** (`WebUtility.HtmlDecode`): Razor encodes entities, "é" arrives as
  `&#xE9;`, and an assertion on accented text would fail on an otherwise correct page.
- If the test project references several ASP.NET Core hosts, their `Program` classes (top-level
  statements) collide in the global namespace: disambiguate with an `<Aliases>` on the `ProjectReference`
  plus `extern alias`, rather than renaming anything.

### Playwright browser journeys (validated — "E2E" step)
- **This level only exists for what the others cannot see**: Blazor's interactive circuit and the shell
  JavaScript. Anything provable by rendering (bUnit) or over HTTP (integration) has no business here: a
  few critical journeys, not a mirror of the other suites.
- **Launch the applications as real processes**, from their own output directory, rather than
  `WebApplicationFactory` + Kestrel: in-process, the front end's static asset manifest is not copied into
  the test project's output, `blazor.web.js` and `app.css` answer 500, and the page stays frozen on its
  skeleton — exactly what Playwright is supposed to exercise. As processes, the content root, wwwroot,
  manifests and configuration are those of a deployment.
- **Override configuration through environment variables**: they win over `appsettings.Development.json`,
  unlike host configuration. Keep the `Development` environment — it is what wires the static assets the
  way the browser expects them.
- **Wait for the circuit before interacting** (`page.WaitForWebSocketAsync()` before navigation): on an
  `InteractiveServer` screen the page is first prerendered statically, and input sent too early is
  **lost**, not deferred.
- **An open WebSocket is not enough**: between the connection and the attachment of event handlers, the
  very first event can still be lost. Neutralise it with a **retried** probe until an observable reaction
  occurs — never a fixed delay, which will be either too short or wasted time.
- **That probe belongs to the navigation, not to the fixture.** Paying it once at fixture start only
  protects the first circuit: every navigation opens a new one and replays the same race. Measured on
  the creation journey, a fixture-only warm-up failed 1 run in 3; moving the probe into the navigation
  helper (`GotoCreateCustomerAsync`) took it to 4 green runs in a row. What is still worth paying once
  in the fixture is the server-side JIT of the interactive path.
- **A Blazor navigation produces no `load` event**: `WaitForURLAsync` (which waits for one by default)
  times out. Assert the URL with `Expect(page).ToHaveURLAsync(...)`, which retries.
- Widen the assertion timeout (`Assertions.SetDefaultExpectTimeout`): 5 s by default is too short for a
  first render paying for JIT and the EF model, and would produce intermittent failures rather than real
  regressions.
- **Verify stability before committing**: run the suite several times in a row. An intermittent E2E test
  costs more than no test at all.

### Swiss design system (validated — "home page" step)
- A single definition point for tokens: `wwwroot/app.css` (`--color-*`, `--space-*`, `--font-size-*`,
  `--line-height-*`, `--transition-*`, `--container-*`, `--max-col-text`). No raw value (hex, colour px,
  font family) in a `*.razor.css`: only `var(--token)`.
- Swiss style: Helvetica Neue, a spacing scale in multiples of 8px, flat areas and 1px rules (`.rule`),
  near-zero radii (2px max), section labels in spaced uppercase (`.section-label`), no CSS framework (no
  Bootstrap).
- Composition of an editorial page: sections on a `3fr 9fr` grid (label / body) separated by `.rule`
  lines, titles at `--font-size-xl`/`2xl`, secondary text in `--color-muted` bounded by `--max-col-text`.
- Diagrams are **inline SVG** in the page (never a binary image): they consume the same `var(--color-*)`,
  stay sharp at any density, and carry `role="img"` + an `aria-label` describing the diagram.
- JS assets of the technical shell go in `wwwroot/App/<Zone>/<name>.js` (an ES module imported through
  `IJSRuntime`), those of a feature in `wwwroot/<Feature>/...`. Always unsubscribe listeners in
  `DisposeAsync`.
- An ARIA attribute driven by a boolean: write `aria-expanded="@(isOpen ? "true" : "false")"`. An
  `aria-expanded="@isOpen"` renders an empty attribute (Blazor's boolean attribute semantics): the value
  `"true"` never exists, which breaks both accessibility and `[aria-expanded="true"]` selectors.
- Mobile menu states: `.menu-toggle` >= 44x44px, a clickable backdrop, closing on a link click.

### Shell placement, guarded (validated — "blank page" regression)
- `_Imports.razor` files are **hierarchical**: moving a component to another folder changes the
  `@using` it inherits, hence the components it can resolve. And Razor **does not report an unknown
  component tag** — it copies it out as literal HTML. Dragging `App.razor` out of `App/` therefore turns
  `<Routes />` into an inert tag: the application compiles, starts, and serves a blank page with no error
  anywhere (no build warning, no console message, no log).
- The compile error such a move does produce (`MapRazorComponents<App>()` no longer resolving) is a
  **decoy**: adding the missing `@using` makes it build and does not make it work. Never silence that
  error without asking why the type moved.
- Pin the placement with a test that names the types (`ShowRoom.Web.Tests/Shell/ShellPlacementTests`):
  `typeof(global::ShowRoom.Web.App.App)` and friends turn a move into a **test-project compilation
  failure**, long before anything renders. Complete it with the reverse direction — every routable
  component (`RouteAttribute`) lives under `Features/`, the shell making only its `Error`/`NotFound`
  surfaces routable.

### API errors as ProblemDetails (validated — "ProblemDetails" step)
- A failed response is **read, not discarded**. `Infrastructure/Api/Problems/` holds an `ApiProblem`
  (status, title, detail, `traceId`, errors) and an `ApiProblemReader` that normalises the **two shapes**
  ShowRoom APIs emit: a validation failure keys `errors` by code (`{"Validation.Email": ["…"]}`), every
  other failure lists them (`[{code, message, category}]`). A screen must never have to know which one
  it got.
- **The reader never throws.** An error body is exactly where the unexpected arrives — an empty body, an
  HTML page from a reverse proxy, a truncated payload. Every parse failure degrades to the bare status;
  a parser blowing up while handling an error would turn a diagnosable failure into an opaque one.
- **The code is the contract, the message is a hint.** The API answers in its own language: rendering
  its `detail` into the UI would put a foreign, backend-worded sentence in front of the user and couple
  the screen to a string the backend is free to reword. Resolve the user-facing sentence from the CODE
  (`<Module>ProblemMessages`), and keep the server text for the diagnostic panel. Exception: on a form
  FIELD, an unmapped server message still beats a generic one — it says what to fix.
- Where the screen knows more than the code table, the screen wins: a 409 on a creation form says
  *which* address is taken, because it holds what the user typed.
- **`Validation.<PropertyName>` maps straight onto form fields.** The backend codes FluentValidation
  failures that way over command properties that carry the form's own names, so server-side validation
  lands on the field it concerns instead of in a vague banner — push it into a **second**
  `ValidationMessageStore`, next to the client-side one.
- **Clear that store on `OnValidationRequested` and per field on `OnFieldChanged`.**
  `EditContext.Validate()` returns false while ANY store holds a message: a stale server error would
  silently veto every later submit — the form would refuse to send and show nothing new. Lock it with a
  test that submits twice.
- The diagnostic block (`Shared/Components/ApiProblemPanel`) is a collapsed `<details>` carrying status,
  server message, codes and **`traceId`** — the field that ties a user-visible failure back to its
  distributed trace. It renders nothing when the response carried no body, so a transport failure shows
  no empty shell.
- Prove the SHAPE against the real API, not against a body the test wrote itself: a contract test in the
  integration suite (`ProblemDetailsContractTests`) asserts the codes, the two `errors` shapes and the
  presence of `traceId`. Unit tests prove the reader parses what we *believe* is returned; only a real
  host proves what *is*.
