# Editing and publishing — revision 4

The solo production loop now connects the hospital recording to a watchable cut, an in-game SpookTuber TV release and persistent progression. This is the production project; multiplayer and the rest of the GDD are still outstanding.

## Play

1. Leave the apartment through the RV departure door. Use **R** while holding MainCam to record. Each take holds up to 60 seconds; additional takes remain in the recovered-tape catalog.
2. Frame the Surgeon with the actual lens/LCD. Visible, lit sightings and actual Warning, Hunt and Attack states can become filmed moments. Looking at a wall or away from the subject does not earn visual evidence.
3. Return to the RV's orange rear panel and press **E**. After returning home, use **E** at **Bobby / Edit & Release** on the desk monitor.
4. Select a recovered tape. **Documentary** and **Horror** build cuts from its recorded moments with context. Related overlapping shots join into a continuous clip. The preview plays the recorded world and original lens, including game audio.
5. Select a timeline shot to adjust its in/out points in half-second steps, reorder or remove it. **Undo/Redo** changes the edit, preserving source footage. **View Source** switches to the full original take.
6. **Upload Episode** publishes within the game. The result shows views, subscribers, revenue, the Team/Crew split and comments supported by included moments. Each expedition can release one episode; changing presets, selecting another take or restarting cannot pay that expedition again.
7. Buy **Upgrade Bobby** with Team Fund. The context editor changes lead-in/out length and framing selection and permits a longer episode. Use Auto Edit again to apply it. It grants no direct views multiplier.

Mouse controls and keyboard/gamepad UI navigation use Unity's native UI input. **Tab/arrows** select, **Enter** activates, **Esc** returns home. **P** at home retains the separate raw-footage review with Space/arrows/P controls.

The main player face remains a blank black display. No microphone audio is captured. Publishing is a local game action, not an upload to a real social service.

## Implemented data flow

- `.sttake` version 3 adds take/run identities and filmed-moment evidence after the existing pose/audio tracks. Readers retain versions 1 and 2 for compatible scene bindings. Older footage remains raw-review material; record a new expedition for evidence-backed publishing.
- Evidence samples at up to 5 Hz. It tests three subject points against the lens framing and real physics occlusion, estimates authored lighting/fog, records stability and confirms actual AI state. A readable interval must last at least 0.5 seconds. Small interruptions up to 0.75 seconds can remain one interval.
- Bobby's EDL stores source camera, timestamps, primary/supporting moment IDs and selection reason. Bounds, minimum clip length, overlap, repeated evidence and duration budget are validated. No gameplay, AI, physics, damage or rewards run inside the replay world.
- Documentary/Horror weighting is deterministic. Episode identity depends on the cut and source identities, not the absolute storage location. Audience noise is derived from that identity. The receipt retains the algorithm version, evidence IDs, comments and exact reward.
- The career stores recovered tapes, a saved draft, published EDLs/receipts, balances, subscribers and the purchased editor capability. The archive restores the exact released cut; a changed draft is identified separately from published results.
- Payment, transaction ID and episode receipt commit together. Purchase cost and installed capability commit together. Failed writes change neither wallet nor inventory. A prior valid take remains available when an invalid take fails to load.
- Career writes flush a sibling temporary file, verify its contents and replace the primary with a backup. A SHA-256 checksum and revision detect accidental damage; this is not an anti-cheat signature. A verified backup is reported when recovered. If both copies are damaged, the game preserves them and blocks overwriting the career.

## Initial tuning, not locked GDD canon

| Setting | Current value |
| --- | --- |
| Basic/context editor episode budget | 24 / 36 seconds |
| Maximum continuous clip | 12 seconds |
| Minimum clip | 1.5 seconds |
| Maximum cuts | 8 |
| Team/Crew split in this local solo career | 60% / 40%, integer minor units |
| Audience reach | 800 + 3 per subscriber, subscriber contribution capped at 30,000 |
| Audience noise | 0.90–1.10, stable per episode |
| Revenue | 3 minor units per view; 100 minor units displayed as 1.00 |
| Subscriber conversion | floor(views × 0.02 × filmed quality) |
| Context editor cost | 2,500 minor units / 25.00 Team Fund |

Quality uses recorded visibility/readability, framing, camera stability and confirmed danger. Retention uses usable duration and event-family variety. Unmarked footage can produce a weak release; it does not invent a monster event or block the next expedition. These curves need playtesting, not presentation as final economy balance.

## Scope boundaries

One local player, one MainCam take per episode, one release per expedition. All recovered takes remain selectable. Cross-take/multi-camera editing and long-run chunk streaming are the next recorder expansion. The current recorder captures actors present at recording start; dynamic-spawn catalogs and generalized material/VFX state are not implemented. Lighting is an authored-map estimate, not a pixel-perfect sensor simulation.

Bobby currently operates through the editing workstation; his dedicated NPC model/animation is not included in this revision. Manual editing supports the generated cuts, not a full arbitrary source-bin editor. No export codec, personal voice recording, online publishing, network authority, personal-money portability, old-video income, trend simulation, promotion shop, full upgrade tree or Thai UI localization is claimed. Repetition decay across different expeditions and final balancing remain pending.

## Checks

Run with Unity 6000.3.18f1 and the selected test project closed:

`Unity.exe -batchmode -buildTarget Win64 -projectPath "<project>" -executeMethod SpookTuber.Editor.BobbyPlayCheck.Run -logFile "<log>"`

Then use `BobbyPlayCheck.RunReload` in a fresh Unity process. The runnable checks cover the actual hospital recording/return/edit/upload path, visual evidence false positives, EDL bounds/undo/redo and cut-boundary playback, recorded-world isolation, stable audience results, duplicate rewards, failed save/purchase behavior, persisted upgrades and damaged-save recovery. `BobbyPlayCheck.RunLegacy` reads the shipped QA version-2 house/hospital takes; stable scene-label identity is retained when visible copy changes. Test careers are under `QA`, separate from player saves. The explicitly funded purchase fixture is a test artifact, not a shipped starting balance.

Keep running `CrewPlayCheck.Run`, `CrewPlayCheck.RunReload`, `HospitalPlayCheck.Run`, `HospitalPlayCheck.RunReload` and `HospitalPlayCheck.RunDoor` for the existing character, camera and mission regressions. The builders preserve the existing build settings and the blank player display.

Actual verification reports and application renders are in `QA`; final build/delivery status is recorded in `WORK_STATE.md`.
