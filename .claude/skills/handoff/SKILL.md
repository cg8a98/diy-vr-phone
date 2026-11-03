---
name: handoff
description: Saves the PM's full working state to docs/HANDOFF.md so a fresh session can continue, and resumes from it. Use when the PM session is getting long, before a break, when a usage limit is near, or when the owner says handoff or resume.
---

# Handoff and resume (PM sessions)

In a Claude Code Project the coordinator manages its own context, so use this only in local or cloud sessions you started yourself, or when the owner asks.

## Handoff
1. Stop dispatching new work. Let running workers reach a safe point and post their own handoff notes.
2. Measure state with git and the ticket list, not from memory: for each open ticket, its branch, last commit, pull request, and status.
3. Rewrite `docs/HANDOFF.md` using its section headings: current phase and milestone, state per ticket, paused workers, decisions waiting for the owner, ordered next steps, gotchas, scratch files.
4. Append one line to `docs/handoff-log.md`: date, what was done, what's next.
5. Commit and push the docs.
6. Tell the owner, in two lines: what was saved, and "Start a new session (or type /clear), then type: resume".

## Resume
1. Read `CLAUDE.md`, `docs/KICKOFF-STATUS.md`, then `docs/HANDOFF.md`.
2. Re-check every ticket's real state with git and the ticket list; note anything that changed since the handoff.
3. Give the owner a three-line status and continue with the first next step.
