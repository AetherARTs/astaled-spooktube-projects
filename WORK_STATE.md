# SPOOKYTUBER work state

Updated 2026-09-18. User authorizes substantial production development. Latest explicit art direction: improve the hoodie, especially sleeves; the face must be a blank black display. LED emoticons are for later.

## Git workflow — user instruction, 2026-09-18
- Commit coherent, tested work regularly and push completed checkpoints to the configured GitHub repository. The user explicitly requested pushing this production delivery and keeping Git up to date during ongoing development.
- Canonical repository: `https://github.com/AetherARTs/astaled-spooktube-projects`, branch `master`. `origin` and `github.com` currently point to the same remote.
- Track editable Blender sources, Unity assets with their .meta files, package manifests/lock, project settings, runnable checks and useful QA evidence. Keep generated builds, Unity caches, local logs and Blender backup files out of commits.
- Historical "no commit or push" notes below describe the earlier deliveries before this Git checkpoint. Consult Git history for the current published state.

## Canonical locations
- Repo: D:/Astaled_Team/Our Projects/SpookTube
- Unity: My project, scene Assets/SpookTuber/Scenes/ProductionHouse.unity
- Blender: ArtSource/Player/CHR_Player_Master.blend; generator build_player.py
- Original refs: D:/Projects/SPOOKYTUBER/References; copies packed in the Blender file and repo References.
- Design docs: D:/Projects/SPOOKYTUBER/Docs/Source and Docs/Planning/SPOOKYTUBER_Coding_Guide_TH.md.
- Runnable build: D:/Projects/SPOOKYTUBER/Builds/Windows/SpookTuber.exe.
- Working/test copy: C:/Users/Kurak/SpookTuber-work/Unity; warm Library and QA.
- Blender executable: D:/Programs/Blender/blender.exe.
- Unity: C:/Program Files/Unity/Hub/Editor/6000.3.18f1/Editor/Unity.exe.

## Character revision 2
- Sleeve silhouette rebuilt: smooth shoulder flow, diagonal elbow folds, wrist gathers, ribbed cuffs and edge stitches. Softer torso/cargos and folded open collar.
- Removed Face mesh, amber eyes/emission and all Blink runtime/shape-key logic. Black display remains in Head.
- Fixed actual mirrored-sleeve winding: 672 faces on the left arm were inward. Blender bmesh corrects normals before subdivision/export; generator asserts outward-facing sleeve walls on both sides.
- 59 bones and Idle/Walk/Run. Base visible 20,876 triangles; dressed visible 41,244; dressed FBX includes hidden chassis, total 46,696.
- Blender QA includes an 85-degree elbow deformation render. ArtSource/Player/Archive preserves v1.
- UVs/colors, no final atlas/LODs. Garments are skinned, not free cloth; charm/tag have runtime springs.
- Eleven ragdoll bodies, fifteen colliders, bounded detached-head movement and repair. Detached head presentation is now created once in Awake, toggled/repositioned on death/repair so takes can track it.

## MainCam implemented
- EQP_MainCam_01 prefab on equipment bench: physical pickup/drop, one local holder, range/line-of-sight validation, no pickup by a detached head.
- E pickup, R record/stop/retry save, Q drop, P review, Space pause, arrows scrub. Gamepad bindings included. A dropped camera keeps recording.
- Held lens spherecast runs after view-camera positioning and before recorder sampling; first-person view also checks wall clearance.
- CrewTake records actual world transforms/bone poses, render visibility and light on/off at up to 20 Hz with simulation timestamps. Up to 60 seconds per take.
- Replay clones only presentation components, includes the recorded lens viewpoint, animates recorded poses and does not re-run physics or game callbacks.
- Versioned .sttake files in Application.persistentDataPath/Takes. Flushed .partial file is published via rename only on success. Failed saves retain frames in memory and block accidental overwrite; R can retry.
- Bad/incompatible/truncated files are rejected without losing a valid loaded take. Stable camera ID and scene bindings work across process restarts.
- Current captured actors must already exist when recording starts. No dynamic-spawn catalog, multi-camera feeds or video export yet. Revision 4 adds filmed evidence, EDL/Bobby editing and version 3 takes; audio/pose readers retain versions 1 and 2 when their scene bindings are available.
- Humanoid hand IK, elbow hints and finger curl now grip the MainCam. First-person framing and a 320x180 live LCD show the actual lens view. Do not call this the complete footage or multiplayer system.

