# Decisions

One line each, newest first. Only decisions that change future work.

| Date | Decision | Why | Decided by |
|---|---|---|---|
| 2026-11-03 | Start build before test headset arrives; headset-dependent tickets (lens tuning, comfort) wait | Most of milestone 1 is testable handheld | Carlos |
| 2026-11-03 | No in-app auto-updater until repo is public; manual APK install from GitHub releases; consistent signing key from day one | Private repo can't serve releases without embedding a secret | Carlos |
| 2026-11-03 | Private data (room scans, measurements, budget, test content, key backup) never in git; phone app storage + PC `private\` folder outside the clone | Repo may go public later | Carlos |
| 2026-11-03 | 2D game streaming: Phase A SteamVR desktop view via ALVR; Phase B VR client based on Nightjar (licence check first, likely GPL) | Fast result first; Nightjar already speaks Rubylight | Carlos |
| 2026-11-03 | PC VR via ALVR + PhoneVR; fork PhoneVR to inject ARCore 6DoF poses if needed | Only open phone ALVR client found | Carlos |
| 2026-11-03 | Room mesh: Scaniverse scan + two printed wall markers for alignment + ARCore Depth live warnings; must be within 3 cm, 5 starts in a row | No LiDAR; room-aware games depend on it | Carlos |
| 2026-11-03 | Builds via GitHub Actions + GameCI; owner installs APKs, no Unity on PC needed | Owner is not a developer | Carlos |
| 2026-11-03 | Stack: Unity 6 LTS, URP, AR Foundation + ARCore, own stereo rig with Cardboard distortion maths, MediaPipeUnityPlugin | See Build Plan section 2 | Carlos |
| 2026-11-03 | Phasing: v0 software on cheap headset → v1 printed shell → v2 eye tracking/foveation/cooling/battery → v3 controllers + haptic gloves | Spend little now; printer later | Carlos |
