# Handoff

Updated: 2026-11-03 by kickoff thread (PM)

## Current phase and milestone
Phase 6 (Build). Milestone 1 - Foundation (see docs/03-build-plan.md section 3).

## State per ticket
| Ticket | Branch | Last commit | Pull request | Status |
|---|---|---|---|---|
| 1.1 Repo skeleton | - | - | - | Ready |
| 1.2 Unity project | - | - | - | Blocked by 1.1 |
| 1.3 CI build | - | - | - | Blocked by 1.2 + owner's Unity secrets |
| 1.4 Stereo rig | - | - | - | Blocked by 1.2 |
| 1.5 6DoF head pose | - | - | - | Blocked by 1.4 |
| 1.6 Lens profile tool | - | - | - | Blocked by 1.4; owner testing needs headset |
| 1.7 Debug overlay | - | - | - | Blocked by 1.5 |
| 1.8 Passthrough toggle | - | - | - | Blocked by 1.5 |
| 1.9 Release signing and publishing | - | - | - | Blocked by 1.3 |

## Paused workers
None.

## Decisions waiting for the owner
- Confirm spending caps (Playbook 2.4).

## Next steps (in order)
1. Builder: ticket 1.1.
2. Owner: add Unity secrets to the repo (guide to be sent with ticket 1.3).
3. Builder: 1.2, then 1.3 and 1.4 in parallel (different files).

## Gotchas
- Unity can't be opened interactively in the cloud container; builders verify compilation through CI (GameCI) once 1.3 lands. Until then, keep 1.2 minimal and correct by construction.
- Never commit scan files or personal data (see .gitignore).

## Scratch files
None.
