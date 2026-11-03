# Founder Playbook: DIY Smartphone VR Headset

2026-11-03 · For Carlos · Status: agreed 2026-11-03

## 1. How to use this playbook
This is your "Maker Playbook": every task that only you can do, from buying parts to soldering. Tasks marked **Blocking** must be done before coding starts. When they are, you say "let's start". Hardware tasks for v1–v3 are listed too, so you can buy parts gradually; Claude will walk you through each one step by step when it comes up. Every task uses the same format:

> **Task name** - Blocking / Before v1 / Before v2 / Before v3 / Ongoing
> What: … Why: … How: numbered steps … Time: … Cost: … (verify current price) · Done when: …

Prices marked "Estimate" are rough guesses from typical listings, not quotes. Check before buying.

## 2. Before coding can start (Blocking)

### 2.1 Accounts and tools

> **GitHub two-factor authentication** - Blocking
> What: turn on 2FA for your GitHub account (cg8a98). Why: Claude will push code to your repository, so the account should be well protected. How: 1. github.com → your avatar → Settings → Password and authentication. 2. Enable two-factor authentication with an authenticator app. 3. Save the recovery codes in a safe place. Time: 10 min. Cost: free. · Done when: GitHub shows 2FA as enabled.

> **Git for Windows** - Blocking
> Done (Carlos confirmed Git is installed, 2026-11-03).

> **Repository ready for Claude** - Blocking
> Done: verified 2026-11-03. The repo is private and came from the starter template (`CLAUDE.md` and `.claude/agents/` present).

> **Claude GitHub App on the repository** - Blocking
> Done: verified 2026-11-03 (Claude cloned the repository with write access).

> **Claude Code Project settings** - Blocking
> What: this project already exists. Check its settings: thread model Sonnet, effort medium, at most two threads at once. Why: keeps usage low; Opus is used only for hard problems. How: open Project settings and check those three values. Time: 5 min. Cost: included in your plan. · Done when: the settings match.

> **Unity account (free Personal licence)** - Blocking (account created 2026-11-03; repository secrets added at "let's start")
> What: a free Unity ID. Why: the headset app is built with Unity. The app is compiled automatically on GitHub (no installs needed on your PC), and that build service needs a Unity licence. How: 1. Create an account at unity.com. 2. Accept the Personal licence. 3. At "let's start" Claude will guide you to add your Unity login as encrypted secrets in the repository (you type them into GitHub; Claude never sees them). Time: 15 min. Cost: free for personal use (verify current Personal licence terms). · Done when: you can sign in to Unity and the repo secrets are added.

> **Phone developer mode and app installs** - Blocking (USB debugging done 2026-11-03; "Install unknown apps" happens at the first install)
> What: allow your S23 Ultra to install test apps. Why: every build arrives as an APK (Android app file) you install directly. How: 1. Settings → About phone → Software information → tap "Build number" 7 times. 2. Settings → Developer options → turn on "USB debugging". 3. The first time you open a downloaded APK, allow your browser or Files app to "Install unknown apps". Time: 5 min. Cost: free. · Done when: "Developer options" appears in Settings.

> **Install SteamVR** - Blocking (done 2026-11-03)
> What: SteamVR on your PC. Why: needed for PC VR streaming and the baseline test in 2.2. How: in Steam, search "SteamVR" → Install. Time: 10 min. Cost: free. · Done when: SteamVR launches (it will say no headset found, which is fine).

> **Password manager** - Not applicable - no customer accounts or shared credentials in a personal project. Use whatever you already use for your GitHub and Unity logins.

> **Separate business email** - Not applicable - no customers yet. Revisit after v2.

### 2.2 Validate before building
Customer interviews are not applicable (personal project). Instead, a baseline test tells us what "better than off-the-shelf" means.

