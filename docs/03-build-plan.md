# Build Plan and Agent Operating Manual: DIY Smartphone VR Headset

2026-11-03 · Status: agreed 2026-11-03

## 1. What we are building
One Android app ("the headset app") that runs on the Galaxy S23 Ultra inside a phone VR headset. It's first used in a cheap store-bought headset (v0), then in Carlos's own 3D-printed one (v1). The app shows a stereo 3D view with full head tracking (6DoF: turning and walking around), hand tracking with gaze and pinch, a scanned model of your room for boundaries and room-aware play, a cinema for Jellyfin movies (2D and 3D), and a big virtual screen for flat PC games. PC VR games stream from Carlos's PC through ALVR (open-source PC-to-headset streaming) to a phone client, which we extend with full head tracking. A thermal governor lowers quality before the phone overheats.

**v0/v1 software scope** (built in v0 on the test headset, carried unchanged into v1 hardware):

| v1 feature | Acceptance criteria (how the owner checks it) |
|---|---|
| Stereo view with 6DoF head tracking | In the headset, walk around a virtual cube for 2 minutes. It stays fixed in place with no visible jitter or drift, and the frame rate shown on the debug overlay stays at 90 fps or above (target 120). |
| Lens profile for each headset | Straight lines in a test grid look straight at the edges for the test headset, and later for the printed one. Eye spacing is set to 66.5 mm and can be changed in settings. |
| Gaze and pinch hand tracking | Look at one of 5 floating buttons and pinch. The right one activates at least 9 times out of 10, with the hand at a comfortable height. |
| Place virtual objects | Place a virtual object on your real desk. It stays on the desk while you walk around it. |
| Room mesh "Scan-and-Load" (must nail) | Load the Scaniverse scan. After alignment, 5 tape-measured reference points match the virtual room to within 3 cm, every time the app starts, 5 times in a row. The play boundary is created automatically from the floor area. |
| Live obstacle warnings | Walk towards an object that isn't in the scan (a chair moved after scanning). A warning and passthrough view appear before you are within 50 cm. |
| Jellyfin cinema (2D and 3D) | Play a 4K 2D movie and a side-by-side 3D movie from your Jellyfin server on a large virtual screen. Seeking, pause and subtitles work. |
| 3-hour session | Watch a full 3-hour movie in one sitting. With a pocket power bank on a cable before v2, the phone doesn't throttle visibly and the headset stays comfortable. |
| PC VR streaming with 6DoF | Play your chosen SteamVR game wirelessly for 30 minutes with an Xbox controller. Walking moves you in the game, with no motion sickness. |
| 2D PC game on a virtual screen | Play your chosen flat PC game on a large virtual screen with an Xbox controller for 30 minutes. Input lag feels comparable to your current Rubylight and Nightjar setup. |
| Thermal governor | During a 30-minute stress session, the overlay shows the governor lowering quality in steps. The phone never shows Android's own overheating warning. |

**Out of scope for v0/v1:** eye tracking, foveated rendering or streaming, active cooling, USB audio, a built-in battery, controllers, gloves, support for other phones, and any app store release.

**Later versions:**
- **v2:** IR eye tracking, foveated rendering and foveated streaming over ALVR, Peltier cooler and fans, USB-C DAC audio, and an external 10,000 mAh battery. The battery goes on the strap or in a pocket, whichever balances better.
- **v3:** DIY 6DoF controllers (for example HadesVR or ESP32-based) and haptic gloves (OpenGloves or SenseShift).
- **After v2:** revisit market research and whether to open-source it or sell it.

## 2. Architecture and tech stack
**In plain language:** Claude writes the app in the repository. Every change is built automatically on GitHub into an APK (Android app file). Carlos downloads it to the phone and installs it, with no programming tools needed on his PC. The PC runs only SteamVR, ALVR and his game-streaming host.

