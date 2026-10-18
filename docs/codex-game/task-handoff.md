# Codex Task Handoff

This is the durable state for one active Codex task. A new Codex chat must read this file and inspect the working-tree changes before acting on a request to continue.

## Active task

- Status: `COMPLETED`
- Started: 2026-10-19
- Last updated: 2026-10-19
- Original user wording: "глянь сетінги, продивися код, ссилки, як все налаштовано та реалізовано, і запрограмуй так щоб кнопка аплай була активна тільки тоді коли дійсно щось треба аплаїти, в інших випадках вона не активна, це про сетінги відео."
- Objective: keep the Video settings Apply button disabled until a real video-setting change is pending.

### Acceptance criteria

1. Video Apply is disabled on initial load when controls match the applied baseline.
2. Changing any Video dropdown, VSync toggle, or Gamma slider enables Apply.
3. Reverting controls to the baseline or applying changes disables Apply again.
4. Existing settings controller behavior and non-Video tab Apply behavior remain wired.

### Assumptions

- The applied Video values are represented by the controls populated in `VideoTabView.SetData`, so the baseline is captured from those controls after initialization.
- `SettingsController` continues to update the model immediately; Apply remains the point that persists and applies Video values.
- The existing non-Video tabs keep their current Apply behavior; this task changes only Video pending-state feedback.

### Work recorded

- Added a Video-settings baseline snapshot and `PendingChangesChanged` event to `VideoTabView`.
- Added listener cleanup before rebinding, change notifications for every Video control, and baseline refresh after Apply.
- Updated `SettingsView` to subscribe to the Video pending-state event and drive `ApplyButton.interactable` from the current Video baseline.
- Updated `menus-settings-progression.md` with the Apply-state ownership and lifecycle.

### Verification

- `git diff --check` passed for the two changed settings scripts.
- Unity recompiled without a visible compile-error modal.
- Play Mode with `ClientMenuTestMode`: initial Video Apply was disabled; changing Quality to Balanced enabled it; Apply disabled it after saving; changing back to High Fidelity enabled it again; applying once more disabled it.
- Play Mode was stopped after verification; no scene changes were saved.

### Remaining work

None.

## Completed task history

### Video settings Apply pending-state tracking — completed

- `VideoTabView` now owns the applied Video baseline and reports whether dropdown, VSync, or Gamma values differ from it.
- `SettingsView` updates the serialized Apply button from that state and refreshes the baseline after Video Apply.
- Verified the disabled → enabled → disabled lifecycle in Play Mode with `ClientMenuTestMode`.

### First vertical anchor correction — superseded by current hierarchy diagnostic

- Set all five template-item rects to top vertical anchors while preserving the existing horizontal stretch and 36-pixel row height.
- Initial Play Mode testing appeared improved, but the user's follow-up screenshot shows the first option still clipped and unused backing height below the list; the correction is therefore not accepted as final.

### TMP dropdown rebuild and first visibility repair — completed before current geometry follow-up

- Rebuilt the one Free XP and four Video settings selectors as inspector-wired `TMP_Dropdown` controls, preserving existing artwork, sizes, text, settings effects, and listeners.
- Fixed template validation by keeping every template-item `Toggle` active beneath its inactive `Template`; fixed invisible rows by stretching their horizontal anchors from `x: 0` to `1`.
- Removed the obsolete custom option scripts and references; static scans, compilation, and initial Play Mode selection/list-blocker checks passed.
- The current task follows a later screenshot that exposes the remaining vertical-template offset.

### Settings dropdown Free XP visual clone — superseded by stable TMP rebuild

- Rebuilt the four Video settings controls as custom Free XP-style button/list dropdowns and preserved their value events and VSync frame-rate disabling.
- Static checks and C# build passed, but runtime review found that multiple lists could stay open and list backing height was fixed; this implementation is now intentionally replaced by the active task.

### Settings dropdown behavior migration — superseded

- Replaced four Video `TMP_Dropdown` controls with `SettingsDropdown` and `SettingsDropdownOption` while keeping existing setting events.
- This custom implementation is obsolete under the active project-wide TMP rebuild.

### Free XP dropdown — incorrect inferred refactor discarded

- The Free XP selector was temporarily converted after an inferred request, then restored when the user clarified Free XP was the desired visual reference.

### Post-battle rewards — implementation complete, deployment and match verification pending

- Persisted and returned Bolts/Free XP reward amounts, routed their authoritative availability through the result popup, and documented the result flow.
- Builds and payload mapping checks passed; pending: deploy the API migration and verify a completed live match.

### Procedural sniper scope mask — implementation complete, visual zoom check deferred

- Replaced `Hud_Aim_Bracket.png` with `ProceduralSniperScopeMask.shader` and its material; `Client.unity/SniperScopeMask` has no sprite and uses a full-screen procedural circular aperture.
- Unity imported the shader/material without compiler errors; pending: match visual verification.

### Main-menu robot-card scrolling and preview stability — closed by user without Play Mode verification

- `Client.unity` includes a horizontal robot-card `ScrollRect`; `RobotView` preserves position while refreshing cards.
- Automated checks passed; required Play Mode checks were closed by user before beginning subsequent work.
