# {{Project name}}

Owner: solo founder, not a developer, Claude Pro plan. Answer first, plain language, no recaps.

## Read first
- `docs/KICKOFF-STATUS.md` - current phase. Before "let's start": no code, no build tickets.
- `docs/HANDOFF.md` - current state of work and next steps.
- `docs/03-build-plan.md` - scope, stack, tickets, and the full operating model.

## Rules (full version in the Build Plan, section 4)
- PM (Opus, main session/coordinator) plans, briefs, reviews diffs, merges. Never writes feature code.
- Builders: `builder` agent (Sonnet). Helpers: `helper` agent (Haiku). Escalation after two failed builder attempts: `escalation` agent (Opus).
- At most two workers at once, never on the same files. One epic at a time.
- Workers never merge and never start other agents. Branch -> pull request -> PM review -> merge.
- Workers hand off at about 50 tool calls (a hook reminds them): push, post handoff note on the ticket, stop.
- Every bug fix says why the bug got through and closes that gap.

## Project specifics
<!-- Filled at "let's start": stack, how to build, how to test, key paths, reference test data. -->
- Build:
- Test:
- Reference cases:
