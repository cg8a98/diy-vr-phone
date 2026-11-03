# DIY Smartphone VR Headset

Owner: Carlos, solo maker, not a developer (knows SolidWorks), Claude Pro plan assumed. Answer first, plain language, no recaps. Every task he does gets numbered beginner steps and a "how you know it worked" check.

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
- Target: Samsung Galaxy S23 Ultra (Android, ARCore + Depth API), landscape stereo in a phone VR headset. IPD default 66.5 mm.
- Stack: Unity 6 LTS, URP, C#, AR Foundation + ARCore, own stereo rig using Google Cardboard distortion maths, MediaPipeUnityPlugin (hands). PC side: SteamVR + ALVR, PhoneVR client.
- Paths: `app/` Unity project · `pc/` PC configs/scripts · `hardware/` CAD, print profiles, wiring, BOM · `docs/` plans and handoff.
- Build: GitHub Actions + GameCI builds an APK on every PR; merges to `main` publish a numbered, signed release. Unity cannot be opened in the cloud container, so CI is the compiler.
- Test: Unity EditMode tests (run in CI) for all maths/logic; owner runs each milestone's manual acceptance test from the Build Plan.
- Reference cases: room tape measurements and test content live in the owner's PRIVATE folder on his PC (never in git); ask him for values.
- Privacy: never commit room scans, measurements, budget, receipts or keys. Camera and hand data never leave the phone.
- Safety: no electronics step is powered on before the owner's wiring photos are reviewed; IR LED resistor values are calculated and shown.
- Licences: record the licence of any reused code or print files in docs/decisions.md before using it.
