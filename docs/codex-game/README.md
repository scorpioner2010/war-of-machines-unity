# Codex Game Knowledge Index

This folder is the first place Codex should read after `AGENTS.md` when a task touches gameplay, networking, UI, bots, or server behavior.

Purpose:
- Give a fast map of existing mechanics before editing code.
- Explain which scripts own which behavior.
- Record current implementation rules and known data flow.
- Keep future Codex sessions from rediscovering the same architecture repeatedly.

Required workflow for Codex:
1. Read `AGENTS.md`.
2. Read this index.
3. Read the mechanic file that matches the user task.
4. Inspect the scripts listed in that mechanic file before writing code.
5. After changing behavior, update the relevant mechanic file in this folder in the same turn.
6. If the task creates a new mechanic or substantially changes ownership, create a new mechanic file and add it to this index.

Mechanic files:
- `ai-bots.md` - bot brain, movement, combat, target acquisition, turret behavior.
- `waypoint-graph.md` - editor waypoint generation, runtime graph, pathfinding.
- `robot-control.md` - vehicle root, input, movement, turret/gun aiming, runtime stats.
- `weapons-damage.md` - reload, firing, projectile prediction, authoritative hit resolution, armor/damage.
- `match-lobby-spawn.md` - matchmaking rooms, additive scene loading, players, bots, spawning, teams.
- `server-settings.md` - global settings prefab, validation, runtime accessors.
- `ui-hud.md` - gameplay HUD, crosshair, reload/ammo, player list, map visibility.
- `api-resources.md` - backend API managers, profile data, robot registry, vehicle prefab/icon lookup.
- `world-maps-spawns.md` - map metadata, scene-scoped spawn points, team spawn selection.
- `menus-settings-progression.md` - main menu, settings screens, development tree, vehicle selection UI.
- `diagnostics.md` - existing diagnostics workflow and tools.

Documentation update rule:
- Any changed gameplay behavior must be reflected here.
- Do not write vague notes like "improved AI". State exact behavior, owner script, important settings, and what other systems depend on it.
- If a mechanic file is missing, create it instead of leaving logic undocumented.
- Keep the script responsibility maps current.

Search hints:
- Use `rg --files Assets/Game/Scripts Assets/Game/Scenes` to find scripts.
- Use `rg -n "ClassName|methodName|settingName" Assets/Game/Scripts Assets/Game/Scenes` for code ownership.
- Prefer the docs here for orientation, then verify details in source code before editing.
