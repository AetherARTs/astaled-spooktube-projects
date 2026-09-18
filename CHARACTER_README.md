# SPOOKTUBER character and MainCam

Blender source: ArtSource/Player/CHR_Player_Master.blend — Blender 5.2.2 LTS.
Unity scene: My project/Assets/SpookTuber/Scenes/ProductionHouse.unity — Unity 6000.3.18f1, URP 17.3.0.

## Current integration — revision 6

The approved player geometry and rig are unchanged; the playable prefab is uniformly scaled to 88% (approximately 1.59 m including antennas). The face remains blank. MainCam now uses `ArtSource/Environment/EQP_MainCam_Master.blend` and `EQP_MainCam_Detailed.fbx` (35,800 triangles), with the existing grip, carry, recording and live LCD systems. BuildEquipment.Build updates only the camera and archives old scene camera geometry for legacy footage.

## Character revision 2
- Blank black screen. The eye meshes, amber emission and Blink blendshape/runtime animation were removed at the user's request. LED expressions are intentionally not implemented yet.
- Rebuilt hoodie sleeves with smoother shoulder transitions, shaped elbow folds, gathered wrists, ribbed cuffs and edge stitching. Softened the torso, folded collar/hood opening and cargo silhouette.
- Blender render QA includes an 85-degree elbow bend, front/base, dressed three-quarter and back views.
- 59 humanoid bones, articulated fingers, head/grip/backpack/light sockets and accessory bones. Idle/Walk/Run clips share the same rest skeleton.
- Approximately 1.81 m including antennas. T-pose rest skeleton, metric geometry, identity authoring transforms.
- Base visible geometry: 20,876 triangles. Dressed visible geometry: 41,244 triangles. Hidden chassis retained for wardrobe changes; complete dressed export: 46,696 triangles.
- UVs and named URP color materials. No final texture atlas or LOD chain yet.
- Garments are skinned; the bunny charm and tag have damped secondary motion. Eleven ragdoll bodies and fifteen colliders include hands and shoes.
- The bounded detached head has collision and energy. Its presentation object is kept stable for recorded takes. Repair restores the character.

## Play
Open ProductionHouse and press Play, or run D:/Projects/SPOOKYTUBER/Builds/Windows/SpookTuber.exe with its accompanying files.

WASD move; mouse look; Shift run; Space jump; F shoulder light; V first/third-person; E interact; Esc release/lock cursor.
MainCam sits on the equipment bench by the window. Approach, look at it, and press E to pick it up. Holding MainCam uses first-person view.
R starts/stops recording; Q puts the physical camera down; it keeps recording when dropped. P opens/closes the most recent take.
During review: Space pauses/resumes; left/right arrows scrub. Gamepad: right trigger records, east button drops, select reviews, south button pauses, D-pad scrubs.
The wardrobe uses E. Surgeon attacks now trigger mission damage and solo cloud recovery. CrewBody's Play Mode context menu also exposes KnockDown and Repair; multiplayer body rescue and in-world repair stations remain pending.

## Actual recorded takes
CrewTake records world positions, rotations, visibility and light on/off state at up to 20 samples per second, using actual simulation timestamps. Camera lens pose, animated bones, wardrobe changes, ragdoll and the detached head are tracked.
Playback builds a separate renderer-only world, interpolates recorded poses and renders the recorded lens viewpoint to a RenderTexture. It does not re-run game scripts, AI or physics.
Each take is limited to 60 seconds. Files are written to Application.persistentDataPath/Takes as versioned .sttake files. An incomplete write remains .partial; only a flushed, completed file is published. A failed save preserves the take in memory; R retries saving while holding the camera.
P can load the latest compatible take after restarting the game. Invalid/truncated files are rejected; incompatible content versions are not silently remapped.
Capture covers the authored ProductionHouse and Hospital actors, including pose, visibility, lights and native game audio. Revision 4 also adds filmed evidence, Bobby editing, episode playback and local upload/progression. Dynamic-spawn catalogs, general animated-material recording, microphone audio, additional cameras and video export remain unimplemented. See PRODUCTION_README.md and DEVLOG.md for the current scope.

## Repeat authoring and checks
With the project closed:
- Blender: blender.exe --background --python ArtSource/Player/build_player.py
- Assets/scene: Unity.exe -batchmode -projectPath "<project>" -executeMethod SpookTuber.Editor.BuildCrew.Build -logFile "<log>"
- Play Mode: Unity.exe -batchmode -projectPath "<project>" -executeMethod SpookTuber.Editor.CrewPlayCheck.Run -logFile "<log>"
- Fresh-process saved-take check, after Play Mode: Unity.exe -batchmode -projectPath "<project>" -executeMethod SpookTuber.Editor.CrewPlayCheck.RunReload -logFile "<log>"
- Windows build: Unity.exe -batchmode -projectPath "<project>" -executeMethod SpookTuber.Editor.CrewPlayCheck.BuildPlayer -logFile "<log>"

BuildCrew regenerates its named prefabs, controller and ProductionHouse scene. Keep manually edited variants separate.
The Blender generator regenerates its master/FBX/QA images. Save manual mesh edits separately until reflected in its source. The pre-revision master is retained in ArtSource/Player/Archive.
The master opens in Idle with the rig hidden; unhide CHR_Player_Rig to pose/edit bones. Reference sheets are packed into the .blend.
Layer 8 is used for the local crew/held camera and layer 9 for replay presentation; the local camera excludes layer 9.

## Build and outstanding work
QA contains actual Blender/Unity renders and executable check results. A take file from regression testing is separate from the player's save directory.
The Windows build uses a staging copy without unused Unity AI Assistant/Inference packages. The user's original Packages and PlayerSettings are preserved; production scene entries are added to Build Settings without removing existing scenes/configuration.
Added: camera hand IK/finger curl, a live LCD, game-audio takes, Hospital/Surgeon with extraction/recovery, and the first local evidence/edit/upload/progression loop. Remaining: multi-camera/cross-take/long recording, multiplayer, the wider CORE/V1 systems and final asset polish. See DEVLOG.md for completed work, current limits and the remaining GDD scope.

Technical references:
- https://docs.blender.org/api/current/bpy.ops.export_scene.html
- https://docs.unity3d.com/6000.3/Documentation/ScriptReference/ModelImporter.html
- https://docs.unity3d.com/6000.3/Documentation/ScriptReference/CharacterJoint.html
- https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Camera-targetTexture.html

