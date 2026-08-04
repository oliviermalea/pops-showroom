# CLAUDE.md — ShowRoom (Claude Code Entry Point)

## Purpose
This file is the **Claude Code** entry point for ShowRoom.

It does **not** duplicate the coding rules. The source of truth for all
contribution rules lives in the `.claude/rules/` files. This file only routes
Claude Code to the right document(s) for the task at hand.

When rules overlap, apply the global rules first, then the specialized files,
following the precedence order defined below.

---

## Always applied
@rules/global.md

---

## Source of truth — rules files
Read and apply the relevant files for the task (paths are relative to the repository root):

- **Global (always apply):** [`.claude/rules/global.md`](rules/global.md)
  Cross-cutting rules: architecture & module boundaries, API/event contract
  discipline, documentation, testability, observability, security, delivery
  safety, DB migration guidelines, and the global PR checklist.

- **Backend scope:** [`.claude/rules/backend.md`](rules/backend.md)
  For backend/domain/API/persistence work.

- **Frontend scope:** [`.claude/rules/frontend.md`](rules/frontend.md)
  For frontend/UI/client orchestration work.

- **Cloud/DevOps scope:** [`.claude/rules/cloud.md`](rules/cloud.md)
  For cloud/runtime/deployment/observability platform work.

- **Tests scope:** [`.claude/rules/tests.md`](rules/tests.md)
  Apply together with the backend file for any test work.

---

## Scope routing (mandatory)
- **Backend/domain/API/persistence** → global + `backend.md` + `tests.md`
- **Frontend/UI/client** → global + `frontend.md`
- **Cloud/runtime/deployment/observability** → global + `cloud.md`
- **Task spanning multiple scopes** → combine the relevant files, preserving the global priority order.

---

## Absolute priority (global)
If multiple rules conflict, apply in this order:
1. Architecture and module boundaries
2. API/event contract integrity and versioning
3. Security and operational reliability
4. Testability and quality gates
5. Code style/conventions

---

## Naming conventions
- Product name in prose: **ShowRoom**.
- Projects / namespaces: **`ShowRoom.*`** (e.g. `ShowRoom.Modules.<Module>`, `ShowRoom.BuildingBlocks`).
- Repository / solution: **`Pops-ShowRoom`** (`Pops-ShowRoom.slnx`).

---

## Maintenance rule
Keep the actual rules in the `.claude/rules/` files. Do **not** re-inline their
content here — update the specialized file, and this entry point stays a thin
router.
