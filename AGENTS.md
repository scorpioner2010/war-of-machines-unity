# Project Agent Rules

## Git workflow
- Never run `git commit`, `git revert`, `git reset`, or discard changes automatically.
- The user handles all commit/revert/discard actions manually.
- If rollback/revert/discard is needed, explain what should be done and wait for the user to do it.
- In most cases, the user decides what to do with git state and staging.

## Code style
- Always use braces for code blocks (`{}`), even for single-line `if`/loops.
- Use `switch` rarely; prefer clear `if`/`else` flows in most gameplay code.

## Performance and implementation constraints
- Prioritize lightweight runtime code for CPU and memory usage.
- Prefer explicit, simple code paths over heavier abstractions when performance matters.
- Avoid LINQ in gameplay/runtime hot paths when a simpler manual implementation is better.
- Do not use reflection in gameplay/runtime code.
- Reflection is allowed only for editor tooling when truly necessary.

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