## Validation and delivery
- Blender UV/weights/influences/normal-direction gate: PASS.
- Unity avatar/material/prefab import gate: PASS.
- Previous V2 Play Mode: 58 checks PASS, including input, wardrobe, physics/repair, MainCam ownership/occlusion/drop, persisted take, recorded ragdoll/lens pose, replay isolation, corrupt data, failed write/retry and camera-wall clearance.
- Fresh Unity process: 4 saved-take reload/render checks PASS.
- Final Windows build and boot results are in QA/standalone_build.txt and QA/standalone_runtime_check.txt.
- QA/Player_*.png and QA/Unity_*.png are real application renders. QA/MainCam_Regression.sttake is a test artifact, separate from player saves.
- Scripts, FBX, materials, prefabs, scene and .meta files were copied from the validated staging project and hash-checked.
- A recurring UnityEditor.Search indexing exception is Editor tooling, separate from game assertions. Do not claim Editor logs were entirely error-free.
- Build staging excludes unused Unity AI Assistant/Inference packages to avoid compiling their compute shaders. Original Unity Packages and PlayerSettings are preserved. Build scene entries now put ProductionHouse and Hospital first while retaining existing scenes/config objects.
- No commit or push. Pre-existing app-ui configuration and the PlayerSettings define change are preserved. This delivery adds ProductionHouse/Hospital build-scene entries while retaining SampleScene.

## Hospital delivery (revision 3)
- New native Blender library: ArtSource/Hospital/Hospital_AssetLibrary.blend and its two Python generators. Twenty-one FBX assets include room kit/medical props, a 14-bone Surgeon (9,168 triangles) and a crew RV (6,012 triangles). Original procedural wear/fabric PNGs and five mechanical WAV cues are generated locally.
- Hospital.unity is an authored six-room layout with four side-room connections, a corridor and an always-accessible RV exit. It is not procedural generation. BuildHospital uses installed AI Navigation to bake and validate all room routes.
- RunSession connects house departure, hospital arrival, cancellable RV countdown, gear recovery, hospital-take review at home and solo cloud recovery after all-down. Journal writes flush/read back/replace with backup; run IDs guard duplicate completion. No economy or upload rewards exist yet.
- Surgeon uses native NavMeshAgent, patrol/noise investigation, an audible warning, pursuit, bounded last-known-position search, attack windup, cooldown and forcing nearby closed doors. Crew footsteps use actual ground travel; door and dropped-camera collisions produce noise.
- A perception bug was reproduced and fixed: an interpolated head could remain visible while the root had already moved behind cover. LOS and pursuit now use the same observed head XZ, never an independently advanced hidden root.
- Version 2 takes capture spatial game-audio playback/cursors alongside pose, visibility and light state. Audio listeners/live sources are restored when preview closes. Unity's valid end-of-clip cursor is normalized before seeking. No microphone capture.
- Cross-scene review has an explicit loading guard; Surgeon/agent are disabled before review begins. Presentation clones run no gameplay/physics. Reviewing or reopening footage does not complete another mission.
- MainCam has two-hand IK, elbow hints, curled fingers, a 70-degree crew view and a 65-degree live lens LCD at 320x180/10 Hz. The recorded replay remains 960x540. The character display remains blank black.
- 60-second takes are a deliberate current ceiling, visible on the REC HUD. Long-run streaming and multi-camera work remain pending. Revision 4 connects evidence, Bobby EDL and local publishing.
- HOSPITAL_README.md contains controls, source locations, regeneration instructions and remaining GDD scope. Current test/build reports and actual rendered images are in QA.
- Unity builders now preserve unrelated build-scene entries. Rebuilding crew assets reconnects the existing house mission stations. The canonical original app-ui configuration and PlayerSettings define changes must remain intact.

