# Revision 5 debug ledger

## Held camera rotation

User screenshots show rolled equipment while the world stays upright.
Repro: `SpookTuber.Editor.SoloPlayCheck.CameraRepro`, ProductionHouse, pick up MainCam, rotate yaw continuously at 180 degrees/second and pitch sinusoidally for three seconds.
The initial stationary rotation check passed (0 degrees). This disproved a permanently wrong parent/axis hypothesis; it did not test the reported moving state.
Continuous movement failed: peak 179.9484 degrees; captured camera roll 45.69 degrees while view roll was zero. Kinematic camera Rigidbody still had Interpolate enabled and UpdateHeldPose wrote position only.
No attached debugger was available in the batch player. Source trace and the paired moving/stationary probes isolate interpolation ownership. The held camera now disables Rigidbody interpolation and writes its view-local rotation each pose update; dropping restores interpolation. Added RMB close-view blend and 30 Hz LCD updates.
The same continuous-turn check passes after the fix; detailed measurements are in v5_camera_probe.txt. Arm reach and visual grip still require the locomotion/inventory integration check.

## Enlarged-scene lighting and flooring

Reproduction: BuildSolo captures the saved scene from fixed corridor and home viewpoints. The first images were nearly black. Paired LightingProbe captures removed shadows, then grading, then material maps. Removing shadows alone did not recover the missing light pools, falsifying the suspected fixture occlusion. Removing grading recovered silhouettes; a 15x local-light probe recovered the expected pools without moving lights. The small-room intensities and ACES shadow compression were unsuitable for the expanded rooms. Tuned practical intensities, neutral grading, ambient fill and metre-based UVs now preserve readable surfaces. The hospital floor had an independent layering error: a -0.015 m module offset put tile tops 0.0005 m below the structural slab. Restoring the authored floor origin reveals the tile surface.

## Phone startup and glass occlusion

SoloSystemsCheck reproduced a NullReferenceException at CrewPhone.Open, with earlier HUD refresh exceptions. CrewPhone is serialized on the player; CrewMotor creates inventory/voice during Awake. Unity does not guarantee the relative Awake order of those components. CrewPhone now resolves those dependencies in Start, after all Awakes. The same integrated 61-check route subsequently passed without game exceptions.

Image review still caught a separate defect that state checks cannot detect: all phone pages were hidden behind the authored glass. Glass extends to z=-0.0205 m; the UI plane was at -0.020 m. Moved UI to -0.024 m and fitted it inside the glass border. Visual acceptance remains separate from the automated state checks; inspect the refreshed phone screenshots before delivery.

## Final phone interaction and presentation

Refreshed images verified the glass-depth fix at 16:9 and 16:10. The hand target exceeded the arm's reach in the old pose; placing the aimed camera within reach reduced measured wrist error to 4.84 mm. Moving the phone wrist outward clears the lower-right buttons without changing the character mesh. Native Dropdown needed an explicit dark item background, correct stretch width, a text arrow and inactive-template lookup. Enter opens apps/devices; successive Escape presses dismiss the list, return to apps and stow the device. Dropdown fade uses real time, so the automated close assertion waits for its fade, not just simulated time. The UI check passed eight assertions. Enlarging map marker text rects fixes missing glyphs; owned-camera markers no longer hide the player's marker.

## Windows render and performance check

The first hidden-window standalone check exercised code and saved a take, but Windows skipped backbuffer presentation: all screenshots were black and frame times were invalid for performance. Those numbers are retained only as a diagnostic in v5_hidden_window_probe.txt. A visible-window rerun produced actual game images and measured the hospital at 68.4 ms median / 80.8 ms p95 while holding and recording. This justified isolating recording, LCD and shadow costs before delivery.
