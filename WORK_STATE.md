# SPOOKYTUBER work state — revision 6

Updated 2026-09-19. The user's active priority is a polished, enjoyable SOLO production game. Read all playtest messages in SOLO_POLISH_PLAN.md, PRODUCTION_EXPANSION_PLAN.md and DEVLOG.md. Preserve the original player/hoodie with blank black screen; whole character scale is 0.88. LED faces and Co-op are deferred.

Revision 6 adds title/loading/options/pause, a detailed Blender MainCam, darkness-first hospital lighting, native URP equipment light isolation and depth-tested world signs. The next active work is deterministic procedural Hospital with exact-layout footage replay, progression-driven size/traversal, then the other three GDD locations and remaining solo systems. The playable hospital is still an authored layout; this is not the completed four-map game.

## Locations and workflow

- Canonical Git repo: `D:/Astaled_Team/Our Projects/SpookTube`, Unity folder `My project`.
- Remote: `https://github.com/AetherARTs/astaled-spooktube-projects`, branch `master`; `origin` and `github.com` point to the same repository.
- User explicitly requests regular commits and pushes. Previous source checkpoint: `262852c` (solo systems/scenes/UI), preceded by `0d6fcab` (Blender kit/fonts) and `7cf9aa6` (moving held-camera rotation).
- Warm test copy: `C:/Users/Kurak/SpookTuber-work/Unity`, reports in sibling `QA`, Windows output in sibling `Build`.
- Delivered revision 6 executable: `D:/Projects/SPOOKYTUBER/Builds/Windows/SpookTuber.exe`. Previous build is preserved in `Windows-before-v6`; SHA256 verification and measured GPU results are in `QA/build_delivery.txt`.
- Unity 6000.3.18f1: `C:/Program Files/Unity/Hub/Editor/6000.3.18f1/Editor/Unity.exe`.
- Blender: `D:/Programs/Blender/blender.exe`. Existing native character source is `ArtSource/Player/CHR_Player_Master.blend`; new environment source is `ArtSource/Environment/Solo_Environment_Library.blend`.
- References: `D:/Projects/SPOOKYTUBER/References`; GDD/plans in `D:/Projects/SPOOKYTUBER/Docs/Source` and repo docs.

## Delivered systems and limits

Read DEVLOG.md for the completed/pending split and HOW_TO_PLAY_TH.md for controls. Core new paths: three-slot equipment; real microphone and central world noise; physical gravity glove; crouch/slide/jump/low-mantle/lean; physical phone; Credit shops and three-day View contract; Bobby automatic edit/upload/playback or manual edit; larger home and hospital with pocket sliding doors.

Hospital is 37×68 m, eight clinical rooms, looping 4–5 m corridors. Home is 30×30 m plus a 16×14 m garage. The original character was not regenerated. Art is authored/stylized, not final near-realistic presentation. One enemy, one authored hospital, one source take per episode and 60 seconds per take remain current limits. The microphone is used for AI detection, not saved speech. NPCs have idle/look behavior and service interactions, not full schedules. Full vertical routes, deeper content/economy, other enemies, Co-op, long recording/multiple cameras/video export and Thai localization remain.

## Verification

- 62 SoloSystems checks; 57 Bobby production checks; 14 fresh-career/recovery checks; 4 legacy footage checks; 10 automatic handoff checks; 8 keyboard/dropdown checks; 3 real microphone checks passed.
- Continuous held-camera rotation repro passed at zero angular drift; prior moving failure was 179.95 degrees. All eight room routes passed NavMesh build checks.
- Current source reports: `QA/v5_systems_check.txt`, `v5_ui_check.txt`, `v5_microphone_check.txt`, `v5_bobby_automatic.txt`, `v5_camera_repro.txt`, `bobby_playmode_validation.txt`, `bobby_reload_validation.txt`, `legacy_take_validation.txt`.
- Windows revision 6 builds successfully with zero errors. `QA/v6_Standalone/v5_standalone_check.txt` records the visible 1600×900 GTX 1050 Ti smoke; the filenames retain the harness's v5 prefix. Menu/Pause passes 18 checks (`v6_shell_check.txt`); a rendered sign has 9,869 visible white pixels and zero behind an opaque blocker (`v6_world_text_check.txt`).
- Old revision-4 totals are historical, not rerun assertions. Its older coordinate-based CrewPlayCheck/HospitalPlayCheck routines are not the current v5 acceptance suite.

## Reproduce current checks

Use `Unity.exe -batchmode -projectPath "<test project>" -executeMethod SpookTuber.Editor.<method> -logFile "<log>"`. Methods: `BuildSolo.Build`, `SoloSystemsCheck.Run`, `SoloSystemsCheck.UI`, `SoloSystemsCheck.Hardware`, `SoloPlayCheck.CameraRepro`, `BobbyPlayCheck.Run`, `BobbyPlayCheck.RunAutomatic`, `BobbyPlayCheck.RunReload`, `BobbyPlayCheck.RunLegacy`, `CrewPlayCheck.BuildPlayer`.

The development executable accepts `--spook-smoke "<QA folder>"` for isolated-career GPU smoke/record checks. It does not run without that explicit flag. Run it in a visible game window: Windows skips backbuffer rendering for hidden windows, producing black captures and invalid FPS figures. `--spook-profile` adds diagnostic samples after stopping recording/LCD/shadows; those diagnostic toggles are not production settings.

## Preserve when continuing

- Keep canonical Packages and unrelated PlayerSettings/defines/config objects intact. Staging excludes unused Unity AI packages solely to keep builds practical; do not overwrite canonical Packages with staging manifests.
- New BuildSolo.Build is the active scene generator. Older BuildHospital.Build recreates the v3/v4 layout. Preserve manual variants before any generator run.
- Keep `Scenes/Legacy/*_v4.unity` and `*_v5.unity` plus existing meta identities for old takes. Their original camera geometry is unpacked. New live scenes use v6 content identities; scene naming and track identity affect compatibility.
- Save paths use Unity persistentDataPath. Tests assign separate QA careers/takes; never substitute test balances into shipped player data. Credit is integer hundredths; View never purchases items.
- Track editable Blender sources, FBX/textures, .meta, scripts, scene/nav/profile assets and selected QA evidence. Exclude Unity caches, builds, microphone audio, huge test careers/takes and local logs. Include font OFL notices with standalone distribution.
- Do not claim the entire GDD is complete. Continue production polish/content from the user's playtest, with Solo ahead of networking.
