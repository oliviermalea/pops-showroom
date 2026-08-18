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

## Conventions frontend Blazor (validees)

### Validation Blazor
- FluentValidation (AbstractValidator<T>), jamais DataAnnotations sur les modeles de formulaire.
- Composant FluentValidationValidator (Infrastructure/Validation/) - pattern de reference pour .NET 8+.
- AddValidatorsFromAssemblyContaining<Program>(ServiceLifetime.Scoped) dans Program.cs.
- Messages d'erreur en francais, orientes utilisateur final.

### Facade
- Un seul <Module>Facade par module, injecte en Scoped, derriere une interface `I<Module>Facade`
  (placee a la racine de `Features/<Module>/`, partagee par tous les ecrans du module).
- Blazor ne reference jamais le client d'API genere directement.
- Transformations (formatage, mapping DTO <-> view model) dans la Facade, pas dans le composant.

### Client d'API (valide — etape « detail client »)
- **Refit**, jamais Kiota, jamais un `HttpClient` nu dans un composant. L'interface vit dans
  `Infrastructure/Api/Refit/<Module>/I<Module>Api.cs`, ses contrats de transport dans
  `.../<Module>/Models/`, l'enregistrement dans `Infrastructure/Api/RefitRegistration.cs`.
- Enregistrer via `AddRefitClient<T>(settings).ConfigureHttpClient(...)` pour passer par
  `IHttpClientFactory` : on herite du service discovery Aspire et du handler de resilience de
  ServiceDefaults, et les appels sortants rejoignent la trace distribuee.
- Les methodes renvoient `ApiResponse<T>` (jamais `T` nu) : un `404`/`400` porte une **issue metier**,
  traitee comme une donnee, pas comme une exception. La facade traduit les statuts en enum d'issue
  (`Found` / `InvalidPublicId` / `NotFound` / `Unavailable`) et degrade toute panne transport en
  `Unavailable` — l'ecran ne doit jamais remonter une exception non geree.
- **Approche selective assumee** : interface ecrite a la main tant que la surface consommee est
  etroite ; generation **Refitter** (`<module>.refitter` a la racine du projet web,
  `refitter --settings-file ...`) quand elle s'elargit. La facade est la seule frontiere : passer de
  l'un a l'autre ne doit toucher aucun appelant.
