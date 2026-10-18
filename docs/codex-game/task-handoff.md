# Codex Task Handoff

This is the durable state for one active task. A new Codex chat must read this file and inspect the recorded working-tree changes before acting on a request to continue.

## Active task

- Status: `IMPLEMENTED_NEEDS_VERIFICATION`
- Started: 2026-10-18
- Last updated: 2026-10-18
- Original user wording: unavailable; the prior chat became inaccessible after its usage limit was reached.
- Recovered objective: make the main-menu robot-card list horizontally scrollable, preserve its position and cards while a selected robot updates, and keep the preview platform visible while profile data or a new preview robot loads.

### Acceptance criteria

1. When there are more owned robots than fit in the main-menu row, the player can drag or use the mouse wheel to reveal them.
2. Selecting or loading another owned robot does not reset the current horizontal scroll position or recreate unchanged robot cards.
3. The preview platform/camera environment does not flash black while profile data or the selected robot is loading.
4. Opening the menu and selecting robots produces no Unity Console errors.

### Work recorded

- `Assets/Game/Scenes/Client.unity`: replaced the direct vehicle-card layout container with a horizontal `ScrollRect`, transparent input image, `RectMask2D`, and `Content` child that owns the horizontal layout and content-size fitter; `RobotView.vehicleContainer` now points to `Content`.
- `Assets/Game/Scripts/UI/MainMenu/RobotView.cs`: retains matching vehicle slots/cards by vehicle ID, updates them in place, and separates destroying the preview robot from clearing slots and hiding the platform.
- `Assets/Game/Scripts/UI/MainMenu/VehicleSlotView.cs`: added in-place vehicle-card update support and slot-index refresh.
- `docs/codex-game/menus-settings-progression.md`: documented the scroll view and preview-platform behavior.

### Verification recorded

- `git diff --check`: passed on 2026-10-18; only line-ending warnings were emitted.
- Last 400 lines of Unity `Editor.log`: no matching compiler errors or exceptions found on 2026-10-18.
- Play Mode test: not run or not recorded. This is required before completion.

### Remaining work

1. In Unity, open the Client scene and enter Play Mode.
2. Verify all four acceptance criteria above, including a profile with enough owned robots to overflow the row.
3. Check the Unity Console after changing selected robots.
4. Review `Assets/Game/Content/Fonts/RobotoCondensed-Regular SDF.asset`: it has a large unrelated-looking uncommitted diff and must not be attributed to this task without confirmation.
5. Record the test result here and mark the task `COMPLETED` only if all criteria pass; otherwise record the observed failure and keep the status incomplete.

## Completed task history

No completed task recorded yet.
