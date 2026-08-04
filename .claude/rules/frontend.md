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

### 4.2 Responsive — non négociable ⚠️
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

### 10.3 Header et footer — pattern `__inner`
Le `<header>` et le `<footer>` s'étendent sur toute la largeur de page (fond, bordure).  
Leur **contenu** doit être enveloppé dans un `__inner` qui porte l'alignement horizontal :

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

- Ne jamais mettre `padding-left/right` directement sur `.site-header` ou `.site-footer`.
- Le `__inner` hérite automatiquement des réductions de `--container-pad` via les media queries globales.
- `position: relative` (nécessaire au menu dropdown mobile) doit être posé sur `__inner`, pas sur l'élément `<header>`.

### 10.4 Overflow horizontal
- Utiliser `overflow-x: clip` sur `.layout`, **jamais** `overflow-x: hidden` sur `body`.  
  `hidden` sur `body` crée un nouveau contexte de défilement qui clippe les éléments `position: fixed` (scroll-to-top, menus).  
  `clip` sur `.layout` bloque le débordement sans créer ce contexte.

### 10.5 Débordement du contenu dans les cartes
Tout texte long ou code (ex. `PublicId`, titres) doit être contraint :
- Titres : `overflow-wrap: break-word`
- Codes/identifiants : `word-break: break-all` + `max-width: 100%`

### 10.6 Conséquence
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
  /Editorial
    /PublishArticle
      Page.razor
      PublishArticleForm.razor
      PublishArticleCommand.cs
      PublishArticleFacade.cs
    /ArticleList
      Page.razor
      ArticleCard.razor
      GetArticlesQuery.cs
      EditorialFacade.cs

  /Identity
    /Login
      Page.razor
      LoginForm.razor
      LoginFacade.cs

  /Acquisition
    /SubmitRequest
      Page.razor
      SubmitRequestForm.razor
      AcquisitionFacade.cs

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
- Kiota-generated API clients and models must live under `Infrastructure/Api/Generated`, with manual registration/configuration kept in `Infrastructure/Api`.

---

## Regles validees - Editorial-01

### Validation Blazor
- FluentValidation (AbstractValidator<T>), jamais DataAnnotations sur les modeles de formulaire.
- Composant FluentValidationValidator (Infrastructure/Validation/) - pattern de reference pour .NET 8+.
- AddValidatorsFromAssemblyContaining<Program>(ServiceLifetime.Scoped) dans Program.cs.
- Messages d'erreur en francais, orientes utilisateur final.

### Facade
- Un seul <Module>Facade par module, injecte en Scoped.
- Blazor ne reference jamais ShowRoomBusinessApiClient directement.
- Transformations (slug, tags) dans la Facade, pas dans le composant.

### Formulaire auteur
- Action principale (Publier, published) + action secondaire (Brouillon, draft) + lien Annuler.
- disabled=isSubmitting pendant soumission, message erreur si echec reseau.
- NavigationManager.NavigateTo apres succes. Jamais d'identifiants techniques dans le formulaire.

### Slug
- Auto-genere depuis le titre dans la Facade (GenerateSlug), jamais expose a l'auteur.
- NFC -> sans diacritiques -> lowercase -> [a-z0-9-] -> espaces en tirets -> trim tirets.

### CSS scope
- Page.razor.css scope isole par page. Variables design system uniquement (--color-*, --space-*, --font-size-*).

### Navigation
- Page liste : lien action principal dans le header. Modele POCO pur + validateur separe.