## Next work
Follow the Brief/GDD; continue real production features without multiplying throwaway scenes. Hospital/Surgeon, hand posing, game audio, extraction and the initial local evidence/edit/upload/progression loop are connected. Next: extend recording across takes/longer runs, strengthen the production-house/Bobby presentation and add the remaining CORE enemies and multiplayer. Revision 4's reward and upgrade values are explicitly initial tuning, not locked GDD canon; see PRODUCTION_README.md.
LED faces are explicitly deferred by the user; keep the display blank unless they change that direction.
Do not report CORE/full game completion from these local checks. Retain original lore decisions and discovery-based presentation.


## Final validation for revision 3
- 120 checks passed: crew/camera 60, fresh house take 4, hospital lifecycle 48, fresh hospital audio take 3, forced-door traversal 5.
- Windows development build succeeded with zero build errors; reported size 175,741,748 bytes. GPU-enabled standalone boot stayed alive without managed/gameplay exceptions or asset-load errors. Its D3D12 info-queue startup message is retained in QA/standalone_runtime_check.txt.
- Source, art and Windows build delivery are SHA-256 verified in QA/source_delivery.txt and QA/build_delivery.txt. No commit or push.
- Forced-door regression now checks actual crossing into the next room. The AI keeps its requested destination because NavMeshAgent.destination can collapse to a partial-path endpoint; any door change schedules a replan after carving updates.

## Revision 4 — production editing and progression

- Added 5 Hz lens evidence: three-point visibility/occlusion, authored light/fog readability, framing, stability and confirmed Surgeon states. Version 3 .sttake files retain moment/run/take IDs alongside recorded world poses and audio.
- Connected the house monitor to Bobby's native Unity workstation. Recovered-tape catalog, Documentary/Horror auto edits, bounded context joins, actual recorded-episode playback, source preview, half-second trim, cut ordering/removal, Undo/Redo, saved drafts and exact published-cut archive are implemented.
- Local in-game upload commits views/subscribers/revenue with the episode receipt and once-only reward ledger. Integer Team/Crew wallets use the explicit current solo 60/40 split. A Team Fund purchase installs a context editor that changes real cut selection/padding and duration budget. No direct upgrade views multiplier.
- Career version 2 migrates the old run journal, adds SHA-256 payload checksums/revisions, flush/readback/replace backup writes, and reports verified-backup recovery. A damaged primary plus damaged backup blocks overwrite. A failed transaction changes neither wallet nor purchased capability.
- Preserved the former desk-label hierarchy identity while changing its visible copy; real version-2 house and hospital footage both load. Unchanged loaded takes are not rewritten or duplicated. Read QA/v4_debug_ledger.md for the reproduced regression and its validation.
- Final checks: 192 PASS — crew/camera 60, fresh house take 4, hospital loop 48, fresh hospital take 3, forced-door traversal 5, production loop 54, fresh career/EDL/cut-boundary/recovery 14, legacy files 4.
- Windows development build succeeded with zero build errors; reported size 175,800,416 bytes. GPU-enabled standalone boot ran 115.7 seconds on the GTX 1050 Ti. The closed-process log was checked; startup diagnostics are retained in QA/standalone_runtime_check.txt. This is not an FPS benchmark or standalone full-loop test; the full loop was exercised in Unity Play Mode.
- PRODUCTION_README.md documents controls, exact initial economy tuning and limits. Single player, one MainCam take per episode, one release per expedition, 60 seconds per take. The dedicated Bobby NPC model/animation, cross-take/multi-camera/streaming, microphone/video export, online/multiplayer, other CORE enemies and the full economy remain pending. The main character's face stays blank black.
- Canonical source/build delivery is verified separately in QA/source_delivery.txt and QA/build_delivery.txt. Existing canonical ProjectSettings/Packages are preserved. No commit or push.

