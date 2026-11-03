# Build Plan and Agent Operating Manual: {{product name}}

{{date}} · Status: draft / agreed

## 1. What we are building
*Product in one paragraph. v1 scope: each feature with acceptance criteria the owner can check. Out of scope for v1. Later versions (v2+) list.*

| v1 feature | Acceptance criteria (how the owner checks it) |
|---|---|

## 2. Architecture and tech stack
*Platform(s), language, frameworks, database/storage, packaging/installer, hosting if any, with one line of reasoning each and the main alternative rejected. Plain-language explanation for the owner. Repository layout. How calculations or core logic are verified (reference cases, independent check). Security and privacy basics.*

## 3. Milestones and tickets
*Milestones (epics) in order, each with a goal, the owner's acceptance test, and an estimated number of sessions. For milestone 1, list every ticket: title, goal, acceptance criteria, model tier (Haiku/Sonnet/Opus), depends on. Later milestones: ticket titles only, detailed when they start.*

| # | Milestone | Goal | Owner's acceptance test | Est. sessions |
|---|---|---|---|---|

| Ticket | Goal | Acceptance criteria | Tier | Depends on |
|---|---|---|---|---|

## 4. Operating model
*Copy `reference/operating-model.md` here in full, unchanged: principle, roles table, model routing, concurrency on Pro, ticket workflow, context and handoffs, saving usage, where things live. Add project-specific stricter rules after it, if any.*

## 5. Usage plan for the Pro plan
*How the weekly allowance is spent: kickoff and research (one-off), per-milestone estimate, a weekly rhythm (heavy build sessions on which days, light review days), what to do when a limit is hit (stop, write handoff, resume after reset - never switch to a bigger model to push through), and the warning signs that the plan is the bottleneck (feeds the upgrade rule in the Founder Playbook).*

## 6. Testing and quality gates
*Test strategy (unit tests for core logic, reference-case tests from the owner's data, manual checks the owner runs). Definition of done for a ticket and for a milestone. What the PM must check before every merge.*

## 7. Release and delivery
*How builds reach the owner for testing (installer, preview link, store build). Versioning. Release checklist pointer to the Founder Playbook.*

## 8. Risks and mitigations
*Technical risks (hard features, unknowns, platform limits), each with a spike ticket or mitigation.*

## 9. Project setup checklist (done at "let's start")
*Repository from the starter template; CLAUDE.md filled with project specifics (stack, commands to build/test, paths); milestone 1 tickets created; HANDOFF.md initialized; Project settings checked (thread model Sonnet, effort medium, at most two threads at once).*
