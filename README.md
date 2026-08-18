# ShowRoom

> POC .NET 10 démontrant une architecture **modulith → distribuée** : bounded contexts isolés,
> communication **machine-to-machine par messaging AMQP** (jamais HTTP), orchestration **.NET Aspire**
> et observabilité **OpenTelemetry** de bout en bout.

Le scénario métier fil rouge : **un client consulte son profil, l'historique de ses commandes et le
détail d'un produit** — chaque capacité vivant dans un bounded context distinct.

> ℹ️ **Maintenance** — ce README est la documentation racine unique du dépôt. Il doit être **tenu à
> jour en continu** : toute évolution de topologie, de service ou de contrat d'API doit s'y refléter
> (voir `.claude/rules/global.md` §3).

---

## Sommaire

- [Topologie](#topologie)
- [Stack technique](#stack-technique)
- [Principes d'architecture](#principes-darchitecture)
- [Structure de la solution](#structure-de-la-solution)
- [Démarrage](#démarrage)
- [APIs disponibles](#apis-disponibles)
- [Front Blazor (ShowRoom.Web)](#front-blazor-showroomweb)
- [Messaging & observabilité](#messaging--observabilité)
- [Tests](#tests)

---

## Topologie

Le système est **distribué** : le bounded context *Customer* est extrait dans son propre service, qui
dialogue avec le service *Business* (Order + Product) **uniquement via RabbitMQ** (request/reply
Wolverine). Les services partagent **une seule base PostgreSQL** (`showroom`) ; l'isolation entre
modules est assurée par **un schéma dédié par module** (`customers`, `orders`, `products`) — séparation
logique, pas physique. Un schéma `wolverine` supplémentaire héberge le **message store** de l'outbox
transactionnel (tables gérées par Wolverine, à côté du schéma métier — pas un second DbContext).

Deux styles de messagerie coexistent :
- **request/reply synchrone** (`IMessageBus.InvokeAsync`) pour lire les données d'un autre module
  (`GetOrdersForCustomer`) — sans garantie de livraison, avec dégradation gracieuse ;
- **publish/subscribe garanti** via l'**outbox transactionnel** pour les *IntegrationEvents*
  (`CustomerRegisteredIntegrationEvent`) — livraison au moins une fois, l'enveloppe étant persistée dans
  la **même transaction** que le changement métier (même DbContext, même connexion) puis délivrée à
  RabbitMQ avec retries.

```mermaid
flowchart LR
    client([Client HTTP])
    web["ShowRoom.Web<br/>(front Blazor — accueil)"]

    subgraph customer[ShowRoom.Customer.Api]
        cust[Module Customer]
    end
    subgraph business[ShowRoom.Business.Api]
        ord[Module Order]
        prod[Module Product]
    end

    rabbit[[RabbitMQ<br/>messaging]]
    db[("showroom<br/>(schémas: customers / orders / products / wolverine / wolverine_business)")]

    client -->|HTTP /customers| cust
    client -->|HTTP /orders, /products| ord

    web -->|HTTP /customers/{publicId}| cust
    web -.->|HTTP (à venir)| ord

    cust -->|"InvokeAsync(GetOrdersForCustomer)"| rabbit
    rabbit -->|GetOrdersForCustomer| ord
    ord -.->|OrdersForCustomerResponse| rabbit
    rabbit -.->|reply| cust

    cust -->|"outbox: CustomerRegistered (livraison garantie)"| rabbit
    rabbit -->|CustomerRegistered| ord

    cust ---|schémas customers + wolverine| db
    ord ---|schémas orders + wolverine_business| db
    prod ---|schéma products| db
```

- **`ShowRoom.Customer.Api`** — héberge le module Customer. **Producteur** : `GET
  /customers/{id}/with-orders` émet `GetOrdersForCustomer` sur le bus et attend la réponse ; `POST
  /customers` publie `CustomerRegisteredIntegrationEvent` via l'**outbox transactionnel** (livraison
  garantie).
- **`ShowRoom.Business.Api`** — héberge Order + Product. **Consommateur** : répond à
  `GetOrdersForCustomer` depuis sa base (sans jamais exposer d'appel HTTP interne) et **consomme**
  `CustomerRegisteredIntegrationEvent` (inbox durable + politique retry/dead-letter, voir plus bas).
- Comme le handler Order vit dans un **autre process**, la requête traverse réellement le broker →
  trace distribuée complète et fiable, sans couplage HTTP entre services.
- Dégradation gracieuse : si le service Order / le broker est indisponible, le client est tout de même
  renvoyé avec `ordersAvailable = false` et une liste de commandes vide.
- **`ShowRoom.Web`** — front Blazor (Server interactive) orchestré par Aspire. Il consomme le service
  Customer en **HTTP** (client typé Refit) : le front est le monde extérieur qui appelle le système, le
  HTTP y est donc légitime — la règle « M2M = AMQP » ne concerne que les échanges entre services. Voir
  [Front Blazor](#front-blazor-showroomweb).

---

## Stack technique

| Domaine | Choix |
|---|---|
| Runtime | .NET 10, C# / Minimal APIs (aucun MVC) |
| Front | Blazor Web App (render mode Server interactif), design system « swiss » maison (CSS variables, aucun framework CSS) |
| Client d'API front | **Refit** (interfaces typées) sur `IHttpClientFactory` — approche sélective : écrit à la main tant que la surface est étroite, générable par **Refitter** quand elle s'élargit |
| Orchestration | .NET Aspire (AppHost + ServiceDefaults) |
| Persistance | EF Core 10 + PostgreSQL (une base `showroom`, un schéma par module) |
| Messaging M2M | RabbitMQ via **Wolverine** — request/reply (`IMessageBus.InvokeAsync`) + **outbox transactionnel** (producteur, DbContext unique) + **inbox durable & retry/dead-letter** (consommateur) ; message stores PostgreSQL par service (`wolverine`, `wolverine_business`) |
| Observabilité | OpenTelemetry (traces + métriques, export OTLP), Serilog (logs structurés) |
| Identifiants | `StronglyTypedId` (Meziantou) en interne, `PublicId` (`prefix_guid`) exposé en HTTP |
| Résultats | `Result`/`Error` + `ErrorCategory` (SmartEnum) → ProblemDetails, plutôt que des exceptions |
| Erreurs HTTP | RFC 7807 uniforme ; corps JSON malformé → `400` propre (`BadRequestExceptionHandler` + `UseExceptionHandler`), jamais de stack trace |
| Validation | FluentValidation ; devise = `Currency` **SmartEnum** ISO 4217 (`Currency.IsValidCode` au boundary, `Currency.FromCode` au domaine) |
| DI | Scrutor (scan des handlers) |
| Versioning API | Asp.Versioning (`/api/v{version}`) |
| Feature flags | Microsoft.FeatureManagement (`FeatureManagement:<Module>`) |
| Doc API | OpenAPI + Scalar (UI en développement) |
| Tests | xUnit v3, Testcontainers (PostgreSQL / RabbitMQ), ArchUnitNET, AwesomeAssertions, Bogus |

---

## Principes d'architecture

Les règles complètes font foi dans [`.claude/rules/`](.claude/rules) ; en résumé :

- **Bounded context isolé** par module (`Domain` pur ← `Persistence` ← `Features`), organisé en
  **Vertical Slice + REPR** (Request · Endpoint · Handler · Response · Assembler).
- **Pas de repository** : les handlers utilisent directement le `DbContext` du module (unité de
  travail).
- **M2M = AMQP, jamais HTTP** : un module lit les données d'un autre uniquement via son contrat de
  message `*.Contracts` sur le bus. Le HTTP est réservé au monde extérieur qui appelle le système.
- **Dual-ID** : `StronglyTypedId` technique interne + `PublicId` (préfixé, ex. `cus_`, `ord_`, `prd_`)
  seul exposé en HTTP.
- **Statuts** low-cardinality en ValueObject / SmartEnum, jamais en `string`/`int` bruts.
- **Observabilité obligatoire** : logs structurés (`BeginModuleScope`), spans enrichis
  (`SetCommonTags`), propagation du contexte de trace à travers le broker.
- **Tests obligatoires** : endpoints + assembleurs prioritaires, tests d'architecture (ArchUnit)
  gardiens des frontières.

---

## Structure de la solution

```text
Pops-ShowRoom.slnx
├─ src/
│  ├─ aspire/
│  │  ├─ ShowRoom.AppHost              # orchestration Aspire (services, PostgreSQL, RabbitMQ)
│  │  └─ ShowRoom.ServiceDefaults      # OpenTelemetry, health checks, service discovery
│  ├─ backends/
│  │  ├─ ShowRoom.Customer.Api         # service Customer (producteur messaging)
│  │  └─ ShowRoom.Business.Api         # service Order + Product (consommateur messaging)
│  ├─ frontends/
│  │  └─ ShowRoom.Web                  # front Blazor (accueil + détail client, clients Refit)
│  ├─ modules/
│  │  ├─ ShowRoom.Modules.Customer     # bounded context Customer
│  │  ├─ ShowRoom.Modules.Order        # bounded context Order
│  │  ├─ ShowRoom.Modules.Order.Contracts   # messages AMQP partagés (contrat inter-service)
│  │  └─ ShowRoom.Modules.Product      # bounded context Product
│  ├─ buildingblocks/ShowRoom.BuildingBlocks  # primitives DDD, Result, PublicId, observabilité, pagination
│  └─ sharedkernel/ShowRoom.SharedKernel      # types partagés : VO (Email, PhoneNumber) + SmartEnum Currency (ISO 4217)
└─ tests/
   ├─ ShowRoom.Testing                        # harnais d'intégration (Testcontainers, factory générique)
   ├─ ShowRoom.Architecture.Tests             # tests de frontières (ArchUnitNET)
   ├─ ShowRoom.Web.Tests                      # mapping + orchestration de la façade du front
   └─ ShowRoom.Modules.<Module>.Tests / .IntegrationTests
```

---

## Démarrage

**Prérequis** : SDK .NET 10, Docker (PostgreSQL + RabbitMQ via Testcontainers/Aspire).

```bash
dotnet run --project src/aspire/ShowRoom.AppHost
```

Aspire démarre PostgreSQL, RabbitMQ, les deux APIs et le front Blazor (`showroom-web`,
`http://localhost:5206`), puis ouvre le **dashboard** (traces, métriques, logs, découverte des
endpoints). Chaque service expose son UI **Scalar** (`/scalar`) en développement.

Le front seul (page d'accueil et écrans sans appel d'API ; les écrans consommant le service Customer
attendent `http://localhost:5205`, port fixé par l'AppHost) :

```bash
dotnet run --project src/frontends/ShowRoom.Web
```

> Astuce : pour observer la trace distribuée, appeler
> `GET /api/v1/customers/{publicId}/with-orders` sur le service Customer — la trace unique traverse
> Customer.Api → RabbitMQ → Business.Api dans le dashboard.

---

## APIs disponibles

Toutes les routes sont versionnées sous `/api/v{version}` (v1 par défaut). Les ressources sont
identifiées par leur **`PublicId`** (jamais l'identifiant technique).

### ShowRoom.Customer.Api

| Verbe | Route | Description | Réponses |
|---|---|---|---|
| `POST` | `/api/v1/customers` | Crée un client | `201` + `PublicId` · `400` · `409` (email déjà utilisé) |
| `GET` | `/api/v1/customers?page=&pageSize=&search=` | Liste paginée, filtre optionnel par nom | `200` · `400` |
| `GET` | `/api/v1/customers/{publicId}` | Détail d'un client | `200` · `400` · `404` |
| `GET` | `/api/v1/customers/{publicId}/with-orders` | Client + historique de commandes (récupéré via AMQP) | `200` (avec `ordersAvailable`) · `400` · `404` |
| `PATCH` | `/api/v1/customers/{publicId}/email` | Change l'email d'un client (lève `CustomerEmailChanged`) | `204` · `400` · `404` · `409` (email déjà pris) |
| `PUT` | `/api/v1/customers/{publicId}` | Met à jour le profil — **Style 1** task-based, events fins (`CustomerRenamed`/`…EmailChanged`/`…PhoneChanged`) | `204` · `400` · `404` · `409` |
| `PUT` | `/api/v1/customers/{publicId}/profile` | Met à jour le profil — **Style 2** coarse, un seul `CustomerProfileUpdated` (+ `ChangedFields`) | `204` · `400` · `404` · `409` |

### ShowRoom.Business.Api

**Orders**

| Verbe | Route | Description | Réponses |
|---|---|---|---|
| `POST` | `/api/v1/orders` | Crée une commande (lignes produit) | `201` + `PublicId` · `400` |
| `GET` | `/api/v1/orders/{publicId}` | Détail d'une commande + lignes | `200` · `400` · `404` |
| `GET` | `/api/v1/orders?page=&pageSize=&customerPublicId=` | Liste paginée, filtre optionnel par client | `200` · `400` |

**Products**

| Verbe | Route | Description | Réponses |
|---|---|---|---|
| `POST` | `/api/v1/products` | Crée un produit | `201` + `PublicId` · `400` |
| `GET` | `/api/v1/products/{publicId}` | Détail d'un produit | `200` · `400` · `404` |
| `GET` | `/api/v1/products?page=&pageSize=&publicId=` | Liste paginée, filtre optionnel par public id | `200` · `400` |

**Communs aux deux services** : `GET /api/status`, `GET /health` (readiness), `GET /alive`
(liveness), `/openapi` + `/scalar` (développement).

---

## Front Blazor (ShowRoom.Web)

`src/frontends/ShowRoom.Web` — **Blazor Web App** (.NET 10) en render mode **InteractiveServer**,
orchestré par Aspire (`showroom-web`) et instrumenté comme les services (ServiceDefaults + Serilog →
OTLP, `/health` et `/alive`).

**Écrans disponibles :**

| Route | Écran | API consommée |
|---|---|---|
| `/` | Accueil — topologie (SVG), manifeste, piliers, modules | — |
| `/customers?page=&search=` | Liste paginée + filtre par nom | `GET /api/v1/customers` |
| `/customers/{publicId}` | Détail d'un client (fiche) | `GET /api/v1/customers/{publicId}` |
| `/customers/{publicId}/orders` | Historique des commandes (agrégat cross-service) | `GET /api/v1/customers/{publicId}/with-orders` |
| `/customers/new` | Création d'un client (formulaire) | `POST /api/v1/customers` |

Les trois écrans du module couvrent la convention des **4 écrans de référence**
([`frontend.md`](.claude/rules/frontend.md) §2.3) : consultation, création, groupement, navigation.
L'état de la liste (page, recherche) vit dans la **query string** : l'écran reste partageable par URL
et le bouton Retour du navigateur fonctionne.

Structure (conforme à [`.claude/rules/frontend.md`](.claude/rules/frontend.md) §11) :

```text
src/frontends/ShowRoom.Web/
├─ App/                            # coquille technique
│  ├─ App.razor                    # document HTML racine
│  ├─ Layout/                      # MainLayout (header/nav/footer, scroll-top), ReconnectModal
│  └─ Routing/                     # Routes, Error, NotFound
├─ Features/
│  ├─ Home/                        # page "/"
│  └─ Customer/                    # module Customer
│     ├─ CustomerFacade.cs         # LA porte d'entrée du module pour l'UI (+ ICustomerFacade)
│     ├─ CustomerFormat.cs         # règles de présentation partagées par les écrans
│     ├─ CustomerListResult.cs     # issue : Loaded / Unavailable
│     ├─ CustomerLookupResult.cs   # issue : Found / InvalidPublicId / NotFound / Unavailable
│     ├─ CustomerCreationResult.cs # issue : Created / EmailAlreadyUsed / Rejected / Unavailable
│     ├─ CustomerList/             # écran : Page.razor, CustomerTable, CustomerListView,
│     │                            #         CustomerListMapper
│     ├─ CustomerDetail/           # écran : Page.razor, CustomerDetailCard,
│     │                            #         CustomerDetailView (view model), CustomerDetailMapper
│     ├─ CustomerOrders/           # écran : Page.razor, CustomerOrderCard, CustomerOrdersView,
│     │                            #         CustomerOrdersMapper
│     └─ CreateCustomer/           # écran : Page.razor, CreateCustomerForm (POCO),
│                                  #         CreateCustomerFormValidator, CreateCustomerMapper
├─ Infrastructure/
│  ├─ Api/
│  │  ├─ BackendApiOptions.cs      # adresses des services (section "BackendApi")
│  │  ├─ RefitRegistration.cs      # AddRefitClient + RefitSettings (System.Text.Json)
│  │  └─ Refit/
│  │     ├─ Models/PagedResponse.cs # enveloppe de pagination partagée (tous modules)
│  │     └─ Customer/              # ICustomerApi + Models/ (List + Get + Create)
│  ├─ Validation/                  # FluentValidationValidator (branche FluentValidation sur EditForm)
│  └─ PublicIds/PublicIdFormat.cs  # contrôle de format au boundary
├─ customer.refitter               # config Refitter (génération optionnelle — voir plus bas)
└─ wwwroot/
   ├─ app.css                      # design system swiss (tokens CSS)
   └─ App/Layout/scrollTop.js      # asset colocalisé du layout
```

**Consommation d'API — Refit, en approche sélective.** Le front n'appelle jamais un `HttpClient` nu ni
un client généré depuis un composant : il orchestre une **façade par module** (`ICustomerFacade`), qui
appelle une **interface Refit** enregistrée sur `IHttpClientFactory` (donc bénéficiant du service
discovery Aspire et du handler de résilience de ServiceDefaults). Les méthodes renvoient
`ApiResponse<T>` : un `404`/`400`/`409` est une **donnée métier** traduite en issue explicite
(`CustomerLookupOutcome`, `CustomerCreationOutcome`), pas une exception. Chaque état (chargement / vide
/ invalide / introuvable / indisponible / succès) a sa branche dans la page.

**Modèle d'hébergement et render modes.** `ShowRoom.Web` est une **Blazor Web App** (modèle unifié
.NET 8+, un seul projet, `blazor.web.js`) — et non l'ancien template « ASP.NET Core hosted WebAssembly »
(supprimé depuis .NET 8, remplacé par les render modes). Aucun WebAssembly n'est embarqué : le client
Refit et la façade vivent côté serveur, donc le navigateur ignore l'existence des APIs (propriété
**BFF**). Passer en `InteractiveWebAssembly` / `InteractiveAuto` exigerait un projet Client, l'exposition
des APIs au navigateur (CORS + auth) et alourdirait le **premier** chargement — c'est un choix
d'architecture, pas un réglage.

Le render mode est décidé **par écran**, pas globalement :

| Écran | Render mode | Circuit SignalR |
|---|---|---|
| Accueil, liste, fiche, commandes | **SSR statique** + `[StreamRendering]` | **aucun** |
| Création de client | `InteractiveServer` | oui (validation au fil de la saisie) |

Vérifié sur le HTML servi : 0 marqueur de composant interactif sur les écrans de lecture, 1 sur
`/customers/new`. Les interactions des écrans statiques passent par le web : **formulaire GET** pour le
filtre, **liens** pour la pagination et les actions « Réessayer » — la navigation enrichie de
`blazor.web.js` les rend sans rechargement complet, et les URLs restent partageables.

La coquille (menu mobile, retour en haut) est pilotée par `wwwroot/App/Layout/shell.js` en **JS pur** :
en faire des composants interactifs rouvrirait un circuit sur chaque page et annulerait le bénéfice du
SSR statique.

**Streaming.** Les écrans de lecture déclarent `@attribute [StreamRendering]` : la coquille part dès le
premier octet, les données sont diffusées dès que l'API répond, au lieu de retenir toute la réponse HTML.
Mesuré sur l'écran des commandes, service Order arrêté (pire cas, 6,3 s de dégradation) :

| | TTFB | Réponse complète |
|---|---|---|
| Sans streaming | **6,36 s** | 6,36 s |
| Avec streaming | **0,03 s** | 6,32 s |

Soit un premier octet ~150× plus rapide, et un écran blanc de 6,4 s remplacé par la coquille + le
squelette immédiats. Sur la liste (API à chaud) : TTFB 0,05 s, réponse complète 0,40 s.

**Compression et cache HTTP.** `UseResponseCompression` (Brotli + Gzip, niveau optimal) est activé sur
les réponses dynamiques. Les assets statiques ne passent pas par là : `MapStaticAssets` les sert déjà
**pré-compressés et empreintés** (vérifié : `app.css` renvoyé en `br`, 1 988 o, avec ETag).

| Page | Poids nu | Poids Brotli | Encodage |
|---|---|---|---|
| `/` | 17 967 o | **4 969 o** (−72 %) | `br` |
| `/customers/new` | 8 742 o | **3 590 o** (−59 %) | `br` |
| `/customers`, fiche, commandes | 21 575 o | 21 575 o | `identity` |

> ⚠️ **Streaming et compression s'excluent.** Les trois écrans en `[StreamRendering]` sortent en
> `Content-Encoding: identity` — un intermédiaire qui compresse pourrait tamponner la réponse et
> détruire le streaming. Le compromis est assumé : sur le chemin dégradé de l'écran commandes, le
> streaming ramène le TTFB de 6,36 s à 0,03 s, ce qu'aucune compression n'approche. TTFB vérifié
> inchangé après activation (`/customers` : 0,05 s).

**Cache HTTP : volontairement aucun cache de sortie.** Décision mesurée, pas supposée :
- les écrans clients portent l'en-tête **`no-store, no-cache, must-revalidate`** — une liste en cache
  masquerait le client tout juste créé, et un « retour » sur un poste partagé rejouerait des données
  personnelles ;
- la page d'accueil, seule candidate, émet un **`Set-Cookie` antiforgery** comme toute réponse SSR Razor
  Components. L'output cache refuse — à juste titre — de stocker une telle réponse : la forcer
  distribuerait le jeton antiforgery d'un visiteur à tous les autres. Mesuré : 5 requêtes successives à
  29–37 ms, sans aucun palier de cache. Gain écarté (~30 ms de rendu serveur) au regard du risque.
  À reconsidérer seulement derrière un CDN/proxy qui retire le cookie.

**Chargement.** La liste ne dit pas « Chargement… » : elle rend un **squelette** reproduisant le
tableau réel (5 lignes), diffusé dès le premier octet grâce au streaming. La pagination reste rendue
mais masquée en `visibility: hidden` : l'arrivée des données ne décale rien. Scintillement et fondu
sont neutralisés sous `prefers-reduced-motion`.

Deux réglages ont été **retirés après mesure**, et c'est instructif :

| Réglage retiré | Pourquoi |
|---|---|
| Plancher d'affichage de 350 ms | En SSR + streaming le squelette part dans le premier flush et le navigateur coalesce les peintures : rien ne clignote. Le plancher retardait la réponse complète de 345 ms (381 ms → 52 ms) sans rien apporter. |
| Squelette calqué sur la page précédente | En SSR statique chaque requête instancie un composant neuf : un champ d'instance repart toujours de sa valeur par défaut. Le calquer sur `PageSize` alourdissait la réponse de 21,6 à 28,4 Ko, payés à chaque requête. |

Trois variantes ont été mesurées sur `/customers` avant d'arbitrer (médiane de 7 requêtes) :

| Variante | TTFB | Réponse complète | Poids | Encodage |
|---|---|---|---|---|
| Streaming + plancher 350 ms | 19 ms | 381 ms | 21 575 o | `identity` |
| **Streaming sans plancher** *(retenue)* | **38 ms** | **52 ms** | 21 575 o | `identity` |
| Sans streaming (SSR simple) | 35 ms | 35 ms | **3 763 o** | `br` |

Le streaming ne gagne du temps que si l'API est lente — son TTFB reste constant quoi qu'il arrive,
alors que celui du SSR simple suit la latence de l'API. Sur la liste (API à ~30 ms) les deux sont à
égalité en temps, et le SSR simple est 5,7× plus léger ; le streaming est conservé pour garder un
indicateur de chargement le jour où la liste ralentira, et par cohérence avec l'écran commandes où
l'écart est décisif (TTFB 0,02 s contre 2 s de réponse complète mesurés en dégradé).

**Tenue en charge (mesurée à 200 026 clients).** Le front n'est pas le facteur limitant : la
pagination borne la page à 20 lignes, donc son poids et son TTFB sont constants quel que soit le
volume. Ce sont les requêtes SQL de la liste qui plafonnent.

L'`ORDER BY "CreatedAt" DESC` de la liste n'avait aucun index : chaque page déclenchait un parcours
séquentiel puis un tri complet **débordant sur disque** (`external merge Disk: 9864kB`). L'index
`IX_customers_CreatedAt` (migration `AddCustomerCreatedAtIndex`) corrige ça :

| | Sans index | Avec index |
|---|---|---|
| SQL page 1 | 41 ms (`Seq Scan` + tri disque) | **0,12 ms** (`Index Scan`) |
| API page 1 | 0,070 s | **0,032 s** |
| API page 5000 | 0,112 s | **0,039 s** |
| Front liste page 1 (TTFB / complet) | 0,032 / 0,071 s | **0,017 / 0,038 s** |
| Front liste page 5000 | 0,030 / 0,107 s | **0,014 / 0,047 s** |

Deux limites subsistent, connues et non corrigées à ce stade :

1. **`OFFSET` profond** — même avec l'index, la page 5000 parcourt 100 020 entrées d'index. Seule une
   pagination *keyset* (`WHERE "CreatedAt" < @dernier`) rend le coût indépendant de la profondeur.
2. **Recherche `ILIKE '%…%'`** — non indexable en b-tree, `Seq Scan` intégral (~148 ms à 200 k). Un
   index **GIN + pg_trgm** est la réponse.

Le `COUNT(*)` de chaque page reste modeste (~35 ms à 200 k) ; au-delà du million de lignes il faudrait
un compte approché ou mis en cache. Le design du front — streaming + squelette — est celui qui encaisse
le mieux ces dégradations : son TTFB reste stable quelle que soit la lenteur de la requête.

**Dégradation cross-service, visible à l'écran.** `/customers/{publicId}/orders` consomme l'endpoint
agrégeant `with-orders` : le service Customer y récupère l'historique auprès du service Order **via
AMQP**. Quand ce saut échoue, le backend répond `200` avec `ordersAvailable = false` (dégradation
gracieuse) — un **succès partiel** que l'écran affiche comme tel : identité du client conservée,
bannière `role="status"` expliquant que seules les commandes manquent, bouton Réessayer. Ce n'est ni
une erreur, ni un « client sans commande ».

> ⚠️ **Budget de temps du client HTTP.** Ce chemin de dégradation ne répond qu'une fois le budget de
> retries du backend épuisé (**6,3 s** mesurées — voir `OrderHistoryRetryPolicy` dans
> [Messaging](#messaging--observabilité)). Le handler de résilience par défaut de ServiceDefaults coupe
> à **10 s par tentative**, ce qui était plus court que le budget backend d'origine (~16 s) : il tuait
> l'appel avant la réponse dégradée et transformait un succès partiel explicable en « service
> indisponible ». Le client `ICustomerApi` remplace donc ce pipeline (`RemoveAllResilienceHandlers` +
> `AddStandardResilienceHandler`, **12 s par tentative**, 1 retry) — voir `RefitRegistration.cs`. Les
> deux budgets sont **couplés** : toute API agrégeant d'autres services impose la même règle — le
> budget du client doit dépasser le pire chemin de dégradation qu'il veut observer, et être resserré
> quand celui-ci l'est.

**Formulaires.** Validation **FluentValidation** uniquement (jamais DataAnnotations) : un modèle POCO,
un `AbstractValidator<T>` séparé aux messages français, branchés sur l'`EditForm` par le composant
`Infrastructure/Validation/FluentValidationValidator`. Il valide tout le modèle à la soumission mais ne
remplace que les messages du champ modifié lors d'une saisie — un champ non encore rempli ne s'affiche
pas en erreur prématurément. Les règles client reproduisent les contraintes du backend, qui reste
l'autorité : un email en doublon n'est détectable que côté serveur (409 → message dédié, l'utilisateur
reste sur le formulaire ; succès → redirection vers la fiche créée).

**Génération avec Refitter (optionnelle).** Tant que la surface consommée reste étroite, l'interface est
écrite à la main (`ICustomerApi` : une méthode). Quand elle s'élargit, `customer.refitter` permet de la
régénérer depuis l'OpenAPI du service — sans rien changer aux appelants, la façade étant la seule
frontière :

```bash
dotnet tool install --global refitter
```

Puis, **le service Customer étant démarré** (l'`openApiPath` pointe sur son document `/openapi/v1.json`) :

```bash
refitter --settings-file src/frontends/ShowRoom.Web/customer.refitter
```

Le fichier de configuration est fourni comme point de départ mais **n'a pas encore été exécuté** :
vérifier le nom d'interface généré (`multipleInterfaces: ByTag` le dérive du tag OpenAPI) avant de
remplacer l'interface écrite à la main.

**Design system « swiss »** : Helvetica Neue, grille d'espacement en multiples de 8 px, aplats et
filets 1 px, libellés capitales espacées, aucun framework CSS. Les tokens (`--color-*`, `--space-*`,
`--font-size-*`, `--transition-*`) sont définis une seule fois dans `wwwroot/app.css` ; chaque page
n'écrit que son propre `*.razor.css` **scopé** et ne consomme que ces variables. Mobile-first :
breakpoints `1024px` / `768px`, cibles tactiles ≥ 44 px, aucun débordement horizontal.

---

## Messaging & observabilité

- **Contrat** : `ShowRoom.Modules.Order.Contracts` — messages `GetOrdersForCustomer` /
  `OrdersForCustomerResponse`, records purs sans dépendance d'implémentation.
- **Producteur** (Customer.Api) : `IMessageBus.InvokeAsync<OrdersForCustomerResponse>` derrière un
  port anti-corruption (`IOrderHistory`), avec dégradation gracieuse et **retry borné sur cold-start**
  (le tout premier message provisionne connexion + reply-queue ; les tentatives suivantes tombent sur
  un chemin chaud, et la lecture étant idempotente le retry est sûr).
- **Budget de temps de ce chemin de lecture** — `OrderHistoryRetryPolicy` (slice
  `Features/GetCustomerWithOrders/`). Cette lecture sert une requête HTTP : son pire cas est de la
  latence vue par l'utilisateur, donc les délais sont **explicites par tentative** au lieu d'hériter du
  timeout Wolverine de 5 s :

  | | Délai | Rôle |
  |---|---|---|
  | Tentative 1 | 2 s | chemin chaud (répond en ms) — échoue vite |
  | Backoff | 200 ms | |
  | Tentative 2 | 4 s | absorbe le provisioning du cold-start |
  | **Pire cas** | **6,2 s** | avant la réponse dégradée (`ordersAvailable = false`) |

  Mesures : cold-start **2,9 s** avec historique complet (la 2ᵉ tentative réussit), chemin chaud
  **0,07 s**, dégradé **6,3 s** (contre ~16 s avec l'ancien budget de 3 × 5 s). Un test unitaire
  (`OrderHistoryRetryPolicyTests`) verrouille ce plafond.
- **Consommateur** (Business.Api) : handler Wolverine écoutant la file RabbitMQ, répondant depuis
  `OrdersContext`.
- **Traces** : sources OpenTelemetry `Wolverine` + `RabbitMQ.Client.*` enregistrées ; Wolverine
  propage le contexte de trace W3C à travers le broker → une seule trace distribuée
  producteur → broker → consommateur → réponse. Chaque élément (HTTP → send → publish → deliver →
  handle → handler métier → **requêtes SQL** via l'instrumentation Npgsql) est un span avec sa
  **durée**. Métriques Wolverine (`Wolverine*`), runtime, ASP.NET Core et **Npgsql** (pool/commandes)
  exportées.
- **Logs** : Serilog structuré, enrichi `[Module] [Feature] [RequestId]`, `RequestId` dérivé du
  `TraceId` pour corréler logs et traces des deux services.

### Outbox transactionnel (livraison garantie des IntegrationEvents)

Là où `GetOrdersForCustomer` est une **lecture** best-effort (request/reply, dégradation gracieuse),
un *IntegrationEvent* comme `CustomerRegisteredIntegrationEvent` exige une **livraison au moins une
fois** entre services. ShowRoom l'assure avec l'**outbox transactionnel Wolverine**, sans mélanger les
genres — **un seul DbContext**, la séparation se faisant au niveau du **schéma** :

- **DbContext unique + schéma dédié** — `CustomersContext` porte le métier (schéma `customers`) ; les
  tables d'enveloppes Wolverine vivent dans le schéma `wolverine` de la **même** base. L'atomicité vient
  de là : l'outbox écrit l'enveloppe **via la connexion du DbContext**, donc dans la même transaction.
  Le message store (`PersistMessagesWithPostgresql(showroom, "wolverine")`) est configuré une seule fois
  dans `ConfigureShowRoomMessaging`, piloté par `Messaging:UseTransactionalOutbox` ; ses tables sont
  auto-provisionnées, **hors migrations** du module.
- **Publication atomique** — le handler `CreateCustomer` injecte `IDbContextOutbox<CustomersContext>`,
  travaille sur `outbox.DbContext`, publie l'événement, puis `SaveChangesAndFlushMessagesAsync()` :
  l'insert du client **et** l'enveloppe sont écrits dans **une seule transaction** (tout ou rien).
- **Livraison durable** — le point d'envoi RabbitMQ est déclaré `UseDurableOutbox()` : un agent Wolverine
  rejoue l'enveloppe jusqu'à acquittement, survivant aux crashes du process et aux coupures du broker.
- **Tests sans broker** — les tests d'intégration désactivent les transports externes
  (`DisableAllExternalWolverineTransports`) et nettoient le stockage Wolverine
  (`ClearAllWolverineStorageAsync`) ; `CreateCustomerOutboxTests` utilise le *message tracking* Wolverine
  (`TrackActivity().ExecuteAndWaitAsync(...)`) pour affirmer que l'événement est bien émis par l'outbox —
  sans RabbitMQ réel.

**Consommateur + retry / dead-letter (Business.Api).** Le module Order consomme
`CustomerRegisteredIntegrationEvent` (slice `Features/OnCustomerRegistered`) :

- **Inbox durable** — `ListenToRabbitQueue(...).UseDurableInbox()` ; Business.Api a **son propre** message
  store (`Messaging:UsePersistentMessageStore`, schéma **`wolverine_business`** — un runtime Wolverine ne
  partage jamais le store d'un autre service).
- **Politique retry/dead-letter** — un message *poison* (non traitable, ici sans `PublicId`) lève
  `UnprocessableCustomerRegisteredException` ; la politique le rejoue quelques fois avec cooldown puis le
  déplace en **dead-letter** (`wolverine_business.wolverine_dead_letters`) plutôt que de le rejouer
  indéfiniment. Elle est **scopée par type d'exception**, donc le request/reply `GetOrdersForCustomer`
  n'est pas affecté.
- **Preuve E2E** — `CustomerRegisteredConsumerE2ETests` (PostgreSQL + RabbitMQ Testcontainers, broker
  réel) : un message bien formé est traité (`MessageSucceeded`), un message poison finit en dead-letter
  (`MovedToErrorQueue`).

> Domain events (in-process, at-most-once) vs Integration events (cross-service, at-least-once) : les
> premiers passent par un `SaveChangesInterceptor` best-effort, les seconds par cet outbox durable.

### Métriques messaging (`MessagingMetrics`) — prêtes pour un board

Meter dédié **`ShowRoom.Messaging`** (enregistré via `AddMeter` dans les deux hosts), en complément —
jamais en remplacement — des spans et des métriques Wolverine. Deux histogrammes de durée (`ms`) :

| Instrument (OTel) | Type · unité | Enregistré par | Mesure |
|---|---|---|---|
| `showroom.messaging.roundtrip.duration` | Histogram · `ms` | Producteur (Customer.Api, port `IOrderHistory`) | round-trip complet perçu par l'appelant : produce → réponse reçue (broker + réseau + traitement) |
| `showroom.messaging.handler.duration` | Histogram · `ms` | Consommateur (Business.Api, handler AMQP) | temps de traitement du message : dequeue → réponse produite |
| `showroom.messaging.retries` | Counter · `{retry}` | Producteur (Customer.Api, port) | nombre de retries request/reply (cold-start / timeouts retentés) |

**Tags** (pour `group by` / filtres dans le board) :

| Tag | Valeurs | Sur |
|---|---|---|
| `messaging.module` | `Customer`, `Order` | roundtrip · handler · retries |
| `messaging.feature` | `GetCustomerWithOrders`, `GetOrdersForCustomer` | roundtrip · handler · retries |
| `messaging.message` | `GetOrdersForCustomer` | roundtrip · retries |
| `messaging.outcome` | `success`, `failure`, `invalid_request` | roundtrip · handler |

**Panneaux type** (histogrammes OTel → en Prometheus, `.` devient `_` et l'unité est suffixée, d'où
`showroom_messaging_roundtrip_duration_milliseconds_*`) :

```promql
# Latence round-trip p95 (par feature)
histogram_quantile(0.95, sum by (le, messaging_feature) (
  rate(showroom_messaging_roundtrip_duration_milliseconds_bucket[5m])))

# Latence de transport pure ≈ round-trip − handler (p95), isole le coût AMQP du traitement métier
histogram_quantile(0.95, sum by (le) (rate(showroom_messaging_roundtrip_duration_milliseconds_bucket[5m])))
- histogram_quantile(0.95, sum by (le) (rate(showroom_messaging_handler_duration_milliseconds_bucket[5m])))

# Taux d'échec du round-trip (dégradations / timeouts non récupérés)
sum(rate(showroom_messaging_roundtrip_duration_milliseconds_count{messaging_outcome!="success"}[5m]))
/ sum(rate(showroom_messaging_roundtrip_duration_milliseconds_count[5m]))
```

Dans le **dashboard Aspire** (dev) : onglet *Metrics* → ressource → meter `ShowRoom.Messaging` ; les
durées sont aussi posées en tags de span (`customer.orders.roundtrip_ms`, `messaging.handler.duration_ms`)
visibles dans l'onglet *Traces*.

**Dashboard Grafana prêt à l'emploi** : [`docs/observability/showroom-messaging.grafana.json`](docs/observability/showroom-messaging.grafana.json)
— importable tel quel (Grafana → *Import* → choisir la datasource Prometheus). Panneaux : round-trip
p50/p95/p99, handler p50/p95/p99, latence de transport (round-trip − handler), débit par `outcome`,
taux d'échec, round-trip p95 par feature, **retries (cold-start)** ; variable `feature` pour filtrer.

### Métriques métier (Orders)

KPIs domaine (pas des timings d'infra), émis sur le **Meter de chaque module** (`ShowRoom.Modules.<Module>`,
enregistré via `AddMeter(<Module>.TelemetrySourceName)`), depuis les handlers `Create*` — pattern : Meter
de module + helper `*Metrics` co-localisé dans le slice.

| Instrument (OTel) | Module | Type | Tags | Mesure |
|---|---|---|---|---|
| `showroom.orders.created` | Order | Counter · `{order}` | `order.currency` | commandes créées (taux = commandes/s) |
| `showroom.orders.amount` | Order | Histogram | `order.currency` | total d'une commande ; `sum/count` = **panier moyen** (€), buckets = distribution |
| `showroom.orders.items` | Order | Histogram · `{item}` | — | nb d'articles/commande ; `sum/count` = **panier moyen en articles** |
| `showroom.customers.registered` | Customer | Counter · `{customer}` | — | clients inscrits (**acquisition**) |
| `showroom.<entity>.create.rejected` | Order · Product · Customer | Counter · `{rejection}` | `reason` (`validation`/`domain`/`conflict`) | créations **rejetées** — qualité du funnel |

```promql
# Panier moyen (€) par devise
sum by (order_currency) (rate(showroom_orders_amount_sum[$__rate_interval]))
/ sum by (order_currency) (rate(showroom_orders_amount_count[$__rate_interval]))

# Panier moyen en articles
sum(rate(showroom_orders_items_sum[$__rate_interval])) / sum(rate(showroom_orders_items_count[$__rate_interval]))

# Clients inscrits / minute
sum(rate(showroom_customers_registered_total[$__rate_interval])) * 60

# Rejets de création par module & raison
sum by (reason) (rate(showroom_orders_create_rejected_total[$__rate_interval]))
```

**Dashboard métier** : [`docs/observability/showroom-business.grafana.json`](docs/observability/showroom-business.grafana.json)
— commandes créées/min, panier moyen (€ et articles), distribution du montant (p50/p95), clients
inscrits/min, **rejets de création par raison** (les 3 modules), totaux sur la période ; variable `currency`.

---

## Tests

```bash
dotnet test Pops-ShowRoom.slnx
```

- **Unitaires** (`*.Tests`) : domaine, assembleurs (`To`/`From`), validateurs.
- **Intégration** (`*.IntegrationTests`) : endpoints sur un host isolé via Testcontainers PostgreSQL ;
  le harnais `ShowRoom.Testing` (`BusinessWebFactory<TEntryPoint>`) est générique et chaque suite cible
  son service.
- **Architecture** (`ShowRoom.Architecture.Tests`) : frontières entre couches et entre modules
  (un module ne dépend que du `*.Contracts` d'un autre, jamais de son implémentation).
- **Front** (`ShowRoom.Web.Tests`) : mapping DTO → view model et orchestration de la façade — chaque
  statut HTTP (200/400/404/5xx) et l'indisponibilité réseau sont couverts via un stub écrit à la main
  de l'interface Refit (aucun framework de mock).
