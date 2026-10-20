# Project Agent Rules

## Required orientation before coding
- Before editing gameplay, networking, UI, server, bot, weapon, map, or settings code, read `docs/codex-game/README.md`.
- After reading the index, read every mechanic file in `docs/codex-game/` that matches the user task.
- Use the mechanic files as the first map of where to inspect code, then verify details in the actual scripts before changing anything.
- If the user references a file but describes behavior owned elsewhere, follow the mechanic docs and inspect the real owner scripts before editing. Example: `WaypointPointSpawner.cs` is waypoint authoring; runtime bot behavior is mostly `BotNavigator`, `BotCombatController`, and vehicle input/weapon scripts.
- If no mechanic file exists for the touched system, create one in `docs/codex-game/` and add it to `docs/codex-game/README.md`.

## Documentation maintenance
- Every gameplay/behavior change must update the relevant file under `docs/codex-game/` in the same task.
- If a code change moves responsibility between scripts, update the script responsibility map in the relevant mechanic file.
- If a settings field is added, renamed, removed, or its meaning changes, update `docs/codex-game/server-settings.md` and the affected mechanic file.
- If a serialized prefab/scene value is required for the behavior, document the prefab/scene file that owns it.
- Documentation must be concrete: state the current behavior, owner scripts, important settings, and dependencies. Avoid vague notes such as "improved AI".
- Keep mechanic docs short enough to scan, but complete enough that a future Codex can find the right scripts before coding.

## Task continuity
- `docs/codex-game/task-handoff.md` is the durable detailed handoff record for the one active Codex task in this repository; `docs/codex-game/task-history.md` is its canonical append-only status index.
- Before starting any task that can change project files, scene state, settings, or runtime behavior, first create or replace its active record in that file. Record the user's task verbatim, its acceptance criteria, assumptions, and set its status to `IN_PROGRESS` before making the first change.
- When the user says "continue", "продовжуй", or otherwise resumes work without restating it, read `task-handoff.md`, inspect the recorded files and the working-tree diff, then continue only the unfinished work described there.
- Update the record after material progress: list edited files, work still required, blockers, and each verification command or manual test with its result.
- Valid statuses are `IN_PROGRESS`, `IMPLEMENTED_NEEDS_VERIFICATION`, `BLOCKED`, and `COMPLETED`. Do not mark a task `COMPLETED` merely because code was written: record the successful verification that demonstrates every acceptance criterion. A task awaiting Play Mode, build, or user reproduction stays `IMPLEMENTED_NEEDS_VERIFICATION`.
- Before beginning the next task, preserve a compact task summary in `task-history.md`, retain the detailed record in `task-handoff.md`, then replace the active record. Never silently overwrite an unfinished record.

## Git workflow
- Never run `git commit`, `git revert`, or `git reset`.
- Codex may remove changes made during the current task with `apply_patch` when they need to be rolled back.
- Codex may also discard non-critical working-tree changes when doing so is needed to complete the requested task.
- Do not discard user changes that are critical to the requested work without asking first.
- The user handles commits, reverts, resets, and staging.
- If the user explicitly confirms that their current side changes are expected, continue work around those dirty files and do not stop only because they remain modified.
- Stop and ask only when approved side changes conflict with the requested work, break important logic, or make verification unreliable.

## Replacing implementations
- When replacing one implementation, feature, system, asset, or configuration with another, fully remove the obsolete variant and all of its unused remnants so the project stays clean.
- Clean up obsolete scripts, assets, references, serialized fields, prefabs/scenes, settings, tests, documentation, and dead code that belonged only to the replaced variant.
- Preserve a legacy variant only when the user explicitly requests backward compatibility, migration support, or a temporary parallel rollout.

## Code style
- Always use braces for code blocks (`{}`), even for single-line `if`/loops.
- Use `switch` rarely; prefer clear `if`/`else` flows in most gameplay code.

## Performance and implementation constraints
- Prioritize lightweight runtime code for CPU and memory usage.
- Prefer explicit, simple code paths over heavier abstractions when performance matters.
- Avoid LINQ in gameplay/runtime hot paths when a simpler manual implementation is better.
- Do not use reflection in gameplay/runtime code.
- Reflection is allowed only for editor tooling when truly necessary.
- Runtime gameplay code must not create or discover required components with `AddComponent`, `GetComponent*`, `FindObject*`, `FindObjects*`, `GameObject.Find`, tag searches, or `Resources.FindObjectsOfTypeAll`.
- Components and dependencies that can exist in prefabs or scenes must be created and wired in the prefab/scene inspector. Runtime code should use serialized references and only validate/report missing configuration.
- Heavy scene-wide lookup APIs are allowed only in editor tooling or one-off migration scripts, not in gameplay/client/server runtime paths.

# Live Diagnostics Workflow

When the user reports lag, freezes, stutter, desync, rubber-banding, high ping, bad FPS, or server performance issues, do not guess first.

Always run diagnostics before editing gameplay code:

1. Run:
   game-diag health

2. If diagnostics is available, run:
   game-diag analyze --last 10
   game-diag spikes --last 30
   game-diag frame-spikes --last 60
   game-diag top client --last 10
   game-diag top server --last 10
   game-diag network --last 10

3. Classify the issue as one of:
   - CLIENT_BOUND
   - SERVER_BOUND
   - NETWORK_BOUND
   - MEMORY_GC_BOUND
   - ENTITY_SCALE_BOUND
   - RPC_STORM
   - UNKNOWN

4. Cite concrete metrics before proposing a code change.

5. Only inspect code related to the top suspects first.

6. After implementing a patch, ask the user to reproduce the issue and run:
   game-diag analyze --last 30

7. Do not remove diagnostics code unless explicitly asked.

On Windows PowerShell, use `.\game-diag.cmd ...` from the repository root if `game-diag` is not on PATH.
