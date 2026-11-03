---
name: project-kickoff
description: Starts every new product project the owner's standard way - a full interview, an agreed action plan, then the Market Research, Founder Playbook and Build Plan documents from fixed templates, then a hard stop until the owner says "let's start". Use when a new project begins, or when the user says kickoff, project-kickoff, or new project.
---

# Project kickoff

The owner wants every project to start the same way, with the same deliverables, interpreted the same way every time. Follow these phases in order. Never skip a phase, never merge phases, never reinterpret a rule. If a rule here seems not to fit, ask the owner instead of improvising.

Before starting, check for `docs/KICKOFF-STATUS.md` (or the same file in the project's files/Library). If it exists, read it and resume at the phase it records. Update it at the end of every phase.

## Phase 1 - Interview (no research, no documents yet)

Goal: understand the idea well enough that the three documents need no guessing.

- Ask questions in rounds. Use a multiple-choice question tool when one is available (max 4 questions per round, always allow a free answer); otherwise ask numbered questions in plain text, at most 6 per round.
- Work through `reference/question-bank.md`. Every item must end up answered, marked "decide for me", or marked "not applicable". Skip items the owner already answered; do not ask twice.
- Ask follow-ups whenever an answer is vague, contradicts an earlier answer, or opens a new decision. Ask as many rounds as needed. Never fill a gap with an assumption without saying so.
- When done, write the **Project Brief** (`docs/00-project-brief.md`, one screen: idea, customer, platform, constraints, budget, time, success criteria, every "decide for me" item with the default you propose). Show it and ask: "Is this right?" Iterate until the owner confirms.

## Phase 2 - Agree the kickoff action plan

Propose, in one short message:
- what the research will cover and roughly how much usage it will take (deep research is the biggest usage cost of kickoff);
- the three documents and where they will live;
- which founder tasks you already expect to be blocking.

Wait for explicit approval. Change the plan if asked. Do not start research before approval.

## Phase 3 - Write the three documents

Use the templates in `templates/` exactly:
- Keep every heading, in the same order. Do not add, remove or rename sections. A section that truly does not apply says "Not applicable - <reason>".
- Replace every `{{placeholder}}` and every guidance line in *italics*. No guidance text may remain.
- Every number, market size, price or competitor fact carries a source link and access date, or is labelled "Estimate - <method>".

Order and method:
1. `docs/01-market-research.md` from `templates/market-research.md`. Deep, cited web research. If you can delegate, send research legwork to Sonnet workers with a narrow brief each (one per section or competitor group) and synthesize the results yourself.
2. `docs/02-founder-playbook.md` from `templates/founder-playbook.md`. Include every standard item in the template plus anything this project specifically needs. Each task has: what, why, how (steps), time, cost, and a done-check.
3. `docs/03-build-plan.md` from `templates/build-plan.md`. Copy the operating model from `reference/operating-model.md` without weakening any rule, then fill the project-specific sections.

Where documents live: the files above are the master copies. If a Claude Docs or document tool is available, also publish a readable copy of each and refresh that copy whenever the master changes. Edits the owner wants go through you, applied to the master first.

## Phase 4 - Review

Send a short summary: the go/no-go verdict from the research, the recommended monetization, the v1 scope, and the list of **blocking founder tasks**. Ask for changes. Apply them to the masters. Repeat until the owner says the documents are agreed. Record "documents agreed" with the date in `KICKOFF-STATUS.md`.

## Phase 5 - The gate

Until the owner writes "let's start":
- Do not write code, scaffold a repository, create build tickets, or start build threads or agents.
- Help the owner finish the blocking founder tasks. Track each one as done/not done in `KICKOFF-STATUS.md`.
- If the owner says "let's start" while blocking tasks are open, list them and ask whether to proceed anyway. Proceed only on a clear yes, and record it.

## Phase 6 - Let's start

1. Confirm the repository exists and was created from the starter template (it contains `CLAUDE.md` and `.claude/agents/`). If not, stop and point the owner to the Founder Playbook task.
2. Create the tickets for milestone 1 from the Build Plan (GitHub issues if the repo has them, otherwise `TICKETS.md`).
3. Work by the operating model in the Build Plan from here on.

## Standing rules during kickoff

- Lead with the answer. No preamble, no recaps. Short messages.
- Plain language: the owner is not a developer. Explain any technical term the first time you use it.
- Never give legal, tax or financial advice as fact. Give the options and point to the professional to confirm with.