- Cles Refitter valides a connaitre : `multipleInterfaces` (et non `generateMultipleInterfaces`),
  `optionalParameters` (et non `generateOptionalParameters`), `operationNameTemplate` au niveau racine
  (l'objet `naming` ne porte que `useOpenApiTitle` et `interfaceName`). `useSystemTextJson`,
  `jsonSerializerOptions`, `typeForAdditionalProperties`, `generateResultTypes` n'existent pas cote
  Refitter (ce sont des reglages NSwag) : la serialisation se configure dans les `RefitSettings` a
  l'enregistrement.
- Le format d'un identifiant public (`abc_` + 32 hex) est verifie au boundary avant l'appel
  (`Infrastructure/PublicIds/PublicIdFormat`) : message precis cote UI et requete inutile evitee.
  L'autorite sur le format reste le backend.

### Etats d'ecran (valide — etape « detail client »)
- Un ecran de donnees expose une branche par etat : chargement, vide/invite, invalide, introuvable,
  indisponible (avec action « Reessayer »), succes. Chaque etat porte le role ARIA adapte
  (`role="status"` + `aria-live` pour le chargement, `role="alert"` pour les erreurs).
- Le composant ne formate rien : un `<Ecran>Mapper.FromApi(dto)` produit un view model deja
  presentable (placeholder `—` pour une valeur absente, date localisee, booleen derive du statut).
  Ce mapper est teste unitairement (nominal + cas limites), au meme titre qu'un assembleur backend.
- Un stub ecrit a la main de l'interface Refit suffit a tester la facade (aucun framework de mock) :
  un `ApiResponse<T>` se construit avec `new ApiResponse<T>(new HttpResponseMessage(status), content,
  new RefitSettings())`.

### Formulaire
- Action principale + action secondaire (le cas echeant) + lien Annuler.
- disabled=isSubmitting pendant soumission, message erreur si echec reseau.
- NavigationManager.NavigateTo apres succes. Jamais d'identifiants techniques dans le formulaire.

### Formulaire — regles validees (etape « creation client »)
- Le validateur client reproduit les contraintes du backend (obligatoire, longueurs, format) pour un
  retour immediat, mais le backend reste l'autorite : une regle qui ne peut etre tranchee que cote
  serveur (unicite d'un email) revient en issue metier (409) et s'affiche en erreur de formulaire,
  sans quitter la page.
- `.Cascade(CascadeMode.Stop)` sur un champ enchainant `NotEmpty` + format : sinon un champ vide
  affiche « obligatoire » ET « format invalide ».
- Le `FluentValidationValidator` valide tout le modele a la soumission, mais sur un `OnFieldChanged`
  il ne remplace QUE les messages du champ concerne (`messageStore.Clear(field)` puis re-ajout des
  seules erreurs de ce champ) : vider tout le store ferait apparaitre des erreurs sur des champs
  jamais touches, et ne clearer que le champ en re-ajoutant toutes les erreurs duplique les messages
  des autres champs.
- **CSS scope et composants enfants** : `EditForm`, `InputText`, `ValidationMessage` rendent leur
  propre balise, qui ne porte PAS l'attribut de scope de la page — un `.form__input { ... }` dans
  `Page.razor.css` ne s'applique donc jamais. Passer par `::deep` ancre sur un element de la page
  (`.<page>__body ::deep .form__input`), ou styler globalement dans `app.css`. Verifier le style
  reellement calcule (hauteur de champ, bordure) avant de considerer un formulaire termine.
- **Reponse `201` renvoyant un identifiant nu** : Refit renvoie le corps **brut** pour un resultat
  `string` (il ne passe pas par le serialiseur JSON), donc un corps `"cus_…"` conserve ses guillemets.
  Les desencadrer dans la facade avant toute navigation, sinon ils finissent dans l'URL.

### CSS scope
- Page.razor.css scope isole par page. Variables design system uniquement (--color-*, --space-*, --font-size-*).

### Navigation
- Page liste : lien action principal dans le header. Modele POCO pur + validateur separe.

### Degradation cross-service (valide — etape « commandes du client »)
- Un backend qui agrege d'autres services peut repondre `200` avec un **succes partiel** (ex.
  `ordersAvailable = false`). L'ecran doit rendre cette nuance : garder ce qui est connu (l'identite du
  client), afficher une banniere `role="status"` (pas `role="alert"` : ce n'est pas une erreur de la
  requete) expliquant QUEL service manque, et proposer « Reessayer ». Ne jamais aplatir ce cas en
  « indisponible » ni en « aucune donnee ».
- Le view model distingue explicitement les trois cas : `OrdersAvailable=false` (degrade),
  `IsEmpty` (le service a repondu, il n'y a rien), `HasOrders`. Un booleen unique ne suffit pas.
- La facade **loggue le succes partiel en `LogWarning`** : sans cela la degradation est invisible en
  production (le HTTP est un 200).
- **Budget de temps du client HTTP** : un chemin de degradation ne repond qu'une fois le budget de
  retries du backend epuise. Mesurer ce pire cas, puis configurer le client pour le depasser —
  `AddStandardResilienceHandler` de ServiceDefaults coupe par defaut a **10 s par tentative**, ce qui
  tue la reponse degradee et la transforme en « service indisponible ». Remplacer le pipeline pour ce
  client (`RemoveAllResilienceHandlers` — API experimentale, suppression `EXTEXP0001` a scoper — puis
  `AddStandardResilienceHandler(options => …)`), et reduire les retries : re-jouer un appel agregeant
  de 16 s n'apporte aucune information.

### Modele d'hebergement et render modes (valide — etapes « streaming » et « SSR statique »)
- ShowRoom.Web est une **Blazor Web App** (modele unifie .NET 8+, projet unique, `blazor.web.js`).
  « Blazor hosted » n'existe plus : c'etait le template ASP.NET Core hosted WebAssembly de .NET 6/7,
  remplace par les render modes. « Blazor Hybrid » est une coquille native (MAUI) : hors sujet ici.
- Le client Refit et la facade vivent **cote serveur** : le navigateur ne connait aucune URL d'API. Cette
  propriete BFF est un choix a defendre — passer en `InteractiveWebAssembly`/`InteractiveAuto` impose un
  projet Client, du CORS, de l'auth cote navigateur, et **alourdit le premier chargement**. WASM ne
  reduit pas le temps de chargement : il le deplace vers les interactions suivantes.
- **Le render mode se decide par ecran.** `App.razor` ne pose AUCUN `@rendermode` : le defaut est le SSR
  statique, et seuls les ecrans qui en ont besoin declarent `@rendermode InteractiveServer`. Un ecran de
  lecture n'a pas besoin de circuit.
- Sur un ecran statique, les interactions passent par le web : **formulaire GET** pour un filtre (l'etat
  atterrit dans la query string, donc URL partageable), **liens** pour la pagination et les actions
  « Reessayer ». Un lien de pagination indisponible est un `<span aria-disabled="true">`, jamais un `<a>`
  inerte. La navigation enrichie evite le rechargement complet.
- **La coquille (layout) ne doit pas etre interactive** : un `@onclick` sur le menu ou le scroll-top
  rouvre un circuit sur CHAQUE page et annule tout le benefice. Ces comportements purement locaux vont
  dans un module JS (`wwwroot/App/Layout/shell.js`) avec des ecouteurs **delegues sur `document`**, pour
  survivre au remplacement du DOM par la navigation enrichie (`enhancedload`).
- Tout ecran dont le rendu attend un appel reseau declare `@attribute [StreamRendering]`. Sans lui, la
  reponse HTML est retenue jusqu'a la fin du composant : mesure sur un chemin degrade a 6,3 s → TTFB
  6,36 s sans streaming contre 0,03 s avec. Le squelette n'a de valeur que s'il est **envoye**
  immediatement : squelette et streaming vont ensemble.
- Verifier le resultat sur le HTML servi, pas sur une intention : `curl` puis compter les marqueurs
  `"type":"server"` (0 = aucun composant interactif, donc aucun circuit). Et mesurer avec
  `curl -w "TTFB=%{time_starttransfer} TOTAL=%{time_total}"`.

### Compression et cache HTTP (valide — etape « compression / cache »)
- `UseResponseCompression` (Brotli + Gzip) sur les reponses **dynamiques** uniquement. Ne pas y ajouter
  les assets statiques : `MapStaticAssets` les sert deja pre-compresses et empreintes.
- `EnableForHttps = true` est un choix a re-examiner le jour ou une authentification arrive : compresser
  sous TLS reouvre la classe BREACH quand une reponse melange un secret et une entree controlee par
  l'attaquant. Aujourd'hui ces pages ne portent ni secret ni jeton de session.
- **Streaming et compression s'excluent** : une reponse `[StreamRendering]` sort en
  `Content-Encoding: identity`. Arbitrer par page — le streaming gagne des que la donnee peut etre lente
  (TTFB 0,03 s au lieu de 6,36 s sur un chemin degrade), la compression gagne sur une page rapide et
  volumineuse (−72 % sur l'accueil). Ne pas supposer que les deux s'additionnent : le verifier avec
  `curl -H "Accept-Encoding: br" -D -`.
- Un ecran portant des donnees metier personnelles emet **`no-store, no-cache, must-revalidate`**. Le
  poser dans un middleware **par chemin** : un composant `[StreamRendering]` a deja vide ses en-tetes
  quand il s'execute.
- **Ne pas mettre d'output cache sur une page Razor Components SSR** : elle emet un `Set-Cookie`
  antiforgery, l'output cache refuse donc de la stocker — et le forcer distribuerait le jeton d'un
  visiteur a tous les autres. Mesurer avant de conclure a un gain : un cache qui n'atteint jamais son
  cache-hit est du code mort trompeur, il vaut mieux le supprimer.

### Etat de chargement (valide — etape « squelette de liste »)
- Un ecran de donnees n'affiche pas « Chargement… » : il rend un **squelette** qui reproduit la
  structure reelle (memes colonnes, memes metriques de ligne), pour que l'arrivee des donnees soit une
  substitution et non un saut de mise en page.
- Le squelette porte `role="status"` + `aria-busy="true"` + `aria-live="polite"` + un `aria-label`
  explicite ; ses lignes sont `aria-hidden="true"` (des barres decoratives n'ont rien a annoncer).
- **Duree minimale d'affichage : uniquement en rendu interactif.** Quand le squelette est bascule
  cote client, un plancher (~350 ms) evite un clignotement. En **SSR + streaming**, il fait partie du
  premier flush HTML et le navigateur coalesce les peintures : le plancher n'evite plus rien et
  retarde la reponse complete d'autant (mesure : 381 ms avec, 52 ms sans). Ne pas le poser par reflexe.
- Le squelette **annonce** l'arrivee d'un tableau, il n'egale pas sa hauteur finale : le calquer sur la
  taille de page a coute +31 % de poids (21,6 → 28,4 Ko) a chaque requete, pour une fidelite utile
  seulement sur un chargement lent. Un nombre de lignes sobre et constant suffit.
- En SSR statique, **un champ d'instance ne survit pas d'une requete a l'autre** : chaque requete
  instancie un composant neuf. Toute « memoire » entre deux rendus (nombre de lignes precedent, etat
  d'ecran) y est du code mort — la verifier avant de l'ecrire.
- Les elements peripheriques qui disparaissent pendant le chargement (pagination, compteur) restent
  **rendus** et sont masques en `visibility: hidden` — jamais retires du DOM : `display: none` libere
  leur place et fait sauter le contenu au retour des donnees.
- Le scintillement est un `@keyframes` sur un `linear-gradient` (`background-size: 200% 100%`), et le
  panneau de resultat arrive en fondu court. Les deux sont **desactives** sous
  `@media (prefers-reduced-motion: reduce)`.

### Liste paginee (valide — etape « liste clients »)
- L'etat de la liste (page, filtres) vit dans la **query string**, jamais dans un champ prive :
  `[SupplyParameterFromQuery]` + `NavigationManager.NavigateTo("/x?page=2&search=…")`. L'ecran reste
  partageable par URL et le bouton Retour du navigateur fonctionne. Ne pas serialiser les valeurs par
  defaut (`page=1`, recherche vide) : l'URL canonique reste propre.
- Le composant d'une page nommee `Page.razor` s'appelle `Page` : un parametre `Page` ne compile pas
  (CS0542). Nommer la propriete `PageNumber` et mapper la cle via
  `[SupplyParameterFromQuery(Name = "page")]`.
- La facade **normalise** page et pageSize (bornes min/max) avant l'appel : une query string editee a
  la main ne doit jamais produire un 400 backend.
- Le view model expose l'etat de navigation **derive** (`HasPrevious`, `HasNext`, `FirstItemIndex`,
  `LastItemIndex`, `IsEmpty`) — le composant n'ecrit aucun calcul. Ne pas se fier aux champs
  `hasPrevious`/`hasNext` renvoyes par l'API : les rederiver des numeros de page.
- Etat vide **contextuel** : « aucun element » et « aucun resultat pour tel filtre » sont deux
  messages distincts.
- Donnees tabulaires = vraie `<table>` (`<caption>` en classe visually-hidden, `<th scope="col">`),
  enveloppee dans un conteneur `overflow-x: auto`. En mobile, masquer la ou les colonnes secondaires
  plutot que d'imposer un defilement horizontal.
- Le formatage partage par plusieurs ecrans d'un meme module (date, placeholder, libelle de statut)
  va dans un `<Module>Format` unique : une fiche et une ligne de liste doivent afficher la meme
  donnee de la meme facon.

### Design system swiss (valide — etape « page d'accueil »)
- Un seul point de definition des tokens : `wwwroot/app.css` (`--color-*`, `--space-*`, `--font-size-*`,
  `--line-height-*`, `--transition-*`, `--container-*`, `--max-col-text`). Aucune valeur brute (hex, px
  de couleur, famille de police) dans un `*.razor.css` : uniquement `var(--token)`.
- Style suisse : Helvetica Neue, echelle d'espacement en multiples de 8px, aplats et filets 1px
  (`.rule`), rayons quasi nuls (2px max), libelles de section en capitales espacees (`.section-label`),
  aucun framework CSS (pas de Bootstrap).
- Composition d'une page editoriale : sections en grille `3fr 9fr` (label / corps) separees par des
  filets `.rule`, titres en `--font-size-xl`/`2xl`, textes secondaires en `--color-muted` bornes a
  `--max-col-text`.
- Les diagrammes sont du **SVG inline** dans la page (pas d'image binaire) : ils consomment les memes
  `var(--color-*)`, restent nets a toute densite et portent `role="img"` + `aria-label` decrivant le
  schema.
- Les assets JS de la coquille technique vont dans `wwwroot/App/<Zone>/<nom>.js` (module ES importe
  par `IJSRuntime`), ceux d'une feature dans `wwwroot/<Feature>/...`. Toujours desabonner les listeners
  dans `DisposeAsync`.
- Attribut ARIA pilote par un booleen : ecrire `aria-expanded="@(isOpen ? "true" : "false")"`. Un
  `aria-expanded="@isOpen"` rend un attribut vide (semantique booleenne Blazor) : la valeur `"true"`
  n'existe jamais, ce qui casse a la fois l'accessibilite et les selecteurs
  `[aria-expanded="true"]`.
- Etats de menu mobile : `.menu-toggle` >= 44x44px, backdrop cliquable, fermeture au clic sur un lien.
