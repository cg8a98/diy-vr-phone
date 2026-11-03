# Project Brief: DIY Smartphone VR Headset

*Agreed 2026-11-03 (draft 4), 2026-11-03. Sources: Carlos's brief PDF, brenpoly's video "I Built a $5 Apple Vision Pro", interview rounds 1–2, and Carlos's review of draft 2.*

## Idea
A 3D-printed VR headset that turns Carlos's Samsung Galaxy S23 Ultra into a comfortable headset for four things: PC VR gaming streamed from his PC, flat 2D PC games on a big virtual screen, long movie sessions (including 3D), and mixed-reality apps controlled by hand tracking. A scanned room mesh ties them together with safe boundaries. It improves on the video's build with better lenses, adjustable eye spacing, a real strap, and later active cooling, eye tracking and an external battery.

## Who it's for
Carlos only, for now. After v2 works, revisit open source, selling print files for other phones, or a product. Market research is deferred until then. There are no employer IP concerns.

## Platform and setup
- **Phone:** Galaxy S23 Ultra (Android). Other phones come later.
- **PC:** Windows 11, Ryzen 9800X3D, Radeon RX 9070 XT, 1 GbE wired. Steam installed; SteamVR still to install.
- **Network:** AT&T router now (Wi-Fi 6E support unknown). Planned TP-Link Deco BE63 Wi-Fi 7 mesh, which gives the phone its 6 GHz band.
- **Media:** Jellyfin server on a laptop with attached storage (4K and 1080p). 3D content to be sourced later.
- **Tools:** no printer or soldering gear yet (a Bambu P1S + AMS 2 Pro and a soldering kit are planned). SolidWorks skills; moving to Fusion 360 is optional.

## Scope
**v0, software on a cheap store-bought headset (start now):** streaming PC VR to the phone, streaming flat 2D PC games to a big virtual screen with an Xbox controller, a Jellyfin/3D movie player, and a mixed-reality app with 6DoF tracking (head position and rotation) and gaze-and-pinch hand tracking. Eye-tracking software work can start here once the IR cameras are bought.
**Room meshing and boundaries ("Scan-and-Load", a core feature across v0 to v1):** the S23 Ultra has no LiDAR, so we combine two sources.
- **Detailed scan:** scan the room once with a photo-scanning app (for example Scaniverse or Polycam), then load the mesh into the headset app and line it up with the real room.
- **Live safety net:** ARCore's camera-based depth sensing warns about obstacles that weren't in the scan.

The mesh gives automatic play-area boundaries, makes virtual objects collide with and hide behind real furniture, and offers a passthrough "peek" when you get close to something. Boundaries come to the mixed-reality app first. Carrying them into PC VR (SteamVR's boundary system) is a stretch goal, because it depends on what the streaming app supports.

**Long-term vision:** games that adapt to the scanned room or apartment, so you're in full VR but the layout matches your real space. Examples are a horror game, a tabletop game, or a shooter. This is why the room mesh needs to be accurate and reliable, not just good enough.

**v1, own printed headset:** a printed shell adapted from the video's files, aspheric lenses, adjustable eye spacing (58–72 mm), and a strap. There's no built-in battery, because nothing needs power beyond the phone yet. It runs everything from v0.
**v2, upgrades:** Peltier cooler and fans, IR eye tracking, foveated rendering and streaming (sharpest detail only where you're looking), USB audio, and the external battery that powers them. The battery goes on the strap at the back, or in a pocket on a longer cable if that balances better. Then revisit monetization.
**v3, spatial peripherals:** DIY 6DoF hand controllers (for example HadesVR or ESP32-based trackers) and haptic feedback gloves (OpenGloves or SenseShift firmware). The haptic gloves are a high-wish feature, deliberately placed after the core functions work.

## Constraints
- **Budget:** spend as little as possible now and buy parts gradually. About $180 for v1+v2 is a soft target and can go a little over. v0 costs about $20–30.
- **Time:** no deadline and no fixed hours. Set a schedule once the printer arrives.
- **Skills:** Claude writes all the software. Carlos does CAD, printing, assembly and the hands-on electronics work (soldering, wiring), with Claude guiding every electronics step in detail so he learns as he goes.

## Success criteria
1. Watch a full 3-hour movie (for example Avatar in 3D) in one sitting, comfortably and without needing to charge.
2. Play a SteamVR game streamed wirelessly, smoothly and without motion sickness.
3. Play a flat 2D PC game on a large virtual screen at the highest quality and lowest latency we can get, using an Xbox controller connected to the phone or the PC. Carlos already uses this kind of PC-to-Android streaming; the exact app is chosen during v0 testing.
4. Place virtual objects in the room and select them with gaze and pinch.
5. **(Must nail.)** A scanned room mesh loads, lines up with the real room to within a few centimetres every time, and sets the play-area boundary automatically. It must be reliable enough to build room-aware games on.

## Confirmed defaults
- **Battery and comfort:** target 3.5 hours per session. In v0 and v1 the phone runs alone. Its own battery probably gives only about 2–3 hours in VR (estimate based on a ~19 Wh battery and typical 6–10 W VR draw), so for long movies the fallback is a power bank in your pocket on a cable. The 10,000 mAh bank becomes part of the build in v2, once the fans, cooler and cameras need it. The Peltier cooler roughly doubles power use, so it runs only when the phone gets hot, or from wall power during gaming.
- **Weight:** front unit about 450 g or less, and lighter wherever we can manage it. Estimate based on the 234 g phone; Quest 3 weighs about 515 g for reference.
- **Safety rules:** IR LEDs stay within eye-safe current limits. Nothing hot touches the face. The battery always has a protected, rated bank (no bare cells). Camera images stay on the phone.
- **Look:** start from the video's print files, adapt them to the S23 Ultra, and refine the look along the way.
- **Updates:** Claude decides cadence and detail, with plain-language explanations.
- **App updates:** download each release from GitHub by hand for now. An in-app updater comes when the repository goes public (decided 2026-11-03).
- **Private data:** room scans, measurements and personal files stay on the phone and PC, never in the repository.

## Risks to check early in v0
- **6DoF tracking in PC VR:** some phone streaming apps (for example PhoneVR) may only send head rotation, not position. If so, we add ARCore position tracking ourselves (agreed).
- **Room-scan quality without LiDAR:** photo scans can miss thin or plain surfaces (glass, white walls). The live depth warnings cover those gaps.

## Measurements
- **IPD (eye spacing):** about 66–67 mm. That's near the middle of the 58–72 mm adjustment range, so the default lens spacing is 66.5 mm.