> **Buy the test headset** - Blocking
> What: a cheap phone VR headset. Why: lets software work start long before you have a printer, and gives a baseline your own design must beat. How: 1. Look for a listing that says it fits **5–7 inch** phones. 2. It must have IPD and focus knobs and a head strap. 3. **Most important:** the front cover must come off or have a window, so the rear cameras can see out. 4. A bundled Bluetooth controller is a bonus. Time: 15 min to order. Cost: Estimate $15–25. · Done when: your S23 Ultra fits inside and the main camera has a clear view out of the front.

> **Baseline test with existing apps** - Blocking
> What: try the headset with existing software before we write any. Why: shows comfort, lens quality and streaming quality as a starting point, and tells us whether head-rotation-only PC VR causes motion sickness for you. How: 1. Install a Google Cardboard demo app and look around for 10 minutes. 2. Install PhoneVR (an ALVR-based phone client; Claude will send links and matching versions) and ALVR on the PC, then try one SteamVR game for 10 minutes. 3. Write down: comfort, blur at the edges, light leaks, heat, any nausea. Claude records the results in `docs/decisions.md`. Time: 1–2 h. Cost: free. · Done when: notes are sent to Claude.

> **Router check** - Before PC VR milestone (not blocking for milestone 1)
> What: find out whether your AT&T gateway has a 6 GHz (Wi-Fi 6E) band. Why: PC VR and 2D game streaming need a fast, quiet wireless link; 5 GHz can work, 6 GHz is better. How: 1. Read the model name on the gateway's label and send it to Claude. 2. Claude tells you whether it has 6 GHz and the best settings. 3. If not, the planned Deco BE63 mesh solves it. Time: 10 min. Cost: free (mesh: Estimate $300–450, optional). · Done when: we know which band the phone will use.

### 2.3 Inputs only the owner can provide

> **Sign-off on scope** - Blocking
> What: confirm the Project Brief (draft 4) and the Build Plan milestones. Why: everything is planned from them. How: reply "agreed" or list changes. Time: 20 min. Cost: free. · Done when: you have agreed both.

> **IPD measurement** - Blocking
> Done: 66–67 mm (2026-11-03). Default lens spacing is 66.5 mm.

