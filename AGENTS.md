# Project Agent Rules

## Persistent task journal
- Before any repository search, command, analysis, or edit for a new task, read `TASK.md`.
- At the start of every explicitly requested task, append a task entry to `TASK.md` before touching implementation files.
- During the task, update the same entry after meaningful discoveries, decisions, file changes, and validation results.
- Keep the journal append-only: never delete completed task entries or overwrite the history of an interrupted task.
- When the task is finished, set its status to `COMPLETED`, record the result, validation, and any remaining limitations, then close the entry.
- If an entry is still `IN_PROGRESS` or `BLOCKED`, treat a later request to continue it as a recovery request: read the entry, inspect the listed next steps and repository state, then resume from there.

## Focused ownership for new logic
- When adding non-trivial gameplay, networking, UI, bot, or spectator behavior, first identify its domain owner and prefer a dedicated focused class for that responsibility.
- Keep coordinator classes limited to orchestration and lifecycle calls. Do not add team identity, visibility delivery, HUD color policy, or other unrelated state to a camera, network service, or presenter merely because it is nearby.
- New logic should expose small explicit inputs and outputs and be independently testable. Reuse an existing owner only when the behavior is genuinely part of that owner's responsibility; otherwise create a separate class and wire it through serialized or explicit references.
- Before editing an existing class, check whether the proposed logic belongs to a separate domain owner and document the ownership boundary in the relevant `docs/codex-game/` mechanic file.
