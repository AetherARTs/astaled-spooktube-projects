# Hospital run — 18 September 2026

This delivery connects a playable solo hospital run to the production house. It is part of the production project, not the completed GDD/CORE scope.

## Play

Run `D:/Projects/SPOOKYTUBER/Builds/Windows/SpookTuber.exe`.

1. Walk to the apartment entry door and press **E** to depart. The MainCam and selected outfit accompany the crew.
2. **WASD** move, mouse look, **Shift** sprint, **Space** jump, **F** shoulder light. **E** opens/closes doors or picks up the MainCam.
3. Frame using the MainCam's live LCD; **R** records/stops, **Q** puts it down. A dropped camera continues recording. Each take currently holds up to 60 seconds.
4. The Surgeon patrols, investigates actual movement/door/impact noise, warns before pursuing and winds up before striking. Break sight lines and use doors to gain time.
5. Return to the orange rear panel on the RV and press **E**. Departure takes three seconds; walking away cancels it. Footage is saved before leaving. Basic gear is recovered even if dropped.
6. Back home, use the desk monitor for **Bobby / Edit & Release**: preview an automatic cut, trim/reorder it, upload it in the game and buy the context-editor upgrade. See `PRODUCTION_README.md`. **P** retains raw-take review; **Space** pauses, arrows scrub, **P** closes. Review does not run AI or grant completion again.

In solo play, a downed crew member gets a brief detached-head interval, then cloud recovery returns them home. **Esc** releases the pointer; **Alt+F4** closes the executable.

## Source

- `ArtSource/Hospital/Hospital_AssetLibrary.blend`: editable kit, Surgeon rig and production van.
- `ArtSource/Hospital/build_hospital.py`: native Blender geometry, FBX exports, original mechanical WAV cues and visual validation renders.
- `ArtSource/Hospital/build_surfaces.py`: original deterministic wear/fabric textures.
- `My project/Assets/SpookTuber/Scenes/ProductionHouse.unity` and `Hospital.unity`: connected scenes.
- `My project/Assets/SpookTuber/Scripts/Editor/BuildHospital.cs`: assembles the authored layout, bakes navigation and checks each room route.

The kit has 21 FBX assets. Hospital contains six medical rooms, connecting passages, a main corridor and the RV forecourt. This is a fixed authored layout with alternate routes, not procedural generation. Keep manual scene/mesh variants separate before running the generators, which rebuild their named outputs.

Layer 8 is the local crew/held gear, layer 9 the isolated replay, layer 10 the Surgeon. The player's display stays blank black. LED expressions remain deferred.

## Persistence and checks

The executable stores takes under `Application.persistentDataPath/Takes` and the career in `HospitalRun.json`. Both publish a flushed temporary file only after a successful write. The career retains a checksum-verified backup, revision and reward ledger. Failed footage writes retain recorded frames and prevent leaving; **R** retries while holding the camera. Game audio and filmed evidence are recorded; microphone audio is not captured.

With Unity 6000.3.18f1 closed for the chosen test project, run these via `-batchmode -projectPath "<project>" -executeMethod SpookTuber.Editor.<method> -logFile "<log>"`:

- `BuildCrew.Build`, then `BuildHospital.Build`: regenerate/import and validate assets.
- `CrewPlayCheck.Run`, then `CrewPlayCheck.RunReload`: character, camera, persistence and fresh-process replay checks.
- `HospitalPlayCheck.Run`, then `HospitalPlayCheck.RunReload`: mission lifecycle, doors, perception, extraction, replay isolation, recovery and sound persistence.
- `HospitalPlayCheck.RunDoor`: the Surgeon reaches, forces and navigates through a closed door after a warning delay.
- `BobbyPlayCheck.Run` and `BobbyPlayCheck.RunReload`: footage evidence, actual episode playback, editing, upload/payment, purchase persistence and save recovery.
- `CrewPlayCheck.BuildPlayer`: build the enabled Windows scenes.

Actual results and application renders are in `QA`. Editor Search can emit an unrelated startup indexing exception; game assertions and build results are reported separately.

## Remaining GDD work

The first local evidence/edit/upload/progression loop is implemented; its exact limits and initial tuning are in `PRODUCTION_README.md`. Multiplayer, Peeker/Clinger, procedural layouts, multi-camera, cross-take editing, long-run recording and the wider economy/content systems remain. Assets still need further art/animation/LOD work and performance measurement on target hardware.
