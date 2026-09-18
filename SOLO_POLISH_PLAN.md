# Solo polish — user playtest, 2026-09-18

This checklist includes all follow-up messages. User priority overrides the previous Co-op-first backlog.

- [x] Camera: reproduce rotation drift, correct hand orientation, smooth carry, RMB close view, clear recording HUD.
- [x] Keep the approved character mesh and blank black face; scale the complete playable rig down uniformly.
- [x] Movement: responsive walk/run/jump, crouch, slide, low ledge mantle, collision-safe corner lean; physical push/drag.
- [x] Inventory: exactly three real quick slots, number/wheel selection, drop/pickup/full handling, camera stow rules.
- [x] Gravity glove: visible gear, pull/hold/push physical props, weight/range/energy/LOS limits, no free monster control.
- [x] Audio: real microphone input/device/meter/threshold/mute; enemy response to voice and physical world noises.
- [x] Bobby handoff: choose automatic editing or manual editing; automatic path actually completes edit and presents the result.
- [x] Phone: physical handheld interface, Mission, discovered Map/GPS, Equipment, useful Solo Harmony, Settings; battery and RV charging.
- [x] Currency: Credit for purchases/sales; View for three-day quota; persistent progression and no duplicate rewards.
- [x] Home: substantially expand apartment/lobby/garage, distinct furnished rooms, Bobby and supply NPC, functional shop.
- [ ] Hospital: larger navigable layout with loops and distinct detailed spaces; realistic proportions, layered materials, props, lighting, vertical routes.
- [x] Sliding doors retract sideways into wall pockets, with physical clearance, safe closing, and AI navigation.
- [x] Test the actual new input/physics/economy paths and capture gameplay views; Windows revision 6 smoke passes. This does not replace long human playtesting.
- [ ] Update controls, DevLog, known remaining work; commit and push tested checkpoints.

## References and direction

User screenshots: `Downloads/มือถือแปลกๆ.png`, `Downloads/กล้องไม่หมุนตามตัว.png`.
Art: `D:/Projects/SPOOKYTUBER/References`; apartment, B1 lobby, hospital, Bobby, equipment, gameplay and HUD sheets.
GDD v0.5 §§32–37: diegetic phone, shoulder light, traversal, selective physics, gravity glove limits.
Official research: https://landfall.se/content-warning ; https://store.steampowered.com/app/3241660/REPO/
Use original Blender assets and Unity systems. Do not copy game assets or rebuild the approved player model.

## Acceptance boundaries

Solo is the active target. Network Co-op remains future work. A passing automated test is not a claim that art is photorealistic or the complete game is finished. Keep original saves/footage recoverable when changing scenes or career schemas.

Revision 5: hospital size/material/prop/navigation work is delivered; near-realistic final art and full vertical routes remain. Current tests and exact remaining scope are in DEVLOG.md.


Latest user extension: darkness-first Horror, procedural room/corridor growth with days/contracts and traversal hazards, all four GDD locations, Main Menu/Loading/Options, detailed equipment. See PRODUCTION_EXPANSION_PLAN.md; these are active work, not completed claims.
