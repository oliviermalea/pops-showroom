# Rules — ShowRoom Cloud (v1)

## QUICK HARD RULES (must always pass)
- Cloud decisions must remain pragmatic and foundation-oriented: prioritize operability over premature complexity.
- Containerization is mandatory in Foundation: one deployable application image for the modulith.
- Use **build once, run anywhere**: the same image must be promoted across environments.
- Environment-specific behavior must come from externalized configuration (env vars / platform config), never hardcoded values.
- Observability baseline is mandatory: logs, metrics, traces, and health endpoints.
- Prefer cloud-native/PaaS managed capabilities before building custom platform components.
- CI/CD must be automated, reproducible, and traceable to a commit and image version.
- Production deployments should be controlled (manual trigger from a validated artifact is recommended in Foundation).

---

## 0) Absolute priority
If rules conflict, apply in this order:
1. Operational reliability and reversibility
2. Security and configuration hygiene
3. Deployment reproducibility and traceability
4. Observability and diagnosability
5. Cost/performance optimization

---

## 1) Scope and intent
These instructions define the ShowRoom Cloud baseline for Foundation phase.

Goal: deliver a cloud-ready operating model that is simple, reliable, and evolvable without locking teams into unnecessary complexity.

---

## 2) Containerization baseline (mandatory)

### 2.1 Single deployable unit (Foundation)
- Package the modular monolith as a single container image.
- Do not split into multiple application containers without explicit architectural need.

### 2.2 Build once, run anywhere
- Build one versioned image per validated change.
- Promote that exact same image through environments (dev/staging/prod).
- Avoid environment-specific image variants.

### 2.3 Runtime configuration
- Inject runtime configuration via environment variables/platform configuration:
  - database URLs
  - secrets
  - feature flags (if used)
  - external service endpoints
- Never hardcode environment values inside application code.

### 2.4 Container operational conventions
- Application logs must go to stdout/stderr.
- Application and data lifecycle must remain separated.
- Keep container startup deterministic and fail-fast on invalid critical config.

---

## 3) Observability baseline (mandatory)

### 3.1 Essential signals
At minimum, ensure:
- structured logs
- key metrics
- traces for critical request paths (when available)

### 3.2 OpenTelemetry standard
- Use OpenTelemetry as default instrumentation approach when feasible.
- Prefer OTLP-compatible emission to keep vendor neutrality.

### 3.3 Collector pattern
- Use an OpenTelemetry Collector (or managed equivalent) as a routing/processing layer when possible.
- Centralize export, filtering, enrichment, and destination switching at collector/platform level.

### 3.4 Correlation
- Ensure request/trace correlation identifiers are propagated across components whenever possible.

---

## 4) Health and readiness model

### 4.1 Mandatory endpoint
- Expose at least one health endpoint (e.g., `/health`) for platform probing.

### 4.2 Probe semantics
- **Liveness**: process is alive (restart candidate if failing).
- **Readiness**: instance can serve traffic safely (remove from traffic if failing).

### 4.3 Dependency-aware readiness
- Readiness should reflect critical dependency availability when relevant
  (database, cache, required external providers).

---

## 5) CI/CD minimal standard (Foundation)

### 5.1 Pipeline minimum stages
Each significant change should trigger:
1. source checkout
2. build/compile
3. automated tests (unit + architecture tests minimum; fast integration tests when possible)
4. container image build (versioned/tagged)
5. image publication (registry or platform artifact channel)
6. deployment to non-production target

### 5.2 Production release mode
- Prefer manual production deployment trigger from a previously validated artifact.
- Keep rollback path clear and documented.

### 5.3 Pipeline quality constraints
- Keep fast feedback loops (pipeline should complete in minutes when possible).
- Separate fast checks from heavier suites (nightly/on-demand for heavy flows).
- Ensure each deployment is traceable to:
  - commit SHA
  - build/pipeline run
  - image tag/digest

---

## 6) Environment strategy (pragmatic)

### 6.1 Foundation recommendation
- Start with a minimal but reliable target chain (at least staging + production).
- Add additional environments only when justified by risk/coordination needs.

### 6.2 Typical progression
- shared development
- integration
- staging
- production

### 6.3 Parity principle
- Keep runtime behavior consistent across environments as much as practical.
- Differences should be explicit and configuration-driven.

---

## 7) Platform approach: PaaS-first

### 7.1 Managed-first principle
Prefer managed platform capabilities first:
- runtime hosting
- secret/config management
- TLS/ingress basics
- logs and baseline metrics
- simple scaling controls

### 7.2 Avoid premature platform engineering
- Do not introduce custom orchestration/platform abstractions too early.
- Increase complexity only when operational pain is proven.

---

## 8) Security and resilience baseline

### 8.1 Configuration and secrets
- Never commit secrets in repository.
- Use secure secret stores/platform secret injection.
- Rotate sensitive credentials with an explicit process.

### 8.2 Failure handling
- Define timeout/retry policy for external dependencies.
- Use circuit-breaker style protections where justified by criticality.
- Keep transaction windows short and side-effects decoupled when possible.

### 8.3 Recovery posture
- Ensure operational runbooks exist for:
  - failed deployment
  - rollback
  - degraded dependency
  - restart/recovery actions

---

## 9) Cost and scalability guardrails (Foundation)

- Favor simple scaling policies first (horizontal/instance count when needed).
- Observe before optimizing: use metrics to guide scaling and cost actions.
- Avoid overprovisioning and unnecessary always-on components in early stage.

---

## 10) Definition of Done (Cloud / Foundation)

A cloud delivery increment is Done only if:
- application is containerized and versioned
- artifact promotion follows build-once-run-anywhere
- runtime config is externalized
- baseline observability is available (logs/metrics/traces as applicable)
- health endpoint and probe semantics are defined
- CI/CD pipeline is automated and traceable
- non-prod deployment is operational
- production release path is controlled and rollback-ready