# Twiggle

AI-guided experience discovery, a source-linked Experience Bank, and an evolving Personal Knowledge Graph.

**Status:** Local manual experience capture. Sessions, append-only original answers, source-linked factual claims, explicit review, claim history, and Experience Bank records are persisted in PostgreSQL. Adaptive AI interviews, skill discovery, hosted authentication, exports/deletion UI, and deployment are not implemented.

Start with [First Saved Experience](docs/first-experience.md) for the update and test flow.

See [Local Development](docs/local-development.md) for Windows setup and verification.

## Documentation

Open [`docs/index.html`](docs/index.html) locally in a browser. Pages are self-contained, responsive, printable, and work offline.

- [Product Blueprint](docs/index.html): purpose, scope, architecture, roadmap, decisions, and Windows setup.
- [Web Architecture & UX](docs/web-architecture.html): web stack, routes, four core wireframes, interaction states, API boundaries, and delivery milestones.
- [Phase 1 — Discover Yourself](docs/phase-1.html): interview policy, state lifecycle, claim-level model, evidence, UI, engineering steps, and evaluation.
- [Phase 2 — Career Intelligence](docs/phase-2.html): market method, career hypotheses, constraints, matching, and traceable CVs.
- [Phase 3 — Opportunities & Feedback](docs/phase-3.html): existing tracker integration, snapshots, applications, decisions, and feedback.
- [Phase 4 — Generalization](docs/phase-4.html): cross-domain validation, domain packs, workspace isolation, and release gates.

## Windows checkout

Run in PowerShell with Git for Windows installed and access to this private repository:

```powershell
New-Item -ItemType Directory -Force "C:\Users\Admin\Documents\GitHub" | Out-Null
Set-Location "C:\Users\Admin\Documents\GitHub"
git clone https://github.com/huizi135/Twiggle.git
Set-Location Twiggle
Start-Process .\docs\index.html
```

If already cloned, inspect `git status` before using `git pull --ff-only`. Do not overwrite local changes.

## Design principles

Experience before keywords. Discover through neutral questions. Preserve individual ownership and original sources. Separate facts, interpretations, preferences, and eligibility. Keep uncertainty visible and corrections effective. Private by default.

The EVIDENCE handoff brief v0.1 (26 October 2026) is the conceptual baseline. Twiggle is the working name. Detailed personal source material is intentionally excluded from the repository; examples in the handbook are synthetic. Technical choices remain proposals until implementation review. The local skeleton pins its dependencies with an npm lockfile and an explicit backend package version. Provider integrations and market datasets remain unvalidated.

Repository documentation and development language: English. Collaboration language: Chinese.
