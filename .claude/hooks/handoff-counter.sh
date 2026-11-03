#!/usr/bin/env bash
# PostToolUse hook: counts tool calls per worker and reminds it to hand off.
# Workers are subagents (agent_id present) or Project threads (main session of a thread).
# Uses only bash, sed and mkdir so it runs in cloud sessions and in Git Bash on Windows.

input="$(cat)"
field() { printf '%s' "$input" | tr -d '\n' | sed -n "s/.*\"$1\"[[:space:]]*:[[:space:]]*\"\([^\"]*\)\".*/\1/p"; }

agent_id="$(field agent_id)"
session_id="$(field session_id)"
key="${agent_id:-main-$session_id}"
key="$(printf '%s' "$key" | tr -c 'A-Za-z0-9_-' '_')"
[ -z "$key" ] && exit 0

dir="${CLAUDE_PROJECT_DIR:-.}/.claude/.state/tool-calls"
mkdir -p "$dir" 2>/dev/null || exit 0
file="$dir/$key"
count=$(( $(cat "$file" 2>/dev/null || echo 0) + 1 ))
printf '%s' "$count" > "$file"

msg=""
if [ -n "$agent_id" ]; then
  if [ "$count" -ge 60 ]; then
    msg="HANDOFF RULE - HARD STOP: you have made $count tool calls. Stop now: push your branch, post the handoff note on the ticket (branch and last commit, done, in progress, ordered remaining steps, scratch files, gotchas), report, and end."
  elif [ "$count" -eq 50 ] || [ "$count" -eq 55 ]; then
    msg="HANDOFF RULE: you have made $count tool calls. Do not start new work. Stop at the next safe point, push, post the handoff note on the ticket, report, and end."
  fi
else
  if [ "$count" -eq 50 ] || [ "$count" -eq 60 ] || { [ "$count" -gt 60 ] && [ $(( (count - 60) % 25 )) -eq 0 ]; }; then
    msg="HANDOFF RULE: this session has made $count tool calls. If you are a builder, helper or escalation worker (for example a Project thread), stop at the next safe point, push, post the handoff note on the ticket, and end. If you are the PM or coordinator, ignore this, but keep docs/HANDOFF.md current and avoid doing worker tasks yourself."
  fi
fi

if [ -n "$msg" ]; then
  printf '{"hookSpecificOutput":{"hookEventName":"PostToolUse","additionalContext":"%s"}}\n' "$msg"
fi
exit 0
