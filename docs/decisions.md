# Decisions

One line each, newest first. Only decisions that change future work.

| Date | Decision | Why | Decided by |
|---|---|---|---|
| 2026-11-09 | Display refresh: stay at a stable 60 Hz for now; revisit in Milestone 7 (thermal) | Four attempts (#25, #27, #29, #31) did not hold 120 Hz while ARCore runs; the S23 Ultra drops to 60. 60 Hz with pose prediction is the norm for phone VR, and rendering two eyes at 120 would double GPU load and heat against the 3-hour session goal | Claude (builder, ticket 1.21) |
| 2026-11-03 | Up to 3 builders at once (overrides the build plan's limit of 2); still never on the same files, one epic at a time | Owner wants faster progress within the same review process | Carlos |
| 2026-11-03 | Android graphics API is OpenGLES3, not Vulkan | ARCore does not support Vulkan; its build step rejects it | Claude (builder, ticket 1.10) |
| 2026-11-03 | Reuse Google Cardboard SDK distortion maths (googlevr/cardboard, Apache License 2.0 for the SDK files; only its third_party/unity_plugin_api folder differs and is not used; keep attribution in CardboardDistortion.cs) | Proven lens model for the stereo rig (1.4); no code copied verbatim, formulas re-implemented in C# | Claude (builder, ticket 1.4) |
| 2026-11-03 | Start build before test headset arrives; headset-dependent tickets (lens tuning, comfort) wait | Most of milestone 1 is testable handheld | Carlos |
| 2026-11-03 | No in-app auto-updater until repo is public; manual APK install from GitHub releases; consistent signing key from day one | Private repo can't serve releases without embedding a secret | Carlos |
| 2026-11-03 | Private data (room scans, measurements, budget, test content, key backup) never in git; phone app storage + PC `private\` folder outside the clone | Repo may go public later | Carlos |
| 2026-11-03 | 2D game streaming: Phase A SteamVR desktop view via ALVR; Phase B VR client based on Nightjar (licence check first, likely GPL) | Fast result first; Nightjar already speaks Rubylight | Carlos |
| 2026-11-03 | PC VR via ALVR + PhoneVR; fork PhoneVR to inject ARCore 6DoF poses if needed | Only open phone ALVR client found | Carlos |
| 2026-11-03 | Room mesh: Scaniverse scan + two printed wall markers for alignment + ARCore Depth live warnings; must be within 3 cm, 5 starts in a row | No LiDAR; room-aware games depend on it | Carlos |
| 2026-11-03 | Builds via GitHub Actions + GameCI; owner installs APKs, no Unity on PC needed | Owner is not a developer | Carlos |
| 2026-11-03 | Stack: Unity 6 LTS, URP, AR Foundation + ARCore, own stereo rig with Cardboard distortion maths, MediaPipeUnityPlugin | See Build Plan section 2 | Carlos |
| 2026-11-03 | Phasing: v0 software on cheap headset → v1 printed shell → v2 eye tracking/foveation/cooling/battery → v3 controllers + haptic gloves | Spend little now; printer later | Carlos |
| 2026-11-09 | Licence check: MediaPipeUnityPlugin is MIT; it bundles Google MediaPipe (Apache 2.0). Both allow use in a closed or open app with attribution | Required before reusing code (CLAUDE.md) | Claude (PM) |
| 2026-11-09 | Hand tracking spike (2.1): MediaPipeUnityPlugin v0.16.3 (MIT, Unity 6 support, 16 KB page fix) plus Google's hand_landmarker.task float16 model v1 (Apache 2.0, as is MediaPipe). Plugin .tgz (290 MB) and model are downloaded in CI by scripts/fetch-mediapipe.sh with SHA-256 checks, not committed. libc++_shared.so is added by the plugin's own mainTemplate.gradle (MIT) | Git LFS bandwidth quota rules out committing the 290 MB package; the hash check pins the exact files | Claude (builder) |