> **Private folder** - Blocking
> What: a folder for personal project data, outside the git clone: `C:\Claude Code Projects\DIY VR Smartphone Headset\private\`. Why: room scans, measurements, receipts and the release-key backup must never reach the repository, so it can go public one day. How: Claude creates it through the folder access you granted, at "let's start". Time: none. Cost: free. · Done when: the folder exists and the git clone lives in a separate subfolder.

> **Update hosting choice** - Done 2026-11-03: auto-updates are skipped for now. You download each release from GitHub by hand, and we revisit when the repository goes public.

> **Room reference measurements** - Before room-mesh milestone
> What: real tape-measure distances in the room you'll play in. Why: the room-mesh success criterion is "lines up to within a few centimetres", and we need ground truth to check that. How: 1. Pick one room. 2. Measure 5 things: two wall-to-wall distances, ceiling height, and the length and height of a table or desk. 3. Take a photo of each measurement. 4. Send them to Claude. Time: 30 min. Cost: free. · Done when: 5 measurements with photos are in `private\reference-room.md` (not in the repository).

> **Test content list** - Before media and streaming milestones
> What: one SteamVR game, one flat PC game, one 2D movie and one 3D movie (side-by-side format) you'll use for every test. Why: testing with the same content each time makes improvements measurable. How: pick them and tell Claude; for 3D, use legally owned content. Time: 30 min. Cost: depends on content. · Done when: the list is in `private\test-content.md`.

> **Room scan** - Before room-mesh milestone
> What: scan your room with a phone scanning app. Why: the detailed mesh for boundaries and room-aware play. How: 1. Install Scaniverse (its free on-device mode includes mesh export, per [SkyeBrowse, accessed 2026-11-03](https://www.skyebrowse.com/news/posts/polycam-vs-scaniverse.md)). 2. Claude sends a scanning guide (route, speed, lighting). 3. Export the mesh. Copy it to the phone's Downloads folder for the app to import, and back it up to the `private\` folder on your PC. Time: 1 h. Cost: free. · Done when: the mesh is on the phone and backed up on the PC, and is not in the repository.

**Hardware path (v1–v3).** Each of these gets a detailed guide from Claude when its time comes, with photos to send back before anything is powered on.

> **3D printer** - Before v1
> What: Bambu Lab P1S with AMS 2 Pro (your plan). Why: prints the headset shell. How: Claude will suggest the print profile (PETG or PLA+, 0.16–0.20 mm layers, 4–5 perimeters). Time: setup about 2 h. Cost: Estimate, check Bambu's store. · Done when: a calibration print comes out clean.

> **Adapt the video's print files** - Before v1
> What: get brenpoly's open print files and adapt them to the S23 Ultra in Fusion 360 or SolidWorks. Why: saves modelling from scratch. How: 1. Download the files from the video's GitHub link. 2. Check the licence allows modification (Claude will read it with you). 3. Claude gives you the S23 Ultra dimensions, camera and button positions, lens spacing (58–72 mm, default 66.5 mm), and a parts layout. 4. Model, print, test fit. Time: several sessions. Cost: filament, Estimate $20. · Done when: the phone seats firmly with the main camera clear and the lenses centred on your eyes.

> **v1 parts: lenses and strap** - Before v1
> What: 37 mm or 42 mm aspheric lenses, a rigid strap. Cost: Estimate $10 lenses, $25 strap (from the brief). · Done when: parts are in hand and fit the printed shell.

> **Soldering kit and safety gear** - Before v2
> What: temperature-controlled soldering iron, solder, flux, multimeter, safety glasses, fume extraction or a fan. Why: v2 wiring (fans, IR LEDs, USB hub). How: Claude will send a specific shopping list and a practice exercise before any real part is soldered. Time: 30 min to order. Cost: Estimate $40–70. · Done when: you've completed the practice exercise.

> **v2 parts: power, cooling, eye tracking, audio** - Before v2
> What: 10,000 mAh USB-PD power bank, bare-board USB-C hub, cabling, bulkhead port, Black Shark magnetic Peltier cooler, 4 micro blower fans, 2 IR cameras, 4 IR LEDs with resistors, USB-C DAC, IEMs. Why: the v2 upgrades. How: buy gradually; Claude checks each part's specs against the safety rules first. Cost: Estimate about $145 total from the brief's list. · Done when: each part is checked and in hand.

> **v3 parts: controllers and haptic gloves** - Before v3
> What: parts for DIY 6DoF controllers (for example HadesVR or ESP32-based) and haptic gloves (OpenGloves or SenseShift firmware). Why: the v3 goals. How: Claude will research current open designs and send a parts list when v2 is done. Cost: to be estimated at that point. · Done when: parts list agreed.

### 2.4 Budget and time

> **Spending caps** - Blocking
> What: v0 cap about $30 (test headset). Total soft target about $180 for v1+v2 parts, slightly over is OK; printer and tools are separate. Why: you want to spend as little as possible now. How: use one card for all project purchases and keep receipts in a folder; Claude keeps a running total in `private\budget.md`. Time: 5 min. Cost: none. · Done when: you've confirmed the caps.

> **Time** - Ongoing
> What: no fixed hours until the printer arrives. Why: no deadline. How: work whenever you like; once the printer arrives, we agree weekly hours and a timeline. Time: none. Cost: none. · Done when: revisited at "printer arrived".

## 3. Before launch

### 3.1 Business and legal
Not applicable - personal project. Revisit after v2 (open source vs. selling print files vs. a product). Your day job is unrelated, so no employer IP issue (confirmed 2026-11-03). One item applies now: respect the licences of the video's print files and any open-source code we reuse. Claude checks each licence before we use it.

### 3.2 Getting paid
Not applicable - personal project. Revisit after v2.

### 3.3 Distribution
Not applicable - apps are installed directly on your phone from build files; no app store. Revisit after v2.

### 3.4 Launch and sales
Not applicable - personal project. Revisit after v2.

## 4. Ongoing routines

### Daily (10-20 min, on work days)
On days you work on the project: check the thread for anything "waiting on you", answer questions, and approve or reject merges Claude recommends.

### Weekly (about 1 hour, fixed day)
Only once a schedule is set (after the printer arrives): review progress against the current milestone, install and test the latest build, check your Claude usage, and agree the next milestone.

### Monthly
Check spending against `private\budget.md`. Not applicable until then: revenue, customers, taxes.

### Per release
Install the new APK, run that milestone's acceptance test from the Build Plan, and tell Claude what passed or failed. For hardware: send photos of wiring before the first power-on.

## 5. When to upgrade the Claude plan
The standard rule (upgrade only when revenue covers it and limits delay the work for two months in a row) does not fit a no-revenue personal project. Adapted rule: stay on your current plan. Consider upgrading only if usage limits stop work in at least two weeks out of four for two months in a row, and the extra cost is worth it to you. Record the decision in `docs/decisions.md`.

## 6. Decision rights
**You decide:** priorities, scope changes, purchases, anything that touches safety (power-on of new electronics, IR LEDs near the eyes, battery choices), and whether to merge risky changes.
**Claude decides:** how work is split into tickets, which model does them, technical approach within the agreed stack, and when to raise a risk with you.

## 7. Founder checklist summary

| Task | Category | When | Time | Cost | Status |
|---|---|---|---|---|---|
| GitHub two-factor authentication | Accounts | Blocking | 10 min | Free | Done |
| Git for Windows | Accounts | Blocking | - | Free | Done |
| Repository ready for Claude | Accounts | Blocking | - | Free | Done |
| Claude GitHub App on repository | Accounts | Blocking | 5 min | Free | Done |
| Claude Code Project settings | Accounts | Blocking | 5 min | Included | Not started |
| Unity account | Accounts | Blocking | 15 min | Free | Done (secrets at "let's start") |
| Phone developer mode | Accounts | Blocking | 5 min | Free | Done |
| Install SteamVR | Accounts | Blocking | 10 min | Free | Done |
| Private folder | Accounts | Blocking | - | Free | At "let's start" |
| Update hosting choice | Accounts | Blocking | - | Free | Done (deferred) |
| Buy test headset | Validate | Blocking | 15 min | Est. $15–25 | Not started |
| Baseline test with existing apps | Validate | Blocking | 1–2 h | Free | Not started |
| Sign-off on scope | Inputs | Blocking | 20 min | Free | Done 2026-11-03 |
| IPD measurement | Inputs | Blocking | - | Free | Done |
| Spending caps | Budget | Blocking | 5 min | - | Not started |
| Router check | Validate | Before PC VR milestone | 10 min | Free | Not started |
| Room reference measurements | Inputs | Before room-mesh milestone | 30 min | Free | Not started |
| Room scan | Inputs | Before room-mesh milestone | 1 h | Free | Not started |
| Test content list | Inputs | Before media milestone | 30 min | Varies | Not started |
| 3D printer | Hardware | Before v1 | 2 h | Est., check store | Not started |
| Adapt the video's print files | Hardware | Before v1 | Several sessions | Est. $20 filament | Not started |
| v1 parts: lenses and strap | Hardware | Before v1 | - | Est. $35 | Not started |
| Soldering kit and practice | Hardware | Before v2 | 30 min + practice | Est. $40–70 | Not started |
| v2 parts | Hardware | Before v2 | - | Est. $145 | Not started |
| v3 parts | Hardware | Before v3 | - | TBD | Not started |
| Time schedule | Budget | Ongoing | - | - | Revisit when printer arrives |
