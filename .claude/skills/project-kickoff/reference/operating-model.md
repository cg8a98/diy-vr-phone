# Agent operating model (standard for every project)

Copy this into the Build Plan unchanged. Projects may add stricter rules, never looser ones.

## Principle
Nothing important lives only in a chat. Plans, decisions, progress and handoff notes live in files and tickets, so any session can be thrown away and replaced by a fresh one at any time. Long sessions cost more per step and get sloppier, so we restart them on purpose.

## Roles

| Role | Who | Does | Never does |
|---|---|---|---|
| Owner and validator | The owner | Sets priorities, approves epics, tests builds on reference cases, talks to customers | Writes code |
| Project manager (PM) | Opus, main conversation / project coordinator | Scopes epics into tickets, briefs workers, reviews every diff, checks numbers against references, merges, keeps HANDOFF.md current | Writes feature code, runs long tool chains, reads files in bulk |
| Builder | Sonnet worker (subagent or thread) | One ticket at a time: code, tests, opens a pull request, posts a report | Merges its own work, spawns further agents, widens scope |
| Helper | Haiku worker | Lookups, renames, formatting, translation files, dependency bumps, shell chores | Calculation or business-logic code |
| Escalation | Opus worker (subagent or thread) | A ticket a builder failed twice, given both failure reports | Anything not escalated |

## Model routing
- Cheapest tier that can do the job: Haiku for mechanical work, Sonnet for normal feature work, Opus only for hard debugging, architecture, or escalations.
- On failure go up one tier. The PM never fixes a failed ticket inline.
- Effort: medium by default. High only for escalations and calculation-critical code.

## Concurrency on Pro
- One PM. At most two builders at once. Never two workers on the same files.
- One epic at a time. No "ticket fanning" (sending many small parallel tickets).
- On Max plans these limits can be raised; the Build Plan says when.

## Ticket workflow
1. PM writes the ticket: goal, acceptance criteria, files likely touched, test to prove it, model tier.
2. Builder works on its own branch, commits and pushes work in progress often.
3. Builder opens a pull request with a report: what changed, how it was tested, results, open questions.
4. PM reads the diff, re-runs or checks the numbers, then merges or sends it back. Workers never merge.
5. Every bug fix states why the bug got through and closes that gap (a test, a check, a rule).

## Context and handoffs
- Builders: at about 50 tool calls, stop at the next safe point, push, and post a handoff note on the ticket; at about 60, stop immediately. A fresh builder continues from the note.
- Handoff note format: branch and last commit, what is done, what is in progress, ordered remaining steps, scratch files, gotchas. Self-contained: a stranger could continue from it.
- Every worker brief includes: "You are the executor. Do not spawn sub-agents. Follow the handoff rule."
- PM: keeps `docs/HANDOFF.md` current (state per ticket, pending decisions for the owner, next steps). In a Claude Code Project the coordinator manages its own context; elsewhere, when the session gets long, the PM writes HANDOFF.md and a fresh session resumes from it.
- New work goes to a fresh worker rather than reviving an old idle one (an idle session re-reads its whole history first).

## Saving usage
- No idle polling: long waits are blocking calls with timeouts, not "check back later" turns.
- Workers stop watching a pull request once it is merged or handed back.
- CLAUDE.md and instructions stay short and point to docs instead of repeating them.
- Memory holds only facts that change future behavior: one fact per entry, no status logs.
- Answers first, no preamble, no recaps.

## Where things live
- `docs/` - the three kickoff documents, HANDOFF.md, decisions log.
- Tickets (GitHub issues, or TICKETS.md) - the long-term memory of the work.
- `CLAUDE.md` - short project rules and pointers.
