# Handoff

Updated: 2026-11-03 by builder (ticket 1.10)

## Current phase and milestone
Phase 6 (Build). Milestone 1 - Foundation (see docs/03-build-plan.md section 3).

## State per ticket
| Ticket | Pull request | Status |
|---|---|---|
| 1.1 to 1.9 | merged to main | Done. First signed release is v0.1.28 |
| 1.10 Milestone 1 integration | branch ticket/1.10-integration | In progress: wires stereo, head pose, test content, overlay, passthrough and lens tool into one app; one touch-gesture plan; missing .meta files; AR background in the URP renderer |

## Touch gestures (one plan, no clashes)
- Three-finger tap: debug overlay.
- Double tap anywhere: passthrough.
- Two-finger tap: lens tool. While it is open it gets all touches and the other two are ignored.

## Paused workers
None.

## Decisions waiting for the owner
- Confirm spending caps (Playbook 2.4).

## Next steps (in order)
1. PM: review and merge ticket 1.10 once CI is green.
2. Carlos: install the new release APK and run the milestone 1 acceptance test on the headset (steps are in the 1.10 pull request).
3. After his results: fix what he reports, then plan milestone 2.

## Gotchas
- Unity can't be opened in the cloud container; CI (GameCI) is the compiler. Write careful C#.
- Never commit scan files or personal data (see .gitignore).
- Android uses OpenGLES3 only (ARCore does not support Vulkan).
- Up to 3 builders at once (owner's decision, 2026-11-03).
- The passthrough image is not scaled to the lens field of view, so it will not line up with the real world yet.

## Scratch files
None.
