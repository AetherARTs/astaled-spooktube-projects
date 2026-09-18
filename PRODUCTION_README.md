# SpookTuber production — revision 6

The solo loop connects the expanded home, a hospital expedition, recorded footage, Bobby/manual editing and persistent progression. See [HOW_TO_PLAY_TH.md](HOW_TO_PLAY_TH.md) for the Thai controls and play guide. The complete GDD is still in development; Co-op is deliberately deferred at the user's request.

## Play and UI

Start at MainMenu and enter ProductionHouse through the Loading scene. Options controls audio, look, FOV, display and URP quality. Escape pauses solo play; save-and-return to title keeps the scene open if persistence fails. Pick up MainCam and the gravity glove at the equipment bench; carry at most three items. Number keys or wheel switch slots, Q drops the selected item, RMB raises MainCam smoothly and R records. Switching away from a recording camera saves the take first. A failed save prevents discarding its frames.

Tab opens the physical crew phone. Mission shows the three-day View contract; Map reveals visited areas and camera/RV locations; Equipment selects tools and toggles the emergency light; Harmony provides Solo Bobby check-ins; Settings controls the actual input device, microphone gate/mute, volume and look sensitivity. Controls lists the keys. Phone use leaves the world running, suppresses world input and hides held tools. Arrow keys and Enter operate the menus; Esc backs out one level. Batteries recharge at home and the RV.

The home includes Bobby and a supply clerk, a studio, living area, wardrobe, equipment area and garage. The hospital is 37 × 68 m with a 47 × 24 m exterior, eight clinical rooms and looping 4–5 m corridors. Sliding doors retract into pockets and refuse to close on actors/props. The Surgeon investigates microphone and physical world noise, warns, pursues and attacks.

The original approved player mesh, hoodie and blank black face remain. The whole playable rig is uniformly scaled to 88%. Movement includes walk/run, jump, crouch, sprint slide, collision-checked low ledge mantle, corner lean and physical prop pushing. The glove pulls/holds/pushes eligible rigidbodies within mass, range, line-of-sight and energy limits. This is selective physics; it is not unrestricted climbing or a fully physical locomotion rig.

## Bobby and release

After returning to the RV and home, use Bobby or the studio desk. Each unpublished tape asks **Bobby edits** or **edit yourself**. Bobby's path builds an evidence-based Horror cut, saves it, releases the episode within the game and starts the result preview automatically. Manual editing starts a source cut; View Source/Add Shot, trim, reorder, remove, Undo/Redo and Save Cut remain available.

The preview plays the recorded world and lens, including game audio. Replays run no AI, physics, damage or rewards. Published results preserve the exact cut and its receipt. One expedition can receive one episode payment: choosing another tape, changing the edit or restarting cannot pay it again. This is local in-game publishing, not a real social-service upload.

## Credit, View and initial tuning

| Value | Purpose / current tuning |
| --- | --- |
| Credit | Spendable wallet, internally integer hundredths; team/crew split retained in the journal |
| View | Contract progress; buying/selling never spends it |
| Contract | 1,000 Views in 3 days initially; next successful target grows by 35% |
| Day | Advances on the next departure after completing the current expedition |
| Missed day 3 | Finish publishing remaining footage or retry from Mission; retry keeps Credit and purchased gear |
| Work Light / Noisemaker | 12.00 / 6.00 Credit; sell for half the purchase price |
| Bobby context editor | 25.00 Credit; changes shot choice/context and budget, no direct View multiplier |
| Edit budget | Basic 24 s / upgraded 36 s; max 8 cuts, each 1.5–12 s |
| Revenue | 0.03 Credit per View; team/crew accounting split 60/40 |
| Subscribers | Audience growth; separate from wallet and quota |

Quality derives from filmed visibility, light readability, framing, stability and confirmed threat states. Retention uses duration and event variety. Audience reach/noise is deterministic per released edit. These curves are initial playable tuning, not final balance.

## Persistence and audio

Career version 3 migrates older careers, adding contract and equipment ownership. Payment/receipt/ledger and purchases commit atomically using flushed, verified temporary writes and a checksum-verified backup. Failed writes change neither wallet nor ownership. Damaged primary plus backup are preserved and block overwriting. The checksum detects corruption, not cheating.

SPOOKTAKE version 3 preserves pose/audio tracks, evidence and stable identities. Recording is currently 20 Hz, 60 seconds per take, up to 3,000 tracks and 128 MiB per file. Actors must exist when recording begins. Legacy revision-4 scenes remain in the build for old footage; current runs use revision-5 scene identities. Multiple recovered takes remain selectable; one episode currently uses one source take.

The Windows microphone is genuinely sampled. Voice levels over the adjustable gate alert nearby threats, alongside footsteps, landing, doors, impacts and noisemakers. Audio stays in a one-second RAM buffer for detection; microphone speech is **not** saved into clips or files. The hardware check opened the Maono wireless microphone and verified advancing capture samples. Human speech sensitivity still needs calibration in the player's room.

## Current validation

Run Unity 6000.3.18f1 with the selected test project closed:

`Unity.exe -batchmode -projectPath "<project>" -executeMethod SpookTuber.Editor.<method> -logFile "<log>"`

- `BuildSolo.Build`: assembles current assets/scenes and verifies routes to all eight clinical rooms. It rebuilds named generated outputs; preserve manual variants before using it.
- `SoloSystemsCheck.Run`: input, movement, three slots, physical glove, phone, world noise, sliding doors, quota and Credit save-failure paths.
- `SoloSystemsCheck.UI`: actual native keyboard navigation, microphone dropdown, back behavior and phone screenshots.
- `SoloSystemsCheck.Hardware`: physical Windows microphone buffer/meter, restoring the prior microphone preference.
- `SoloPlayCheck.CameraRepro`: continuous moving-view camera rotation regression.
- `BobbyPlayCheck.Run`, `RunAutomatic`, `RunReload`, `RunLegacy`: real recording/edit/release, automatic handoff, fresh-process persistence/recovery and archived footage compatibility.
- `CrewPlayCheck.BuildPlayer`: current Windows development build.

QA reports and renders record actual results. Older CrewPlayCheck/HospitalPlayCheck scene-coordinate tests describe revision 4; their historical totals are not new revision-5 passes. Editor Search startup diagnostics are tracked separately from gameplay failures.

## Remaining production work

The authored map is larger and more detailed but not final near-realistic art. More environmental storytelling, animation, LOD/occlusion/performance tuning, interactions, encounters and playtest balance remain. Bobby/Supply NPCs look at the player but do not have full schedules/dialogue/locomotion. There is one hospital and one enemy type. Full vertical routes, other CORE enemies, procedural layouts, unrestricted climbing, Co-op, cross-take/multi-camera editing, streaming long recordings, voice/video export, online publishing, deeper economy and Thai UI localization remain. LED faces are explicitly deferred; keep the player display black.
