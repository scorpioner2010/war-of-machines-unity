# Codex Task Handoff

This is the durable state for one active task. A new Codex chat must read this file and inspect the recorded working-tree changes before acting on a request to continue.

## Active task

- Status: `IMPLEMENTED_NEEDS_VERIFICATION`
- Started: 2026-10-18
- Last updated: 2026-10-18
- Original user wording: "супер, я помітив баг після бою ось такий, деякі параметри просто не відображаються чи начислилися чи ні, болтів пише що 0 але я думаю там в реальності не 0, тобюі треба знайти в чому причина та виправит це"
- Objective: diagnose and fix incomplete or incorrect post-battle reward/statistic presentation, particularly zero Bolts when a reward may have been granted.

### Acceptance criteria

1. The post-battle result popup displays the authoritative per-player kills, damage, XP, Bolts, Free XP, and MMR delta for the finished match.
2. A missing backend reward record cannot silently appear as a valid zero-reward result; it is either recovered/retried or presented as pending/error while the player profile refreshes.
3. Backend reward calculation and the client popup use the same stable player identity.
4. The relevant match, API, and UI mechanic documentation describes the current result flow and fallback behavior.
5. Static checks and a targeted verification of the result-payload mapping pass; a real match verification is recorded if it can be run.

### Assumptions

- The backend is the authority for persistent XP, Bolts, Free XP, and MMR rewards.
- Server-side kills and damage remain authoritative room statistics, even if a backend reward lookup is temporarily unavailable.
- Existing pending-result delivery across scene changes must continue to work.

### Work recorded

- Orientation complete: read `README.md`, `match-lobby-spawn.md`, `menus-settings-progression.md`, and `api-resources.md`.
- Screenshot evidence: the result popup showed XP `150` and MMR `-12`, but Bolts, Free XP, Kills, and Damage as `0`.
- Initial ownership map: `GameplaySpawner` sends `PlayerBattleResult`; `MatchResultService` loads backend rewards and builds that payload; `GameResultUI` renders it; `MatchesManager` defines the reward DTO/API call.
- Root cause confirmed in `D:\Programing\ServerProjects\war-of-machines-API\WarOfMachinesAPI\Controllers\MatchesController.cs`: the API calculated and added Bolts/Free XP to the player profile, but `MatchParticipant` did not store either amount and the participant endpoint did not return either field. Unity therefore deserialized absent numbers as zero and rendered them as a valid reward.
- Backend implementation: `MatchParticipant` now persists `BoltsEarned` and `FreeXpEarned`; `20261018120000_AddMatchParticipantRewardAmounts` adds both database columns; end and participant-history responses return `xpEarned`, `bolts`, `freeXp`, `mmrDelta`, and `rewardsCalculated`. Existing historical rows are explicitly identified as incomplete with `rewardsCalculated: false`.
- Client implementation: `MatchesManager` parses `rewardsCalculated`; `MatchResultService` accepts a payload only when each submitted stable `userId` has an authoritative record, and performs the history request even if the end request did not report success. The new `RewardsAvailable` flag crosses `PlayerBattleResult`, the TargetRpc, and the reconnect queue. `GameResultUI` renders reward fields only when authoritative; otherwise it displays pending values and retains the authoritative kills/damage.
- Documentation updated: Unity `match-lobby-spawn.md`, `api-resources.md`, `menus-settings-progression.md`; API `matches-and-rewards.md`, `api-endpoints.md`, and `database-ef-core.md`.

### Verification recorded

- `dotnet build WarOfMachinesAPI.csproj --no-restore -p:OutputPath=C:\Users\TorasPC\AppData\Local\Temp\codex-wom-api-build-20261018\`: passed with 0 warnings and 0 errors. A normal output build could not overwrite the already-running `WarOfMachines.exe` (PID 43016), so it was not stopped.
- `dotnet build Assembly-CSharp.csproj --no-restore -p:OutputPath=C:\Users\TorasPC\AppData\Local\Temp\codex-wom-unity-build-20261018\`: passed with 0 errors (24 existing Unity/FishNet/reference warnings).
- `git diff --check` passed in both repositories. Source mapping confirms persisted `BoltsEarned`/`FreeXpEarned` -> API `bolts`/`freeXp` -> Unity DTO -> `PlayerBattleResult` -> result popup, with `rewardsCalculated` gating the display.

### Remaining work

1. Restart/deploy the API so it loads the new build and startup migration `20261018120000_AddMatchParticipantRewardAmounts`.
2. Complete one real match after that restart and confirm the result popup shows the same nonzero Bolts/Free XP amount that the refreshed profile receives; also confirm a reward-lookup failure shows `pending`, not `0`.

## Completed task history

### Procedural sniper scope mask — implementation complete, visual zoom check deferred

- Previous status: `IMPLEMENTED_NEEDS_VERIFICATION`; preserved when the user started the post-battle result task.
- Replaced `Hud_Aim_Bracket.png` with `ProceduralSniperScopeMask.shader` and its material; `Client.unity/SniperScopeMask` now has no sprite and uses a full-screen procedural circular aperture.
- Unity 6 imported the shader/material with no shader or C# compiler error, `git diff --check` passed, and no content/scene reference to the deleted texture remained.
- Deferred verification: join a match with a sniper-capable vehicle, enter/exit sniper zoom, and confirm circle placement, edge sharpness, and CanvasGroup fade.

### Main-menu robot-card scrolling and preview stability — closed by user without Play Mode verification

- Previous status: `IMPLEMENTED_NEEDS_VERIFICATION`.
- User instructed that this task be closed on 2026-10-18 before beginning the procedural scope-mask task.
- Recorded implementation: `Client.unity` added a horizontal robot-card `ScrollRect`; `RobotView.cs` retains matching cards and horizontal position; `VehicleSlotView.cs` supports in-place updates; `menus-settings-progression.md` documents it.
- Recorded automated checks: `git diff --check` passed (only line-ending warnings); the last 400 lines of `Editor.log` contained no matching compiler errors or exceptions.
- Not verified: the four required Play Mode criteria and the unrelated large diff in `RobotoCondensed-Regular SDF.asset`.
