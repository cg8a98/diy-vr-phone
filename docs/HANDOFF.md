# Handoff

Updated: 2026-11-10 by PM

## Current phase and milestone
Phase 6 (Build). Milestone 1 done (2026-11-09, v0.1.59). Milestone 2 Hands: all tickets merged, owner acceptance test pending (five-button 9/10, placed cube stays on the desk). Milestone 3 Room mesh started early (ticket table in docs/03-build-plan.md section 3).

Last release the owner tested: v0.1.92. Newest: v0.1.102 (updater fix, five-button fix, place cubes, faster hands). The owner's running test list lives outside git at the project files folder `tests/test-backlog.md`.

## State per ticket
| Ticket | Pull request | Status |
|---|---|---|
| 2.1-2.5, 2.2b | #50 #54 #56 #58/#67 #66 | Merged. Hands GPU 20-30 fps, sharp skeleton in passthrough (owner-confirmed on v0.1.92) |
| 2.5b five-button fix | #71 | Merged. TextMesh auto-adds MeshRenderer; build tests now run scene-building code |
| 2.6 place objects | #72 | Merged. Gaze + pinch places 8 cm cube on AR planes, D-pad DOWN clears |
| 2.2c hand lag | #73 | Merged. Overlay "Lag = cam + cv + inf + post", duplicate frames skipped, look-ahead |
| 1.24 updater | #63 #70 | Merged. #70 fixed missing install permission (androidlib needs sourceSets); CI now checks the APK manifest |
| 3.1 scan load | #75 | Merged (v0.1.110). glTFast 6.14.1, START = file picker, hold START = hide/show |
| 3.2 marker room frame | #76 | Merged (v0.1.110). 150 mm printed markers in hardware/room-markers, D-pad LEFT/RIGHT |
| 3.3 align scan | #78 | Merged. Saved per scan in scan-align.json; ControllerModal.IsOpen masks GamepadControls while aligning |
| 3.4 accuracy check | - | Next; needs the owner's tape measurements (kept on the phone, never in git) |

## Controller map (Xbox)
A/B/X/Y, shoulders, select: lens tool and prompts. Left stick click: five-button test. Right stick click: plane patches. D-pad UP: lens tool (inside it); hold 1 s with the lens tool closed = scan align mode (sticks move/turn, triggers up/down, shoulder = fine, A save, B cancel). D-pad DOWN: clear placed cubes. D-pad LEFT: room frame show/hide (hold = recalibrate). D-pad RIGHT: drop room test cube. START: load scan (hold = hide/show).

## Gotchas
- Unity can't be opened in the cloud container; CI (GameCI) is the compiler. Every new file needs a .meta.
- GitHub GraphQL is blocked: use `gh api` REST. CI artifact downloads are blocked; check release APKs instead.
- An .androidlib with its own build.gradle must set sourceSets (manifest, res, java) or its files are ignored.
- `*.png` is LFS-tracked and the LFS verify call fails from the container; small PNGs need a .gitattributes exemption.
- Scene-building code needs an EditMode test that runs it (LogAssert.NoUnexpectedReceived), not just maths tests.
- Passthrough is mono at infinity; near virtual objects won't align perfectly until depth reprojection.
- Never commit scan files, room measurements or personal data.
- Builder model: Fable while the promo lasts (until 2026-11-16), then Sonnet. Running 4 builders at once hit the 5-hour limit.

## Paused workers
None.

## Next steps (in order)
1. Owner installs the newest release by hand once and runs the test backlog (7 tests).
2. PM plans 3.4 with the owner's reference-point measurements; 3.5 colliders and boundary after the align test passes.
3. Tune pinch thresholds and hand lag from the owner's numbers.
