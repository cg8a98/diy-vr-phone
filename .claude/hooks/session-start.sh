#!/usr/bin/env bash
# SessionStart hook: plain-text output is added to the session's context.
root="${CLAUDE_PROJECT_DIR:-.}"
phase="$(sed -n 's/^Phase:[[:space:]]*//p' "$root/docs/KICKOFF-STATUS.md" 2>/dev/null | head -n 1)"
echo "Project rules: CLAUDE.md. Current kickoff phase: ${phase:-unknown - read docs/KICKOFF-STATUS.md}."
echo "Before \"let's start\": no code, no build tickets. Read docs/HANDOFF.md before working."
echo "Routing: PM plans/reviews/merges only; builder=Sonnet, helper=Haiku, escalation=Opus after two failed builder attempts; max two workers at once; workers never merge or start other agents."
exit 0
