# Project starter

Template for every new project. Create new repositories from it on GitHub ("Use this template"), so each project starts with the same rules and tools.

What's inside:
- `CLAUDE.md` - short project rules, read by every session and thread.
- `.claude/agents/` - builder (Sonnet), helper (Haiku), escalation (Opus).
- `.claude/hooks/` - start-of-session reminder, and the tool-call counter that tells workers to hand off at about 50 calls.
- `.claude/skills/project-kickoff/` - the kickoff process and the three document templates.
- `.claude/skills/handoff/` - save state and resume, for sessions outside a Claude Code Project.
- `docs/` - kickoff status, handoff file, decisions log. The kickoff documents are written here.

Replace this README with the product's own once the project starts.
