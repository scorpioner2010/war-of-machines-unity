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
