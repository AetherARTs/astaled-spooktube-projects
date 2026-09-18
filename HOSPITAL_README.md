# Hospital and home — revision 5

See [HOW_TO_PLAY_TH.md](HOW_TO_PLAY_TH.md) for controls and [PRODUCTION_README.md](PRODUCTION_README.md) for the connected production loop, economy, validation commands and limits.

The current authored hospital is 37 × 68 m (2,516 m²), with a 47 × 24 m forecourt, eight clinical rooms, a 5 m central corridor and 4 m outer loop passages. Rooms include wards, theatre, radiology, isolation, pharmacy, records and morgue. Sliding doors move sideways into pockets, clear the NavMesh when open, and prevent crushing actors/props. Low marked ledges support mantle; a complete second storey and full vertical routes are not delivered.

The home is 30 × 30 m with a 16 × 14 m garage. Furnished living/studio/equipment spaces connect to Bobby, the supply clerk, wardrobe and RV departure. The original player stays unchanged in mesh/rig design and uses an overall 0.88 scale.

## Editable sources

- `ArtSource/Hospital/Hospital_AssetLibrary.blend`: existing medical kit, Surgeon and RV.
- `ArtSource/Environment/Solo_Environment_Library.blend`: 34 added environment/equipment/NPC assets.
- `ArtSource/Environment/build_environment.py` and `build_materials.py`: original Blender models and 21 original 1K material maps.
- `ArtSource/asset_tools.py`: shared Blender mesh/UV/export helpers.
- `My project/Assets/SpookTuber/Scripts/Editor/BuildSolo.cs`: current scene assembly, materials, routes and scene capture.
- `My project/Assets/SpookTuber/Scenes/ProductionHouse.unity` and `Hospital.unity`: playable current scenes.
- `Scenes/Legacy/*_v4.unity`: archived bindings for existing recorded footage.

BuildSolo rebuilds its named generated assets/scenes. Preserve manual variants before rerunning. BuildHospital remains the older layout builder, not the current scene-delivery command. Layer 8 is local crew/held gear; layer 9 is replay; layer 10 is the Surgeon.

## Scope and evidence

Revision-5 system and Bobby checks exercise actual input, physical props, door clearance/navigation, real recording/extraction, editing, payment and save recovery. `QA/v5_scene_build.txt`, `v5_systems_check.txt`, `bobby_playmode_validation.txt` and the current build/runtime reports give the results. Older test totals remain historical.

World noise includes real microphone levels, footsteps, landing, doors and impacts. The mic is used for AI detection; recorded clips contain game audio, not recorded speech. Noise delivery is validated separately from human speech calibration. Presentation remains stylized and needs further art, performance and encounter work to reach the user's near-realistic target. Co-op is deferred; the active target is enjoyable Solo play.
