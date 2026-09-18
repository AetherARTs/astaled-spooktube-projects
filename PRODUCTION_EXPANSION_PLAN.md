# Production expansion — latest user direction, 2026-09-18

This extends the ongoing solo production task. It does not replace camera/phone/UI/microphone/economy requirements or authorize declaring the entire GDD finished from a feature list.

## New explicit requirements

- Horror darkness comes first. Environmental light is sparse; shoulder/production light should be necessary for filming readable subjects. Home remains the recovery/preparation contrast.
- Finish Hospital as modular procedural rooms joined by rooms/corridors. More completed days and successful quota contracts grow room count/extent and traversal complexity.
- Include pits, jump routes, parkour and climbable paths. Required routes must remain reachable and have a valid return to RV; optional risk routes can reward exploration.
- After Hospital is complete, build all remaining GDD V1 locations: Weapon Manufacturing Facility, Flickerhorm AETHER Academy, Haunted Mansion. Distinct themes, room sets, hazards and threats; not four renamed copies.
- Add real title/Menu scene, Loading scene, Options and complete navigation between playing, settings and returning/quitting.
- Rework visible equipment in Blender with deliberate silhouettes, mechanisms, materials and grips. Revision 6 replaces MainCam's old primitive housing with the authored detailed model. Do not regenerate the approved player.
- Continue the core systems and the complete four-map scope. Co-op remains behind solo polish, as already instructed.

## Work order and acceptance

1. Resolve measured rendering cost, deliver dark Hospital lighting with flashlight comparison and a Windows smoke run. Preserve the tested v5 checkpoint and user saves.
2. Main Menu/Loading/Options and game settings, connected to actual scene loads and persistence, keyboard/mouse usable.
3. Deterministic procedural Hospital: connected graph, variable room population, loops, collision/navigation-safe doors, solvable traversal, quota/day scaling and exact layout recreation for footage replay.
4. Detailed MainCam and production gear in Blender; check first-person grip, LCD, collisions and Unity import.
5. Complete the other three themed modular locations with unique room props, hazards and gameplay. Use the Brief names Foreman/Professor/Host as design identities; reference-art alternate names do not silently rename canon.
6. Fill remaining Solo core interactions/content, run a complete prepare→film→escape→edit→release cycle in each location, test saved replay after restart, profile Windows and update Git/DevLog/builds regularly.

## Decisions carried forward

Credit is spendable money. View remains the agreed three-day contract progress. The new daily/weekly wording is interpreted as progression-driven level growth; do not add a second overlapping weekly currency/quota without a design reason. Use completed expeditions and successful contracts for growth, not retry count.

Procedural layout identity must accompany recorded footage before randomizing live scenes. Rebuild the exact seed/layout for playback; never bind an old take to a different generated scene. Preserve existing authored scenes as legacy replay content. Room growth needs a bounded recording/render budget with merged static room meshes, not an unlimited increase of every-frame transform tracks.

Current measured bottleneck: 1600×900 GTX 1050 Ti, hospital ~60 ms with camera; recording-off ~61 ms; LCD-off ~36 ms; shadows-off ~15 ms. Optimize duplicate shadow rendering and authored light density. Hidden-window FPS is invalid; validate visible rendering.
