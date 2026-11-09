# Handoff

Updated: 2026-11-09 by builder (ticket 1.23)

## Current phase and milestone
Phase 6 (Build). Milestone 1 - Foundation is DONE. Milestone 2 - Hands is next (see docs/03-build-plan.md section 3).

Milestone 1 acceptance test passed by Carlos on 2026-11-09 with v0.1.59 in the VR Shinecon headset (walked around the cube, grid straight, info panel readable). Lens profile: lens spacing 52 mm, IPD 67 mm (on-phone, not in git).

## State per ticket
| Ticket | Pull request | Status |
|---|---|---|
| 1.1 to 1.9 | merged to main | Done. First signed release is v0.1.28 |
| 1.10 Milestone 1 integration | merged to main | Done (v0.1.59): wires stereo, head pose, test content, overlay, passthrough and lens tool into one app; one touch-gesture plan; missing .meta files; AR background in the URP renderer |
| 1.23 Settings panel at the same depth as the grid (#46) | branch ticket/1.23-panel-depth | In review: lens panel and info panel at 2.0 m (same apparent size), lens tool no longer has its own grid |

## Touch gestures (one plan, no clashes)
- Three-finger tap: debug overlay.
- Double tap anywhere: passthrough.
- Two-finger tap: lens tool. While it is open it gets all touches and the other two are ignored.

## Paused workers
None.

## Decisions waiting for the owner
- Confirm spending caps (Playbook 2.4).

## Next steps (in order)
1. PM: review and merge ticket 1.23 once CI is green; Carlos checks the panels in the headset (steps are in the pull request).
2. PM: plan Milestone 2 - Hands (docs/03-build-plan.md section 3).

## Gotchas
- Unity can't be opened in the cloud container; CI (GameCI) is the compiler. Write careful C#.
- Never commit scan files or personal data (see .gitignore).
- Android uses OpenGLES3 only (ARCore does not support Vulkan).
- Up to 3 builders at once (owner's decision, 2026-11-03).
- The passthrough image is not scaled to the lens field of view, so it will not line up with the real world yet.

## Scratch files
None.