| Part | Choice | Why | Main alternative rejected |
|---|---|---|---|
| App engine | Unity 6 LTS, Universal Render Pipeline, C# | Same stack as the reference video. It has the best AR and VR tooling on Android and a free Personal licence. | Native Android with OpenGL: much more code for the same result. |
| Tracking | AR Foundation with the ARCore plugin (6DoF visual-inertial tracking) | ARCore runs on the S23 Ultra, and its Depth API is supported on this phone ([Google ARCore devices, accessed 2026-11-03](https://developers.google.com/ar/devices)). | Cardboard SDK head tracking: rotation only (3DoF). |
| Stereo rendering | Our own stereo camera rig using Google Cardboard's open-source lens distortion maths, with per-headset lens profiles | Lets ARCore drive the head pose while keeping correct lens correction. This is the same approach as the video. | The Cardboard XR plugin as a whole: it brings its own rotation-only tracking. |
| Hand tracking | MediaPipe hand landmarks via the open-source MediaPipeUnityPlugin, GPU accelerated ([homuler/MediaPipeUnityPlugin](https://github.com/homuler/MediapipeUnityPlugin)) | 21 landmarks per hand, runs on the phone, widely used with Unity. | Building our own model: far too much work. |
| Room mesh | Scan with Scaniverse (free mesh export) and load the GLB/OBJ file into the app. Align it using two printed markers stuck to the wall at measured spots plus ARCore's floor detection, then build physics colliders and the boundary from the mesh. Live warnings use the ARCore Depth API. | Without LiDAR, photo scanning gives the best detail. The markers make alignment repeatable every session, which is what "must nail" needs. | Scanning inside our app with ARCore depth: lower detail. It stays as a fallback. |
| PC VR streaming | ALVR server on the PC (SteamVR) plus the PhoneVR client on the phone. PhoneVR is open source and supports ALVR v20.8.0 ([PhoneVR README, accessed 2026-11-03](https://github.com/ShootingKing-AM/PhoneVR/blob/master/README.md)). Its docs don't mention 6DoF, so we expect to fork it and feed it ARCore head poses. | It's the only maintained open-source phone client for ALVR we found. | Commercial phone streamers: closed source, so we can't add 6DoF or foveation. |
| 2D game streaming | Phase A uses SteamVR's desktop or theatre view through the ALVR stream, so there's nothing new to build. Phase B, only if A's latency or quality falls short, builds a VR virtual screen based on Nightjar's open-source streaming pipeline, so it talks to Rubylight with its full feature set ([Nightjar v4.0.0 notes, accessed 2026-11-03](https://newreleases.io/project/github/RamazanKara/nightjar/release/v4.0.0)). Its licence is checked first. Moonlight-family clients are usually GPL, which would require our app's source to stay open if we ever distribute it. | Phase A gets a result quickly, and B is the quality ceiling. Nightjar itself has no VR mode. | Running Nightjar inside the headset: no stereo or VR support. |
| Media | Jellyfin's web API for browsing, plus the Android hardware video decoder for 4K HEVC and AV1. It plays on a virtual cinema screen with side-by-side and top-bottom 3D modes. | Uses the phone's hardware decoder to save battery. | Unity's built-in VideoPlayer alone: weaker 4K, HDR and codec support. A first ticket tests this. |
| Thermal governor | Small Android (Java) plugin reading Android's thermal status, called from C#. It steps resolution, frame rate, hand-tracking rate and later foveation. | Acts before Android throttles the phone. | Waiting for Android: it throttles hard and suddenly. |
| Updates | On launch, the app checks for a newer release. If there is one, it shows a prompt, downloads the APK and hands it to Android's installer. Every merge to `main` publishes a new release automatically. Builds are signed with one permanent release key, kept as a GitHub secret with a backup in Carlos's private folder, because Android only accepts updates signed with the same key. **Deferred (decided 2026-11-03):** for now Carlos downloads each release from GitHub by hand. The in-app updater is built when the repository goes public. Signing and releases are set up from the start so that switch is easy later. | Keeps testing smooth, the way Rubylight and Nightjar update themselves. | Hosting updates now: needs a public releases repository or a PC update server, which Carlos declined for now. |
| Builds | GitHub Actions with the free GameCI Unity builder. Each pull request produces an APK. | Carlos needs no Unity install. If needed, Unity on his PC is the fallback for hands-on debugging. | Building only on his PC: needs tooling knowledge he doesn't have yet. |

**Repository layout**
- `app/` is the Unity project.
- `pc/` holds ALVR, SteamVR and streaming-host settings and helper scripts.
- `hardware/` holds CAD files, print profiles, wiring diagrams, lens profiles and the bill of materials.
- `docs/` holds the kickoff documents, HANDOFF.md and decisions.md.

**Private data stays out of the repository.** Room scans, room measurements, marker positions, the budget and receipts, and the test content list never go into git, so the repo can go public later without cleanup.
- **On the phone:** the app keeps its working copy in its own storage, and scans are imported from the phone's Downloads folder.
- **On the PC:** the backup and master copy live in `C:\Claude Code Projects\DIY VR Smartphone Headset\private\`, outside the git clone.
- **Safety net:** `.gitignore` also blocks scan file types (`*.glb`, `*.obj`, `*.ply`, `*.spz`) and any `private/` folder.

**How core logic is verified:** automated tests for the maths (pose smoothing, lens distortion, mesh alignment, thermal steps), checked against reference values. Tape-measured room points (kept in the private folder) check the room mesh. A manual acceptance test runs in the headset for every milestone.

**Security and privacy:** camera images and hand data never leave the phone. Room scans and other personal data stay on Carlos's phone and PC, never in the repository. Streaming stays on the home network. Secrets (the Unity login) live only in GitHub's encrypted repository secrets.

## 3. Milestones and tickets

| # | Milestone | Goal | Owner's acceptance test | Est. sessions |
|---|---|---|---|---|
| 1 | Foundation | Unity app builds automatically, renders stereo with 6DoF ARCore tracking, and has a lens profile for the test headset | Install the APK, walk around the virtual cube, and check that grid lines look straight (rows 1–2 of the feature table) | 6–8 |
| 2 | Hands | Gaze-and-pinch interaction and placing objects | Rows 3–4 of the feature table | 4–6 |
| 3 | Room mesh (must nail) | Scan-and-Load, marker alignment, boundary, colliders, live depth warnings, passthrough peek | Rows 5–6 of the feature table, using the reference-room measurements | 6–10 |
| 4 | Cinema | Jellyfin browser and 2D/3D player | Row 7 of the feature table | 4–6 |
| 5 | PC VR | PhoneVR plus ALVR baseline, then a fork with ARCore 6DoF poses | Row 9 of the feature table | 6–10 |
| 6 | 2D game screen | Phase A, then Phase B only if needed | Row 10 of the feature table | 2–8 |
| 7 | Thermal and endurance | Thermal governor, plus the 3-hour session test | Rows 8 and 11 of the feature table | 3–4 |
| H1 | v1 hardware (Carlos-led, Claude guides) | Printed shell adapted from the video's files, lenses, eye spacing, strap; new lens profile | All v0 acceptance tests pass again in the printed headset, and it's more comfortable than the test headset | Guides only |
| 8 | v2 eye tracking | IR camera pipeline and gaze calibration | Gaze dot follows your eyes to within a few degrees | TBD |
| 9 | v2 foveation | Foveated rendering in the app and foveated streaming over ALVR | Same image quality where you look at a lower bitrate | TBD |
| H2 | v2 electronics (Carlos-led, Claude guides) | Hub, fans, Peltier cooler, IR LEDs, DAC, battery | Safety checklist passed, then a 3.5-hour session with cooling running | Guides only |
| 10 | v3 controllers | DIY 6DoF controllers | TBD when v2 is done | TBD |
| 11 | v3 haptic gloves | OpenGloves or SenseShift gloves, with finger tracking and haptics in our app and SteamVR | TBD when v2 is done | TBD |

The order of milestones 4–6 can change. Carlos's baseline test (Playbook 2.2) can move PC VR earlier if streaming works well out of the box.

**Milestone 1 tickets**

| Ticket | Goal | Acceptance criteria | Tier | Depends on |
|---|---|---|---|---|
| 1.1 Repo skeleton | Folders, CLAUDE.md, .gitignore and Git LFS for Unity, docs/HANDOFF.md, docs/decisions.md | Repo matches the layout in section 2, and CLAUDE.md lists the build and test commands | Haiku | - |
| 1.2 Unity project | Unity 6 LTS project with URP, Android settings (ARM64, Vulkan, 120 Hz requested), AR Foundation and ARCore installed | Project opens and compiles in CI | Sonnet | 1.1 |
| 1.3 CI build | GameCI GitHub Action builds a signed debug APK on every pull request and attaches it | A pull request shows a downloadable APK, and Carlos installs it | Sonnet | 1.2 + Carlos's Unity secrets |
| 1.4 Stereo rig | Two-eye camera rig with Cardboard distortion maths, eye spacing setting (default 66.5 mm), landscape lock, no screen dimming | Test grid shows on both halves, and unit tests cover the distortion maths | Sonnet | 1.2 |
| 1.5 6DoF head pose | ARCore pose drives the rig, with pose prediction and smoothing | Virtual cube stays fixed while walking, and the smoothing has unit tests | Sonnet (Opus if jitter persists) | 1.4 |
| 1.6 Lens profile tool | In-headset screen to adjust distortion and field of view, saved per headset | Carlos tunes the test headset in under 10 minutes, and the profile saves to a file | Sonnet | 1.4 |
| 1.7 Debug overlay | FPS, tracking state, thermal status, build version | Overlay visible, and can be toggled | Haiku | 1.5 |
| 1.8 Passthrough toggle | Show the camera view when tracking is lost or on demand | Toggle works, and the view appears automatically when tracking is lost | Sonnet | 1.5 |
| 1.9 Release signing and publishing | Permanent release key as a GitHub secret. Every merge to `main` builds a signed APK and publishes it as a numbered GitHub release, which Carlos downloads by hand | A merge produces a new release with an incrementing version number | Sonnet | 1.3 |

Later milestones get their ticket lists when they start.

## 4. Operating model
### Principle
Nothing important lives only in a chat. Plans, decisions, progress and handoff notes live in files and tickets, so any session can be thrown away and replaced by a fresh one at any time. Long sessions cost more per step and get sloppier, so we restart them on purpose.

### Roles

| Role | Who | Does | Never does |
|---|---|---|---|
| Owner and validator | The owner | Sets priorities, approves epics, tests builds on reference cases, talks to customers | Writes code |
| Project manager (PM) | Opus, main conversation / project coordinator | Scopes epics into tickets, briefs workers, reviews every diff, checks numbers against references, merges, keeps HANDOFF.md current | Writes feature code, runs long tool chains, reads files in bulk |
| Builder | Sonnet worker (subagent or thread) | One ticket at a time: code, tests, opens a pull request, posts a report | Merges its own work, spawns further agents, widens scope |
| Helper | Haiku worker | Lookups, renames, formatting, translation files, dependency bumps, shell chores | Calculation or business-logic code |
| Escalation | Opus worker (subagent or thread) | A ticket a builder failed twice, given both failure reports | Anything not escalated |

### Model routing
- Cheapest tier that can do the job: Haiku for mechanical work, Sonnet for normal feature work, Opus only for hard debugging, architecture, or escalations.
- On failure go up one tier. The PM never fixes a failed ticket inline.
- Effort: medium by default. High only for escalations and calculation-critical code.

### Concurrency on Pro
- One PM. At most two builders at once. Never two workers on the same files.
- One epic at a time. No "ticket fanning" (sending many small parallel tickets).
- On Max plans these limits can be raised; the Build Plan says when.

### Ticket workflow
1. PM writes the ticket: goal, acceptance criteria, files likely touched, test to prove it, model tier.
2. Builder works on its own branch, commits and pushes work in progress often.
3. Builder opens a pull request with a report: what changed, how it was tested, results, open questions.
4. PM reads the diff, re-runs or checks the numbers, then merges or sends it back. Workers never merge.
5. Every bug fix states why the bug got through and closes that gap (a test, a check, a rule).

### Context and handoffs
- Builders: at about 50 tool calls, stop at the next safe point, push, and post a handoff note on the ticket; at about 60, stop immediately. A fresh builder continues from the note.
- Handoff note format: branch and last commit, what is done, what is in progress, ordered remaining steps, scratch files, gotchas. Self-contained: a stranger could continue from it.
- Every worker brief includes: "You are the executor. Do not spawn sub-agents. Follow the handoff rule."
- PM: keeps `docs/HANDOFF.md` current (state per ticket, pending decisions for the owner, next steps). In a Claude Code Project the coordinator manages its own context; elsewhere, when the session gets long, the PM writes HANDOFF.md and a fresh session resumes from it.
- New work goes to a fresh worker rather than reviving an old idle one (an idle session re-reads its whole history first).

### Saving usage
- No idle polling: long waits are blocking calls with timeouts, not "check back later" turns.
- Workers stop watching a pull request once it is merged or handed back.
- CLAUDE.md and instructions stay short and point to docs instead of repeating them.
- Memory holds only facts that change future behavior: one fact per entry, no status logs.
- Answers first, no preamble, no recaps.

### Where things live
- `docs/` - the three kickoff documents, HANDOFF.md, decisions log.
- Tickets (GitHub issues, or TICKETS.md) - the long-term memory of the work.
- `CLAUDE.md` - short project rules and pointers.

**Project-specific stricter rules**
- **Hardware safety first.** No electronics guide asks Carlos to power anything until he has sent photos and Claude has checked the wiring. IR LED circuits need current-limiting resistor values calculated and shown, with the eye-safety reasoning, before they're built. Batteries are always protected, rated power banks, never bare cells.
- **Guides for a beginner.** Every task Carlos does on his PC, phone or workbench gets numbered steps, with no unexplained jargon and a "how you know it worked" check.
- **Licences.** Before reusing any open-source code or print files (PhoneVR, ALVR, MediaPipe plugin, Cardboard, the video's files), the PM records the licence in docs/decisions.md.

## 5. Usage plan for the Pro plan
This plan assumes a Pro plan; tell Claude if you're on another plan and this section will be adjusted.
- **One-off:** kickoff, including the targeted research, is already spent.
- **Per milestone:** roughly the session estimates in section 3. One milestone at a time, with at most two builders running at once.
- **Rhythm:** no fixed schedule until the printer arrives. Work happens when Carlos is available to test builds, because each milestone ends with a test in the headset. Heavy build sessions go on days he can test the same day or the next.
- **When a limit is hit:** stop, write the handoff note, and resume after the reset. Never switch to a bigger model to push through.
- **Warning signs:** work stopped at the limit in two or more weeks out of four. That feeds the adapted upgrade rule in the Founder Playbook, section 5.

## 6. Testing and quality gates
- **Automated:** Unity EditMode tests for all maths and logic (distortion, pose smoothing, mesh alignment, boundary generation, thermal steps). They run in CI on every pull request.
- **Reference cases:**
  - The tape-measured room points (docs/reference-room.md) are the ground truth for the room mesh.
  - The test content list (docs/test-content.md) sets the movies and games used for streaming and playback tests.
- **Manual:** each milestone's acceptance test is run by Carlos in the headset. Claude sends a short checklist with each build.
- **Ticket done:** tests pass in CI, an APK is built, the PM has reviewed the diff, and the PR report lists what Carlos should look at.
- **Milestone done:** Carlos has run the acceptance test and reported a pass, and the result is recorded in docs/HANDOFF.md.
- **Before every merge, the PM checks:** CI is green, there's no new permission or network access that isn't explained, camera and hand data stay on the device, and no secrets are in the code.

## 7. Release and delivery
- **Getting builds:** every merge to `main` publishes a numbered GitHub release. Carlos downloads the APK on the phone and installs it over the old version, and his settings are kept because every build uses the same signing key. APKs from pull requests are available for testing a change before it's merged. An in-app updater comes when the repository goes public.
- **Versions:** v0.x during the test-headset phase, v1.x with printed hardware, v2.x with electronics. Each merged milestone gets a tagged GitHub release with an APK and short notes.
- **PC side:** setup steps and settings files live in `pc/`.
- **Hardware:** releases include the print files and the bill of materials.
- **Checklist:** see the Founder Playbook, section 4, under "Per release".

## 8. Risks and mitigations

| Risk | Mitigation |
|---|---|
| PhoneVR is head-rotation only, or hard to extend for 6DoF | Spike ticket at the start of milestone 5. Fallback: build a minimal ALVR client inside our Unity app. ALVR ships its client core as a library. |
| ARCore tracking jitter or latency causes motion sickness | Pose prediction and smoothing in ticket 1.5, with an Opus escalation path. Lock camera exposure, keep 120 Hz, and measure with the debug overlay. |
| Camera conflict: ARCore and MediaPipe both need the rear camera | Use AR Foundation's CPU image access, so MediaPipe reads ARCore's frames and doesn't open the camera a second time. Proved in an early milestone 2 ticket. |
| Room scans from photos miss glass or plain walls | Live ARCore depth warnings. Carlos can also mark walls manually in the app. Re-scan guide. |
| Mesh alignment drifts between sessions | Two fixed printed markers, re-aligned on every app start. The acceptance test demands 5 starts in a row within 3 cm. |
| Heat: ARCore, MediaPipe and rendering together throttle the phone | Thermal governor (milestone 7) lowers the hand-tracking rate first. The v2 Peltier cooler adds headroom. |
| Battery: the phone alone gives an estimated 2–3 hours | Pocket power bank on a cable until v2 adds the external battery. |
| Test headset lenses are poor | Lens profile tool. The test headset is only a stepping stone; v1 uses aspheric lenses. |
| Wi-Fi: the AT&T gateway may lack 6 GHz | Router check task; 5 GHz fallback; Deco BE63 mesh planned. |
| Unity licence in CI | Unity Personal works with GameCI. Fallback: build on Carlos's PC with a step-by-step guide. |
| Licence terms of reused files or code | Licence check rule in section 4. |
| Update hosting: the code repository is private, so the app can't download its releases without a login | Auto-updates deferred until the repository goes public (decided 2026-11-03). Never bake a GitHub token into the app. |
| Personal data leaking into the repository | Private folder outside the clone, `.gitignore` rules, and the PM checks every diff for scan or measurement files. |

## 9. Project setup checklist (done at "let's start")
- [ ] Repository has `CLAUDE.md` and `.claude/agents/` (from the starter template, or added)
- [ ] CLAUDE.md filled in: stack (Unity 6 LTS, URP, AR Foundation, ARCore), build command (CI via GameCI), test command (Unity EditMode tests), paths (`app/`, `pc/`, `hardware/`, `docs/`)
- [ ] Unity secrets added to the repository by Carlos
- [ ] Release signing key created and stored as a secret, with a backup in the private folder
- [ ] `.gitignore` rules for private data added, and `private\` folder created on the PC
- [ ] Milestone 1 tickets created as GitHub issues
- [ ] docs/HANDOFF.md initialized
- [ ] Project settings checked: thread model Sonnet, effort medium, at most two threads at once
