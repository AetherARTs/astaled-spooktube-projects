# Revision 5 debug ledger

## Held camera rotation

User screenshots show rolled equipment while the world stays upright.
Repro: `SpookTuber.Editor.SoloPlayCheck.CameraRepro`, ProductionHouse, pick up MainCam, rotate yaw continuously at 180 degrees/second and pitch sinusoidally for three seconds.
The initial stationary rotation check passed (0 degrees). This disproved a permanently wrong parent/axis hypothesis; it did not test the reported moving state.
Continuous movement failed: peak 179.9484 degrees; captured camera roll 45.69 degrees while view roll was zero. Kinematic camera Rigidbody still had Interpolate enabled and UpdateHeldPose wrote position only.
No attached debugger was available in the batch player. Source trace and the paired moving/stationary probes isolate interpolation ownership. The held camera now disables Rigidbody interpolation and writes its view-local rotation each pose update; dropping restores interpolation. Added RMB close-view blend and 30 Hz LCD updates.
The same continuous-turn check passes after the fix; detailed measurements are in v5_camera_probe.txt. Arm reach and visual grip still require the locomotion/inventory integration check.
