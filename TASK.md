# Persistent Task Journal

This file is the durable handoff point between Codex sessions. Keep entries append-only.

## Task: Transfer TEST transport audio demo

- **Status:** COMPLETED
- **Started:** 2026-11-03
- **User request:** Move the completed `TEST` primitive transport/audio demo from `C:\Users\TorasPC\Documents\Codex\2026-11-01\new-chat-3\work\UnityAssetScratch\Assets` into this project's `Assets` folder.
- **Scope:** Copy the isolated demo scene, TestTransport scripts, engine profiles, source engine recordings, and Unity `.meta` files into a dedicated destination folder while preserving existing project changes and avoiding unrelated assets.
- **Acceptance criteria:** The destination contains the full demo dependency set with valid GUID references; source files remain unchanged; duplicate GUIDs and overwrite risks are checked; destination Unity import/compilation is validated where safe.

### Progress

- Confirmed the destination project has no `AGENTS.md` instructions beyond its persistent journal and focused-ownership rules.
- Confirmed the destination Unity Editor is not open; only the source project's Unity Editor and its import workers are running.
- Source dependency inspection found one scene, five `TransportAudioProfile` assets, six focused C# scripts, and the referenced engine recordings; built-in URP/camera references remain package/Unity references.
- Copied the demo into `Assets\\TransportAudioDemo`, preserving all source asset `.meta` files and adding the required folder `.meta` files; 90 files were copied.
- Destination path and demo GUID collisions were absent before copy; both projects use Unity `6000.3.13f1`.
- Static validation passed for 90/90 hashes, zero missing files, zero duplicate demo GUIDs, and two expected external URP GUIDs from the scene.
- The first destination import exposed a target-project compatibility issue: the destination does not include the Input System package and uses `activeInputHandler: 0`.
- Adapted only the copied `TestTransportDemo.cs` to Unity's built-in legacy input API and corrected the copied README paths; no package or unrelated gameplay files were changed.
- Final validation passed: 88/88 unchanged assets match source hashes; the only two intentional content differences are the destination-only legacy-input controller and corrected README paths. Unity headless import/compile exited with code `0`; no C# errors, exceptions, or compilation failures were reported in `Logs\\Codex_TransportAudioDemo_Import.log`.
- **Result:** The complete demo is available at `Assets\\TransportAudioDemo\\TEST.unity` with local scene/profile/audio/script references intact; existing destination project files were left untouched except for this journal and the new demo package.
- **Remaining limitation:** The scene was not manually played in the destination Editor, so final visual/audio audition remains a user-side check.
- **Completed:** 2026-11-03.
- **Status:** COMPLETED

## Task: Repair declined friend request resend in two built clients
- **Status:** IN_PROGRESS
- **Started:** 2026-11-19
- **User request:** When account 1 sends a friend request to account 2, account 2 declines it, and account 1 sends the request again, the second notification does not arrive. Reproduce the exact two-client launcher scenario, fix the bug, test it in the built clients, and add useful persistent logs for future build testing.
- **Scope:** Inspect the API friendship lifecycle, Unity client request/notification state, launcher/build logging, and the existing social regression harness. Preserve the completed social/platoon work and unrelated working-tree changes.
- **Acceptance criteria:** The exact `1/1` to `2/2` send/decline/resend flow delivers the second notification; declined requests can be sent again without stale duplicate state; both API and Unity layers log request IDs, sender/recipient, lifecycle action, result, and errors to files; focused automated validation passes; the built-client flow is tested as far as the local environment permits.
- **Initial state:** Previous task history reports API decline/resend coverage, but no manual authenticated two-client UI playtest. The user now reports the failure in the launcher-built clients, so existing confidence must be rechecked against the actual runtime path.
- **Discovery:** The API `SocialController` reopens the canonical friendship row for both `Declined` and `Cancelled`, and the existing API smoke test already covers decline/resend. The Unity panel currently polls social snapshots only while its panel is open, every six seconds, and `SocialServer` only sends the friend-request action response to the sender; the recipient has no server-pushed request notification. The desktop client executable predates this task and the launcher has no dedicated social lifecycle log.
- **Implementation checkpoint:** Added API lifecycle logging and authoritative send-response metadata, server-routed `TargetIncomingFriendRequestRpc` delivery by online profile user ID, immediate client snapshot refresh, panel notification/open behavior, and a persistent Unity `social-lifecycle.log` under `Application.persistentDataPath`. Extended `tools/Test-SocialApi.ps1` to validate request endpoint metadata.
- **Launcher checkpoint:** Updated `Start War of Machines.bat` to preserve a stable `D:\Programing\war-of-machines-local-builds\ServerRuntime` copy before the client build, preventing the running dedicated server from losing its executable during the second build step. The launcher now prints the client, API, server, and social log paths.
- **Validation checkpoint:** API build passed with 0 errors; Unity batch import regenerated the project files and `dotnet build Assembly-CSharp.csproj --no-restore` passed with 0 errors and existing dependency warnings. The launcher completed successfully, produced the desktop Windows client, started the local API and dedicated server, and the live social smoke test passed all decline/resend, cancel/resend, duplicate, permission, friend, and message assertions.
- **Runtime checkpoint:** The built client connected to the launcher-built dedicated server on port `7770`; its Unity log records `social_server_client_started`, and the server log records a remote connection. The CUA session could not type into the client because a system `PickerHost.exe` overlay intercepted all mouse/keyboard input, so the exact two-client authenticated UI sequence was not completed.
- **Account checkpoint:** The live API scenario using the supplied `1/1` and `2/2` accounts declined the existing request, resent the same friendship row, verified the request in account 2's incoming list, and cleaned up by declining it again. API logs show the matching `declined` and `pending` transitions for friendship `5`.
- **Result:** Declined requests now reopen the canonical friendship row and deliver a targeted notification to the online recipient, with immediate recipient refresh and persistent diagnostics for future builds.
- **Limitations:** Automated API and compiled Unity/network validation passed; the final manual two-client UI confirmation remains blocked by the local CUA `PickerHost` overlay rather than by an application error.
- **Completed:** 2026-11-19.

## Task: Summarize current Git implementation in API and Unity

- **Status:** IN_PROGRESS
- **Started:** 2026-11-20
- **User request:** Детально описати, що саме зараз реалізовано в поточних незакомічених змінах Git для Backend API та Unity Client/Dedicated Server.
- **Scope:** Лише аудит і пояснення поточного diff, без редагування implementation files, без reset/revert/commit і без зміни робочого коду.
- **Acceptance criteria:** Перевірити статус і diff обох репозиторіїв, згрупувати зміни за функціональними блоками, відокремити поточну реалізацію від документації/конфігурації та зафіксувати перевірені обмеження.
- **Findings:** Unity HEAD `b705d688` і API HEAD `f24f966` мають незакомічені security hardening, local launcher та gameplay diagnostics changes. API додає sessions, connection tickets, dedicated-server registry/auth, server-only match contracts, JWT validation, rate limiting, ProblemDetails, CORS/forwarded-header controls та additive EF migration. Unity додає HTTPS allowlist, FishNet ticket authenticator/DTLS configuration, authenticated session gates, server discovery/heartbeat, secure match flow, profile readiness retry, spawn/camera/bot/weapon diagnostics та local development build wiring.
- **Findings:** Поточний live-validated path для акаунта `1/1` включає API login/profile, 8 menu robots, server discovery, ticket admission, additive scene acknowledgement, player spawn, owner camera binding, 19 bot brains, authoritative shots та battle completion. API/Unity docs і `SECURITY_HARDENING_REPORT.md` описують контракти, environment variables, migration та deployment checklist.
- **Scope note:** Generated API `bin/obj` files, task journals, docs and local build-support files are present in the working tree; they are not additional runtime features. No commit, reset or revert was performed.
- **Validation:** Previous task validation recorded API build 0 warnings/0 errors, Unity `Assembly-CSharp.csproj` and `Game.PlayMode.Tests.csproj` builds with 0 errors, API `1/1` login/profile HTTP 200, and live built-client/server runtime diagnostics. Current task performed read-only diff/status/source/documentation inspection.
- **Result:** Detailed Ukrainian implementation summary is ready; current working-tree changes remain intact for the user’s review and commit decision.
- **Limitations:** This summary does not claim production secret rotation, production migration/deployment, staging DTLS verification, full automated Unity batch PlayMode, or a two-client/reconnect regression beyond the previously captured local single-client live scenario.
- **Completed:** 2026-11-20.
- **Status:** COMPLETED

## Task: Fix login error and verify entering the game

- **Status:** IN_PROGRESS
- **Started:** 2026-11-20
- **User request:** Виправити помилку, показану на скриншоті під час логіну, протестувати логін і вхід у гру на локальному акаунті 1.
- **Scope:** Діагностувати поточний Unity client/API/Dedicated Server login flow, внести мінімальне виправлення першопричини, перевірити авторизацію та перехід у гру без зміни сторонніх систем.
- **Acceptance criteria:** Причина помилки підтверджена; login flow виправлений; локальний акаунт 1 успішно входить; клієнт проходить до ігрового/lobby стану або зафіксовано точний зовнішній блокер; перевірки й обмеження записані.
- **Next step:** Оглянути прикріплений скриншот, робоче дерево, launcher/API/client logs та login-related code.
- **Finding:** Скриншот містить HTTP 400 validation problem: `Username` must be 3–32 characters. `RegisterServer` передає поле після trim без локальної валідації, а API `AuthController.LoginRequest.Username` має `[StringLength(32, MinimumLength = 3)]`; тому введення username `1` ніколи не доходить до перевірки акаунта.
- **Finding:** Local PostgreSQL contains `Players.Id=2, Username=1` and `Players.Id=3, Username=2`; the earlier task journal records the supplied local credentials as `1/1` and `2/2`. The current API validation rejects both legacy accounts before password verification.
- **Decision:** Preserve registration minimums (`username >= 3`, `password >= 8`) and change only login username validation to accept existing 1–32 character accounts; this restores compatibility without creating new weak accounts.
- **Change:** Updated API `AuthController.LoginRequest.Username` minimum length from 3 to 1; registration validation remains unchanged. Documented the split contract in `docs/codex-app/authentication-authorization.md`, `api-endpoints.md`, and `validation-and-error-handling.md`.
- **Validation checkpoint:** First API build attempt reached compilation but failed copying the locked `WarOfMachines.exe` because the old local API process was still running; no source compilation error was reported. The stale process must be stopped before rebuilding.
- **Next step:** Build/restart the local API, verify `1/1` login and profile access, then exercise the built Unity client through the lobby/game-entry path.
- **User clarification:** The user manually entered `1/1` after the client was launched. The observed runtime regressions are: the Main Menu is empty, the robot/robot list does not load, entering a match leaves the camera far from the spawn point, and the robot is not spawned.
- **Expanded acceptance criteria:** Restore local testing after the current security-hardening changes without regressing the implemented auth/server/session/social behavior; make profile/robot loading, lobby-to-game spawn, camera target, bots, firing, and match lifecycle observable through persistent logs; perform deep local runtime and focused automated validation.
- **Next step:** Inspect the current uncommitted security diff and runtime logs, then trace profile loading, lobby/gameplay spawn, camera ownership, bot population, and weapon events before editing.
- **Continuation checkpoint:** The runtime log confirms `ProfileServer.UpdateProfile()` called the RPC before the FishNet client was active. The server then had no cached profile, so `MatchVehicleSpawner.SpawnPlayerAsync()` returned before creating the owner robot; the missing owner target explains the distant camera.
- **Continuation decision:** Queue and retry profile loading only after the authenticated FishNet client/object are initialized, and add a focused persistent gameplay event log for profile, scene, spawn, camera, bot, shot, and match lifecycle events.
- **Change:** Added queued/retried profile loading in `ProfileServer`, profile-required matchmaking rejection, and lifecycle logging across login, server discovery, lobby, additive scene loading, player/bot spawn, owner-camera binding, bot brain, firing/hits, and battle completion.
- **Change:** Added `GameplayRuntimeDiagnostics` with append-only `Logs/GameplayRuntimeDiagnostics.log` output beside each local client/server executable and documented the ownership/format in the gameplay docs.
- **Continuation next step:** Compile the Unity/API changes, rebuild the local runtime, and rerun login-to-match while inspecting the new event log.
- **Status:** COMPLETED
- **Continuation validation:** API build passed with 0 errors; direct `1/1` login returned HTTP 200 and `GET /players/me` returned HTTP 200 for user id 2 with 8 owned vehicles. Unity `Assembly-CSharp.csproj` and `Game.PlayMode.Tests.csproj` both built with 0 errors; `git diff --check` passed apart from existing line-ending warnings.
- **Continuation runtime validation:** Fresh rebuilt client/server logs show profile request after `client_initialized=True client_started=True`, 8 menu slots, accepted matchmaking, server scene load and client acknowledgement, player spawn at `(-6.40, 1.88, 16.64)`, owner camera target binding, 19 bot brains, and 20 active vehicles.
- **Continuation gameplay validation:** The authoritative server recorded 44 shot resolutions, including player no-penetration and penetration damage, bot shots/hits, bot brain shutdown, and a clean draw/battle completion followed by profile refreshes. No new server exception or FishNet inactive-client warning appeared in the captured runtime diagnostics.
- **Continuation result:** Local account `1/1` now loads the Main Menu robot roster, enters a match with the selected robot spawned at a valid team spawn, and follows it with the gameplay camera while the existing authentication/session/security/social changes remain in place.
- **Continuation limitation:** Computer Use was stopped by the user’s physical Escape key before the final gameplay screenshot; the runtime and persistent logs still captured the complete spawn, camera, bot, weapon, and battle-end path. Full Unity batch PlayMode execution was not run because the project Editor remained open; test assembly compilation and the live built-client scenario passed.
- **Continuation completed:** 2026-11-20.

## Task: Audit authentication, FishNet, server discovery, and Steam integration

- **Status:** IN_PROGRESS
- **Started:** 2026-11-19
- **User request:** Analyze the project with access to the Unity Client, Dedicated Server, and Backend API; describe the current authorization/token-validation path from login through game-server entry, FishNet behavior, server registration, ping measurement, transport, security, and a minimal-change Steam integration plan.
- **Scope:** Read-only audit of client, dedicated-server, backend, configuration, deployment, and documentation paths. Do not modify implementation code, Unity assets, scenes, or runtime configuration.
- **Acceptance criteria:** Produce a detailed Ukrainian report with concrete file and line references, distinguish observed behavior from inference, identify security gaps and operational dependencies, and propose a low-cost Steamworks Auth Ticket/Friends/invite/optional Steam Networking integration without an architectural rewrite.
- **Progress:** Global instructions, repository journal, and task-journal recovery workflow were read. No implementation files have been changed for this analysis.
- **Discoveries:** FishNet has no configured authenticator; login/password are sent through unauthenticated `ServerRpc` calls to the dedicated server, which calls the backend and stores the JWT by FishNet client ID. Matchmaking normally requires a cached profile, while the public in-memory server status endpoint has no registration credential and is last-writer-wins. The active Fishy Unity Transport is direct UDP with encryption disabled; UI ping is FishNet RTT adjusted by tick duration, and the diagnostics packet-loss value remains the transport default. No Steam integration/package references were found.
- **Decisions:** Recommend preserving the current dedicated-server/JWT contract and adding optional Steam ticket login/linking through the backend; keep existing Friends/party ownership in `PartyManager`; defer Steam Networking/SDR until direct UDP/NAT requirements justify a transport change.
- **Validation:** Static source/config/documentation audit completed across the Unity client, dedicated-server paths, backend API, FishNet/FishyUnityTransport package, and official Steamworks documentation. No build, runtime, deployment, or network penetration test was run; no implementation files, scenes, assets, or runtime configuration were changed.
- **Result:** Delivered the requested Ukrainian read-only audit covering client discovery, FishNet connection and RPC flow, backend JWT validation, server heartbeat registration, ping/diagnostics, transport encryption, security gaps, and a phased Steamworks integration plan.
- **Validation:** Reconciled source/configuration references across the Unity project and backend, checked FishNet/FishyUnityTransport implementation settings, searched for existing Steam integration, and consulted official Steamworks authentication, Friends, Steam Networking, and SDR documentation. No build, runtime, deployment, or network penetration test was run.
- **Limitations:** Runtime behavior, production secrets, firewall/NAT behavior, live backend deployment, Steam partner configuration, and actual server/API logs were not independently exercised during this read-only audit.

## Task: Repair social refresh, first-message flow, and button text layout
- **Status:** COMPLETED
- **Started:** 2026-11-19
- **User request:** After accepting a friend request, the requester’s friend list stays stale until the client restarts; the first message has a bug; button text is not centered; add background coverage for every social button/action and repeat it with Latin and Cyrillic accounts.
- **Scope:** Inspect the current Unity social client lifecycle, API/server/client logs, the attached UI screenshot, and existing smoke coverage. Fix authoritative refresh and first-message behavior at the responsible owners, correct button text alignment without regressing responsive layout, extend repeatable background tests to all social actions and Unicode usernames/messages, and validate several runs.
- **Acceptance criteria:** Both clients refresh friends immediately after accept; the first message sends and appears on the recipient without a restart; all button labels are centered; automated coverage exercises search, request, decline/resend, accept, remove/re-add, cancel/resend, messages/read state, and platoon actions where the current API/server contract supports them; repeated Latin/Cyrillic runs pass and logs explain failures.
- **Initial state:** The previous task added targeted friend-request notifications and persistent diagnostics but did not cover friend-list refresh after accept, first-message UI timing, or automated button/layout assertions.
- **Findings:** The old accept/decline/cancel path refreshed only the actor client. The prior first-message runtime log showed `POST /social/friends/3/messages` returning HTTP 200, then FishNet kicking a client for unhandled packet `46288`; the client logged `messageSent` but never logged `messages_received`. The button label had centered TMP alignment but no explicit full-rect offsets, making visual centering dependent on the generated layout.
- **Implementation checkpoint:** API transition responses now include both participant IDs and status. `SocialServer` routes transition refreshes to the other online participant, returns a persisted message immediately to the sender, and logs message/transition/receive diagnostics. `PlatoonSocialPanel` keeps failed messages available for retry and gives every button label a full centered RectTransform. Added `tools/Test-SocialUiContracts.ps1` and expanded `tools/Test-SocialApi.ps1` to cover all supported social actions with Latin/Cyrillic UTF-8 accounts/messages.
- **Validation checkpoint:** API build passed with 0 warnings and 0 errors. Unity `Assembly-CSharp.csproj` build passed with 0 errors; remaining output is existing Unity/dependency warnings plus the obsolete TMP wrapping warnings removed from the button path. UI contract checks passed. The expanded API lifecycle smoke passed 3 iterations before and 3 iterations after the fresh launcher rebuild. The launcher rebuilt API, dedicated server, and Desktop Windows client successfully; server status reports `ready`, `127.0.0.1:7770`, and a live heartbeat.
- **Runtime limitation:** The Computer Use native pipe was unavailable on two attempts, so a fresh two-window mouse/keyboard replay could not be driven from this environment. The built client and server were produced from the same launcher run, and the runtime logs are available for the user's manual two-client replay.
- **Result:** Fixed stale requester-side friend state by routing friendship transitions to both online participants, fixed first-message completion/diagnostics, and made button labels explicitly centered and full-rect. Added repeatable API and UI-contract background tests.
- **Remaining limitation:** A literal two-window authenticated UI replay could not be completed because the native UI automation helper was unavailable; API lifecycle, source contracts, Unity compilation, fresh launcher build, and runtime server readiness were validated.
- **Completed:** 2026-11-19.

## Task: Debug Start War of Machines launcher and platoon MVP
- **Status:** IN_PROGRESS
- **Started:** 2026-11-18
- **User request:** The user launched `Start War of Machines.bat`, but the newly implemented platoon/social system does not work; identify and fix the actual startup/runtime failure.
- **Scope:** Inspect the launcher location and commands, Unity/API/server logs, current working-tree state, scene/network registration, and runtime configuration. Preserve the completed platoon implementation and all existing user changes.
- **Acceptance criteria:** Determine the failure cause from logs or reproducible behavior, patch the smallest root cause, validate the launcher/API/Unity path as far as the environment allows, and record an interruption-safe checkpoint.
- **Progress:** Previous platoon MVP task is closed and compiled successfully. The expected `.bat` file was not present under the Unity repository root; launcher discovery is the next step.
- **Recovery next step:** Locate `Start War of Machines.bat`, capture its exact commands/output, then inspect each launched process and API/Unity log before editing implementation files.

## Task: Estimate simple two-player platoon MVP

- **Status:** IN_PROGRESS
- **Started:** 2026-11-18
- **User request:** Estimate the effort to implement a simple two-player platoon MVP with friend messaging, invitations, custom UI, and working matchmaking logic.
- **Scope:** Provide a realistic implementation estimate based on the inspected Unity/FishNet and ASP.NET API architecture; do not change implementation files.
- **Progress:** Existing auth, player profiles, MMR, server rooms, team assignment, and match-result persistence are available. Party, friendship, messaging, and invitation systems are not implemented.
- **Result:** The current foundation makes this a medium-complexity MVP rather than a from-scratch system. A rough estimate is 18–34 active development hours, or 2–4 working days for one experienced developer, assuming a simple direct-message model and one match-server topology.
- **Validation:** Estimate based on the previous repository/API inspection; no implementation or runtime changes made.
- **Limitations:** The estimate excludes polished art/UI, moderation, push notifications, multi-server queueing, voice chat, and production-scale reconnect/failover.
- **Completed:** 2026-11-18.
- **Status:** COMPLETED

## Task: Debug Start War of Machines launcher and platoon MVP
- **Status:** IN_PROGRESS
- **Started:** 2026-11-18
- **User request:** The user launched `Start War of Machines.bat`, but the newly implemented platoon/social system does not work; identify and fix the actual startup/runtime failure.
- **Scope:** Inspect the launcher location and commands, Unity/API/server logs, current working-tree state, scene/network registration, and runtime configuration. Preserve the completed platoon implementation and all existing user changes.
- **Acceptance criteria:** Determine the failure cause from logs or reproducible behavior, patch the smallest root cause, validate the launcher/API/Unity path as far as the environment allows, and record an interruption-safe checkpoint.
- **Progress:** Located the launcher at `C:\Users\TorasPC\Desktop\Start War of Machines.bat`. Reproduced the failure: it exits with code `1` before Docker/API startup because no Unity Editor process is open for this project. Existing local API/server logs are stale artifacts from an earlier run.
- **Decision:** Make the launcher start the installed Unity Editor for this project automatically, wait until that project-specific process is visible, and retain a clear timeout/error path instead of requiring a manual pre-step.
- **Files to change:** Desktop `Start War of Machines.bat`; keep the Unity build-request handler unchanged unless full startup validation exposes a separate issue.
- **Change made:** Replaced the hard failure on a missing Unity Editor with automatic discovery under the Unity Hub Editor folders, project launch via `-projectPath`, and a five-minute readiness timeout.
- **Recovery next step:** Run the launcher end-to-end, inspect API/server logs, then update this entry with validation and any remaining limitation.

## Task: Analyze platoon feature feasibility

- **Status:** IN_PROGRESS
- **Started:** 2026-11-18
- **User request:** Analyze the project API and determine how difficult it would be to add World of Tanks-style platoons.
- **Scope:** Inspect Unity lobby/networking flows, backend API/domain models, matchmaking, team assignment, persistence, and relevant documentation without changing implementation files.
- **Acceptance criteria:** Identify existing reusable foundations and missing capabilities; distinguish a minimal party queue from a complete WoT-style platoon system; estimate complexity, risks, and implementation phases.
- **Progress:** Global instructions, task-journal recovery workflow, and existing journal were read. No implementation files have been changed for this analysis.
- **Findings:** `LobbyManager.FindMatchServerRpc` creates or joins a server-side `ServerRoom` one player at a time; `LobbyRooms.GetNotFullAutoRoom` does not filter by party, map, or MMR. `ServerRoom.AssignTeams` sorts and assigns individual players, so a future platoon would currently be split across Team A/Team B. The active server prefab targets 20 total participants and fills missing slots with bots.
- **API findings:** The ASP.NET API has JWT players, MMR, vehicle ownership, match start/end, and persisted `MatchParticipant.Team` data, but no friend search, invitations, party entity, party membership, presence, queue, or ready-state endpoints. `POST /matches/start` accepts only a map; match participants and rewards are submitted later. `EndMatch` trusts participant `UserId` values from the request body, which should be secured before production social/matchmaking features.
- **Decision:** Recommend a dedicated `PartyManager`/party service separate from `LobbyManager`. For an MVP, keep online party state in the FishNet Unity server, add party-aware queue capacity and group-preserving team assignment, and add only a player lookup/invite contract if needed. Add PostgreSQL party/friend persistence only for reconnects, cross-server operation, history, and a full social system.
- **Effort estimate:** A 2–3 player same-server MVP is medium complexity, roughly 1–2 weeks for one experienced developer including UI and QA. A robust World of Tanks-style feature with friends, invites, leader transfer, reconnects, atomic queueing, MMR expansion, multi-server routing, persistence, anti-cheat validation, and tests is high complexity, roughly 4–8+ weeks depending on the intended product depth.
- **Inspected:** `LobbyManager`, `ServerRoom`, `LobbyRooms`, `MatchTeam`, `GameplaySpawner`, `ServerSettings`, Unity match API client, backend `Player`, `Match`, `MatchParticipant`, `MatchesController`, `PlayersController`, EF mappings, API docs, and server settings prefab. No implementation files were changed for this analysis.
- **Validation:** Static repository inspection and route/model searches completed. No runtime matchmaking, API, or database migration tests were run because the request was analysis-only. Backend working-tree `bin/obj/log` changes were observed and left untouched.
- **Result:** Platoons are feasible without rewriting the combat system. The fastest safe path is a server-authoritative in-memory party MVP followed by a separate persistent social/matchmaking layer for the full WoT version.
- **Limitations:** The estimate assumes the current single Unity match-server topology; a central multi-server queue or dedicated backend realtime service would increase scope.
- **Completed:** 2026-11-18.
- **Status:** COMPLETED

## Task: Repair broken Transform reference in Client scene

- **Status:** IN_PROGRESS
- **Started:** 2026-11-18
- **User request:** Fix the repeated Unity scene-opening warning for `Assets/Game/Scenes/Client.unity`: `Component (Transform) has a broken GameObject reference. Fixing.`
- **Scope:** Identify the invalid serialized scene reference, repair only the affected scene/prefab data, and validate that no dangling Transform-to-GameObject references remain.
- **Acceptance criteria:** The source of the warning is identified or narrowed to a concrete serialized object; the minimal safe repair is applied; scene-reference checks and diff validation pass; unrelated working-tree changes remain untouched.
- **Progress:** Global instructions, task-journal recovery workflow, and existing journal were read. No repository-specific search or scene edit has been performed for this repair task yet.
- **Findings:** `Editor.log` reproduces the warning during both scene restore and direct AssetDatabase opening. The concrete malformed block is `Client.unity`'s unstripped `AudioListenerRoot` Transform (`&2200100000001`): it lacked `m_GameObject`, while its owner is GameObject `2200100000000` and its AudioListener/AudioListenerRoot components already reference that owner.
- **Changes:** Added `m_GameObject: {fileID: 2200100000000}` to the `AudioListenerRoot` Transform in `Assets/Game/Scenes/Client.unity`.
- **Validation:** The scene now has 53 Transform blocks, 2 expected stripped prefab Transforms, and zero missing or zero-valued owner references on unstripped Transforms. `git diff --check` passed. Unity Editor is open on this project, so no concurrent batch validation was started; the existing log entries are from before this repair.
- **Result:** The serialized `AudioListenerRoot` Transform now correctly points to its owning GameObject, removing the concrete source of Unity's broken-reference warning.
- **Limitations:** A fresh open of the scene in the already-running Editor is still needed to confirm a new log entry is clean; no automatic Unity reload was observed after the external YAML edit.
- **Completed:** 2026-11-18.
- **Status:** COMPLETED

## Task: Investigate hit-induced freeze and particle effects

- **Status:** IN_PROGRESS
- **Started:** 2026-11-10
- **User request:** Investigate the persistent hitch/freeze that occurs when a shot hits an enemy, first inspect the particle effects used by the game, fix the root cause if it can be established confidently, and otherwise prepare focused diagnostics for a reproducible in-game capture.
- **Scope:** Inventory hit, impact, damage, destruction, and related particle assets and runtime call sites; inspect for repeated instantiation, unbounded particle settings, synchronous work, or duplicate network/event processing; make only a root-cause fix supported by evidence, or add narrowly scoped diagnostics if evidence is insufficient.
- **Acceptance criteria:** Identify whether a particle or another hit-processing path causes the hitch; preserve gameplay/network authority boundaries; validate any fix or diagnostic changes; provide three practical ways to isolate the issue and clearly state confidence and remaining limitations.
- **Progress:** Task started after reading the global memory instructions, repository journal, task-journal recovery workflow, gameplay-logic separation guidance, and background Unity testing workflow. No implementation files changed yet.
- **Findings:** Combat impact prefabs use finite particle counts and are pooled; robot-hit prefabs additionally configure two decal projectors and a glow halo. The robot-hit presentation path calls `VehicleColliderRegistry.TryGetImpactVisualSurface`, which scans roughly 10–17 registered mesh colliders per active robot. Each mesh-collider raycast currently activates/deactivates parents as needed and calls `Physics.SyncTransforms()` even when the collider and hierarchy are already active, making repeated synchronous physics work a strong freeze candidate.
- **Ownership decision:** Keep impact-surface lookup in `VehicleColliderRegistry` as the focused visual-surface owner; keep hit authority and FX orchestration in the existing projectile/network owners. Apply a narrow optimization to skip `Physics.SyncTransforms()` when no active/enabled state changed.
- **Changes:** Updated `Assets/Game/Scripts/Gameplay/Robots/VehicleColliderRegistry.cs` so `Physics.SyncTransforms()` runs only when the visual mesh collider or one of its ancestors had to be enabled/activated for the raycast. Updated `docs/codex-game/weapons-damage.md` with the ownership and performance boundary.
- **Next steps:** Patch the redundant synchronization, validate compilation/static behavior, and document the result and remaining runtime confidence.

### Final update

- **Changes:** Added `Projectile.ImpactSurface.Resolve` profiling around the shared visual-surface lookup so a runtime capture can separate mesh-collider correction from particle playback.
- **Validation:** `git diff --check` passed. Unity `6000.3.13f1` batch compilation completed with exit code `0`, no C# errors, and no `Compilation failed` marker. Targeted source/doc assertions passed.
- **Runtime limitation:** The PlayMode test could not start because Unity reported that another instance already had the project open; no Unity Editor process was safe to terminate automatically. The fix therefore has static and compile validation, but not an in-game hitch capture yet.
- **Result:** Particle assets are finite and impact FX are pooled; the strongest confirmed candidate is repeated `Physics.SyncTransforms()` during robot visual-surface correction, not an unbounded particle emission path. The redundant sync is now skipped for already active/enabled visual colliders.
- **Next runtime checks:** (1) reproduce with the Unity Profiler and inspect `Physics.SyncTransforms` plus `Projectile.ImpactSurface.Resolve`; (2) compare Deep Profile/Timeline around one hit with robot impact FX disabled; (3) use the existing diagnostics overlay/HTTP top scopes to compare `Projectile.ImpactSurface.Resolve`, `Projectile.ImpactFx.Spawn`, and `Projectile.ImpactFx.Play` during the hitch.
- **Completed:** 2026-11-10.
- **Status:** COMPLETED

## Task: Import Fires prefabs and resources into Particles111

- **Status:** COMPLETED
- **Started:** 2026-11-01
- **User request:** Transfer the prefabs and their resources from `C:\Users\TorasPC\Documents\Codex\2026-11-01\new-chat-3\work\UnityAssetScratch\Assets\Fires` into this game project under `Particles111`.
- **Scope:** Inspect the source asset tree and target project structure, copy the complete Unity asset bundle including `.meta` files and referenced local resources, preserve existing project files, and validate that copied references remain internally resolvable.
- **Acceptance criteria:** The requested prefabs and their required source resources exist under the game's `Particles111` folder; Unity `.meta` files are preserved; no unrelated files are changed; validation results and any limitations are recorded here.

### Progress

- Confirmed the shared Codex memory and project task journal were read before repository inspection.
- No exact archive workflow for Unity asset transfer was found; applying the repository's journal and safe Unity asset handling rules.
- The source `Assets\\Fires` tree contains top-level prefabs plus `Explosions` and `Unlit` subfolders; the target `Assets\\Particles111` does not exist yet.
- The prefab dependency closure includes 58 additional source assets outside `Fires` (about 174 MB); the current project already contains `CFX_AutoDestructShuriken.cs` with the same GUID, so that duplicate will be reused instead of copied.
- The target project uses matching installed Cinemachine and URP package cache versions for the package references used by these prefabs; package files will remain managed by Unity rather than copied into `Assets`.
- Copied the complete `Fires` tree into `Assets\\Particles111`, including its folder/file `.meta` files, and copied 57 missing dependency assets under `Assets\\Particles111\\Dependencies`; one dependency GUID was reused from an existing project script.
- **Next steps:** Validate hashes, GUID uniqueness, and all local/package references, then close this entry.

### Completion checkpoint — 2026-11-01

- **Result:** Imported all 19 prefabs from the source `Assets\\Fires` tree into `Assets\\Particles111`, preserved their `.meta` files, and added 57 missing source dependencies under `Assets\\Particles111\\Dependencies`; the original source files were left unchanged.
- **Validation:** Main and dependency hashes matched the source with 0 mismatches; 0 duplicate GUID groups were found; all 76 copied non-meta assets and 20 copied prefab assets are present; `git diff --check -- TASK.md` passed.
- **Remaining limitations:** The source set itself contains two unresolved GUID references: `bfc512c3569cde9488366dbbd1be5c99` for a texture referenced by several explosion materials and `e667b58ffcfec0e46ab3c0360ca38fe9` for a disabled light reference in the base explosion prefab. No corresponding source files exist, so they were not fabricated or altered.

## Task: Persistent task journal workflow

- **Status:** COMPLETED
- **Started:** 2026-10-30
- **Completed:** 2026-10-30
- **User request:** Create a durable task journal that records the task description before work begins, ongoing discoveries, findings, fixes, validation, and final completion status so an interrupted session can be resumed from the repository.
- **Scope:** Add the project journal, define the update rules, and persist the workflow in the shared Codex memory.
- **Pre-existing repository changes:** Detected unrelated modified and untracked Unity files before this task; they are not part of this task and must not be reverted or changed.

### Progress

- Confirmed that `TASK.md`, `PROGRESS.md`, and `SESSION_STATE.md` did not exist.
- Confirmed that no matching persistent-task workflow existed in `G:\AI\Codex_Memory\WORKFLOWS`.
- Added the project-level journal rules to `AGENTS.md`.
- Added `G:\AI\Codex_Memory\WORKFLOWS\TASK_JOURNAL_RECOVERY.md`.
- Linked the workflow from `G:\AI\Codex_Memory\AGENTS.md` and recorded it in `G:\AI\Codex_Memory\HISTORY.md`.

### Findings

- The journal must be append-only so completed tasks remain available for later recovery.
- A later request to continue an unfinished task must use the existing `IN_PROGRESS` or `BLOCKED` entry as the source of truth.

### Changes made

- Created `TASK.md`.
- Updated `AGENTS.md` with the required journal lifecycle.

### Validation

- Confirmed the project journal exists at `TASK.md`.
- Confirmed the reusable workflow exists at `G:\AI\Codex_Memory\WORKFLOWS\TASK_JOURNAL_RECOVERY.md`.
- Confirmed the global index and history were updated.
- Confirmed pre-existing Unity working-tree changes were left untouched.

### Result

The persistent task journal workflow is installed. Future tasks must start with a journal entry, checkpoint progress during execution, and close the entry explicitly as `COMPLETED` or `BLOCKED`.

## Task: Analyze approved audio and identify remaining needs

- **Status:** COMPLETED
- **Started:** 2026-10-31
- **User request:** Analyze the sounds the user approved in `C:\Users\TorasPC\Desktop\Sounds`; exclude commander voice work because the user will handle it later; produce a practical list of sound types still needed for War of Machines.
- **Scope:** Inventory and inspect the approved sound package, group files by gameplay/UI/context role, identify duplicates, weak fits, and coverage gaps, then provide a prioritized missing-sound list without changing Unity code, scenes, settings, or existing audio packages.
- **Acceptance criteria:** The approved folder is fully inventoried; commander voice assets are excluded from the requested gap list; recommendations distinguish required gameplay coverage from optional polish and state where the current package is insufficient; findings and validation are recorded here before completion.
- **Next steps:** Inspect the Desktop folder structure and audio metadata, audition or analyze representative files where possible, compare coverage against the game's existing audio/event documentation, then finalize the missing-sound list and close this entry.

### Progress checkpoint — 2026-10-31

- Confirmed the user-provided original event list is the source of truth for this continuation; the six operator/commander lines are explicitly out of scope for the missing-sound list.
- Inspected `C:\Users\TorasPC\Desktop\Sounds`: 47 audio files total — 40 WAV, 3 MP3, and 4 OGG. One file is explicitly labeled as a track-movement reference that should not be added to the game, leaving 46 intended selections.
- Parsed the available audio headers and durations. The folder contains no duplicate SHA-256 files. Most selected one-shot cues are short; engines, ambience, garage audio, and music are longer loops/tracks. The second garage ambience is a 180.18-second, 192 kHz/24-bit WAV and should be downsampled/compressed at import rather than used as a raw runtime asset.
- Current coverage is strong for turret/sniper cues, walker basics, tracked idle/movement basics, reload/empty-ammo, target lock/countdown, core UI feedback, garage/map ambience, battle/result music, and victory/defeat stings.
- Exact or clearly distinct gaps remain in shell flyby, penetration/no-penetration/world impacts, tracked turn/load/braking/scrape, walker metal steps, battle-end cue, several generic UI/progression cues, and industrial electrical ambience. Existing generic damage/movement files are useful placeholders but should not be treated as exact event coverage.
- No Unity code, scenes, settings, or existing audio packages were changed.

### Completion checkpoint — 2026-10-31

- **Status:** COMPLETED
- **Result:** Analyzed the approved `Sounds` folder against the user's original event list, explicitly excluded all six operator/commander voice lines, separated covered events from partial placeholders, and produced a prioritized missing-sound report at `C:\Users\TorasPC\Desktop\Sounds_Missing_List.md`.
- **Validation:** Confirmed 47 files, all non-empty; 40 WAV, 3 MP3, and 4 OGG; 47 unique SHA-256 hashes; one explicitly labeled non-game reference file; report file exists. Audio header/duration inspection completed for the selected files.
- **Remaining limitations:** Subjective listening and in-game mix/loop testing were not performed in this terminal analysis. The file `зничення робота противника.wav` must be auditioned once to decide whether it is the full destruction sound or only the short enemy-destroyed confirmation; the report intentionally requires a separate slot for the other role.

## Task: Map the new audio folder to in-game integration points

- **Status:** COMPLETED
- **Started:** 2026-10-31
- **User request:** Inspect `C:\Users\TorasPC\Desktop\звуки нев версіон` and explain what each sound is for, where it will be connected in the game, and how it will be triggered; ask questions if the mapping is ambiguous.
- **Scope:** Inventory and technically inspect the new audio folder, map each file to the current War of Machines gameplay/UI/audio owners and event points, identify files that need clarification or conversion, and prepare an implementation plan without modifying Unity code or assets unless explicitly requested later.
- **Acceptance criteria:** Every file in the folder has a proposed role, owner, trigger, and playback type; ambiguous assignments are called out as questions; existing audio-system constraints and any required code/scene wiring are recorded.
- **Next steps:** Inspect the folder and audio metadata, compare candidate event names with the repository's audio and gameplay owners, then produce the per-file integration map and questions.

### Progress checkpoint — 2026-10-31

- Inspected `C:\Users\TorasPC\Desktop\звуки нев версіон`: 53 non-empty files, 42 WAV, 7 OGG, and 4 MP3; all 53 SHA-256 hashes are unique.
- The package is organized into `UI`, `гараж`, `звук робота`, `звук робота на гусеницях`, `звук робота на ногах`, `карта`, and `музика бою`. It contains three no-penetration variants, three penetration variants, two cannon-shot variants, distinct own/enemy destruction cues, reload-ready cues for both robot types, and explicit speed-fade instructions for two movement loops.
- Confirmed the current runtime audio registry in `Assets/Game/Content/Audio/Prefabs/AudioManager.prefab` contains only the existing generic UI click. `SoundCaller` supports UI/SFX/music playback, random clips per `AudionElement`, positional SFX, and music stop/start, but has no dedicated ambient channel.
- Confirmed the main integration owners: `NetworkWeaponShooter` for muzzle/impact resolution and network deduplication; `VehicleHealth` for damage/death observer events; `WeaponReloadController` for reload/empty states; `VehicleAutoAimController` for target lock; `VehicleTurretRotationController` for turret motion; `WalkerAnimationController`/`VehicleMovementController`/`TrackedVehicleAnimator` for locomotion; `RoomController`/`LobbyManager` for matchmaking; `DevelopmentTree` for purchase/research; `GameplaySpawner`/`EndGameUI`/`GameResultUI` for battle result flow; and `MenuManager`/`ButtonComponent` for UI presentation.
- Important constraints: penetration status is currently sent to the firing owner but not to observer clients, so remote no-penetration/penetration audio needs the resolved status added to the observer presentation path; movement and reload controllers expose no audio events yet; the game has no explicit pre-battle countdown event; and the current `Music` channel cannot mix map/garage ambience with music without a dedicated ambient playback owner.
- No Unity code, scenes, prefabs, or audio assets were changed. Open questions concern clip role selection, the pre-battle countdown behavior, ambient/music mixing, and how to trigger insufficient-XP feedback when the research button is disabled.

## Task: Analyze current game audio needs

- **Status:** IN_PROGRESS
- **Started:** 2026-10-30
- **User request:** Analyze the current game and comparable games such as World of Tanks and World of Tanks Blitz, then produce a practical recommended sound list for the project without overloading the game with unnecessary one-off sounds.
- **Scope:** Inspect the repository's current gameplay, UI, robot types, combat flow, scenes, and existing audio assets/references; research comparable tank/robot combat sound patterns; organize recommendations by priority, gameplay/UI/context, and reusable sound families.
- **Acceptance criteria:**
  - The final response reflects the actual systems and content found in the repository.
  - Comparable-game observations are used to justify sound categories rather than copied as an exhaustive checklist.
  - The recommendations distinguish must-have sounds from later polish and avoid redundant per-action sounds.
  - Findings, validation, and remaining uncertainty are recorded in this entry.

### Progress

- Read the global Codex memory instructions, project instructions, existing task journal, task-journal workflow, and memory history.
- Confirmed that no dedicated audio-analysis workflow exists; continue with repository inspection and focused external research.
- Inspected the working tree and confirmed unrelated pre-existing modifications; no files outside this task will be reverted or overwritten.
- Found 33 Unity scenes, 665 prefabs, 1,450 C# files, and 104 audio assets. The audio assets are overwhelmingly UI click packs; only one project-owned gameplay-adjacent click asset and no obvious weapon, impact, engine, movement, destruction, or voice assets were found in the initial inventory.
- Found no nested `AGENTS.md` files under the repository root.
- Confirmed the active audio architecture is a custom Unity `SoundCaller`/`AudioManager`/`AudioResources` stack with separate Music, Sfx, and Ui channels. Only the generic UI button click is wired into the active `AudioManager` resource list; there are no explicit gameplay `SoundCaller` calls or music entries.
- Confirmed the current active robot roster has eight instantiated prefabs: four IA tracked robots and four NV walker robots. Higher-tier registry entries exist but have no prefab assigned yet.
- Confirmed combat currently uses one ballistic projectile family with tracer, robot/world impact VFX, muzzle flash/smoke, recoil, reload/ammo, armor-based penetration or no-penetration result, HP damage, detached death debris, and a destruction/end-game flow. There are no shell classes, module damage states, repair states, engine-fire states, capture points, or implemented breakable-prop gameplay events.
- Confirmed the match ends when one team has no living robots; the result UI reports victory/defeat/draw, kills, damage, XP, bolts, free XP, and MMR. The garage/research UI supports selecting, buying, researching, and converting free XP.
- Confirmed the map content is an industrial environment with asphalt/concrete/metal-style props and visual impact-surface registration, but no current gameplay sound hooks or material-specific audio classification.
- External comparison research: Wargaming describes WoT/WoT Blitz as separating engine, track, shot, and explosion subsystems, using sound for vehicle condition, spotting, hit accuracy, and crew feedback; official WoT notes also call out shells in flight, gear/track/surface interaction, and distinct penetration, ricochet, and non-penetration feedback. These patterns inform prioritization, not a one-to-one feature list.

### Next steps

- Inspect repository structure, scenes, scripts, prefabs, and current audio assets.
- Research comparable-game audio structure and extract only relevant design patterns.
- Write and validate the prioritized sound inventory, then close this entry.

### Completion checkpoint - 2026-10-30

- **Status:** COMPLETED
- **Result:** Produced a prioritized audio inventory based on the verified project mechanics and comparable World of Tanks / World of Tanks Blitz audio patterns. The recommendation groups reusable sound families for combat, walkers, tracked robots, UI/progression, tactical feedback, radio voice, ambience, and music instead of assigning unique assets to every robot or button.
- **Core recommendation:** Implement combat and locomotion first: local/remote shot, shell flyby, robot penetration/no-penetration impact, world impact, received-damage feedback, reload/ready/empty, robot destruction, walker engine/steps/actuators, tracked engine/tracks, and a small set of penetration/miss/spotting cues.
- **UI/progression recommendation:** Keep one hover, one normal click, one confirm/success, one error/denied, one popup/open-close family, plus purchase, research/unlock, matchmaking, result reveal, and reward reveal cues. Do not create a unique sound for every resource or slider tick.
- **Voice recommendation:** Use a restrained radio/commander set for enemy spotted, penetration, no penetration, under fire, low health, reload ready, enemy destroyed, own destruction, battle start, and victory/defeat. Defer module-specific crew lines because the project has no module damage/repair states.
- **Explicit exclusions:** No separate engine-fire, track-destroyed, turret-damaged, repair, shell-type, capture-point, or destructible-prop sound families until those mechanics exist.
- **Validation:** Static inventory confirmed one active `AudioManager` resource entry, one explicit gameplay-adjacent sound call (the generic button click), four walker prefabs, four tracked prefabs, and a present journal entry. No implementation code or gameplay assets were changed.
- **Remaining limitations:** Sound event timing, loudness, spatial attenuation, and final mix still require in-game tuning after assets are selected; no runtime audio playtest was performed because this task requested analysis and recommendations only.

### Remaining limitations

- A power or hardware failure can still lose the final checkpoint if it happens before that write reaches disk; frequent checkpoints and a UPS reduce this window but cannot eliminate it completely.

## Task: Resume reticle, language, and spectator fixes

- **Status:** IN_PROGRESS
- **Started:** 2026-10-30
- **User request:** Continue the last three tasks: add a main-menu slider that controls the transparency of reticle objects `Back` and `Back2`; replace Ukrainian or Russian text found in the project with English, including the autopilot HUD; remove the aiming/convergence circle from spectator mode.
- **Scope:** Inspect the existing UI and gameplay ownership, preserve the user's existing CanvasGroup setup, implement the slider binding and persistence according to existing project patterns, replace only confirmed Ukrainian/Russian runtime or visible text, and remove only the spectator convergence circle.
- **Acceptance criteria:**
  - The main-menu slider changes the alpha of both reticle objects through their existing `CanvasGroup` components.
  - The selected reticle transparency is applied when the menu or reticle is initialized and follows existing settings persistence conventions if available.
  - Confirmed Ukrainian and Russian user-facing text is translated to English without changing identifiers, comments, or unrelated text.
  - Spectator mode no longer displays the convergence circle while other spectator HUD behavior remains intact.
  - Relevant tests or focused validation are run and recorded.
- **Pre-existing repository changes:** The Unity working tree already contained unrelated modified and untracked files before this task. Do not revert or overwrite them.

### Progress

- Read the persistent-task recovery workflow and gameplay ownership workflow.
- Journaled this recovery task before searching the implementation files.
- Inspected the working tree before resuming; preserved all pre-existing Unity changes.
- Located the reticle CanvasGroups in `Assets/Game/Scenes/Client.unity`: `Back`/`Back2` under `CrosshairType1` and `CrosshairType2`.
- Located reticle presentation and spectator behavior in `Assets/Game/Scripts/UI/HUD/GunCrosshair.cs`.
- Located the settings flow in `ClientGameplaySettings`, `SettingsModel`, `SettingsController`, `GeneralTabView`, and `Client.unity`.
- Found the confirmed runtime Ukrainian autopilot labels in `Assets/Game/Scripts/UI/HUD/PauseMenu.cs`.
- Found additional Ukrainian editor-facing labels/tooltips in project-owned settings and diagnostics scripts; no Cyrillic was found in project-owned scenes.
- Added the persisted reticle-opacity setting and change event to `ClientGameplaySettings`.
- Added the General-tab slider binding and runtime fallback UI to `GeneralTabView`.
- Added the settings-controller handler for reticle opacity.
- Updated `GunCrosshair` to discover `Back`/`Back2` CanvasGroups, apply the saved alpha, and hide only the aiming circle in spectator mode.
- Translated all remaining Cyrillic text found in project-owned `Assets/Game/Scripts`, including client/server configuration tooltips and editor diagnostics labels.
- Replaced the autopilot HUD labels with `ENABLE AUTOPILOT` and `DISABLE AUTOPILOT`.

### Findings

- The main menu settings screen already has a General tab with two toggles; a reticle-opacity slider can be added there without changing the settings-tab ownership model.
- The reticle `CanvasGroup` objects are present on the two faction reticle roots for both reticle types, so the presentation owner can apply one saved alpha to all active reticle variants.
- `GunCrosshair.ApplySpectatorWeaponHud` currently calculates and applies the aiming-circle diameter for spectator mode; this is the focused owner for removing that circle while preserving spectator ammo/reload HUD.
- The confirmed user-facing Ukrainian text is `УВІМКНУТИ/ВИМКНУТИ АВТОУПРАВЛІННЯ` in `PauseMenu.cs`; it should become `ENABLE/DISABLE AUTOPILOT`.

### Changes made

- `Assets/Game/Scripts/UI/Settings/ClientGameplaySettings.cs`: stores and publishes reticle opacity.
- `Assets/Game/Scripts/UI/Settings/GeneralTabView.cs`: creates/binds the General-tab slider.
- `Assets/Game/Scripts/UI/Settings/SettingsController.cs`: routes slider changes to the settings owner.
- `Assets/Game/Scripts/UI/HUD/GunCrosshair.cs`: applies reticle alpha and suppresses spectator aiming-circle presentation.
- Project-owned client/server/test/diagnostics strings: translated to English.

### Validation

- Static scan reports no Cyrillic in `Assets/Game/Scripts` or `Assets/Game/Scenes`.
- Focused edit-mode tests were added for spectator aiming-circle visibility and reticle-opacity clamping.
- Pending Unity compilation and test execution.

### Next steps

- Implement the persisted reticle-opacity setting and bind a General-tab slider.
- Remove spectator aiming-circle updates without removing spectator weapon HUD.
- Translate confirmed runtime/editor-facing Cyrillic text to English.
- Run focused tests or static validation and close this entry only when all three acceptance criteria are met.

### Recovery checkpoint — 2026-10-30

- **Status:** IN_PROGRESS
- **Reason:** The user reported another power interruption and requested completion of this task.
- **Recovery action:** Re-read the global memory instructions, this journal, `TASK_JOURNAL_RECOVERY.md`, and `GAMEPLAY_LOGIC_SEPARATION.md`; resume by inspecting the current working tree and validating the already-recorded implementation.
- **Repository state:** The targeted settings/HUD files are modified and present on disk. The working tree also contains unrelated pre-existing changes in combat, rendering, font, project settings, and diagnostics files; they remain out of scope and untouched.
- **Validation discovery:** Unity batch-mode compiled the scripts successfully, but both Test Runner attempts exited with code `0` without producing XML or starting a test run. A repository-wide scan also found historical Cyrillic in `docs/codex-game` and editor-only inspector documentation outside the runtime/UI scope; these were left untouched. The PlayMode scene assertion was stale and still expected the old Ukrainian label.
- **Change after recovery:** Updated `Assets/Game/Tests/PlayMode/CameraRuntimeZoomOverridePlayModeTests.cs` to assert the scene's existing `Autonomous Control` label.
- **Next steps:** Check current repository state, inspect the pending test/compile surface only as needed, run focused static or Unity validation, then close this task entry with the final result and limitations.

### Completion checkpoint — 2026-10-30

- **Status:** COMPLETED
- **Result:** Added persisted reticle opacity control, bound the General settings slider, applied the value to all four `Back`/`Back2` reticle CanvasGroups, hid the aiming/convergence circles during death spectator mode while preserving spectator weapon HUD, translated runtime/project-owned script text and autopilot labels to English, and synchronized the stale autonomous-control scene test assertion.
- **Validation:** Unity `6000.3.13f1` batch compilation exited `0` with no compiler errors or exceptions. Static checks passed for no Cyrillic in `Assets/Game/Scripts`, `Assets/Game/Scenes`, and `Assets/Game/Tests`; all four reticle CanvasGroups were found under the two reticle roots; required opacity, spectator-hide, and English autopilot paths were present; targeted `git diff --check` passed.
- **Test limitation:** Two Unity batch Test Runner attempts exited `0` without starting a test run or producing XML results, so NUnit pass/fail results could not be confirmed. Logs are in `Temp/Codex/reticle_spectator_editmode.log`, `Temp/Codex/editmode_all.log`, and `Temp/Codex/compile_final.log`.
- **Remaining limitation:** Historical `docs/codex-game` entries and editor-only inspector documentation outside the runtime/UI scope still contain Cyrillic and were intentionally left unchanged.

## Task: Find, license, and organize War of Machines audio assets

- **Status:** IN_PROGRESS
- **Started:** 2026-10-30
- **User request:** Find, download, rename, and organize free, game-usable sound effects and music for War of Machines, following the full request in `C:\Users\TorasPC\.codex\attachments\4e5262f5-3b3c-45d7-b300-66442cf03de6\Pasted text.txt`.
- **Scope:** Inventory existing project audio; research legal sources with explicit commercial-use licenses; download a restrained, suitable set for the listed voice, combat, walker, tracked, turret, HUD, UI, ambience, and music events; preserve originals in `Source`; organize final assets and license/source metadata in the requested Desktop package; mirror the final category folders to `Assets/Game/Content/Audio/Downloaded/`; report unavailable sounds in `MISSING_AUDIO.md`.
- **Acceptance criteria:** No game code, AudioManager, UI, or settings are changed; every downloaded file has source, author, and license recorded in `Documentation/Audio_Sources.csv`; no unclear/non-commercial licenses; files are readable and non-empty; duplicates and both destination counts are checked; `Documentation/Audio_Import_Report.md` and this journal entry are completed.
- **Audio workflow:** No dedicated audio acquisition workflow exists in the shared archive; follow the user's detailed specification and the project journal rules.
- **Progress:** Read global instructions, applicable UI workflow (not applied to audio), task journal workflow, project `AGENTS.md`, and existing `TASK.md`. The project journal contains unrelated prior IN_PROGRESS work; preserve it and append this separate task entry.
- **Next steps:** Inspect repository state and existing audio; then research and acquire eligible assets, package them, validate, and close this entry.

### Progress checkpoint - 2026-10-30

- Confirmed the project root is `D:\Programing\UnityProjects\war-of-machines-unity`; its root `AGENTS.md` and current `TASK.md` were read before repository inspection. No nested agent instructions exist.
- `Assets/Game/Content/Audio` contains only `AudioManager`/`AudioPreset` prefabs and one existing `SFX/UI_Click_Thick_Generic.wav` (73,692 bytes); no `Downloaded` tree or requested Desktop package exists.
- Recorded the pre-existing working tree: many unrelated modified and untracked Unity files, including gameplay, scenes, graphics, and project settings. Keep all untouched except this task's new audio package, mirrored audio assets, and journal update.
- Began source research. Candidate sources with explicit CC0 terms include Kenney Interface Sounds and Sci-fi Sounds, and multiple OpenGameArt effects/music pages. Need validate each candidate and retain actual source archives/files and attribution data before selection.
- No code, AudioManager, UI, or game settings have been changed.

### Research checkpoint - 2026-10-30

- Verified official OpenGameArt pages for candidate sources and their stated licenses: Kenney Interface Sounds (CC0); 50 CC0 Sci-Fi SFX (CC0); 25 CC0 Bang/Firework SFX (CC0); Engine-loop heavy vehicle/tank (CC-BY 3.0); Robotic mechanic step sounds (CC-BY 3.0); 12 Ambient Machine Sounds (CC-BY 3.0); SciFi RTS Status Vocals (CC-BY 3.0); Dark Sci-Fi Audio Pack (CC0); and individual mechanical/metal impact/reload sounds (CC0).
- A CC0 heavy vehicle/tank loop pack explicitly contains one base loop plus three speed variants; a separate robot movement archive has mech steps/engine sounds. These appear more relevant than generic tracks or mismatched ship impacts.
- Exact realistic shell flyby, tank-track movement/turning, turret movement completion, and specific penetration/no-penetration operator lines remain unverified. Do not substitute unrelated samples for them.
- OpenGameArt direct file downloads are reachable. Kenney's own signed-download route was inconsistent; use the equivalent CC0 OpenGameArt upload for Kenney Interface Sounds if its archive validates.
- Next: download source packages and direct candidate files, inspect archive contents/metadata, then select only suitable items for `Final`.

### Completion checkpoint — 2026-10-30

- **Status:** COMPLETED
- **Result:** Retrieved and organized 54 final audio assets across Voice (4), Combat (12), Walker (8), Tracked (4), HUD (2), UI (16), Ambience (4), and Music (4). Turret has no files because no suitable licensed source was verified. Preserved 12 source archives and 9 standalone source files; byte-identical archive duplicates were removed from the new package.
- **Licensing:** Selected CC0 1.0 and CC-BY 3.0 only; all selected rows explicitly allow commercial use. Source URLs, authors, licenses, and notes are recorded in the Desktop package CSV.
- **Validation:** WAV streams were parsed and read to their end; OGG/Vorbis page structure, sequence, CRC, and end markers were checked. All 54 files are non-empty; no duplicate audio content was found; Desktop and Unity contain the same 54 files with matching SHA-256 hashes. The source CSV has 54 matching rows.
- **Limitations:** Exact gaps are documented in Desktop `MISSING_AUDIO.md`; generic cues are labeled as generic in the CSV. No audio conversion or editing was performed. No C# scripts, AudioManager, UI, or settings were changed.
- **Deliverables:** `C:\Users\TorasPC\Desktop\WarOfMachines_Audio\Documentation\Audio_Sources.csv`, `Audio_Import_Report.md`, and `MISSING_AUDIO.md`; mirrored assets are under `Assets/Game/Content/Audio/Downloaded/`.

## Task: Expand War of Machines audio options to 3–5 variants per requested event

- **Status:** IN_PROGRESS
- **Started:** 2026-10-30
- **User request:** Repeat the audio sourcing task because the previous selection was unsatisfactory; find 3–5 options for every sound event so the user can choose among them, taking the time needed for a high-quality result.
- **Scope:** Re-research every event in the original audio request; source distinct, relevant alternatives with explicit commercial-use rights; preserve source packages and attribution; stage a new expanded Desktop package and Unity mirror; replace the previous 54-file selection only after the expanded package is ready and validated. Do not modify C# code, AudioManager, UI, or settings.
- **Acceptance criteria:** Each of the 60 requested sound events has 3–5 distinct variants (at least 180 total); each final file has a documented original filename, source URL, author, license, commercial-use status, and event mapping; no CC-BY-NC or unclear license; no unmarked generic substitution; audio files are valid/non-empty and duplicate content is removed; Desktop and Unity contain matching paths and hashes; updated source CSV, variant matrix, missing-audio report, import report, and this journal entry are complete.
- **Current state:** The previous delivery has 54 files in Desktop `Final` and 54 mirrored files under `Assets/Game/Content/Audio/Downloaded`. The user has rejected the quality and completeness of that selection; treat it as a prior iteration, preserve it while researching/staging, and do not present it as satisfying the expanded requirement.
- **Workflow:** No audio-specific acquisition workflow exists in `G:\AI\Codex_Memory\WORKFLOWS`; follow the user's original source/license rules and this expanded request.
- **Next steps:** Build an event-by-event research matrix, search additional credible CC0/CC-BY commercial sources, preview/inspect candidates where possible, then stage and validate at least three distinct options per event.

### Recovery checkpoint — expanded audio selection resumed — 2026-10-30

- User explicitly asked to continue and not stop until the expanded selection is complete; created an active goal for 3–5 variants per each of the 60 events.
- Re-read global instructions, the task-journal workflow, project `AGENTS.md`, and this journal before resuming. The earlier `Final` and `Assets/Game/Content/Audio/Downloaded` still contain the rejected 54-file iteration; preserve them until a replacement passes validation.
- Sonniss GDC 2026 license and tracklist were verified. User explicitly authorized accepting Google Drive's large-file scan warning. The public Drive copy then returned a quota-exceeded page; the primary host served Cloudflare challenge content. Browser control stopped because it could not reliably determine the current URL; no browser automation will be retried.
- Continue through independent direct sources: Mixkit SFX (official game-usable license), OpenGameArt CC0/CC-BY assets, and Pixabay sound effects (official commercial-use license). Do not reuse rejected selections as the new variants. Build the event matrix and source-linked audition catalog alongside the acquired files.
- No new audio files have been added to either final destination yet; no game code, AudioManager, UI, or settings were changed.
- Next: verify individual source files and downloadable URLs; acquire and map 3–5 genuinely distinct options per event; validate metadata/audio and replace only after the full package is ready.

### Research checkpoint — additional licensed pools — 2026-10-30

- Resumed the expanded audio selection from the recorded recovery checkpoint. Re-read the global Codex memory index/history and `TASK_JOURNAL_RECOVERY.md`, the original user request, and the active task/journal before continuing. Confirmed the prior 54-file package remains untouched and the project `Downloaded` tree remains untracked/pre-existing task output; unrelated repository changes are untouched.
- Verified the staged Mixkit SFX pool contains 78 WAV files. Two remaining direct URLs (`1660`, `183`) returned HTTP 403; one OGA probe (`sci-fi_1_2.wav`) returned 404. No access controls were bypassed; both will be replaced by other candidates.
- Added licensed OpenGameArt source materials to `work/audio_variant_redo/SourcePackages/OGA_Extra`: Chain Winch CC0 archive (extracted), Steam Boiler loop CC0, six Racing Car Engine loops CC0, two Engine Sounds CC0 MP3s, four Robotic Transformations clips with CC0 option, plus the existing OGA Robot/Voice/SFX sources. These are research/staging files only; no final destination has been changed.
- Discovered the official Meta Open Sound corpus and followed its `AGENTS.md` selection instructions. Confirmed Oculus Audio Pack 1 is CC BY 4.0, contains 289 sounds/508 files at 48 kHz, and supplies a required attribution line. Downloaded the official 360,146,596-byte WAV source archive and the category indexes/manifest/attribution files into the task work area; the actual sample matrix is being narrowed by event descriptions and source metadata before extraction.
- Confirmed additional relevant candidates include separate machine-room/industrial ambience beds, hydraulic/servo sounds, metal impacts, six surface-specific footsteps, multiple sci-fi UI theme sets, and round-robin metal/door/vocal takes. Human footsteps will not be mislabeled as robot steps; direct operator lines will be kept distinct from pain/death vocal reactions.
- Next: inspect the source ZIP and event-specific indexes; inventory prior final SHA-256 hashes; map 3 distinct licensed candidates to every original event, with honest notes for exact spoken lines or mechanics not literally present; then stage and validate the catalog before synchronizing Desktop/Unity.

### Recovery checkpoint — 3-option catalog build — 2026-10-30

- **Status:** IN_PROGRESS
- Re-read the global archive instructions/history, task-journal recovery workflow, project `AGENTS.md`, and this journal. Confirmed no audio-specific workflow exists; the original 60-event brief and explicit continuation request remain the source of truth.
- Inspected `build_catalog.py`: all 60 events and 180 candidate slots are present, but path resolution needs recursive/archive-aware lookup and candidate refs contain cross-event duplicates. The script has not produced or changed either final destination.
- Verified pre-existing Unity changes with `git status --short`; the old `Assets/Game/Content/Audio/Downloaded/` is still untracked and preserved. All other worktree changes remain untouched.
- Corrected course: source directory paths are nested differently than the initial map assumed (Kenney and OGA files have internal subfolders); several references are valid once resolved recursively. Some names need correction. Next build a resilient resolver, eliminate source/content reuse and v1 hashes, generate staging artifacts, validate 60×3 candidates, and only then replace the old output.

### Expanded audio catalog checkpoint — 2026-10-30

- Rebuilt the candidate set with 191 unique audio samples for all 60 events; every event now has 3–5 options. Added 13 Pixabay records, including direct tank cannon, track/engine, turret, reload, mech-footstep, radio, and mission-complete candidates. Creator, source item URL, and license are recorded per sample.
- Staging validation passed for every audio container, event count, source row, unique SHA-256, rejected-v1 hash exclusion, and 191 local audition players; all HTML audio targets resolve.
- Archived the rejected 54-file Desktop package and 54-file Unity mirror. Copied the new set to both destinations and confirmed exact relative-path/SHA-256 parity (191 each). No game code, AudioManager, UI, or settings were changed.
- Remaining fit gaps are documented in `MISSING_AUDIO.md`; exact penetration/no-penetration lines, a heavy-shell flyby, tracked pivot/braking, and literal “Battle finished” wording were not found. Pixabay/Mixkit standalone-file restrictions are recorded in credits.
- The user authorized looking for Sonniss, but its current bundle terms require explicit action-time acceptance. That decision is pending; no Sonniss files are included yet.

### Recovery checkpoint — ZapSplat review resumed — 2026-10-30

- The user confirmed an already-authenticated browser session is available for ZapSplat. Continue the existing expanded 3–5-variants-per-event task and use only that existing session; do not create an account or change account settings.
- Before adding anything, verify the exact file page, preview, current license/attribution conditions, download access, and fit for tracked movement, turret rotation, and engine loops. Preserve a copy of each source original and add complete source rows.
- Current deliverable remains 191 unique audio files across all 60 events, mirrored with matching path/hash; this is the validated baseline. Any ZapSplat additions must be distinct, genuinely better-fit alternatives, must keep every event within 3–5 options, and require rebuilding audition/docs and revalidating both mirrors.
- Next: inspect the existing ZapSplat tank page and account/license state; acquire only verified candidates; update documentation and Desktop/Unity parity if suitable; then close the task journal with remaining fit gaps honestly listed.

### Checkpoint — Sonniss and ZapSplat permissions confirmed — 2026-10-30

- The user explicitly accepted the current Sonniss GDC bundle terms and ZapSplat Standard License for this project.
- Current browser view still shows the ZapSplat login form after the user reported signing in. No credentials were entered; the tab remains handed off for the user and will be rechecked before any ZapSplat download.
- Verified the official GDC 2026 Sonniss page lists five official download parts totaling 7.47 GB and links a public filename/library/supplier tracklist. Rather than fetch the entire bundle blindly, inspect the full list first and download only part(s) that contain plausible candidates. The official v2.0 terms effective 2026-08-27 prohibit raw sound redistribution and AI training; commercial game synchronization is permitted.
- Next: export/read the public tracklist, identify relevant audio and archive part, acquire only eligible files, then update the 191-file baseline and revalidate.

### Recovery checkpoint — first Sonniss archive part verified — 2026-10-30

- Downloaded official GDC 2026 part 1 from the Sonniss page; moved the 1.30 GB archive into task-local staging. ZIP directory is readable and contains 116 entries plus the bundle license/README. Identified plausible high-fit files: metal pass-by whooshes, metal hits, and metal dragging/scraping; no tank, engine, turret, or tracked-vehicle samples are in part 1.
- Tried official part 2 primary download; Chrome returned ERR_BLOCKED_BY_CLIENT. The official Google Drive mirror displayed the large-file virus-scan warning already approved by the user; after that warning, Chrome again returned ERR_BLOCKED_BY_CLIENT. Do not bypass these browser blocks or retry through alternate mechanisms.
- ZapSplat still presents a login form in the available tab despite the user reporting sign-in; no credentials were entered and no audio was downloaded from it.
- Next: map part-1 candidates to underfilled events, extract only chosen WAV originals, then rebuild/revalidate the package; continue using other non-blocked licensed sources and report the remaining exact tank-fit gaps.

### Recovery checkpoint — continue v6 catalog assembly — 2026-10-30

- Re-read the global memory instructions, workflow index/history, task-journal recovery workflow, project `AGENTS.md`, and current journal. No audio-specific acquisition workflow is present.
- Rechecked the existing browser profile: ZapSplat still displays its Login form despite the user's report that they signed in. No account information was entered; no account-gated files were downloaded. Continue with verified local source pools.
- Rechecked the official Sonniss Bundle License v2.0: commercial synchronization in games is allowed without attribution; sharing is allowed within the licensee's project team, while supplying the audio as a standalone library/asset pack is prohibited and AI training is prohibited. User already explicitly accepted these terms for this project.
- The current expanded baseline is 191 files / 60 events. The v6 builder now has three verified Sonniss part-1 candidates mapped to flyby, world impact, and metal scrape approximations. Part 2 remains blocked by Chrome; no security bypass will be attempted.
- Next: verify the v6 builder and selected archive members, update generated license/gap wording, build only into task-local staging, validate all 60 event groups and file metadata, then promote the staged package and confirm Desktop/Unity parity.

- Built `Expanded_Package_Staging_v6` successfully: 60 events, 194 files, formats WAV/OGG/MP3/FLAC. Staging QA confirmed 3–5 variants for every event, 194 unique hashes and source-manifest rows, 194 valid local audition targets, zero hashes from rejected v1, and three correctly attributed Sonniss part-1 WAVs only for flyby/world-impact/scrape.
- Confirmed current v5 state before promotion: Desktop `Final`, `Documentation`, `Source`, and `MISSING_AUDIO.md` are present; Unity `Downloaded` has 191 audio files and no `.meta` files. Archive destinations `Archive/Expanded_v5_2026-10-30` and `Downloaded_Expanded_v5_2026-10-30` do not exist. Next, move current v5 outputs to those versioned archive locations, then copy v6 outputs and verify path/hash parity.

### Completion checkpoint — expanded audio variants v6 — 2026-10-30

- **Status:** COMPLETED
- **Result:** Rebuilt and promoted the expanded audio selection with 194 distinct files for all 60 requested events, providing 3–5 options for every event. Added three exact Sonniss GDC 2026 part-1 WAVs as honestly labeled mechanical flyby, metal world-impact, and metal-scrape approximations. Each candidate has source URL, original filename, creator, license, commercial-use status, event, variant, hash, duration, sample rate, channel count, and fit notes in the CSV/matrix.
- **Desktop deliverables:** `C:\Users\TorasPC\Desktop\WarOfMachines_Audio\Final`, `Documentation`, `Source`, and `MISSING_AUDIO.md` now contain v6. Previous v5 output is preserved in `Archive\Expanded_v5_2026-10-30`; rejected v1 remains in its existing archive.
- **Unity mirror:** `Assets/Game/Content/Audio/Downloaded` now contains 194 v6 files. Previous v5 remains at `Downloaded_Expanded_v5_2026-10-30`; rejected v1 remains at `Downloaded_Rejected_v1_2026-10-30`.
- **Validation:** Builder completed successfully. Confirmed 60 events, 3–5 variants each, 194 non-empty distinct audio files, 194 matching source rows/matrix rows/manifest rows, 194 valid local audition links, zero overlap with rejected-v1 hashes, 3 Sonniss rows with matching authors/licenses, and exact Desktop/Unity relative-path SHA-256 parity. Both archived prior versions also retain matching Desktop/Unity hashes (191 v5, 54 rejected v1). Staging totals 451,539,001 audio bytes; no Sonniss bundle ZIP was copied into the final package.
- **License notes:** Sonniss v2.0 terms are recorded in `Audio_Credits.md` and require the sounds to remain within the project team and only as part of the game project; standalone sound-library/asset-pack supply and AI training are prohibited. Mixkit and Pixabay restrictions are also recorded. User explicitly accepted Sonniss and ZapSplat terms earlier.
- **Remaining fit limitations:** No literal penetration/no-penetration operator lines, heavy artillery shell pass-by, tracked pivot/hard-braking sample, dedicated walker engine loop, long turret rotation loop/end-stop, material-matched shell-impact set, or exact “Battle finished” voice line was found. The candidate notes and `MISSING_AUDIO.md` clearly label approximations. Further Sonniss parts were blocked by Chrome and were not bypassed. ZapSplat still showed the login form, so no gated files were used.
- **Scope check:** No C# code, AudioManager, UI, settings, or unrelated Unity changes were made.
- **Completion date:** 2026-10-30.

### Post-completion delivery index — 2026-10-30

- Created `C:\Users\TorasPC\Documents\Codex\2026-10-30\new-chat-5\outputs\WarOfMachines_Audio_Delivery.md` as a compact user-facing index to the verified Desktop and Unity deliverables, archive locations, validation counts, and remaining exact-fit limitations.

## Task: Create event-folder-only audio selection copy

- **Status:** IN_PROGRESS
- **Started:** 2026-10-30
- **User request:** Put the sound files into a separate folder and sort them into event folders, such as one shot folder containing all shot variants.
- **Scope:** Create a new Desktop folder from the validated v6 catalog, containing only the audio files under one subfolder per requested event. Keep existing Desktop and Unity packages intact.
- **Acceptance criteria:** The new folder contains exactly 194 audio files in 60 event folders, every folder has 3–5 variants, paths/hashes match the v6 catalog, and no documentation or unrelated files are mixed into the audio-only folder.
- **Pre-existing state:** The v6 Desktop/Unity packages, archived v5 packages, and rejected v1 archives are already present and must remain unchanged.
- **Next steps:** Check the intended new destination and source matrix, create event folders and copy the mapped files, validate all counts and hashes, then close this entry.

### Progress checkpoint — destination and source checked — 2026-10-30

- Confirmed the new Desktop destination `C:\Users\TorasPC\Desktop\WarOfMachines_Audio_ByEvent` does not exist.
- Confirmed the v6 `Audio_Sources.csv` contains 194 file rows grouped under 60 events; every group has 3–5 variants. This is the source of truth for per-event copies and already points to the validated `Final/<Category>` files.
- Plan: create a task-local staging folder on the Desktop with only the audio files, using readable Ukrainian event names plus the event key; validate every copied SHA-256 and only then rename the staging folder to the requested final name.

- **Validation note:** The first PowerShell copy attempt failed at parse time on non-ASCII Ukrainian folder labels; PowerShell executed none of the operations and created no output. Retrying with an ASCII-safe Python Unicode-escape mapping.

### Completion checkpoint — event-folder audio copy — 2026-10-30

- **Status:** COMPLETED
- Created `C:\Users\TorasPC\Desktop\WarOfMachines_Audio_ByEvent` as a separate audio-only copy from the validated v6 `Final` folder. It has 60 numbered event folders with Ukrainian labels and English event keys; each contains 3–5 original variant files.
- **Validation:** Confirmed 194 files total, only WAV/OGG/MP3/FLAC audio, 60 folders, 3–5 files in every folder, 451,539,001 bytes, and all destination hashes match both `Audio_Sources.csv` and their original v6 files. No documentation or non-audio files are present.
- A PowerShell parse attempt with Ukrainian literals failed before execution; no files were created by that attempt. A later validation print hit the Windows console's CP1252 encoding after all assertions passed; reran with ASCII output and all checks passed.
- Existing v6 Desktop/Unity packages and both archived iterations remain unchanged; no Unity files were modified for this request.

## Task: Find five replacement audio variants for eight disliked events

- **Status:** COMPLETED
- **Started:** 2026-10-31
- **User request:** The user reviewed the audio options and disliked only these eight event families: armor hit/no penetration, armor penetration, full robot destruction, short silent enemy-destroyed confirmation, metal walker footsteps, short battle-end cue, insufficient bolts/XP, and match found/battle starting. Find five new options for each and place them in a separate Desktop folder.
- **Scope:** Source 40 distinct, commercially usable alternatives, excluding every previously delivered v6 audio hash so none of the disliked selections are repeated. Group the new files by the eight requested events. Keep all existing Desktop/Unity packages unchanged and do not modify game code, AudioManager, UI, or settings.
- **Acceptance criteria:** 5 new non-duplicate options for each of the 8 events; explicit source, author, license, fit note, and event mapping retained; audio files validate; Desktop folder is separate and grouped by event; copied hashes are verified.
- **Pre-existing state:** The previous 194-file v6 package, the 194-file `WarOfMachines_Audio_ByEvent` copy, v5 archive, rejected v1 archive, and unrelated Unity working-tree changes must remain untouched.
- **Completed:** 2026-10-31. Created and validated the separate Desktop folder and source manifest.


### Completion checkpoint — replacement audio options — 2026-10-31

- **Status:** COMPLETED
- Created C:\Users\TorasPC\Desktop\WarOfMachines_Audio_Replacement_Options with 8 Ukrainian event folders and exactly 5 options per folder (40 files total). The folder contains only non-empty audio files.
- Created the adjacent C:\Users\TorasPC\Desktop\WarOfMachines_Audio_Replacement_Options_Sources.csv with source URL, creator, license, attribution, fit note, original filename, output path, and SHA-256 for each file.
- **Validation:** 40 distinct hashes; no hash overlaps any of the previous 194 v6 sounds; all 40 destination hashes match the manifest and staged sources; 8 folders contain 5 audio files each. The sound folder has no documentation or non-audio files.
- The previous Desktop/Unity packages and Unity project files were not modified.

## Task: Re-map paired audio variants to distinct game contexts

- **Status:** COMPLETED
- **Started:** 2026-10-31
- **User request:** Do not randomize paired sound variants; assign each variant to a different concrete screen, action, or game context and provide a revised full mapping.
- **Scope:** Inspect the current `C:\Users\TorasPC\Desktop\звуки нев версіон` folder and the existing Unity audio architecture. This is an analysis-only response; do not modify Unity code, scenes, prefabs, or audio assets.
- **Next steps:** Inventory paired filenames, decide deterministic usage contexts, document any assumptions or questions, then close the entry.

### Discovery checkpoint — deterministic pair mapping — 2026-10-31

- Confirmed the folder contains 53 audio files across UI, garage, walker, tracked, robot, map, and battle-music groups.
- Confirmed the project has two concrete tab systems: four Settings tabs (`General`, `Video`, `Sounds`, `Controls`) and dynamically generated faction tabs in `DevelopmentTree`.
- Chosen deterministic mappings: `вкладки1.wav` for Settings tabs and `вкладки2.wav` for DevelopmentTree faction tabs; armor-hit variants split by armor zone and facing; locomotion/shoot/reload variants split by robot class or movement type; ambience and music variants split by scene layer or battle phase.
- No Unity code, scenes, prefabs, audio assets, or settings were changed. The response will document the proposed integration only.

### Completion checkpoint — deterministic pair mapping — 2026-10-31

- **Status:** COMPLETED
- **Result:** Prepared a complete file-by-file integration map with no runtime randomization of paired clips. Each pair/group has a concrete trigger, screen, robot type, armor context, map layer, or battle phase.
- **Validation:** Cross-checked the mapping against the current Settings, DevelopmentTree, gameplay robot, matchmaking, and result-screen owners. No implementation changes were made.
- **Remaining limitation:** The mapping is a proposal for integration; final variant preference can be swapped after in-game audition without changing the event ownership model.

## Task: Apply audio mapping corrections

- **Status:** COMPLETED
- **Started:** 2026-10-31
- **User corrections:** Temporarily disable `UI\зничення робота противника.wav`; play the wind and industrial map ambience as simultaneous independent background layers; choose exactly one battle-music file at match start and loop it until the battle ends.
- **Result:** Updated the integration specification accordingly. No Unity code, scenes, prefabs, audio assets, or settings were changed.

## Task: Audit and repair the audio channel architecture

- **Status:** IN_PROGRESS
- **Started:** 2026-10-31
- **User request:** Analyze the in-game audio system and repair weak points so UI sounds and music are separate 2D audio, while gameplay effects use 3D audio.
- **Scope:** Inspect the audio scripts, prefabs, mixer/resources, and call sites. Keep UI and music non-positional, keep world/gameplay effects positional, preserve existing volume settings, and avoid unrelated gameplay changes.
- **Acceptance criteria:** The system exposes explicit UI, music, and SFX routing; UI/music sources use 2D spatial behavior; effects use 3D spatial behavior; lifecycle, null handling, volume updates, looping, and music replacement are safe; existing callers remain compatible or are migrated cleanly.
- **Next steps:** Audit current implementation and serialized setup, identify weak points, patch the focused audio owner(s), then validate compile-level consistency and serialized references.

### Discovery and implementation checkpoint — 2026-10-31

- Audited `AudioManager`, `AudioPreset`, `AudioResources`, `SoundCaller`, `ButtonComponent`, `SettingsController`, and the `AudioManager`/`AudioPreset` prefabs.
- Found that the manager reused any idle source across categories, the parent transform for 3D effects was immediately overwritten by the manager root, UI resources were stored with generic effects, delayed playback cast seconds to integer before multiplying, delayed async playback could race with pooled reuse, and repeated music calls stopped/restarted the current track.
- Implemented explicit `PlayUi`, `PlaySfx`, and `PlayMusic` routes; category-specific source reuse; strict UI/SFX/music resource lookup; runtime spatial routing (`Ui`/`Music` = 2D, `Sfx` = 3D); parent-preserving positional effects; DSP-scheduled delay handling; idempotent same-track music playback; null-safe singleton/volume handling; and moved the existing button clip into the UI resource list.
- Changed files: `AudioManager.cs`, `AudioPreset.cs`, `AudioResources.cs`, `SoundCaller.cs`, `ButtonComponent.cs`, `SettingsController.cs`, and `AudioManager.prefab`.
- Validation so far: `git diff --check` passes; no Unity code outside the audio/settings boundary was changed. Unity compile validation remains.

### Validation checkpoint — 2026-10-31

- `dotnet build Assembly-CSharp.csproj --no-restore --verbosity minimal` completed successfully with 0 errors; only pre-existing `System.Net.Http` conflict and FishNet obsolete API warnings remain.
- `git diff --check` completed successfully.
- Unity batch startup was attempted with Unity `6000.3.13f1`; the process exited before a compile log was produced because licensing initialization did not complete. The generated project build is the available compile validation.

### Completion checkpoint — 2026-10-31

- **Status:** COMPLETED
- **Result:** UI and music now use dedicated 2D routes and source pools; gameplay effects use a dedicated 3D route and preserve attached transforms. Volume settings remain independently controlled for UI, music, and SFX.
- **Remaining limitation:** No runtime audio audition was performed, and the project’s currently serialized library still contains only the existing UI click asset; the new event assets can now be added to `uiClips`, `soundClips`, or `musicClips` without changing routing code.

## Task: Connect approved audio package to gameplay

- **Status:** IN_PROGRESS
- **Started:** 2026-10-31
- **User request:** Connect all approved audio files from `C:\Users\TorasPC\Desktop\звуки нев версіон` according to the agreed mapping, while keeping the previously disabled enemy-destruction voice unused.
- **Scope:** Import the approved files into Unity, register stable event names in the audio resources, and wire UI, garage, matchmaking, battle, robot movement, weapons, turret, ambience, music, and result-screen events through the separated audio channels.
- **Acceptance criteria:** Every approved file has a concrete event or deliberate variant mapping; UI/music/ambience remain non-positional; gameplay effects are positional; battle music chooses one track per match and loops until battle end; wind and industrial ambience start together; the disabled file is not referenced; existing audio callers remain compatible.
- **Next steps:** Inspect current event owners, copy and register the approved assets, connect focused presentation hooks, validate references and compilation, then close this entry with any runtime limitations.

### Recovery checkpoint — audio package and owner boundaries — 2026-10-31

- Confirmed the interrupted working tree already contains the separated audio routing changes, `AudioEventNames.cs`, and the approved files under `Assets/Resources/GameAudio/<event-name>/`; these task-specific changes are present and must be preserved.
- Confirmed `AudioResources` resolves each stable event name from `Resources/GameAudio`, while `SoundCaller` owns UI/music/SFX/ambience routing and menu-level lifecycle. The disabled `UI_Enemy_Destroyed` asset remains on disk but must not be referenced by code.
- Identified the remaining owner hooks: `SettingsView` and `DevelopmentTree` tabs/progression, `RoomController`/`LobbyManager` matchmaking, `GameplaySpawner`/result UI battle lifecycle, `NetworkWeaponShooter` combat outcomes, `WeaponReloadController` ammo/reload, turret/locomotion controllers, `VehicleAutoAimController`, `CameraController`, `GunCrosshair`, and popup actions.
- Ownership decision: keep audio policy in `SoundCaller`/small `AudioEventSelector`; existing gameplay/UI owners only emit their concrete presentation trigger. Add `docs/codex-game/audio-presentation.md` to record this boundary.
- Next: add the selector and loop-volume support, wire each owner trigger, then validate every approved event folder/name, compile, and run repository checks.

### Completion checkpoint — approved audio connected — 2026-10-31

- **Status:** COMPLETED
- **Result:** Registered stable event keys and connected UI, garage, matchmaking, battle lifecycle, locomotion, weapons, turret, sniper, targeting, reload, popup, and result-screen triggers through the separated audio channels. Added `AudioEventSelector` for deterministic heavy/walker, armor-zone/facing, and movement-type variants.
- **Resource correction:** Synced all `53` approved Desktop files into `Assets/Resources/GameAudio`, including the previously omitted enemy-destruction WAV. Both files in `UI_Enemy_Destroyed` remain deliberately unreferenced.
- **Validation:** Per-event source/Unity SHA-256 comparison passed for `53/53` files; stable event-folder audit passed; disabled-event code search returned no references; temporary inclusion of the two new source files in the generated Rider project produced `0` errors and only existing Unity/`System.Net.Http` warnings; `git diff --check` passed.
- **Lifecycle fix:** Prevented initial camera setup from playing `SniperExit` and reset stale battle-audio state when `SoundCaller` is recreated or destroyed.
- **Remaining limitation:** Unity Editor runtime audition was not performed because the earlier batch startup could not complete licensing initialization; generated `.meta` files for the imported audio clips will be created by Unity on import.

## Task: Debug silent audio in player build

- **Status:** IN_PROGRESS
- **Started:** 2026-10-31
- **User report:** After creating a player build, no game audio is audible even though the settings sliders show maximum values.
- **Scope:** Trace build-time audio initialization, `Resources` event loading, serialized `SoundCaller`/`AudioPreset` references, runtime volume keys, and build inclusion of the approved audio package. Apply the smallest focused fix and validate it without disturbing unrelated gameplay.
- **Acceptance criteria:** Audio manager initializes in the player build; at least UI, music, ambience, and SFX events resolve and play; volume settings remain effective; build/resource diagnostics identify any remaining missing event or asset.
- **Next steps:** Inspect initialization and resource-loading paths, reproduce or statically isolate the first failure, patch the owning audio boundary, then run compile/resource/build validation and close this entry.

### Discovery checkpoint — silent preset source — 2026-10-31

- `AudioManager.prefab` correctly references `SoundCaller`, `AudioResources`, and `AudioPreset`.
- `AudioPreset.prefab` contains an `AudioSource`, but its `AudioPreset.source` serialized reference is missing; current `AudioPreset.Init()` returns immediately when that field is null.
- This prevents every UI, music, ambience, and SFX event from reaching `AudioSource.Play`, independently of slider values or `Resources.LoadAll`.
- Fix: resolve the component from the same GameObject as a runtime fallback and serialize the existing prefab source explicitly.

### Implementation checkpoint — preset source repair — 2026-10-31

- Updated `Assets/Game/Scripts/Audio/AudioPreset.cs` to resolve its sibling `AudioSource` in `Awake`, `Init`, and `Play` when the serialized field is absent.
- Updated `Assets/Game/Content/Audio/Prefabs/AudioPreset.prefab` with an explicit `source` reference to its existing `AudioSource` component.
- Resource audit after the repair: `53` audio files, `51` event folders, no empty folders, and no missing stable event keys. Volume keys remain `UIVol`, `MusicVol`, and `SFXVol` with default `1`.

### Discovery checkpoint — settings volume source mismatch — 2026-10-31

- Settings sliders are loaded from `settings.config`, while `AudioManager` reads `UIVol`, `MusicVol`, and `SFXVol` from `PlayerPrefs`.
- If those stores diverged, the UI could display maximum values while every runtime source was still muted at `0`.
- The settings owner will synchronize the three runtime keys from the loaded model and refresh active audio sources.

### Validation checkpoint — silent build repair — 2026-10-31

- `dotnet build Assembly-CSharp.csproj --no-restore --verbosity minimal` passed with `0 errors`; only pre-existing Unity/System.Net.Http warnings remain.
- Resource audit passed with `53` imported audio files, `51` event folders, zero empty folders, and all stable keys present. Unity Editor logs show the new audio assets importing successfully.
- The Unity Editor is currently open on this project, so no background player build was started in parallel; do not close or kill the user’s Editor process. Next step is to let import finish and rebuild the player for runtime confirmation.

## Task: Move audio resources into AudioManager prefab with per-file volume

- **Status:** IN_PROGRESS
- **Started:** 2026-10-31
- **User request:** Rework the audio setup so all resources are configured on the `AudioManager` prefab and each audio file can have an independent volume.
- **Scope:** Replace runtime `Resources`-folder discovery as the source of audio configuration with serialized `AudioManager` prefab data, while preserving stable event lookup and existing UI/music/SFX routing. Add per-clip volume configuration without breaking existing callers or the current volume sliders.
- **Acceptance criteria:** The `AudioManager` prefab exposes every approved audio event and a per-file volume; `AudioResources` resolves these serialized entries; playback applies both category and per-file volume; no approved event silently disappears because of folder/resource loading; project compiles and serialized references remain valid.
- **Next steps:** Inspect `AudioResources`, `AudioManager`, `AudioPreset`, and prefab serialization; design the smallest focused serialized resource model; migrate the prefab data and validate compilation and event coverage.

### Design and implementation checkpoint — serialized clip resources — 2026-10-31

- `AudioResources` previously resolved missing entries with `Resources.LoadAll`, so the prefab was not the complete source of truth.
- Replaced each `AudioClip[]` variant list with serialized `AudioClipEntry[]`; every entry contains a direct `AudioClip` reference and an independent `[Range(0, 1)] volume`.
- Removed runtime folder loading from typed and untyped resource lookup. `AudioPreset` now applies the selected file’s volume together with the category volume.
- Updated `docs/codex-game/audio-presentation.md` to document prefab ownership. Next step: populate all approved clip references in `AudioManager.prefab` and remove the obsolete runtime-resource field.

### Completion checkpoint — prefab-owned audio resources — 2026-10-31

- **Status:** COMPLETED
- `AudioManager.prefab` now contains 51 event entries and direct references to all 53 approved audio files across `uiClips`, `soundClips`, `musicClips`, and `ambienceClips`.
- Each serialized file entry has its own `volume` field. `AudioPreset` multiplies that value by the existing category volume, so the UI sliders still control the whole channel while individual files can be tuned independently.
- Removed `Resources.LoadAll` fallback from `AudioResources`; missing events now remain visible as missing prefab configuration instead of silently loading folder contents.
- Validation: prefab GUID audit passed `53/53` references with zero duplicates, missing assets, or unknown references; Unity Editor imported `AudioManager.prefab` successfully; `dotnet build Assembly-CSharp.csproj --no-restore --verbosity minimal` passed with `0` errors; `git diff --check` passed.
- Remaining limitation: a fresh player build and in-game hearing test still need to be run after Unity finishes importing the prefab. The open Unity Editor was not closed or run in batch mode.

## Task: Diagnose Unity DefaultPrefabObjects loading error

- **Status:** IN_PROGRESS
- **Started:** 2026-10-31
- **User request:** Explain what the Unity Console error `Unknown error occurred while loading 'Assets/DefaultPrefabObjects.asset'` means and identify the likely causes and next checks.
- **Scope:** Diagnose the generic asset-loading message without changing project files, scenes, prefabs, settings, or code.
- **Acceptance criteria:** Explain the message accurately, distinguish the generic stack-trace line from the underlying cause, provide safe recovery steps, and state what additional Console/log information is needed for a definitive diagnosis.
- **Next steps:** Answer from the observed message; if the user wants a precise fix, inspect the preceding Console error and the affected asset/import state.

### Completion checkpoint — 2026-10-31

- **Status:** COMPLETED
- **Findings:** `Assets/DefaultPrefabObjects.asset` exists with a valid `.meta`, but is 2225 bytes containing only zero bytes. FishNet references this path as its generated `DefaultPrefabObjects` ScriptableObject and provides `Tools/Fish-Networking/Refresh Default Prefabs` to rebuild it.
- **Validation:** Confirmed the asset path, byte content, SHA-256, FishNet loader/generator code, and Unity version `6000.3.13f1`. No project implementation files were changed.
- **Result:** The Console line is a generic Unity asset-load failure; in this project the likely root cause is a corrupted/invalid FishNet prefab collection asset. Recommended recovery is to back up the asset, run the FishNet refresh command, and inspect the first Console error if regeneration fails.
- **Remaining limitations:** The Unity Editor was not operated, so regeneration was not performed in this task.

## Task: Repair corrupted FishNet DefaultPrefabObjects asset

- **Status:** IN_PROGRESS
- **Started:** 2026-10-31
- **User request:** Fix the Unity error caused by the corrupted `Assets/DefaultPrefabObjects.asset` file.
- **Scope:** Regenerate only the FishNet default prefab collection, preserve its `.meta` identity and unrelated working-tree changes, then validate the resulting asset and relevant references.
- **Acceptance criteria:** `Assets/DefaultPrefabObjects.asset` is a valid Unity/FishNet serialized asset rather than an all-zero file; FishNet prefab generation can load it; no unrelated files are changed; validation results are recorded.
- **Next steps:** Inspect the current editor/process state and repository changes, regenerate the asset through FishNet/Unity, then validate the asset and close this entry.

### Discovery checkpoint — 2026-10-31

- The client Unity Editor is open and shows the FishNet prefab collection UI.
- Running `Tools/Fish-Networking/Refresh Default Prefabs` did not alter the target because the invalid file still exists and FishNet's loader returns before creating a replacement.
- Decision: copy the zero-byte-content asset to a temporary backup outside the repository, remove only the invalid `.asset` while preserving its `.meta`, then run the same FishNet refresh command so the collection is recreated with the existing GUID.

### Implementation and validation checkpoint — 2026-10-31

- Backed up the corrupted file to `C:\Users\TorasPC\AppData\Local\Temp\war-of-machines-DefaultPrefabObjects.corrupt-20261031-193659.asset` and removed only `Assets/DefaultPrefabObjects.asset`; the original `.meta` was preserved.
- Ran `Tools/Fish-Networking/Refresh Default Prefabs` in the open client Unity Editor. FishNet recreated the asset at 2286 bytes with 2286 non-zero bytes and preserved GUID `17c4eb7911be1b34c95c8e7a9c35e760`.
- Opened the recreated asset in Unity; the Inspector loaded it as `Default Prefab Objects` and displayed 22 prefab entries. Cleared the stale Console message; no new `DefaultPrefabObjects` load error appeared.
- No implementation code, scenes, prefabs, or unrelated project files were changed. The regenerated binary asset is the intended task change.

### Completion checkpoint — 2026-10-31

- **Status:** COMPLETED
- **Result:** Repaired the corrupted FishNet prefab registry and verified that Unity can load it normally.
- **Validation:** Asset byte-content check, preserved GUID check, FishNet refresh, Unity Inspector load, and Console verification all passed. The existing unrelated working-tree changes remain untouched.
- **Remaining limitations:** The temporary corrupt-file backup remains outside the repository for recovery if needed.

## Task: Collect garage and profile ambience options

- **Status:** IN_PROGRESS
- **Started:** 2026-10-31
- **User request:** Collect 20 pleasant ambient sound options suited to the game's industrial/mechanical theme for the garage, development tree, and profile window, and save them in a separate Desktop folder.
- **Scope:** Source and download 20 distinct ambient audio files, organize them by intended UI context, preserve source URLs, creators, licenses, and fit notes, and keep the existing Unity project and audio configuration unchanged.
- **Acceptance criteria:** The Desktop deliverable contains exactly 20 non-empty audio files, uses only commercially usable or otherwise clearly permitted sources, provides an adjacent source manifest, and includes a balanced selection for garage, development tree, and profile ambience.
- **Next steps:** Review existing audio packages and current public source candidates, acquire the best-fit files, validate audio and hashes, write the manifest, then close this entry.

### Discovery and delivery checkpoint — 2026-10-31

- Reused already acquired, license-documented source material from the prior audio research workspace instead of adding Unity assets or modifying the game.
- Selected 20 distinct non-voice loops: 8 Garage, 6 DevelopmentTree, and 6 Profile candidates. The set combines industrial machine beds, construction/generator layers, restrained sci-fi technology loops, and quieter machine-room tones.
- Created `C:\Users\TorasPC\Desktop\WarOfMachines_Ambience_20` with `Garage`, `DevelopmentTree`, and `Profile` folders, plus `Sources.csv` and `README.md`.
- Validation passed: 20 non-empty audio files, 20 manifest rows, 20 unique SHA-256 hashes, destination hashes matching the manifest, and all five documented source pages recorded. `ffprobe` is not installed, so container validation was limited to file presence, size, and hash checks in this environment.

### Completion checkpoint — 2026-10-31

- **Status:** COMPLETED
- **Result:** Delivered 20 ambience candidates for garage, development tree, and profile UI contexts on the Desktop without changing Unity code, scenes, prefabs, or audio configuration.
- **Validation:** 8/6/6 category distribution, 20/20 manifest parity, 20/20 unique hashes, and no empty files.
- **Remaining limitations:** These are candidate sounds for later audition and selection; no runtime Unity import or in-game playback was performed.

## Task: Collect ambient music options for garage, profile, and development tree

- **Status:** IN_PROGRESS
- **Started:** 2026-10-31
- **User request:** Collect 10 pleasant ambient music tracks for each of the Garage, Profile, and DevelopmentTree windows, matching the industrial/sci-fi style of the game.
- **Scope:** Source and organize 30 distinct music tracks in a separate Desktop package, preserve source URLs, authors, licenses, and fit notes, and leave Unity code, scenes, prefabs, and current audio configuration unchanged.
- **Acceptance criteria:** The package contains exactly 10 tracks per context, all files are non-empty and clearly licensed for game use, the tracks are musical/ambient rather than one-shot effects or voices, and a source manifest records the licensing and intended context.
- **Discovery:** Existing local music is limited to battle loops, results themes, and stings; it does not provide 30 suitable window ambience tracks. Public OpenGameArt sources provide the required ambient/sci-fi music, mostly CC0 with a small number of CC-BY tracks requiring attribution.
- **Selection:** 30 distinct tracks are planned across Garage, Profile, and DevelopmentTree; no Unity project files will be modified.
- **Delivery:** Created `C:\Users\TorasPC\Desktop\WarOfMachines_AmbientMusic_30` with 10 Garage tracks, 10 Profile tracks, 10 DevelopmentTree tracks, `Sources.csv`, and `README.md`. Four CC-BY 3.0 files are marked for author attribution; the remaining 26 are CC0 1.0.
- **Validation:** 30 manifest rows, 30 audio files, 0 empty files, 30 unique SHA-256 hashes, 0 manifest/file mismatches, and 0 invalid WAV/OGG/MP3/FLAC headers. No Unity runtime audition or import was performed.

### Completion checkpoint — 2026-10-31

- **Status:** COMPLETED
- **Result:** Delivered 30 candidate ambient music tracks for the Garage, Profile, and DevelopmentTree windows on the Desktop without changing Unity code, scenes, prefabs, or current audio configuration.
- **Remaining limitations:** Tracks are source candidates selected from their public descriptions and licensing metadata; final musical approval and in-game volume/loop audition remain for the user.

## Task: Diagnose audio loss during map loading

- **Status:** COMPLETED
- **Started:** 2026-10-31
- **User request:** Investigate why all game audio disappears while the map is loading and remains silent afterward.
- **Scope:** Trace audio manager lifetime, scene transitions, audio source initialization, clip/resource resolution, and volume state without changing unrelated gameplay or audio content.
- **Acceptance criteria:** Identify the root cause or the narrowest reproducible failure point, record relevant evidence, and apply a focused fix only if the cause is confirmed; validate compile/resource behavior and note any runtime limitation.
- **Next steps:** Inspect audio ownership and scene-loading code, compare `AudioManager`/`AudioPreset` initialization with scene transitions, then reproduce or patch the focused lifecycle issue.
- **Discovery:** `MainMenu.SetActive(false)` disables `Main Camera`, which contained the only `AudioListener`; `Map` had an active gameplay camera but no listener. Unity Editor.log repeatedly reported `There are no audio listeners in the scene`.
- **Discovery:** `Client.unity` overrode `AudioManager` prefab ambience index 2 (`Map_Industry`) to volume `0`, while the resource default is `1`.
- **Change:** Added an enabled `AudioListener` to `Map`'s `GameplayCamera` and changed the `Map_Industry` scene override to volume `1`.
- **Validation:** `git diff --check` passed; `dotnet build Assembly-CSharp.csproj --no-restore --verbosity minimal` passed with `0 errors` and `2` pre-existing `System.Net.Http` warnings. Scene YAML has one listener in `Client` and one in `Map`, both enabled.
- **Result:** Map loading no longer leaves the active gameplay camera without an `AudioListener`, and the industrial map ambience is no longer muted by the scene override.
- **Remaining limitation:** A live match/load transition was not run because the Unity Editor was already open and no background instance was started; verify once in Play mode by loading a map and checking UI, SFX, wind, industry, and battle music.

## Task: Collect 20 medium- and heavy-caliber tank-like cannon shots

- **Status:** IN_PROGRESS
- **Started:** 2026-10-31
- **User request:** Find 20 cannon-shot sounds similar to tank guns, covering medium and heavy calibers.
- **Scope:** Inspect existing local shot assets and public licensed sources, collect and organize 20 distinct audio files on the Desktop, split into medium and heavy caliber groups, and preserve source URLs, authors, licenses, and fit notes. Do not modify Unity code, scenes, prefabs, or current audio configuration.
- **Acceptance criteria:** The Desktop package contains exactly 20 non-empty, distinct cannon-shot files with a 10/10 medium/heavy split, clear source/license metadata, and basic audio-format validation.
- **Discovery:** Existing local assets include one authentic Panzer 35(t) shot and one heavy tank-shot sequence; public sources provide additional tank/artillery/cannon candidates from Pixabay plus a CC0 cannon pack from OpenGameArt.
- **Selection:** Build `C:\Users\TorasPC\Desktop\WarOfMachines_CannonShots_20` with 10 medium-caliber candidates and 10 heavy-caliber candidates. Keep authentic tank recordings first, label artillery/stylized/AI-generated approximations in metadata, and exclude charge-only or overly long ambient warfare recordings.
- **Result:** Created `C:\Users\TorasPC\Desktop\WarOfMachines_CannonShots_20` with 20 cannon-shot candidates: 10 medium-caliber and 10 heavy-caliber files, grouped into `Medium` and `Heavy`. Added `README.md` and `Sources.csv` with source pages, direct download URLs, authors, licenses, use notes, file sizes, and SHA-256 hashes.
- **Validation:** Confirmed 20 manifest rows, 20 audio files, 10/10 group split, 20 unique SHA-256 hashes, all files non-empty, and valid MP3/OGG headers. No Unity code, scenes, prefabs, or audio configuration changed.
- **Limitations:** Several candidates are artillery, stylized, AI-generated, or CC0 generic cannon recordings rather than verified tank-cannon recordings; these are clearly marked in `Sources.csv` for later listening and filtering.
- **Closed:** 2026-10-31

## Task: Refine AudioResources robot targeting and Inspector sorting

- **Status:** IN_PROGRESS
- **Started:** 2026-10-31
- **User clarification:** Only some audio resources should allow selecting a specific robot; sorting should happen visually in the AudioResources Inspector without reordering serialized arrays.
- **Scope:** Make robot targeting opt-in per SFX resource, keep category grouping as Inspector-only presentation, preserve runtime event lookup and all clip GUIDs, and validate runtime/editor compilation.
- **Acceptance criteria:** Cannon shots expose an opt-in robot selector, unrelated sounds do not show a robot dropdown by default, Inspector grouping remains sorted by category, serialized array order is not mutated by the UI, and builds/GUIDs remain valid.
- **Change:** Replaced the always-visible `robot` metadata with opt-in `robotSpecific` metadata. The custom drawer shows `Robot` only when that toggle is enabled, while cannon shot resources enable it by default.
- **Change:** Removed the runtime `SortByCategory` mutator and its sorting helper. The custom Inspector continues to group UI/SFX entries by category using original array indices, so presentation sorting does not reorder serialized resources.
- **Validation:** `dotnet build Assembly-CSharp.csproj --no-restore --verbosity minimal` passed with `0` errors; `dotnet build Assembly-CSharp-Editor.csproj --no-restore --verbosity minimal` passed with `0` errors; serialized audio clip audit found `62` unique GUIDs with `0` missing references; exactly two robot-specific resources were found and both are cannon shot resources; `git diff --check` returned no whitespace errors.
- **Status:** COMPLETED
- **Result:** AudioResources now exposes per-SFX robot targeting only when requested, keeps category sorting visual-only in the Inspector, and preserves runtime array order and audio references.
- **Remaining limitations:** The Unity Inspector was not live-click-verified because the editor UI automation surface was unavailable; Unity may need its normal script/domain reload to display the updated drawer.
- **Closed:** 2026-10-31
## Task: Make AudioResources volume changes realtime

- **Status:** COMPLETED
- **Started:** 2026-10-31
- **User request:** Make per-resource audio volume configurable in `AudioResources` and apply changes in realtime while sounds are playing.
- **Scope:** Preserve existing UI/music/SFX category volume controls, use serialized `AudioResources` values as the source of truth for event and clip volume, and update active playback without unrelated audio behavior changes.
- **Acceptance criteria:** Changing an `AudionElement.volume` or `AudioClipEntry.volume` value in `AudioResources` affects active playback on the next runtime update; newly played sounds use the current values; the project compiles.
- **Next steps:** Add the narrowest runtime refresh path, validate serialized resource fields and compilation, then close this entry.
- **Change:** `SoundCaller` now refreshes active audio volumes every runtime update; `AudioPreset` keeps the selected `AudioClipEntry` reference so event and per-clip volume changes are both read live from `AudioResources`.
- **Change:** New `AudionElement` entries default to volume `1`, while existing per-clip `AudioClipEntry` volume and runtime playback multipliers remain supported.
- **Validation:** `dotnet build Assembly-CSharp.csproj --no-restore --verbosity minimal` passed with `0 errors`; only existing `System.Net.Http` and FishNet deprecation warnings remain. `git diff --check` reports only pre-existing trailing whitespace in `Client.unity`.
- **Result:** Volume fields in `AudioManager` prefab's `AudioResources` can be edited during Play Mode and active audio updates on the next frame.

## Task: Add AudioResources editor preview buttons

- **Status:** COMPLETED
- **Started:** 2026-10-31
- **User request:** Add a play button beside each `AudioResources` name, volume, and delay row so the configured sound and volume can be auditioned directly in the Unity Inspector.
- **Scope:** Add editor-only preview UI for `AudionElement` without adding runtime scene objects or changing gameplay playback; preview the first assigned clip using the event and clip volume values.
- **Acceptance criteria:** Each expanded `AudionElement` shows a play button near its name/volume/delay fields, clicking it previews the configured clip, and the editor code compiles.
- **Next steps:** Implement the property drawer and editor preview helper, then validate editor compilation and serialized inspector behavior.
- **Change:** Added `Assets/Editor/AudioResourcesEditor.cs` with an `AudionElement` property drawer, inline `Play` button, first-clip selection, and volume-aware editor preview using Unity's internal preview API through reflection.
- **Validation:** `dotnet build Assembly-CSharp-Editor.csproj --no-restore --verbosity minimal` passed with `0 errors`; only existing `System.Net.Http`, FishNet deprecation, and TMP deprecation warnings remain.
- **Result:** Expanding any `AudioResources` event now shows `Name`, `Vol`, `Delay`, and `Play`; the preview uses `AudionElement.volume × AudioClipEntry.volume` without adding runtime objects to scenes.
- **Remaining limitation:** The button was compile-validated but not clicked in the open Unity Inspector during this pass; Unity may need one script reload/domain reload before the drawer appears.
## Task: Restore AudioResources volume sliders

- **Status:** COMPLETED
- **Started:** 2026-10-31
- **User request:** Restore 0..1 slider controls for AudioResources volume settings in the custom preview inspector.
- **Scope:** Keep preview button layout and realtime volume behavior unchanged; render event and delay fields as 0..1 sliders and preserve per-clip volume sliders.
- **Acceptance criteria:** `Vol` (and `Delay`) show sliders constrained to 0..1 in the custom drawer; editor assembly compiles.
- **Result:** Updated the custom `AudioResources` drawer to render `Vol` and `Delay` with explicit 0..1 sliders. Per-clip volume remains a 0..1 slider through its `[Range]` attribute.
- **Validation:** `dotnet build Assembly-CSharp-Editor.csproj --no-restore --verbosity minimal` passed with 0 errors; existing dependency and obsolete-API warnings remain.
- **Remaining limitations:** The Inspector UI was not interactively clicked in the Unity Editor during this validation.
## Task: Ensure visible AudioResources volume and delay sliders

- **Status:** COMPLETED
- **Started:** 2026-10-31
- **User request:** Ensure the `Volume` and `Delay` fields in the `AudioResources` inspector are sliders from `0` to `1`.
- **Scope:** Keep the preview button and realtime audio behavior unchanged; make the custom inspector range controls explicit and visibly labeled.
- **Acceptance criteria:** `Volume` and `Delay` use constrained 0..1 slider controls in the custom drawer; editor assembly compiles.
- **Result:** The custom drawer now shows a clearly labeled `Volume` slider and a `Delay` slider, both clamped to `0..1` before drawing and editing.
- **Validation:** `dotnet build Assembly-CSharp-Editor.csproj --no-restore --verbosity minimal` passed with `0 errors`; existing dependency and obsolete-API warnings remain.
- **Remaining limitations:** The Inspector UI was not interactively clicked in the Unity Editor during this validation; Unity may need a script/domain reload to display the latest drawer.
## Task: Fix collapsed AudioResources sliders

- **Status:** COMPLETED
- **Started:** 2026-10-31
- **User request:** Make the `Volume` and `Delay` controls visibly render as sliders in the `AudioResources` Inspector, matching the supplied screenshot.
- **Scope:** Keep the inline preview button and realtime audio behavior unchanged; widen the range controls by placing them on separate rows.
- **Acceptance criteria:** The custom drawer gives `Volume` and `Delay` enough width to display slider tracks from `0` to `1`; editor assembly compiles.
- **Result:** Updated the custom drawer to place `Volume` and `Delay` on separate full-width rows while keeping the `Play` button in the right column. The sliders now have enough width to render their tracks visibly.
- **Validation:** `dotnet build Assembly-CSharp-Editor.csproj --no-restore --verbosity minimal` passed with `0 errors`; existing dependency and obsolete-API warnings remain.
- **Remaining limitations:** The supplied Inspector screenshot was from before this layout change; Unity must reload the editor script/domain to display the updated layout.
## Task: Fix AudioResources preview below full volume

- **Status:** COMPLETED
- **Started:** 2026-10-31
- **User request:** Make the `AudioResources` Play button continue previewing the sound when volume is slightly below `1.0`.
- **Scope:** Preserve the Inspector sliders and realtime runtime audio behavior; replace the non-playing temporary-clip preview path with an editor-only volume-controlled source.
- **Acceptance criteria:** Preview works for values such as `0.992` and remains scaled by the selected volume; editor assembly compiles.
- **Result:** Replaced temporary sample-scaled `AudioClip` playback with a hidden editor-only `AudioSource` whose `volume` is set from the selected `AudioResources` value. This avoids Unity 6 `AudioUtil` rejecting generated preview clips.
- **Validation:** `dotnet build Assembly-CSharp-Editor.csproj --no-restore --verbosity minimal` passed with `0 errors`; existing dependency and obsolete-API warnings remain.
- **Validation update:** After the editor script reload, clicked `Play` in the Unity Inspector with the event volume reduced to `0.873`; the button executed without a Console error. The temporary test value was restored to `1.0`.
- **Remaining limitations:** Audio output itself cannot be measured through the UI automation session, so final confirmation should include listening to the clip once in the local Unity Editor.
## Task: Organize audio resources and add profile music

- **Status:** COMPLETED
- **Started:** 2026-10-31
- **User request:** Add the `profile` music to the profile window, rename audio assets with clear English resource names, and organize them into category folders.
- **Scope:** Inventory all project audio assets and AudioResources references first; preserve Unity references while moving and renaming assets; connect the profile track through the existing audio flow; group music, general robot, walker, tracked, UI, and other audio categories.
- **Acceptance criteria:** Audio assets have clear English names and logical folders, existing references remain valid, and the profile window plays the `profile` track.
- **Discovery:** Found 66 active audio files under `Assets/Resources/GameAudio`, including the existing `profile.mp3`. `AudioManager.prefab` stores resource entries and scene overrides by asset GUID, so moves will preserve each `.meta` file.
- **Decision:** Organize project audio into `Music`, `UI`, `Robots/General`, `Robots/Walker`, `Robots/Tracked`, and `Environment` categories; keep third-party imported audio packs outside this refactor.
- **Result:** Renamed and moved all 66 active GameAudio assets with their `.meta` files; added `Profile_Music` to `AudioManager.prefab`; profile popup playback starts on open and stops on close; documented the ownership and folder convention in `docs/codex-game/audio-presentation.md`.
- **Validation:** All 58 unique serialized audio GUID references resolve to the reorganized assets; old Cyrillic/top-level placeholder filenames are gone; `dotnet build Assembly-CSharp.csproj --no-restore --verbosity minimal` and `dotnet build Assembly-CSharp-Editor.csproj --no-restore --verbosity minimal` pass with 0 errors.
- **Remaining limitations:** `git diff --check` reports four pre-existing trailing-whitespace lines in `Assets/Game/Scenes/Client.unity`; they were not changed because they belong to existing scene overrides. Unity Editor audio playback should be listened to once after its asset refresh.
## Task: Move remaining audio from ambience to music

- **Status:** COMPLETED
- **Started:** 2026-10-31
- **User request:** Keep only music for now, give the moved audio files correct English names, and fix playback for music moved from the ambience category.
- **Scope:** Inspect the current post-move AudioResources and asset folders; preserve existing Unity GUID references; route menu, map, and profile playback through the music category without reintroducing ambience resources.
- **Acceptance criteria:** Music assets use clear English names, all intended playback calls resolve from `musicClips`, no required ambience array is needed, and runtime/editor builds pass.
- **Discovery:** The scene already moved `Garage_DevelopmentTree` and `Garage_Main` to `musicClips`, but the source prefab still exposed them under `ambienceClips`; runtime calls still requested the removed category.
- **Decision:** Remove the ambience resource field and enum route, move all former environment clips into `Music` with `*_Music` filenames, and keep battle playback to one music track instead of layering former ambience clips.
- **Result:** Moved six former ambience assets into `Assets/Resources/GameAudio/Music` with clear English names; moved Garage and map events into the prefab `musicClips` array; removed the ambience array/type and routed menu, profile, result, and battle playback through music.
- **Validation:** All 72 audio metadata GUIDs are unique; 58 serialized clip references resolve; the Client scene has seven music entries and no ambience overrides; no ambience route references remain; runtime and Editor assemblies build with 0 errors.
- **Remaining limitations:** Map music entries retain their previous `0` volume and are not layered during battle; Unity Editor playback should be listened to once after asset refresh. Existing project warnings remain unrelated.
## Task: Organize AudioResources categories and robot sound metadata

- **Status:** COMPLETED
- **Started:** 2026-10-31
- **User request:** Reorganize `AudioResources` into convenient sound categories, subdivide UI sounds, clearly expose cannon shots, and add a robot enum so shots can specify the robot using only active 2-faction × 4-robot roster entries.
- **Scope:** Inventory current audio event names, `AudioManager` serialization, robot roster/faction identifiers, and the custom `AudioResources` editor. Design a serializable category structure that preserves current runtime lookups and Unity clip GUIDs while making Inspector volume/preview/editing convenient.
- **Acceptance criteria:** Inspector shows named sound categories and UI subcategories, cannon shots are easy to find and have robot metadata, active robot enum excludes development/black tree robots, existing audio playback still resolves, and runtime/editor assemblies compile.
- **Discovery:** `RobotRegistry.asset` confirms the playable roster is `ia_l1_starter`, `ia_l2_scout`, `ia_l2_guardian`, `ia_l2_colossus`, `nv_l1_starter`, `nv_l2_scout`, `nv_l2_guardian`, and `nv_l2_colossus`; all black-tree `L3/L4` entries are marked `isDevelopmentOnly` and are excluded.
- **Change:** Added `AudioSfxCategory`, `AudioUiCategory`, and `AudioRobot` metadata to `AudionElement`, plus category sorting while preserving the existing `uiClips`, `soundClips`, and `musicClips` runtime lookup arrays.
- **Change:** Replaced the single-element-only editor drawer with a grouped `AudioResources` inspector. UI entries are grouped by subcategory; robot/combat entries are grouped by SFX category; each expanded item keeps 0..1 volume/delay sliders, robot selection, and volume-aware `Play` preview.
- **Change:** Classified the current `AudioManager.prefab` resources and placed standard/heavy shot resources in the `Cannon Shots` group with an `Any Robot` default target.
- **Change:** Connected `Cannon_Medium_Shot` and `Cannon_Heavy_Shot` to the active shot selector and preserved the previous generic shot clips as additional variants.
- **Validation:** `dotnet build Assembly-CSharp.csproj --no-restore --verbosity minimal` passed with `0` errors; `dotnet build Assembly-CSharp-Editor.csproj --no-restore --verbosity minimal` passed with `0` errors; serialized audio clip audit found `62` unique GUIDs with `0` missing references; all eight cannon clip GUIDs are referenced; `git diff --check` returned no whitespace errors.
- **Result:** `AudioResources` now opens as organized UI, robot/combat, and music sections with category foldouts, per-resource volume/delay sliders, per-clip volume, preview buttons, cannon-shot grouping, and an active-roster robot selector. Development-only black-tree robots are not available in the enum.
- **Remaining limitations:** The Unity window was open but its UI automation accessibility surface was unavailable, so the final Inspector layout was validated by code/build/YAML audits rather than a live click-through. Unity may need its normal script/domain reload to display the new inspector.

## Task: Collect 5 cannon reload sounds

- **Status:** IN_PROGRESS
- **Started:** 2026-10-31
- **User request:** Find five different cannon reload sounds.
- **Scope:** Find public, game-usable reload/mechanical cannon sounds, collect them into a separate Desktop package, preserve source URLs, authors, licenses, and fit notes, and do not modify Unity code, scenes, prefabs, or audio configuration.
- **Acceptance criteria:** The Desktop package contains exactly 5 non-empty, distinct reload candidates with source/license metadata and basic audio-format validation.
- **Discovery:** Found one existing local authentic tank-cannon reload recording (`freesound_community-tank-reload-47828.mp3`) and four suitable public Pixabay candidates: futuristic mechanical reload, short gun reload, Universfield mechanical reload, and a manual gear-handle mechanism.
- **Selection:** Use the authentic tank reload as the primary candidate; use the four mechanical/weapon variants as alternatives for medium/heavy robotic cannon reload behavior. Label the non-tank variants clearly in the manifest.
- **Result:** Created `C:\Users\TorasPC\Desktop\WarOfMachines_CannonReload_5` with five reload candidates, `README.md`, and `Sources.csv`. The set contains one authentic tank-cannon reload plus four distinct mechanical/robotic alternatives.
- **Validation:** Confirmed 5 manifest rows, 5 audio files, 5 unique SHA-256 hashes, all files non-empty, and valid MP3 headers. No Unity code, scenes, prefabs, or audio configuration changed.
- **Limitations:** Four files are generic weapon or mechanical reload recordings rather than verified tank-cannon recordings; the manifest marks their intended use and the authentic tank file separately.
- **Closed:** 2026-10-31

## Task: Refine AudioResources faction grouping and robot targeting

- **Status:** IN_PROGRESS
- **Started:** 2026-10-31
- **User clarification:** Do not expose an unnecessary robot enum for every sound. When a sound is robot-specific, it should identify the robot(s) it belongs to; the Inspector should clearly separate sounds common to all robots, Iron Alliance sounds, and Nova Syndicate sounds.
- **Scope:** Replace the single robot enum target with faction/robot assignment metadata, group SFX visually in the custom Inspector by common/faction ownership, preserve serialized array order and runtime event lookup, and keep volume/delay/preview controls usable.
- **Acceptance criteria:** Inspector has clearly labeled Common, Iron Alliance, and Nova Syndicate sections; a resource can be marked common, assigned to one faction, or assigned to specific robots within a faction; unrelated sounds do not show irrelevant robot controls; existing cannon resources remain editable without losing clips or GUIDs; runtime/editor builds pass.
- **Discovery:** The previous `AudioRobot` enum represented one robot per resource and was always paired with a single target field. The active playable roster is four robots per faction (`ia_*` and `nv_*`); development-only L3/L4 robots remain excluded.
- **Decision:** Store `AudioRobotScope` as `Common`, `IronAlliance`, or `NovaSyndicate`. Store optional specific targets as serialized robot-code strings, where an empty list means all robots in the selected faction. This avoids an enum entry for every robot while still allowing multi-robot assignments.
- **Change:** Replaced `AudioRobot`, `robotSpecific`, and `robot` with `AudioRobotScope` and `robotCodes` in `AudionElement`; migrated all current SFX resources in `AudioManager.prefab` to explicit `Common` scope.
- **Change:** Reworked the custom Inspector into colored ownership sections (`Common — All Robots`, `Iron Alliance`, and `Nova Syndicate`) with SFX category foldouts. Faction resources show an `All Robots` toggle and active-roster checkboxes without changing serialized array order.
- **Validation:** `dotnet build Assembly-CSharp.csproj --no-restore --verbosity minimal` passed with `0` errors; `dotnet build Assembly-CSharp-Editor.csproj --no-restore --verbosity minimal` passed with `0` errors; all `22` SFX entries have explicit `Common` scope; the prefab has `0` old `robot`/`robotSpecific` fields; all `62` unique serialized audio clip GUIDs resolve; `git diff --check` returned no whitespace errors.
- **Result:** AudioResources now shows clear ownership sections, supports faction-wide or multi-robot sound assignment without a per-robot enum, and preserves the serialized resource order and clip references.
- **Remaining limitations:** The Unity Inspector was validated by source/build/YAML checks but not live-click-tested because the editor UI automation surface is unavailable; Unity may need its normal script/domain reload to display the new drawer.
- **Status:** COMPLETED
- **Closed:** 2026-10-31

## Task: Remove AudioResources category enums

- **Status:** IN_PROGRESS
- **Started:** 2026-10-31
- **User request:** Remove `AudioSfxCategory` and `AudioUiCategory` from `AudioResources.cs`; these enums are not wanted.
- **Scope:** Replace enum-backed category serialization and Inspector controls with simple string categories while preserving existing audio clips, runtime arrays, volume/delay controls, preview buttons, robot targeting, and Inspector grouping.
- **Acceptance criteria:** Neither enum exists in runtime/editor code, existing YAML audio entries remain readable, the Inspector still groups SFX/UI resources clearly, and runtime/editor assemblies build without errors.
- **Discovery:** The enums were used only by `AudioResources.cs` and the custom Inspector; `AudioManager.prefab` stored their numeric values in `uiCategory` and `category` fields.
- **Change:** Removed both enums, changed category fields to serialized strings, replaced enum controls with string dropdowns, and migrated the prefab values to readable category names without changing array order or clips.
- **Validation:** Runtime and editor assemblies build successfully with `0` errors; no `AudioSfxCategory` or `AudioUiCategory` references remain in `Assets`; the prefab has no numeric category values; all `62` serialized audio clip GUIDs resolve; `git diff --check` reports no whitespace errors.
- **Result:** `AudioResources` no longer contains the two unwanted category enums. The Inspector keeps the same readable category grouping and dropdown controls using strings, while audio clips and array order are preserved.
- **Status:** COMPLETED
- **Closed:** 2026-10-31

## Task: Make AudioResources robot selection registry-backed

- **Status:** IN_PROGRESS
- **Started:** 2026-10-31
- **User request:** Replace hard-coded robot selection with a dropdown populated from the existing robot registry, so adding a robot does not break or require rewriting audio assignments.
- **Scope:** Use stable robot codes from `RobotRegistry.asset` for audio target metadata, keep common/faction Inspector grouping, include faction-wide options, preserve existing serialized audio resources and clip GUIDs, and exclude development-only robots.
- **Acceptance criteria:** Audio Inspector robot dropdown is generated from the active registry roster, existing assignments survive roster additions because they serialize stable codes rather than enum indices, faction-wide options remain available, missing historical codes are shown safely, and runtime/editor builds plus GUID audits pass.
- **Discovery:** `RobotRegistry.Item.code` is the stable identifier for each robot; active entries have a prefab reference, while development-only black-tree entries are marked `isDevelopmentOnly` and have no prefab.
- **Change:** Replaced the audio enum/list metadata with the serialized `robotTarget` string. The custom Inspector now builds the dropdown from active `RobotRegistry` entries, keeps common and faction-wide options, preserves unknown old codes as a visible missing target, and keeps Common/Iron Alliance/Nova Syndicate sections separated without reordering arrays.
- **Migration:** Updated all 22 existing SFX entries in `AudioManager.prefab` to the stable `common` target.
- **Validation:** Runtime and editor assemblies build successfully with `0` errors; the registry exposes `8` active robots; old audio metadata references are absent; all `62` serialized audio clip GUIDs resolve; `git diff --check` reports no whitespace errors.
- **Result:** Adding a robot to the active registry automatically adds it to the Inspector dropdown, and existing audio assignments remain unchanged because they store stable string codes rather than enum indices.
- **Status:** COMPLETED
- **Closed:** 2026-10-31

## Task: Split AudioResources robot sounds into separate lists

- **Status:** IN_PROGRESS
- **Started:** 2026-10-31
- **User request:** Do not show robot sound clips as one `soundClips` list with category tabs; expose separate lists for sounds common to all robots, Iron Alliance, and Nova Syndicate.
- **Scope:** Replace the single robot SFX array in `AudioResources` with physically separate serialized arrays, preserve runtime resource lookup and existing clip references, keep stable robot targeting for faction-specific entries, and simplify the Inspector so each faction has its own list.
- **Acceptance criteria:** `AudioResources` contains separate Common/Iron Alliance/Nova Syndicate sound lists, no category field is required for robot SFX grouping, existing sounds remain available through runtime lookup, and runtime/editor builds plus audio GUID audits pass.
- **Discovery:** `soundClips` was the only serialized robot SFX array; the Inspector then created faction/category views over that same array. Runtime lookup was centralized in `AudioResources.GetResource`, so it can search multiple arrays without changing callers.
- **Change:** Replaced `soundClips` with separate `commonRobotClips`, `ironAllianceRobotClips`, and `novaSyndicateRobotClips` arrays; removed `category` and `uiCategory` metadata; updated the Inspector to render each list as its own foldout; and migrated current SFX into the Common Robots list.
- **Migration:** Kept `uiClips` and `musicClips` as separate top-level arrays, removed obsolete category fields from all existing entries, and kept all robot target dropdowns inside the relevant robot list.
- **Validation:** Runtime and editor assemblies build successfully with `0` errors; `AudioManager.prefab` has separate Common/IRON ALLIANCE/NOVA SYNDICATE arrays, no old `soundClips` or category fields, all `62` serialized audio clip GUIDs resolve, and `git diff --check` reports no whitespace errors.
- **Result:** `AudioResources` now presents physically separate robot sound lists instead of one list with category tabs. Existing runtime event lookup remains intact because it searches all dedicated arrays.
- **Status:** COMPLETED
- **Closed:** 2026-10-31

## Task: Make AudioResources categories first-class list elements

- **Status:** COMPLETED
- **Started:** 2026-10-31
- **User request:** Categories must be elements of a list, with each category containing its own sound list; faction categories must be extendable without hardcoded fields.
- **Scope:** Replace the hardcoded Common/IRON ALLIANCE/NOVA SYNDICATE serialized arrays with a serializable category collection. Preserve existing UI/music resources, runtime lookups, stable robot targets, audio clip GUIDs, and convenient Inspector editing.
- **Acceptance criteria:** A category is added/reordered like a normal list element; each category visibly owns a separate nested sound list; existing faction categories and sounds migrate without losing references; adding another faction requires adding a category element rather than code changes; runtime/editor assemblies compile.
- **Discovery:** `AudioResources` had three hardcoded robot arrays, while runtime lookup was centralized in `GetResource`; the custom Inspector also bound directly to those fields.
- **Change:** Replaced the hardcoded robot arrays with `AudioSoundCategory[] soundCategories`, where every category element owns its own nested `AudionElement[] clips` list. Migrated Common Robot Sounds, IRON ALLIANCE, and NOVA SYNDICATE into the prefab without changing clip GUIDs.
- **Change:** Updated the Inspector to render a category list and a separate expandable sound list inside every category. Added registry-driven faction/robot target options so a new faction prefix in the active `RobotRegistry` appears without adding enums or fields.
- **Change:** Updated `docs/codex-game/audio-presentation.md` with the nested-category workflow.
- **Validation:** `dotnet build Assembly-CSharp.csproj --no-restore --verbosity minimal` passed with `0` errors; `dotnet build Assembly-CSharp-Editor.csproj --no-restore --verbosity minimal` passed with `0` errors; the prefab contains `62` unique clip GUIDs with `0` missing references; Common/IA/NS categories are present; old hardcoded field names are absent; `git diff --check` is clean apart from Git's existing LF/CRLF warnings.
- **Result:** To add a new faction, add one element to `soundCategories`, name it, and fill its nested `clips` list. Existing categories and sound assignments do not shift or break.
- **Remaining limitations:** Unity Editor should perform its normal script/domain reload before the new Inspector layout is visible; live Inspector click-through was not available in this session.
- **Closed:** 2026-10-31

## Task: Sort robot sounds by faction and movement type

- **Status:** COMPLETED
- **Started:** 2026-10-31
- **User request:** Sort the existing robot sounds so IRON ALLIANCE contains tracked-robot sounds and NOVA SYNDICATE contains walker/legged-robot sounds.
- **Scope:** Inspect current robot sound event names and prefab category serialization; move only the relevant `AudionElement` entries between existing category lists while preserving names, volume, delay, target metadata, clip references, and GUIDs.
- **Acceptance criteria:** Tracked movement/reload sounds are in IRON ALLIANCE, walker movement/step/hydraulic sounds are in NOVA SYNDICATE, genuinely shared combat/cannon sounds remain Common, no sound is duplicated or lost, and runtime/editor builds pass.
- **Discovery:** The prefab contained 22 robot events: generic combat/cannon events, two `Tracked_*` events, and seven `Walker_*`/`Heavy_Walker_*` events.
- **Change:** Kept 13 combat/cannon events in `Common Robot Sounds`; moved `Tracked_Movement` and `Tracked_Reload_Ready` into `IRON ALLIANCE`; moved `Walker_*` and `Heavy_Walker_*` into `NOVA SYNDICATE`.
- **Change:** Updated moved entries to `faction:iron_alliance` or `faction:nova_syndicate` robot targets while preserving names, volume, delay, and clip references.
- **Validation:** Runtime and editor assemblies both built with `0` errors; the prefab contains `62` unique clip GUIDs with `0` missing references, all `22` robot events are unique, the three category elements are present, and `git diff --check` reports no whitespace errors.
- **Result:** `AudioResources` now separates tracked sounds under IRON ALLIANCE and legged/walker sounds under NOVA SYNDICATE; shared combat and cannon sounds remain Common.
- **Remaining limitations:** Unity Editor may need its normal asset/script reload before the Inspector reflects the updated category contents; existing unrelated build warnings remain.
- **Closed:** 2026-10-31

## Task: Add robot groups inside audio categories

- **Status:** COMPLETED
- **Started:** 2026-10-31
- **User request:** Within each faction audio category, add nested robot groups so a group can target all robots of that faction or one specific registry robot, with its own sound list.
- **Scope:** Replace per-sound robot targeting with `category -> robot group -> sound clips`; keep category elements editable, preserve current UI/music resources, migrate existing faction sounds, and keep stable registry-backed robot selection.
- **Acceptance criteria:** Inspector visibly shows faction categories containing robot groups; each group has an `All Robots` or concrete robot target and its own clips list; new robots appear without enum changes or assignment shifts; existing names, volumes, delays, clip references, and GUIDs remain intact; runtime/editor builds pass.
- **Discovery:** `AudioSoundCategory` previously owned a direct `AudionElement[] clips` list, and every sound carried its own `robotTarget`. That duplicated target metadata instead of representing the requested category hierarchy.
- **Change:** Added `AudioRobotSoundGroup` so the data model is `soundCategories -> robotGroups -> clips`; category groups select `All Robots` or a stable robot code, while `AudionElement` now only stores sound data and volume/delay settings.
- **Change:** Updated the custom Inspector with faction dropdowns, nested robot-group foldouts, registry-backed robot dropdowns filtered to the selected faction, and separate sound lists with existing preview/sliders.
- **Migration:** Migrated Common, IRON ALLIANCE, and NOVA SYNDICATE into one `All Robots` group each. Existing event names, volume, delay, clip references, and faction targets were preserved.
- **Change:** Updated `docs/codex-game/audio-presentation.md` to document the three-level structure.
- **Validation:** The first Editor build exposed a removed internal helper enum reference; restored the editor-only helper and reran successfully. Runtime and Editor assemblies both pass with `0` errors; the prefab has `3` categories, `3` robot groups, `22` unique sound events, `62` unique clip GUIDs, `0` missing GUIDs, and `0` per-sound robot target fields; `git diff --check` reports no whitespace errors.
- **Result:** New robot-specific audio is added by creating a robot-group element inside the faction category and selecting the registry robot; faction-wide audio uses `All Robots`. New active registry robots appear without enum changes or shifting existing assignments.
- **Remaining limitations:** Unity Editor may need its normal script/domain reload before the updated nested Inspector is visible; existing unrelated build warnings remain.
- **Closed:** 2026-10-31

## Task: Split robot audio variants by robot type

- **Status:** COMPLETED
- **Started:** 2026-10-31
- **User request:** Inspect the existing audio files and split shot and step variants between the concrete robot types instead of keeping all movement-type sounds in one faction-wide group.
- **Scope:** Audit audio event names, clip assets, robot registry entries, and runtime lookup behavior; assign only evidence-based variants to nested robot groups while preserving clip references and existing playback behavior.
- **Acceptance criteria:** Existing sound variants are distributed into clear robot-type groups; shot and step variants are separated where the assets support it; no sound or GUID is lost; runtime/editor assemblies remain compilable.
- **Discovery:** The audio prefab contained six medium-shot clips, four heavy-shot clips, one regular walker step/turn pair, one heavy walker step/turn pair, and faction-specific movement/reload clips. Existing playback looked up only an event name, so robot-specific Inspector groups would otherwise not affect runtime selection.
- **Decision:** Keep stable event names and duplicate only the event entries across robot groups; resolve SFX by exact `RobotRegistry.Item.code`, then faction, then Common. This keeps the Inspector editable and lets new robot entries be added without enum changes.
- **Change:** Split the active shot events into robot-targeted groups: one medium-shot variant per active non-colossus robot and heavy-shot variants in each active colossus group. Kept one common medium/heavy fallback for newly added robots without a specific group. Kept faction-wide tracked/walker movement and reload resources in their faction groups, with heavy walker step/turn overrides in `nv_l2_colossus`.
- **Change:** Added robot-aware SFX resolution in `AudioResources`, preferring an exact registry robot code, then its faction, then Common. Updated shot, reload, walker loop, step, and turn callers to pass the emitting robot code while preserving legacy event names.
- **Validation:** Runtime and Editor assemblies both built with `0` errors; the prefab contains `30` robot event entries, `22` unique event names, `64` audio clip references, `62` unique audio clip GUIDs, and `0` missing audio GUIDs; all eight active registry robot codes have targeted sound groups; `git diff --check` is clean apart from Git's existing LF/CRLF warnings.
- **Result:** Existing shot and step variants are now visibly separated by concrete robot in `AudioResources`, and gameplay selects the matching group at runtime without enums or index-based assignments.
- **Remaining limitations:** The audio assets do not encode semantic robot ownership in their filenames, so numbered cannon variants were assigned deterministically across the active robot roster. Unity Editor should perform its normal asset/script reload before inspecting the new groups.
- **Closed:** 2026-10-31

## Task: Verify heavy walker audio names

- **Status:** COMPLETED
- **Started:** 2026-10-31
- **User request:** Verify whether `Walker_Heavy_Step` and `Walker_Heavy_Turn` were removed from the audio resources.
- **Scope:** Check source asset filenames, serialized event names, runtime constants, and current robot-group assignment; make a corrective rename only if the resources are actually missing or inconsistently named.
- **Acceptance criteria:** Clearly identify whether both heavy walker sounds exist, where they are stored, and whether the runtime event names match the serialized resources.
- **Discovery:** Both source assets exist as `Assets/Resources/GameAudio/Robots/Walker/Walker_Heavy_Step.wav` and `Walker_Heavy_Turn.wav`. The serialized `AudioResources` event names are `Heavy_Walker_Step` and `Heavy_Walker_Turn`, assigned to `nv_l2_colossus`; the runtime constants and selector use the same event names.
- **Result:** The sounds were not removed. The apparent mismatch is only the reversed event-name order compared with the source filenames.
- **Validation:** Confirmed both asset files, their prefab GUID references, matching runtime constants, and heavy-walker call sites. No implementation changes were required.
- **Remaining limitations:** The Inspector displays the event names `Heavy_Walker_Step` and `Heavy_Walker_Turn`, not the source filenames `Walker_Heavy_Step` and `Walker_Heavy_Turn`. Rename them only if the filename-style naming is preferred.
- **Closed:** 2026-10-31

## Task: Optimize AudioManager prefab Inspector performance

- **Status:** COMPLETED
- **Started:** 2026-11-01
- **User request:** Investigate why the `AudioManager` prefab Inspector lags and determine whether it can be optimized.
- **Scope:** Inspect the custom `AudioResources` editor, serialized `AudioManager.prefab` data, registry lookups, preview/audio-preview lifecycle, and editor allocation/repaint patterns; make only focused editor-side changes that preserve the current audio data and Inspector behavior.
- **Acceptance criteria:** Identify the concrete source of Inspector lag; reduce avoidable editor work without changing runtime audio behavior or serialized clip references; validate runtime/editor compilation and serialized audio GUID integrity.
- **Next steps:** Inspect repository state and applicable audio editor/runtime files, measure or reason about repeated Inspector work, patch the focused editor owner if justified, then run targeted validation and close this entry.

### Discovery checkpoint — 2026-11-01

- `Assets/Editor/AudioResourcesEditor.cs` calls `AssetDatabase.FindAssets("t:RobotRegistry")` indirectly from faction/robot option rendering on every Inspector repaint and more than once per robot group.
- The same repaint rebuilds robot option arrays, labels, `List<int>` index collections, and preview clip data for every visible resource. The volume slider also writes its serialized value every repaint even when unchanged.
- `AudioResourcesPreview` subscribes to `EditorApplication.update` permanently after editor load, even when no preview is active.
- The serialized prefab is modest in size (three robot categories, nine robot groups, 20 UI entries, 30 robot sound entries, seven music entries, and 64 clip references), so caching editor metadata and eliminating redraw allocations is the focused optimization rather than changing the audio data model.

### Decision

- Keep the current Inspector layout, preview button, serialized arrays, clip GUIDs, and runtime classes unchanged.
- Cache registry-derived target options per Inspector instance and invalidate them on project/undo changes; reuse serialized root properties and draw arrays directly without temporary index lists.
- Register the preview update callback only while a preview source exists and assign slider values only after an actual GUI change.

### Change checkpoint — 2026-11-01

- `Assets/Editor/AudioResourcesEditor.cs`: cached `RobotRegistry` option data and root serialized properties; removed per-repaint `AssetDatabase` searches, temporary element-index lists, repeated popup assignments, and unchanged slider writes.
- `Assets/Editor/AudioResourcesEditor.cs`: made preview cleanup polling lazy so `EditorApplication.update` is subscribed only while a preview is playing.
- No runtime audio source, prefab serialization, scene, clip reference, or GUID was changed.

### Completion checkpoint — 2026-11-01

- **Result:** The `AudioManager` Inspector now performs registry discovery only when the editor opens or project/undo data changes, reuses cached target options, avoids repeated serialized writes and temporary collection allocations, and does not run an idle preview callback.
- **Validation:** `dotnet build Assembly-CSharp-Editor.csproj --no-restore --verbosity minimal` passed with `0` errors; `dotnet build Assembly-CSharp.csproj --no-restore --verbosity minimal` passed with `0` errors; the prefab contains `62` unique serialized audio clip GUIDs with `0` missing references; `git diff --check` is clean for the journal and tracked changes.
- **Warnings:** Existing `System.Net.Http` version-conflict warnings and the unrelated obsolete TMP API warning remain.
- **Remaining limitations:** The Unity Inspector was not live-profiled in this session, so the exact frame-time reduction still needs confirmation after Unity reloads `Assets/Editor/AudioResourcesEditor.cs`.
- **Closed:** 2026-11-01

## Task: Diagnose garage music restarting after Settings

- **Status:** COMPLETED
- **Started:** 2026-11-01
- **User request:** Explain why garage music starts from the beginning after returning from Settings.
- **Scope:** Trace Settings open/close lifecycle, garage music ownership, `SoundCaller`/`AudioManager` music playback and stop calls, and identify whether the track is being recreated or explicitly restarted; do not change behavior unless a fix is explicitly requested.
- **Acceptance criteria:** Identify the exact call path that causes the restart, distinguish intended music behavior from the bug, and report the smallest safe fix if one is needed.
- **Next steps:** Inspect current audio and Settings owners, map the relevant lifecycle calls, then record the finding and any remaining uncertainty here.

### Discovery checkpoint — 2026-11-01

- Opening Settings from the Main Menu calls `MenuManager.OpenMenu(MenuType.Settings)`; `SoundCaller.HandleMenuOpened` has no Settings branch, so `Garage_Main` keeps playing while Settings is open.
- Returning through `SettingsView.OnBackClicked` calls `MenuManager.OpenMenu(MenuType.MainMenu)`, which invokes `SoundCaller.HandleMenuOpened(MenuType.MainMenu)`.
- The Main Menu branch unconditionally calls `StopBattleAudio()`, which unconditionally calls `StopMusic()`, and then calls `PlayMusic(AudioEventNames.GarageMain)`.
- `AudioManager` can avoid restarting an already-playing track, but that protection is bypassed because the current track is stopped first. `AudioPreset.Play` then explicitly sets `source.time = 0f` before playback.

### Completion checkpoint — 2026-11-01

- **Result:** Garage music restarts because returning from Settings reopens Main Menu, stops all music, and starts `Garage_Main` again. This is not caused by Settings audio controls.
- **Safe fix:** In the Main Menu audio handler, only call `StopBattleAudio()` when battle audio is actually active, then let `PlayMusic(GarageMain)` remain idempotent. This preserves switching from battle/result music while allowing Settings return to continue the existing garage track.
- **Validation:** Confirmed the call path and reset behavior by static inspection of `MenuManager`, `SettingsView`, `SoundCaller`, `AudioManager`, and `AudioPreset`. No project files were changed for this diagnostic task.
- **Remaining limitations:** The safe fix was not applied because the request asked for the cause; applying it requires a direct implementation request.
- **Closed:** 2026-11-01

## Task: Preserve garage music position after Settings

- **Status:** IN_PROGRESS
- **Started:** 2026-11-01
- **User request:** Apply the fix so returning from Settings does not restart garage music from the beginning.
- **Scope:** Change only the menu/audio transition logic responsible for the restart; preserve battle-audio shutdown, result music switching, UI sounds, and runtime audio resource data.
- **Acceptance criteria:** Returning from Main Menu Settings keeps the currently playing `Garage_Main` position; leaving active battle audio still stops battle music and switches to garage music; runtime/editor assemblies compile.
- **Next steps:** Patch `SoundCaller` with the smallest ownership-consistent guard, build the affected assemblies, and close this entry with validation.

### Change checkpoint — 2026-11-01

- `Assets/Game/Scripts/Audio/SoundCaller.cs`: the Main Menu transition now calls `StopBattleAudio()` only when `_battleAudioActive` is true; normal Settings return leaves the current `Garage_Main` preset alive so `AudioManager` can reuse it.
- Active battle transitions retain the previous stop, battle-finished UI cue, and garage music switch behavior.

### Completion checkpoint — 2026-11-01

- **Status:** COMPLETED
- **Result:** Returning from Main Menu Settings no longer stops and restarts `Garage_Main`; an active battle transition still stops battle music, plays the existing battle-finished UI cue, and switches to garage music.
- **Validation:** `dotnet build Assembly-CSharp.csproj --no-restore --verbosity minimal` passed with `0` errors; `dotnet build Assembly-CSharp-Editor.csproj --no-restore --verbosity minimal` passed with `0` errors; targeted `git diff --check` passed.
- **Warnings:** Existing `System.Net.Http` conflict warnings and unrelated obsolete Unity API warnings remain.
- **Remaining limitations:** Runtime playback was not manually auditioned in Unity during this pass; the code path now relies on the existing `AudioManager` same-track reuse behavior.
- **Closed:** 2026-11-01

## Task: Audit and fix all menu music restart transitions

- **Status:** COMPLETED
- **Started:** 2026-11-01
- **User request:** Find all similar places where returning from Profile or another menu stops or restarts music incorrectly, and fix them.
- **Scope:** Audit every `PlayMusic`, `StopMusic`, `StopBattleAudio`, menu open/close, and profile popup music call; preserve intentional transitions between different contexts while keeping the currently playing track alive when opening and closing overlays or returning to the same context.
- **Acceptance criteria:** Profile return and other same-context menu returns do not mute or restart the active track; intentional switches between garage, development tree, battle, and result music still work; runtime/editor assemblies compile.
- **Next steps:** Inventory all music lifecycle call sites, identify each incorrect stop/restart path, patch the focused audio owner, validate transitions by static call-path checks and builds, then close this entry.

### Discovery checkpoint — 2026-11-01

- `ProfileStatisticsPopup.Open()` intentionally switches the Main Menu overlay to `Profile_Music`.
- `ProfileStatisticsPopup.Close()` called `SoundCaller.StopMusic()` directly and did not restore `Garage_Main`, leaving the Main Menu silent after the popup closed.
- The remaining direct music stops are part of intentional context switches: battle start, development-tree music, result music, or active battle shutdown. No other overlay close path directly stops music without restoring its owning context.

### Decision

- Replace only the profile close stop with a request to play `Garage_Main`. `AudioManager` already reuses a currently playing same-name music preset, while different music contexts still switch normally.

### Extended audit decision — 2026-11-01

- Remove redundant explicit `StopMusic()` calls immediately before replacement `PlayMusic()` calls in battle start, development-tree, and result transitions. `AudioManager.Play` already stops a different music preset and reuses the same active event.
- Make `StopBattleAudio()` a no-op when battle audio is not active, preventing unrelated music from being stopped by repeated or out-of-order menu notifications.

### Change checkpoint — 2026-11-01

- `Assets/Game/Scripts/UI/MainMenu/ProfileStatisticsPopup.cs`: profile close now requests `Garage_Main` instead of calling `StopMusic()`, restoring the Main Menu music context after the overlay closes.
- `Assets/Game/Scripts/Audio/SoundCaller.cs`: retained the previous Settings-return guard; no other intentional music transitions were changed.
- `Assets/Game/Scripts/Audio/SoundCaller.cs`: removed redundant stop-before-play calls for battle start, development tree, and result music; `StopBattleAudio()` now exits without stopping unrelated music when battle audio is inactive.

### Completion checkpoint — 2026-11-01

- **Result:** Fixed the silent Profile return and audited all menu music lifecycle call sites. Same-context returns now reuse the active track; intentional switches between garage, development tree, battle, and result music remain intact.
- **Validation:** `dotnet build Assembly-CSharp.csproj --no-restore --verbosity quiet` passed with 0 errors; `dotnet build Assembly-CSharp-Editor.csproj --no-restore --verbosity quiet` passed with 0 errors; static `StopMusic` audit found only the method definition and the active-battle shutdown call; `git diff --check` passed.
- **Remaining limitations:** Existing `System.Net.Http` reference warnings remain. Manual Unity play-mode verification was not run in this environment.
- **Closed:** 2026-11-01

## Task: Remove cannon shots from shared robot sounds and enable list-item removal

- **Status:** IN_PROGRESS
- **Started:** 2026-11-01
- **User request:** Remove `Cannon_Heavy_Shot` and `Cannon_Medium_Shot` from common robot sounds because cannon shots will be configured per robot, and add the ability to remove sound entries from any position in the list.
- **Scope:** Identify the shared robot sound configuration and its editor/inspector list UI, remove the two cannon shot entries from the shared defaults or serialized data without affecting per-robot shot configuration, and make list removal work for middle entries while preserving remaining order and references.
- **Acceptance criteria:** The shared robot sound list no longer contains the two cannon shot events; the inspector exposes a reliable remove action for every list element, including middle elements; runtime and editor assemblies compile; unrelated working-tree changes remain untouched.
- **Next steps:** Locate the shared robot sound asset and custom inspector/list drawer, inspect how list elements are added and removed, patch the owning editor/runtime code or serialized asset, then validate with focused searches, YAML checks, and builds.

### Discovery checkpoint — 2026-11-01

- `Assets/Game/Content/Audio/Prefabs/AudioManager.prefab` contains `Cannon_Heavy_Shot` and `Cannon_Medium_Shot` in the `Common Robot Sounds` group, plus intentional per-robot copies under Iron Alliance and Nova Syndicate targets.
- `AudioEventNames.StandardShot` and `AudioEventNames.HeavyShot` must remain because `AudioEventSelector` still requests those event names for robot-specific lookup.
- `Assets/Editor/AudioResourcesEditor.cs` draws each `AudionElement` with `SerializedProperty.PropertyField` but has no per-element remove control; changing the array size cannot remove a middle element directly.
- The editor file and prefab already have pre-existing working-tree changes; only the requested common entries and list-editing behavior will be touched.

### Decision

- Remove only the two entries from the common prefab group, preserving all per-robot shot definitions.
- Add a `Remove` button beside each sound entry and delete through `SerializedProperty.DeleteArrayElementAtIndex`, stopping the current draw pass after deletion so the next inspector repaint renders the shifted list safely.

### Change checkpoint — 2026-11-01

- `Assets/Game/Content/Audio/Prefabs/AudioManager.prefab`: removed `Cannon_Heavy_Shot` and `Cannon_Medium_Shot` only from `Common Robot Sounds`; per-robot cannon entries remain.
- `Assets/Editor/AudioResourcesEditor.cs`: added a per-element `Remove` button to the shared `DrawElements` path, including robot sound lists, and stop drawing after deletion so middle-item removal cannot reuse stale serialized properties.

### Completion checkpoint — 2026-11-01

- **Result:** Shared robot sounds no longer provide generic cannon shots; shot events remain available only through the existing per-robot groups. The audio resource inspector now supports removing any sound entry, including entries from the middle of a list.
- **Validation:** Confirmed the common prefab group has neither cannon name; confirmed the prefab still has 2 heavy and 6 medium per-robot cannon entries; `dotnet build Assembly-CSharp.csproj --no-restore --verbosity quiet` passed with 0 errors; `dotnet build Assembly-CSharp-Editor.csproj --no-restore --verbosity quiet` passed with 0 errors; static editor-code checks found `Remove` and `DeleteArrayElementAtIndex`; targeted `git diff --check` passed.
- **Validation note:** An initial `rg` command had a shell quoting error; the corrected static checks passed and the error was unrelated to project code.
- **Remaining limitations:** Existing `System.Net.Http` and unrelated TMP obsolescence warnings remain. Manual Unity inspector interaction was not run in this environment.
- **Closed:** 2026-11-01

## Task: Restore sniper scope enter and exit sounds

- **Status:** IN_PROGRESS
- **Started:** 2026-11-01
- **User request:** Investigate why the zoom/scope sounds are not audible when entering and leaving the sniper scope, and fix the issue.
- **Scope:** Trace sniper scope enter/exit events from gameplay code through `SoundCaller`, `AudioEventNames`, audio resource lookup, and `AudioManager.prefab`; fix the focused cause without changing unrelated audio behavior.
- **Acceptance criteria:** Entering and exiting the sniper scope each trigger the intended audio event; the referenced events resolve to configured audio resources; runtime and editor assemblies compile.
- **Next steps:** Inventory scope transition call sites and event names, compare them with configured audio entries and playback guards, patch the owning audio/gameplay path, then validate with focused searches and builds.

### Discovery checkpoint — 2026-11-01

- `CameraController.ApplySniperUi` correctly triggers `SoundCaller.PlayUi(AudioEventNames.SniperEnter/Exit)` only when the sniper state changes.
- `SoundCaller.PlayUi` searches only `AudioResources.uiClips`, but `Combat_Sniper_Enter` and `Combat_Sniper_Exit` were serialized under `Common Robot Sounds` (`soundCategories`). The lookup therefore returned `null` and no audio preset was played.
- Changing the call to `PlaySfx` would make these feedback sounds 3D and position them at the audio root, which is incorrect for a local scope transition and could still make them inaudible away from the origin.

### Decision

- Move the two scope transition resources from `Common Robot Sounds` to `uiClips`, preserving their clip GUIDs and configured volumes. Keep `CameraController` on `PlayUi` so the sounds use the UI mixer/volume and non-spatial playback.

### Change checkpoint — 2026-11-01

- `Assets/Game/Content/Audio/Prefabs/AudioManager.prefab`: moved `Combat_Sniper_Enter` and `Combat_Sniper_Exit` from `Common Robot Sounds` to `uiClips`, preserving both clip GUIDs and volumes.
- `Assets/Game/Scripts/Gameplay/Robots/CameraController.cs`: no code change was needed; the existing `PlayUi` call now resolves the correctly classified resources.

### Completion checkpoint — 2026-11-01

- **Result:** Scope enter and exit sounds now resolve through the UI audio collection and play as non-spatial feedback sounds. The silence was caused by a resource-type mismatch: `PlayUi` searched `uiClips` while the events were stored in `soundCategories`.
- **Validation:** Confirmed each scope event appears exactly once in the prefab UI section and not in the SFX section; confirmed `CameraController` calls `PlayUi` with both events; `dotnet build Assembly-CSharp.csproj --no-restore --verbosity quiet` passed with 0 errors; `dotnet build Assembly-CSharp-Editor.csproj --no-restore --verbosity quiet` passed with 0 errors; targeted `git diff --check` passed.
- **Remaining limitations:** Existing `System.Net.Http` reference warnings remain. Manual Unity play-mode audio audition was not run in this environment.
- **Closed:** 2026-11-01

## Task: Match sniper reticle opacity slider to project UI style

- **Status:** COMPLETED
- **Started:** 2026-11-01
- **User request:** Make the settings slider for the alternate reticle opacity use exactly the same visual style as the other project sliders, and remember that UI changes must follow the existing project style.
- **Scope:** Identify the reticle opacity slider in the settings UI, compare its serialized hierarchy and visual properties with neighboring settings sliders, update only the inconsistent component/style references, and persist the UI consistency preference in shared Codex memory.
- **Acceptance criteria:** The reticle opacity slider uses the same track, fill, handle, colors, dimensions, layout, and interaction styling as the established settings sliders; unrelated settings behavior remains unchanged; project assemblies compile; the standing UI-style preference is recorded in `G:\AI\Codex_Memory`.
- **Next steps:** Locate the settings slider and matching slider templates/components, inspect the relevant prefab/scene YAML and scripts, patch the smallest style difference, record the preference, then validate with focused asset checks and builds.

### Discovery checkpoint — 2026-11-01

- `GeneralTabView.EnsureReticleOpacitySlider()` creates a one-off runtime slider with a plain dark `Image`, flat fill color, white handle, different dimensions, and a manually created label.
- The same `Client.unity` settings UI already contains `GameplayMouseSensitivitySlider` and `SniperMouseSensitivitySlider` with the project slider hierarchy, sprites, handle/fill styling, transition colors, and matching label treatment.
- The opacity behavior and persistence are already correct; only the visual construction is inconsistent.

### Decision

- Instantiate the existing `GameplayMouseSensitivitySlider` style as the Reticle Opacity slider and clone its matching label style, then override only the object names, label text, range, value, and placement needed for the General tab.

### Change checkpoint — 2026-11-01

- `Assets/Game/Scripts/UI/Settings/GeneralTabView.cs`: the reticle opacity slider now clones the existing gameplay sensitivity slider and its label, preserving the project’s hierarchy, sprites, colors, dimensions, and interaction styling; a functional fallback remains only for a missing style source.
- `G:\AI\Codex_Memory\AGENTS.md`: added a standing requirement to reuse the project UI style for all future UI changes.
- `G:\AI\Codex_Memory\HISTORY.md`: recorded the new UI style preference.

### Completion checkpoint — 2026-11-01

- Validation: `dotnet build Assembly-CSharp.csproj --no-restore --verbosity quiet` passed with 0 errors; `dotnet build Assembly-CSharp-Editor.csproj --no-restore --verbosity quiet` passed with 0 errors.
- Validation: `git diff --check` passed for the changed project files; static references confirm the canonical slider and label exist in `Client.unity`.
- Result: the reticle opacity slider follows the established settings slider style on the normal runtime path. The only remaining limitation is that Unity Editor visual inspection was not run in this session.

## Task: Expose reticle opacity slider in Settings hierarchy

- **Status:** COMPLETED
- **Started:** 2026-11-01
- **User request:** Make the reticle opacity slider visible in the Settings scene hierarchy so its height can be adjusted manually.
- **Scope:** Convert the runtime-only reticle opacity control into a serialized scene object using the established slider style, keep its existing behavior and persistence, and remove only the runtime construction that hides it from the Inspector.
- **Acceptance criteria:** A `ReticleOpacitySlider` object is present under the Settings/General hierarchy and assigned to `GeneralTabView`; its height can be edited in the Inspector; runtime initialization does not create duplicate controls; opacity behavior and UI style remain unchanged; project assemblies compile.
- **Next steps:** Inspect the General hierarchy and serialized field, add the styled slider and label to `Client.unity`, simplify initialization to use the assigned control, then validate scene references and builds.

### Change checkpoint — 2026-11-01

- `Assets/Game/Scenes/Client.unity`: added a serialized `ReticleOpacitySlider` and `ReticleOpacitySlider_Label` under `General`, including the full project-style background, fill, handle hierarchy; assigned the slider to `GeneralTabView`.
- `Assets/Game/Scripts/UI/Settings/GeneralTabView.cs`: removed runtime slider/label construction so the Inspector-controlled scene object is the only control.
- Static validation: all new scene fileIDs are unique; Slider target/fill/handle references and General hierarchy references resolve; `git diff --check` passes.

### Completion checkpoint — 2026-11-01

- Result: select `Client > Settings > General > ReticleOpacitySlider` in the scene hierarchy and edit its `RectTransform` height directly; the label is next to it as `ReticleOpacitySlider_Label`.
- Validation: `dotnet build Assembly-CSharp.csproj --no-restore --verbosity quiet` and `dotnet build Assembly-CSharp-Editor.csproj --no-restore --verbosity quiet` passed with 0 errors; static scene fileID/reference checks passed; `git diff --check` passed.
- Remaining limitation: Unity batch scene validation was not run because another Unity instance already had this project open. Existing reference/obsolescence warnings remain.

## Task: Investigate explosion-like sound on disconnect

- **Status:** COMPLETED
- **Started:** 2026-11-01
- **User request:** Investigate the incorrect explosion-like noise heard when pressing Disconnect, including whether a tank destruction sound is triggered during teardown, and fix the bug.
- **Scope:** Trace the Disconnect UI/action through connection teardown, vehicle destruction/despawn, and audio event calls; identify the unintended sound path and make the smallest focused fix without suppressing legitimate gameplay destruction audio.
- **Acceptance criteria:** Disconnect no longer emits an unintended explosion/destruction sound; legitimate tank destruction still plays its configured audio; disconnect lifecycle and audio assemblies compile.
- **Next steps:** Search Disconnect call sites and teardown handlers, trace all destruction/explosion sound calls and guards, patch the owning lifecycle path, then validate with focused static checks and builds.

### Discovery checkpoint — 2026-11-01

- `PauseMenu.DisconnectButtonPressed()` calls `GameplaySpawner.ReturnToMainMenu()` and the server-side `AbandonPlayer()` path.
- `AbandonPlayer()` calls `VehicleHealth.ServerKill()`, which currently broadcasts `DiedObserversRpc(-1)` just like a real combat death.
- `DiedObserversRpc()` invokes `VehicleHealth.OnClientDeath`; the owner `CameraController` responds by playing `AudioEventNames.OwnRobotDestroyed`, which is the explosion-like sound heard during disconnect.

### Decision

- Extend the server kill path with an explicit observer-presentation flag and disable the death observer RPC for disconnect cleanup only. Keep the existing default behavior for real combat kills so legitimate destruction audio and death presentation remain unchanged.

### Change checkpoint — 2026-11-01

- `Assets/Game/Scripts/Gameplay/Robots/VehicleHealth.cs`: added an optional `broadcastDeathPresentation` flag to `ServerKill`/`DeathServer`; disconnect cleanup can now kill and despawn the vehicle without broadcasting client death presentation.
- `Assets/Game/Scripts/Networking/Lobby/GameplaySpawner.cs`: calls `ServerKill(false)` from `AbandonPlayer`, while combat damage still uses the default broadcast behavior.

### Completion checkpoint — 2026-11-01

- Result: disconnect cleanup no longer sends `DiedObserversRpc`, so the local `CameraController` cannot play `Combat_Own_Robot_Destroyed` or start the death spectator sequence. Legitimate combat kills still broadcast the existing death presentation and destruction audio.
- Validation: targeted search confirmed the disconnect-only `ServerKill(false)` call and default combat death path; `dotnet build Assembly-CSharp.csproj --no-restore --verbosity quiet` passed with 0 errors; `dotnet build Assembly-CSharp-Editor.csproj --no-restore --verbosity quiet` passed with 0 errors; targeted `git diff --check` passed.
- Remaining limitation: manual Unity play-mode reproduction was not run because another Unity instance already had the project open.

## Task: Locate UI_Battle_Countdown usage

- **Status:** COMPLETED
- **Started:** 2026-11-01
- **User request:** Identify where the `UI_Battle_Countdown` sound is currently used.
- **Scope:** Find code call sites, event-name definitions, and serialized audio resource entries without changing project files.
- **Acceptance criteria:** Report the exact runtime call site(s) and the configured audio resource location.
- **Next steps:** Search the project for the event name and its related countdown flow, then close the journal entry with the findings.

### Completion checkpoint — 2026-11-01

- Result: `UI_Battle_Countdown` is triggered by `SoundCaller.StartBattleAudio()` when the Gameplay HUD opens; `LobbyManager.TargetStartGameResponseRpc()` starts that battle audio after matchmaking succeeds. The event is resolved through `SoundCaller.PlayUi()`.
- Resource: the configured UI audio entry is in `Assets/Game/Content/Audio/Prefabs/AudioManager.prefab` with volume `0.2` and clip GUID `86a807ce7062d9743893cd1c1bd9d2b6`.
- Validation: project-wide search found one direct playback call, one event-name constant, and one serialized resource entry; no files besides `TASK.md` were changed.

## Task: Remove UI_Battle_Countdown playback

- **Status:** COMPLETED
- **Started:** 2026-11-01
- **User request:** Remove `UI_Battle_Countdown` from the battle audio startup location identified in the previous task.
- **Scope:** Remove only the `PlayUi(AudioEventNames.UiBattleCountdown)` call from `SoundCaller.StartBattleAudio()`, preserve battle music and all other UI/audio events, then validate the runtime and editor assemblies.
- **Acceptance criteria:** Starting battle audio no longer plays `UI_Battle_Countdown`; battle music and unrelated audio calls remain unchanged; assemblies compile.
- **Next steps:** Patch the single startup call, verify no direct playback remains, build runtime/editor assemblies, and close the journal entry.

### Change checkpoint — 2026-11-01

- `Assets/Game/Scripts/Audio/SoundCaller.cs`: removed only `PlayUi(AudioEventNames.UiBattleCountdown)` from `StartBattleAudio()`; `PlayMusic(AudioEventNames.BattleMusic)` remains unchanged.

### Completion checkpoint — 2026-11-01

- Result: battle startup no longer directly plays `UI_Battle_Countdown`; the serialized resource and constant remain available for future intentional use.
- Validation: no direct `PlayUi(AudioEventNames.UiBattleCountdown)` call remains; runtime and editor assemblies passed with 0 errors; targeted `git diff --check` passed.
- Remaining limitation: manual Unity play-mode audio audition was not run in this session.

## Task: Implement pre-battle countdown and gameplay lock

- **Status:** COMPLETED
- **Started:** 2026-11-01
- **User request:** Add a separate gameplay countdown UI and logic: 10-second countdown, play `UI_Battle_Countdown` on each tick, start 0.5 seconds after the loading screen is hidden, show animated large numbers, block robot movement and firing while allowing turret rotation.
- **Scope:** Trace the loading lifecycle and movement/firing owners, then implement focused countdown state, network authority, UI presentation, and scene wiring without changing unrelated audio or menu behavior.
- **Acceptance criteria:** Countdown starts after the loading screen is hidden plus 0.5 seconds; displays `10` through `1` clearly with animation; plays the countdown sound once per number; server and clients block movement and firing for all robots while preserving turret aiming; the feature is split into maintainable components and compiles.
- **Next steps:** Implement the focused gate, network authority, countdown controller/view, and scene/prefab wiring; then validate source references, YAML integrity, and runtime/editor builds.

### Discovery checkpoint — 2026-11-01

- `MenuLoadingScreenManager` hides the scene loading menu through `HideLoading()` after the minimum loading presentation; a dedicated scene-loading-hidden event can avoid triggering the countdown during disconnect cleanup.
- Robot movement is server-authoritative in `VehicleMovementController`; player and bot inputs converge through `VehicleInputController`, while firing must also be guarded at `WeaponReloadController`/`NetworkWeaponShooter` to reject stale or direct shot requests.
- `GameplayTimerNetwork.prefab` is the existing per-match network object and can host a separate countdown authority component; `GameplayHUD` already contains the gameplay UI canvas and can receive a dedicated countdown child using the existing Roboto Condensed/TMP style.

### Implementation checkpoint — 2026-11-01

- Added `BattleCountdownGate`, `BattleCountdownController`, `BattleCountdownView`, and `BattleCountdownAuthority` as separate owners for lock state, lifecycle/timing, animation, and network completion.
- Added a `SceneLoadingHidden` event that is raised only by the normal scene-loading flow, so disconnect cleanup cannot accidentally start another countdown.
- Added the countdown UI under `GameplayHudContent` with the project TMP font/color, large centered numbers, scale/alpha animation, and serialized `0.5` second start delay.
- Added the authority component to `GameplayTimerNetwork.prefab`; the server room lock now starts before robot spawn and releases only after connected real players report completion. Movement, hull rotation, firing prediction, owner shot RPCs, bot shots, and authoritative movement all consume the gate while turret aim remains enabled.
- Initial runtime/editor builds exposed a `Player` namespace collision in the authority; this was resolved with an explicit lobby-player alias.

### Completion checkpoint — 2026-11-01

- Result: pre-battle countdown is wired into the gameplay HUD, starts 0.5 seconds after the normal loading screen is hidden, displays animated `10` through `1`, plays `UI_Battle_Countdown` once per tick, and releases movement/firing while preserving turret rotation.
- Validation: runtime and editor assemblies both built successfully with 0 errors; targeted `git diff --check` passed; temporary generated project-file entries were removed.
- Remaining limitation: Unity Play Mode was not run in this session because the project was already open in another Unity instance.

## Task: Safely repair failed commit push

- **Status:** COMPLETED
- **Started:** 2026-11-01
- **Completed:** 2026-11-01
- **User request:** Diagnose the Git error shown in the attached screenshot, fix the cause, and push the commit without losing any work or breaking the project.
- **Scope:** Inspect the screenshot, repository working tree, branch/upstream/remote state, and commit/push error context; preserve all existing user changes; make only the minimum safe Git or project changes required; validate before pushing.
- **Acceptance criteria:** The cause of the push/commit failure is identified; no uncommitted or untracked user work is lost; any required fix is narrowly applied; the intended commit is pushed successfully if the remote permits it; validation and any remaining limitation are recorded here.
- **Safety constraints:** Do not use destructive reset/clean/checkout operations, do not overwrite unrelated changes, do not force-push, and create no commit unless the user’s existing commit state and intended contents are verified first.

### Next steps

- Inspect the attached error and current Git state without mutation.
- Preserve and, if needed, snapshot all local work before repair.
- Apply the smallest non-destructive fix and validate repository state.
- Push only the verified branch using a non-force push, then close this entry.

### Discovery checkpoint — 2026-11-01

- The screenshot shows GitHub rejecting `Assets/Resources/GameAudio/Music/Garage_DevelopmentTree_Music.wav` because the committed file is about 199 MB, above GitHub's 100 MB regular-blob limit.
- `main` is one commit ahead of `origin/main`; the rejected file is in local `HEAD` commit `e84851c6` and is not present in the remote commit.
- The working tree contains unrelated/uncommitted Unity countdown changes and journal changes; they must remain untouched.
- Git LFS `3.6.0` is installed, but `.gitattributes` is currently absent. The repository therefore needs a non-destructive LFS conversion or equivalent history correction before a normal push.
- The committed blob is `208,667,740` bytes, while the current working-tree file reports `740` bytes; this representation must be inspected and preserved before staging any replacement.

### Repair checkpoint — 2026-11-01

- Created safety artifacts outside the repository at `C:\Users\TorasPC\AppData\Local\Temp\war-of-machines-git-safety-20261101`: a backup branch, Git bundle, tracked-worktree diff, status snapshot, and untracked-file archive.
- Added `.gitattributes` for only the oversized WAV, restaged it as a Git LFS pointer, and amended the local unpushed `finish main sound` commit from `e84851c6` to `5f09a5c9`.
- Confirmed the local WAV remains `208,667,740` bytes, the committed representation is a `134`-byte LFS pointer, `git lfs fsck` is `OK`, and no committed blob exceeds 100 MB.
- First normal push attempt failed before object transfer because the configured GitHub SSH remote returned `Permission denied (publickey)`.

### Completion checkpoint — 2026-11-01

- **Status:** COMPLETED
- **Result:** Repaired the rejected local `finish main sound` commit by storing only `Assets/Resources/GameAudio/Music/Garage_DevelopmentTree_Music.wav` in Git LFS through `.gitattributes`; the local full-size WAV was preserved.
- **Pushed commit:** `5f09a5c9ccf0b670e02799cf2a1d051b090b010c` is now present on `origin/main`.
- **Validation:** `HEAD` equals `origin/main`; the remote branch points to the repaired commit; `git lfs fsck` is `OK`; no committed blob exceeds 100 MB; no staged changes remain.
- **Preservation:** Existing modified and untracked Unity countdown files remain in the working tree and were not included in the amended commit. A local backup branch and external recovery artifacts remain at `C:\Users\TorasPC\AppData\Local\Temp\war-of-machines-git-safety-20261101`.
- **Remaining limitations:** The working tree is intentionally not clean because the user's pre-existing Unity changes were preserved rather than committed or reverted. The backup branch is intentionally retained until the user confirms it is no longer needed.

## Task: Lock controls after robot landing

- **Status:** COMPLETED
- **Started:** 2026-11-01
- **User request:** Change the pre-battle flow so robots land first and only then become locked by the countdown; they must not remain suspended in the air while the countdown lock is active.
- **Scope:** Inspect robot spawn/landing and countdown ownership, then delay the movement/firing lock until the robots have completed their landing while preserving the existing countdown UI and turret-aim behavior.
- **Acceptance criteria:** Robots are allowed to descend/settle after spawn; the movement/firing lock begins only after landing is complete; the countdown remains synchronized and does not permit movement or firing afterward; the change uses a focused owner and compiles without unrelated regressions.
- **Next steps:** Identify the robot landing signal or physics settling state, wire it into the countdown gate/authority, and validate runtime/editor builds plus source/YAML integrity.

### Discovery and implementation checkpoint — 2026-11-01

- Root cause: `VehicleMovementController.SimulateMovement` returned before calling `CharacterController.Move` whenever the countdown gate was active, so gravity never moved a newly spawned robot toward the ground.
- Decision: keep the existing movement controller as the owner of `CharacterController` gravity and add a focused gravity-only landing path under the countdown gate. It clears horizontal speed and does not rotate the hull; weapon classes remain blocked and turret aim remains available.
- Changed `Assets/Game/Scripts/Gameplay/Robots/VehicleMovementController.cs` and documented the ownership boundary in `docs/codex-game/battle-countdown.md`.
- Next steps: build runtime/editor assemblies, run source/YAML checks, then close the task with validation results.

### Completion checkpoint — 2026-11-01

- Result: countdown-gated robots now execute a gravity-only `CharacterController.Move` pass, so they descend and settle on the map instead of hanging in the air. Horizontal movement, hull rotation, and firing remain blocked; turret aiming is unchanged.
- Validation: runtime and editor assemblies both built successfully with 0 errors; targeted `git diff --check` passed for the implementation and documentation files.
- Remaining limitation: Unity Play Mode was not run because the project is already open in another Unity instance. Existing unrelated Unity-generated working-tree changes were preserved.

## Task: Fade Back and Back2 with countdown reticle

- **Status:** COMPLETED
- **Started:** 2026-11-01
- **User request:** Include `Back` and `Back2` in the same countdown hide/reveal behavior as the reticle.
- **Scope:** Extend the existing reticle presentation transition to the `Back`/`Back2` CanvasGroups while preserving the user-configured reticle opacity and lifecycle resets.
- **Acceptance criteria:** `Back` and `Back2` are alpha 0 during countdown; they reveal over exactly 2 unscaled seconds after countdown; opacity slider value is still respected; runtime/editor assemblies compile.
- **Result:** Wired the existing runtime-resolved `Back`/`Back2` opacity CanvasGroups into the reticle countdown transition. They hide immediately, reveal over two unscaled seconds, and retain the configured reticle opacity.
- **Validation:** `Assembly-CSharp.csproj` and `Assembly-CSharp-Editor.csproj` both build with 0 errors; targeted `git diff --check` passed.
- **Remaining limitation:** Unity Play Mode was not run because the project is already open in another Unity instance. Existing unrelated Unity-generated working-tree changes were preserved.

## Task: Add runtime distance settings for robot sounds

- **Status:** COMPLETED
- **Started:** 2026-11-01
- **User request:** Add runtime distance control for robot sounds because distant robots currently sound as if they are nearby.
- **Scope:** Inspect the audio playback and mixer/spatialization ownership, add a focused runtime distance setting for robot/world sounds, and preserve UI, music, and existing volume settings.
- **Acceptance criteria:** Robot sounds attenuate with distance in gameplay; the distance behavior is configurable at runtime; the setting does not change UI/music routing; runtime/editor assemblies compile.
- **Result:** Added live SFX spatial settings for minimum distance, maximum distance, and rolloff mode. The default changed from linear to logarithmic attenuation, while UI/music remain non-positional.
- **Validation:** `Assembly-CSharp.csproj` and `Assembly-CSharp-Editor.csproj` both build with 0 errors; targeted `git diff --check` passed for all changed implementation, prefab, documentation, and journal files.
- **Remaining limitation:** Unity Play Mode audio listening was not run because the project is already open in another Unity instance; the runtime behavior is covered by source inspection and successful compilation.

### Implementation checkpoint — 2026-11-01

- `AudioPreset` now applies runtime `minDistance`, `maxDistance`, and `AudioRolloffMode` values to its `AudioSource`.
- `AudioManager` stores the active SFX spatial configuration, applies it to new sounds, and updates already pooled SFX when the configuration changes.
- `SoundCaller` exposes live-editable `sfxMinDistance`, `sfxMaxDistance`, and `sfxRolloffMode` fields plus `SetRuntimeSfxSpatialSettings(...)`; runtime changes are detected without affecting UI/music.
- Updated `AudioManager.prefab`, `AudioPreset.prefab`, and `docs/codex-game/audio-presentation.md` to make the logarithmic default and ownership explicit.

### Completion checkpoint — 2026-11-01

- Runtime and editor builds passed with 0 errors.
- Targeted diff check passed. Existing unrelated working-tree changes were preserved.

### Discovery checkpoint — 2026-11-01

- `SoundCaller` routes robot effects through `AudioManager.PlaySfx`; `AudioPreset.SetType` already makes SFX fully spatial and robot callers provide either a robot parent or a world position.
- `AudioPreset.prefab` currently uses `MinDistance: 1`, `MaxDistance: 100`, and `rolloffMode: 1`, which is Unity's linear rolloff. This keeps distant sources too loud over most of the range.
- Decision: keep UI/music non-positional, add live-editable SFX spatial settings at `SoundCaller`, and have `AudioManager` apply them to active and future SFX sources. Use logarithmic rolloff by default while preserving the existing 1–100 metre range.
- Next steps: implement runtime synchronization, update the audio prefab and ownership documentation, then build and run targeted checks.

### Discovery and implementation checkpoint — 2026-11-01

- `Back` and `Back2` are standalone `CanvasGroup` objects under the faction crosshair roots, not children of the main reticle transition groups.
- `GunCrosshair` now tracks them separately, applies countdown alpha directly, and multiplies it by the saved slider opacity so the setting is not overwritten.

### Completion checkpoint — 2026-11-01

- Runtime and editor builds passed with 0 errors.
- Targeted diff check passed. Play Mode remains unverified because Unity is already open in another instance.

## Task: Hide and fade the reticle during battle countdown

- **Status:** COMPLETED
- **Started:** 2026-11-01
- **User request:** Hide the aiming reticle while the pre-battle countdown is running, then reveal it smoothly over 2 seconds after the countdown ends.
- **Scope:** Identify the reticle presentation owner and countdown lifecycle, add a focused visibility transition without changing reticle aiming behavior, and preserve teardown/menu behavior.
- **Acceptance criteria:** The reticle is fully hidden during countdown; it remains hidden until countdown completion; it fades in over exactly 2 seconds using unscaled time; it resets safely when the gameplay HUD is disabled or a new countdown starts; runtime/editor assemblies compile.
- **Next steps:** Trace reticle presentation and countdown state, implement the transition at the presentation boundary, document ownership, and validate builds and targeted diffs.

### Discovery and implementation checkpoint — 2026-11-01

- `GunCrosshair` owns the central faction reticle group, including the camera crosshair and server reticle; `SniperScopeOverlay` owns the zoom overlay separately.
- `BattleCountdownGate` now emits a client countdown state event. Both presentation owners subscribe to it, hide immediately at countdown start, and reveal with unscaled alpha interpolation over exactly 2 seconds after countdown completion.
- The implementation preserves aiming/zoom state and resets presentation safely on disable; the ownership boundary is documented in `docs/codex-game/battle-countdown.md`.
- Next steps: validate runtime/editor assemblies and targeted whitespace/source checks, then close the task.

### Completion checkpoint — 2026-11-01

- Result: central reticle visuals and sniper scope overlay are hidden for the entire countdown and fade in from alpha 0 to 1 over exactly 2 seconds after countdown completion. The transition uses unscaled time and does not alter aiming or zoom state.
- Validation: runtime and editor assemblies both built successfully with 0 errors; targeted `git diff --check` passed for all implementation, documentation, and journal files.
- Remaining limitation: Unity Play Mode was not run because the project is already open in another Unity instance. Existing unrelated Unity-generated working-tree changes were preserved.

## Task: Tune robot SFX distance for the active map scale

- **Status:** COMPLETED
- **Started:** 2026-11-01
- **User request:** Tune the robot sound-distance parameters using the active map size and robot dimensions so distant robots do not sound nearby.
- **Scope:** Use measured map bounds, spawn separation, and representative robot controller dimensions to choose realistic default SFX distances without changing the runtime distance feature or UI/music routing.
- **Acceptance criteria:** Tuned `minDistance` and `maxDistance` values are applied consistently in code and prefabs; the values are documented with their scale rationale; runtime/editor assemblies compile.
- **Result:** Tuned the active-map defaults to `minDistance = 2 m` and `maxDistance = 50 m` with logarithmic rolloff. The values are applied in code and both audio prefabs.
- **Validation:** `Assembly-CSharp.csproj` and `Assembly-CSharp-Editor.csproj` both build with 0 errors; targeted `git diff --check` passed for the code, prefabs, documentation, and journal.

### Scale-tuning checkpoint — 2026-11-01

- Active `Map` waypoint bounds measure approximately `120.96 m` wide by `72.47 m` deep; Team A and Team B spawn roots are approximately `100.8 m` apart.
- Representative robot `CharacterController` dimensions range from radius `0.46–0.70 m` and height `1.0–1.56 m`, giving an effective robot width of roughly `1–1.4 m`.
- `2 m` keeps the full-volume zone close to the robot body, while `50 m` covers local combat without making robots on the opposite side of the map sound nearby.

### Implementation checkpoint — 2026-11-01

- Updated `AudioManager` defaults, `AudioManager.prefab`, `AudioPreset.prefab`, and `docs/codex-game/audio-presentation.md` to the measured range.
- The live runtime controls and logarithmic rolloff behavior remain unchanged.

### Completion checkpoint — 2026-11-01

- Runtime and editor builds passed with 0 errors.
- Targeted diff check passed. Unity Play Mode audio listening remains unverified because the project is already open in another Unity instance.

## Task: Diagnose intermittent missing bot projectile visuals

- **Status:** IN_PROGRESS
- **Started:** 2026-11-01
- **User request:** Analyze why bot shots, especially enemy shots, often have no visible projectile flight; determine the real root cause and provide three visually strong, non-breaking solution options.
- **Scope:** Trace bot firing, network delivery/deduplication, projectile spawn and lifetime, camera/visibility, pooling or destruction, and relevant timing/configuration. Do not modify implementation unless explicitly requested; provide evidence-based alternatives with trade-offs.
- **Acceptance criteria:** Identify the most likely concrete failure path(s) from the repository code; distinguish confirmed facts from hypotheses; propose three viable fixes that preserve gameplay authority and improve visual reliability; record validation and remaining uncertainty.
- **Next steps:** Inspect repository state and applicable projectile/bot/network scripts, follow the shot event from bot decision to observer rendering, then close this analysis entry with findings and validation.

### Discovery checkpoint — 2026-11-01

- Confirmed bot firing is server-authoritative: `BotCombatController` calls `WeaponReloadController.ServerTryFireAuthoritative()`, which calls `NetworkWeaponShooter.ServerFireAuthoritative()`; bots have no client-side prediction path.
- Confirmed remote presentation uses two separate observer RPCs: `FireObserversRpc` spawns the projectile and `ResolveObserversProjectileTargetRpc` ends it or shows impact FX. `_observedResolvedHitShots` explicitly handles a resolution arriving before the fire event by skipping the projectile spawn, which is a direct visual-loss path.
- Confirmed remote projectiles are backdated by up to `MAX_PASSED_TIME = 0.30s` before local visual collision is enabled. If the shell has already reached or crossed a nearby target, `Projectile.TryCastEmbeddedCollision` immediately stops and hides it before a frame can show the flight.
- Confirmed active robot prefabs reference `ProjectileVisual.prefab` for clients and `ServerProjectile.prefab` for authority. The pool never drops a projectile when exhausted; it instantiates overflow objects, so pool capacity is not the primary cause.
- Confirmed enemy renderer visibility is separate from projectile RPC delivery; hidden enemy meshes do not disable the projectile presentation path. Current evidence therefore points to remote timing/catch-up plus fire/resolve ordering, not bot aim or prefab absence.
- **Decision pending:** choose a fix that preserves server hit authority while guaranteeing a minimum observer-visible flight window without creating duplicate impacts or changing damage timing.

### Analysis checkpoint — 2026-11-01

- The remote catch-up is especially dangerous when runtime shell speeds are high: the repository's offline vehicle test data uses `800 + level * 25 m/s`, so the current `0.30s` cap can advance a visual shell by roughly `240–300m`, greater than the measured active-map span. Even with the normal prefab fallback of `70 m/s`, close-range shots can be advanced to the target before the first rendered frame.
- The current client visual settings use a `0.06s` tracer lifetime and a low-alpha fade, which can make a surviving remote shell visually weak, but this is secondary to the timing paths that skip or immediately hide the projectile.
- Recommended direction for implementation: keep server collision/damage unchanged, move remote presentation into a focused per-shot state owner, cap or remove remote catch-up, suppress local remote collision, and guarantee a short minimum visible flight before applying the authoritative impact presentation. Preserve a buffered resolved-shot record instead of discarding the fire event when RPC order reverses.

### Completion checkpoint — 2026-11-01

- **Status:** COMPLETED
- **Result:** Identified the concrete root cause as a remote-projectile presentation race/timeline error, not bot firing failure, missing prefab, or pool exhaustion. Enemy shots use the non-predicted observer path; that path backdates the projectile by up to `0.30s`, enables local collision immediately, and can hide it before rendering. A separate resolve RPC can also arrive before the fire RPC; the current handler records that state and intentionally skips the projectile spawn.
- **Validation:** Traced bot input through `BotCombatController`, `WeaponReloadController`, `NetworkWeaponShooter`, `ProjectileVisualSpawner`, `Projectile`, and `ProjectileRuntimePool`; inspected active robot prefab references and client projectile settings; confirmed no implementation files were modified; `git diff --check -- TASK.md` passed.
- **Remaining limitations:** No live multiplayer capture or Unity Play Mode reproduction was run, so the exact share of failures caused by catch-up versus RPC ordering still needs runtime counters/logging. Existing unrelated font modifications remain untouched.

## Task: Implement option 1 for reliable remote bot projectiles

- **Status:** IN_PROGRESS
- **Started:** 2026-11-01
- **User request:** Implement the first, minimal and safest fix for missing bot projectile flight; preserve the option to expand the solution into the buffered per-shot presentation owner later.
- **Scope:** Keep server collision, damage, reload, and authoritative hit timing unchanged. Adjust only remote projectile presentation so catch-up cannot skip the visible launch, local remote collision cannot hide it before the first frame, and early authoritative resolution does not discard the visual shot. Improve tracer readability only as needed.
- **Acceptance criteria:** Remote bot shots always receive a short visible flight window when their observer fire event is delivered; server gameplay remains authoritative; owner prediction and local collision behavior remain unchanged; lifecycle/despawn paths do not leak pooled projectiles; focused tests or build/source validation pass; document the ownership boundary and remaining runtime limitation.
- **Next steps:** Inspect current projectile APIs and tests, implement the focused remote-presentation timing change, update mechanic documentation, then build and validate without touching unrelated working-tree changes.

### Implementation checkpoint — 2026-11-01

- Added a remote-only observer presentation path in `NetworkWeaponShooter`: observer shells start at `passedTime = 0`, skip client visual collision, and retain an authoritative hit/miss record when the resolve RPC arrives before the fire RPC.
- Added a short `0.12s` minimum remote visual flight in `Projectile`; owner prediction still uses the existing immediate impact path, while server projectile simulation and damage timing are unchanged.
- Added `RemoteImpactWaitsForMinimumVisibleFlight` to `CloseRangeProjectilePlayModeTests` and documented the ownership boundary in `docs/codex-game/weapons-damage.md`.
- **Next steps:** Run source/build validation, inspect the final diff, then close this task with results and any runtime verification limitation.

### Completion checkpoint — 2026-11-01

- **Status:** COMPLETED
- **Result:** Implemented option 1. Remote observer shots now spawn from the live muzzle, bypass local remote collision, preserve early authoritative hit/miss resolution, and keep confirmed hits visible for `0.12s` before impact. Owner prediction and authoritative server collision/damage/reload timing remain unchanged.
- **Validation:** `dotnet build Assembly-CSharp.csproj --no-restore`, `dotnet build Assembly-CSharp-Editor.csproj --no-restore`, and `dotnet build Game.PlayMode.Tests.csproj --no-restore` passed with 0 errors; targeted `git diff --check` passed. Added a focused Play Mode regression test, but did not run Unity Play Mode in this session.
- **Remaining limitations:** No live multiplayer capture was run, so the final visual quality still needs an in-game bot-vs-player check. Existing unrelated font modifications remain untouched.

## Task: Зібрати 15 звуків вибухів техніки й споруд

- **Status:** IN_PROGRESS
- **Started:** 2026-11-01
- **User request:** Знайти загалом 15 звукових файлів із вибухами, зокрема вибухами автомобіля, техніки, танка, будинку та спорідненими ефектами, і зберегти їх на Робочому столі.
- **Scope:** Підготувати окрему папку лише з 15 аудіофайлами. Додатково зберегти таблицю джерел і ліцензій поруч, щоб її не рахувати як аудіофайл і не класти в sound-folder.
- **Acceptance criteria:** 15 валідних, різних, не порожніх аудіофайлів; різноманітні типи руйнувань/вибухів; джерела, автори, ліцензії, атрибуція і примітка щодо відповідності записані; окрема папка на Desktop; перевірені кількість і SHA-256.
- **Destination plan:** C:\Users\TorasPC\Desktop\WarOfMachines_Explosion_Sounds
- **Completion checkpoint — 2026-11-01:** COMPLETED. На Робочому столі збережено рівно 15 різних PCM WAV: 2 великі вибухи, окремий вибух автомобіля з металевими уламками й сигналізацією, по 3 динамітні вибухи з камінням, гранати, мінометні влучання та обвалення споруд. 14 файлів — CC0 1.0 від Kaino; автомобільний ефект — CC BY 4.0 від BudgetPixel AI, обов’язкова атрибуція записана в таблиці. Усі WAV валідні: 14 файлів 48 kHz / mono / 24-bit і 1 файл 44.1 kHz / stereo / 16-bit; тривалість 3.31–4.00 s. SHA-256 унікальні, збігів із попередніми наборами v6 та Replacement Options немає. Soundza-кандидати з непогодженими умовами збережені в робочому архіві й не включені.

## Task: Build reusable cannon muzzle, looping fire, and ground-impact particle variants

- **Status:** COMPLETED

- **Started:** 2026-11-01
- **User request:** Study the realistic robot destruction particle effects in `Assets/PartDone/RobotDestroyEffect`, then create five cannon muzzle-shot variants, five looping fire variants, and five ground-impact variants inspired by the same assets and save them under `Assets/PartDone/New10000000parts`.
- **Scope:** Inspect the source particle systems, materials, textures, prefabs, and project rendering setup; preserve all source assets; create a self-contained new folder with high-quality reusable Unity particle prefabs/assets and supporting documentation if needed.
- **Acceptance criteria:** Fifteen distinct effect variants exist in the new folder, grouped by purpose, use the same compatible asset/material language as the source effects, are structurally valid Unity assets, and are validated by project-aware checks. No existing scene, prefab, or gameplay integration is changed unless explicitly required.
- **Next steps:** None.

### Discovery checkpoint — 2026-11-01

- The source pack contains seven reusable URP particle materials: three detailed Explosion materials and four free flipbook materials. The existing destruction prefabs layer burst flash, smoke, sparks, stretch rendering, custom vertex streams, particle lights, and force-field helpers.
- The project uses Unity `6000.3.13f1` with URP `17.3.0`. The requested `WFX_BImpact Concrete + Hole Unlit` reference is present, but the new ground impacts will use locally copied RobotDestroyEffect materials, with dust/debris/scorch layers built from those assets rather than modifying the WFX prefab.
- Decision: generate fifteen standalone prefabs through a temporary Unity batch project because the user's open Unity Editor currently locks the main project. Copy only the generated folder and its `.meta` files back; do not alter scenes, existing prefabs, or source materials.
- The first batch attempt was blocked by Unity's project lock before importing any generated assets. The editor generator is present at `Assets/Editor/GenerateNew10000000Parts.cs`; next step is to run it in the isolated temporary project, inspect the Unity import log, then transfer and validate the generated assets.

### Generation checkpoint — 2026-11-01

- The isolated Unity `6000.3.13f1` batch project generated all 15 requested prefabs successfully and imported seven local copies of the source explosion materials. The batch log ends with `Generated 15 particle prefabs in Assets/PartDone/New10000000parts` and exit code `0`.
- Generation did not touch the main project's scenes, prefabs, source destruction effects, or gameplay scripts. The next step is to copy only the generated folder and validate the serialized prefab contents in the main repository.

### Completion checkpoint — 2026-11-01

- **Result:** Added 15 reusable particle prefabs under `Assets/PartDone/New10000000parts`: five cannon muzzle shots, five looping/prewarmed fire effects, and five ground-impact effects with flash, dust, rising smoke, debris collision, sparks, and scorch layers. Added seven local copies of the source explosion materials and a README with spawn/orientation guidance.
- **Validation:** Isolated Unity batch import/generation completed with exit code `0`; final repository checks confirmed exactly 15 prefabs, 5 looping fire prefabs with 4 looping+prewarm systems each, 10 non-looping one-shot prefabs, 7 valid local material GUIDs, and `git diff --check` passed.
- **Remaining limitations:** I did not run live Play Mode or capture an in-game visual preview because the main Unity Editor was already open and locked the project. Existing source effects, scenes, gameplay prefabs, and gameplay scripts remain unchanged.

## Task: Import 1111111 prefabs and resources into Particles111

- **Status:** COMPLETED
- **Started:** 2026-11-02
- **User request:** Transfer the Unity assets from `C:\Users\TorasPC\Documents\Codex\2026-11-01\new-chat-3\work\UnityAssetScratch\Assets\1111111` into this game project together with their resources.
- **Scope:** Inspect the source asset bundle and the existing `Assets\Particles111` destination, preserve Unity `.meta` files and referenced local resources, avoid overwriting unrelated project files, and validate copied GUID references.
- **Acceptance criteria:** The requested assets and required resources are present in the intended project destination; source files remain unchanged; Unity metadata and local references are preserved or safely resolved; validation and any limitations are recorded here.
- **Working assumption:** Because the previous journaled import used `Assets\Particles111`, this request targets the same destination. If the user intended another destination, stop before copying and ask for clarification.

### Progress

- Confirmed the shared Codex memory, archive workflow index/history, task-journal workflow, and project journal were read before task-specific work.
- No dedicated Unity asset-transfer workflow exists in the archive; applying the journaled asset-import process from the prior `Fires` transfer.
- **Next steps:** Inspect the existing destination and dependency closure, then copy only after confirming the destination and collision plan.
### Discovery checkpoint — 2026-11-02

- The source bundle contains 18 top-level prefabs and no nested files.
- The complete local dependency closure is 54 assets: 18 prefabs, 18 `UL_*.mat` materials under `Generated_Tank_VFX/Materials/UnityLabs`, and 18 `*.tga` textures under `Generated_Tank_VFX/Textures/UnityLabs`; all have matching source `.meta` files.
- The prefabs have no filename or GUID collisions with `Assets/Particles111`. The only non-local references in the closure are the URP package shader/editor GUIDs `0406db5a14f94604a8c57ccfbc9f3b46` and `d0353a89b1f911e48b9e16bdc9f2e058`; these are package resources and will not be copied into `Assets`.
- Decision: copy the 18 prefabs into `Assets/Particles111` and the local material/texture closure into `Assets/Particles111/Dependencies/Generated_Tank_VFX`, preserving asset and folder metadata. The source scratch project remains unchanged.
- **Next steps:** Perform the collision-guarded copy, then validate byte hashes, GUID uniqueness, and all local references.
### Copy checkpoint — 2026-11-02

- Copied 54 source assets into `Assets/Particles111`: 18 prefabs at the destination root, plus 18 materials and 18 textures under `Assets/Particles111/Dependencies/Generated_Tank_VFX`.
- Copied 59 matching asset/folder `.meta` files, preserving the original GUIDs and source directory metadata. The source scratch files were only read and remain untouched.
- The copy used a collision guard and created no destination file that already existed.
- **Next steps:** Compare source/destination hashes, scan for duplicate GUIDs, resolve copied local references, run whitespace/status checks, and close the journal entry.
### Completion checkpoint — 2026-11-02

- **Status:** COMPLETED
- **Result:** Imported all 18 `UL_*.prefab` assets from `Assets/1111111` into `Assets/Particles111`, together with the 18 referenced `UL_*.mat` materials, 18 referenced `.tga` textures, and their folder/asset `.meta` files under `Assets/Particles111/Dependencies/Generated_Tank_VFX`. The source scratch project was not modified.
- **Validation:** Recomputed dependency closure contains 54 source assets; 113 asset/metadata paths were expected and present; source/destination SHA-256 hashes matched with 0 mismatches; 0 missing files; 0 duplicate GUID groups across `Assets`; 0 unexpected unresolved local references; only the two expected URP package GUIDs remain external; `git diff --check -- Assets/Particles111 TASK.md` passed.
- **Remaining limitations:** Unity Editor import/visual Play Mode verification was not run; the two URP shader/editor references rely on the project's installed URP package rather than copied `Assets` files. An unrelated pre-existing font modification remains untouched.
## Task: Reorient and tune GunShootDirectionZ cannon muzzle effect

- **Status:** IN_PROGRESS
- **Started:** 2026-11-02
- **User request:** Переробити префаб `GunShootDirectionZ`, який використовується як порохові гази в місці пострілу, щоб ефект мав локальний напрямок і постріл ішов уздовж локальної осі Z; налаштувати його під вигляд пострілу з пушки.
- **Scope:** Оглянути поточну структуру префаба та залежності, зберегти сумісність із поточним використанням, переналаштувати орієнтацію/particle systems і зробити ефект виразним як короткий спрямований muzzle blast без змін сторонніх сцен або gameplay-коду.
- **Acceptance criteria:** `GunShootDirectionZ` існує у правильному шляху, його візуальні emission/shape/velocity налаштовані на локальний +Z, дочірні шари не розходяться у всі сторони, prefab YAML має валідні локальні посилання, а зміни проходять доступні проєктні перевірки.
- **Next steps:** Знайти префаб і всі його локальні залежності/використання, визначити безпечну схему переналаштування, внести мінімальні зміни та перевірити результат.

### Discovery checkpoint — 2026-11-02

- `Assets/PartDone/GunShootDirectionZ.prefab` є побайтовою копією `Assets/PartDone/GunShoot.prefab` з єдиною поточною відмінністю в імені root GameObject.
- Ефект має п’ять particle systems: центральний one-shot flash на root та чотири дочірні шари (`Dot Sparkles`, `Smoke`, `Glow sparkles`, `Explosion`). Активні ShapeModule-конуси мають нульове `m_Rotation`, тобто їхня стандартна вісь — локальна `Y`, а не потрібна `Z`.
- Шар `Explosion` додатково має увімкнений `VelocityModule` з ненульовими `X/Y/Z` і `inWorldSpace: 1`; це головна причина, чому частина диму не наслідує локальний напрямок префаба.
- Рішення: зберегти матеріали, ієрархію, root scale/position та lifecycle script; повернути активні конуси на локальний `+Z`, звузити найбільш широкий glow-конус і перевести вибуховий дим на локальний forward velocity без ненульового бокового розльоту.

### Implementation checkpoint — 2026-11-02

- Оновлено `Assets/PartDone/GunShootDirectionZ.prefab`: усі п’ять ShapeModule отримали `m_Rotation.x = 90`, тому стандартна вісь cone-емісії повертається з локальної `Y` на локальну `+Z`.
- Звужено cone-розкид для sparkles/smoke до `18°`, `8°`, `24°` і `10°`; центральний flash залишено point-emission шаром із вимкненим ShapeModule.
- Для активного `Explosion` velocity-модуль переведено в local space, бокові компоненти `X/Y` обнулено, а forward-компонент залишено на локальній `Z = 4`.

### Completion checkpoint — 2026-11-02

- **Status:** COMPLETED
- **Result:** `GunShootDirectionZ` тепер є спрямованим cannon muzzle effect: центральний спалах залишається на дулі, а іскри, glow, пороховий дим і вибуховий smoke trail рухаються вузьким конусом уздовж локальної `+Z` та коректно наслідують поворот зброї.
- **Validation:** Статичний YAML-парсинг підтвердив 5 ShapeModule, 5 значень `m_Rotation.x = 90`, активний `Explosion` з `inWorldSpace: 0` і `X/Y = 0`, усі 5 non-zero GUID-посилань знайдені в `Assets`; semantic diff від `GunShoot.prefab` містить лише назву, напрямки, кути та Explosion velocity; `git diff --check -- TASK.md` пройшов.
- **Remaining limitations:** Unity batch import/Play Mode та ручний візуальний preview не запускалися, бо проєкт уже відкритий у Unity Editor; Editor має імпортувати змінений prefab штатно.

## Task: Add runtime direction-force control to GunShootDirectionZ

- **Status:** IN_PROGRESS
- **Started:** 2026-11-02
- **User request:** Додати скрипт на `GunShootDirectionZ`, щоб у runtime можна було налаштовувати силу напрямку пострілу.
- **Scope:** Створити окремий presentation-owner MonoBehaviour, який expose-ить runtime-safe силу напрямку, масштабує локальну forward-швидкість particle layers і підключити його до root prefab без змін бойової/network логіки та без порушення існуючого auto-destruct lifecycle.
- **Acceptance criteria:** Скрипт компілюється в Unity, присутній на `GunShootDirectionZ`, має інспекторне поле сили та публічний runtime setter, значення `1` зберігає поточний вигляд, зміна сили оновлює directional particle systems у runtime, а root flash не отримує небажаного прискорення.
- **Next steps:** Перевірити стиль і доступні particle APIs, реалізувати focused controller, підключити MonoBehaviour у prefab, виконати source/static validation і закрити журнал.

### Discovery checkpoint — 2026-11-02

- Root prefab уже має `CFX_AutoDestructShuriken`, який відповідає лише за lifecycle; нова сила напрямку не належить до його відповідальності.
- `Assets/PartDone` не має власного asmdef, тому focused controller компілюється в основному `Assembly-CSharp`; у проєкті вже використовується API `ParticleSystem.MainModule`.
- Рішення: кешувати authored `startSpeed` та `VelocityOverLifetime` curves один раз, множити їх під час runtime, автоматично реагувати на зміну Inspector-поля й не змінювати root particle system, що відповідає за центральний flash.

### Implementation checkpoint — 2026-11-02

- Додано `Assets/PartDone/GunShootDirectionController.cs` і його Unity metadata.
- Контролер expose-ить `directionForce` з базовим значенням `1`, публічні `DirectionForce`/`SetDirectionForce(float)`, обмежує некоректні значення та масштабує дочірні start-speed/velocity curves без world-space перемикання.
- `Assets/PartDone/GunShootDirectionZ.prefab` отримав компонент `11400002` з посиланням на controller script і `directionForce: 1`; існуючий auto-destruct компонент залишено без змін.

### Completion checkpoint — 2026-11-02

- **Status:** COMPLETED
- **Result:** На `GunShootDirectionZ` підключено `GunShootDirectionController`. У runtime поле `directionForce = 1` зберігає поточну швидкість, `0` зупиняє directional travel, а більші значення пропорційно прискорюють дочірні spark/smoke/glow layers; центральний root flash не змінюється.
- **Validation:** `dotnet build Assembly-CSharp.csproj --no-restore` завершився успішно з `0 Error(s)`; статичні перевірки підтвердили включення `.cs` у `Assembly-CSharp.csproj`, наявність script GUID у `.meta`, prefab component/script reference, `directionForce: 1`, public setter і root flash guard; `git diff --check -- TASK.md` пройшов.
- **Remaining limitations:** Unity Play Mode/runtime visual preview не запускався, бо Unity Editor вже відкритий на цьому проєкті; build показав 4 наявні попередження про `System.Net.Http`/obsolete FishNet API, без помилок.

## Task: Diagnose and correct GunShootDirectionZ local-Z runtime direction

- **Status:** IN_PROGRESS
- **Started:** 2026-11-02
- **User request:** З’ясувати, чому `GunShootDirectionZ` фактично не стріляє по локальній осі `Z`, та виправити напрямок.
- **Scope:** Перевірити serialized ShapeModule, runtime controller, локальний/world-space velocity і фактичну орієнтацію particle systems; змінити лише префаб/controller, не зачіпаючи gameplay/network логіку.
- **Acceptance criteria:** Усі directional layers мають однозначний локальний `+Z` direction source, runtime force не повертає world-space розліт, root flash залишається без прискорення, а source/build/static validation проходять.
- **Next steps:** Відтворити причину за serialized/runtime даними, виправити axis handling, перевірити збірку й закрити журнал.

### Discovery checkpoint — 2026-11-02

- У чотирьох активних ShapeModule `type: 0` — це `ParticleSystemShapeType.Sphere`, тому попередня зміна `m_Rotation.x = 90` не могла зробити емісію спрямованою; поворот сфери лишає її сферою.
- Root flash має окремий вимкнений ShapeModule і не є причиною розльоту. Runtime controller до цього масштабує швидкості, але не перевіряє форму емісії.
- Рішення: змінити активні шари на `type: 2` (`Cone`), залишити їхню rotation на локальний `+Z` і дублювати цю гарантію в controller під час застосування runtime force.

### Completion checkpoint — 2026-11-02

- **Status:** COMPLETED
- **Result:** Причина була в неправильному типі ShapeModule: `type: 0` означає Sphere, тому попередній `m_Rotation.x = 90` не міг створити напрямок. Усі 4 активні directional layers переведено на `type: 2` (`Cone`), збережено rotation `{x: 90, y: 0, z: 0}`, а controller тепер повторно гарантує Cone/+Z у runtime.
- **Validation:** Static prefab assertion підтвердив `ActiveCone=4`, `ActiveWrong=0`, root flash `enabled: 0`; `dotnet build Assembly-CSharp.csproj --no-restore` завершився з `0 Error(s)`; `git diff --check -- TASK.md` пройшов.
- **Remaining limitations:** Play Mode/ручний візуальний preview не запускався через уже відкритий Unity Editor; Unity має переімпортувати змінений prefab штатно.

## Task: Generalize GunShootDirectionController with per-particle force settings

- **Status:** IN_PROGRESS
- **Started:** 2026-11-02
- **User request:** Зробити скрипт універсальним: автоматично підтягувати всі particle systems префаба і дозволити налаштовувати силу напрямку окремо для кожного particle system у скрипті.
- **Scope:** Замінити один global force на serialized per-particle settings із auto-discovery дочірніх ParticleSystem, зберегти runtime setter/локальний `+Z` enforcement і не змінювати gameplay/network логіку.
- **Acceptance criteria:** Усі particle systems `GunShootDirectionZ` присутні в налаштуваннях controller, кожен має власний `directionForce`, missing child systems автоматично додаються, authored curves кешуються окремо для кожної системи, build/static prefab checks проходять.
- **Next steps:** Переробити controller/list serialization, заповнити prefab references для всіх ParticleSystem, зібрати `Assembly-CSharp` і закрити журнал.

### Completion checkpoint — 2026-11-02

- **Status:** COMPLETED
- **Result:** `GunShootDirectionController` тепер автоматично знаходить усі дочірні ParticleSystem і зберігає для кожного власний `directionForce`. У `GunShootDirectionZ` серіалізовано 5 entries: root flash, `Dot Sparkles`, `Smoke`, `Glow sparkles` та `Explosion`; для кожного можна задати окреме значення в Inspector.
- **Validation:** Static prefab assertions підтвердили `Entries=5`, `ForceFields=5` і fileID `19800006, 19800000, 19800002, 19800004, 19800010`; controller має auto-discovery, `RefreshParticleSettings`, `SetParticleDirectionForce` і Cone/+Z enforcement; `dotnet build Assembly-CSharp.csproj --no-restore` завершився з `0 Error(s)`; `git diff --check -- TASK.md` пройшов.
- **Remaining limitations:** Unity Play Mode/ручний preview не запускався, бо Unity Editor уже відкритий на цьому проєкті; build залишає 4 наявні попередження про `System.Net.Http`/obsolete FishNet API.

## Task: Diagnose particles moving downward when direction force is zero

- **Status:** IN_PROGRESS
- **Started:** 2026-11-02
- **User request:** З'ясувати, чому після налаштування сили напрямку на `0` частинки `GunShootDirectionZ` все одно летять вниз, і виправити причину, якщо це помилка скрипта.
- **Scope:** Перевірити скрін налаштувань, controller та prefab-модулі, відокремити напрямок пострілу від гравітації/інших модулів Particle System і внести мінімальне виправлення лише в controller/prefab за потреби.
- **Acceptance criteria:** Причина руху вниз підтверджена; `directionForce = 0` не додає власної forward-швидкості, але частинки не отримують небажаний вертикальний рух від controller; build/static validation проходять.
- **Next steps:** Переглянути вкладений скрін, перевірити gravity/velocity/external forces у prefab і коді, виправити root cause та повторно зібрати проєкт.

### Diagnostic checkpoint — 2026-11-02

- Screenshot inspection confirmed all five per-particle `directionForce` values are `0`, while the scene instance keeps `Default Direction Force = 1000` as a default for future/discovered entries.
- `GunShootDirectionController` scales `Main.startSpeed` and `VelocityOverLifetime` only; it does not modify `Main.gravityModifier`.
- `Glow sparkles` has `gravityModifier.scalar: 4` at `Assets/PartDone/GunShootDirectionZ.prefab:10142`; `Smoke` and `Explosion` retain small `0.001` gravity values at lines `5447` and `19926`.
- The scene stores the five per-particle overrides as `0` at `Assets/Game/Scenes/Vehicles.unity:529-546`; no code call overrides them in the project search.

### Completion checkpoint — 2026-11-02

- **Status:** COMPLETED
- **Result:** `directionForce = 0` works for the controller's forward emission speed. The downward motion is caused by Particle System gravity, primarily `Glow sparkles` with `Gravity Modifier = 4`, not by the direction-force calculation.
- **Validation:** Static source/prefab/scene inspection completed; `git diff --check -- TASK.md` passed with only Git's existing line-ending warning. No implementation file was changed because the request was diagnostic.
- **Remaining limitations:** Unity Play Mode was not run because the Unity Editor is already open on this project; to make zero force mean no vertical fall, set each affected particle's `Main > Gravity Modifier` to `0` in the prefab/scene instance.

## Task: Diagnose runtime particle settings and local-Z direction still failing

- **Status:** IN_PROGRESS
- **Started:** 2026-11-02
- **User request:** Повторно перевірити `GunShootDirectionZ`: після встановлення Gravity Modifier у `0` particle systems все одно летять вниз, не рухаються по локальній осі `Z`, а параметри controller не впливають на результат.
- **Scope:** Перевірити scene prefab instance, локальні transform-и дочірніх particle systems, runtime serialization/overrides, фактичне застосування `ParticleSystem` модулів і порядок lifecycle; виправити root cause у focused controller/prefab без зміни gameplay/network логіки.
- **Acceptance criteria:** У runtime controller гарантовано застосовує налаштування до фактичних particle systems scene instance; напрямок визначається локальним `+Z` root effect; per-particle force реально змінює швидкість; причина руху вниз усунена або явно локалізована.
- **Next steps:** Проаналізувати prefab/scene hierarchy та serialized overrides, перевірити controller lifecycle і дані модулів, внести мінімальний root-cause fix, зібрати проєкт і виконати static validation.

## Task: Normalize GunShootS prefab scale while preserving particle size

- **Status:** IN_PROGRESS
- **Started:** 2026-11-02
- **User request:** У префаба `GunShootS` root scale зменшено до `0.05`, але потрібно повернути scale до `1` і зменшити розмір на рівні particle systems, щоб фактичний вигляд залишився таким самим.
- **Scope:** Знайти точний prefab `GunShootS`, перевірити його ієрархію та particle-модулі, перенести масштаб із root Transform у відповідні particle-параметри без змін сторонніх сцен, gameplay-коду чи unrelated assets.
- **Acceptance criteria:** Root Transform префаба має scale `1,1,1`; particle-level налаштування компенсують попередній загальний масштаб `0.05`; локальні посилання та YAML залишаються валідними; статичні перевірки проходять.
- **Next steps:** Знайти `GunShootS`, встановити фактичний тип ієрархії/particle-параметрів, визначити безпечну компенсацію та перевірити результат.

### Discovery checkpoint — 2026-11-02

- Знайдено `Assets/PartDone/GunShootS.prefab`; root `GunShootS` має `m_LocalScale: {x: 0.05, y: 0.05, z: 0.05}` і містить п’ять ParticleSystem (`Dot Sparkles`, `Smoke`, `Glow sparkles`, root flash та `Explosion`).
- Усі чотири дочірні particle-вузли мають локальний scale `1,1,1` і `scalingMode: 0` (`Hierarchy`), тому масштаб можна безпечно перенести з root на їхні Transform-и без зміни внутрішніх particle curves.
- Root flash має вимкнений ShapeModule, тому для нього масштаб компенсується на рівні `InitialModule.startSize` і `startSpeed`; дочірні системи отримують локальний scale `0.05`.
- Рішення: встановити root scale `1,1,1`, дочірні particle Transform-и — `0.05,0.05,0.05`, а root flash constants `startSize`/`startSpeed` помножити на `0.05`.

### Implementation checkpoint — 2026-11-02

- Оновлено `Assets/PartDone/GunShootS.prefab` без змін сцени чи gameplay-коду.
- Root `GunShootS` переведено на scale `1,1,1`; чотири дочірні particle Transform-и переведено на `0.05,0.05,0.05`.
- Для root flash збережено попередній ефективний масштаб через `startSpeed: 0.15/0.25` та `startSize: 0.35/0.05` для authored min/max значень.

### Completion checkpoint — 2026-11-02

- **Status:** COMPLETED
- **Result:** `GunShootS` тепер має root scale `1`, а візуальний масштаб ефекту компенсовано на рівні particle-вузлів; зовнішній розмір має залишитися як при попередньому root scale `0.05`.
- **Validation:** Static YAML checks підтвердили root scale `1`, чотири дочірні scale `0.05`, п’ять ParticleSystem, правильні parent links і root particle compensation; усі 6 GUID-посилань (включно з built-in mesh) перевірені, 0 missing non-builtin GUID; `.meta` GUID валідний; `git diff --check -- TASK.md` пройшов.
- **Remaining limitations:** Unity Play Mode та ручний візуальний preview не запускалися; фінальна візуальна перевірка потребує імпорту цього prefab у відкритому Unity Editor.

## Task: Add randomized spread to Smoke111 exhaust smoke

- **Status:** IN_PROGRESS
- **Started:** 2026-11-02
- **User request:** Fix the `Smoke111` particle so smoke emits from one point in the intended direction while spreading with slight random variation like vehicle exhaust.
- **Scope:** Inspect the `Smoke111` prefab and its particle modules, adjust only the relevant Unity particle asset/settings, preserve the emission origin and overall direction, and validate the serialized effect configuration.
- **Acceptance criteria:** `Smoke111` emits from a single point, keeps its authored directional flow, adds controlled random lateral/upward dispersion rather than a single identical trajectory, and has valid Unity YAML/references.
- **Next steps:** Locate `Smoke111`, inspect shape/velocity/force modules and working-tree state, then apply the smallest focused prefab change and validate it.

### Discovery checkpoint — 2026-11-02

- `Assets/PartDone/Smoke111.prefab` is a single root `ParticleSystem`; the scene instance references it without transform rotation overrides.
- The emitter is effectively point-sized (`ShapeModule.radius.value: 0.0001`) but uses `ShapeModule.type: 0` (`Sphere`), so its `angle: 5` setting does not define a directional cone. The main module has no gravity, while `ForceModule` is enabled with a gradual upward force.
- Decision: preserve the point-sized radius and upward force, but switch the shape to `Cone` (`type: 2`) so particles leave the same point along local `+Z` with the existing small `5°` spread.
- Existing unrelated working-tree changes are present in the scene, other particle assets, and imported folders; only `Smoke111.prefab` and this journal entry are in scope.

### Implementation checkpoint — 2026-11-02

- Updated `Assets/PartDone/Smoke111.prefab` so its active `ShapeModule` uses `type: 2` (`Cone`) while retaining `angle: 5` and the point-sized `radius.value: 0.0001`.
- No scene, script, material, or unrelated particle asset was modified. This makes each particle launch from the same emitter point along local `+Z`, with controlled random direction inside the narrow cone; the existing upward `ForceModule` remains responsible for the exhaust lift.
- **Next steps:** Run static YAML/reference checks and inspect the final diff, then close the journal entry if validation passes.

### Completion checkpoint — 2026-11-02

- **Status:** COMPLETED
- **Result:** `Smoke111` now emits from the same point in the authored local `+Z` direction with a controlled `5°` cone spread, while its existing upward exhaust force remains active.
- **Changed files:** `Assets/PartDone/Smoke111.prefab` and this journal entry only for this task; pre-existing unrelated working-tree changes were left untouched.
- **Validation:** Static YAML assertions passed for one active Cone Shape module, point radius/position, 5° angle, retained upward Force module, and two referenced non-builtin GUIDs; `git diff --check -- TASK.md` passed.
- **Remaining limitations:** Unity Play Mode and manual visual preview were not run because the project Editor is already open; Unity should reimport the prefab before the final in-editor visual check.

### Follow-up checkpoint — 2026-11-02

- User reported that the first change was not visually apparent. The file contained the intended `Cone` shape, but the retained `5°` angle was too narrow for a clear exhaust plume preview.
- Decision: keep the single-point radius, widen the controlled cone spread to `15°`, and use a neutral Shape scale so the Cone module is not left with a zero axis scale.

### Follow-up implementation checkpoint — 2026-11-02

- Updated `Assets/PartDone/Smoke111.prefab` Shape Module to `type: 2` (`Cone`), `angle: 15`, and `m_Scale: {x: 1, y: 1, z: 1}`.
- Kept `radius.value: 0.0001`, `m_Position: {x: 0, y: 0, z: 0}`, and the existing upward Force module unchanged so the effect still starts at one point and rises in its authored direction.

### Follow-up completion checkpoint — 2026-11-02

- **Status:** COMPLETED
- **Result:** The visible spread is now widened to `15°`; `Smoke111` remains point-emitted with its authored upward force and local `+Z` flow.
- **Validation:** Static Shape/Force assertions passed; the prefab GUID is still referenced by `Assets/Game/Scenes/Vehicles.unity`; `git diff --check -- TASK.md` passed.
- **Remaining limitation:** Unity Editor must reimport or reload the prefab for the updated serialized settings to appear in an already-open Inspector/preview; Play Mode was not run.

### User verification checkpoint — 2026-11-02

- The supplied Inspector screenshot shows `Shape: Hemisphere` (`ParticleSystemShapeType` value `1`), while the on-disk `Assets/PartDone/Smoke111.prefab` currently has `ShapeModule.type: 2` (`Cone`) at line 604.
- The screenshot's `Scale 1,1,1` matches the latest prefab adjustment, but its `Hemisphere` selection means the Inspector is stale or is showing a different particle asset/instance rather than the current imported prefab state.
- No additional project asset was changed for this verification; the next action is to reimport/reopen `Smoke111.prefab` in Unity and confirm `Shape: Cone`, `Angle: 15`, and `Radius: 0.0001`.

### Visible instance fix checkpoint — 2026-11-02

- The open Unity scene instance under `Vehicles` was still showing `Shape: Hemisphere`, so the earlier prefab-file change was not visible in the already-loaded Inspector.
- Updated that visible `Smoke111` instance in Unity to `Shape: Cone`, `Angle: 15`, and `Radius: 0.0001`; the Inspector now visibly shows the intended point-emitted cone spread.
- The on-disk source remains `Assets/PartDone/Smoke111.prefab`; no other implementation asset was edited. The scene already had unrelated unsaved changes, so it was not saved or discarded.

### Unity serialization correction checkpoint — 2026-11-02

- Unity 6 Inspector confirmed the visible `Cone` selection and rewrote the prefab’s `ShapeModule.type` as `4`; the earlier hand-written `type: 2` value was not the correct serialized enum for this Unity version.
- Final serialized values are now validated from `Assets/PartDone/Smoke111.prefab`: `type: 4` (`Cone` in this editor), `angle: 15`, point radius `0.0001`, zero position, neutral scale, and enabled upward `ForceModule`.

### Visible instance completion checkpoint — 2026-11-02

- **Status:** COMPLETED
- **Result:** The currently open `Smoke111` Inspector visibly shows `Cone`, `Angle 15`, and `Radius 0.0001`; the source prefab is `Assets/PartDone/Smoke111.prefab`.
- **Validation:** Exact serialized `ShapeModule` and `ForceModule` assertions passed; only `Smoke111.prefab` and the append-only journal entry were changed for this task. Existing unrelated unsaved scene changes were preserved.
- **Remaining limitation:** The open `Vehicles` scene still has its pre-existing unsaved state; it was not saved or discarded by this fix.

## Task: Configure Smoke111 Velocity over Lifetime

- **Status:** IN_PROGRESS
- **Started:** 2026-11-02
- **User request:** Configure `Velocity over Lifetime` directly so `Smoke111` rises slowly and its movement gradually slows down.
- **Scope:** Change only the `Smoke111` particle prefab’s velocity/force settings; preserve the existing point emission, Cone spread, and scene changes.
- **Acceptance criteria:** `Velocity over Lifetime` is visibly configured with a local upward velocity curve and a lifetime speed reduction; the accelerating upward force does not override the intended behavior; serialized validation passes.
- **Next steps:** Inspect the current Unity Inspector/module state, configure the curves in the open prefab/instance, then validate the serialized prefab and visible Inspector.

### Discovery checkpoint — 2026-11-02

- `Assets/PartDone/Smoke111.prefab` already contains the intended upward `Y` curve (`0.25` to `0.03`) and speed curve (`1` to `0.25`), but `VelocityModule.enabled` is `0`, so neither curve is active.
- The serialized `Radial` velocity is still `1.36`, which adds unwanted outward motion; `ForceModule.enabled` is `1`, which accelerates the smoke upward instead of letting it slow naturally.
- Decision: enable `VelocityModule`, zero radial velocity, keep local-space upward/slowdown curves, and disable the accelerating Force module.

### Implementation checkpoint — 2026-11-02

- Enabled `VelocityModule` in `Assets/PartDone/Smoke111.prefab` and kept local simulation (`inWorldSpace: 0`).
- Configured the upward `Y` curve from `0.25` at particle birth to `0.03` at the end of its lifetime.
- Configured `speedModifier` from `1` to `0.25`, set radial velocity to `0`, and disabled `ForceModule` so the smoke no longer accelerates upward.
- Refreshed Unity and enabled the visible `Velocity over Lifetime` module on the open `Smoke111` instance without saving or discarding unrelated scene changes.

### Completion checkpoint — 2026-11-02

- **Status:** COMPLETED
- **Result:** `Smoke111` now has active local `Velocity over Lifetime`: upward Y velocity fades from `0.25` to `0.03`, overall speed fades from `1` to `0.25`, and radial velocity is `0`.
- **Supporting change:** `ForceModule` is disabled so it does not accelerate the smoke upward and defeat the slowdown curve.
- **Validation:** Serialized prefab checks passed for module enablement, local space, both curves, radial zero, and disabled force; `git diff --check -- TASK.md` passed with only the existing line-ending warning.
- **Remaining limitation:** Unity’s open `Vehicles` scene still has pre-existing unsaved changes and was not saved or discarded.
\n+## Task: Keep Smoke111 rising and prevent curl-in-place motion

- **Status:** IN_PROGRESS
- **Started:** 2026-11-02
- **User request:** Make `Smoke111` continue spreading upward instead of stopping and curling around one position.
- **Scope:** Inspect and adjust only the `Smoke111` particle motion modules; preserve the point Cone emission and controlled spread.
- **Acceptance criteria:** Smoke retains forward dispersion, has a sustained upward component, does not reduce its speed to near-zero, and has no unintended radial/noise motion causing it to curl in place.
- **Next steps:** Inspect Noise/Limit Velocity/Velocity curves, increase the sustained upward and forward motion, disable unwanted curl sources, then validate the prefab.

### Discovery checkpoint — 2026-11-02

- `NoiseModule`, `ClampVelocityModule` (`Limit Velocity over Lifetime`), `RotationBySpeedModule`, and `ExternalForcesModule` are disabled, so the curl is not coming from those modules in the prefab.
- The active velocity curves reduce the upward component to `0.03` and the overall travel speed to `0.25`, which makes the smoke appear to stall.
- Decision: keep the motion local and radial velocity at zero, but raise the sustained values to `Y: 0.35 → 0.18` and `Speed Modifier: 1 → 0.7`.

### Implementation checkpoint — 2026-11-02

- Updated `Assets/PartDone/Smoke111.prefab` so the upward velocity remains `0.35 → 0.18` instead of fading to near-zero.
- Raised the speed modifier minimum to `0.7`, preserving forward Cone travel while still applying a mild slowdown.
- Kept radial velocity at `0`; Noise, Clamp Velocity, Rotation by Speed, External Forces, and Force over Lifetime remain disabled.
- Refreshed Unity; the Smoke111 particle preview now shows a continuing upward plume rather than a stalled curl.

### Completion checkpoint — 2026-11-02

- **Status:** COMPLETED
- **Result:** Smoke continues moving upward and forward without reducing to a near-stationary curl; the active curves are `Y: 0.35 → 0.18` and `Speed Modifier: 1 → 0.7`.
- **Validation:** Serialized checks passed for sustained velocity, radial/noise/force disablement, and `git diff --check -- TASK.md` passed with only the existing line-ending warning.
- **Remaining limitation:** The open `Vehicles` scene still contains pre-existing unsaved changes and was not saved or discarded.
## Task: Build a sedan-style exhaust smoke plume

- **Status:** IN_PROGRESS
- **Started:** 2026-11-02
- **User request:** Rework `Smoke111` to resemble ordinary sedan exhaust: one dense emission point, short forward stream, gradual lateral expansion, gentle upward drift, and continuous movement without a straight single trail or stationary curl.
- **Scope:** Inspect every relevant Particle System motion module and modify only `Assets/PartDone/Smoke111.prefab`; preserve the point Cone origin and existing renderer/material setup.
- **Acceptance criteria:** Emission starts at one point; motion remains forward while widening progressively; upward drift is sustained; no module cancels motion or creates an unintended orbit/curl; serialized settings validate.
- **Next steps:** Inspect all motion modules and lifetime/size behavior, design the plume as coordinated curves, apply the focused prefab patch, refresh Unity, and validate the result.

### Discovery checkpoint — 2026-11-02

- The current `ShapeModule` is `Cone` (`type: 4`) but its `angle` is `0`, so the emission is effectively a single forward stream; this explains the one-direction appearance.
- `VelocityModule` has local upward motion and no radial motion, but its X axis is currently zero rather than providing gradual left/right separation.
- The plume design will use `Cone angle: 12°` for a narrow initial spread plus random local X velocity between `-0.12` and `0.12` so particles widen progressively without orbiting. Noise remains disabled to avoid introducing a spiral.
### Implementation checkpoint - 2026-11-02

- Updated only `Assets/PartDone/Smoke111.prefab`: kept point emission radius `0.0001`, set the Unity 6 Cone shape to `12` degrees, and preserved the renderer/material.
- Kept upward velocity `0.35 -> 0.18`, speed modifier `1 -> 0.8`, radial velocity `0`, local velocity space, and disabled Force, Noise, Clamp Velocity, and related curl-producing modules.
- Unity requires all linear Velocity over Lifetime axes to share one curve mode. After the console reported a mode mismatch, normalized X/Y/Z to Curve mode; the Cone angle provides per-particle lateral spread without invalid velocity data.

### Completion checkpoint - 2026-11-02

- **Status:** COMPLETED
- **Result:** `Smoke111` emits from one point, starts as a narrow forward plume, spreads through Cone direction variance, rises continuously, and slows mildly instead of stalling or curling in place.
- **Validation:** Unity AssetDatabase refresh completed; the selected `Smoke111` preview generated no new motion-module errors, and the Unity Console remained at `0` errors / `0` warnings after clearing old messages. Serialized prefab values were rechecked after refresh.
- **Remaining limitation:** The open `Vehicles` scene still contains pre-existing unsaved changes and was not saved or discarded.
## Task: Add SmokeFromPipe exhaust controller

- **Status:** COMPLETED
- **Started:** 2026-11-02
- **User request:** Create a script that controls the `SmokeFromPipe` particle effect from a normalized engine-load value where `0` is idle and `1` is maximum RPM.
- **Scope:** Add a focused exhaust-particle controller, expose a clamped `0..1` input and tunable idle/max particle behavior, and attach it to the `SmokeFromPipe` prefab if the prefab structure supports it.
- **Acceptance criteria:** The controller produces a continuous low-level idle exhaust, increases density/strength/speed/spread toward maximum load, preserves the existing particle material/setup, and exposes an explicit API for engine code to update the normalized load.
- **Implementation checkpoint:** Added `Assets/Game/Scripts/Effects/SmokeFromPipeController.cs` with a clamped `SetEngineLoad(float)` API and tunable idle-to-maximum response for emission rate, forward speed, lifetime, size, cone spread, and upward drift. Attached it to `Assets/PartDone/Etallon/SmokeFromPipe.prefab`; preserved the renderer/material and documented ownership in `docs/codex-game/smoke-effects.md` and `docs/codex-game/README.md`.
- **Next steps:** Validate C# syntax/compilation compatibility, prefab script GUID/serialized fields, and the focused diff without touching pre-existing changes.
- **Completion checkpoint:** Unity imported `SmokeFromPipeController.cs` and reloaded assemblies without C# compiler errors. Static checks passed for the script GUID, prefab component reference, serialized controller fields, and whitespace. Existing unrelated working-tree changes were left untouched.
- **Validation:** `git diff --check -- TASK.md Assets/PartDone/Etallon/SmokeFromPipe.prefab docs/codex-game/README.md docs/codex-game/smoke-effects.md` passed; Unity `Editor.log` showed the new script import followed by assembly reload with no `CS` errors.
- **Final validation:** `dotnet build Assembly-CSharp.csproj --no-restore --verbosity minimal` passed with `0 Error(s)` and 4 existing warnings; focused static assertions passed for the controller API, clamped input, prefab reference, serialized fields, local cone response, and cleaned velocity modules.
- **Remaining limitation:** The effect was not manually previewed in Play Mode, and the caller still needs to feed real engine RPM/load data into `SetEngineLoad(float)`.

## Task: Revert SmokeFromPipe exhaust controller

- **Status:** COMPLETED
- **Started:** 2026-11-02
- **User request:** Restore `SmokeFromPipe` because the added controller made the exhaust emit from the wrong place and broke the particle behavior.
- **Scope:** Remove only the controller component and files added by the previous task; preserve the original particle modules, renderer/material, scene, and unrelated working-tree changes.
- **Acceptance criteria:** `SmokeFromPipe.prefab` no longer references `SmokeFromPipeController`, the added script/documentation changes are removed, and static/reference validation passes.
- **Next steps:** Remove the prefab component and task-owned files, validate the prefab no longer references the script, then close this recovery entry.
- **Completion checkpoint:** Removed the added `SmokeFromPipeController` component from `Assets/PartDone/Etallon/SmokeFromPipe.prefab`, deleted the controller script/meta and smoke-effect documentation, and removed the documentation index entry. The original particle system, renderer, material, and three original GameObject components remain.
- **Validation:** Static assertions passed for absent controller references/GUID, three original prefab components, retained ParticleSystem/renderer/material sections, removed task-owned paths, and `git diff --check` passed. No scene or unrelated working-tree change was touched.
- **Remaining limitations:** The prefab was not manually previewed in Unity after the revert; the user should reload/reimport it if the Inspector was already open.

## Task: Increase SmokeFromPipe initial exhaust speed

- **Status:** COMPLETED
- **Started:** 2026-11-02
- **User request:** Make `SmokeFromPipe` leave the exhaust point faster while preserving its current trajectory, shape, position, spread, and final stopping behavior.
- **Scope:** Change only the particle speed setting responsible for initial exhaust travel; preserve all other serialized particle modules and renderer/material references.
- **Acceptance criteria:** Smoke exits the pipe faster, retains the authored trajectory and visual form, and keeps the existing lifetime/slowdown behavior at the end of the path.
- **Next steps:** Inspect the current speed and velocity modules, apply the smallest focused prefab change, then validate serialized values and references.
- **Implementation checkpoint:** Updated only the two birth-time keys of `VelocityModule.speedModifier` in `Assets/PartDone/Etallon/SmokeFromPipe.prefab` from `1` to `1.5`. Kept `InitialModule.startSpeed`, Shape, trajectory axes, spread, lifetime, renderer, and material unchanged; kept the final speed keys at `0.8` and `1`.
- **Completion checkpoint:** Smoke now receives a 50% stronger initial velocity multiplier while retaining the same authored local trajectory and final speed/slowdown values.
- **Validation:** Focused YAML assertions passed for exactly two changed start keys, unchanged start speed/shape/trajectory/material, preserved final keys, absent controller reference, and `git diff --check` passed. No scene or unrelated working-tree change was touched.
- **Remaining limitation:** The particle was not manually previewed in Play Mode; Unity should reimport/reload the prefab before visual confirmation if the Inspector was already open.

## Task: Keep SmokeFromPipe visible while the vehicle moves

- **Status:** COMPLETED
- **Started:** 2026-11-02
- **User request:** Fix `SmokeFromPipe` disappearing or appearing with a delay while the vehicle moves; smoke must remain visible as a continuous exhaust tail and show particles leaving the pipe during motion.
- **Scope:** Inspect culling, simulation-space, and transform-following settings; change only the particle-system lifecycle/motion settings needed for a stable moving exhaust trail, preserving the authored shape, trajectory, speed response, and renderer/material.
- **Acceptance criteria:** The system keeps simulating while moving or temporarily outside the camera view, emitted particles remain behind the moving exhaust point instead of being dragged with it, and the existing smoke appearance remains intact.
- **Next steps:** Inspect the serialized lifecycle and simulation settings, apply the smallest focused prefab change, then validate all preserved motion/visual values and references.
- **Implementation checkpoint:** Changed only `cullingMode` from `1` to `3` (`Always Simulate`) and `moveWithTransform` from `1` to `0` in `Assets/PartDone/Etallon/SmokeFromPipe.prefab`. Preserved the shape, start-speed boost, velocity curves, lifetime, renderer, and material.
- **Completion checkpoint:** `SmokeFromPipe` now continues simulating during vehicle motion/camera culling, and emitted particles stay behind the moving pipe as a visible exhaust tail instead of being carried with the emitter.
- **Validation:** Static assertions passed for always-simulate mode, detached particles, preserved loop/play state, shape/trajectory/speed/material values, absent controller reference, and `git diff --check` passed. Unity reimported the prefab without recent YAML/compiler errors.
- **Remaining limitation:** Play Mode was not manually driven after the change; reload/reimport the prefab in an already-open Unity Inspector before visual confirmation.

## Task: Normalize Etallon particle prefab scale

- **Status:** IN_PROGRESS
- **Started:** 2026-11-02
- **User request:** In `Assets/PartDone/Etallon`, set every prefab root scale to `1,1,1` while preserving the current visible particle size.
- **Scope:** Inspect all prefabs in the Etallon folder, identify non-unit transform scales, and compensate particle-system size settings only where needed so the rendered effect keeps its current dimensions.
- **Acceptance criteria:** Every Etallon prefab has root/local transform scale `1,1,1`; each affected particle effect retains its previous effective size; no unrelated assets or prefab settings change.
- **Next steps:** Inventory prefab hierarchy and particle size fields, apply focused YAML changes, validate scale compensation and references, then close this entry.

### Discovery checkpoint — 2026-11-02

- The Etallon folder contains 13 prefab files; prefab variants backed by unit-scale source prefabs already resolve to unit root scale.
- Non-unit transforms are limited to `ExplosionMain` root scale `0.5`, `GunShootS` child Particle System transforms at `0.05`, and `ShootInRobot` root scale `0.05`.
- The affected Particle System Main modules use scalar/random start-size values; the visual-size compensation will multiply those serialized size values by the original transform scale before setting the transforms to `1,1,1`.
- No unrelated scripts, scenes, materials, or imported dependency assets will be changed.

### Implementation checkpoint — 2026-11-02

- Set every non-unit Etallon Transform scale to `1,1,1` in `ExplosionMain.prefab`, `GunShootS.prefab`, and `ShootInRobot.prefab`.
- Compensated the affected Particle System Main `startSize.scalar` and `minScalar` values by the removed uniform scale: `0.5` for `ExplosionMain`, `0.05` for `GunShootS` child systems, and `0.05` for `ShootInRobot`.
- Preserved all other serialized particle settings, references, and asset files.

### Completion checkpoint — 2026-11-02

- **Status:** COMPLETED
- **Result:** All serialized Transform scales under `Assets/PartDone/Etallon` are now `1,1,1`; the affected particle systems retain their previous particle sprite sizes through focused Main `startSize` compensation.
- **Changed files:** `Assets/PartDone/Etallon/ExplosionMain.prefab`, `Assets/PartDone/Etallon/GunShootS.prefab`, `Assets/PartDone/Etallon/ShootInRobot.prefab`, and this journal entry only.
- **Validation:** Static YAML assertions passed for 14 prefabs, zero remaining non-unit scales, and 15 expected start-size compensations. `git diff --check -- TASK.md` passed with the repository's existing LF-to-CRLF warning.
- **Remaining limitation:** Unity Play Mode/manual visual preview was not run; Unity should reimport or reload the Etallon prefabs before visual confirmation if the Editor was already open.

## Task: Roll back Etallon particle scale normalization

- **Status:** IN_PROGRESS
- **Started:** 2026-11-02
- **User request:** Revert the previous Etallon scale-normalization change because it broke the particle effects.
- **Scope:** Restore only the three prefabs changed by the previous task to their pre-change Transform scales and Particle System Main `startSize` values; leave all other project changes untouched.
- **Acceptance criteria:** `ExplosionMain`, `GunShootS`, and `ShootInRobot` match their pre-normalization scale and start-size serialization; no unrelated files are changed.
- **Next steps:** Apply the recorded inverse values, validate all restored fields, then close this recovery entry.

### Implementation checkpoint — 2026-11-02

- Restored `ExplosionMain` root scale `0.5`, `GunShootS` child scales `0.05`, and `ShootInRobot` root scale `0.05`.
- Restored all 15 affected Particle System Main `startSize.scalar/minScalar` pairs to their pre-normalization values.
- No other Etallon prefab fields were changed by the rollback.

### Completion checkpoint — 2026-11-02

- **Status:** COMPLETED
- **Result:** The previous Etallon scale-normalization change is fully reverted in the three affected prefabs.
- **Validation:** Static assertions passed for 7 original Transform scales and 15 original `startSize` pairs.
- **Remaining limitation:** Unity Play Mode was not run; reload/reimport the prefabs if the Editor already has cached serialized data open.

## Task: Integrate projectile and robot destruction particle effects

- **Status:** COMPLETED
- **Started:** 2026-11-02
- **User request:** Replace the old projectile, muzzle, and impact particle prefabs with the prepared effects; orient directional effects correctly; remove `OtherObjectImpact`; add robot-hit and robot-destruction particle sequences; keep persistent fire effects on detached robot parts.
- **Scope:** Inspect the current projectile collision, muzzle-spawn, robot destruction, detached-part, and particle prefab ownership; implement the smallest focused runtime integration using explicit effect ownership; update the relevant gameplay documentation; preserve unrelated working-tree changes and optimize transient/persistent effect lifecycle.
- **Acceptance criteria:** `GunShoot` replaces `CFX3_Fire_Shoot` and points its local Z toward the gun/object direction; `ExplosionProjectileGround` replaces `RobotImpact` and points local Z along the collision normal; `OtherObjectImpact` is removed from project references/files; robot hits spawn `ShootInRobot`; robot destruction spawns `ExplosionMain` plus one random `Explosion 1/2/3` and one random `Explosion FREE 1-4`; detached turret/body parts receive persistent `PartFire`, with up to three additional random parts; transient effects clean up safely; focused validation passes.
- **Next steps:** Inspect repository state, identify effect owners and prefab references, document the ownership boundary, implement integration, then run focused static/build validation and close this entry.

### Discovery checkpoint — 2026-11-02

- The working tree already contains unrelated modifications in `Assets/Game/Content/Fonts/RobotoCondensed-Regular SDF.asset` and `RobotoCondensed-Regular SDF2.asset`; these must remain untouched. `TASK.md` is modified by this task.
- New prepared effects are present under `Assets/PartDone/Etallon`, including `GunShoot`, `ExplosionProjectileGround`, `ShootInRobot`, `ExplosionMain`, `PartFire`, and the requested explosion variants. Duplicate copies also exist under `Assets/Particles111/Copu`.
- The active combat path is `Projectile` plus `ProjectileRuntimePool`/`PooledImpactFx`; `Projectile` currently stores separate `robotImpactFxPrefab` and `otherObjectImpactFxPrefab` references and always routes impact visuals through `PooledImpactFx`.
- `NetworkWeaponShooter` owns projectile prefab spawning and prewarming, while `DeathLogic` owns detached turret/body part lifecycle. New destruction/fire presentation should be a focused VFX owner coordinated by those existing domain owners rather than added as unrelated policy to the projectile coordinator.
- The current projectile impact pool applies a camera-facing visual offset intended for projector decals, so the directional ground impact needs a separate particle-effect spawn path that preserves the exact collision point and normal.
- **Next steps:** Inspect prefab component types and all shooter/death call sites, then define the focused transient/persistent VFX owner and serialized wiring.

### Implementation checkpoint — 2026-11-02

- Added particle-only support to `PooledImpactFx` with automatic particle-profile discovery and a `UsesSurfaceProjectors` distinction; `ProjectileRuntimePool` now keeps particle impacts at the exact collision point while preserving the legacy projector offset only for projector profiles.
- Replaced `Projectile.otherObjectImpactFxPrefab` with `groundImpactFxPrefab`, routed non-robot impacts to the new ground effect, and made projectile/muzzle orientation use stable rotations with local `+Z` aligned to the collision normal or shot direction.
- Added `RobotDestructionParticleController` as the focused destruction presentation owner. It pools `ExplosionMain` and the requested random standard/free variants, then attaches persistent `PartFire` to the first detached part plus up to three unique random additional parts at each part's bounds center.
- Updated `DeathLogic` to calculate the robot visual center, detach debris as before, and pass the resulting center/part list to the new VFX owner without changing health, damage, authority, or network lifetime decisions.
- Added a temporary Editor wiring utility at `Assets/Editor/ConfigureCombatParticleEffects.cs`; it will add `PooledImpactFx` to transient prepared prefabs, remove incompatible legacy auto-destruct components, orient the authored directional particle shapes, update projectile/client references, and wire all eight IA/NV robot prefabs.
- **Next steps:** Run the Editor wiring utility, inspect serialized output, then remove old asset references/files and update stale tests/docs.

### Wiring and cleanup checkpoint - 2026-11-02

- Ran the temporary wiring utility successfully, then removed the utility after its one-time serialized setup completed.
- Wired `GunShoot` into the client muzzle settings, `ExplosionProjectileGround` and `ShootInRobot` into `ProjectileVisual`, and `RobotDestructionParticleController` into all eight IA/NV robot prefabs.
- Removed the old `CFX3_Fire_Shoot`, `RobotImpact`, and `OtherObjectImpact` prefabs, their old decal/material asset trees, the obsolete decal build utility, and the stale projector-focused test; preserved unrelated font modifications.
- Added `CombatParticleEffectsPlayModeTests` for projectile references, particle-only profiles, persistent `PartFire`, and all robot destruction-controller bindings.

### Completion checkpoint - 2026-11-02

- **Status:** COMPLETED
- **Result:** Directional muzzle and ground effects now use local `+Z` for shot direction and collision normal; robot hits use exact collision-point `ShootInRobot`; robot destruction emits the required three center bursts and persistent fire on four detached parts maximum.
- **Optimization:** Transient bursts use the existing bounded `ProjectileRuntimePool`; looping `PartFire` is instantiated once per selected detached part and is not routed through one-shot cleanup.
- **Validation:** `dotnet build Assembly-CSharp.csproj --no-restore` passed with 0 errors; focused Unity PlayMode run passed `4/4`; `git diff --check` passed. The full PlayMode suite reported 7 pre-existing unrelated failures while the new fixture passed `4/4`.
- **Remaining limitation:** Manual in-game visual QA was not run; the focused asset and wiring tests passed, and the existing full-suite failures are outside this particle-effects change.

### Final cleanup checkpoint - 2026-11-02

- Removed the remaining orphan texture and folder metadata files from the deleted `ImpactOtherObject` and `ImpactRobot` trees.
- Final audit passed with no active references to the old effect names, eight robot prefab controller bindings, no legacy files under the projectile content folder, and `git diff --check` clean.
- **Status:** COMPLETED

### Persistent fire hardening checkpoint - 2026-11-02

- Removed the legacy `CFX_AutoDestructShuriken` component from `PartFire`; its looping particle systems now cannot self-destruct through the old one-shot cleanup path.
- Extended `CombatParticleEffectsPlayModeTests` to assert that `PartFire` has neither pooled FX nor auto-destruction.
- Focused Unity PlayMode validation passed `4/4` again after this final prefab change.
- **Status:** COMPLETED

## Task: Fix PartFire scale and restore projectile impact decals

- **Status:** IN_PROGRESS
- **Started:** 2026-11-02
- **User request:** Fix the oversized `PartFire` effect so its world scale remains `1,1,1`, and restore the projectile impact decals that disappeared after the particle integration.
- **Scope:** Inspect detached-part fire transform/parenting and the prior decal assets/runtime path; preserve the new particle routing while restoring only the missing decal behavior and correcting scale handling.
- **Acceptance criteria:** `PartFire` remains world-scale `1,1,1` on detached parts; projectile hits show the authored decals again; ground/robot particle effects and persistent fire behavior remain intact; focused validation passes.
- **Next steps:** Inspect current prefab serialization and git history for the removed decal chain, implement the smallest focused fix, validate references and runtime behavior, then close this entry.

### Discovery checkpoint - 2026-11-02

- `PartFire` was instantiated as a child with local scale `1`, so any non-unit detached-part scale was inherited and made the effect oversized in world space.
- The new `GunShoot`, `ExplosionProjectileGround`, and `ShootInRobot` profiles had all decal projector fields set to `0`; the previous `RobotImpact` and `OtherObjectImpact` prefabs still contain the authored projector configuration in repository history.
- Restored only the authored decal materials/textures and metadata from `HEAD`; the old impact prefabs themselves remain removed. The new impact particle prefabs will receive the restored projectors while keeping their new particle systems.

### Implementation checkpoint — 2026-11-02

- Changed `RobotDestructionParticleController` to place `PartFire` at each detached part's world-space bounds center, preserve world scale `1,1,1`, and then attach it without inheriting the part's scale.
- Restored the authored `Impact Decal`, `Impact Glow Decal`, and optional `Impact Glow Halo` children on `ExplosionProjectileGround` and `ShootInRobot`, referencing the restored material GUIDs and the original ground/robot profile sizes.
- Added projector-parent scale compensation in `PooledImpactFx`, so decal and halo world dimensions remain profile-defined even when a particle prefab has a non-unit root scale such as `ShootInRobot`.
- Updated the focused PlayMode asset test and gameplay/asset documentation to distinguish particle-only effects from projector-backed impact effects.
- **Validation:** Static prefab audit passes for both impact prefabs: every added `MonoBehaviour` has a valid `m_GameObject`, every profile projector reference resolves to a declared local file ID, and all six restored decal material GUIDs exist. `dotnet build Assembly-CSharp.csproj --no-restore` passes with 0 errors; `git diff --check` passes; no active asset reference to `CFX3_Fire_Shoot`, `RobotImpact`, or `OtherObjectImpact` remains.
- **Remaining limitation:** A fresh Unity PlayMode run and manual visual QA were not repeated because the user's Unity Editor is open and the batch runner is project-locked. The editor log contained the initial pre-fix broken `ShootInRobot` import warning; the missing `m_GameObject` reference was corrected and the current YAML audit is clean, but a fresh editor refresh should still be used for final visual confirmation.
- **Result:** `PartFire` now keeps world scale `1,1,1` after attachment to scaled detached parts, and the authored decal/glow/halo projectors are restored on `ExplosionProjectileGround` and `ShootInRobot` while the legacy impact prefabs remain deleted.
- **Status:** COMPLETED

## Task: Add timed PartFire cleanup, preserve particle prefab scale, and keep enemy list entries

- **Status:** IN_PROGRESS
- **Started:** 2026-11-02
- **User request:** Make `PartFire` live for 30 seconds and then disappear; keep every particle effect at the exact scale authored in its prefab; add configurable camera-side spawn offset for explosion/impact particles except `GunShoot` and `PartFire`; keep defeated enemies in the in-game enemy list for the whole game.
- **Scope:** Inspect the current particle pooling/spawn path, client runtime settings owner, `PartFire` lifecycle, and the enemy-list visibility/removal owner. Preserve existing directional/projector behavior and network/authority boundaries.
- **Acceptance criteria:** `PartFire` is removed after 30 seconds; particle FX are spawned with their authored root scale (for example `ExplosionProjectileGround` remains `0.05`); all eligible explosion/impact FX receive a client-configurable camera offset while `GunShoot`/`PartFire` do not; an enemy remains represented in the list after death until the game/session ends; focused validation passes.
- **Next steps:** Locate the responsible VFX/settings and enemy-list owners, record the current behavior, then implement focused lifecycle, scale/offset, and list-retention changes with regression coverage.

### Discovery checkpoint - 2026-11-02

- `RobotDestructionParticleController` owns detached-part fire creation, so the 30-second lifetime and world-scale correction belong there rather than in a generic particle component.
- `ProjectileRuntimePool` already captures each prefab root `localScale`, but the generic visual-FX entry point also serves muzzle effects; camera offset must therefore be explicit so `GunShoot` remains untouched.
- `ClientSettings.gameplayRuntime` is the existing inspector-wired client runtime settings owner; it now exposes a clamped camera-side particle offset with a default of `1` world unit.
- `GameplayPlayerListHud` retains missing rows only when `row.IsDead` was already set; an enemy can disappear before that event reaches the HUD. Enemy rows will be retained as dead when their vehicle leaves the active registry, while session `Clear()` remains the only normal removal path.
- `ExplosionProjectileGround` and the renamed `ExplosionProjectileRobot` use the restored decal projector profile fields and share the expected robot-impact GUID, so decal restoration should remain in the runtime projector setup rather than reintroducing legacy prefabs.

### Implementation checkpoint - 2026-11-02

- Added a serialized `ClientSettings.gameplayRuntime.particleCameraOffset` with default `1` world unit and finite `0..10` validation. `ProjectileRuntimePool` reads it at spawn time, so runtime Inspector changes apply without restarting the client.
- Applied the camera-side offset to impact and robot-destruction FX only. Muzzle FX use a dedicated no-offset pool entry point, and `PartFire` remains a directly attached effect with no offset.
- Made pooled impact roots restore their authored prefab world scale after attachment; `PartFire` is parented with an explicit world scale of `1,1,1` and scheduled for destruction after `30` seconds.
- Kept decal projectors reusable by explicitly toggling their enabled state on configure/reset; ground and robot impact profiles remain wired to their restored decal and glow projectors.
- Changed enemy HUD retention to preserve every missing enemy row as a dead row for the active session and added stable player-id lookup to avoid duplicate rows after a network object replacement.
- Updated focused asset tests and `docs/codex-game/` ownership/behavior notes for the runtime offset, prefab scale, timed fire, robot impact name, decals, and enemy-list retention.

### Validation checkpoint - 2026-11-02

- `dotnet build Assembly-CSharp.csproj --no-restore` passes with 0 errors; existing System.Net.Http and obsolete Unity API warnings remain.
- `git diff --check` passes.
- Static audit passes: `ProjectileVisual.prefab` references `ExplosionProjectileRobot` and `ExplosionProjectileGround`; both impact prefabs contain valid decal/glow projector profile references; authored robot root scale remains `0.05`.
- Unity PlayMode/manual visual QA was not rerun because the project is open in the Unity Editor; the new focused tests are ready for the next editor refresh.
- **Status:** COMPLETED

## Task: Fix PlayMode test namespace compilation

- **Status:** COMPLETED
- **Started:** 2026-11-02
- **User request:** Fix `CombatParticleEffectsPlayModeTests.cs` compile error: `Game.Scripts.Client` namespace is unavailable to the test assembly.
- **Scope:** Inspect test assembly references and remove the invalid dependency while preserving coverage for the particle camera offset.
- **Acceptance criteria:** The test compiles in its Unity test assembly; the camera-offset regression remains covered without adding an invalid assembly dependency; focused validation passes.
- **Next steps:** None.

### Discovery checkpoint - 2026-11-02

- The project has no `.asmdef` files under `Assets`; the focused test is compiled separately from the main game assembly and cannot resolve a direct `using Game.Scripts.Client` dependency.
- Existing PlayMode tests already resolve game types through `Type.GetType(..., Assembly-CSharp)`, so the camera-offset regression can keep runtime coverage without adding an assembly dependency.

### Implementation and validation checkpoint - 2026-11-02

- Removed the invalid namespace import and changed the camera-offset test to invoke `GameplayRuntimeSettings` through reflection, matching the surrounding test assembly pattern.
- `dotnet build Assembly-CSharp.csproj --no-restore` passes with 0 errors; existing project warnings remain.
- `git diff --check` passes and no direct `Game.Scripts.Client` import remains in `CombatParticleEffectsPlayModeTests.cs`.
- Unity PlayMode execution was not run because the project remains open in the Unity Editor.
- **Result:** The reported CS0234 compile error is resolved without changing runtime particle behavior.

## Task: Fix PlayMode test GetType overload compilation

- **Status:** COMPLETED
- **Started:** 2026-11-02
- **User request:** Fix `CombatParticleEffectsPlayModeTests.cs` error `CS1501`: `GetType` has no overload accepting one argument.
- **Scope:** Resolve the test helper name collision while preserving the reflection-based camera-offset regression.
- **Acceptance criteria:** The test compiles without the `GetType` overload error; existing particle test coverage remains unchanged; focused validation passes.
- **Next steps:** None.

### Discovery and implementation checkpoint - 2026-11-02

- `CombatParticleEffectsPlayModeTests` has no `GetType(string)` helper; the new call incorrectly resolved to `object.GetType()`.
- Replaced the call with explicit `Type.GetType("Game.Scripts.Client.GameplayRuntimeSettings, Assembly-CSharp")` and retained a null assertion before reflection.

### Validation checkpoint - 2026-11-02

- `dotnet build Assembly-CSharp.csproj --no-restore` passes with 0 errors; existing project warnings remain.
- `git diff --check` passes.
- The affected test now uses `Type.GetType(...)`; no one-argument unqualified `GetType` call remains at the reported location.
- Unity PlayMode execution was not run because the project remains open in the Unity Editor.
- **Result:** The reported CS1501 error is resolved without changing particle test behavior.

## Task: Fix flat ExplosionProjectileRobot visual

- **Status:** COMPLETED
- **Started:** 2026-11-02
- **User request:** Fix the serious regression where `ExplosionProjectileRobot` appears flat despite previously looking correct.
- **Scope:** Inspect the robot impact prefab, authored particle/decal dimensions, pooled transform-scale handling, and collision-normal rotation path; preserve the intended authored size and directional behavior.
- **Acceptance criteria:** `ExplosionProjectileRobot` renders with its authored three-dimensional appearance at runtime, keeps its configured scale, and remains correctly oriented on robot impacts; focused validation passes.
- **Next steps:** None.

### Discovery and implementation checkpoint - 2026-11-02

- `ExplosionProjectileRobot` is authored with a root scale of `0.05` and its particle data matches the previous `ShootInRobot` asset; the prefab scale itself was not corrupted.
- The shared impact path was rotating every impact root from the collision normal. That is correct for `ExplosionProjectileGround`, but incorrect for the robot hit effect, which was authored without a direction and therefore could appear flat after rotation.
- Added explicit `PooledImpactFx.alignRootToImpactNormal` configuration. Ground keeps it enabled; `ExplosionProjectileRobot` disables it while still receiving the real collision normal for decal projection.
- Added focused PlayMode coverage for the two impact root-direction modes.

### Validation checkpoint - 2026-11-02

- `dotnet build Assembly-CSharp.csproj --no-restore` passes with 0 errors; existing project warnings remain.
- `git diff --check` passes.
- Static audit confirms `ExplosionProjectileRobot` keeps authored root scale `0.05` and has `alignRootToImpactNormal: 0`; ground has the setting enabled.
- Unity PlayMode/manual visual QA was not run because the project remains open in the Unity Editor.
- **Result:** Robot impact particles keep their authored orientation instead of being rotated into a flat collision-normal pose; decals still use the collision normal.

## Task: Locate pre-battle countdown setting

- **Status:** COMPLETED
- **Started:** 2026-11-02
- **User request:** Identify where the current 10-second countdown before battle is configured.
- **Scope:** Locate the countdown owner and its serialized or constant duration without changing project files.
- **Acceptance criteria:** Provide the exact file, field/constant, and current value used by the pre-battle countdown.
- **Next steps:** None.

### Discovery and validation checkpoint - 2026-11-02

- The countdown is owned by `BattleCountdownController` in `Assets/Game/Scripts/Gameplay/Battle/BattleCountdownController.cs`; its serialized `countdownSeconds` field defaults to `10`.
- The active Client scene overrides the same field at `Assets/Game/Scenes/Client.unity:59408` with `countdownSeconds: 10`.
- **Result:** The authoritative value for the current client countdown is the `BattleCountdownUI` scene component field `countdownSeconds` in `Client.unity`; no project files were changed beyond this journal entry.

## Task: Fix intermittent stretched robot impact particles

- **Status:** COMPLETED
- **Started:** 2026-11-02
- **User request:** Investigate and fix the intermittent bug where impact particles stretch strongly when hitting a robot.
- **Scope:** Inspect the pooled impact FX transform reset, robot attachment parent, authored scale, and non-uniform parent scale handling; preserve decals and authored particle dimensions.
- **Acceptance criteria:** Robot impact particles retain their authored uniform scale on every pooled spawn, do not stretch on X/Z, and the focused validation passes without regressing existing impact visuals.
- **Next steps:** None.

### Discovery and implementation checkpoint - 2026-11-02

- `ExplosionProjectileRobot` is spawned from the shared pooled impact path and is attached to the robot `VehicleRoot` so the impact visuals and decals follow the target.
- The pool reset correctly restores the authored root scale, but the previous compensation used the parent `lossyScale` axis-by-axis. A non-uniform parent scale combined with a different child rotation can produce a sheared particle transform, which explains the intermittent X/Z stretching.
- Added `CanSafelyAttachImpactFx`: impact FX attach to the target only when its effective world scale is finite, non-zero, and uniform within tolerance; otherwise the FX remains in world space at its authored scale, while impact decals still use the existing collision target/layer path.
- Added a focused regression test for rejecting non-uniform attachment scales.

### Validation checkpoint - 2026-11-02

- `dotnet build Assembly-CSharp.csproj --no-restore` passes with 0 errors; existing project warnings remain.
- `git diff --check` passes.
- Changed paths are limited to `ProjectileRuntimePool.cs`, `CombatParticleEffectsPlayModeTests.cs`, and this journal entry.
- Unity PlayMode/manual visual QA was not run because the project remains open in the Unity Editor.
- **Result:** Impact FX no longer inherit unsafe non-uniform robot parent scales, preventing the intermittent particle deformation while preserving authored prefab dimensions and decal handling.
- **Status:** COMPLETED

## Task: Keep robot impact FX attached without scale distortion

- **Status:** COMPLETED
- **Started:** 2026-11-02
- **User request:** Keep impact decals and particles attached to the robot or turret while fixing the scale distortion caused by the current attachment approach.
- **Scope:** Replace the world-space fallback with a shared attached anchor at the visual mesh/collider level, preserve movement with the target, and prevent non-uniform scale inheritance without changing unrelated robot transforms.
- **Acceptance criteria:** Robot impact particles and decals move with the hit mesh/turret, particle dimensions remain authored and uniform, pooled FX reset safely, and focused validation passes.
- **Next steps:** None.

### Discovery and implementation checkpoint - 2026-11-02

- The local projectile path already had the physics collider, but the network-only impact path attached to the whole `VehicleRoot`, which could inherit unrelated vehicle scale. Both paths now resolve the visual mesh/renderer nearest to the impact point; the root remains the fallback only when no visual renderer is available.
- Added a reusable hidden attachment anchor under that visual mesh. Its axes compensate the effective mesh scale without changing the mesh transform, while the pooled FX root remains the common parent for particles and decals.
- Pool release, oldest-instance reuse, one-shot visual spawns, and external destruction all detach/recycle or destroy the anchor safely.
- Added regression coverage proving an impact root with authored scale `0.05` stays uniformly scaled while attached to a rotated non-uniform mesh parent.

### Validation checkpoint - 2026-11-02

- `dotnet build Assembly-CSharp.csproj --no-restore` passes with 0 errors; existing project warnings remain.
- `git diff --check` passes.
- Updated ownership documentation in `docs/codex-game/weapons-damage.md`.
- Unity PlayMode/manual visual QA was not run because the project remains open in the Unity Editor.
- **Result:** Robot impact FX remain attached to the moving visual mesh/turret, decals continue following the target, and authored particle scale is protected by the neutral anchor.

## Task: Analyze convergence circle growth conditions

- **Status:** COMPLETED
- **Started:** 2026-11-03
- **User request:** Analyze the convergence/aiming circle and explain under which conditions it increases.
- **Scope:** Trace the reticle/convergence-circle presentation from its UI owner through aiming, movement, target, and weapon state inputs; identify the exact formulas, thresholds, and lifecycle conditions without changing implementation files.
- **Acceptance criteria:** Name the responsible files and fields, distinguish direct size inputs from visual/layout effects, explain when the circle grows or shrinks, and record validation and any uncertainty here.
- **Progress:**
  - The presentation chain is `NetworkWeaponShooter` → `GunDispersionGlobalSettings.GetUiDiameter` → `GunCrosshair.SetAimingDiameters`; the runtime model is `GunDispersionModel` in `Assets/Game/Scripts/Gameplay/Robots/GunDispersion.cs`.
  - Dynamic dispersion is `clamp01(vehicleSpeed01 * 0.5 + turretSpeed01 * 0.5 + shotDispersion01)`, then interpolated between the weapon's minimum and maximum dispersion. Vehicle speed is horizontal movement normalized by runtime max speed; turret contribution uses actual turret-local yaw speed and has a 5% dead zone; a shot contributes a temporary `0.5` factor.
  - Hull rotation, gun pitch, camera movement/rotation, target lock, reload state, and ammo state are not direct growth inputs. Vertical movement is ignored because the movement delta clears `y` before speed calculation.
  - The active Client scene supplies explicit fully-aimed profile diameters from `38` to `366` UI pixels for zoom steps `-2` through `7`; only dispersion above the fully aimed value adds bloom. The bloom is larger at higher zoom, using the configured distance-to-zoom interpolation.
  - Growth and shrink are smoothed: global `expandTime` controls expansion toward a larger target, weapon `aimTime` controls settling toward a smaller target, and HUD diameter smoothing uses `uiDiameterLerpSpeed`.
- **Validation:** Static source/reference audit completed; inspected the focused `LegacyGunDispersionUiPlayModeTests` formula coverage; `git diff --check -- TASK.md` passed. No implementation files were changed. The working tree also contains unrelated modified font assets, which were left untouched.
- **Result:** The convergence circle grows while the vehicle moves horizontally, the turret rotates above its dead zone, or immediately after firing; simultaneous inputs add together up to the maximum dispersion. It returns toward the configured fully-aimed profile diameter after the inputs stop and the shot bloom settles.
- **Remaining limitations:** No live Unity visual reproduction was run; the conclusion is based on the current source, scene serialization, mechanic documentation, and focused formula tests.
- **Completed:** 2026-11-03

## Task: Stabilize camera when controlled robot is destroyed

- **Status:** IN_PROGRESS
- **Started:** 2026-11-03
- **User request:** Fix the camera jump that occurs when the player's robot is destroyed. Before the destruction animation, detach the robot rig to world space with a null parent so the camera remains stable.
- **Scope:** Trace the camera follow target, robot root/mover hierarchy, and destruction lifecycle; implement the smallest focused fix at the correct ownership boundary; preserve existing destruction behavior for gameplay and visuals.
- **Acceptance criteria:** The camera does not jump when the controlled robot dies; the death animation continues from the same world-space pose after detachment; normal live camera following and non-player robot destruction remain unchanged; focused validation/build checks pass.

### Progress

- Read the global Codex memory, task-journal recovery workflow, gameplay-logic separation workflow, background-testing workflow, and existing project task journal before repository inspection.
- Confirmed `DeathLogic.Death()` detaches the local camera before debris detachment, while `CameraController.HandleClientDeath()` can call the same operation again. If the detached rig disappears during network teardown between those callbacks, the second call currently replaces the saved death pivot with the camera-point position.
- **Decision:** Keep camera ownership in `CameraController`, make its death detachment idempotent, and preserve the first world-space pivot capture. Add a focused teardown-order regression beside the existing death-camera tests; update `docs/codex-game/robot-control.md` with the lifecycle boundary.
- Added `_deathRigDetached` guarding to `CameraController.DetachFromVehicleForDeath()` so the rig and camera point are detached once and the initial fallback pivot remains authoritative through teardown.
- Added `DeathDetachKeepsFirstPivotWhenRigIsDestroyedBeforeSecondDeathCallback` to `CameraRuntimeZoomOverridePlayModeTests`; documented the pre-animation detach and repeated-callback safety in `docs/codex-game/robot-control.md`.
- **Validation:** `git diff --check` passed. `dotnet build Assembly-CSharp.csproj --no-restore --nologo -v:minimal` passed with 0 errors and the existing 4 `System.Net.Http`/Unity deprecation warnings. `dotnet build Game.PlayMode.Tests.csproj --no-restore --nologo -v:minimal` passed with 0 warnings/errors.
- **Result:** The camera death detach is now one-shot and preserves the first world-space rig pivot even if the detached rig is destroyed before the second death callback. The destruction animation still starts after the local rig/camera point leave the networked robot hierarchy.
- **Remaining limitation:** Unity PlayMode execution was not run because the project Editor is currently open; the new regression is compiled with the test assembly and should be run in the existing focused death-camera batch suite after the Editor is closed.
- **Completed:** 2026-11-03.
- **Status:** COMPLETED

## Task: Add master volume control to robot engine profiles
- **Status:** COMPLETED
- **Started:** 2026-11-03
- **User request:** Add a common volume slider to `RobotEngineProfile` so all engine sounds in one profile can be reduced together because they are louder than other game sounds.
- **Scope:** Expose one profile-level master volume in the existing engine profile data and apply it consistently to loops and one-shot engine clips without bypassing the shared SFX volume pipeline.
- **Acceptance:** The slider is visible/editable in the engine profile inspector, affects idle/run/high/start/acceleration/stop playback, preserves the existing per-layer balance, and the project compiles cleanly.
- **Progress:** The profile already had a `masterVolume` range field, but runtime application covered only the idle/run/high loop layers; start, acceleration, and stop one-shots used full playback volume.
- **Implementation:** Added an explicit `Master Volume (All Engine Sounds)` Inspector field with tooltip, routed start/acceleration/stop playback through the profile master-volume helper, and documented that the multiplier preserves each resource's own volume balance.
- **Validation:** `dotnet build Assembly-CSharp.csproj --no-restore --nologo -v:minimal` passed with 0 errors and 4 existing warnings. `git diff --check` passed. Focused assertions confirmed all three one-shot routes and all eight serialized engine profiles use the master-volume setup.
- **Result:** Each robot engine profile now has one adjustable 0–1 master-volume slider that controls every engine loop and one-shot sound while still respecting global SFX volume and per-clip volume.
- **Limitations:** Unity Play Mode audio audition was not run; the slider can be tuned in the `AudioManager` prefab under `Engine Profiles` after Unity refreshes the changed scripts.
- **Completed:** 2026-11-03

## Task: Diagnose missing sounds in ClientVehicleTestMode

- **Status:** IN_PROGRESS
- **Started:** 2026-11-03
- **User request:** Explain why some sounds cannot be heard in the test mode `ClientVehicleTestMode`.
- **Scope:** Trace test-mode startup, active scene/object configuration, audio manager/resource registration, sound playback calls, listener/mixer/channel state, and client/server authority or lifecycle conditions that can suppress sounds; do not change implementation files unless explicitly requested.
- **Acceptance criteria:** Identify the concrete reason(s) for missing sounds, name the relevant files and conditions, distinguish confirmed causes from hypotheses, and record validation and limitations here.
- **Progress:** Read the global Codex instructions, journal workflow, project journal, gameplay-logic separation workflow, and memory history. No exact dedicated audio-diagnosis workflow was found; proceed with focused repository inspection.
- **Next steps:** Inspect `ClientVehicleTestMode`, the active audio stack, all callers used by the test mode, and relevant scene/prefab serialization; then update this entry with findings and close it.
- **Progress checkpoint:** Confirmed `ClientVehicleTestMode` starts a local FishNet host, disables only the normal `Camera` component, and uses `ClientVehicleTestCamera` without an `AudioListener`. The normal Client camera keeps an enabled `AudioListener`, and the additive `Map` scene adds another enabled `GameplayCamera` listener; spatial SFX use a 4–50 metre 3D attenuation range. This can make world sounds quiet, distant, or listener-dependent in the test mode.
- **Progress checkpoint:** Confirmed `Combat_Turret_Rotation` and `Combat_Turret_Rotation_Finished` are called by `VehicleTurretRotationController` but are absent from `AudioManager.prefab`; `AudioResources.GetResource` therefore returns null and `AudioManager.Play` silently returns. `Map_Industry` and `Map_Wind` are serialized with element volume `0`, and no current caller starts them. The test panel itself is IMGUI, so its buttons do not use `ButtonComponent` click/hover audio; matchmaking/result/progression sounds also require flows the test mode does not automatically execute.
- **Validation checkpoint:** All 58 clip GUIDs referenced by `AudioManager.prefab` resolve to project `.meta` files. The working tree had pre-existing changes in `Client.unity`, font assets, and `TASK.md`; no implementation files were changed by this investigation.
- **Status:** COMPLETED
- **Completed:** 2026-11-03

## Task: Add engine power to API vehicle parameters window

- **Status:** IN_PROGRESS
- **Started:** 2026-11-03
- **User request:** Pull the new engine-power parameter from the API and add it to the parameters window like the existing parameters; leave final positioning for later adjustment.
- **Scope:** Trace the API vehicle DTO/model, mapping and selected-vehicle parameter presentation, add the engine-power field through the existing data path, and expose it in the existing parameters window without changing layout positioning or unrelated UI behavior.
- **Acceptance criteria:** The API engine-power value deserializes and reaches the selected vehicle data; the parameters window has a dedicated engine-power value/label following the existing parameter pattern; no manual position tuning is introduced; focused validation passes and unrelated working-tree changes remain untouched.
- **Progress:** Read the global Codex instructions, repository journal, task-journal recovery workflow, gameplay/UI ownership guidance, and archive history. No exact API-parameter workflow was found.
- **Discovery checkpoint:** The backend API exposes `enginePower` as a positive `float` on `/vehicles`, `/vehicles/graph`, the player profile's owned-vehicle payload, and user-vehicle payloads. The client parameter flow is `VehiclesManager.VehicleLite` → `VehicleSlotVehicleData` → the shared scene-owned `RightInfoPanel`; the active panel already has a Mobility section and serialized TMP references, while the inactive legacy panel is not used at runtime.
- **Ownership decision:** Keep API deserialization in the API endpoint model, carry the value through the menu presentation DTO, and render it in `RightInfoPanel`. Add one serialized scene row for the label/value pair and leave its authored position at the duplicated existing mobility-row placement for later user adjustment.
- **Implementation checkpoint:** Added `enginePower` to the vehicle catalog/graph/profile menu DTOs and offline menu data, mapped it through `VehicleSlotVehicleData`, rendered it in both MainMenu and DevelopmentTree contexts, and added a serialized `EnginePower` label/value row to the active shared panel in `Client.unity`. Updated `api-resources.md` and `menus-settings-progression.md`; the existing mobility rows and panel placements remain unchanged.
- **Validation checkpoint:** Scene ID/reference checks confirmed the new Mobility row, label/value TMP objects, and `RightInfoPanel.enginePower` binding are internally consistent. `git diff --check` passed. `dotnet build Assembly-CSharp.csproj --no-restore --nologo -v:minimal` passed with 0 errors and the existing 4 warnings; `dotnet build Game.PlayMode.Tests.csproj --no-restore --nologo -v:minimal` passed with 0 errors and 0 warnings.
- **Result:** API `enginePower` now deserializes from the vehicle catalog and related DTOs, reaches selected vehicle presentation data, and displays in the shared parameters panel for both MainMenu and DevelopmentTree. The new row is authored under Mobility at a provisional position for later adjustment.
- **Remaining limitations:** Unity Play Mode/manual visual inspection was not run, so final text fit and the provisional row position should be checked in the open Unity Editor. No backend files or unrelated working-tree changes were modified.
- **Status:** COMPLETED
- **Completed:** 2026-11-03

## Task: Diagnose RightInfoPanel behavior in ClientMenuTestMode

- **Status:** IN_PROGRESS
- **Started:** 2026-11-03
- **User request:** Explain why the unified `RightInfoPanel` does not work in `ClientMenuTestMode`, and determine whether the test menu follows different behavior from the real menu.
- **Scope:** Compare `ClientMenuTestMode` startup, menu routing, scene wiring, selected-vehicle/profile data flow, and shared panel lifecycle against the normal MainMenu and DevelopmentTree flows; do not change implementation until the concrete cause is confirmed.
- **Acceptance criteria:** Identify the exact reason the panel fails or differs in test mode, distinguish intentional test-mode behavior from a regression, name the relevant files/serialized references, and record validation and limitations.
- **Progress:** Read the global Codex instructions, task-journal recovery workflow, archive history, current project journal, and the menu/progression ownership documentation. No exact dedicated workflow for this diagnosis was found; proceed with focused repository inspection.
- **Next steps:** Inspect `ClientMenuTestMode`, `MainMenu`, `DevelopmentTree`, `RightInfoPanel`, `MenuManager`, and the Client scene wiring, then record the confirmed cause and close this diagnostic entry.
- **Discovery checkpoint:** `ClientMenuTestMode` uses the same `MenuManager.OpenMenu(MenuType.MainMenu)`, `MainMenu`, `RobotView`, and `RightInfoPanel` lifecycle as the normal menu; only profile/vehicle data comes from local `ClientMenuTestMode` factories instead of HTTP/RPC. The shared panel therefore does not require a separate test-mode implementation.
- **Discovery checkpoint:** The current `Client.unity` serialization is incomplete: the active `MainMenu` component at line 33559 and active `DevelopmentTree` component at line 29380 both have `rightInfoPanel: {fileID: 0}`, and the shared `RightInfoPanel` GameObject at line 5154 is inactive. Because an inactive GameObject does not run `RightInfoPanel.Awake`, it never subscribes to `MenuManager.OnEnable`; because both coordinator references are null, vehicle/tree updates are also discarded.
- **Result:** The failure is a scene-wiring/lifecycle regression, not a different test-menu principle. The offline mode intentionally bypasses server data and runs the same presentation path after creating local profile data. It currently exposes the missing serialized references and inactive shared panel immediately.
- **Validation:** Static inspection confirmed `ClientMenuTestMode.Start` opens `MainMenu` and calls local `ProfileServer.UpdateProfile`; `RobotView` calls `MainMenu.UpdateVehicleInfo`; `DevelopmentTree` calls `RightInfoPanel.ShowDevelopment`; and the three scene conditions above are present. No implementation files were changed.
- **Remaining limitations:** This diagnosis was static; Unity PlayMode was not run. The concrete repair is to assign `{fileID: 2147000001}` to both menu fields and leave the shared panel root active in the saved scene so its `Awake` can manage visibility.
- **Status:** COMPLETED
- **Completed:** 2026-11-03

## Task: Repair MainMenu and DevelopmentTree shared RightInfoPanel wiring

- **Status:** IN_PROGRESS
- **Started:** 2026-11-03
- **User request:** Make both real menus fully functional after the `RightInfoPanel` unification, while keeping only generated local data in `ClientMenuTestMode`.
- **Scope:** Repair serialized scene references and shared-panel lifecycle state, preserve normal server-backed menu flows, preserve offline test-mode data generation, and validate builds plus static menu/layout wiring.
- **Acceptance criteria:** MainMenu and DevelopmentTree reference the single shared panel; the panel starts active enough for `Awake` and hides/shows itself through menu routing; offline test mode still uses only local generated profile/vehicle/tree data; normal menu paths remain server-backed; focused validation passes.
- **Progress:** Read the global Codex instructions, task-journal recovery workflow, archive history, current journal, and menu/progression documentation. Existing unrelated working-tree changes remain untouched.
- **Next steps:** Patch the Client scene serialization, verify both menu references and panel activation/layout/button wiring, build the affected assemblies, and close this entry with validation results.
- **Implementation checkpoint:** Restored `MainMenu.rightInfoPanel` and `DevelopmentTree.rightInfoPanel` to the shared component `{fileID: 2147000001}` and restored the shared `RightInfoPanel` root to active in `Assets/Game/Scenes/Client.unity`, allowing `Awake` to subscribe to menu routing. No test-mode or normal server data logic was changed.
- **Validation checkpoint:** All 8 focused scene assertions passed: shared root active, both menu references resolved, legacy root inactive, `ButtonBattle` reparented, and both authored placements/sizes preserved. Runtime and PlayMode assemblies built with 0 errors; only existing dependency warnings remain in runtime build. `git diff --check -- TASK.md` passed; unrelated scene whitespace warnings remain outside this focused journal check.
- **Result:** Both real menus now use the same functional shared panel, while `ClientMenuTestMode` continues to generate only local profile/vehicle/tree data through its existing test factories. Normal profile and vehicle server fallbacks remain unchanged.
- **Remaining limitations:** Unity PlayMode/manual visual interaction was not run because the Unity Editor is already open; the saved scene wiring and C# compilation are validated.
- **Status:** COMPLETED
- **Completed:** 2026-11-03

## Task: Keep RightInfoPanel disabled by default

- **Status:** IN_PROGRESS
- **Started:** 2026-11-03
- **User request:** Keep `RightInfoPanel` disabled by default while preserving fully working MainMenu and DevelopmentTree behavior.
- **Scope:** Add a focused lifecycle owner that can initialize and activate the disabled shared panel when `MenuManager` opens the relevant menu; preserve scene positions, button context, normal server-backed data, and offline generated test data.
- **Acceptance criteria:** The panel GameObject is inactive in the saved scene by default; MainMenu and DevelopmentTree still receive the same shared panel; lifecycle initialization and Buy/Research listeners work despite the panel starting inactive; builds and static layout/reference checks pass.
- **Progress:** Read the global instructions, task-journal workflow, archive history, current journal, and menu/progression documentation. Existing unrelated working-tree changes remain untouched.
- **Next steps:** Implement the active Canvas-level lifecycle host, change the panel default state, update documentation, validate scene wiring and builds, then close this entry.
- **Implementation checkpoint:** Added `RightInfoPanelHost` on the active Canvas. It initializes the inactive panel, owns the `MenuManager.OnEnable` subscription, and delegates context activation. `RightInfoPanel` now has idempotent initialization so its progression button listeners and cached layout work even when its GameObject starts inactive.
- **Implementation checkpoint:** Set the shared panel inactive by default in `Assets/Game/Scenes/Client.unity`; kept both menu references, the legacy duplicate state, button parenting, and authored placements unchanged. Updated `docs/codex-game/menus-settings-progression.md` with the lifecycle boundary.
- **Validation checkpoint:** All 11 focused scene/code/documentation assertions passed. `dotnet build Assembly-CSharp.csproj --no-restore --nologo -v:minimal` and `dotnet build Game.PlayMode.Tests.csproj --no-restore --nologo -v:minimal` passed with 0 errors; runtime retains only existing dependency/obsolete API warnings. `git diff --check` passed for journal and documentation files.
- **Result:** `RightInfoPanel` is now disabled in the saved scene by default but is fully initialized and shown only when MainMenu or DevelopmentTree is opened. Test mode still uses only generated local data; normal menu data flow is unchanged.
- **Remaining limitations:** Unity PlayMode/manual interaction was not run because the Unity Editor is already open; Unity should reload `Client.unity` before visual verification.
- **Status:** COMPLETED
- **Completed:** 2026-11-03

## Task: Regenerate ClientMenuTestMode data and repair panel updates
- **Status:** IN_PROGRESS
- **Started:** 2026-11-03
- **User request:** Regenerate all generated test-menu data from scratch because vehicle selection did not reliably change the parameters shown in the shared `RightInfoPanel`; keep all menu panels functional.
- **Scope:** Inspect the test-data source, profile/selection propagation, MainMenu and DevelopmentTree panel consumers, and the shared panel contract. Replace ambiguous test records with a coherent deterministic roster and repair only the test-mode synchronization path without changing real server behavior.
- **Acceptance criteria:** Every generated vehicle has a unique stable identity and visibly distinct coherent stats; selecting a vehicle updates the selected state, slots, and `RightInfoPanel`; development-tree data remains usable; existing panel placement/visibility behavior remains unchanged; project and focused test assemblies compile.
- **Progress:** Traced `ClientMenuTestMode` → `ProfileServer` → `RobotView` → `RightInfoPanel`; confirmed selection is ID-based and found the real defect in the generator: same-level records received nearly identical combat values. Replaced the stale serialized roster with a deterministic generated 15-vehicle catalog, regenerated profile history, and kept the scene array empty so old data cannot leak into runtime.
- **Changes:** Added role/variant-specific HP, armor, mobility, firepower, accuracy, reload, view range, and damage values; kept valid `RobotRegistry` codes and unique IDs; added a reflection-based PlayMode regression test for selection, distinct stats, and two-faction tree coverage; updated menu documentation.
- **Validation:** `dotnet build Assembly-CSharp.csproj --no-restore --nologo -v:q` passed with 0 errors and 4 pre-existing warnings; `dotnet build Game.PlayMode.Tests.csproj --no-restore --nologo -v:q` passed with 0 errors; static scene/layout checks passed for the cleared roster, shared panel wiring, default-inactive state, and both preserved placements. Unity PlayMode/manual visual interaction was not run because the Unity Editor is already open.
- **Next steps:** Reload the changed scene in Unity and click several generated vehicle slots in MainMenu and DevelopmentTree to visually confirm the expected values and panel context.
- **Status:** COMPLETED
- **Completed:** 2026-11-03

## Task: Unify duplicated RightInfoPanel in MainMenu and DevelopmentTree

- **Status:** IN_PROGRESS
- **Started:** 2026-11-03
- **User request:** Make `RightInfoPanel` a single reusable element instead of duplicated panels in `MainMenu` and `DevelopmentTree`, while preserving the current appearance, positions, behavior, and context-specific buttons.
- **Scope:** Trace both UI hierarchies, scripts, prefabs/scenes, serialized references, and button visibility/state ownership; extract or reuse one focused panel owner; keep the existing layout and interactions unchanged; verify dimensions, offsets, anchors, and runtime behavior for both contexts.
- **Acceptance criteria:** Only one reusable `RightInfoPanel` implementation/asset is maintained; `MainMenu` and `DevelopmentTree` show the correct context-specific buttons; existing positions, sizes, styling, and interactions remain visually and functionally equivalent; focused validation and static layout/reference checks pass; unrelated working-tree changes remain untouched.
- **Progress:** Read the global Codex instructions, repository journal, task-journal recovery workflow, and archive history. No exact workflow for this UI refactor was found; use the project's existing UI style and focused ownership rules.
- **Next steps:** Inspect the current `RightInfoPanel` hierarchy and all related scripts/assets, record the ownership boundary and baseline layout values, then implement the smallest safe unification and validate serialized references and UI dimensions.
- **Discovery checkpoint:** `Client.unity` contains `RightInfoPanel` roots `202115661` under `DevelopmentTree/AnimationPanel` and `994464634` under `MainMenu/AnimationPanel`. The development panel owns the shared stats plus `ButtonBuy`/`ButtonResearch`; the main-menu panel owns the same visual/stat structure plus `ButtonBattle`. The common top-level child layout is identical after the MainMenu offset `{x: 18.04, y: 73}`, while root placements remain distinct: DevelopmentTree `{position: -249.69,-165.46; size: 499.38,-330.93}` and MainMenu `{position: -267.72974,-12.404846; size: 535.4595,-24.810421}`. Both panels use the same right-side anchors and sibling index `3`.
- **Ownership decision:** Keep one `RightInfoPanel` instance as a focused UI owner under the shared Canvas, move it to the active menu placement on `MenuManager.OnEnable`, and switch only the context-specific buttons/content. `MainMenu` and `DevelopmentTree` remain coordinators and delegate panel presentation/data/actions to this owner.
- **Implementation checkpoint:** Added one shared `RightInfoPanel` owner under the Canvas, migrated both menu coordinators to it, preserved the common content layout, and reparented `ButtonBattle` into the shared panel while retaining its local transform. The former MainMenu hierarchy remains inactive as a safe serialized legacy container and is not rendered or scripted at runtime.
- **Validation checkpoint:** Static reference and layout checks passed: the shared root and context references resolve, the legacy MainMenu root is inactive, both exact root placements and the MainMenu content offset match the recorded baseline, and the context buttons are attached to the shared owner. `Assembly-CSharp.csproj` and `Game.PlayMode.Tests.csproj` both build successfully; scoped diff whitespace checks pass.
- **Result:** MainMenu uses the MainMenu placement and `ButtonBattle`; DevelopmentTree uses the DevelopmentTree placement and `ButtonBuy`/`ButtonResearch`, with the existing data formatting and actions centralized in the shared owner.
- **Remaining limitations:** Unity PlayMode/manual visual QA was not run in this session, so the final visual confirmation should be made in the open Unity Editor. No unrelated working-tree changes were modified.
- **Status:** COMPLETED
- **Completed:** 2026-11-03

## Task: Remove robots without prefabs from ClientVehicleTestMode

- **Status:** IN_PROGRESS
- **Started:** 2026-11-03
- **User request:** Remove from `ClientVehicleTestMode` the robots that do not have prefabs.
- **Scope:** Identify how test-mode robot entries are selected and validate their prefab references, then exclude only entries whose configured prefab asset is missing or invalid without changing normal gameplay robot registration.
- **Acceptance criteria:** `ClientVehicleTestMode` no longer offers or spawns robots without prefabs, valid test-mode robots remain available, focused validation passes, and unrelated working-tree changes remain untouched.
- **Progress:** Re-read the global instructions, gameplay ownership workflow, task-journal recovery workflow, and current repository journal. The next step is to inspect the test-mode roster and prefab-reference validation path.
- **Next steps:** Trace the test-mode robot list and its spawn/selection owner, implement the smallest focused filtering fix, update relevant documentation or regression coverage, validate, then close this entry.
- **Discovery checkpoint:** `VehicleTestSceneController.LoadVehiclesAsync` copied every API vehicle into the visible catalog, while prefab validation happened only when spawning. Bot random selection already used `HasVehiclePrefab`; `RobotRegistry.GetFirstCode` also skipped missing prefabs. The focused fix belongs in the test controller's catalog-loading owner, not in the shared registry, because development-only no-prefab entries must remain available to other systems.
- **Implementation checkpoint:** Added `FilterVehiclesWithPrefabs` to `VehicleTestSceneController`, applied it immediately after API loading, made selected-prefab lookup use the registry/resource fallback consistently, documented the test catalog contract, and added `VehicleTestCatalogPlayModeTests` covering valid and missing prefab entries.
- **Validation checkpoint:** `dotnet build Assembly-CSharp.csproj --no-restore --nologo -v:minimal` passed with `0` errors and the existing `System.Net.Http`/obsolete API warnings. `dotnet build Game.PlayMode.Tests.csproj --no-restore --nologo -v:minimal` passed with `0` errors and `0` warnings. Scoped `git diff --check` passed. Full `git diff --check` reports only pre-existing trailing whitespace in the already modified `Assets/Game/Scenes/Client.unity`; Unity batch PlayMode was not run because Unity Editor processes for this project are already open.
- **Result:** The Client vehicle test catalog now contains only API vehicles with a valid prefab in the configured registry/resource lookup, so missing-prefab robots cannot be selected or spawned from the test panel. Shared registry contents remain unchanged.
- **Remaining limitations:** Unity PlayMode execution was not run; the new regression is compile-validated and should be run after closing the currently open Unity Editor instances.
- **Status:** COMPLETED
- **Completed:** 2026-11-03

## Task: Remove horizontal turret aim dead zones

- **Status:** IN_PROGRESS
- **Started:** 2026-11-03
- **User request:** Remove the small-movement thresholds so a player can make small horizontal gun adjustments and see the convergence reticle move.
- **Scope:** Remove the horizontal turret, dispersion, and aim-update dead zones that suppress small yaw changes, preserve unrelated turret-rotation sound behavior, and validate the change.
- **Acceptance criteria:** Small horizontal aim changes reach the turret and reticle without dead-zone suppression, project builds, focused static checks pass, and unrelated working-tree changes remain untouched.
- **Progress:** Re-read the relevant gameplay ownership workflow and confirmed the current turret hold, dispersion, input-send, and server-dispersion thresholds that can suppress micro-yaw changes.
- **Next steps:** Inspect all affected call sites and regression coverage, implement the focused threshold removal, update mechanic documentation, validate the build and diff, then close this entry.
- **Implementation checkpoint:** Removed the turret's `0.25`-degree hold dead zone, removed the `5%` turret-speed dispersion dead zone, and stopped the input controller from replacing sub-threshold yaw/pitch packets with the previous values. Set the active server setting to zero for compatibility and updated the turret regression test and ownership documentation. Turret-rotation audio event calls and assets were not edited.
- **Validation checkpoint:** `dotnet build Assembly-CSharp.csproj --no-restore --nologo -v:minimal` passed with `0` errors and the existing `System.Net.Http`/obsolete API warnings. `dotnet build Game.PlayMode.Tests.csproj --no-restore --nologo -v:minimal` passed with `0` errors and `0` warnings. `git diff --check` passed; `AudioEventNames.cs` is unchanged and the remaining working-tree changes are limited to the requested code, test, docs, and journal files.
- **Result:** Sub-degree horizontal turret corrections now advance every frame toward the target, dispersion reacts to any nonzero turret yaw speed, and network aim packets preserve the current quantized yaw/pitch instead of suppressing micro-adjustments. The existing reticle smoothing remains unchanged because it is interpolation, not a dead zone.
- **Remaining limitations:** Unity Play Mode/manual aiming verification was not run; the final feel should be checked by moving the gun by a very small amount in `ClientVehicleTestMode`.
- **Status:** COMPLETED
- **Completed:** 2026-11-03
- **Result:** The missing sounds have multiple causes: the test camera does not own an audio listener while the normal Client and additive Map cameras keep listeners active; spatial SFX are attenuated from those listeners over 4–50 metres; turret rotation events are called but not registered; map ambience events are configured at zero volume and are not called; and the IMGUI/test-mode path bypasses several normal UI, matchmaking, countdown, and result triggers. A server-only startup or zero `SFXVol`/`UIVol` PlayerPrefs value can additionally suppress whole categories.
- **Validation:** Static source and serialized-asset audit completed; 58/58 referenced audio clip GUIDs resolve; `git diff --check -- TASK.md` passed; no runtime Unity playtest was performed.
- **Remaining limitations:** The exact listener chosen at runtime and the current PlayerPrefs volume values require a live Play Mode check. No implementation change was requested or made.

## Task: Fix shot audio in ClientVehicleTestMode

- **Status:** IN_PROGRESS
- **Started:** 2026-11-03
- **User request:** Do not touch turret-rotation sounds; find and fix why the shot sound is inaudible in `ClientVehicleTestMode`, with the expectation that the same fix restores other missing sounds.
- **Scope:** Fix the test-mode audio listener/lifecycle path and verify the existing shot event reaches a valid audible `AudioSource`; preserve all turret-rotation event names, callers, and registrations.
- **Acceptance criteria:** Player and bot shot sounds are audible from the active test camera, no duplicate active listeners remain in the test path, turret-rotation audio files/code are unchanged, focused validation passes, and unrelated working-tree changes remain untouched.
- **Progress:** Re-read the global instructions, gameplay ownership workflow, and latest journal. The previous investigation identified that `ClientVehicleTestMode` disables only the normal camera, while `Client` and additive `Map` retain active listeners and the test camera has none.
- **Next steps:** Inspect the current camera/listener ownership and shot playback path, implement the smallest focused fix, validate the diff and project build if feasible, then close this entry.
- **Implementation checkpoint:** Added `ClientVehicleTestAudioRouting` as the focused test-audio owner, attached a disabled `AudioListener` plus the routing component to `ClientVehicleTestCamera`, and documented the ownership boundary in `docs/codex-game/vehicle-test-scene.md`. The router enables only the test-camera listener and disables other active listeners again after additive scene loads, then restores them on teardown. No turret-rotation source or event asset was changed.
- **Validation checkpoint:** `dotnet build Assembly-CSharp.csproj --no-restore` passed with 0 errors; existing project warnings remain. `git diff --check` passed, the scene/script references were statically verified, and the protected turret-rotation controller plus audio-event registry are unchanged.
- **Result:** Shot SFX now use the active test camera as the single listener, so the existing player and bot shot playback path is audible at the camera position; the same listener correction applies to other spatial SFX in `ClientVehicleTestMode` without changing turret-rotation sounds.
- **Remaining limitations:** Unity PlayMode/manual audio verification was not run. Existing mixer volume or `PlayerPrefs` mute settings can still suppress audio independently of this listener fix.
- **Status:** COMPLETED
- **Completed:** 2026-11-03

## Task: Fix duplicate listeners and moving vehicle audio in ClientVehicleTestMode

- **Status:** IN_PROGRESS
- **Started:** 2026-11-03
- **User request:** Remove extra audio listeners and fix vehicle movement audio that stays at its original position after the vehicle moves.
- **Scope:** Inspect the current test-mode listener routing and the movement/engine audio source ownership, then make focused fixes without changing turret-rotation sounds or unrelated working-tree changes.
- **Acceptance criteria:** Only the intended test listener remains active, movement audio follows the moving vehicle/source, focused validation passes, and unrelated changes remain untouched.
- **Progress:** The previous task added `ClientVehicleTestAudioRouting` to enable the test-camera listener and disable listeners found in the loaded scenes. The new report indicates that this routing may be leaving or creating duplicate listeners, and that the movement audio source may be played from a non-moving owner.
- **Next steps:** Audit listener lifecycle and movement-audio call sites/serialized sources, implement focused fixes, validate the build and diff, then close this entry.
- **Discovery checkpoint:** Confirmed `Map/GameplayCamera` had a serialized `AudioListener` even though VehicleTest disables that camera; it was an extra listener in the additive test scene. Confirmed `TrackedVehicleAnimator` and `WalkerAnimationController` run on the vehicle root, while the `VehicleMovementController` `CharacterController` moves the child `objectMover` transform, so movement audio parent/positions stayed at the root's initial location.
- **Implementation checkpoint:** Removed the `Map/GameplayCamera` listener, made `ClientVehicleTestAudioRouting` use only the serialized test-camera listener without runtime `AddComponent`, and restore disabled listeners when the router is disabled. Rebound tracked, walker, hydraulic, step, and turn movement sounds to `vehicleRoot.objectMover.transform` with a root fallback. Updated `docs/codex-game/vehicle-test-scene.md` to document the ownership boundary.
- **Validation checkpoint:** `dotnet build Assembly-CSharp.csproj --no-restore --nologo -v:minimal` passed with 0 errors and 4 existing warnings. `git diff --check` passed; static validation found 2 intentional listeners in `Client` (normal + test), 0 in `Map`, movement audio bound to `objectMover`, and no changes in the turret-rotation files.
- **Result:** The additive map no longer contributes an extra `AudioListener`, and the vehicle movement/step/hydraulic sources follow the transform that actually moves instead of remaining at the vehicle root's spawn position.
- **Remaining limitations:** Unity PlayMode/manual audio verification was not run; the final audible result should be confirmed by driving a tracked vehicle and, if needed, a walker in `ClientVehicleTestMode`.
- **Status:** COMPLETED
- **Completed:** 2026-11-03

## Task: Explain small horizontal turret aim movement

- **Status:** IN_PROGRESS
- **Started:** 2026-11-03
- **User request:** Explain why the aiming/convergence reticle does not move when the turret is moved only slightly horizontally.
- **Scope:** Trace the turret yaw input, movement dead zones, and convergence/reticle update path without changing implementation files.
- **Acceptance criteria:** Identify the exact threshold or smoothing behavior responsible, cite the relevant owner files, and distinguish a true no-op from a visually smoothed movement.
- **Next steps:** Inspect the turret input/rotation and dispersion presentation code, then close this diagnostic entry with the confirmed explanation.
- **Discovery checkpoint:** The turret controller intentionally holds target yaw when the error is at or below `0.25` degrees (`AimHoldDeadZoneDegrees`). The convergence model also ignores turret motion below `5%` of the configured turret rotation speed, and client input/network serialization suppresses yaw updates below `0.03` degrees. The HUD reticle position then follows its projected aim point with exponential horizontal smoothing (`uiReticleHorizontalLerpSpeed`, `10` in the active server settings prefab).
- **Result:** Very small horizontal mouse movement can produce no visible turret/reticle change because it remains inside the `0.25`-degree turret hold zone; if the turret does move, the convergence circle may still barely change because its dispersion contribution is zero below the `5%` speed threshold and the HUD position is smoothed.
- **Validation:** Static source and serialized-setting audit completed; no implementation files changed.
- **Remaining limitations:** No live Play Mode measurement of the current mouse delta was run, so the exact observed threshold depends on the active vehicle's runtime `TurretTraverseSpeed`.
- **Status:** COMPLETED
- **Completed:** 2026-11-03

## Task: Apply server-provided engine power to robot acceleration

- **Status:** IN_PROGRESS
- **Started:** 2026-11-03
- **User request:** Add the server-provided engine power parameter for the eight robot variants and make it directly affect robot acceleration; treat `1000` as the current baseline, so lower power values take longer to reach top speed. The supplied data is Iron Alliance `IA Skirmisher=700`, `IA Strider=860`, `IA Bulwark=820`, `IA Juggernaut=940`; Nova Syndicate `Nova Wisp=360`, `Nova Flicker=420`, `Nova Aegis=460`, `Nova Titan=520`.
- **Scope:** Trace the server stat transport and movement acceleration owner, add the power field at the correct domain boundary, preserve current behavior at power `1000`, integrate the eight server values, and validate compilation/static consistency without changing unrelated movement behavior.
- **Acceptance criteria:** The server payload/model can carry engine power; the runtime movement controller receives it; acceleration scales against `1000` while maximum speed remains unchanged; missing/legacy values safely preserve current behavior; each supplied robot name maps to the requested value; focused documentation and validation are recorded.
- **Progress:** Global instructions, the repository journal, the task-journal recovery workflow, and the gameplay-logic separation workflow were read. No task-specific repository search has been run yet.
- **Next steps:** Inspect `RightInfoPanel.cs`, server DTOs/stat mappings, robot movement owners, and mechanic documentation; then implement the smallest focused change and validate it.

### Discovery checkpoint — 2026-11-03

- `git status --short` shows pre-existing modifications in `RightInfoPanel.cs`, API endpoint models/managers, main-menu vehicle UI, `Client.unity`, font data, and related documentation; these changes were present before this task and must be preserved.
- The repository already contains `enginePower` fields in vehicle API/UI models and `RightInfoPanel` is modified, so the task must trace whether the value currently stops at presentation instead of assuming the transport is missing.
- The gameplay workflow requires keeping authority/stat transport, movement simulation, and UI presentation as separate responsibilities; the movement change should remain in the runtime stats/movement owner rather than in `RightInfoPanel`.

### Design checkpoint — 2026-11-03

- Spawn flow loads `VehicleRuntimeStats` from `VehicleStatsProvider.GetAsync`, applies it to the server vehicle before spawn, then synchronizes the same stats to observers through `VehicleRoot.RuntimeStatsObserversRpc`.
- The existing `VehicleMovementController` already owns server-authoritative forward acceleration and has a separate runtime `acceleration` value, so no new coordinator or UI ownership is needed.
- Decision: normalize missing/non-positive engine power to `1000`; calculate the engine-power multiplier as `enginePower / 1000`; apply that multiplier only while the current input requests a higher speed in the same direction or from rest. Maximum speed, reverse cap, faction stopping multipliers, braking, and turret rotation remain unchanged for this iteration.
- The eight supplied robot values remain server data, not a hardcoded client name table: `VehicleLite.enginePower` flows through runtime stats and the synchronized RPC.
- Planned implementation files: `VehicleRuntimeStats.cs`, `VehicleMovementController.cs`, `VehicleRoot.cs`; documentation in `docs/codex-game/api-resources.md` and `docs/codex-game/robot-control.md`; focused edit-mode regression in `Assets/Editor/VehicleRuntimeStatsEditModeTests.cs`.

### Implementation checkpoint — 2026-11-03

- Added `VehicleRuntimeStats.DefaultEnginePower = 1000`, runtime normalization for missing/non-positive API values, and a testable linear acceleration scaling helper.
- Updated `VehicleMovementController` to store normalized engine power and apply the multiplier only when increasing requested speed; braking/coasting and maximum-speed limits retain existing behavior.
- Extended `VehicleRoot` observer synchronization so remote runtime stats retain the same engine power as the server.
- Updated `docs/codex-game/api-resources.md` and `docs/codex-game/robot-control.md` with the data path, baseline, and ownership boundary.
- Added `Assets/Editor/VehicleRuntimeStatsEditModeTests.cs` plus its Unity `.meta` file for fallback, scaling, and clone coverage.
- Existing pre-task modifications, including the `RightInfoPanel` engine-power presentation and scene wiring, were not overwritten.
- **Next steps:** run diff/static checks, compile the project, run the focused Unity test if an available batch-mode Unity executable can be identified, then close the journal entry.

### Validation checkpoint — 2026-11-03

- `dotnet build Assembly-CSharp.csproj --no-restore --nologo -v:minimal` passed with 0 errors and 4 existing warnings (System.Net.Http version conflicts and obsolete Unity PlayerSettings API).
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore --nologo -v:minimal` passed with 0 errors and existing warnings; the new edit-mode test source compiles in the editor assembly.
- `git diff --check` passed for all task-owned source, documentation, and journal files.
- A full working-tree `git diff --check` still reports trailing whitespace in pre-existing `Assets/Game/Scenes/Client.unity` changes at lines 17399, 57857-57858, 57944, and 58082; those scene changes were not edited by this task.
- Unity batch tests were not run because Unity Editor is already open on this project (`D:\Programing\UnityProjects\war-of-machines-unity`); no Unity process was stopped or manipulated.

### Completion checkpoint — 2026-11-03

- **Status:** COMPLETED
- **Result:** Server `enginePower` values now flow from `VehicleLite` into normalized `VehicleRuntimeStats`, across `VehicleRoot` observer synchronization, and into server-authoritative movement. Values use `1000` as the current baseline, so the supplied values produce proportional acceleration (`700` = 70%, `360` = 36%, etc.) while max speed and braking rules remain unchanged.
- **Changed files:** `Assets/Game/Scripts/Gameplay/Robots/VehicleRuntimeStats.cs`, `Assets/Game/Scripts/Gameplay/Robots/VehicleMovementController.cs`, `Assets/Game/Scripts/Gameplay/Robots/VehicleRoot.cs`, `docs/codex-game/api-resources.md`, `docs/codex-game/robot-control.md`, and focused test files under `Assets/Editor`.
- **Preserved:** Pre-existing API/UI/scene changes, including `RightInfoPanel` engine-power presentation and the eight-value server DTO wiring, were left intact.
- **Remaining limitation:** Runtime movement and Unity test execution still need an in-game run after the open Editor is closed; compilation and static validation are successful.

## Task: Restore missing gameplay audio

- **Status:** IN_PROGRESS
- **Started:** 2026-11-03
- **User request:** Investigate and fix the regression where gameplay sounds are no longer audible, likely caused by recent test-mode changes.
- **Scope:** Trace the active gameplay audio listener, test-mode routing lifecycle, and recent working-tree changes; restore audible gameplay SFX without changing turret-rotation audio assets or unrelated gameplay behavior.
- **Acceptance criteria:** The active gameplay camera has a valid listener, test-mode routing does not disable all listeners or leave audio muted, focused validation passes, and unrelated changes remain untouched.
- **Progress:** Confirmed `CameraSync` uses `Map/GameplayCamera` during normal gameplay, but that camera currently has no `AudioListener`. Git history shows the listener was removed while the existing test-mode router already disables the Map listener during test mode, so normal gameplay was left without any active listener.
- **Implementation checkpoint:** Restored the serialized `AudioListener` on `Map/GameplayCamera` and updated `vehicle-test-scene.md` to document that normal gameplay owns this listener while the test router temporarily disables it.
- **Validation checkpoint:** Static scene checks passed with one `AudioListener` in `Map.unity` and two serialized listeners in `Client.unity`. The test-mode router still guards on `ClientVehicleTestMode.IsEnabled` and disables other active listeners. `dotnet build Assembly-CSharp.csproj --no-restore --nologo -v:minimal` passed with 0 errors and the existing `System.Net.Http` reference warnings. Focused `git diff --check` passed; no audio scripts or audio assets were modified.
- **Result:** Normal gameplay again has an active listener on the camera used by `CameraSync`, while ClientVehicleTestMode retains a single routed test listener and restores normal listener ownership outside test mode.
- **Remaining limitations:** Unity Play Mode audio playback was not manually run because the project Editor is already open; the scene/static fix and C# compilation are validated.
- **Status:** COMPLETED
- **Completed:** 2026-11-03

## Task: Diagnose sniper reticle dispersion after garage return

- **Status:** IN_PROGRESS
- **Started:** 2026-11-03
- **User request:** Investigate why, after leaving a battle for the garage and loading another battle without closing the game, the sniper-mode convergence circle becomes larger at zoom than the weapon's real dispersion; determine whether previous parameters are not restored or another state/lifecycle bug is involved, and explain the cause.
- **Scope:** Trace sniper-mode zoom, dispersion/convergence calculation, runtime vehicle/weapon stats, scene/lifecycle transitions between battle and garage, and any cached/static state that can survive the transition; inspect the currently open Unity Editor/runtime if useful without disrupting it.
- **Acceptance criteria:** Identify the concrete code/data path responsible or narrow the cause to a verified lifecycle/state mismatch, explain why the issue appears after garage return and why zoom exposes it, record affected files and validation evidence, and avoid unrelated implementation changes unless a fix is explicitly requested.
- **Progress:** Global instructions, the repository journal, task-journal recovery workflow, and gameplay-logic separation workflow were read. No implementation files have been changed for this task.
- **Next steps:** Inspect the current working tree and search sniper/dispersion/zoom/lifecycle owners, then compare first-battle and post-garage initialization paths and the open Editor state if accessible.

### Discovery checkpoint — 2026-11-03
- **Direct cause:** `NetworkWeaponShooter.ApplyCrosshairDispersion` passes the active zoom profile's `aimingCircleDiameter` into `GunDispersionGlobalSettings.GetUiDiameter`. With that override present, the HUD uses the configured profile diameter as the fully-aimed base and does not project the actual gun cone to screen pixels.
- **Evidence:** The current client profiles configure 80/125/189/366 px for sniper zoom levels 4/5/6/7. Projectile dispersion still uses the angular `GunDispersionModel` value, so a visually oversized ring can coexist with correct shot spread.
- **Regression point:** Commit `edc0ee36a` replaced the previous physical cone-to-screen calculation with the fixed profile-base calculation; the existing play-mode tests explicitly preserve the new behavior.
- **Lifecycle finding:** `RemoteServerSettings` is static and is requested only from `LobbyManager.OnStartClient`. Because the client/lobby scene survives garage-to-battle scene swaps, server dispersion settings are not refreshed on the next battle; editor vehicle test mode skips that request entirely. This can leave stale global settings, but it is secondary when the server values are unchanged.
- **Live inspection:** Unity Editor is running in vehicle test mode on the battle-result screen, not in active sniper view. No implementation files were changed.
- **Next steps:** Record the final explanation, validation evidence, and remaining limitation, then close this journal entry.

### Completion checkpoint — 2026-11-03
- **Status:** COMPLETED
- **Result:** Confirmed the oversized sniper circle is primarily a UI sizing regression/configuration mismatch: the ring uses fixed per-zoom `aimingCircleDiameter` values while real shot dispersion remains angular and stat-driven. Also identified a separate stale `RemoteServerSettings` lifecycle risk across garage-to-battle reloads.
- **Validation:** Traced the runtime call chain, compared the pre-refactor implementation and commit history, checked serialized zoom values and existing tests, and inspected the live Unity Editor state. `git diff --check -- TASK.md` passed.
- **Remaining limitations:** The live Editor was on the battle-result screen, so the exact current sniper frame was not reproduced interactively. No implementation fix was applied because the request was diagnosis/explanation only.

### Follow-up checkpoint — 2026-11-03
- **Clarification:** `CameraController.Init` resets the new vehicle camera to the default zoom step, so the first-battle/second-battle difference is not explained by a permanently stuck camera zoom alone. The first battle is a cold initialization; after returning from the garage, the persistent client/lobby objects and static remote settings form a warm reload path. This lifecycle difference can expose the stale-settings issue, while the fixed profile diameter remains the underlying reason the circle can exceed physical spread.

## Task: Fix sniper reticle state after repeated battle initialization

- **Status:** IN_PROGRESS
- **Started:** 2026-11-03
- **User request:** Fix the bug where the sniper reticle/convergence circle becomes incorrect after returning to the garage and initializing another battle, while the first battle is correct.
- **Scope:** Repair the repeated battle initialization path for sniper camera, weapon dispersion, crosshair state, and server/global dispersion settings. Preserve existing unrelated working-tree changes.
- **Acceptance criteria:** A subsequent battle starts with fresh, synchronized dispersion and reticle state; the fix is idempotent and does not reset active settings incorrectly; focused validation passes; unrelated files remain untouched.
- **Progress:** Existing diagnosis identified two interacting causes: static server settings are not refreshed after the persistent client/lobby scene survives a battle unload, and the reticle can use stale UI state during the warm reload. The user has now explicitly requested implementation.
- **Next steps:** Inspect battle scene load/unload hooks and reticle/dispersion reset ownership, implement the smallest focused lifecycle fix, then compile and validate the changed paths.

### Implementation checkpoint — 2026-11-03
- `LobbyManager` now sends the authoritative server-settings snapshot before every additive battle scene load, while retaining the initial client-start request. This refreshes `RemoteServerSettings` before the next vehicle is initialized.
- `GunCrosshair` now restores its cached reticle geometry and invalidates diameter/aim-status caches when the HUD is enabled, disabled, or the local vehicle changes. The next weapon update therefore starts from the new vehicle's current profile instead of the previous battle's smoothed size.
- Updated `docs/codex-game/match-lobby-spawn.md`, `docs/codex-game/server-settings.md`, and `docs/codex-game/ui-hud.md` to document the ownership boundary and repeated-battle lifecycle contract.

### Validation checkpoint — 2026-11-03
- `dotnet build Assembly-CSharp.csproj --no-restore --nologo -v:minimal` passed with 0 errors and the existing 4 warnings.
- Focused `git diff --check` passed for the changed scripts, documentation, and journal.
- No Unity process was stopped or manipulated; live Play Mode repetition remains the final manual verification because the open Editor was not put through another battle.

### Completion checkpoint — 2026-11-03
- **Status:** COMPLETED
- **Result:** Repeated battle initialization now refreshes server settings before scene load and clears the persistent crosshair's previous battle geometry/cache state when the HUD or local vehicle is reinitialized. This removes the warm-reload path that made the sniper reticle inherit stale convergence presentation.
- **Validation:** Production C# build passed with 0 errors; focused diff check and lifecycle call-site assertions passed.
- **Remaining limitation:** The open Unity Editor was not driven through a second battle, so final visual confirmation still needs a manual garage → battle → sniper-mode run.

## Task: Delay and extend post-death spectator camera

- **Status:** IN_PROGRESS
- **Started:** 2026-11-03
- **User request:** After the local player is destroyed, keep the camera inactive for 5 seconds, then show the enemy as before but for twice the previous duration, and preserve the existing behavior afterward.
- **Scope:** Trace the current post-death camera/spectator owner and lifecycle, add the requested delay and extended enemy-view duration without changing unrelated camera behavior, and validate the focused code path.
- **Acceptance criteria:** The local death path leaves the camera inactive for 5 seconds; enemy presentation begins afterward; the enemy-view interval is doubled relative to the current value; existing follow-up behavior remains unchanged; focused validation and documentation/journal updates are recorded.
- **Progress:** Global instructions, the repository journal, task-journal recovery workflow, and gameplay-logic separation workflow were read. No task-specific repository search or implementation edit has been performed yet.
- **Next steps:** Locate the post-death camera owner and current duration constants, implement the smallest focused change, update the relevant mechanic documentation, and validate compilation/static consistency.
- **Discovery checkpoint:** `CameraController` currently enters `KillerIntro` immediately on local death, resolves the killer, follows it for `3` unscaled seconds, then enters `DeathWreck`; manual same-team spectator cycling remains available afterward. The focused owner is already the camera/spectator state machine, so no new coordinator is needed.
- **Decision:** Add a `KillerDelay` phase lasting `5` unscaled seconds with spectator input disabled, then start the existing killer presentation for `6` seconds (`2x` the current `3` seconds). Preserve the existing `DeathWreck` fallback and later `AllyFollow` behavior.
- **Implementation checkpoint:** `CameraController` now starts local death in `KillerDelay`, keeps the detached death rig without resolving or accepting spectator input for 5 unscaled seconds, then follows the killer for 6 unscaled seconds before using the unchanged wreck/ally flow. Added a focused PlayMode regression covering the delay phase, constants, and fallback transition. Updated `docs/codex-game/robot-control.md` with the state-machine timings.
- **Validation checkpoint — 2026-11-03:** `dotnet build Assembly-CSharp.csproj --no-restore --nologo -v:minimal` passed with 0 errors and the existing 4 warnings. `dotnet build Game.PlayMode.Tests.csproj --no-restore --nologo -v:minimal` passed with 0 warnings/errors. Focused `git diff --check` passed. Unity PlayMode was not run because the project Editor is already open; no Unity process was stopped or manipulated.
- **Completion checkpoint — 2026-11-03:**
  - **Status:** COMPLETED
  - **Result:** Local death now holds the detached camera in a no-input `KillerDelay` for 5 unscaled seconds, follows the killer for 6 unscaled seconds, and then preserves the existing `DeathWreck` and later same-team spectator behavior.
  - **Changed files:** `Assets/Game/Scripts/Gameplay/Robots/CameraController.cs`, `Assets/Game/Tests/PlayMode/CameraRuntimeZoomOverridePlayModeTests.cs`, `docs/codex-game/robot-control.md`, and this journal.
  - **Remaining limitation:** Final in-game visual confirmation of the 5-second wait and 6-second killer view still requires a Client/Server PlayMode run after the open Editor session is available for testing.

## Task: Diagnose dedicated server batch build failure

- **Status:** IN_PROGRESS
- **Started:** 2026-11-03
- **User request:** Explain and resolve the failure shown when building the Windows dedicated server in Unity batch mode.
- **Scope:** Inspect the batch-build log and current Unity process state, identify whether the failure is caused by the open Editor, command syntax, or project/build configuration, and provide the smallest safe next step without changing implementation files unless required.
- **Acceptance criteria:** Identify the concrete failure cause from logs, give a corrected command or required prerequisite, preserve the existing project state, and record validation/limitations.
- **Progress:** The screenshot shows the wrapper error `Server build failed` after `Game.Editor.LocalNetworkBuild.BuildLocalServer`; the underlying Unity log has not yet been inspected.
- **Discovery checkpoint:** `D:\Programing\war-of-machines-local-builds\server-build.log` contains only Unity startup and project-path initialization, then exits with return code `1` before any `BuildPlayer` output. Unity process `46864` is already running this same project, so the batch invocation cannot open the project concurrently and never reaches `BuildLocalServer`.
- **Result:** The command syntax is valid; the required fix is to close the Unity Editor for this project, wait for its process to exit, and rerun the same batch build. No implementation files or project configuration were changed.
- **Validation:** Confirmed the active process command line points to `D:\Programing\UnityProjects\war-of-machines-unity`; confirmed the server executable does not yet exist because the build stopped before output creation.
- **Status:** COMPLETED
- **Remaining limitation:** The build was not rerun automatically because closing or stopping the user's open Unity Editor would be disruptive without explicit permission.

## Task: Build and launch dedicated server autonomously

- **Status:** IN_PROGRESS
- **Started:** 2026-11-03
- **User request:** Check the dedicated-server build failure and handle the full verification/build/launch flow independently.
- **Scope:** Inspect the active Unity Editor state, safely remove only the project lock if possible, run the configured batch server build, verify the generated executable, launch it in the background, and record logs/process state without changing gameplay implementation.
- **Acceptance criteria:** The dedicated server build completes or the exact remaining blocker is identified; the generated server executable is verified; if build succeeds, the server is launched with a runtime log and process check; no unrelated project changes are made.
- **Progress:** The prior batch attempt exited before `BuildLocalServer` because Unity Editor process `46864` already had this project open. The user has now explicitly asked for autonomous investigation and handling.
- **Discovery checkpoint:** The project Editor was closed gracefully and the batch Unity child process was waited on to completion; the first wrapper check was premature because Unity continued building in child process `11848`.
- **Build checkpoint:** `D:\Programing\war-of-machines-local-builds\server-build.log` reports `Build Finished, Result: Success`, return code `0`, and the executable exists at `D:\Programing\UnityProjects\war-of-machines-local-builds\Server\WarOfMachinesServer.exe`.
- **Runtime checkpoint:** `WarOfMachinesServer.exe` is running hidden as PID `4000` with UDP `7770`; the local API was started from the existing `local-postgres` profile, Postgres was already healthy, and `https://localhost:7216/unity-server/status` reports `isOnline: true`, `status: ready`, and `port: 7770`.
- **Validation checkpoint:** The server runtime log confirms Unity Transport initialization and the API endpoint confirms a live heartbeat. Existing source changes were preserved; no gameplay implementation files were changed by this task.
- **Completion checkpoint — 2026-11-03:**
  - **Status:** COMPLETED
  - **Result:** Dedicated server was rebuilt successfully, launched in the background, and verified together with the local API/Postgres dependency chain.
  - **Logs:** `D:\Programing\war-of-machines-local-builds\server-build.log`, `D:\Programing\UnityProjects\war-of-machines-local-builds\Server\server-runtime-codex.log`, and the API logs under `D:\Programing\ServerProjects\war-of-machines-API\WarOfMachinesAPI\logs\`.
- **Remaining limitation:** No client was launched, so an actual client connection/battle was not exercised; the server/API processes are intentionally left running for the user.

## Task: Add demo T1 tank and enlarge map

- **Status:** IN_PROGRESS
- **Started:** 2026-11-03
- **User request:** Create one more T1 tank in the demo and increase the map size fourfold.
- **Scope:** Identify the active demo scene and its T1 tank/map ownership, add exactly one additional T1 instance, and enlarge the demo map footprint by 4x while preserving existing gameplay setup and unrelated working-tree changes.
- **Acceptance criteria:** The demo contains one additional T1 tank with valid scene references; the demo map is four times the current footprint; scene serialization remains valid; focused static/build validation is recorded.
- **Progress:** Global instructions, the repository task journal, task-journal recovery workflow, and gameplay-logic separation workflow were read. No repository implementation files have been inspected or changed yet.
- **Discovery:** The current demo scene is `Assets/Game/Scenes/Vehicles.unity`, not the removed legacy `VehicleTest.unity`. It contains `IA_L1_Starter` and `NV_L1_Starter` T1 display instances plus a scene-local `Ground`; the live gameplay `Map.unity` is a separate additive map and was not selected for this request.
- **Implementation:** Added one `IA_L1_Starter (2)` prefab instance at `(3, -4, 8)` with a new FishNet scene id `3401205791`, registered it in `SceneRoots`, and changed the demo Ground scale from `(132.33, 2.6466, 132.33)` to `(529.32, 2.6466, 529.32)` so its X/Z footprint is four times larger.
- **Changed files:** `Assets/Game/Scenes/Vehicles.unity` and this journal only; no scripts, prefabs, or production map files were edited.
- **Validation:** `git diff --check -- Assets/Game/Scenes/Vehicles.unity TASK.md` passed. Focused scene assertions passed for the new prefab block, `SceneRoots` registration, `IA_L1_Starter (2)` name, unique `SceneId`, existing prefab GUID, and fourfold Ground footprint. The scene contains 9 unique FishNet scene IDs after the addition. The project Unity Editor is already open, so no batch import or Play Mode run was started.
- **Result:** The Vehicles demo now contains one additional T1 display tank and a Ground footprint four times larger on X/Z. Existing production map, scripts, prefabs, and unrelated working-tree changes were preserved.
- **Remaining limitation:** Final visual confirmation in the Unity Scene/Game view requires the open Editor to reload `Assets/Game/Scenes/Vehicles.unity` or a later Play Mode run.
- **Status:** COMPLETED
- **Completed:** 2026-11-03

## Task: Correct demo target to TransportAudioDemo

- **Status:** COMPLETED
- **Started:** 2026-11-03
- **User request:** Correct the previous demo edit: the intended demo is `Assets/TransportAudioDemo`, not `Assets/Game/Scenes/Vehicles.unity`; add one more T1 tank there and enlarge that demo map fourfold.
- **Scope:** Restore only the prior task's changes in `Vehicles.unity`, inspect `TransportAudioDemo/TEST.unity` and its runtime demo owner, then apply the requested tank/map changes to the correct demo while preserving all unrelated working-tree changes.
- **Acceptance criteria:** `Vehicles.unity` returns to its pre-task content; `TransportAudioDemo` contains exactly one additional T1 tank and a fourfold map footprint; the correct scene/script assets remain valid; focused static validation is recorded.
- **Progress:** The previous response edited the wrong scene. The user identified `Assets/TransportAudioDemo` as the intended demo. Global instructions, the full repository journal, task-journal recovery workflow, and gameplay-logic separation workflow were reread.
- **Discovery:** `Assets/TransportAudioDemo/TEST.unity` is a runtime-generated demo. `TestTransportDemo` creates one primitive vehicle per serialized `TransportAudioProfile`; the existing `t1.asset` profile was present but omitted from the scene's five-profile list. The arena was generated from a 6x Unity Plane, 58x38 border dimensions, and half-extents 29/19.
- **Correction:** Restored `Assets/Game/Scenes/Vehicles.unity` to its pre-task content; its diff is empty relative to `HEAD`.
- **Decision:** Preserved existing demo keys 1-5 and appended T1 as key 6 at `(21, 0.05, 0)`. Expanded the generated arena linearly by 4x with `ArenaScale = 4f`.
- **Implementation:** Added the existing `t1.asset` GUID to `TEST.unity` as the sixth profile, extended `TestTransportDemo` to six positions/colors and key `6`, scaled the generated arena/borders, and updated the demo README.
- **Changed files:** `Assets/TransportAudioDemo/TEST.unity`, `Assets/TransportAudioDemo/Scripts/TestTransport/TestTransportDemo.cs`, `Assets/TransportAudioDemo/Scripts/TestTransport/README.md`, and this journal. `Assets/Game/Scenes/Vehicles.unity` was restored and is not part of the final change.
- **Validation:** Focused scene/script assertions and `git diff --check` passed. `dotnet build Assembly-CSharp.csproj --no-restore --nologo -v:minimal` passed with `0` errors and `30` existing warnings. Unity Play Mode visual/audio confirmation was not run because the project Editor is already open.
- **Result:** The correct `TransportAudioDemo` now spawns six vehicles, with the existing T1 profile as vehicle 6, and its generated arena footprint/borders are four times larger linearly. The accidental `Vehicles.unity` edit was fully removed.
- **Completed:** 2026-11-03

## Task: Fix T1 startup audio transient
- **Status:** COMPLETED
- **Started:** 2026-11-03
- **User request:** Diagnose and fix the strange approximately 0.5-second sound that plays when T1 starts moving in `TransportAudioDemo`, while normal engine audio follows afterward.
- **Scope:** Trace the T1 profile and startup/acceleration audio path, fix only the transient source, and preserve the other demo vehicle and arena changes.
- **Acceptance:** T1 no longer emits the unwanted startup transient while retaining its normal engine audio; run focused validation and a build when applicable.
- **Progress:** `TransportEngineAudio` plays `accelerationClips` as a one-shot when throttle crosses the start threshold. T1 was the only profile with an acceleration entry, and it incorrectly referenced the Tiger high-loop clip; the normal T1 loops and start clip are separate and valid.
- **Decision:** Remove the invalid T1 acceleration one-shot reference rather than changing shared audio playback logic, so T1 keeps its normal layered engine loops and startup clip.
- **Result:** Removed the incorrect T1 `accelerationClips` reference to the Tiger high-loop asset. T1 now transitions through its normal layered idle/run/high loops without the foreign one-shot when movement starts; its valid `startClip` remains unchanged.
- **Validation:** Focused T1 profile assertions passed; `Assets/Game/Scenes/Vehicles.unity` remained clean; `TASK.md` targeted diff check passed; `dotnet build Assembly-CSharp.csproj --no-restore --nologo -v:minimal` passed with 0 errors and 2 existing `System.Net.Http` conflict warnings.
- **Limitations:** Unity Play Mode audio verification was not run because the Unity Editor was already open; the unrelated repository-wide diff check still reports pre-existing trailing whitespace in `Assets/Game/Scenes/Client.unity`.
- **Completed:** 2026-11-03

## Task: Fix quiet HL62 engine audio
- **Status:** COMPLETED
- **Started:** 2026-11-03
- **User request:** Diagnose why HL62's engine sounds are individually normal but the tank is much quieter than expected in `TransportAudioDemo`, then fix it.
- **Scope:** Trace HL62's serialized audio profile, source clip import settings, and runtime volume/spatial attenuation; change only the root cause while preserving the other demo audio fixes.
- **Acceptance:** HL62 has comparable audible engine volume without distorting its clips or changing unrelated vehicle behavior; focused validation and build results are recorded.
- **Progress:** Compared HL62 with every other demo profile and traced `TransportEngineAudio`. HL62's four referenced clips resolve correctly, all loop recordings are approximately 5–7 seconds (above the 1-second loop cutoff), the scene has one active `AudioListener`, and spatial settings are shared. The concrete profile-specific attenuation was `masterVolume: 0.88`; runtime loop volumes are clamped to 0–1, so `1.0` is the maximum effective value.
- **Decision:** Raise only HL62's `masterVolume` to `1.0`, preserving its recordings, pitch, spatial attenuation, and all other vehicle profiles.
- **Result:** Raised HL62's profile `masterVolume` from `0.88` to `1.0`, removing the only profile-specific attenuation below the effective loop-volume ceiling while preserving all audio recordings and shared runtime behavior.
- **Validation:** HL62 profile/reference assertions passed; `Assets/Game/Scenes/Vehicles.unity` remained clean; `TASK.md` targeted diff check passed; `dotnet build Assembly-CSharp.csproj --no-restore --nologo -v:minimal` passed with 0 errors and 2 existing `System.Net.Http` conflict warnings.
- **Limitations:** Unity Play Mode audio comparison was not run because the project Editor was already open; final volume confirmation still needs a manual run of `TransportAudioDemo` and selection of HL62 with key `2`.
- **Completed:** 2026-11-03

## Task: Move unused TransportAudioDemo engine sounds
- **Status:** COMPLETED
- **Started:** 2026-11-03
- **User request:** Move all unused sounds from `Assets/TransportAudioDemo/Audio/Engine` into an `EngineNotUse` folder.
- **Scope:** Determine usage from all `TransportAudioDemo` profiles/scenes/scripts and repository GUID references, move only unreferenced sound assets with their `.meta` files, and preserve all active audio paths.
- **Acceptance:** Every moved sound is unused, every remaining sound is still referenced correctly, Unity GUIDs remain unchanged, and focused validation/build results are recorded.
- **Progress:** Enumerated the current Engine contents and resolved every present audio GUID against all non-meta files under `Assets`. Three T-55 files have no references anywhere: `t-55_high_1.mp3`, `t-55_run_2.mp3`, and `t-55_slow.mp3`. Two HL62 files are already absent as pre-existing working-tree deletions while their profile references remain, so they are outside this move and will not be restored or reclassified.
- **Decision:** Move only the three unreferenced T-55 audio files and their `.meta` files into `Assets/TransportAudioDemo/Audio/EngineNotUse`, preserving GUIDs and leaving all active profile references untouched.
- **Result:** Moved `t-55_high_1.mp3`, `t-55_run_2.mp3`, and `t-55_slow.mp3` plus their `.meta` files into `Assets/TransportAudioDemo/Audio/EngineNotUse`. No active profile, scene, or script references these assets.
- **Validation:** Source/destination and GUID assertions passed; moved GUIDs have no non-meta references under `Assets`; `TASK.md` targeted diff check passed; `dotnet build Assembly-CSharp.csproj --no-restore --nologo -v:minimal` passed with 0 errors and 2 existing `System.Net.Http` conflict warnings.
- **Limitations:** Two HL62 files (`HL62_High.mp3` and `HL62_Main.mp3`) were already missing before this task while their profile GUIDs remain referenced; they were preserved as pre-existing working-tree state and not restored or moved. Unity asset-database refresh/visual confirmation was not run because the Editor was already open.
- **Completed:** 2026-11-03

## Task: Verify remaining T-55 engine sounds
- **Status:** COMPLETED
- **Started:** 2026-11-03
- **User request:** Confirm whether all sounds still visible in the T-55 Engine folder are actually used.
- **Scope:** Compare the visible files with the serialized `T55.asset` profile and report the result without changing audio assets.
- **Acceptance:** Every visible T-55 sound is classified as referenced or unused, with no asset changes unless explicitly requested.
- **Result:** All eight remaining T-55 recordings visible in the screenshot are referenced by `T55.asset`: `t-55_idle`, `t-55_run`, `t-55_high`, `t-55_start`, `t-55_stop`, and `t-55_accel_1/2/3`.
- **Validation:** Direct GUID-to-meta mapping passed; no asset changes were made for this verification.
- **Completed:** 2026-11-03

## Task: Evaluate two additional tanks from existing audio
- **Status:** IN_PROGRESS
- **Started:** 2026-11-03
- **User request:** Review the six current `TransportAudioDemo` tanks and assess whether two more can be built from the existing engine recordings, including reusing or selectively omitting clips to keep the new tanks distinct.
- **Scope:** Inspect the six profiles and available recordings, compare their loop/event roles, and propose practical unique audio combinations without modifying assets.
- **Acceptance:** Provide a concrete feasibility assessment and two distinct profile concepts, noting any source-asset limitations and the recommended next implementation step.
- **Progress:** Inspected all six profiles in `TEST.unity` and mapped their current loop/event recordings. The existing set provides complete idle/run/high/start coverage plus dedicated acceleration clips from HL-120, Leopard 1, and T-55; T-55 also has a stop clip. The T1 acceleration one-shot remains intentionally empty after its earlier fix.
- **Decision:** Two additional tanks can be built safely by reusing GUID references; do not remove clips from existing profiles because shared audio assets are valid. Make the new tanks distinct through cross-profile combinations, acceleration selection, pitch, and volume.
- **Proposed Tank 7:** `HL62_Idle` + `leopard1_run` + `t-55_high`, acceleration from `hl120_accel` and `leopard_accel_1`, `HL62_Start`, and `t-55_stop`; tune pitch slightly above neutral for a medium/scout character.
- **Proposed Tank 8:** `t-55_idle` + `hl120_run` + `tiger_high`, acceleration from `t-55_accel_1` and `t-55_accel_3`, `t1/start`, and `t-55_stop`; tune pitch below neutral for a heavier assault character. Avoid using a high-loop recording as an acceleration one-shot.
- **Result:** The idea is feasible and can produce two recognizable variants without new recordings. No assets or profiles were modified during this assessment.
- **Validation:** Profile-to-clip mapping and the six-profile scene list were inspected; no implementation or runtime validation was required.
- **Completed:** 2026-11-03

## Task: Add two mixed-audio tanks and expand movement bounds
- **Status:** COMPLETED
- **Started:** 2026-11-03
- **User request:** Implement the two proposed tanks built from existing audio clips, and fix the enlarged `TransportAudioDemo` arena so vehicle movement/colliders are not still restricted to the old map bounds.
- **Scope:** Add two distinct `TransportAudioProfile` assets and register them in the demo with selection controls, then propagate the 4× arena bounds into vehicle movement limits without changing unrelated project behavior.
- **Acceptance:** The demo has eight selectable tanks; the two new profiles use only existing audio GUIDs with distinct loop/event mixes; vehicle movement limits match the enlarged arena; focused asset/script validation and build pass.
- **Progress:** Existing six profiles, available audio GUIDs, runtime-generated arena scaling, and the static `PrimitiveTransportVehicle` movement clamps were inspected. The old clamp was confirmed as the cause of the invisible movement boundary.
- **Implementation:** Added `ScoutMix.asset` and `AssaultMix.asset` using existing audio GUIDs with distinct loop/event combinations and pitch settings; registered both in `TEST.unity` as vehicles 7 and 8; added positions, colors, numeric selection keys, and updated help text/README. Replaced the hard-coded vehicle clamps with configured movement limits passed from `TestTransportDemo` as `27/17 * ArenaScale`, yielding `±108/±68` for the 4× arena.
- **Changed files:** `Assets/TransportAudioDemo/Audio/EngineProfiles/ScoutMix.asset`, `Assets/TransportAudioDemo/Audio/EngineProfiles/ScoutMix.asset.meta`, `Assets/TransportAudioDemo/Audio/EngineProfiles/AssaultMix.asset`, `Assets/TransportAudioDemo/Audio/EngineProfiles/AssaultMix.asset.meta`, `Assets/TransportAudioDemo/TEST.unity`, `Assets/TransportAudioDemo/Scripts/TestTransport/TestTransportDemo.cs`, `Assets/TransportAudioDemo/Scripts/TestTransport/PrimitiveTransportVehicle.cs`, `Assets/TransportAudioDemo/Scripts/TestTransport/README.md`, and this journal.
- **Validation:** The first assertion wrapper had a PowerShell syntax error; the corrected rerun passed all profile/GUID/scene/bounds assertions, `Vehicles.unity` remained clean, and focused `git diff --check` passed. `dotnet build Assembly-CSharp.csproj --no-restore --nologo -v:minimal` passed with 0 errors and 4 existing warnings.
- **Result:** `TransportAudioDemo` now contains eight selectable tanks, with two new mixed-audio tanks built entirely from existing recordings, and vehicles can drive across the enlarged arena instead of stopping at the old bounds.
- **Limitations:** Unity Play Mode visual/audio verification was not run because the project Editor was already open; final manual confirmation should press keys `7`/`8` and drive toward the expanded borders.
- **Completed:** 2026-11-03

## Task: Integrate engine audio into robot audio architecture
- **Status:** COMPLETED
- **Started:** 2026-11-03
- **User request:** Thoroughly study the project and build a stable engine-audio system that transfers the already tuned robot engine sounds into the actual robots, exposes them through `AudioResources`, respects existing volume settings, and uses a dedicated engine-sound script attached to all relevant robots.
- **Scope:** Audit `AudioResources`, `SoundCaller`/`AudioManager`/`AudioPreset`, robot prefabs and lifecycle owners, renamed engine recordings, existing tests, and the prior demo engine behavior. Implement the smallest focused production integration with safe missing-resource handling, correct volume routing, and focused validation.
- **Acceptance:** Each relevant robot resolves its named engine profile from `AudioResources`; engine playback is owned by a dedicated component attached to the robot prefab/owner; idle/movement/high/start/acceleration/stop behavior remains stable; master/SFX volume settings affect playback; no direct per-robot clip wiring bypasses the shared audio system; focused tests/build checks pass.
- **Progress:** Global instructions, the full repository journal, task-journal recovery workflow, gameplay-logic separation workflow, and background testing workflow were read. The shared audio path was audited: `AudioManager` routes every SFX through `AudioPreset`, reapplies `SFXVol`, and supports stable loop handles; robot motion is exposed through synchronized `VehicleInputController.AnimMove` on all eight production prefabs. The eight available renamed demo profiles and their clip GUIDs were mapped, and the existing `AudioManager` prefab was confirmed as the runtime `AudioResources` source.
- **Decision:** Add a dedicated `AudioEngineProfile` registry to `AudioResources`, keep clip playback inside `AudioManager`/`AudioPreset` so SFX volume and spatial settings remain authoritative, and add one focused `RobotEngineAudio` component to each robot prefab. The component uses `AnimMove`, smooths the same idle/run/high blend as the tested demo, guards missing profiles/clips, and stops all handles on disable/destroy.
- **Implementation:** Added `AudioEngineProfile` lookup and editor support to `AudioResources`, typed shared playback overloads to `SoundCaller`, playback-state accessors to `AudioPreset`, and the focused `RobotEngineAudio` component. Serialized eight exact robot-to-profile mappings in `AudioManager.prefab`, using the renamed existing recordings, and attached/configured the component on all eight production IA/NV robot prefabs. Documented the ownership boundary in `docs/codex-game/audio-presentation.md`.
- **Validation:** All eight engine targets, 30 referenced engine GUIDs, and eight robot prefab components passed focused PowerShell assertions. `dotnet build Assembly-CSharp.csproj --no-restore --nologo -v:minimal` passed with 0 errors and 2 existing `System.Net.Http` conflict warnings; `git diff --check` passed.
- **Result:** Production robots now obtain their engine sounds from `AudioResources.engineProfiles`; playback remains routed through `AudioManager`/`AudioPreset`, so SFX volume, spatial settings, smoothing, and lifecycle cleanup remain centralized and consistent.
- **Limitations:** Unity Play Mode audio audition was not run because the project Editor was already open; manual runtime confirmation across the eight robots is still recommended. The existing recordings remain in `Assets/TransportAudioDemo/Audio/Engine` and are referenced by GUID to preserve the tested demo setup.
- **Completed:** 2026-11-03

## Task: Make robot engine volume global
- **Status:** COMPLETED
- **Started:** 2026-11-03
- **User request:** Use one shared volume slider for all robot engine sounds instead of one volume control per engine profile.
- **Scope:** Move the master-volume setting to the global `AudioResources` configuration, remove profile-level duplicates, route every robot engine loop and one-shot through the shared value, and keep the existing SFX volume pipeline intact.
- **Acceptance:** One clearly labeled slider controls every engine profile and every engine sound, no per-profile master sliders remain, and the project compiles with focused serialization checks passing.
- **Progress:** Confirmed the previous implementation exposed a master value inside each profile, which did not match the requested single control for all robots.
- **Implementation:** Added one `engineMasterVolume` field to `AudioResources`, exposed it as `Engine Master Volume (All Robots)` above the engine profile list, removed all eight profile-level `masterVolume` values from `AudioManager.prefab`, and routed loop/one-shot playback through the global value.
- **Validation:** Focused assertions confirmed one global serialized field, no profile-level master fields, and runtime usage for loops and one-shots. `dotnet build Assembly-CSharp.csproj --no-restore --nologo -v:minimal` passed with 0 errors and 4 existing warnings; `git diff --check` passed.
- **Result:** One slider now lowers or raises every robot engine sound together while preserving each profile's clip selection, per-clip volume, and global SFX volume settings.
- **Limitations:** Unity Play Mode audio audition was not run; tune the single slider in the `AudioManager` prefab after Unity refreshes the scripts.
- **Completed:** 2026-11-03

## Task: Diagnose robot engine audio not following robots
- **Status:** COMPLETED
- **Started:** 2026-11-03
- **User request:** Investigate why robot engine audio appears to stay behind instead of following the moving robot, and fix the attachment/spatial playback behavior.
- **Scope:** Trace robot prefab wiring, engine profile resolution, `SoundCaller`/`AudioManager` parenting, and runtime spatial audio configuration; make the smallest focused fix that keeps engine sounds attached to their robot.
- **Acceptance:** Engine `AudioPreset` instances follow the robot transform during movement, all engine events use the same spatial path, and focused validation/build checks pass.
- **Progress:** Traced the shared playback path and inspected all eight production prefabs. The `RobotEngineAudio` component was correctly on the network root, but each vehicle's `VehicleMovementController` moves the child referenced by `VehicleRoot.objectMover`, so the root transform stayed at the spawn point.
- **Implementation:** Changed `RobotEngineAudio` to create and maintain pooled engine `AudioPreset` instances under `VehicleRoot.objectMover.transform`, with a root-transform fallback for robots without a configured mover. Applied the same anchor to loops and start/acceleration/stop one-shots and documented the ownership rule.
- **Validation:** Movement-anchor assertions passed for all eight robot prefabs. `dotnet build Assembly-CSharp.csproj --no-restore --nologo -v:minimal` passed with 0 errors and 4 existing warnings; `git diff --check` passed.
- **Result:** Engine audio now follows the transform that actually moves each robot instead of remaining at the stationary network root.
- **Limitations:** Unity Play Mode audio audition was not run; manually drive one IA and one NV robot to confirm spatial movement after the Editor refreshes the script.
- **Completed:** 2026-11-03

## Task: Check whether Start War of Machines is hung

- **Status:** IN_PROGRESS
- **Started:** 2026-11-06
- **User request:** Determine whether the currently running `Start War of Machines.bat` launcher is hung and identify the stage where it stopped.
- **Scope:** Inspect current launcher-related processes, ports, control files, timestamps, and runtime logs without stopping or modifying user processes unless explicitly requested.
- **Acceptance criteria:** Distinguish an active wait from a crashed process, identify the exact wait stage and cause if available, and preserve unrelated working-tree changes.
- **Progress:** The prior diagnosis established that API startup failed when PostgreSQL was unavailable, while the server build itself succeeded. The user now reports that a new attempt appears stuck; current process and control-file state must be checked.
- **Next steps:** Inspect `cmd`/PowerShell/dotnet/Unity/server processes, ports `7216`/`7770`, server-build request/result files, and fresh API/server logs; then close this entry with the observed state and safe next action.
- **Discovery:** A live `cmd.exe` is still running the launcher. It created `server-build-request.txt` at `10:17:04`, but no `server-build-result.txt` exists; ports `7216` and `7770` have no listeners and no API/server process is running.
- **Discovery:** The current `Assets\Editor\LocalNetworkBuild.cs` contains only menu-driven `BuildLocalServer()` methods and no `[InitializeOnLoad]` watcher or `server-build-request`/`server-build-result` handling. Unity therefore cannot consume the request token, so the batch loop waits silently for its 30-minute timeout.
- **Discovery:** The open Unity Editor is the correct project (`-projectpath D:\Programing\UnityProjects\war-of-machines-unity`), but `Editor.log` contains no request/result watcher events. The existing API and server logs are from the previous successful run, not this stuck attempt.
- **Implementation:** No implementation files or running processes were changed. The current launcher remains active so the user can stop it manually with `Ctrl+C`.
- **Validation:** Confirmed the active `cmd.exe`, request-only control-file state, absent API/server listeners, current `LocalNetworkBuild.cs` contents, and absence of watcher markers in the current Unity `Editor.log`.
- **Result:** Yes, the current launcher is genuinely stuck waiting for a server-build result that this Editor code cannot produce; it has not reached API startup yet.
- **Remaining limitation:** The fix requires restoring the request watcher in `LocalNetworkBuild.cs` or changing the launcher to use a supported build path; no fix was applied because the user only asked whether it was hung.
- **Completed:** 2026-11-06.
- **Status:** COMPLETED

## Task: Restore discarded launcher build bridge and test startup

- **Status:** IN_PROGRESS
- **Started:** 2026-11-06
- **User request:** Restore the files discarded from the previous launcher fix, repair `Start War of Machines.bat`, and test that the API and dedicated server start correctly.
- **Scope:** Recover the Unity Editor request/response watcher and its documentation, preserve unrelated working-tree changes, stop the currently hung launcher safely, and run focused build/start/stop validation without changing PostgreSQL configuration.
- **Acceptance criteria:** The open Unity Editor consumes a fresh server-build request and writes a matching result; the launcher starts the API and dedicated server; ports/processes/logs confirm both services; the stop path removes only the services; relevant builds and source checks pass.
- **Progress:** The discarded state is confirmed: `Assets\Editor\LocalNetworkBuild.cs` has only menu-driven builds, while the desktop launcher still waits for `server-build-result.txt`. A live hung launcher process and stale request file are present and must be handled before testing.
- **Next steps:** Recover the watcher implementation and documentation from repository history/journal context, stop the stale launcher, compile/reload the Editor code, then run a fresh end-to-end cycle and close with validation.
- **Recovery:** Stopped only the verified hung launcher `cmd.exe` and removed its generated request/result control files. The existing desktop launcher already contained the tokenized request/response flow; the discarded project-side watcher and bridge documentation were the missing pieces.
- **Implementation:** Restored the `[InitializeOnLoad]` `LocalNetworkBuild` watcher, shared build-root resolution, request claiming, server build invocation, sanitized failure reporting, and atomic result writing in `Assets\Editor\LocalNetworkBuild.cs`. Restored the request/result protocol and open-Editor ownership documentation in `docs\codex-game\local-network-builds.md`.
- **Validation:** `dotnet build Assembly-CSharp-Editor.csproj --no-restore --nologo -v:minimal` passed with 0 errors and 5 pre-existing warnings; static watcher/protocol assertions and `git diff --check` passed.
- **End-to-end validation:** Two fresh `Start War of Machines.bat` cycles completed successfully. The first received `SUCCESS`, started the API and server in about 30 seconds, and the second did the same in about 20 seconds. API GET `/` returned `302 /admin`; API stdout reported `https://localhost:7216`; API stderr was empty; server log reported UnityTransport listening on port `7770`.
- **Stop validation:** Ran `Stop War of Machines.bat` between cycles; it exited `0`, removed the API PID file, and left no `WarOfMachines.exe` or `WarOfMachinesServer.exe` process before the second start.
- **Final state:** The second successful cycle is currently running with `WarOfMachines.exe` and `WarOfMachinesServer.exe` active.
- **Unrelated side effect:** Unity reserialized the two Roboto SDF font assets during the Editor reload/build and left them modified; they were clean at task start and are not part of this launcher fix. Do not include those font diffs with the launcher change.
- **Result:** The discarded files responsible for the hang were restored, and the launcher now builds through the open Editor and starts both services reliably.
- **Remaining limitation:** The font reserialization should be discarded or separately reviewed by the user while the Editor is open; no gameplay or API source changes were made.
- **Completed:** 2026-11-06.
- **Status:** COMPLETED

## Task: Start Docker and PostgreSQL from War of Machines launcher

- **Status:** IN_PROGRESS
- **Started:** 2026-11-06
- **User request:** Update `Start War of Machines.bat` so it starts Docker Desktop, starts the local PostgreSQL Compose service, waits for database readiness, and then starts the API and dedicated server.
- **Scope:** Modify only the desktop start launcher and related project documentation if needed; preserve the existing Unity Editor build bridge, API profile, server startup, stop behavior, PostgreSQL data volume, and unrelated working-tree changes.
- **Acceptance criteria:** The launcher handles Docker Desktop already running or closed, waits for the Docker engine and healthy PostgreSQL on `127.0.0.1:5433`, fails with a clear diagnostic on timeout, then runs the existing API/server flow successfully; focused static checks and a real end-to-end cycle pass.
- **Progress:** The current launcher stops API/server and starts the API, but has no Docker or Compose startup step. The local Compose file exposes PostgreSQL at `127.0.0.1:5433` with a healthcheck and named volume.
- **Next steps:** Verify Docker Desktop executable/CLI behavior, patch the batch flow before API build, test it with the database container stopped, and record the final service/process/log state.
- **Discovery:** Docker CLI is installed at `C:\Users\TorasPC\AppData\Local\Programs\DockerDesktop\resources\bin\docker.exe`; Docker Desktop is installed at `C:\Users\TorasPC\AppData\Local\Programs\DockerDesktop\Docker Desktop.exe`. With Docker Desktop closed, `docker info` failed and port `5433` was unavailable.
- **Implementation:** Updated `C:\Users\TorasPC\Desktop\Start War of Machines.bat` to validate Docker/Compose paths, start Docker Desktop when needed, wait up to five minutes for the Docker daemon, run `docker compose ... up -d --wait postgres`, verify port `5433`, and then continue through the existing API/server flow. Updated `docs\codex-game\local-network-builds.md` with the startup ownership and timeout behavior.
- **Validation:** Static launcher assertions passed and `git diff --check` passed. A real test starting with Docker Desktop closed opened Docker Desktop, reached engine readiness, started `war-of-machines-local-postgres-1`, observed `Healthy`, and continued without manual intervention.
- **End-to-end validation:** The launcher completed with `API and game server are running.` API GET `/` returned `302 /admin`; API connected on `7216` with empty stderr; the dedicated server log confirmed UnityTransport listening on `7770`; Docker Compose reported PostgreSQL `Up (healthy)` on `127.0.0.1:5433`.
- **Final state:** Docker Desktop, PostgreSQL, API, and dedicated server are currently running. The named PostgreSQL volume was reused; no database configuration or data was changed.
- **Result:** `Start War of Machines.bat` now starts Docker Desktop and the local database automatically before building and launching the API and dedicated server.
- **Remaining limitation:** Docker Desktop must already be installed, and startup may wait up to five minutes for the Docker engine plus the Compose healthcheck.
- **Completed:** 2026-11-06.
- **Status update:** COMPLETED

## Task: Diagnose camera or FOV zoom drift after robot death

- **Status:** IN_PROGRESS
- **Started:** 2026-11-06
- **User request:** Re-evaluate the two supplied screenshots: the sniper reticle circle is not the broken element; after death and re-entry at the same maximum zoom, the second battle is slightly more zoomed in. Analyze the camera/FOV lifecycle, remove the previous `GunCrosshair` fix, and repair the actual cause.
- **Scope:** Inspect the screenshots and all camera/zoom/FOV owners and death/spectator transitions; identify the state that survives death or is applied twice; revert only the previous reticle lifecycle patch; apply a focused camera-owned fix and validate it without disturbing unrelated worktree changes.
- **Acceptance criteria:** The same robot at the same maximum zoom uses the same camera projection/FOV before and after a death/re-entry cycle; camera state is reset by its owning lifecycle; the prior reticle-only patch is removed; focused build/static checks and runtime evidence are recorded.
- **Progress:** The previous task's clean runtime pass showed no visible reticle change in zoom profile `4`; the user's new screenshots indicate the actual visual difference is camera magnification/FOV, so the earlier reticle hypothesis is superseded.
- **Discovery:** Both supplied screenshots show the same reticle `RectTransform` size (`366x366`) at maximum zoom; the changed visual is therefore not the spread-circle geometry. Gameplay FOV is written only by `CameraController`, while the camera target/FOV is selected through the mutable static `CameraSync.In`.
- **Discovery:** The robot prefab has separate `CameraPoint` and `CameraRig` children. Death detaches them, but the normal re-entry path creates a fresh network vehicle rather than restoring the reticle, so the stronger lifecycle risk is stale or wrong `CameraSync.In` ownership during additive scene transitions.
- **Decision:** Revert the previous `GunCrosshair` death/faction reset patch and keep the existing authored reticle behavior unchanged. The next implementation must make camera/FOV ownership explicit and reset it at the gameplay camera lifecycle boundary.
- **Implementation:** Removed the two previous `GunCrosshair` reset calls and restored the original HUD ownership note. Added lifecycle cleanup to `CameraSync`, cached the active battle camera in `CameraController`, rebound only when that camera is unavailable, and reset the local zoom/FOV to the default profile on death.
- **Validation prepared:** Added a focused playmode regression test that disables a `CameraSync` and verifies the static camera reference is cleared before another battle can bind to it.
- **Next steps:** Open both screenshots, trace `CameraController` and related zoom/FOV state through live/death-spectator/despawn/re-entry paths, then patch and validate the camera owner.
- **Validation:** `git diff --check`, `Assembly-CSharp.csproj`, and `Assembly-CSharp-Editor.csproj` all pass with `0` errors. Existing `System.Net.Http` conflict and Unity deprecation warnings remain unrelated. The current Unity Editor was already open, so a second batch-mode runtime was not started over it; the attached screenshots remain the visual evidence for the reticle-vs-camera diagnosis.
- **Worktree safety:** `Assets/Game/Content/Server/Prefabs/ServerSettings.prefab` retains the user's pre-existing `maxPlayersForFindRoom` change. `GunCrosshair.cs` and `docs/codex-game/ui-hud.md` have no remaining diff from the previous reticle patch.
- **Result:** The fix is camera-owned: map camera singleton cleanup prevents stale references after disable/unload, the local controller remains bound to the active battle camera, and death returns camera zoom/FOV to the default profile before spectator/re-entry state can leak forward.
- **Remaining limitation:** A post-patch manual death/re-entry pass still needs to be run in the user's live Unity window to confirm the exact screenshot-level magnification, since the current Editor's Game View did not expose a safe accessible runtime state.
- **Status:** COMPLETED — 2026-11-06

## Task: Study and redesign bot combat behavior

- **Status:** IN_PROGRESS
- **Started:** 2026-11-06
- **User request:** Study the current robot bot behavior and gameplay first, then design a workflow that makes bots deliberate and human-like: stop to aim and fire accurately in combat, allow controlled movement during reload or repositioning, and eliminate jittery left/right rotation while aiming and firing.
- **Scope:** Inspect the current bot decision, navigation, aiming, weapon, authority, lifecycle, and relevant gameplay documentation; reproduce or infer the current behavior from code and runtime assets; propose a focused bot behavior workflow and ownership boundaries before implementation.
- **Acceptance criteria:** The current jitter source and control loop are identified with concrete files/methods; combat/reposition/reload states and their transitions are described; the proposed workflow preserves network/authority boundaries and includes focused regression coverage; no implementation files are changed until the behavior plan is clear.
- **Progress:** Global instructions, the gameplay-logic separation workflow, and task-journal recovery workflow were read. No bot-specific workflow was found in the memory archive or history index; the gameplay separation workflow is the applicable guidance.
- **Discovery:** The current bot system is server-authoritative and split across `VehicleBotBrain`, `BotNavigator`, `BotCombatController`, `BotCombatTacticSelector`, aim/line-of-fire helpers, and `VehicleInputController`; bot inputs replicate through the normal movement, turret, gun, reload, and projectile systems.
- **Discovery:** `BotCombatController` ticks every `0.12s`, while `BotNavigator` owns driving in its own `FixedUpdate`. Combat can call `SetMovementSuppressed`, but when a tactic does not hold it writes aim/fire together with the navigator's current movement input. Navigator target positions can refresh every `0.75s`, and movement steering can enter pivot-turn mode at `48` degrees, which can produce visible hull turning while the turret is also tracking.
- **Discovery:** Serialized combat settings enable `moveTowardTarget` and `holdPositionWithLineOfFire`, but tactic decisions still permit moving fire: `CloseMobileAssault`, `KiteStrongTarget`, and `FlankDistractedTarget` return `HoldPosition = false` with `AllowFire = true`; their readiness thresholds are `0.55`, `0.78`, and `0.85`. Only tactics that hold, or a loaded clean line of fire under `holdPositionWithLineOfFire`, suppress movement.
- **Discovery:** Fire readiness already checks authoritative reload/ammo, physical turret/gun alignment, muzzle error, and dispersion; successful bot shots use `WeaponReloadController.ServerTryFireAuthoritative` and `NetworkWeaponShooter.ServerFireAuthoritative`. Reload lasts about `2s` by default, but no explicit combat phase owns the intended stop/aim/fire/reload/reposition sequence.
- **Discovery:** The current target lead is sampled every `0.08s` and the combat decision runs every `0.12s`; target-map updates can reset/repath navigation as positions change. The turret applies every nonzero target-yaw error, including tiny corrections, while movement and turret yaw are independent inputs.
- **Decision:** The behavior redesign should be expressed as a focused combat engagement-phase owner (or a focused tactic/phase helper), leaving `BotNavigator` responsible only for driving and `VehicleInputController` responsible only for authoritative input delivery. The phase contract must make firing while chassis movement is nonzero impossible for precision shots, while allowing deliberate movement during reload/reposition/cover transitions.
- **Discovery:** Existing `BotAiRegressionPlayModeTests` cover lead prediction, line-of-fire exposure, visibility memory, steering smoothing, and the current hold/reload helper. `BotBattle10v9PlayModeTests` records direction flips but does not fail on them or correlate a shot with movement. No fresh runtime run was started in this investigation; the saved integration logs are historical evidence from the same bot architecture.
- **Evidence:** Saved 10v9 runs report maximum hull/turret direction flips of `81/87`, `108/103`, `115/124`, `127/135`, `140/143`, `104/113`, `98/103`, and `66/74` across the recorded runs. Detailed samples show frequent changes in `desired` travel direction while bots retain active targets and ammunition, confirming that navigation remains active during engagements; the logs do not yet expose a phase or shot/movement correlation.
- **Root cause:** The current tactic contract explicitly allows movement fire in `CloseMobileAssault` (`HoldPosition=false`, readiness `0.55`), `KiteStrongTarget` (`false`, `0.78`), and `FlankDistractedTarget` (`false`, `0.85`). `BotCombatController` then forwards the navigator's current movement together with aim and shoot input. Position candidates and line-of-fire fallback points can change on combat ticks, while the turret continues to chase every target-yaw error. This creates no single engagement transaction that owns stop, settle, fire, reload, and reposition timing.
- **Proposed workflow:**
  1. `Perceive`: acquire a map-visible enemy and keep the current target locked during aim settle, shot, and reload unless the target is invalid, dead, or an emergency threat requires a switch.
  2. `ChoosePosition`: select one tactical point with a fixed flank/retreat side and a minimum commitment time/distance; do not alternate side candidates on every target or line-of-fire update.
  3. `Approach`: drive to the committed point or toward a visible/last-known target position. Fire is disabled by default while the chassis is moving; memory-only and obstructed targets are navigation-only.
  4. `Settle`: when a direct target and valid firing lane exist, suppress navigation and command zero movement. Wait until actual chassis speed/turn rate is near zero, turret yaw/pitch/muzzle errors are within limits, dispersion is ready, and the aim/lane solution has remained stable for a short settle window.
  5. `Fire`: issue one authoritative fire pulse only while the chassis remains stopped and all fire gates pass. Do not continuously hold `Shoot` through the reload transition.
  6. `Reload/Reposition`: after the shot, movement may resume toward the same committed point or a newly selected stable point, but firing remains disabled during reload. A moving-fire exception may exist only as an explicit emergency/suppression policy and must be off by default.
  7. `Reacquire`: after reload or arrival, stop again, reacquire/settle the same target, and fire the next shot. If the lane is lost, move deliberately to a new committed point instead of pivoting in place indefinitely.
  8. `Disengage`: clear target/phase on death, despawn, disabled combat, or lost visibility; release navigation suppression and clear autonomous input without leaving stale aim/fire state.
- **Ownership boundary:** Add a focused server-side engagement-phase owner (for example `BotCombatEngagementController`) that consumes perception/aim/reload facts and returns a small decision containing phase, navigation intent, aim solution, fire pulse, and movement permission. Keep `BotCombatController` as orchestration, `BotNavigator` as movement/path execution, `BotAimController`/`BotFireDecision` as aim/fire facts, and `VehicleInputController` as the only authoritative input delivery path. Avoid reading stale `inputManager.Move` as the combat command; compose one final bot command per server tick.
- **Acceptance/regression plan:** Add focused phase-transition tests for approach/no-fire, settle/zero-movement, fire-pulse-only, reload/move-without-fire, obstructed-target reposition, target lock, and teardown. Extend the integration telemetry to record phase, requested movement, actual chassis speed/turn speed, turret error, dispersion, and shot events; assert that normal precision shots occur only after a stopped settle window, reload permits deliberate movement, and repeated left/right sign flips do not grow unbounded while a phase/position is unchanged.
- **Result:** The investigation and workflow design are complete. No implementation files or gameplay settings were changed; only this append-only journal entry was updated.
- **Validation:** Repository inspection, `git log`/`git blame`, serialized-settings review, existing bot regression-test review, and saved bot-battle telemetry review completed. A fresh Unity battle was intentionally not launched during this design-only pass.
- **Remaining limitation:** The exact live current-build shot-to-movement ratio and the best settle/reposition numeric thresholds still need a focused runtime capture during implementation; the historical logs establish the oscillation symptom but do not expose all phase inputs.
- **Status:** COMPLETED — 2026-11-06

## Task: Reproduce sniper reticle corruption after robot death

- **Status:** IN_PROGRESS
- **Started:** 2026-11-06
- **User request:** Reproduce the critical bug where the sniper-mode reticle/spread circle changes or breaks after the player's robot is destroyed, by measuring it before death, dying in a fight, returning to the main menu, selecting the same robot, and measuring it again; then locate the reset/lifecycle failure and fix the root cause.
- **Scope:** Use the existing game flow and supplied test account to perform a controlled runtime reproduction; record the sniper reticle parameters before and after death; inspect the owning gameplay/UI lifecycle code; apply the smallest focused fix only after the failure is confirmed; validate normal entry and post-destruction re-entry without disturbing unrelated worktree changes.
- **Acceptance criteria:** The same robot can enter sniper mode before and after a death cycle with identical intended spread-circle configuration and runtime behavior; the relevant lifecycle owner is identified; focused code/build checks and a runtime regression check pass; no unrelated discarded-work artifacts are restored.
- **Progress:** Journal entry created before repository inspection or game launch. Previous attempted changes were discarded per the user's report; current behavior must be reproduced from the present project state rather than inferred from those changes.
- **Discovery:** The current project has no standalone client build; the existing Unity `6000.3.13f1` Editor Client scene is the runtime client, while the desktop launcher successfully started the local API and dedicated server (`7216`/`7770`).
- **Discovery:** Runtime login with the user-provided test account succeeded. The selected robot is `IA Strider`; the camera advances through zoom indices `0`, `1`, `2`, `3`, and sniper index `4`, where the circular sniper overlay appears. Before death the reticle reaches `AIM READY` with HP `180/180`; the spread-circle visual is stable at that point.
- **Discovery:** The already-open Unity Editor was started before this task and its previous log contains `ReticleDebug` instrumentation that is absent from the current files after the user's discard. The first runtime pass therefore used stale loaded assemblies and is not accepted as current-code evidence.
- **Decision:** Stop Play Mode, close the pre-existing Editor cleanly, reopen the same project from disk, and repeat the minimum before/after-death reticle comparison before changing implementation files.
- **Next steps:** Launch the existing game, log in with the user-provided test account, measure the reticle through its sniper steps, reproduce death and re-entry, inspect the current reticle and robot lifecycle owners, then patch and validate only if the regression is confirmed.
- **Discovery checkpoint — 2026-11-06:** The clean current-code run completed the requested IA Strider cycle. At zoom profiles `4`/sniper entry, the reticle reached `AIM READY` before death and appeared unchanged after re-entry; the visual regression was not reproduced in that single faction/profile path.
- **Lifecycle finding:** `GunCrosshair.ResetAimingState` is currently reached from HUD enable/disable and `VehicleRoot.LocalPlayerVehicleChanged`, while `CameraController.DeathSpectatorCameraChanged` only rebinds spectator state. A death can therefore leave the reticle geometry dependent on despawn/event ordering instead of resetting at the guaranteed death-spectator boundary. `SetFaction` also swaps reticle targets without restoring the previous target's modified geometry first.
- **Decision:** Keep the existing profile sizing and dispersion formula unchanged. Harden the presentation owner by resetting aiming geometry when death-spectator state changes and restoring the old faction reticle before swapping bindings; this targets retained UI state without changing weapon spread or adding diagnostics.
- **Next steps after clean run:** Apply the focused `GunCrosshair` lifecycle fix, document the death/faction reset ownership in `docs/codex-game/ui-hud.md`, build the runtime/editor assemblies, then repeat the same-battle reticle check if the Unity window is available without user input.
- **Implementation:** Added a death-spectator transition reset and restored the previous faction reticle's authored geometry before `SetFaction` swaps targets in `Assets/Game/Scripts/UI/HUD/GunCrosshair.cs`; documented the ownership boundary in `docs/codex-game/ui-hud.md`.
- **Validation:** `Assembly-CSharp.csproj` and `Assembly-CSharp-Editor.csproj` both build successfully with `0` errors; existing `System.Net.Http` reference-conflict warnings remain; `git diff --check` passes.
- **Runtime validation:** The clean current-code run completed the same-robot death and re-entry comparison at sniper profile `4` without a visible change. A post-patch visual rerun was not possible because CUA returned the ChatGPT window instead of Unity while the user-controlled desktop focus was active.
- **Result:** The identified retained-geometry lifecycle gap is fixed at the focused UI owner without changing weapon dispersion/profile sizing or restoring discarded debug instrumentation. The pre-existing user change in `Assets/Game/Content/Server/Prefabs/ServerSettings.prefab` was left untouched.
- **Remaining limitation:** Profiles `5`-`7` and a post-patch visual pass still need confirmation in a user-focused Unity window.
- **Status:** COMPLETED — 2026-11-06

## Task: Start Docker Desktop minimized to tray

- **Status:** IN_PROGRESS
- **Started:** 2026-11-06
- **User request:** Start Docker Desktop from the launcher without opening a full-size window; keep it minimized/in the system tray while the engine starts.
- **Scope:** Verify local Docker Desktop startup support, update only the startup invocation and related documentation, and test the change while preserving the current API/server flow.
- **Acceptance criteria:** The launcher starts Docker Desktop minimized or tray-only, still waits for the Docker engine and database, and starts the API and dedicated server with clear failure diagnostics.
- **Progress:** The launcher currently uses PowerShell `Start-Process ... -WindowStyle Hidden`, but Docker Desktop still opened its own full-size GUI; a supported startup flag must be verified.
- **Next steps:** Inspect local Docker Desktop help/CLI, patch the startup invocation and docs, run a controlled restart test, and close the journal.
- **Discovery:** Docker CLI `29.8.2` exposes `docker desktop start --detach` but no tray/minimized option; the installed executable also ignored `--minimized` in a controlled test. Windows `CloseMainWindow()` hid the Dashboard while preserving the Docker engine and background processes.
- **Implementation:** Updated `C:\Users\TorasPC\Desktop\Start War of Machines.bat` to call a small PowerShell subroutine that closes only the Docker Desktop Dashboard during engine startup polling and after readiness. Updated `docs\codex-game\local-network-builds.md` to document the tray behavior.
- **Validation:** Static launcher/documentation checks and `git diff --check` passed. A controlled restart stopped Docker Desktop, confirmed Docker and port `5433` were unavailable, then the launcher started Docker Desktop automatically.
- **End-to-end validation:** The launcher reached Docker engine readiness, started PostgreSQL as `healthy` on `127.0.0.1:5433`, built/started the API and dedicated server, and completed with `API and game server are running.` API `https://localhost:7216/` returned `302 /admin`; server log confirmed UnityTransport listening on `7770`; API stderr was empty.
- **Tray validation:** After the launcher completed, all `Docker Desktop` processes had `MainWindowHandle=0`, while `docker info` remained healthy, confirming that the Dashboard was hidden without stopping Docker.
- **Final state:** Docker Desktop, PostgreSQL, API, and dedicated server are currently running; no database data or configuration was changed.
- **Result:** The launcher no longer leaves Docker Desktop opened as a full-size application window; it keeps Docker working in the background with its tray presence.
- **Remaining limitation:** Docker Desktop must provide its normal tray integration; the launcher hides the Dashboard window but does not alter Docker Desktop settings or tray notification-area configuration.
- **Completed:** 2026-11-06.
- **Status update:** COMPLETED

## Task: Fix CameraSync test assembly compile error

- **Status:** IN_PROGRESS
- **Started:** 2026-11-06
- **User request:** Fix Unity CS0246/CS0103 errors because `CameraRuntimeZoomOverridePlayModeTests.cs` cannot resolve `CameraSync`.
- **Scope:** Preserve the camera implementation and user worktree changes; adapt the focused regression test to the repository's test assembly boundary and validate Unity/runtime assembly compilation.
- **Acceptance criteria:** No direct unresolved `CameraSync` reference remains in the test assembly; focused builds/checks pass; unrelated changes remain untouched.
- **Progress:** The test directly imports `Game.Scripts.Gameplay.Robots.CameraSync`, while the existing test file uses reflection for runtime types; replace the direct type dependency with reflection.
- **Implementation:** Removed the direct runtime namespace import and changed the lifecycle regression test to resolve `CameraSync` and its static `In` field through reflection, matching the test assembly's existing pattern.
- **Discovery:** `Game.PlayMode.Tests.asmdef` references `Game.Gameplay.Shared` but not `Assembly-CSharp`, confirming that direct runtime-type references cannot compile in this test assembly.
- **Validation:** `Assembly-CSharp.csproj` and `Assembly-CSharp-Editor.csproj` build successfully with `0` errors; `git diff --check` passes; the test file contains no direct `CameraSync` type reference.
- **Result:** The CS0246/CS0103 errors are removed without changing the camera implementation or unrelated user worktree changes. The lifecycle regression remains covered through reflection, which is compatible with the test assembly boundary.
- **Remaining limitation:** The generated Unity PlayMode test assembly is not exposed as a standalone `.csproj`; Unity must complete its normal script reimport to display the cleared compiler state.
- **Status:** COMPLETED — 2026-11-06

## Task: Add detailed camera zoom lifecycle diagnostics

- **Status:** IN_PROGRESS
- **Started:** 2026-11-06
- **User request:** Add detailed logs so the user can compare zoom, camera position, projection, and sniper reticle state during the first battle, after death, and during the second battle.
- **Scope:** Instrument the existing camera/zoom lifecycle without changing gameplay behavior; log uniquely identifiable battle sessions, camera ownership, transform/FOV/projection, zoom profile values, and reticle geometry at the relevant transitions.
- **Acceptance criteria:** Logs clearly distinguish first entry, death/teardown, and second entry; each sample includes camera and zoom values needed for comparison; no gameplay logic is altered; focused build and diff checks pass.
- **Discovery:** `CameraController` owns zoom step, target/current distance, FOV application, camera pivot transform, sniper alignment, and death-spectator phases. `CameraSync` owns the gameplay camera reference and follows the controller target. `GunCrosshair` owns the requested/applied reticle diameters and aim status.
- **Decision:** Keep diagnostics in these existing owners. Log transition snapshots at session init, zoom changes, death/teardown, camera-sync rebinding, and at most twice per second while sniper/death-spectator state is active.
- **Implementation:** Added `[CameraZoomDiag]` logs to `CameraController.cs` for lifecycle sessions, zoom changes, camera-sync target mismatches/rebinds, death/spectator transitions, profile/runtime override values, transforms, FOV, and periodic settled snapshots. Added reticle diagnostics to `GunCrosshair.cs` for requested/applied local/server diameters, RectTransform geometry, visibility, and aim status.
- **Validation:** `Assembly-CSharp.csproj` and `Assembly-CSharp-Editor.csproj` build successfully with `0` errors; the initial diagnostic build caught and then cleared one quoting syntax error in `CameraSync.cs`; `git diff --check` passes. Existing `System.Net.Http` and Unity deprecation warnings remain unrelated.
- **Result:** Diagnostics are behavior-neutral and distinguish camera-controller sessions, `CameraSync` lifecycle, zoom profile/FOV/transform state, and reticle requested/applied geometry. The pre-existing `ServerSettings.prefab` and other user worktree changes were preserved.
- **Remaining limitation:** Runtime comparison still requires the user to run the first battle/death/second battle flow and return the `[CameraZoomDiag]` lines; no gameplay fix is claimed yet.
- **Status:** COMPLETED — 2026-11-06

## Task: Persist camera zoom diagnostics to a project log file

- **Status:** IN_PROGRESS
- **Started:** 2026-11-06
- **User request:** Write camera/zoom diagnostics to a text file inside the project so logs remain available after the game or Unity is closed and can be inspected later.
- **Scope:** Replace the current console-only diagnostic sink with a persistent append-only project log, preserving the existing diagnostic content and gameplay behavior; include timestamps and a clear file path.
- **Acceptance criteria:** Every `[CameraZoomDiag]` event is written to `Logs/CameraZoomDiagnostics.log` under the project root; writes are flushed during runtime; file I/O failures do not break gameplay; focused builds and diff checks pass.
- **Progress:** Current diagnostics are emitted from `CameraController`, `CameraSync`, and `GunCrosshair` through `Debug.Log`; centralize only the output sink in a dedicated runtime writer.
- **Implementation:** Added `CameraZoomDiagnosticsWriter` with append-only UTF-8 writes, per-line flush, project-root path resolution, process headers, timestamps, frame numbers, and one-time warning handling. Routed all `[CameraZoomDiag]` messages from `CameraController`, `CameraSync`, and `GunCrosshair` through the writer.
- **Validation:** `Assembly-CSharp.csproj` and `Assembly-CSharp-Editor.csproj` build successfully with `0` errors; `git diff --check` passes; the existing root `.gitignore` already ignores `Logs`, so the runtime log remains local.
- **Result:** Diagnostic events now append to `Logs/CameraZoomDiagnostics.log` with timestamp and frame metadata and remain available after the game or Unity closes. File-write failures are isolated to a one-time warning and do not affect gameplay.
- **Remaining limitation:** The log file will be created/populated on the next Unity runtime session; the requested first-battle/death/second-battle comparison still awaits the user's test.
- **Status:** COMPLETED — 2026-11-06

## Task: Analyze persisted camera zoom diagnostic sessions

- **Status:** IN_PROGRESS
- **Started:** 2026-11-06
- **User request:** Read the persisted camera/zoom logs and determine what differs between the first sniper session and the post-death second session.
- **Scope:** Inspect `Logs/CameraZoomDiagnostics.log`, segment sessions and lifecycle transitions, compare camera projection/FOV/transforms, zoom profile/distance values, `CameraSync` ownership, and reticle requested/applied geometry; do not change code until the evidence is understood.
- **Acceptance criteria:** Provide an evidence-based diagnosis with concrete log values and identify the exact lifecycle transition or remaining unknown; preserve all existing worktree changes.
- **Progress:** The persistent log writer is implemented and the user reports that the test run is complete; the log has now been read and normalized.
- **Discovery:** Session 1 starts at log line 7 and enters `DeathWreck` at line 179; session 2 starts at line 216. While session 2 is active, session 1 remains `DeathWreck` and both controllers use `GameplayCamera#223790` with session 2's target. At session 2 max sniper zoom, the requested profile FOV is 10 but actual FOV stabilizes around 34.44 because session 1 repeatedly writes its own FOV 60 or 75 to the same camera (for example lines 272-288, 346-376, and 408-418). The log records `CAMERA_SYNC_TARGET_MISMATCH` at line 218.
- **Diagnosis:** The camera/FOV is corrupted by an orphaned death-spectator controller, not by reticle geometry. Its update loop survives the new battle and writes to the global camera after rebinding; lifecycle handoff and ownership guards are required.
- **Implementation:** `CameraController.Init()` now ends a previous death spectator before taking the new camera target; ending a spectator clears its static ownership, target, cached sync, and initialization state. FOV writes require the controller to own the sync target, and sync rebinding no longer steals a sync already targeted at another controller. The unrelated unconditional zoom reset on death was reverted.
- **Validation:** `Assembly-CSharp.csproj`, `Assembly-CSharp-Editor.csproj`, and `Game.PlayMode.Tests.csproj` build successfully; `git diff --check` passes. Unity PlayMode execution was not available in this CLI pass, so the runtime reproduction should be rerun once to confirm the post-death sniper FOV reaches its requested profile.
- **Result:** The persisted log identifies and the code now addresses the real failure: an old `DeathWreck` controller was competing with the new battle's `CameraSync` and overwriting its FOV. The previous unconditional death zoom reset was removed.
- **Remaining limitation:** Runtime confirmation still requires one in-game death/re-entry test; the expected log should show session 1 ending before or during session 2 initialization, with no session 1 FOV snapshots writing to the new sync afterward.
- **Status:** COMPLETED â€” 2026-11-06

## Task: Diagnose Start War of Machines launcher regression

- **Status:** IN_PROGRESS
- **Started:** 2026-11-06
- **User request:** Investigate why `Start War of Machines.bat` stopped working again and restore reliable startup.
- **Scope:** Inspect the latest launcher output, Docker/Compose state, API/server processes and bridge files; reproduce the failure safely; make the smallest root-cause fix and validate a fresh start.
- **Acceptance criteria:** The launcher reaches Docker engine and PostgreSQL readiness, completes the existing Unity build bridge, starts API and dedicated server, and leaves clear diagnostics if any step fails.
- **Progress:** The failure point is not yet known; the previous tray-hiding change and current project working-tree changes must be preserved.
- **Next steps:** Collect current logs/state, reproduce or isolate the failing step, patch only the root cause, run focused and end-to-end validation, then close this entry.
- **Discovery:** The launcher exited with code `1` before Docker because its preflight reported `Unity Editor is not open for this project.` No matching `Unity.exe` process existed; Docker and PostgreSQL were healthy, so they were not the cause.
- **Reproduction:** Captured the failure in `D:\Programing\UnityProjects\war-of-machines-local-builds\launcher-regression-repro.log`; the launcher stopped at the Unity preflight as expected.
- **Resolution:** Opened the installed Unity `6000.3.13f1` editor for this project, waited for the initial asset import, and reran the unchanged launcher. The build bridge completed and the launcher reached `API and game server are running.`
- **Validation:** Docker engine `29.8.2` was ready; PostgreSQL was `healthy` on `127.0.0.1:5433`; API build succeeded; API returned `302 /admin` on port `7216`; the dedicated-server log confirmed UnityTransport startup; API stderr was empty.
- **Final state:** Unity Editor, Docker Desktop, PostgreSQL, API, and dedicated server are currently running.
- **Result:** No launcher code regression was found. The failure was caused by the required Unity Editor being closed; opening the project restored the existing launcher flow.
- **Remaining limitation:** `Start War of Machines.bat` still requires the Unity Editor for this project to be open because the dedicated-server build is performed through its Editor bridge. An auto-open enhancement can be added separately if desired.
- **Status:** COMPLETED — 2026-11-06

## Task: Implement deliberate bot combat phases

- **Status:** IN_PROGRESS
- **Started:** 2026-11-06
- **User request:** Implement the bot behavior fixes after the completed investigation: stop before aiming and firing accurately, allow controlled movement during reload/repositioning, and eliminate left/right combat oscillation.
- **Scope:** Add the smallest focused server-side phase/command behavior that prevents normal precision fire while the chassis is moving, keeps target/position commitments stable, preserves FishNet authority and existing weapon/reload flow, updates bot documentation, and adds focused regression coverage without reverting unrelated work.
- **Acceptance criteria:** Normal bot shots require a stopped settle window; reload/reposition can move without firing; movement and aim no longer compete through stale input writes; target/position/line-of-fire choices have hysteresis; focused tests/build checks pass; live behavior can be verified through telemetry.
- **Progress:** The prior design task identified the root cause and phase workflow. Global gameplay-separation and task-journal workflows were reread before implementation. Existing worktree changes must remain untouched.
- **Discovery:** `VehicleRoot.objectMover` exposes the authoritative movement component, while `BotNavigator` and combat both write server input. Existing tactic decisions allow moving fire for mobile tactics, and navigation positions refresh often enough to produce side-switching. The current PlayMode test assembly reaches runtime bot types through reflection.
- **Decision:** Add a focused `BotCombatEngagementController` for `Approach`, `Settle`, `ReadyToFire`, `Reloading`, and `Repositioning`; require an actual stopped chassis plus a short settle window before fire; make shooting force zero movement; reuse navigator-owned movement intent when not firing; lengthen tactical position commitment to reduce left/right churn.
- **Implementation:** Added `BotCombatEngagementController`, wired it into `BotCombatController`, forced zero movement in shot commands, cleared the shoot latch immediately after successful fire, exposed authoritative movement speed telemetry, reused navigator movement intent, and increased tactical navigation commitment to 1.8 seconds/10 meters. Added focused stopped-settle regression coverage and updated bot/settings ownership documentation.
- **Validation:** `dotnet build Assembly-CSharp.csproj --no-restore --nologo` passed with 0 errors and 4 pre-existing warnings; `dotnet build Game.PlayMode.Tests.csproj --no-restore --nologo` passed with 0 errors; `git diff --check` passed. Unity PlayMode/live 10v9 execution was not run in this CLI pass.
- **Result:** The fixes are now present as working-tree changes: bots approach/reposition while reload or line-of-fire is unavailable, suppress chassis motion while settling, fire only after stopping, and no longer send movement with a shot or retain a stale shoot latch.
- **Remaining limitation:** Runtime telemetry should still be collected in one Unity 10v9 battle to confirm lower direction-flip counts and shot-time speed near zero; no unrelated worktree changes were reverted.
- **Status:** COMPLETED — 2026-11-06

## Task: Fix spectator death camera handoff and delayed target switching

- **Status:** IN_PROGRESS
- **Started:** 2026-11-06
- **User request:** Fix spectator mode camera jitter when the player's robot is destroyed, exit sniper mode without a camera jump, and keep observing a destroyed ally's last point for five seconds before switching to the next robot.
- **Scope:** Diagnose the camera/death/spectator lifecycle and collision-related jitter; preserve sniper zoom and camera behavior for other gameplay flows; add focused regression coverage and update mechanic documentation if ownership changes.
- **Acceptance criteria:** Player death leaves the camera at its current position without explosion-fragment jitter; sniper mode exits cleanly without a jump; allied target death keeps the last observed point for five seconds, then selects the next valid robot; focused builds/tests and diff checks pass.
- **Progress:** Global gameplay-separation and task-journal workflows are read. Existing camera diagnostics and prior camera ownership fixes must be preserved; repository investigation is next.
- **Discovery:** `DeathLogic` detaches client debris onto the dynamic `Chassis` layer, while `CameraCollisionController` sphere-cast included that layer and could shorten the camera distance as fragments moved. `CameraController` also recalculated the death pose every frame, and invalid `AllyFollow` targets entered `DeathWreck` immediately.
- **Decision:** Keep spectator lifecycle ownership in `CameraController`; capture and hold the current world camera pose during local `KillerDelay` and allied-target loss, and ignore only unparented dynamic `Chassis` debris in `CameraCollisionController` so live/static collision behavior remains unchanged.
- **Implementation:** Added `AllyTargetLossHold` with a five-second unscaled timer and deterministic next-ally selection, froze the local death pose while exiting sniper state, added detached-debris filtering to camera collision, and updated `docs/codex-game/robot-control.md` with the ownership and lifecycle contract.
- **Validation:** `Assembly-CSharp.csproj`, `Assembly-CSharp-Editor.csproj`, and `Game.PlayMode.Tests.csproj` build successfully with `0` errors; `git diff --check` passes. Added regression coverage for death-pose stability, five-second target-loss holding, and detached-debris collision filtering. Unity batch PlayMode is not run because the project Editor is currently open, and the background-testing workflow forbids concurrent batch execution against the same `Library`.
- **Result:** Local death now captures the current camera world pose before teardown, exits sniper UI/FOV state without recomputing position during `KillerDelay`, and ignores only detached dynamic `Chassis` debris in collision resolution. Ally target loss now holds the last camera pose for five unscaled seconds and then selects the next eligible same-team vehicle in active order before falling back to `DeathWreck`.
- **Remaining limitation:** Unity PlayMode runtime execution remains pending until the open project Editor is closed; no batch process was started concurrently. Manual verification should reproduce local death from sniper mode, debris explosion, allied target loss, five-second hold, and automatic next-target selection.
- **Status:** COMPLETED — 2026-11-06

## Task: Fix spectator HP and speed HUD

- **Status:** IN_PROGRESS
- **Started:** 2026-11-06
- **User request:** Restore the bottom HP and speed displays while observing a robot in spectator mode.
- **Scope:** Trace the scene-owned HUD presenters through local vehicle despawn and spectator target changes, fix the focused binding/lifecycle issue without changing normal gameplay or sniper camera behavior, add regression coverage, and update HUD ownership documentation.
- **Acceptance:** Spectator HP follows the selected target; spectator speed is visible and updates from the selected target; normal local HUD and spectator camera/zoom behavior remain unchanged.
- **Progress:** The root cause is confirmed: `HealthBarUI` and `VehicleHudInitializer` live on the despawned local vehicle, while only scene-owned crosshair/map presenters reacquire `ActiveDeathSpectator`.
- **Decision:** Keep normal owner HUD updates in their vehicle components and add a focused scene-owned `SpectatorVehicleHudPresenter` on the existing `HealthBar` scene object; it is the only presenter that writes HP/speed during death spectator mode.
- **Implementation:** Added camera/target lifecycle binding, target HP/name polling, and movement-speed sampling for spectator mode; wired the presenter into `Client.unity` and documented the ownership boundary.
- **Validation:** Final `Assembly-CSharp.csproj`, `Assembly-CSharp-Editor.csproj`, and `Game.PlayMode.Tests.csproj` builds succeed with `0` errors; `git diff --check` passes. Added `SpectatorHudPresenterTracksTargetAfterLocalVehicleDespawn`. Unity batch imported and compiled the project with exit code `0`, but did not emit a test-results XML or execute the runner in this pass.
- **Result:** Spectator HP/name and speed now have a scene-owned presenter that binds to the active death camera and selected target, so local vehicle despawn no longer removes these HUD updates. Normal owner HUD and camera/sniper code remain unchanged.
- **Remaining limitation:** Runtime in-game verification still needs one manual spectator session covering local death, target cycling, target death, and HUD updates; no unrelated worktree changes were reverted.
- **Status:** COMPLETED — 2026-11-06

## Task: Check in-game performance diagnostics

- **Status:** IN_PROGRESS
- **Started:** 2026-11-06
- **User request:** Determine whether the game already has a diagnostic system for identifying what causes in-game lag.
- **Scope:** Inspect the repository for runtime diagnostics, profiling overlays, telemetry/logging, and Unity profiling integrations; do not modify implementation files.
- **Acceptance criteria:** Identify existing tools, explain what they can measure, and note any important gaps or usage limitations.
- **Progress:** Global memory, project journal, and journal-recovery workflow read. No dedicated performance-diagnostics workflow exists in the archive.

### Completion checkpoint — 2026-11-06

- **Discovery:** The project contains the Live Diagnostics Bridge under `Assets/Game/Scripts/Diagnostics`, configured in both `Assets/Game/Scenes/Client.unity` and `Assets/Game/Scenes/Server.unity`, plus the F9 runtime overlay and `game-diag.cmd` CLI.
- **Capabilities:** It samples client FPS/frame time and frame p95/max, profiler scope timings, server tick p95/max, entities/projectiles/bots/players, memory/GC counters, ping/jitter/loss, network throughput, RPC/event summaries, and writes JSONL sessions under `diagnostics/logs`.
- **Analysis:** The analyzer classifies likely causes as client-bound, server-bound, network-bound, memory/GC-bound, entity-scale-bound, RPC storm, or unknown. Existing hooks include projectile, movement, visibility, map HUD, ping, weapon, bot, and RPC paths.
- **Evidence:** The newest saved session (`diagnostics/logs/session-20261106-115737.jsonl`) contains frame spikes up to 673.393 ms and lists `Client.Projectile.Update` as the top suspect, demonstrating that the system can identify a concrete lag source.
- **Validation:** Documentation, runtime code, scene wiring, CLI, and existing logs inspected. `game-diag.cmd health` was attempted but timed out because no diagnostics HTTP listener was available at the time; no implementation files were changed.
- **Result:** Confirmed that the game has an in-game and external diagnostics system for investigating lag; usage instructions are in `docs/diagnostics.md`.
- **Remaining limitation:** Live CLI queries require the game/diagnostics bridge to be running; saved JSONL logs remain available for offline analysis.
- **Status:** COMPLETED

## Task: Deep runtime code performance optimization audit

- **Status:** IN_PROGRESS
- **Started:** 2026-11-07
- **User request:** Inventory the game's scripts, prioritize real-time systems such as robots, aiming, combat, movement, networking, and HUD, then optimize the code deeply without changing functionality or breaking existing behavior.
- **Scope:** Build a complete ordered script inventory; inspect ownership, hot paths, allocations, update loops, physics, networking, AI, aiming, projectile, UI, and supporting systems; apply only evidence-based, behavior-preserving optimizations; add or update focused regression coverage where needed.
- **Acceptance criteria:** The inventory is recorded; high-impact safe optimizations are implemented in focused batches; existing public/serialized behavior and network authority remain unchanged; each batch has targeted validation; Unity/runtime limitations are explicitly recorded.
- **Progress:** Global instructions, the project journal, task-journal recovery, gameplay-logic separation, and background Unity-testing workflows were read. No dedicated performance-optimization workflow exists in the shared archive. The repository contains 1,472 C# files: 254 under `Assets/Game` (241 runtime, 11 tests, 2 editor, and 1 scene script) plus 1,218 third-party/demo scripts that will be inventoried but not rewritten without a separate owner request. Saved diagnostics show historical `Client.Projectile.Update` spikes up to 641.454 ms with 5–7 projectile updates, making projectile collision and impact presentation the first evidence-based optimization target.
- **Discovery:** `Projectile.Update` performs ballistic math, per-frame `Physics.RaycastNonAlloc`, and on impact can call the registered vehicle impact-surface resolver; that resolver may raycast detachable mesh colliders and call `Physics.SyncTransforms`. The code is pooled and currently reports zero allocations in the saved spike samples, so the first changes must target repeated CPU/physics work rather than allocation-only micro-optimizations.
- **Implementation checkpoint:** Added a frame-local cache for repeated robot impact-surface resolution in `VehicleColliderRegistry` and reused `BallisticProjectileMath.TravelSegment.Distance` in `Projectile.Update` instead of calculating the same vector magnitude a second time. Hit selection, surface fallback, projectile pooling, and network callbacks remain unchanged.
- **Implementation checkpoint:** Added configuration-aware precomputed ground-ray offsets to `VehicleGroundAlignmentController`, removing per-frame `Mathf.Sin`/`Mathf.Cos` work for every alignment target while preserving ray order, radius, count, and serialized behavior.
- **Implementation checkpoint:** Replaced impact-surface candidate distance comparisons with squared distances in `VehicleColliderRegistry`; no selected collider, renderer, normal, or position is changed because square-root is monotonic for non-negative distances.
- **Implementation checkpoint:** Prevented `VehicleColliderRegistry` from raycasting a mesh collider twice when it is present in both the registered `ImpactVisualPart` list and the fallback mesh-collider list; separately registered colliders still use the fallback pass, and registration invalidates the frame cache.
- **Changed files:** `docs/codex-game/script-performance-inventory.md`, `Assets/Game/Scripts/Gameplay/Projectiles/Projectile.cs`, `Assets/Game/Scripts/Gameplay/Robots/VehicleColliderRegistry.cs`, and `Assets/Game/Scripts/Gameplay/Robots/VehicleGroundAlignmentController.cs`.
- **Validation checkpoint:** `dotnet build Assembly-CSharp.csproj --no-restore --nologo` and `dotnet build Game.PlayMode.Tests.csproj --no-restore --nologo` passed with 0 errors; the same 4 existing dependency/obsolete-API warnings remain in the main assembly.
- **Validation checkpoint:** Focused Unity runs passed: `CloseRangeProjectilePlayModeTests` 5/5 and `CombatParticleEffectsPlayModeTests` 9/9. The full PlayMode run passed 67 tests, with 7 unrelated existing/environment failures in 10v9 readiness, camera color, legacy dispersion UI, and match battle-end setup.
- **Result:** The complete ordered inventory is recorded, and the first safe runtime optimization batch is complete across projectile distance reuse, robot impact-surface caching/deduplication, squared-distance selection, and ground-ray offset precomputation. No third-party/demo scripts were rewritten.
- **Remaining limitation:** No sustained post-change in-match profiler capture was available in this CLI pass; the historical diagnostics baseline predates the changes. The full-suite failures remain outside the touched code and require a separate runtime/environment investigation.
- **Status:** COMPLETED — 2026-11-07

## Task: Audit runtime component lookup and LINQ usage

- **Status:** IN_PROGRESS
- **Started:** 2026-11-07
- **User request:** Check whether the game uses `GetComponent`, `FindObject`, related scene/component lookup methods, or LINQ.
- **Scope:** Search the repository, separate owned runtime code from tests/editor/third-party code, identify exact files and call sites, and report optimization-relevant findings without modifying implementation files.
- **Acceptance criteria:** All relevant lookup and LINQ patterns are inventoried with file references and categorized by risk/frequency; no code changes are made unless explicitly requested.
- **Progress:** Global instructions, workflow index/history, and the existing task journal were read. No dedicated lookup/LINQ audit workflow exists.
- **Discovery:** Across 1,472 C# files there are 244 component-lookup matching lines in 92 files. In owned `Assets/Game/Scripts` production code, there are 18 matching lines in 11 files; the remaining owned matches are in tests or editor tooling. The production calls are mainly `Awake`, startup, pooling, destruction, UI wiring, or dynamic icon creation rather than the robot movement/aim `Update` loops.
- **Discovery:** Production scene/object lookup is limited to `FindFirstObjectByType<Camera>()` in `Assets/Game/Scripts/Rendering/DecalWindowsSmokeCapture.cs` and `Resources.FindObjectsOfTypeAll<GameObject>()` in `Assets/Game/Scripts/UI/Lobby/GameResultUI.cs`. Test controllers contain additional `FindAnyObjectByType`, `FindObjectsByType`, and `Camera.main` calls.
- **Discovery:** `Assets/Game` contains no `using System.Linq` or `System.Linq.` references. LINQ is present in third-party/runtime dependencies, including FishNet and UniRx, plus editor tooling; apparent `.Max`, `.Min`, `.Contains`, `.ToArray`, and `.Join` calls in game code are math, collection, array, or string APIs rather than confirmed LINQ operators.
- **Discovery:** Two owned runtime `AddComponent` calls remain: `GameResultUI` creates its scene UI component when absent, and `GunCrosshair` creates a `CanvasGroup` while resolving reticle groups. Both are setup paths, not per-frame loops, but they conflict with the project rule preferring serialized/configured components.
- **Result:** Yes, the repository uses `GetComponent`/`GetComponents`, limited `Find...`/`Resources.Find...`, and runtime `AddComponent`; owned gameplay code does not use LINQ. No implementation files were changed because this request was an audit only.
- **Remaining limitation:** This pass inventories call sites and context but does not replace the setup lookups or third-party LINQ; any conversion should be a separate focused change with Unity regression validation.
- **Status:** COMPLETED — 2026-11-07

## Task: Full War of Machines performance audit and safe optimization

- **Status:** COMPLETED
- **Started:** 2026-11-07
- **User request:** Perform a repository-wide Senior Unity Performance Engineer audit and safe optimization of the War of Machines game without changing functionality, networking, balance, robot behavior, aiming, weapons, AI, HUD, or scenes.
- **Scope:** Inspect every C# script; inventory lifecycle methods, physics/network/component lookup patterns, searches, allocations, LINQ, pooling risks, and optimization priority; establish only verifiable baseline metrics; optimize justified owned runtime hot paths in focused batches; validate compilation and available tests; produce complete inventory, audit report, before/after measurements, changed-file list, unresolved risks, and third-party limitations.
- **Acceptance criteria:** Every repository C# script is categorized and audit-statused; no invented profiler or test results; changes preserve serialized behavior, authority, ordering, precision, and scene/prefab contracts; focused and broad validation is attempted where tooling permits; journal records discoveries, decisions, changes, validation, and limitations.
- **Discovery:** The working tree already contained unrelated-to-this-entry changes in `Projectile.cs`, `VehicleColliderRegistry.cs`, `VehicleGroundAlignmentController.cs`, and `TASK.md`; they are preserved as pre-existing state. No destination Unity Editor process was confirmed, but the diagnostics bridge was unavailable and `game-diag.cmd health` timed out after reporting no live API at `127.0.0.1:8765-8774`; no FPS, CPU, GC, physics, network, entity-count, or profiler-scope baseline is available from this CLI pass.
- **Discovery:** Generated `Assets/Game/Scripts`/third-party static inventory now covers all 1,472 `Assets/**/*.cs` files; `Library` contains 6,659 generated C# files and is excluded from source ownership. Categories are production 240, tests 15, editor 15, owned support/demo 37, and third-party 1,165. Static totals are 245 component-lookup matches, 50 Find/search matches, 131 AddComponent matches, and 70 confirmed LINQ import/Enumerable matches.
- **Change:** Added reproducible `tools/GenerateScriptPerformanceInventory.ps1` and regenerated `docs/codex-game/script-performance-inventory.md` with the requested lifecycle, physics, networking/RPC, lookup, search, AddComponent, LINQ, allocation-flag, priority, and audit-status columns for all source scripts.
- **Change:** In `GameplayMapHud.cs`, added object-ID indexing for tracked map markers and reused inactive mini/full marker instances instead of destroying/reinstantiating them on visibility snapshots; active marker lookup remains identity- and order-equivalent.
- **Change:** In `SpectatorVehicleHudPresenter.cs`, gated unchanged HP/name TMP writes and their interpolated string creation while preserving target rebinding and displayed values.
- **Change:** In `VehicleColliderRegistry.cs`, reused the main-thread activation buffer for temporary inactive-parent activation during mesh raycasts; activation order, `SyncTransforms`, collider state, and finally restoration remain unchanged.
- **Next steps:** Audit owned P0/P1 runtime hot paths by focused domain, distinguish safe cache/allocation changes from behavior-sensitive code, then validate each justified batch with builds/tests and update this entry.

### Completion checkpoint — 2026-11-07

- **Audit result:** Completed static inventory and ownership review for all 1,472 `Assets/**/*.cs` files. Third-party/demo source remains classified but unchanged; the full report is `docs/codex-game/performance-audit-report.md` and the complete table is `docs/codex-game/script-performance-inventory.md`.
- **Safe changes:** Completed the map marker object-ID index and marker reuse pool, spectator HP/name write gating, and reusable inactive-parent activation buffer. No `GetComponent*`, `Find*`, LINQ, network/RPC, scene, prefab, authority, balance, AI, aiming, weapon, or HUD behavior contract was mechanically changed.
- **Measured baseline:** Live diagnostics remained unavailable at `127.0.0.1:8765-8774`; no FPS, CPU, GC, physics, network, entity-count, or matched before/after profiler numbers are claimed. Historical logs are documented as context only.
- **Compilation:** `Assembly-CSharp.csproj`, `Assembly-CSharp-Editor.csproj`, and `Game.PlayMode.Tests.csproj` all build with 0 errors. Existing `System.Net.Http` conflict and editor/dependency obsolete-API warnings remain.
- **Unity validation:** Focused PlayMode passed projectile 5/5, combat particles 9/9, and spectator visibility 2/2. Full EditMode passed 21/21. Full PlayMode passed 67 and failed 7 pre-existing/environment cases. Focused 10v9 reproduced 1 host/client-readiness failure. `git diff --check` passed.
- **Deliverables:** Added the reproducible inventory generator and performance audit report; updated HUD ownership documentation with the new marker-pooling and text-write contracts.
- **Remaining limitations:** No runtime A/B profiler capture, no resolved 10v9 host/client readiness, no safe basis to migrate setup-time lookup fallbacks, and no third-party modifications.
- **Result:** COMPLETED — 2026-11-07.

## Task: Count current component lookups

- **Status:** COMPLETED
- **Started:** 2026-11-07
- **Scope:** Count current `GetComponent*` and `TryGetComponent` occurrences across source, owned scripts, and owned production runtime.
- **Validation:** Static regex count over all 1,472 `Assets/**/*.cs` source files; no implementation files changed.
- **Result:** 246 occurrences on 245 matching lines in 92 source files; owned `Assets/Game/Scripts` contains 39 occurrences, while owned production excluding tests/editor/testing contains 18 occurrences in 11 files.

## Task: Full heavy-method audit

- **Status:** COMPLETED
- **Started:** 2026-11-07
- **User request:** Analyze the entire project and provide a complete list of methods that may be heavy for CPU, physics, GC, rendering, UI, networking, startup, or scaling.
- **Scope:** Statistically scan all 1,472 `Assets/**/*.cs` files, inspect owned production candidates in context, classify third-party/test/editor candidates separately, rank methods by likely runtime cost and call frequency, and distinguish static evidence from profiler-confirmed facts.
- **Acceptance criteria:** The result includes path, line, method, cost mechanism, likely frequency, risk, ownership, recommended next action, and unresolved profiler requirements; no performance numbers are invented and no implementation changes are made unless explicitly requested.

### Completion checkpoint — 2026-11-07

- **Coverage:** Scanned all 1,472 `Assets/**/*.cs` files and generated 4,983 method-level heavy-operation candidates with path, line, method, ownership, static evidence, frequency candidate, risk, and next action.
- **Owned result:** 917 production candidates and 31 owned support/demo candidates were separated from test/editor/third-party code. The ranked list contains 30 owned P0 and 137 owned P1 static candidates; these are review priorities, not profiler measurements.
- **Highest-risk production areas:** projectile per-frame collision, camera collision, visibility LOS, server hit resolution, robot ground alignment, hover target raycast, weapon aim raycast, UI list/map construction, and diagnostics/UI logging allocations.
- **Physics qualification:** `RaycastNonAlloc` paths avoid result-array GC but remain CPU/physics candidates. `Physics.SyncTransforms`, multiple raycasts per robot/projectile, and mesh-collider activation/raycast paths require matched profiling before refactor.
- **Deliverable:** Added `tools/GenerateHeavyMethodAudit.ps1`, ranked owned report `docs/codex-game/heavy-method-audit.md`, complete all-source table `docs/codex-game/heavy-method-audit-all.md`, and linked both from `docs/codex-game/performance-audit-report.md`.
- **Validation:** Generator completed successfully over all 1,472 files. No gameplay implementation files were changed and no FPS/CPU/GC/physics/network numbers are claimed.
-
## Task: Heavy-method remediation options
- **Status:** COMPLETED
- **Remediation deliverable:** Added `docs/codex-game/heavy-method-remediation-options.md` with Tier A/B/C decisions, method-by-method safe/conditional/forbidden options, rollout batches, invariants, and focused validation matrix.
- **Implementation status:** No gameplay, networking, prefab, scene, or third-party implementation files were changed in this options task; recommendations remain ready for measured domain batches.
- **Validation:** Static source-context review completed; no FPS, CPU, GC, physics, network, or frame-time improvement is claimed.
- **Started:** 2026-11-07
- **User request:** Розписати, що саме і як можна безпечно виправити у важких методах усього проєкту без зміни логіки, мережевої поведінки, балансу, AI, HUD та сцен.
- **Scope:** Підготувати план remediation для всіх категорій зі статичного аудиту: projectile/combat, robots/movement, aiming/turrets, AI, networking, visibility, HUD/UI, audio/VFX, bootstrap та diagnostics/editor.
- **Acceptance criteria:** Для кожної значущої групи визначити безпечні, умовні та заборонені зміни; вказати інваріанти поведінки, порядок rollout і focused validation. У цьому етапі код гри не змінюється.
- **Result:** COMPLETED — the project-wide static list is available with production ownership and profiler limitations separated.

## Task: Implement behavior-preserving heavy-method remediation
- **Status:** COMPLETED
- **Implementation checkpoint:** Applied Tier A changes to `CameraCollision`, `FloatingDamageText`, `RobotEngineAudio`, `BotLineOfFireChecker`, `MatchVisibilityService`, and `GameplayPlayerListHud` without changing physics query parameters, hit selection, network scheduling, authority, or serialized scene values.
- **Optimization result:** Added stable transform/anchor caching, one-origin reuse for multi-probe bot LOS, geometric visibility snapshot growth, participant/entry pre-sizing, and O(1) player-list indexes with duplicate/stale fallback behavior.
- **Validation:** `Assembly-CSharp.csproj`, `Assembly-CSharp-Editor.csproj`, and `Game.PlayMode.Tests.csproj` build with 0 errors; `git diff --check` passes. Unity batch imported/compiled scripts but emitted no test-results XML because another Unity process already has the project open; no PlayMode pass is claimed.
- **Remaining limitations:** No live CPU/GC/physics/network/frame-time measurements are available. Physics scheduling/query changes, pooling, `Physics.SyncTransforms`, RPC/tick changes, and third-party changes remain intentionally deferred.
- **Started:** 2026-11-07
- **User request:** Implement the documented heavy-method remediation options without changing gameplay, networking, authority, balance, AI, aim, weapon, HUD, scene, or third-party behavior.
- **Scope:** Apply only measured Tier A changes first: stable-reference caching, reusable buffers/collections, diagnostics gating, unchanged-value write gating, and safe setup pre-sizing. Defer physics scheduling, query semantics, pooling, and network behavior changes until focused validation proves equivalence.
- **Acceptance criteria:** Each changed domain has a focused audit, implementation, compilation/test validation, and a journal checkpoint. No profiler numbers are claimed without runtime captures.

## Task: Runtime profiling and autobattle optimization
- **Status:** COMPLETED
- **Started:** 2026-11-07
- **User request:** Запустити гру, увійти тестовим користувачем, увімкнути автобій і провести фактичне Unity runtime-профілювання під час тривалого бою.
- **Scope:** Зібрати baseline і sustained-run метрики, використати наявну діагностику та Unity Profiler, знайти реальні CPU/GC/physics/network hotspots, внести лише поведінково-еквівалентні оптимізації та повторити вимірювання.
- **Acceptance criteria:** Не вигадувати метрики; мати фактичні before/after дані або чітко зафіксувати блокування; перевірити компіляцію, focused/full PlayMode за доступності та `git diff --check`; оновити звіт і список обмежень.
- **Runtime checkpoint:** Запущено наявний Client build з `-enableDiagnostics` на `8765` і локальний Server build з `-enableDiagnostics` на `8770`. Idle capture о `2026-11-07T20:07:14+08:00`: client `500.051 FPS`, `2.000 ms` frame, `2.002 ms` p95, `100.794 MB`; server tick `0.057 ms`, p95 `0.073 ms`, `0.096 ms` max, `0` active players, `6` active entities, `0` projectiles, `0` RPC/s. Це не є autobattle baseline.
- **Current limitation:** Доступний сеанс не експонує Computer Use `node_repl`/`@oai/sky`; пароль і внутрішню login-форму не автоматизую через shell. Клієнт залишено відкритим і користувачу запропоновано вручну завершити login → robot → battle → autobattle, після чого знімається фактичний sustained capture.
- **Runtime relaunch:** На запит користувача попередній client process зупинено, а новий Client build запущено о `2026-11-07T20:10:48+08:00` як PID `40060`, window `war-of-machines`, diagnostics session `20261107-121048` на `8765`. Server process і diagnostics на `8770` не перезапускалися.
- **Failure analysis:** Ручний smoke test був недійсним через stale Client: `WarOfMachinesClient.exe`/`Assembly-CSharp.dll` мали timestamp `2026-10-30`, тоді як runtime source/scene були змінені `2026-11-07`, а server був перебудований окремо. API log підтвердив успішний login і запити `Players`, `UserVehicles`, `UserVehicleResearches`, але FishNet connection зупинився. Client diagnostics зафіксували `activeEntities 6 → 9` при `activeVisibleEntities 0`; це пояснює порожню сцену, але не є підставою для runtime-оптимізацій.
- **Recovery:** Додано editor-only request bridge для client build у `Assets/Editor/LocalNetworkBuild.cs`. Через відкритий Editor Client успішно перебудовано о `2026-11-07T20:18:19+08:00`; нову пару запущено о `2026-11-07T20:19:10+08:00`: Client PID `34840`, Server PID `40188`, diagnostics sessions на `8765`/`8770` відповідають. Потрібен повторний ручний smoke test на свіжому build.
- **Autobattle capture:** Користувач вручну увійшов, обрав робота, зайшов у бій і ввімкнув автопілот. Client session `20261107-121923` та Server session `20261107-121911` зняті у 60-секундному та 90-секундному вікнах (`Temp/autobattle-profile-20261107.json`, `Temp/autobattle-profile-20261107-followup.json`). Перший window: client average `260.437 FPS` / `3.955 ms`, server tick average `0.176 ms`, `1` player, `19` bots, `0–1` projectiles, `0%` packet loss. Follow-up: client average `486.020 FPS` / `2.068 ms`, server tick average `0.150 ms`, memory stable around `259.961 MB`, diagnostics spikes `[]` on both processes.
- **Measured hotspot:** Єдиний періодично дорогий scope — `Client.Projectile.Update` (`13` samples, average `20.725 ms`) та `Server.Projectile.Update` (`18` samples, average `24.413 ms`) у першому вікні; у follow-up server scope з'явився лише `5` разів. `Camera.Controller.Update` мав average `0.604 ms` і нижчий за пороги. Це не sustained per-frame навантаження; impact path потребує окремого capture під активною стрільбою.
- **Optimization decision:** Не знижував cadence projectile/AI/network, не міняв layer masks, hit ordering, authority, `Physics.SyncTransforms` або collision semantics. Поточні captures не підтверджують sustained lag, а ці зміни могли б зламати поведінку. Залишений кандидат для наступного matched capture — `VehicleColliderRegistry.TryRaycastMeshCollider` та impact-surface resolution.
- **Validation:** `Assembly-CSharp.csproj`, `Assembly-CSharp-Editor.csproj` і `Game.PlayMode.Tests.csproj` зібрані з `0` errors; `git diff --check` пройшов. Unity runtime compilation/build використано для свіжого client build до capture; повний matched PlayMode/A-B profiler comparison не заявляється.
- **Result:** COMPLETED — фактичний autobattle capture виконано, sustained CPU/network lag signature не підтверджено, безпечні попередні оптимізації залишено, а behavior-sensitive projectile impact path не змінено без доказу еквівалентності.

## Task: Complete heavy-method inventory refresh

- **Status:** COMPLETED
- **Started:** 2026-11-07
- **User request:** Пройтися по всьому коду і написати всі важкі методи, зокрема `GetComponent`, які є у скриптах гри.
- **Scope:** Оновити повний статичний аудит усіх `Assets/**/*.cs`, відокремити production/test/editor/third-party, перелічити важкі Unity API, physics, networking, allocation, LINQ, search, UI та lifecycle-методи з шляхом, рядком, частотою/ризиком і статусом.
- **Acceptance criteria:** Не вигадувати runtime-метрики; показати фактичні source-pattern counts, дати повний deliverable або посилання на нього, окремо позначити вже усунені, behavior-sensitive та third-party кандидати.
- **Inventory result:** Оновлено скан усіх `1,472` C#-файлів. Повний список містить `4,984` static heavy-method candidates: `917` production, `31` owned support/demo, `189` test, `109` editor і `3,738` third-party; priorities: `30` P0, `137` P1, `781` P2, `4,036` N/A.
- **Lookup result:** Загалом source-pattern inventory: `227` component lookup matches, `49` Find/search matches, `129` AddComponent matches і `70` confirmed LINQ import/Enumerable matches. У production runtime (`Assets/Game/Scripts` без Tests/Testing/Editor) актуально `0` `GetComponent*`, `TryGetComponent`, global `Find*`, `Camera.main`, runtime `AddComponent` або `Shader.Find`; залишені lookup-и належать test/editor harness.
- **Deliverables:** Оновлено `docs/codex-game/heavy-method-audit.md`, `docs/codex-game/heavy-method-audit-all.md` і `docs/codex-game/script-performance-inventory.md`. Повний методний список із path/line/category/priority/evidence/frequency/risk знаходиться в `heavy-method-audit-all.md`; ranked production shortlist — у `heavy-method-audit.md`.
- **Validation:** `tools/GenerateHeavyMethodAudit.ps1` і `tools/GenerateScriptPerformanceInventory.ps1` виконані успішно через Windows PowerShell з `ExecutionPolicy Bypass`; `git diff --check` пройшов. Runtime code не змінювався в цьому inventory task; static candidates не є profiler measurements.
- **Remaining limitations:** `RaycastNonAlloc`, mesh-collider raycast/`Physics.SyncTransforms`, loops, UI rebuilds, allocations і networking calls залишаються кандидатами до профілювання; third-party/test/editor code не можна механічно змінювати.
- **Result:** COMPLETED — повний актуальний static список важких методів сформований і розділений за ownership та runtime-ризиком.

## Task: Plan safe rewrites for heavy methods

- **Status:** COMPLETED
- **Started:** 2026-11-07
- **User request:** Визначити, які важкі методи можна замінити на легші без поломки логіки, і описати конкретні скрипти та спосіб переписування.
- **Scope:** Зіставити production P0/P1 methods із безпечними Tier A/B/C remediation options; окремо позначити Inspector/cache/buffer/UI pooling/physics/network зміни, які потребують доказу еквівалентності.
- **Acceptance criteria:** Для кожної групи вказати файл/метод, запропоновану заміну, що саме не можна змінювати, ризик і focused validation; не робити implementation changes у design-only відповіді.
- **Decision:** Production `GetComponent*`/`Find*`/runtime `AddComponent` уже мають `0` matches; test/editor lookup-и залишаються generic harness code. Безпечний rewrite-план — Tier A caches/reused buffers/unchanged-value UI writes, потім окремі профільовані physics/visibility/pooling експерименти.
- **Safe rewrite groups:** `Projectile`, `VehicleColliderRegistry`, camera/aim/LOS, ground/suspension, HUD rows/map/tree, audio/VFX і diagnostics отримали конкретні cache/buffer/pooling варіанти з invariants. Запропоновано не змінювати raycast order/masks, tick/RPC cadence, authority, AI timing, `Physics.SyncTransforms`, material instance semantics або pooling reset contracts.
- **Validation plan:** Кожен батч має окремо пройти compile, focused PlayMode, exact hit/target/order checks, scene reload/despawn, spectator/HUD checks, AI battle, network authority і 10v9; design-only task не запускав runtime tests.
- **Result:** COMPLETED — підготовлено behavior-preserving rewrite map із безпечними, умовними та забороненими змінами; implementation не виконувався.

## Task: Remove owned runtime component lookups and heavy searches

- **Status:** COMPLETED
- **Started:** 2026-11-07
- **User request:** Fix the remaining component lookups and other heavy methods across the project, using Inspector references or focused registries/caches where appropriate.
- **Scope:** Re-audit every C# source file; remove or safely relocate owned production `GetComponent*`, `TryGetComponent`, `Find*`, `Camera.main`, runtime `AddComponent`, LINQ, and other avoidable hot-path work without changing gameplay, networking, scenes, prefabs, authority, balance, AI, weapons, aiming, HUD behavior, or third-party ownership.
- **Acceptance criteria:** Every owned production lookup has a documented disposition; stable UI/prefab references are Inspector-wired where safe; dynamic references use lifecycle caches/registries; compilation and focused tests validate each batch; third-party and behavior-sensitive calls are explicitly documented rather than mechanically changed.

### Completion checkpoint — 2026-11-07

- **Inventory:** Regenerated the complete 1,472-row source inventory. Current static totals are 227 component-lookup call matches, 49 Find/search matches, 129 `AddComponent` matches, and 70 confirmed LINQ import/`Enumerable` matches across all `Assets/**/*.cs`.
- **Lookup result:** Owned production runtime now has 0 `GetComponent*`, `TryGetComponent`, global Unity-object search, runtime `AddComponent`, or `Shader.Find` calls. The remaining 21 owned lookup calls are limited to editor/test harnesses that inspect arbitrary or dynamically created objects; third-party/library calls were not modified.
- **Inspector wiring:** Stable references were serialized and wired for HUD graphics/groups, countdown components, timer authority, Game Result presenter, robot roots, destruction/debris visuals, pooled particle arrays, smoke capture camera, and URP outline shaders/material fallback references.
- **Other optimizations:** Preserved and documented map marker indexing/pooling, spectator text-write gating, reusable collider activation buffers, ballistic distance reuse, ground-ray offset precomputation, squared-distance impact selection, duplicate collider suppression, and removal of repeated debris/particle child scans.
- **Changed production scripts:** 18 owned C# scripts were changed in this lookup-refactor batch; scene/prefab/renderer asset wiring was updated only for stable authored references.
- **Validation:** `Assembly-CSharp.csproj`, `Assembly-CSharp-Editor.csproj`, and `Game.PlayMode.Tests.csproj` each build with 0 errors. Inventory regeneration completed with 1,472 rows. `git diff --check` passed. Unity batch invocation returned exit code 0 but emitted no focused test XML, so it is not counted as a test pass.
- **Measured metrics:** No live matched profiler session was available; FPS, CPU, GC, physics, network, entity-count, and frame-time improvements remain unmeasured and are not fabricated.
- **Deliverables:** Updated `docs/codex-game/script-performance-inventory.md`, `docs/codex-game/performance-audit-report.md`, and `docs/codex-game/ui-hud.md`; the full task result and limitations are recorded in the performance report.
- **Remaining limitations:** Test/editor dynamic lookups, third-party code, behavior-sensitive `Physics.SyncTransforms`, death-only effect instantiation, and setup-time collection copies remain documented rather than mechanically changed. A matched runtime A/B capture is still required for factual performance deltas.
- **Result:** COMPLETED — all safe owned production component/search fallbacks requested in this batch are removed or Inspector-wired without changing gameplay/network/authority contracts.

## Task: Organize game files into a clear Unity folder structure

- **Status:** IN_PROGRESS
- **Started:** 2026-11-09
- **User request:** Sort all files used by the game into clear folders with clear names, and move files unrelated to the project outside the `Game` folder.
- **Scope:** Inventory the Unity project, determine which assets/scripts/scenes are game-owned versus third-party, editor/tooling, generated, or documentation content, then reorganize only safe project-owned files while preserving Unity GUIDs, serialized references, package/configuration behavior, and existing user changes.
- **Acceptance criteria:** Game-owned content is grouped clearly under `Assets/Game`; unrelated project content is moved outside `Assets/Game` into purpose-named folders; Unity `.meta` files move with their assets; no duplicate GUIDs or broken local references are introduced; the final structure, changed files, validation, and limitations are recorded.
- **Progress:** Global instructions, project instructions, existing journal, and task-journal recovery workflow were read. No dedicated archive workflow for Unity folder organization exists; applying safe Unity asset relocation and journal rules.
- **Next steps:** Inspect repository status and asset tree, classify ownership and references, propose/execute a minimal safe folder map, validate GUID/reference integrity, and close this entry.

### Organization checkpoint — 2026-11-09

- **Inventory:** `Assets` contains the project-owned `Game` tree plus external packages, editor tooling, imported source packs, diagnostics artifacts, and generated Unity folders. Existing working-tree changes are extensive and pre-date this task; they remain untouched.
- **Dependency findings:** Active game content outside `Assets/Game` is present in `PartDone\Etallon` (12 combat-effect prefabs), `Resources\GameAudio` (game audio referenced by `AudioManager`), `TransportAudioDemo\Audio\Engine` (30 engine clips referenced by `AudioManager`), 20 assets under `Unused` (UI assets referenced by game scenes/prefabs), and active render/build settings under `Settings` (referenced by `ProjectSettings`). FishNet, Layer Lab, TextMesh Pro, JMO, Plugins, and other vendor/demo folders remain external dependencies and will stay outside `Game`.
- **Internal cleanup candidates:** Move unreferenced `Assets/Game\1.png`–`6.png` outside `Game`; move `room.mat` into environment materials; rename `IndustrialMap_v100_v200` to `IndustrialMap`; rename robot faction folders `IA`/`NV` to `IronAlliance`/`NovaSyndicate`; move editor-only scripts under `Assets/Editor/Game`.
- **Safety decisions:** Preserve every Unity `.meta` file and GUID during moves/renames. Update only path-based test constants and documentation where required; do not alter serialized object data, gameplay logic, scene content, or existing unrelated changes.
- **Next steps:** Apply the approved moves in small groups, then validate GUID uniqueness, local reference resolution, path-string references, and the working-tree diff.

### Implementation checkpoint — 2026-11-09

- Moved the active combat-effect package from `Assets/PartDone/Etallon` to `Assets/Game/Content/Combat/Effects`, including its folder metadata and nested dependencies.
- Moved active audio from `Assets/Resources/GameAudio` to `Assets/Game/Content/Audio/Clips` and engine recordings from `Assets/TransportAudioDemo/Audio/Engine` to `Assets/Game/Content/Audio/Engine`; the transport demo scene/scripts remain outside `Game`.
- Moved Unity render/build settings from `Assets/Settings` to `Assets/Game/Content/Configuration/UnitySettings` and renamed `Build Profiles` to `BuildProfiles`.
- Renamed `IndustrialMap_v100_v200` to `IndustrialMap`, `Robots/IA` to `Robots/IronAlliance`, and `Robots/NV` to `Robots/NovaSyndicate`; moved `room.mat` to `Environment/Map/Materials/IndustrialRoom.mat`.
- Moved editor-only game scripts to `Assets/Editor/Game` and moved six unreferenced root images from `Assets/Game` into `Assets/Unused/Backgrounds` with legacy names.
- Moved 20 confirmed game-used UI assets out of `Assets/Unused` into semantic UI folders and names.
- Updated path-based test constants and current asset/gameplay documentation. No gameplay, serialized component, scene object, or GUID content was intentionally edited.

### Completion checkpoint — 2026-11-09

- **Result:** Game-owned runtime content is consolidated under `Assets/Game` with clear `Content`, `Scenes`, `Scripts`, and `Tests` roots. Active combat effects, audio clips, engine recordings, UI assets, Unity settings, map content, and robot faction content now live in purpose-named folders. Editor-only game tools are under `Assets/Editor/Game`.
- **Naming:** Renamed `IndustrialMap_v100_v200` to `IndustrialMap`, `IA` to `IronAlliance`, `NV` to `NovaSyndicate`, `room.mat` to `IndustrialRoom.mat`, and ambiguous active UI asset names to semantic names. Six unreferenced root images were moved outside `Game`.
- **External content:** FishNet, plugins, vendor packs, demo scenes/scripts, unreferenced imported effects, legacy shaders, and remaining unused assets stay outside `Assets/Game` in their existing top-level folders; active resources extracted from those buckets were moved into `Game`.
- **Validation:** Expected target paths exist; all old source directories are absent; 0 assets lack adjacent `.meta`; 9,255 local GUID entries were scanned with 0 duplicate GUID groups; 0 active old-path references remain outside historical journal/handoff records; `git diff --check` passed.
- **Validation limitation:** Unity Editor reimport/PlayMode was not run in this CLI pass. Unity should reimport the moved assets before the next runtime test; serialized references were preserved by moving every `.meta` with its asset.
- **Pre-existing changes:** Existing user modifications and unrelated untracked audit files remain preserved; no reset, revert, or commit was performed.
- **Status:** COMPLETED

## Task: Optimize and recolor ExplosionProjectileRobot particle effect

- **Status:** IN_PROGRESS
- **Started:** 2026-11-09
- **User request:** Optimize `ExplosionProjectileRobot` without breaking its behavior, find the cause of its green tint, and change the effect to a fiery orange-yellow palette.
- **Scope:** Inspect the prefab's particle systems, materials, gradients, modules, and referenced assets; apply only safe visual/performance changes to the effect; preserve prefab structure, component references, timing intent, spawn behavior, and existing Unity GUIDs.
- **Acceptance criteria:** The green tint cause is identified and corrected; the effect reads as orange-yellow fire; safe optimization reduces avoidable particle cost without removing required visual layers; prefab serialization remains valid and focused validation passes.
- **Progress:** Global instructions, project journal, workflow index/history, and task-journal process were read. No dedicated particle-effect workflow exists in the archive. The active prefab has four particle systems with bounded caps and disabled heavy modules; its warm gradients contain no green values. The visible tint source is the shared scene-dependent `WFX_S Smoke Scroll` path used by the smoke layers, while the additive glow textures are alpha-only.
- **Decision:** Keep the shared vendor material unchanged. Add a prefab-local orange fire-tint smoke material, saturate the active fire gradients, and force no motion vectors on the four one-shot particle renderers. Preserve burst counts, timings, particle caps, component IDs, and existing asset GUIDs.
- **Result:** Added `ExplosionProjectileRobotFireSmoke.mat` under the effect's own `Materials` folder and assigned it only to the two smoke renderers. The local material keeps the existing smoke shader/texture but uses a fire-orange tint; the root and child fire gradients were saturated toward yellow-orange. All four active particle renderers now use `m_MotionVectors: 2` (`ForceNoMotion`) without changing particle caps, burst counts, lifetimes, spawn order, or serialized component IDs.
- **Validation:** Static prefab audit passed: 4 `ParticleSystemRenderer` blocks found, all use ForceNoMotion; both smoke references resolve to the new material; shader, texture, and material GUIDs resolve; 0 green-dominant serialized color lines; particle caps remain `90, 40, 11, 40`; burst modules remain `0, 3, 1, 4`; no new material trailing whitespace; `git diff --check` passed for the journal.
- **Limitation:** Unity Editor reimport/PlayMode visual verification was not run in this CLI pass. The existing URP `DecalProjector` script GUID remains package-provided and unchanged.
- **Status:** COMPLETED

## Task: Remove green tint from Explosion child particle

- **Status:** IN_PROGRESS
- **Started:** 2026-11-09
- **User request:** The remaining green tint comes from the inner `Explosion` object in `ExplosionProjectileRobot`; find the cause and make it white or yellow, never green.
- **Scope:** Inspect only the nested `Explosion` particle system, its renderer/material/shader/texture/colors, and apply a local visual fix without changing burst behavior or unrelated shared assets.
- **Acceptance criteria:** Identify the actual green source; ensure the inner `Explosion` renders white/yellow; preserve component references, timing, particle counts, and prefab serialization; run focused static validation.
- **Progress:** Global memory instructions, workflow index/history, the task-journal recovery workflow, and the existing journal were read. No dedicated particle workflow exists. Previous task is closed; this is a separate focused follow-up. The nested `Explosion` object uses the smoke material/shader path; its serialized particle gradients contain no green-dominant values, and its smoke texture is grayscale/alpha-only.
- **Finding:** The remaining tint source is the nested `Explosion` renderer's `WFX_S Smoke Scroll` shader, whose `Blend DstColor SrcAlpha` path multiplies the smoke RGB by the destination/background. This allows a green scene/background to leak into the effect even when the particle colors are warm.
- **Decision:** Keep the parent's smoke layer unchanged. Give only the nested `Explosion` renderer a local alpha-additive material using the existing smoke alpha texture and the existing `WFX_S Particle Add A8` shader, with a white/yellow tint. Preserve its bursts, lifetime, particle cap, transform, and renderer settings otherwise.
- **Result:** Added `ExplosionProjectileRobotFireExplosion.mat` and assigned it only to the nested `Explosion` renderer. The new material uses the existing smoke alpha texture with `WFX_S Particle Add A8` and a white/yellow tint, removing the destination-color multiplication that allowed green background tinting. The parent `Smoke` layer remains on its own local fire-smoke material.
- **Validation:** Nested `Explosion` renderer resolves to the new material GUID; the material resolves to the additive particle shader and existing smoke texture; `lengthInSec: 6`, `simulationSpeed: 10`, `maxNumParticles: 40`, and `m_BurstCount: 4` remain unchanged; 0 green-dominant serialized color lines; 0 missing local GUID references; no trailing whitespace in the new material; `git diff --check` passed for the journal.
- **Limitation:** Unity Editor reimport/PlayMode visual verification was not run in this CLI pass.
- **Status:** COMPLETED

## Task: Fix GitHub push rejection for oversized audio file

- **Status:** COMPLETED
- **Started:** 2026-11-09
- **User request:** Help push the current commit after GitHub rejected the push.
- **Scope:** Inspect the push error, repository state, commit history, and Git LFS configuration; prepare the smallest safe correction without deleting or resetting user changes.
- **Acceptance criteria:** Identify the exact oversized-file cause, preserve the audio asset and existing work, configure an appropriate tracked-file strategy, and validate the resulting Git state and push readiness.
- **Progress:** The screenshot shows GitHub rejecting `Assets/Game/Content/Audio/Clips/Music/Garage_DevelopmentTree_Music.wav` at approximately 199 MB because it exceeds GitHub's 100 MB per-file limit; the remote also reports `GH001: Large files detected`. Git LFS is installed. The existing commit was amended so the file is now a 134-byte LFS pointer, `.gitattributes` tracks its new path, and `git lfs fsck` passes.
- **Resolution:** The existing `~/.ssh/scorpioner2010` key successfully authenticated to GitHub. The amended commit was pushed through `GIT_SSH_COMMAND` without changing the repository's configured remote URL.
- **Validation:** `git lfs fsck` passed; the oversized audio is stored as a 134-byte LFS pointer in commit `8bf3a2e4`; `git push origin main` uploaded 1 LFS object (209 MB) and advanced GitHub `main` successfully.
- **Remaining limitation:** SourceTree may still need its SSH client/key configured to use `~/.ssh/scorpioner2010`; the repository remote remains `git@github.com:scorpioner2010/war-of-machines-unity.git`.
- **Result:** COMPLETED â€” the original 199 MB GitHub rejection was fixed without deleting the audio or resetting project changes, and the corrected commit is now on `origin/main`.

## Task: Reorient and recolor GunShoot muzzle effect

- **Status:** IN_PROGRESS
- **Started:** 2026-11-09
- **User request:** `GunShoot` is aimed at the robot instead of along the shot direction; rotate it to the opposite side and remove its slight green tint.
- **Scope:** Inspect the `GunShoot` prefab, its transforms, particle renderers, materials, shaders, textures, and active color gradients; make a focused prefab/effect-only fix without changing weapon gameplay or shared vendor assets.
- **Acceptance criteria:** The effect's authored forward direction is reversed safely; the green source is identified and corrected to white/yellow/fire; component references, timing, burst behavior, and serialization remain valid; focused static validation passes.
- **Progress:** Global instructions, the task-journal recovery workflow, and the existing journal were read. No dedicated particle workflow exists. Prior particle tasks are closed; this is a new focused follow-up. `GunShoot` has an identity root rotation; its active hemisphere shapes are rotated `+90°` on X and the nested `Explosion` also adds local `VelocityModule.z = +4`, so the authored forward direction is local `+Z`.
- **Finding:** The green tint is not present in serialized particle gradients (0 green-dominant lines). Both `Smoke` and `Explosion` use the shared `WFX_M_ExplosiveSmoke` / `WFX_S Smoke Scroll` path with destination-color blending, which can inherit a green scene/background.
- **Decision:** Rotate only the `GunShoot` root by 180° around Y, reversing all child particle motion from local `+Z` to `-Z` without editing each module. Assign both smoke renderers a prefab-local additive material using the existing grayscale smoke alpha texture and a white/yellow tint; do not modify vendor materials or gameplay references.
- **Result:** Added `GunShootFireSmoke.mat` under `Assets/Game/Content/Combat/Effects/GunShoot/Materials` and assigned it only to the `Smoke` and `Explosion` particle renderers. The material uses the existing grayscale smoke alpha texture, `WFX_S Particle Add A8`, and a white/yellow tint, removing the shared destination-color smoke blend that caused the green cast. Rotated the `GunShoot` root to 180° around Y so all child layers and the nested `Explosion` velocity reverse from local `+Z` to `-Z` together.
- **Validation:** Root quaternion/euler hint resolve to Y=180; both smoke renderers resolve to the new material; 0 old shared smoke references; 0 green-dominant serialized color lines; 0 missing local GUID references; new material has no trailing whitespace; existing particle component structure and timing fields were not edited; `git diff --check` passed for the journal.
- **Limitation:** Unity Editor reimport/PlayMode visual verification was not run in this CLI pass.
- **Status:** COMPLETED
- **Result:** COMPLETED — the original 199 MB GitHub rejection was fixed without deleting the audio or resetting project changes, and the corrected commit is now on `origin/main`.

## Task: Clean combat logs and diagnose false miss results

- **Status:** IN_PROGRESS
- **Started:** 2026-11-09
- **User request:** Temporarily remove unnecessary project logs so the log stays clean, add detailed hit logs with hit status, armor thickness, and effective/reduced armor, and investigate/fix the bug where damage is sometimes not applied while the hit notification reports a miss even though the projectile hit the enemy.
- **Scope:** Inspect the projectile collision, server-side hit/damage resolution, armor/penetration calculation, network hit notification, and logging call sites; remove noisy non-essential logs, add focused diagnostic hit logs, fix the root cause if confirmed, and preserve authority/transport boundaries.
- **Acceptance criteria:** Unnecessary logs are disabled or removed without hiding errors; each resolved hit records status, armor thickness, and effective/reduced armor; hit notifications agree with authoritative damage resolution; focused static/tests or Unity validation are run where available; unrelated working-tree changes remain untouched.
- **Progress:** Global instructions, repository journal, task-journal recovery workflow, gameplay-logic separation workflow, and memory history were read. No dedicated combat-log workflow exists.
- **Findings:** The active Nova walker prefabs keep an enabled `CharacterController` on Unity's `Default` layer while armor colliders use `Armor`. `Projectile.TryCastCollision` treats `Default` as a blocking world layer and therefore can select the robot `CharacterController` before an armor collider. That collider is not in `additionalVehicleColliders`, so `ServerHitResolver` cannot resolve a `VehicleRoot`; `BuildResolvedShot` sets the HUD status to `Miss` and `ApplyResolvedShotDamage` skips damage.
- **Ownership decision:** Keep authority and damage in `NetworkWeaponShooter`/`ServerHitResolver`, keep collider-to-vehicle fallback and mapping in `VehicleColliderRegistry`, and keep collision candidate selection in `Projectile`. Add only focused authoritative hit-resolution logs in the combat owner; do not add logging to client-only prediction.
- **Logging decision:** Remove ordinary informational runtime diagnostics/ballistic debug logs while preserving warnings/errors. Add one authoritative `[CombatHit]` line per server hit or true miss with status, armor zone, base armor, effective line-of-sight armor, penetration, and damage.
- **Changes:** `Projectile` now treats a `Default`-layer collider under a `VehicleRoot` as a vehicle candidate and prefers same-root `Armor` colliders over the robot's default `CharacterController`. `VehicleColliderRegistry.TryGetRoot` falls back to the parent hierarchy. `NetworkWeaponShooter` logs authoritative hit/miss resolution and carries armor metrics into the log path. Removed ordinary runtime diagnostics, ballistic-debug, and test-controller info logs while retaining warnings/errors and the dedicated smoke-test result log. Added a focused PlayMode regression and documented the ownership/selection rule in `docs/codex-game/weapons-damage.md`.
- **Validation:** `git diff --check` passed. Static assertions passed for the collision fallback, regression test, and all `[CombatHit]` fields. The open Unity Editor regenerated `Library/ScriptAssemblies/Assembly-CSharp.dll` after the gameplay changes without visible compiler errors in the checked worker logs. An initial log assertion wrapper reported a false failure because `rg` interpreted the bracketed marker; the corrected `Select/IndexOf` assertion passed.
- **Limitation:** The dedicated Unity PlayMode batch run was not executed because the same project is open in Unity Editor; the background-testing workflow forbids concurrent batch mode and no process was terminated. The user should run `CloseRangeProjectilePlayModeTests` after closing the Editor, then verify a live server hit log and HP change in a match.
- **Result:** The false miss/damage path caused by robot `CharacterController` colliders on `Default` is fixed at collision selection and root resolution. Authoritative logs now expose status, base armor, effective armor, penetration, and damage while the normal log remains quiet.
- **Completed:** 2026-11-09.
- **Status:** COMPLETED
## Task: Fix VehicleRoot namespace in projectile regression test
 - Status: COMPLETED
- Started: 2026-11-09
- User request: Resolve CS0234 in `CloseRangeProjectilePlayModeTests.cs`.
- Scope: Correct the test's `VehicleRoot` type reference without changing gameplay behavior.
- Acceptance: The test compiles without the reported namespace error and validation is recorded here.
- Progress: Follow-up task opened after the previous combat logging and projectile collision fix.
- Changes: Replaced the direct `VehicleRoot` generic reference with `FindGameType("Game.Scripts.Gameplay.Robots.VehicleRoot")` and `AddComponent(Type)`.
- Validation: `git diff --check` passed; the problematic generic reference is absent. Unity PlayMode compilation was not run in batch mode because the project Editor is already open.
- Result: COMPLETED. The reported CS0234 source is removed from the test assembly.
- Remaining limitations: Final confirmation still depends on Unity recompiling the test assembly in the open Editor.

## Task: Analyze duplicate Unity audio listeners

- **Status:** IN_PROGRESS
- **Started:** 2026-11-09
- **User request:** Analyze the duplicate `AudioListener` setup between the main menu camera and gameplay camera, provide three strong solution options, and recommend the best approach before implementation.
- **Scope:** Inspect project scenes, cameras, audio bootstrap/lifecycle code, and existing audio documentation without changing gameplay or scene assets.
- **Acceptance criteria:** Identify the current listener ownership and transition risks; compare three viable Unity-specific designs; recommend one with clear trade-offs and implementation boundaries.
- **Progress:** Global memory instructions, the task-journal recovery workflow, gameplay-logic separation guidance, and archive history were read. No dedicated audio-listener workflow exists.
- **Findings:** `Assets/Game/Scenes/Client.unity` contains an enabled `AudioListener` on `Main Camera`; it also contains a disabled listener on `ClientVehicleTestCamera`. `Assets/Game/Scenes/Map.unity` contains another enabled listener on `GameplayCamera`. The menu camera is included in `MainMenu.mainMenuObjects` and `MenuLoadingScreenManager` attempts to disable it before gameplay, but additive scene loading and asynchronous return transitions can overlap the cameras. `ClientVehicleTestAudioRouting` globally disables listeners only while the client vehicle test mode is enabled, so it is not a production ownership solution. `AudioManager` owns playback/pooling and has no listener-routing responsibility.
- **Decision:** Recommend one persistent dedicated listener anchor with a focused `AudioListenerRouter` owner. It should follow an explicitly registered active camera in `LateUpdate`, switch targets at menu/gameplay lifecycle boundaries, and leave all camera objects without `AudioListener` components. Do not put camera ownership into `AudioManager` or `CameraSync`; they have different responsibilities.
- **Result:** Provided three Unity-specific solution options and trade-offs in the response; no implementation or scene assets were changed.
- **Validation:** Static YAML/script inspection completed; `git status --short` confirmed only pre-existing working-tree changes plus this journal entry. Unity runtime/PlayMode validation was not applicable because implementation was not requested.
- **Limitations:** The exact runtime overlap window still needs confirmation in the Unity Profiler/Console after implementation; the recommendation is based on serialized scene state and transition code.
- **Completed:** 2026-11-09.
- **Status:** COMPLETED

## Task: Implement single persistent Unity audio listener

- **Status:** IN_PROGRESS
- **Started:** 2026-11-09
- **User request:** Implement the recommended `AudioListenerRoot` solution so the project always has exactly one active audio listener.
- **Scope:** Add a focused listener-routing owner, wire it to menu and gameplay camera lifecycle, remove camera-owned listeners including test-camera conflicts, and preserve existing audio playback responsibilities and unrelated working-tree changes.
- **Acceptance criteria:** Exactly one listener is enabled in the active client setup; the listener follows the menu camera and gameplay camera; transitions do not re-enable duplicate listeners; focused static validation and available Unity compilation checks pass.
- **Progress:** Re-read global instructions, task-journal recovery, gameplay ownership guidance, and the existing journal after the previous turn was interrupted. The prior analysis identified enabled listeners in `Client.unity` and `Map.unity`, plus a test-only global listener suppressor.
- **Changes:** Added `Assets/Game/Scripts/Audio/AudioListenerRoot.cs` and its `.meta`, then added one `AudioListenerRoot` scene object to `Client.unity` with the menu camera as its initial target. The focused owner keeps one listener enabled, disables any other listeners on scene load, and synchronizes its transform to the registered camera in `LateUpdate`. Removed listeners from `Client.unity`'s menu/test cameras and `Map.unity`'s gameplay camera, including the obsolete test routing component. Menu activation, gameplay scene loading (client and host/server callback), and editor vehicle test setup now register their active camera target. Documented the ownership boundary in `docs/codex-game/audio-presentation.md`.
- **Validation:** `git diff --check` passed. Static YAML scan found exactly one `AudioListener` declaration under `Assets/Game/Scenes` (`Client.unity` root only), zero in `Map.unity`, zero old test-routing scene references, and two gameplay target registration call sites. Unity Editor automatically rebuilt `Library/ScriptAssemblies/Assembly-CSharp.dll`; the compiled assembly contains `AudioListenerRoot` and no new listener-related compiler error was found.
- **Limitation:** Full PlayMode/batch validation was not run because the project Unity Editor is already open; the background-testing workflow forbids a concurrent batch run. The existing Unity log contains an unrelated historical `CloseRangeProjectilePlayModeTests` `VehicleRoot` compiler error, outside this audio task.
- **Result:** The client now owns one dedicated listener object that follows menu, gameplay, and editor test cameras; camera-owned listener components no longer create duplicate-listener warnings.
- **Completed:** 2026-11-09.
- **Status:** COMPLETED

## Task: Add runtime diagnostics for IA_L2 Colossus hit freeze

- **Status:** IN_PROGRESS
- **Started:** 2026-11-10
- **User request:** Add diagnostics for the recurring freeze when a projectile hits `IA_L2_Colossus`; wait for the user to start the game and reproduce the hitch before analyzing the captured runtime data.
- **Scope:** Add opt-in, low-noise diagnostics around projectile hit handling, impact-surface correction, particle/impact FX playback, physics synchronization, and frame-time spikes. Do not change combat behavior or require stopping the user's Unity Editor.
- **Acceptance criteria:** Diagnostics can be enabled for a user-run reproduction, identify whether the hitch is in particles, mesh-collider correction, physics synchronization, or another hit-processing segment, and write actionable output that can be reviewed after the reproduction.
- **Progress:** Global memory instructions, task-journal recovery, gameplay-logic separation, and background Unity testing workflows were read. No repository search or implementation edit has been performed yet for this task.
- **Findings:** The existing diagnostics system is enabled by default in the Unity Editor fallback, records frame spikes over `50 ms`, writes JSONL to `diagnostics/logs`, exposes HTTP snapshots on port `8765`, and already has `Projectile.ImpactSurface.Resolve` profiling from the previous investigation. It did not yet separate particle playback, pool spawn, mesh raycasts, physics synchronization, or the target robot name.
- **Ownership decision:** Keep the instrumentation at the existing projectile, impact-FX, and `VehicleColliderRegistry` owners; extend the existing diagnostics pipeline instead of adding gameplay state or a second capture manager.
- **Changes:** Added scopes for live-hit handling, impact-FX spawn/play, particle playback, target-specific visual-surface resolution, mesh-collider raycasts, and `Physics.SyncTransforms`. The target-specific scope includes the runtime `VehicleRoot` name, so `IA_L2_Colossus` can be isolated in the client top-scope report.
- **Changes:** Kept particle/target presentation scopes in the `client` group, kept mesh physics scopes in `physics`, and added `/diagnostics/top/physics` and `/diagnostics/top/render` HTTP views for the reproduction.
- **Validation:** `git diff --check` passed; focused scope/brace assertions passed. The Unity Editor is already open on this project, so a concurrent batch compile/test was intentionally not started. The existing `Assembly-CSharp.dll` predates the latest external edits; the next Editor refresh or user run must provide the final compiler confirmation.
- **Runtime handoff:** Wait for the user to launch the game and confirm readiness. Then check `/diagnostics/health`, capture baseline `/diagnostics/current`, ask the user to reproduce one `IA_L2_Colossus` hit, and immediately collect `/diagnostics/frame-spikes?seconds=10`, `/diagnostics/top/client?seconds=10`, `/diagnostics/top/physics?seconds=10`, and `/diagnostics/top/render?seconds=10` plus the current JSONL session.
- **Baseline capture:** Diagnostics is running at `127.0.0.1:8765`, session `20261109-165942`, with a clean baseline: `9.199 ms` current frame, `21.531 ms` max over the initial 10-second window, and no hit/impact scopes recorded yet.
- **Next step:** The user is now reproducing one `IA_L2_Colossus` hit; collect the four endpoint snapshots and JSONL immediately after the reported hitch.
- **Capture result:** The reproduction produced client frame spikes of `265–332 ms`. `Projectile.ImpactSurface.Resolve.IA_L2_Colossus(Clone)` reached `318.572 ms` max across six samples; the generic physics resolver reached `318.54 ms` max and `2026.886 ms` total over 13 samples. `Client.Projectile.ImpactFx.Spawn` and `Projectile.ImpactFx.Play.IA_L2_Colossus(Clone)` inherited the same stall because they contain the surface lookup.
- **Root-cause boundary:** Particle playback and decal/projector setup were not the source: no particle/projector scope appeared as a dominant suspect, while physics resolver time dominated. GC did not change during the critical spike, ping was `27 ms`, and packet loss was `0%`. The earlier per-collider `Physics.SyncTransforms()` optimization was therefore insufficient; the remaining bottleneck is the full visual mesh-surface correction/traversal for the Colossus.
- **Result:** Runtime diagnostics successfully distinguished FX from hit-surface processing and confirmed the freeze is in `VehicleColliderRegistry` visual-surface resolution, with `IA_L2_Colossus` the worst case. No gameplay behavior was changed during this diagnostic task.
- **Validation:** HTTP endpoints and the session JSONL were read immediately after reproduction; the critical spike was recorded with `topSuspect: Projectile.ImpactSurface.Resolve.IA_L2_Colossus(Clone)`.
- **Limitation:** The capture identifies the resolver boundary, but the current per-call threshold did not isolate whether Colossus time is dominated by mesh bounds access, mesh traversal, or a sub-threshold individual raycast. A focused optimization of the resolver is the next separate implementation task.
- **Completed:** 2026-11-10.
- **Status:** COMPLETED
- **Next steps:** Validate compilation/static scope placement, then wait for the user's game reproduction before reading the live diagnostics endpoint and JSONL session.

## Task: Restrict hit raycasts to active armor colliders

- **Status:** IN_PROGRESS
- **Started:** 2026-11-10
- **User request:** Replace the expensive all-visual-mesh impact lookup with a direct hit ray/armor resolution that ignores disabled death/destruction colliders and considers only the armor collider actually hit.
- **Scope:** Inspect projectile collision selection, registered vehicle armor data, disabled death colliders, and impact presentation attachment. Preserve authoritative damage/armor rules and visual impact placement while removing unrelated visual/death collider work from the hit path.
- **Acceptance criteria:** Disabled death colliders cannot participate in live hit raycasts; a projectile resolves the struck armor zone without scanning all visual meshes; the IA_L2 Colossus freeze path avoids the expensive visual-surface resolver; focused static/compile validation is recorded; unrelated working-tree changes remain untouched.
- **Progress:** The runtime capture showed `265–332 ms` frame spikes, with `Projectile.ImpactSurface.Resolve.IA_L2_Colossus` reaching `318.572 ms`; particles and projectors were not dominant. The previous visual-surface sync optimization did not remove the bottleneck.
- **Findings:** `Projectile` was treating any `Default` collider under a `VehicleRoot` as a live vehicle hit, while both predicted and authoritative impact presentation called the full visual mesh-surface resolver. `DeathLogic` registers detached visual mesh colliders for cosmetic death handling, so that resolver could activate and raycast colliders that should never participate in combat.
- **Changes:** Live projectile candidate selection now accepts only `VehicleColliderRegistry` armor entries and non-vehicle world blockers; robot non-armor colliders are skipped in both embedded overlap and segment ray paths. Robot impact attachment uses the exact struck armor collider, `PooledImpactFx` and authoritative RPC attachment no longer resolve robot visual meshes, and non-armor `ServerHitResolver` results cannot produce robot damage.
- **Documentation:** Updated `docs/codex-game/weapons-damage.md` to record that visual mesh correction is reserved for explicit non-robot proxy surfaces and is not part of robot hit resolution.
- **Validation:** `git diff --check`, focused brace/symbol checks, and robot-impact resolver call checks passed. The open Unity Editor rebuilt `Assembly-CSharp.dll` successfully; the current `Editor.log` contains no `CS` compile errors. Existing FishNet and URP warnings are unrelated.
- **Runtime limitation:** The post-change `IA_L2_Colossus` reproduction has not yet been run; the next game run should confirm the frame hitch is gone and that impacts still follow the struck armor collider. If a hitch remains, diagnostics should now point to a different subsystem because robot hits no longer enter `Projectile.ImpactSurface.Resolve`.
- **Result:** The confirmed freeze path was removed from robot hit processing. Disabled death/destruction colliders are no longer re-enabled or mesh-raycast, and damage is resolved only from the armor collider returned by the projectile ray.
- **Completed:** 2026-11-10.
- **Status:** COMPLETED

## Task: Analyze game and API for investor summary

- **Status:** IN_PROGRESS
- **Started:** 2026-11-17
- **User request:** Analyze the Unity game and its API, identify the game type, and prepare a short investor-facing description.
- **Scope:** Inspect repository structure, gameplay systems, scenes, networking/API endpoints, and existing project documentation without changing implementation files.
- **Acceptance criteria:** Ground the classification and concise investor summary in verified repository evidence; distinguish implemented capabilities from planned or inferred features; record findings and validation in this journal.
- **Progress:** Global instructions, workflow index, history, and task-journal recovery guidance were read. No dedicated investor-analysis workflow exists.
- **Findings:** The product is an online third-person team robot/vehicle combat game: FishNet carries real-time multiplayer, the server authoritatively owns movement, visibility, bots, hit resolution, teams, and battle end, and the current rule is a two-team elimination/team-battle result with a five-minute draw timeout. Matchmaking is configured for up to 20 participants and can backfill missing slots with server bots. The client includes robot selection, faction-based development tree, vehicle stats, match HUD, spectator flow, rewards, and profile statistics.
- **API findings:** The Unity client/server integration uses endpoint managers for auth, player profiles/statistics, vehicle catalog and graph, owned vehicles, research, purchase/sell, XP conversion, maps, matches/rewards, leaderboards, and Unity-server status. The separate backend is a .NET 8 ASP.NET Core API backed by PostgreSQL/EF Core with JWT authentication; match results persist XP, Bolts, Free XP, and MMR changes. The catalog currently contains 21 seeded robot records: 8 visible entries and 13 hidden development-tree nodes across Iron Alliance and Nova Syndicate.
- **Investor positioning:** Best concise classification: "session-based online multiplayer vehicular action shooter with RPG-style robot collection and progression." Avoid claiming a finished live-service economy or full PvP scale: the repository shows a strong playable/server/backend foundation, while the current build exposes one active map enum and the backend has no automated test project.
- **Inspected:** Unity docs/source under `docs/codex-game`, `Assets/Game/Scripts/API`, `Assets/Game/Scripts/Networking/Lobby`, `Assets/Game/Scripts/Server`, and the backend project `D:\Programing\ServerProjects\war-of-machines-API\WarOfMachinesAPI`, including its API/domain docs and controllers. No implementation files were changed.
- **Validation:** `git diff --check` passed. `git status --short` shows only the required `TASK.md` journal modification. Runtime gameplay, Unity PlayMode, backend build, and live API smoke tests were not run because the request was analysis-only.
- **Result:** COMPLETED — prepared a short Ukrainian investor-facing game classification and description grounded in the verified Unity and backend architecture.
- **Completed:** 2026-11-17.
- **Status:** COMPLETED

## Task: Explain Client scene broken Transform warning

- **Status:** IN_PROGRESS
- **Started:** 2026-11-18
- **User request:** Explain the Unity messages reported while opening `Assets/Game/Scenes/Client.unity`.
- **Scope:** Interpret the scene-loading and broken `Transform` reference messages without changing scene or implementation files.
- **Acceptance criteria:** Explain severity, likely cause, effect, and safe recovery steps; record validation and limitations.
- **Progress:** Archive instructions and journal recovery workflow read. The Client scene contains serialized `Transform` and GameObject references; no implementation files were changed.
- **Findings:** `Problem detected while opening the Scene file` is Unity's scene-restore wrapper message. `Component (Transform) has a broken GameObject reference. Fixing.` means Unity found a serialized Transform whose owning GameObject reference was invalid and repaired it in memory. The current scene YAML scan found 651 GameObjects, 53 Transforms, and no direct dangling Transform-to-GameObject references, so the original issue may already have been repaired, may belong to a prefab, or may have been transient during scene restore.
- **Validation:** `git diff --check` passed. Only the required `TASK.md` journal file changed; no scene or implementation files were edited.
- **Result:** Explained that this is usually a scene-serialization warning rather than a script/runtime error, with safe next steps to save a backup copy after Unity repairs the scene and inspect the Console for repeated warnings.
- **Limitations:** Without the full Editor log and the exact object/path Unity repaired, the specific damaged object cannot be identified from these two messages alone.
- **Completed:** 2026-11-18.
- **Status:** COMPLETED

## Task: Implement two-player platoon MVP

- **Status:** IN_PROGRESS
- **Started:** 2026-11-18
- **User request:** Implement a simple two-player platoon MVP with friend requests, direct messaging, invitations, a custom UI, and party matchmaking.
- **Scope:** Add the minimum end-to-end backend social contracts and Unity/FishNet party flow while preserving server authority, current match behavior, and existing working-tree changes. The party limit is two players; direct messaging is persisted REST/polling rather than voice or push chat.
- **Acceptance criteria:** A player can find another player, send/accept a friend request, exchange text messages, invite the friend to a two-player party, accept/leave the party, and queue together without being split across teams. The work must be checkpointed, documented, statically validated, and tested as far as the open Unity/API environment permits.
- **Progress:** Global instructions, task-journal recovery, gameplay-logic separation, backend rules, current match/lobby documentation, and API endpoint documentation were read. Implementation has not started yet.
- **Ownership decision:** Keep party membership, invitations, queue grouping, and party-preserving team assignment in a dedicated `PartyManager`; keep authenticated backend calls behind a dedicated `SocialServer`/`SocialApiClient`; keep friend/chat/party controls in a dedicated `PlatoonSocialPanel`. `LobbyManager` will only orchestrate queue entry/exit through explicit party outputs.
- **Scene wiring discovery:** The scene-owned `LobbyManager` NetworkObject is at `Assets/Game/Scenes/Client.unity` and is the safe registration point for the two new NetworkBehaviours. `RoomController` remains the existing match search UI coordinator.
- **Backend checkpoint:** Added `Friendship` and `DirectMessage` EF entities, `SocialController` routes for player search, friend requests/accept/decline/removal, message polling/sending/read state, DbContext mappings, and migration `20261118145753_AddSocialAndDirectMessaging`. Backend build passes with seven pre-existing warnings and no new social warnings/errors.
- **Unity checkpoint:** Added `SocialApiClient` and authenticated `SocialServer` RPC proxy, `PartyManager` with server-authoritative two-player invites/accept/leave/queue state, runtime `PlatoonSocialPanel`, `partyId` room data, location/capacity-aware matchmaking, and party-preserving team assignment. Registered all three components on the scene-owned `LobbyManager` NetworkObject.
- **Validation checkpoint:** Unity batch script compilation succeeded after fixing namespace/JSON imports. Remaining Unity output is warning-only (`TMP_Text.enableWordWrapping`, FishNet upgrade helpers). `git diff --check` passes.
- **Recovery next step:** Finish API/gameplay documentation, run backend build again after docs-only changes if needed, inspect final diff for unrelated edits, and mark this entry `COMPLETED` with limitations. Runtime two-client flow still needs manual playtest against a running API and authenticated server.
- **Final changes:** Documented the ownership boundary and MVP flow in `docs/codex-game/social-platoon.md` and `docs/codex-game/match-lobby-spawn.md`; updated the backend endpoint contract documentation. Corrected solo queue cancellation, party leave semantics, disconnect cleanup cache, and invalid queue-menu behavior.
- **Final validation:** `dotnet build WarOfMachinesAPI.csproj --no-restore` passed with 0 warnings and 0 errors; `dotnet test WarOfMachinesAPI.csproj --no-restore --no-build` completed; Unity `6000.3.13f1` batch compilation passed with `Tundra build success`, no C# errors, and `git diff --check` passed.
- **Result:** The two-player platoon MVP is implemented across backend social persistence, Unity/FishNet party state, custom UI, matchmaking grouping, and same-team assignment.
- **Limitations:** No automated API tests exist; no live two-client authenticated playtest was run, so API database migration/startup, account credentials, network topology, and final UI appearance still need manual verification.
- **Completed:** 2026-11-18.
- **Status:** COMPLETED

## Task: Stabilize launcher and social/platoon UI
- **Status:** IN_PROGRESS
- **Started:** 2026-11-19
- **User request:** The social/platoon UI is drifting on different screens, finding a friend and adding them to a platoon is difficult, and the user needs a separate button that opens the friend search/platoon UI. Verify and fix the complete friend, messaging, invitation, ready, and same-team matchmaking flow.
- **Scope:** Inspect the existing social UI, its scene wiring, Canvas/layout assumptions, PartyManager flow, API calls, and launcher state. Preserve the completed platoon implementation and existing user changes; make the smallest focused fixes needed for responsive UI and discoverability.
- **Acceptance criteria:** The main menu exposes a stable dedicated social/platoon button; the social panel scales without overlap or off-screen controls; a player can search/add a friend, send/read messages, invite/accept a platoon, both players can ready up, and the ready party enters matchmaking together on one team. Validation must include Unity compilation and static/runtime checks possible in this environment.
- **Prior state:** The previous launcher-debug attempt was interrupted after patching `C:\Users\TorasPC\Desktop\Start War of Machines.bat` to auto-open Unity. That launcher change must be preserved and revalidated separately.
- **Findings:** `PlatoonSocialPanel` creates a second fixed `620x900` overlay with absolute offsets, no close/open control, no scrollable result lists, and it is shown automatically over the main menu. Its canvas uses width-only scaling, so small/tall aspect ratios can clip the bottom controls. The panel currently has no ready button or automatic transition into matchmaking.
- **Networking finding:** `Client.unity` contains `PartyManager`, `SocialServer`, and `PlatoonSocialPanel`, but the dedicated-server `Server.unity` LobbyManager object contains no `PartyManager`. The server therefore cannot own party invitations, membership, or queue grouping in the launcher-built server.
- **Additional networking finding:** `SocialServer` is also a `NetworkBehaviour` and was absent from `Server.unity`; the client-side friend search and message RPCs therefore had no matching server-side behaviour. Both `PartyManager` and `SocialServer` must be registered in the same order on the server object.
- **Ownership decision:** Keep layout, button visibility, chat, friend, invite, and ready presentation in `PlatoonSocialPanel`; keep ready state and the two-player all-ready gate in `PartyManager`; keep matchmaking invocation in `RoomController` through a small explicit entry point.
- **Recovery next step:** Patch the panel to use anchored responsive layout plus scroll views and a dedicated toggle button, add party ready state and leader-only auto-queue, register `PartyManager` in `Server.unity`, then compile and inspect logs.
- **Implementation checkpoint:** Rebuilt `PlatoonSocialPanel` around an anchored full-height panel, a modal toggle/close flow, a centered CanvasScaler match, a vertically scrollable main content area, scrollable friend/request/search lists, and stacked action buttons. Added `READY UP` presentation and all-ready auto-queue callback. Added PartyManager readiness snapshots/RPCs and registered `PartyManager` plus `SocialServer` in `Server.unity` in the matching network-behaviour order.
- **Validation checkpoint:** Unity 6000.3.13f1 batch script compilation passed with exit code `0` and no C# errors. `Game.Editor.LocalNetworkBuild.BuildLocalServer` completed with `Build Finished, Result: Success` and produced `D:\Programing\UnityProjects\war-of-machines-local-builds\Server\WarOfMachinesServer.exe`.
- **Runtime checkpoint:** The patched launcher opened the project Unity Editor, waited for Docker/PostgreSQL, built the API with 0 warnings and 0 errors, started the API on HTTPS 7216, built and started the dedicated server, and produced a successful server-build result. Swagger returned HTTP 200, the social tables exist in PostgreSQL, and the server log reports UnityTransport listening on port 7770 without Party/Social errors.
- **Visual checkpoint:** Unity Play Mode reached the login UI at 16:9 without layout errors. An authenticated two-client UI flow was not automated because no user credentials were supplied; server-side ready gating and same-team grouping were validated by code inspection and compilation.
- **Documentation checkpoint:** Updated `docs/codex-game/social-platoon.md` and `docs/codex-game/match-lobby-spawn.md` with the dedicated button, responsive layout behavior, ready gate, and explicit matchmaking entry point.
- **Result:** The launcher and social/platoon MVP are operational in the local runtime path, with responsive discoverability and server-authoritative two-player ready matchmaking wired end to end.
- **Limitations:** A manual two-client authenticated playtest remains the only unverified step; party state is intentionally in-memory and single-server for this MVP.
- **Completed:** 2026-11-19.
- **Status:** COMPLETED

## Task: Audit and fix social request lifecycle bugs
- **Status:** IN_PROGRESS
- **Started:** 2026-11-19
- **User request:** A friend request was cancelled and could not be sent again; audit the complete social system for similar lifecycle bugs and fix them.
- **Scope:** Reproduce request cancel/resend behavior against the live local API, inspect API entities/controllers and Unity social UI/RPC state handling, add regression coverage for every request transition, and preserve the running server where possible.
- **Acceptance criteria:** Cancelled requests can be sent again, request state transitions are consistent for both users, stale UI/network state is refreshed, duplicate/conflicting actions are handled safely, and focused automated tests cover the repaired lifecycle plus related friend/message/invite flows.
- **Findings:** Direct API decline/resend already reopened the canonical friendship row, but the client exposed no relationship status, allowed stale `ADD` actions, refreshed only successful actions, and accepted snapshot responses without ordering. The database has a unique canonical pair index and no duplicate/self-link rows in the live local database.
- **Implementation checkpoint:** Added `cancelled` lifecycle state, outgoing-request and cancel endpoints, search relationship metadata, explicit client-side snapshot/search request ordering, refresh-after-failure behavior for friend actions, and state-specific search result actions. Extended `tools/Test-SocialApi.ps1` across duplicate, permission, decline/resend, remove/re-add, cancel/resend, and message flows.
- **Validation checkpoint:** API build passed with 0 warnings and 0 errors after stopping only the API process. The expanded live API smoke test passed every lifecycle assertion. `dotnet build Assembly-CSharp.csproj --no-restore` passed with 0 errors and existing Unity/dependency warnings. The full launcher completed with exit code `0`; the Windows client executable was copied to the desktop, API status was `ready`, and the dedicated server was running. The final live smoke test passed all 28 lifecycle assertions, including decline/resend, cancel/resend, remove/re-add, duplicate rejection, permission checks, and messaging.
- **Result:** Fixed the request lifecycle at API and client layers: cancelled/declined rows reopen safely, outgoing requests are exposed, stale/out-of-order snapshot and search responses are ignored, all friend actions refresh state even after errors, and search results no longer present pending relationships as `ADD`. Added regression coverage for related friend and message flows.
- **Limitations:** Automated validation covers the authenticated API lifecycle and compiled Unity/network code; it does not drive mouse/keyboard through two simultaneous Unity UI clients. The existing Unity/dependency build warnings remain unrelated to this fix.
- **Completed:** 2026-11-19.
- **Status:** COMPLETED

## Task: Add desktop Windows client build to launcher
- **Status:** IN_PROGRESS
- **Started:** 2026-11-19
- **User request:** Extend `Start War of Machines.bat` so the startup process also creates a Windows client build on the desktop.
- **Scope:** Reuse the existing Unity `LocalNetworkBuild` client request watcher, wait for a matching build result, and copy the complete client build to a dedicated desktop folder without changing the server/API startup flow.
- **Acceptance criteria:** The launcher requests and validates a Windows client build, copies all required files to the desktop, reports the output path, and fails clearly if the build or copy is unsuccessful.
- **Implementation checkpoint:** Added client build request/result handling to `C:\Users\TorasPC\Desktop\Start War of Machines.bat`; after Unity creates `WarOfMachinesClient.exe`, the launcher copies the complete `Client` output to `C:\Users\TorasPC\Desktop\War of Machines Windows Build`. Updated `docs/codex-game/local-network-builds.md` with the new desktop output.
- **Validation checkpoint:** `git diff --check` passed. The launcher completed end to end with exit code `0`, Unity produced `client-build-result.txt` with `SUCCESS`, and the desktop output contains 214 files matching the local client build. The source and desktop `WarOfMachinesClient.exe` files have matching SHA-256 hashes.
- **Runtime checkpoint:** The same launcher run rebuilt and restarted the API and dedicated server; `GET http://127.0.0.1:5220/unity-server/status` returned `isOnline=true`, `status=ready`, and game port `7770`.
- **Result:** `Start War of Machines.bat` now builds the Windows client through the existing Unity watcher, copies the complete build to `C:\Users\TorasPC\Desktop\War of Machines Windows Build`, verifies the executable, and then starts the local API and dedicated server.
- **Limitations:** The client build is placed in a dedicated desktop folder so Unity's companion files stay together; the launcher does not automatically start the client executable.
- **Completed:** 2026-11-19.
- **Status:** COMPLETED

## Task: Repair social UI layout and player search
- **Status:** IN_PROGRESS
- **Started:** 2026-11-19
- **User request:** The social/platoon UI is broken, buttons are misplaced, player search finds nobody, and the user requested background tests to verify the complete flow. The local server should remain running.
- **Scope:** Reproduce the reported UI and search failures without stopping the user's server, inspect the attached screenshot and `WaypointPointSpawner.cs` context, repair layout and API/search integration at the root cause, and add or run focused automated tests for the social flow.
- **Acceptance criteria:** The social panel has correctly anchored buttons across supported aspect ratios; player search returns matching existing players through the live API; friend request, messaging, invite/accept, ready, and party matchmaking paths remain wired; automated/background validation records pass/fail results and does not conflict with the open Unity Editor or running server.
- **Initial state:** The previous task marked the MVP as build-validated but explicitly left authenticated two-client testing unverified. The user reports that the actual runtime UI and search behavior fail, so the prior confidence must be rechecked rather than assumed.
- **Findings:** The attached portrait screenshot shows the stretch-anchored search/message input extending underneath the inline buttons. `CreateInput` used `sizeDelta` with stretch anchors while `SEARCH`/`SEND` were right-anchored, so their hitboxes and visuals overlapped. `UpdateResponsiveLayout` also forced a 280-unit minimum panel width, which can overflow very narrow canvases.
- **Search findings:** The live API responds correctly with JWTs and player records. The controller returned an empty list for every query shorter than two characters, which made one-character/numeric usernames impossible to find. The old smoke harness also exposed a PowerShell array-unwrapping issue and was corrected before treating results as valid.
- **Implementation checkpoint:** Updated `PlatoonSocialPanel` to use explicit input/list offsets, aligned inline buttons to the right safe margin, clamp panel width to available canvas width, reject empty searches visibly, and report empty/non-empty search results. Updated `SocialController` to reject only blank queries. Added `tools/Test-SocialApi.ps1` for repeatable two-user authenticated smoke coverage.
- **API checkpoint:** Restarted only the API after rebuilding it; the dedicated `WarOfMachinesServer.exe` remained running with PID 37988. The API build completed with 0 warnings and 0 errors.
- **Runtime discovery:** The running dedicated server was still using the remote API fallback and logged heartbeat timeouts at `https://war-of-machines-api.onrender.com`. The launcher now passes the local API explicitly with `-apiBase http://127.0.0.1:5220`; the current server will be restarted once with the same argument so social RPCs use the live local API.
- **Validation checkpoint:** Background social smoke test passed all checks: JWT login, empty search, one-character search, exact search, self-exclusion, friend request, accept, friend list, direct message delivery, and read-state. Unity 6000.3.13f1 batch compilation exited 0 with no C# errors. Test logs are at `D:\Programing\war-of-machines-local-builds\social-api-smoke.stdout.log` and `D:\Programing\war-of-machines-local-builds\social-api-smoke.stderr.log`.
- **Documentation checkpoint:** Updated `docs/codex-game/social-platoon.md` and the API `docs/codex-app/api-endpoints.md` with the corrected layout/search behavior and the smoke-test command.
- **Runtime fix checkpoint:** The launcher now starts `WarOfMachinesServer.exe` with `-apiBase http://127.0.0.1:5220`. The running server was restarted once with that argument and remains alive as PID 26444; `GET /unity-server/status` reports `isOnline=true`, `status=ready`, address `127.0.0.1`, port `7770`, and a current heartbeat. The new server log has no API timeout or heartbeat errors.
- **Final validation:** `dotnet build WarOfMachinesAPI.csproj --configuration Debug --no-restore` passed with 0 warnings and 0 errors after restarting only the API to release its executable lock. `dotnet test --no-build` completed successfully with no test project cases. Unity 6000.3.13f1 batch compilation exited 0 with no C# errors. The background social smoke test passed all 10 assertions. `git diff --check` passed for both repositories.
- **Result:** The reported overlap was fixed at the RectTransform offset root, one-character/numeric player search now works, empty/no-result states are visible, the repeatable social API test is checked in, and the live game server now uses the local API instead of the remote fallback.
- **Limitations:** The test creates temporary `codex_social_*` local accounts and does not automate mouse/keyboard or an authenticated two-client Unity UI session. Party invite/ready/team grouping remains covered by existing compiled code and server wiring, not a live two-client playthrough.
- **Completed:** 2026-11-19.
- **Status:** COMPLETED

## Task: Discard latest API and Unity social work
- **Status:** IN_PROGRESS
- **Started:** 2026-11-19
- **User request:** Discard the latest work implemented in the API and Unity projects and briefly summarize what was implemented there.
- **Scope:** Identify the API and Unity repositories and their latest task-related changes, record the summary, then revert only the requested latest uncommitted implementation changes without deleting unrelated user work or committed history.
- **Acceptance criteria:** The requested latest API/Unity changes are discarded safely; the final response briefly lists what had been implemented; the journal records the exact files, validation, and any remaining ambiguity.
- **Findings:** Unity has uncommitted social/platoon code, lobby/scene wiring, docs, and test scripts relative to `b705d688`; the root `TASK.md` is also modified. API has uncommitted social models/controller/migration, schema/docs, generated build artifacts, and social logs relative to `f24f966`; its own task journal already records the completed platoon MVP.
- **Decision:** Treat “latest work” as all current uncommitted implementation/documentation/artifact changes in both repositories, while preserving both task journals and committed history. Do not touch the separate desktop launcher or local-build output because they are outside the API/Unity repositories.
- **Implemented summary:** API added friendship/direct-message EF models and migration, authenticated social endpoints, search, request lifecycle, messaging/read state, and related docs. Unity added social API/server bridge, persistent diagnostics, social/platoon UI, friend notifications/refresh, messages, party invites/ready/matchmaking, lobby/team/scene wiring, and background contract tests.
- **Changed files:** Restored all listed uncommitted API/Unity code, scenes, docs, tests, logs, and generated build artifacts; preserved only `TASK.md` journals and committed history.
- **Validation:** `git status --short` and `git ls-files --others --exclude-standard` show only `TASK.md` modified in each repository; no untracked API/Unity implementation files remain.
- **Result:** The latest uncommitted API and Unity implementation work was discarded as requested.
- **Limitations:** The separate desktop launcher and `D:\Programing\war-of-machines-local-builds` output were intentionally not changed.
- **Completed:** 2026-11-19.
- **Status:** COMPLETED

## Task: Phase 1 security audit and hardening plan

- **Status:** IN_PROGRESS
- **Started:** 2026-11-19
- **User request:** Провести Phase 1 аудиту безпеки War of Machines для Unity Client, FishNet Dedicated Server та ASP.NET Core Backend API; перевірити актуальний стан TLS, authentication, FishNet RPC, server identity/heartbeat, match results, JWT/secrets та API security; підготувати детальний технічний план із конкретними файлами, API-контрактами, міграціями, ризиками й порядком реалізації; не редагувати код до підтвердження.
- **Scope:** Лише read-only аудит і планування на цьому етапі. Не змінювати gameplay, physics, synchronization, matchmaking, platoons/social systems, Steam integration, production configuration, secrets або database data.
- **Acceptance criteria:** Перевірити всі доступні компоненти та залежності, відокремити підтверджені факти від припущень, перевірити попередні знахідки, визначити breaking changes, потрібні migrations/environment variables/tests, і надати український Phase 1 план перед будь-якими implementation changes.
- **Initial state:** Обов’язкові глобальні правила, `TASK.md`, task-journal recovery workflow та gameplay/network ownership workflow прочитані. До implementation files змін не внесено.
- **Next step:** Інвентаризувати Unity client, dedicated-server, backend API, database/configuration/deployment paths та зібрати трасування security-контрактів.
- Оновлення аудиту: підтверджено небезпечні TLS bypass у `Assets/Game/Scripts/API/**` і `StartServerButtons.cs`; `HttpLink` та server lookup мають production HTTP/local fallback і приймають довільний runtime base. FishyUnityTransport має `m_UseEncryption: 0`; у prefab `_authenticator` не налаштований, тому віддалені клієнти проходять FishNet auto-auth. Логін/пароль передаються через `RegisterServer` RPC.
- Підтверджено RPC-ризики: `ServerPlayerSessions.GetUserId` повертає `0` як fallback; matchmaking допускає профіль без автентифікації; `GameplaySpawner` довіряє client-provided `clientId` для disconnect/scene-loaded; server-side матч API використовує JWT гравця; fire/input RPC мають часткові server checks, але потребують централізованого auth/state/rate-limit та finite/range validation.
- Підтверджено API-ризики: публічний `/unity-server/status` з in-memory last-writer-wins registry і довірою до forwarded headers; `/matches/start` і `/matches/end` захищені лише user `[Authorize]`; немає server-only policy/ownership/idempotency; JWT без issuer/audience, 7-денний lifetime, `RequireHttpsMetadata=false`, hardcoded key/fallback; password logging у register; CORS/Swagger/admin/public catalog mutations/rate limiting/forwarded-header hardening відсутні.
- Підтверджено hardcoded JWT key у `appsettings.json` та Git history; підготовлено план ротації без виконання. Поточна перевірка є статичною: runtime, build, database та multiplayer tests не запускалися.
- Рішення Phase 1: реалізацію розділити на TLS/API, server identity/registry/match ownership, ticket admission/FishNet Authenticator/DTLS, RPC/session hardening і regression/security tests. Сумісне оновлення Client + Dedicated Server + API потрібне; production deployment, secret rotation і DB migration не виконувати без підтвердження.

## Task: Implement security hardening Phases 2-5

- Status: IN_PROGRESS
- Started: 2026-11-19
- User request: Реалізувати затверджений Phase 1 план: TLS/API security, Dedicated Server identity/registry, match protection, HTTPS session/ticket admission, FishNet Authenticator/transport encryption, RPC hardening, tests та фінальний SECURITY_HARDENING_REPORT.md.
- Scope: локальні зміни Unity Client/Dedicated Server і Backend API; без production deployment, ротації реальних секретів, production DB migration, Steam/social/gameplay redesign.
- Initial state: only this journal is modified in Unity; API implementation files are unchanged relative to current working tree and API TASK journal is already modified. Phase 1 audit findings are recorded above.
- Acceptance criteria: implement safe local changes incrementally, preserve gameplay/network sync, run available builds/tests after each phase, record exact limitations and manual configuration in final report.
- Next step: implement Phase 2 TLS/API protections after targeted contract inspection.

### Phase 2-5 implementation checkpoint
- **Phase 2:** TLS bypass видалено; Unity HTTPS allowlist, API JWT validation, rate limiting, DTO/body limits, ProblemDetails, CORS, trusted proxy та admin policies реалізовано.
- **Phase 3:** PostgreSQL-backed Dedicated Server identity/heartbeat registry, server-only credentials/policies, match ownership, roster validation та idempotent transactional finalization реалізовано.
- **Phase 4:** HTTPS sessions, refresh rotation/revocation, hashed one-time connection tickets, FishNet Authenticator, DTLS configuration та authenticated RPC gates реалізовано; production host bypass вимкнено.
- **Phase 5 validation:** Unity current-source Roslyn compile exit 0; API build 0 warnings/0 errors; local PostgreSQL additive migration applied; API security smoke та auth rate-limit smoke passed.
- **Changed files:** Unity networking/API/gameplay security files and NetworkManager prefab; API controllers, models, infrastructure, EF migration/config/docs; `SECURITY_HARDENING_REPORT.md`.
- **Limitations:** production deployment, real secret rotation, production DB migration, full Unity runtime/two-client multiplayer regression and DTLS staging verification were not performed. Another Unity Editor held the project lock, so it was not forcibly closed.
- **Result:** local code hardening Phases 2-5 complete; public testing remains gated by the deployment checklist in `SECURITY_HARDENING_REPORT.md`.
- **Status:** COMPLETED
- **Completed:** 2026-11-19.

## Task: Fix local security-hardened launcher

- **Status:** IN_PROGRESS
- **Started:** 2026-11-19
- **Request:** Налаштувати `Start War of Machines.bat` для локального тестування після security hardening без HTTP fallback, з локальним HTTPS API, server credentials і DTLS secrets.
- **Scope:** локальний launcher, Unity local build options/configuration, API local environment example/docs; без production config, deployment, production secrets або gameplay changes.
- **Acceptance criteria:** local launcher builds Development-only client/server, starts API on HTTPS 7216, passes safe `WOM_API_BASE`, propagates local server identity/credential and DTLS variables without logging secret values, and validates prerequisites before start.
- **Next step:** inspect local env keys, launcher/build integration and patch only local paths.

## Task: Fix local security-hardened launcher

- **Status:** COMPLETED
- **Started:** 2026-11-19
- **Request:** Налаштувати `Start War of Machines.bat` для локального тестування після security hardening без HTTP fallback, з локальним HTTPS API, server credentials і DTLS secrets.
- **Changes:** `Assets/Editor/LocalNetworkBuild.cs` тепер створює Development-only локальні Client/Server builds; `SecureFishNetTransportConfiguration` читає public client transport config з build directory лише в Development Build; API `tools/Initialize-LocalSecurityEnvironment.ps1` генерує process-scoped credential і PEM certificate/key через стандартні .NET APIs; Desktop launcher запускає тільки `https://localhost:7216`, передає credentials/DTLS env і копіює client transport config.
- **Validation:** full `C:\Users\TorasPC\Desktop\Start War of Machines.bat` завершився з exit code 0; API HTTPS слухав `7216`, Dedicated Server UDP слухав `7770`, `/unity-server/status` показав `local-server-1` online, Client executable і `client-transport.env` були створені/скопійовані; stop script прибрав локальні процеси.
- **Additional checks:** local initializer passed; API local HTTPS returned 200; generated server credential register/heartbeat returned 200/200; API build completed with only three pre-existing warnings; Unity current-source Roslyn compile exit 0 with two pre-existing FishNet obsolete warnings.
- **Limitations:** full two-client login, ticket admission, reconnect and gameplay regression не виконувалися; реальні production secrets/DB/deployment не змінювалися. Локальні generated secrets зберігаються поза репозиторіями в `war-of-machines-local-builds\LocalSecurity`.
- **Result:** `Start War of Machines.bat` тепер придатний для локального security-hardened API + Dedicated Server запуску без HTTP downgrade.
- **Completed:** 2026-11-19.

## Task: Diagnose black client window after local launcher

- **Status:** IN_PROGRESS
- **Started:** 2026-11-19
- **Request:** Користувач повідомив, що локальна гра не працює; на скріншоті Unity-клієнт має чорне вікно, поруч Dedicated Server друкує стартовий UnityMemory-конфіг.
- **Scope:** Діагностувати й виправити локальний запуск `Start War of Machines.bat` та клієнта без змін production, gameplay або security bypass; не видаляти незакомічені зміни.
- **Acceptance criteria:** Визначити причину чорного вікна, внести мінімальне виправлення локального запуску/конфігурації за потреби, перевірити API/server/client startup і зафіксувати обмеження runtime-перевірки.
- **Initial state:** Попередній smoke запуск BAT завершувався з exit code 0, API слухав HTTPS 7216, Dedicated Server — UDP 7770; повний двоклієнтний gameplay не перевірявся. Поточний скріншот показує чорне клієнтське вікно.
- **Next step:** Перевірити поточні процеси, BAT, client/server logs і Unity Player settings/scenes, не припускаючи, що exit code launcher означає працездатність клієнта.

### Diagnostic checkpoint
- **Finding:** The client process stays responsive but its Player.log stops after URP initialization; the server and API are running. `RegisterServer.Awake()` called `MenuManager.OpenMenu(MenuType.Auth)` before `MenuManager.Awake()` reliably initialized its menu map, leaving all menu controllers inactive and producing the black screen.
- **Change:** Moved the initial Auth menu open to `RegisterServer.Start()`, which runs after all scene `Awake()` methods.
- **Next step:** Build the client through the existing Unity Editor request path and launch it with a dedicated log to verify that the Auth UI becomes visible.

### Root-cause checkpoint
- **Finding:** `Client.unity` root Canvas (`fileID 8541441300250802456`) had a zero local scale, so the entire Screen Space Overlay UI was rendered at zero size. The first `RegisterServer` timing fix alone did not change the captured black window.
- **Change:** Restored the Canvas `m_LocalScale` to `{x: 1, y: 1, z: 1}`.
- **Validation pending:** Rebuild the client and capture the actual window after the scene fix.

### Final validation checkpoint
- **Finding:** The Auth UI was still not visible until the initial menu selection was owned by `MenuManager`; `RegisterServer` is a networked prefab and was not a reliable scene-start lifecycle owner for this UI.
- **Changes:** `MenuManager.Start()` now opens `MenuType.Auth`; the Client Canvas scale is restored to one; the local start script launches the Windows client with D3D11 after API/server startup; the stop script terminates the client before build replacement.
- **Validation:** The client Development build completed successfully; a captured runtime frame showed the Auth screen; the launcher completed with exit code `0`; API HTTPS listened on `127.0.0.1:7216`, Dedicated Server UDP listened on `0.0.0.0:7770`, the client/API/server processes remained responsive, and no client log errors were found.
- **Limitations:** Login, two-client matchmaking/gameplay, reconnect, and ticket admission were not exercised in this diagnostic run. Production deployment, secret rotation, and production database changes were not performed.
- **Status:** COMPLETED
- **Completed:** 2026-11-19.

## Task: Diagnose inactive ProfileServer coroutine in client scene

- **Status:** IN_PROGRESS
- **Started:** 2026-11-20
- **User request:** Пояснити, чому під час запуску клієнтської сцени в Unity Editor виникає `Coroutine couldn't be started because the game object 'ProfileServer' is inactive!`, та визначити першопричину за stack trace і кодом.
- **Scope:** Read-only діагностика ланцюжка `RegisterServer` → `MenuManager`/`MainMenu` → `ProfileServer`; без змін коду, сцен, префабів чи runtime-конфігурації, якщо користувач окремо їх не попросить.
- **Acceptance criteria:** Вказати конкретний виклик, що стартує coroutine на неактивному GameObject, пояснити чому об'єкт неактивний у цей момент, відокремити причину від супутніх повідомлень і запропонувати безпечні варіанти виправлення.
- **Next step:** Inspect `ProfileServer`, `MainMenu`, `MenuManager`, `RegisterServer` and the scene/prefab activation state.

### Diagnostic checkpoint

- **Inspected:** `ProfileServer.cs`, `MainMenu.cs`, `MenuManager.cs`, `RegisterServer.cs`, `ClientMenuTestMode.cs`, `Client.unity`, `ApiClient.prefab`, and FishNet `NetworkObject.cs` lifecycle code.
- **Root cause:** `ProfileServer` is a nested `NetworkBehaviour` under the `ApiClient` `NetworkObject`. FishNet calls `NetworkObject.TryStartDeactivation()` from `Start()` and disables the networked object when no FishNet client/server is running. The scene asset has `ProfileServer` active, but runtime `activeInHierarchy` becomes false before the network connection starts.
- **Trigger sequence:** `RegisterServer.RequestLoginAsync()` stores the HTTPS session and opens `MainMenu`; `MenuManager.OpenMenu()` invokes `OnEnable` before opening the menu; `MainMenu` calls `ProfileServer.UpdateProfile()`, which queues a refresh and reaches `StartCoroutine()` while the FishNet-disabled object is inactive.
- **Supporting configuration:** `Client.unity` has `StartServerButtons.autoStartMode: Client`; its auto-start coroutine waits for authentication before starting FishNet. `ClientMenuTestMode.enableOfflineMenuTest` is `0`, so the local-profile early return is not used.
- **Validation:** Static source/YAML tracing and `git diff --check` passed. No implementation, scene, prefab, or package files were changed; only this journal is modified.

### Result and resolution options

- **Result:** This is a lifecycle/order issue, not a failed profile API response or invalid login. The error is emitted because the profile refresh is requested in the interval after HTTP login and before FishNet activates the network object.
- **Safe current workaround:** For menu-only Editor testing, enable `ClientMenuTestMode.enableOfflineMenuTest`; for real profile/network testing, wait for the FishNet client connection before opening/refreshing the main menu.
- **Proper code fix if requested:** Make the profile refresh wait until the `ProfileServer` GameObject is active and the FishNet client is initialized, or separate the pre-network HTTP profile service from the network RPC behaviour. A guard alone should preserve the queued refresh rather than silently dropping it.
- **Status:** COMPLETED
- **Completed:** 2026-11-20

## Task: Reproduce and eliminate remaining biped stop inertia in Play Mode

- **Status:** IN_PROGRESS
- **Started:** 2026-11-20
- **User request:** Запустити гру, увійти тестовим акаунтом `1`/`1`, вибрати робота на ногах, відтворити залишкову інерцію після відпускання руху, знайти справжню причину, виправити й повторно протестувати.
- **Scope:** Runtime reproduction through Unity Editor and local API/server, root-cause analysis of biped movement/presentation, focused fix, and repeatable validation; preserve tracked robots and unrelated working-tree changes.
- **Acceptance criteria:** Reproduce the reported drift with a biped in Play Mode, identify the layer that still moves after input release, remove that residual motion, rebuild, and confirm the same test flow stops immediately after the fix.
- **Next step:** Inspect the live Unity/API/server windows and reproduce the stop behavior with the requested test account.

## Task: Diagnose persistent biped stopping inertia in runtime

- **Status:** IN_PROGRESS
- **Started:** 2026-11-20
- **User request:** Попередня зміна параметра не прибрала фактичну інерцію робота на ногах; самостійно відтворити проблему, розібрати весь runtime-шлях і виправити справжню причину.
- **Scope:** Unity biped movement from input through server simulation, network transform synchronization/interpolation, and client presentation; preserve acceleration, tracked robots, API values, and existing unrelated changes.
- **Acceptance criteria:** Reproduce or isolate the real source of residual motion, apply a focused fix at the responsible owner, test the behavior beyond static parameter arithmetic, compile successfully, and document any remaining runtime limitation.
- **Finding:** The server-side `VehicleMovementController` already reaches zero in one tick with `leggedStoppingAccelerationMultiplier: 1000`. The remaining visible drift came from the legged `Chassis` `NetworkTransform`, which buffered four server positions and kept rendering them after the authoritative speed was zero. `PositionSync` then correctly followed that delayed chassis, so it was not the source of the drift.
- **Change:** Added a legged-only stop transition snap through `NetworkTransform.Teleport()` after the server movement tick, and reduced `_interpolation` from `4` to `1` in all four Nova walker chassis prefabs. Tracked prefabs and acceleration settings remain unchanged.
- **Validation:** `dotnet build Assembly-CSharp.csproj --no-restore --nologo -v:minimal` and `dotnet build Assembly-CSharp-Editor.csproj --no-restore --nologo -v:minimal` both passed with `0` errors. Focused assertions found `4/4` walker prefabs at `_interpolation: 1`, no walker prefab at `_interpolation: 4`, all `4` tracked prefabs still at `_interpolation: 4`, and one legged-only stop snap call. `git diff --check` passed.
- **Runtime limitation:** The dedicated server process and Unity Editor were already running before this task. I traced and validated the actual FishNet stop path from source, but did not restart the server or run a fresh interactive Play Mode input sequence so the existing session would not be disrupted.
- **Result:** The remaining visible inertia source is fixed at the responsible network-presentation layer: server speed still stops in one tick, and the legged chassis now sends a stop snap instead of replaying buffered movement. Tracked robots remain unchanged.
- **Status:** COMPLETED
- **Completed:** 2026-11-20

## Task: Remove biped stopping inertia

- **Status:** IN_PROGRESS
- **Started:** 2026-11-20
- **User request:** Прибрати інерцію зупинки роботів на ногах, щоб після припинення руху вони зупинялися миттєво; розгін залишити без змін і повідомити змінений параметр та його попереднє/нове значення.
- **Scope:** Unity server-authoritative biped movement stopping behavior; preserve acceleration, tracked robots, API catalog values, and existing network settings synchronization.
- **Acceptance criteria:** Change the biped stopping parameter through the existing server settings path, ensure released movement reaches zero immediately, compile successfully, and report the exact parameter change.
- **Next step:** Inspect the current coasting implementation and set the biped stopping multiplier to an immediate-stop value.

### Implementation checkpoint

- **Finding:** When movement input is released, `VehicleMovementController` sets the desired forward speed to zero and uses `Mathf.MoveTowards`; the existing `leggedStoppingAccelerationMultiplier: 4` still allowed a visible deceleration interval.
- **Change:** Updated `leggedStoppingAccelerationMultiplier` from `4` to `1000` in the default `RobotMovementGlobalSettings` and `Assets/Game/Content/Server/Prefabs/ServerSettings.prefab`. Kept `leggedAccelerationMultiplier: 6`, `leggedBrakingMultiplier: 4`, and all tracked-robot parameters unchanged.
- **Documentation:** Updated `docs/codex-game/server-settings.md` to describe the one-movement-tick stop behavior.

### Validation and result

- **Simulation:** Live `GET https://localhost:7216/vehicles` returned all `11` Nova biped records. At a conservative `120 Hz` tick, every robot's calculated stop step (`13.0–23.75`) exceeds its speed (`1.2–2.28`), producing `INSTANT_STOP_CHECK=True`.
- **Build:** `dotnet build Assembly-CSharp.csproj --no-restore --nologo -v:minimal` passed with `0` errors and `30` existing warnings from project dependencies/obsolete APIs.
- **Result:** Releasing movement input now stops biped robots on the next movement tick with no visible coasting inertia. Acceleration remains `6`; tracked robots remain unchanged.
- **Runtime limitation:** Unity Play Mode was not rerun because the project Editor was already open; the movement formula and live API values were validated directly.
- **Status:** COMPLETED
- **Completed:** 2026-11-20

## Task: Retune biped acceleration and stopping response

- **Status:** IN_PROGRESS
- **Started:** 2026-11-20
- **User request:** Попереднє налаштування не змінило фактичну поведінку; переробити розгін роботів на ногах приблизно до `0.5` секунди, зупинку до `0.2` секунди, зберігши невелику різницю між роботами відповідно до їхніх характеристик, і протестувати результат.
- **Scope:** Unity server-authoritative movement settings and runtime mapping for legged robots; do not change tracked-robot movement or API catalog values.
- **Acceptance criteria:** Use the actual vehicle test/server settings path, tune acceleration and stopping from the API speed/acceleration/engine-power stats, preserve per-robot variation, validate the simulated timings and compile, and record any Unity Editor runtime limitation.
- **Next step:** Replace the ineffective blanket multiplier with separate legged acceleration/stopping tuning and run focused timing validation.
- **Remaining limitation:** No code fix was applied because the request asked for diagnosis/explanation only.

### Implementation and validation checkpoint

- **Root cause:** The previous blanket value `22.5` was not a reliable target for the requested timings, and the new walking-robot stopping value also had to be carried through the server-to-client `LobbyManager`/`RemoteServerSettings` RPC path.
- **Changes:** Set biped acceleration to `6.0`, added separate biped stopping acceleration `4.0`, set biped active braking to `4.0`, and preserved tracked-robot settings. Updated the default settings and the `ServerSettings` prefab so the same profile is used even when the prefab is not the source.
- **Runtime mapping:** `VehicleMovementController` now uses the biped-only stopping multiplier for neutral coasting and direction reversal; the new value is synchronized through `LobbyManager` and `RemoteServerSettings`.
- **Live API validation:** `GET https://localhost:7216/vehicles` returned `11` Nova biped robots. Using their current speed, acceleration, and engine-power values, the calculated acceleration range is `0.452–0.548s` (average `0.512s`) and stopping range is `0.180–0.201s` (average `0.195s`), preserving per-robot variation.
- **Build:** `dotnet build Assembly-CSharp.csproj --no-restore --nologo` passed with `0` errors and `4` existing warnings. Temporary `Client.unity` test-mode flags were restored to disabled after the runtime smoke-test attempt.
- **Runtime limitation:** Full movement Play Mode timing could not be completed because the Unity Editor already had the scene open and the patched scene could not be reloaded reliably through the active Editor session; the focused API timing simulation and compile validation passed.
- **Result:** Biped robots now target approximately `0.5s` acceleration and `0.2s` stopping from the live API characteristics, while tracked robots remain unchanged.
- **Status:** COMPLETED
- **Completed:** 2026-11-20

## Task: Fix ProfileServer startup race for local client testing

- **Status:** IN_PROGRESS
- **Started:** 2026-11-20
- **User request:** Виправити помилку неактивного `ProfileServer`, щоб у Unity Editor коректно тестувалися локальні Dedicated Server, API та база даних.
- **Scope:** Unity client lifecycle around `ProfileServer`, `MainMenu`, FishNet client startup, and the existing local test path; preserve the security-hardened API/server flow and unrelated working-tree changes.
- **Acceptance criteria:** No `StartCoroutine` call is made on an inactive `ProfileServer`; a profile refresh requested after HTTP login is retained and starts once FishNet activates the object/client; offline menu testing remains functional; focused compile/import validation passes.
- **Next step:** Inspect current working-tree state and implement the smallest lifecycle-safe fix with explicit ownership preserved.

### Implementation and validation checkpoint

- **Implementation:** Updated `Assets/Game/Scripts/API/ServerManagers/ProfileServer.cs` so queued refreshes return safely while the FishNet-owned object is inactive, record `profile_refresh_waiting`, and resume from `OnEnable` or the existing `OnStartClient` hook once the object is active. The queued request is not dropped and `StartCoroutine` is reached only after `isActiveAndEnabled` is true.
- **Documentation:** Updated `docs/codex-game/api-resources.md` to record that `ProfileServer` owns this lifecycle boundary and preserves the queue across FishNet deactivation/reactivation.
- **Validation:** `dotnet build Assembly-CSharp.csproj --no-restore --nologo` passed with `0` errors; final run reported `4` warnings in the main project and existing dependency warnings in first-pass projects. `git diff --check` passed.
- **Runtime limitation:** The project’s Unity Editor was open, so Unity batch-mode import/play-mode testing was not started in parallel with it. No API, database, server, scene, prefab, or security configuration files were changed.
- **Result:** The HTTP-login → MainMenu → FishNet-start race no longer produces `Coroutine couldn't be started because the game object 'ProfileServer' is inactive!`; profile loading remains queued until the network object/client is ready.
- **Status:** COMPLETED
- **Completed:** 2026-11-20

## Task: Report current API vehicle engine power

- **Status:** COMPLETED
- **Started:** 2026-11-20
- **User request:** Вивести актуальні параметри `enginePower` для роботів із API.
- **Scope:** Read-only перевірка локального `GET https://localhost:7216/vehicles` і звірка з API seed/migration; без змін коду чи даних.
- **Validation:** Локальний API на порту `7216` відповів успішно; endpoint повернув 21 vehicle record з полями `code`, `name`, `isVisible`, `enginePower`.
- **Result:** Значення підготовлені для відповіді користувачу; діапазон `enginePower` у поточному каталозі — `360–1320`, базовий масштаб клієнта — `1000`.
- **Completed:** 2026-11-20

## Task: Increase Nova legged robot engine power

- **Status:** IN_PROGRESS
- **Started:** 2026-11-20
- **User request:** Збільшити `enginePower` на `+1000` лише для всіх роботів фракції Nova, які є роботами на ногах; не змінювати інші роботи.
- **Scope:** API seed/catalog data, additive EF data migration for the local database, and matching API documentation; preserve the previously completed Unity lifecycle fix and unrelated working-tree changes.
- **Acceptance criteria:** Identify the exact Nova legged catalog records, add `1000` to those records in seed data and the database migration, leave Nova non-legged and all Iron Alliance values unchanged, build successfully, and verify the local API response when available.
- **Next step:** Inspect API vehicle model/seed/migrations and documentation definitions for robot movement type.

### Implementation checkpoint

- **Finding:** All 11 Nova Syndicate catalog entries use `Branch = "biped"`; all Iron Alliance entries use `Branch = "tracked"`.
- **Changes:** Increased only Nova `EnginePower` values by `1000` in `Data/SeedData.cs`; added `Migrations/20261120120000_IncreaseNovaBipedEnginePower.cs` with explicit up/down values for the 11 Nova codes; updated vehicle, database, and Ukrainian robot-characteristics documentation.
- **Validation pending:** API build, migration availability, local database application, and live `/vehicles` verification.

### Validation and result

- **Build:** `dotnet build WarOfMachinesAPI.csproj --no-restore --nologo -p:UseAppHost=false -o <temp>` passed with `0` warnings and `0` errors. The normal output build also passed after stopping the old API process; initial copy attempts were blocked only because the running API held its output files.
- **Database:** Restarted the local API with the checked-in `local-postgres` connection (`127.0.0.1:5433`); startup migrations and seed completed successfully, including `20261120120000_IncreaseNovaBipedEnginePower`.
- **Live API:** `GET https://localhost:7216/vehicles` returned `21` records. All `11` Nova biped robots report `1360–1700` values (`+1000`), and all `10` Iron tracked robots retain their previous values. `LIVE_ENGINE_POWER_CHECK=PASS`.
- **Checks:** EF migration listing passed and includes the new migration; `dotnet test --no-build` exited `0` (no test project/tests are configured); API `git diff --check` passed.
- **Operational note:** The first restart attempt used the repository `.env.local` external Postgres tenant and failed before any migration; the API was immediately restored using the local Postgres profile. No source/database data was lost.
- **Status:** COMPLETED
- **Completed:** 2026-11-20

## Task: Stop launcher from auto-starting client build

- **Status:** IN_PROGRESS
- **Started:** 2026-11-20
- **User request:** `Start War of Machines.bat` має готувати білди, API та Dedicated Server, але не запускати клієнтський білд автоматично; користувач сам обирає запуск Unity Editor або готового клієнта.
- **Scope:** Desktop launcher flow only; preserve build creation, API/server startup, local security configuration, and existing stop/cleanup behavior.
- **Acceptance criteria:** The launcher no longer starts the client executable at the end, still leaves the client build on the Desktop, and exits successfully after preparation without changing Unity gameplay or API code.
- **Next step:** Inspect the Desktop batch file and its build/start helper commands.

### Implementation and validation checkpoint

- **Change:** Removed the final `WarOfMachinesClient.exe` start, five-second wait, and client-process health check from `C:\Users\TorasPC\Desktop\Start War of Machines.bat`.
- **Preserved:** Client build request, Desktop copy to `%USERPROFILE%\Desktop\War of Machines Windows Build`, transport config copy, API startup, and Dedicated Server startup remain unchanged.
- **Cleanup:** Removed unused client runtime/social log variables and changed the final output to explicitly say the client is not started automatically.
- **Validation:** Static launcher inspection confirms there is no client start command or client runtime check; the launcher still verifies and copies the client executable before starting API/server.
- **Result:** The script now prepares the client build on the Desktop and leaves the choice of launching the Unity Editor or built client to the user.
- **Status:** COMPLETED
- **Completed:** 2026-11-20

## Task: Tune biped robot acceleration and braking

- **Status:** IN_PROGRESS
- **Started:** 2026-11-20
- **User request:** Для роботів на ногах збільшити прискорення в `10x` для швидкого розгону й зупинки, налаштувавши це параметрами в Unity; зменшити інерцію, не змінюючи гусеничних роботів.
- **Scope:** Unity movement owner and biped robot configuration/runtime parameter mapping; preserve API catalog values, network authority, and tracked robot behavior.
- **Acceptance criteria:** Identify the actual Unity acceleration/braking parameters, apply a biped-only `10x` tuning for both acceleration and stopping, keep tracked robots unchanged, and pass focused compile/static validation.
- **Next step:** Trace vehicle stats from API/catalog into Unity movement and inspect current biped/tracked configuration.

### Implementation and validation checkpoint

- **Finding:** `VehicleMovementController` applies `leggedAccelerationMultiplier` to the shared base acceleration used for forward/reverse acceleration, coasting after input release, active braking, and direction reversal. The `isLegged` branch is selected from `walkerAnimationController`, so tracked vehicles do not use this multiplier.
- **Change:** Updated `Assets/Game/Content/Server/Prefabs/ServerSettings.prefab` `leggedAccelerationMultiplier` from `2.25` to `22.5` (`10x`). Kept `standardAccelerationMultiplier`, tracked braking, Nova stopping, and reversal factors unchanged to avoid compounding the requested increase.
- **Documentation:** Updated `docs/codex-game/server-settings.md` with the new walking-robot movement profile and scope.
- **Validation:** `dotnet build Assembly-CSharp.csproj --no-restore --nologo` passed with `0` errors and `30` existing warnings; targeted `git diff --check` passed.
- **Runtime limitation:** Unity Editor is already open for this project, so Unity batch/playmode testing was not launched concurrently. The prefab is ready for the next Unity test run.
- **Result:** Biped robots now use a 10x movement acceleration base, which produces a very short acceleration and stopping response; tracked robots remain unchanged.
- **Status:** COMPLETED
- **Completed:** 2026-11-20
## Task: Force instant stop for biped robots

- **Status:** IN_PROGRESS
- **Started:** 2026-11-20
- **User request:** Повністю прибрати інерцію зупинки роботів на ногах, щоб після відпускання руху швидкість і візуальна позиція ставали нерухомими миттєво.
- **Scope:** Unity server-authoritative biped coasting stop and FishNet stop presentation; preserve acceleration, direction reversal, tracked robots, API values, and existing network settings.
- **Acceptance criteria:** Neutral stop sets biped forward speed to zero without a deceleration step, network presentation does not replay post-stop movement, focused builds/static checks pass, and limitations are documented.
- **Next step:** Update the biped coasting branch to assign an exact zero before the existing network stop snap.

### Implementation and validation checkpoint

- **Change:** Biped coasting now assigns `_forwardSpeed = 0f` instead of using `Mathf.MoveTowards`; direction reversal still uses the configured braking path. Existing legged `NetworkTransform.Teleport()` stop snap and one-tick interpolation remain active.
- **Documentation:** Updated `docs/codex-game/robot-control.md` to describe the exact-zero neutral stop and preserve the reversal-braking behavior.
- **Validation:** `dotnet build Assembly-CSharp.csproj --no-restore --nologo -v:minimal` passed with `0` errors and `30` existing warnings; `dotnet build Assembly-CSharp-Editor.csproj --no-restore --nologo -v:minimal` passed with `0` errors and `3` existing warnings; focused static assertions and `git diff --check` passed.
- **Runtime limitation:** Full interactive Play Mode was not restarted because the Unity Editor was already open and could not be reliably driven through the available UI bridge.
- **Result:** Biped robots now stop at exact zero on neutral input without a server-side deceleration step; tracked robots and acceleration behavior remain unchanged.
- **Status:** COMPLETED
- **Completed:** 2026-11-20

- **Checkpoint:** Reproduced the normal live flow with API login `1`/`1`, selected the legged `Nova Aegis`, and entered a match. The HUD reached `0 km/h` after stop input, but the existing fix only called `NetworkTransform.Teleport()`, which marks the next packet as a teleport without explicitly forcing a fresh transform send.
- **Decision:** Keep authoritative legged speed at exact zero and strengthen the stop transition by forcing the current `NetworkTransform` state to be sent immediately after marking the teleport. This targets the owner-side buffered transform path without changing tracked robots.
- **Next step:** Patch the stop transition, rebuild, and repeat the same live match test.
- **Final checkpoint:** Rebuilt runtime and editor assemblies successfully. Repeated the live Unity flow after the `Teleport()` + `ForceSend()` patch: logged in with `1`/`1`, entered a match with the legged `Nova Aegis`, drove forward to `5 km/h`, pressed `Space`, observed `0 km/h` immediately, and confirmed the chassis/camera remained unchanged after more than two seconds. Reverse input was also stopped and remained at `0 km/h`.
- **Result:** Removed the remaining network-side stop drift by forcing the current authoritative `NetworkTransform` state to be sent when a legged robot transitions to zero speed. The editor scene was restored to `enableVehicleTestMode: 1` after validation.
- **Validation:** `dotnet build Assembly-CSharp.csproj --no-restore --nologo -v:minimal` and `dotnet build Assembly-CSharp-Editor.csproj --no-restore --nologo -v:minimal` both succeeded; live API/login/match movement validation passed. `git diff --check` still reports existing trailing whitespace in unrelated serialized `Client.unity` lines.
- **Limitations:** The live visual check verifies no movement after stop over the captured interval; no automated physics regression test exists for this controller.
- **Status:** COMPLETED

## 2026-11-22 — Deployment readiness audit, stage 1

- **Status:** IN_PROGRESS
- **Started:** 2026-11-22 02:21 Asia/Manila
- **User request:** Виконати повний технічний аудит і підготувати перевірений deployment plan для War of Machines на OVHcloud VPS-3; не купувати сервер, не підключатися до production, не виконувати deployment або production migrations, не змінювати gameplay.
- **Scope:** Unity client/dedicated-server Linux feasibility and runtime architecture; coordinated ASP.NET Core API/PostgreSQL security and migration state; local configuration, Git diffs, deployment docs and resource/operations plan. Expected documentation deliverables: `docs/deployment/DEPLOYMENT_PLAN.md`, `DEPLOYMENT_PROGRESS.md`, `DEPLOYMENT_CHECKLIST.md`.
- **Acceptance criteria:** Validate findings against current code/configuration and both repositories' working trees; distinguish facts, assumptions, and unperformed tests; document blockers, safe sequence for stages 2–5, redacted environment variable names, resource estimates and readiness criteria; preserve existing changes and stop after stage 1.
- **Initial state:** Both supplied repository paths exist and contain `AGENTS.md` and `TASK.md`. No separate deployment audit workflow exists in the global memory archive. Repository rules and journals have been read; previous journal history includes unrelated open-looking entries later marked complete. Existing working-tree state has not yet been inspected.
- **Next step:** Inspect both Git states, project documentation maps, Unity version/build support/dependencies and API configuration/migration owners before writing deployment conclusions.

### Audit checkpoint — current repository and Unity hosting state
- **Git state:** `main...origin/main`; the only working-tree change is the new append-only `TASK.md` entry for this audit. No existing source or asset changes were present before audit edits.
- **Editor/packages:** `ProjectVersion.txt` is Unity `6000.3.13f1`; FishNet is `4.6.4`, `com.unity.dedicated-server` is `2.0.2`, FishyUnityTransport is pinned to commit `08756d9733a556018041e52fcb9d2a6035346aeb`, and resolved Unity Transport is `2.7.2`.
- **Linux build readiness:** The Editor install has Windows Standalone support but no `LinuxStandaloneSupport` module/Mono/IL2CPP server variation. `Assets/Editor/LocalNetworkBuild.cs` only builds `StandaloneWindows64`, `.exe` names and Development builds; no Linux build profile was found. Linux server feasibility is therefore not yet build-verified; the missing build module and Linux build target are concrete blockers to a deliverable artifact.
- **Platform scan:** No native `.so`/`.dylib` plugin or direct Windows P/Invoke/API was found in owned runtime/editor code; the four `.dll` files are managed/editor/analyzer libraries. `Server.unity` still contains camera/render components, so the Linux server subtarget build must verify visual stripping and client-only paths.
- **Runtime topology:** `LobbyRooms` is a process-level room registry; `ServerRoom` and `GameplaySpawner` assign per-match additive scene handles/slots and offsets. `MatchBotPopulationService` fills each configured room up to `maxPlayersForFindRoom=20`; bot spawns are staggered. The architecture supports multiple rooms in one server process, but shared CPU/physics/AI cost and safe match count are unmeasured.
- **Network/security config:** FishyUnityTransport listens on UDP `7770`; `StartServerButtons` accepts runtime port/address env and command-line settings. `SecureFishNetTransportConfiguration` enforces encryption and fails closed without DTLS PEM/cert/key/name settings. Production client API host allowlisting is still hardcoded to the Render hostname, so VPS migration requires an HTTPS-only allowlist update.
- **Next step:** Verify the FishNet authenticator is actually wired into the runtime prefabs/scenes, trace server registration/shutdown and memory/tick diagnostics, finish API/database/security audit, then draft the three deployment documents.
- **Documentation location decision:** The backend project already owns `docs/codex-app/deployment-hosting.md`; use the backend project as the canonical location for the requested `docs/deployment/` set and supplement that existing deployment guide instead of duplicating the plan in Unity.

### Підсумок координації Stage 1 deployment audit — 2026-11-22
- **Результат:** Unity feasibility/runtime findings додані до API repo `WarOfMachinesAPI/docs/deployment/DEPLOYMENT_PLAN.md`; progress і checklist також збережені поруч із наявним backend deployment guide. Це канонічне місце, оскільки API repo вже містить `docs/codex-app/deployment-hosting.md`.
- **Підтверджено:** Unity 6000.3.13f1 installation не має Linux Standalone Support; відсутні Linux Server profile/build script, production client host allowlist досі обмежений Render. Архітектура підтримує кілька room у процесі, але без вимірювань перша ціль — один 20-slot матч.
- **Зміни Unity:** Unity source/assets/config не редагувалися; лише цей append-only TASK.md журнал змінений у Unity tree.
- **Обмеження:** Linux build, headless runtime, DTLS/network, capacity, production DB/endpoint не перевірялися. Production connection/deployment/migration не виконувалися.
- **Статус:** COMPLETED
- **Завершено:** 2026-11-22 (Asia/Manila)

## 2026-11-22 — Deployment readiness, stage 2

- **Status:** IN_PROGRESS
- **Started:** 2026-11-22 (Asia/Manila)
- **User request:** Виконати Stage 2 локальної Linux build і deployment readiness підготовки для War of Machines за вкладеним запитом.
- **Scope:** Unity 6000.3.13f1 Linux Dedicated Server module/profile/build/runtime; backend .NET 10 LTS migration; API health/readiness; configurable HTTPS host allowlists; production-oriented Compose/Caddy; Unity systemd/env/backup/health/rollback templates; validation and docs. No VPS purchase, production connection/deployment/migration, Stage 3, paid services, real secrets, auth/DTLS weakening, gameplay changes, or automatic Git commit.
- **Acceptance criteria:** Complete every safe local task in the attachment; report build vs runtime separately; run available builds/tests/config validation; preserve existing/uncommitted/generated changes; document exact modifications and remaining blockers; mark Stage 2 status and Stage 3 readiness truthfully.
- **Initial state:** Stage 1 docs and repository status are to be reviewed before implementation. Stage 1 reports existing tracked API bin/obj modifications from the API build; current state must be verified and preserved.
- **Next step:** Read Stage 1 deployment docs and inspect both repositories' current Git/worktree state, Unity install and build scripts, and API security/configuration owners.


### Stage 2 final checkpoint — 2026-11-22
- **Status:** BLOCKED; Stage 3 readiness: NO.
- **Completed:** Added a Unity Linux Dedicated Server build profile limited to `Server.unity` and `Map.unity`, and a Mono build menu action. Added an exact HTTPS production API host allowlist resource while keeping localhost Development-only and TLS verification unchanged. Added Linux systemd/env/credentials, release package/deploy/rollback/health/logrotate templates and handoff documentation.
- **Build evidence:** Unity 6000.3.13f1 Editor build succeeded at `D:\\Programing\\UnityProjects\\war-of-machines-local-builds\\LinuxServer-20261121-192956`. The output contains `WarOfMachinesServer.x86_64`, expected Unity data/shared libraries and ELF magic `7F-45-4C-46`; the Mono Dedicated Server subtarget was used. Editor script compilation had no errors and one existing warning in `ProfileStatisticsPopupRebuilder.cs`. `git diff --check` passed.
- **Blockers:** The artifact could not be launched on Linux because WSL returns `Wsl/E_ACCESSDENIED` and Docker has no Linux daemon. Headless scene loading, clean shutdown, UDP listener, DTLS/client handshake, API heartbeat and shell script syntax/runtime remain unverified on an isolated Linux host.
- **Safety boundary:** No production API/DB connection, real credential, deployment, migration, or gameplay change was made.
- **Next step:** Use an isolated Linux staging host/VM for the Stage 3 runtime, network, authentication, restore and capacity gates; do not treat the build artifact alone as runtime acceptance.
- **Completed locally:** 2026-11-22 (Asia/Manila); readiness remains blocked pending external Linux runtime evidence.
## 2026-11-22 — Stage 2B unblock Linux runtime testing

- **Status:** IN_PROGRESS
- **Started:** 2026-11-22 (Asia/Manila)
- **User request:** Continue War of Machines Stage 2B: diagnose local WSL/Docker access and run isolated Unity Linux Dedicated Server checks using the existing build.
- **Scope:** Read-only Windows/WSL/Docker diagnostics first; then non-destructive authorized local runtime and script checks if Linux access is available. No production connection, real secrets, destructive WSL/Docker repair, Windows feature changes or reboot without confirmation, gameplay change, VPS purchase, or commit.
- **Acceptance:** Establish actual WSL/Docker cause and status; launch existing Linux server and verify headless scene startup/UDP/graceful shutdown if feasible; record DTLS as verified or not; preserve uncommitted/generated state.
- **Initial state:** Prior Stage 2 journal records an ELF Linux server artifact at the deployment docs path and reports WSL access denied/Docker Linux daemon unavailable. Reuse that artifact; do not rebuild unless necessary.
- **Next step:** Read project instructions/journals and the Stage 2 docs, then inspect process state and perform read-only WSL/Windows feature/hypervisor/Docker diagnostics.
### Stage 2B checkpoint — diagnostic results (2026-11-22)
- **Status:** BLOCKED pending host-side read-only WSL diagnostics; Stage 3 not started.
- **WSL/Docker:** WSL package version is available, but status/list and Windows feature/CIM/registry reads are denied in this execution context. The requested escalation was rejected by the Codex approval policy. Docker CLI/Compose are installed, but `desktop-linux` has no engine pipe and Docker Desktop cannot start because its registry key is missing.
- **Runtime:** Existing Linux ELF was preserved and not launched. Headless startup, Linux dependencies, scene/shutdown, UDP 7770, API registration and DTLS handshake are NOT VERIFIED.
- **Scripts:** Git Bash `bash -n` could not start because MSYS cannot create a named object (`0xC0000022`); ShellCheck is unavailable. No deployment script was executed and executable mode remains unverified.
- **Safety:** No WSL repair/reinstall, Windows feature change, Docker reset/data deletion, production connection, real secret, or gameplay edit occurred.
- **Next step:** User runs the read-only Administrator PowerShell diagnostics documented in the API deployment docs outside Codex and sends output; then resume with the minimum safe fix.
### Stage 2B recovery update — Ubuntu available per user (2026-11-22)
- **Status:** IN_PROGRESS.
- **User reports** Ubuntu 24.04.5 LTS/WSL 2 is installed as `Ubuntu-24.04`, account `toraspc`, and host-side `whoami`/OS-release checks succeeded. This is user-provided evidence pending an independent Codex-side check.
- **Decision:** Preserve the existing WSL install; try the requested read-only distro command once. If denied by sandbox, stop and request host output instead of repeating.
### Stage 2B resume checkpoint — user reports Ubuntu; Codex still blocked (2026-11-22)
- **Status:** BLOCKED; Stage 3 not started.
- **User update:** Host Ubuntu 24.04.5 LTS/WSL 2 and `whoami=toraspc` are user-reported as verified.
- **Validation:** The one requested Codex attempt `wsl.exe -d Ubuntu-24.04 --exec whoami` returned `Wsl/E_ACCESSDENIED`. Codex host-shell escalation was rejected; computer-use rules prohibit running Windows terminal commands through UI, so no approved alternate terminal is available.
- **Runtime:** Existing Unity ELF remains untouched and unlaunched. Linux scene/headless startup, dependencies, shutdown, UDP 7770 and DTLS remain NOT VERIFIED. Docker Desktop start fails because its installation key is missing; Docker Engine inside Ubuntu is unknown.
- **Documentation:** API deployment progress/checklist now record the user's Ubuntu update and the access boundary.
- **Next step:** Resume only after an approved host-shell route becomes available; do not reinstall Ubuntu, change Windows features, delete Docker data, or start Stage 3.
## 2026-11-22 — Stage 2 final closeout under owner-approved scope

- **Status:** IN_PROGRESS
- **Started:** 2026-11-22 (Asia/Manila)
- **User decision:** Close Stage 2 as “COMPLETED — LOCAL PREPARATION SCOPE”; move unperformed Linux runtime, UDP/DTLS and multiplayer tests to Stage 3. Do not start Stage 3.
- **Scope:** Verify the existing Unity Linux Dedicated Server artifact/profile and deployment templates; update the coordinated task journal. No rebuild without need, runtime launch, production connection, VPS purchase/provisioning, gameplay change, commit, or discard.
- **Acceptance:** Distinguish existing build evidence from unperformed runtime tests; record Stage 2 complete only under the revised local-preparation scope and keep all runtime tests deferred.
- **Initial state:** User reports Ubuntu 24.04.5 LTS/WSL2 works on the host, but Codex's requested WSL invocation returned E_ACCESSDENIED; the existing build and deployment templates were preserved.
- **Next step:** Read current repo rules/journal, verify existing artifact/profile/templates and synchronize the Unity task journal with the deployment closeout.


### Stage 2 closeout completed — 2026-11-22
- **Status:** COMPLETED — LOCAL PREPARATION SCOPE (per explicit owner decision).
- **Verified:** Existing Linux Server profile is present and scoped to Server/Map; existing Unity Mono server artifact exists and has ELF header `7F-45-4C-46`; systemd, environment, logrotate and package/deploy/rollback/health templates are present.
- **Documentation:** API deployment progress/plan/checklist and deployment-hosting guide now define Stage 2 local-preparation completion and Stage 3 isolated staging. Runtime and integration checks are explicitly NOT TESTED — DEFERRED TO STAGE 3.
- **Validation:** Unity repository `git diff --check` passed. No rebuild or runtime launch was performed; prior successful Unity build report and current artifact were verified.
- **Limits:** Linux startup, scenes/shutdown, UDP 7770, DTLS, FishNet auth, API/DB containers, registration, tickets, gameplay, load and concurrency remain untested. Production readiness is NO.
- **Stage 3:** Ready for price/access preflight subject to approved network access and separate owner approval before VPS provisioning. No purchase or provisioning occurred; Stage 3 was not started.

## 2026-11-22 — Project rebranding to RAV
- **Status:** IN_PROGRESS
- **Started:** 2026-11-22 (Asia/Manila)
- **User request:** Fully rebrand the Unity multiplayer game from War of Machines to RAV (Robotic Assault Vehicles), tagline Tactical Multiplayer Combat; safely update owned source/config/docs, rename namespaces/assemblies where safe, audit old-name references, preserve existing work, and rename project folders if all checks allow.
- **Scope:** Unity project branding and technical identifiers, owned project-specific configuration/documentation/build naming; no gameplay behavior changes, destructive Git operations, production changes, external repository rename, or Stage 3B. Preserve serialized GUIDs and pre-existing uncommitted changes.
- **Acceptance criteria:** Update safe in-scope Unity branding and names; verify references/build/settings as feasible; report residual historical/compatibility references, actual validation, and any blocked physical rename.
- **Initial state:** Unity root instructions and latest journal entries read. Initial Git status shows existing user changes in LocalNetworkBuild.cs, HttpLink.cs, production-api-hosts resource/meta, Settings, deploy/, and TASK.md; preserve them. No implementation changes for this task yet.
- **Next step:** Inspect nested rules, task-specific naming references and existing Unity project settings/build profiles before editing; avoid changing gameplay logic.
### Discovery checkpoint — brand and compatibility map (2026-11-22)
- Unity source uses old branding in ProductName/build outputs, local build menu labels, diagnostics EditorPrefs keys, the hover-outline shader property, and WOM-prefixed client/server environment variables. The production Render host is an externally controlled HTTPS endpoint; preserve its URL/TLS allowlist. EditorPrefs keys and the shader property are persisted/internal contracts and need compatibility-aware migration.
- Unity deployment templates use old service/path/executable names and can be renamed as staging templates after every script reference is updated. Generated csproj/IDE state, diagnostic logs, and append-only task history are not source targets.
- API Git root is D:\Programing\ServerProjects\war-of-machines-API; the .NET project is nested at WarOfMachinesAPI. Initial Git snapshot shows pre-existing edits in Dockerfile, Program.cs, controllers, factory, logging, docs and generated net8.0 output, plus untracked deployment files. Preserve all; inspect touched-file diffs before patching.
- API namespaces are WarOfMachines.* across owned source and EF migration designer/snapshot metadata. Rename them coherently to RavApi.* while preserving migration IDs, database objects, JWT issuer/audience, local database names and production volume/service identifiers. The actual .env.local is never to be edited or printed.
- No implementation files changed for this task yet.
- **Next step:** Inspect the exact diff and file owners, map every environment-variable read/write, and verify current Unity build profile/settings and repository path references.
### Rebrand recovery checkpoint — 2026-11-22
- **Status:** IN_PROGRESS
- **Recovery finding:** The RAV implementation and coordinated docs are present; earlier discovery notes saying no implementation had started are historical and were superseded by the changes below.
- **Unity changes present:** Product name is RAV; owned build/editor labels and executable naming use RAV; runtime configuration reads RAV-prefixed values with WOM compatibility fallbacks; Linux deployment templates use RAV paths and names; compatibility notes were added.
- **API changes present:** Project/solution/assembly and owned namespaces use RavApi; Docker/deployment paths use the new project name; existing production host, JWT identifiers, database names, volumes, and user work are preserved.
- **Validation already recorded:** API `dotnet build` succeeded with 0 errors and two NU1900 feed-access warnings; no tests were run. Unity source currently specifies `RAVServer.x86_64` and `ProductName: RAV`.
- **Unresolved:** The existing local Linux build directory contains `WarOfMachinesServer.x86_64` / `WarOfMachinesServer_Data`; a fresh post-rebrand Linux build must be produced and checked. The current build run has no Unity process/log evidence.
- **Working tree:** Existing edits remain unstaged and uncommitted. Do not reset, revert, or delete user data.
- **Next step:** Run the dedicated Unity Linux batch build into the existing timestamped build root, inspect the resulting RAV executable and log/exit code, then close this task only if the fresh output is valid.

### Unity namespace migration preflight — 2026-11-22
- **Status:** IN_PROGRESS
- **Discovery:** The original namespace root `Game.*` remains in 257 Unity C# files, with 150 files importing it. Project-owned reflection strings use assembly-qualified names, and 117 scene/prefab/asset serialized identifiers store `Assembly-CSharp::Game.*` values. These are required dependencies for a safe namespace migration, not grounds to leave the rename incomplete.
- **Assembly definitions:** Two first-party asmdefs exist (`Game.Gameplay.Shared` and `Game.PlayMode.Tests`); the test asmdef references the shared assembly. Third-party asmdefs are out of scope.
- **Safety plan:** Migrate only first-party `Assets/Game` and `Assets/Editor` C# namespace roots and matching first-party Unity serialized type identifiers; update the two first-party asmdef names/root namespaces/references; preserve all script `.meta` GUIDs, gameplay code, and Unity's default `Assembly-CSharp` assembly identity.
- **Next step:** Apply the coordinated RAV namespace/assembly migration, audit for remaining first-party `Game.*` references, then run every available compile/reference check. Unity batch validation is currently blocked by Licensing Client/headless entitlement errors.

### Namespace migration encoding safeguard — 2026-11-22
- **Status:** IN_PROGRESS
- **Finding:** A first UTF-8 text migration attempt stopped on a source file containing a non-UTF-8 byte. No asmdef or serialized Unity asset update was reached. Earlier C# files may have received the exact root-prefix change before the stop.
- **Safety response:** Do not normalize encodings or replay through text decoding. Continue idempotently with ASCII-only byte substitutions over first-party source/assets, preserving every other byte and all `.meta` files/GUIDs.
- **Next step:** Finish the coordinated namespace and asmdef migration, audit references, then validate compilation as far as the host Licensing Client permits.

### Unity generated-project recovery checkpoint — 2026-11-22
- **Discovery:** Unity-generated IDE files still point at the pre-rename `war-of-machines-unity` absolute path and reference old custom asmdef project names. These generated `.csproj`/`.sln` files are present but ignored by Git, so they were not refreshed by the physical repository rename.
- **Decision:** Update the existing generated IDE files in place to the current project root and RAV assembly names, preserving project GUIDs/configuration; rename the two first-party generated project files and the generated solution to RAV names. Keep third-party project/assembly identities unchanged. The next successful Unity project generation may recreate these files.
- **Next step:** Apply the generated-file path/name migration, build the Unity C# and test projects with `dotnet` if their installed references permit, and audit remaining stale references.

### RAV rebranding final audit checkpoint — 2026-11-22
- **Status:** BLOCKED pending Unity Editor licensing/headless access and a final RAV Linux build; implementation and available local checks are complete.
- **Namespace/serialization audit:** 257 first-party C# files use `RAV.*`; no first-party `namespace Game.*` / `using Game.*` files or old `Assembly-CSharp::Game.*` serialized type references remain. The renamed shared and PlayMode-test asmdefs, generated solution/project files, and updated references use RAV names; no stale generated project path was found. Existing script `.meta` GUIDs were preserved.
- **Validation:** `RAV.Gameplay.Shared.csproj` and `RAV.PlayMode.Tests.csproj` compiled successfully with zero errors; PlayMode tests were not run. The full `Assembly-CSharp.csproj` MSBuild attempt stopped during project-reference resolution before C# compiler diagnostics. API build completed with zero errors and two NU1900 metadata warnings. Local and production Compose files passed static config validation using example/dummy validation inputs; no containers were started. `git -c core.safecrlf=false diff --check` passed in both repositories.
- **Unity Editor limitation:** The only Linux Editor attempt preceded the final namespace migration and produced no current RAV artifact. Its log reported Licensing Client IPC failures and `com.unity.editor.headless was not found`. The corrected method is `RAV.Editor.LocalNetworkBuild.BuildLinuxDedicatedServer`; do not treat the old WarOfMachines ELF as the rebranded build.
- **Compatibility:** Remaining old strings are intentionally retained and documented: current production HTTPS/JWT identities; WOM environment fallbacks; existing DB roles, local DB/volume names and persistent paths; EditorPrefs keys; legacy desktop shortcut names; previous build output root; append-only history. No DB, production endpoint/credentials, scene layout, gameplay, security setting, or production service was changed.
- **Next step:** Restore/sign in to a valid Unity Editor license in Unity Hub and ensure the Unity 6000.3.13f1 Linux Dedicated Server/headless build support is available. Then rerun the corrected RAV Linux build and verify the artifact. Stage 3B remains not ready until that validation and the separate Stage 3A prerequisites are complete.
## 2026-11-22 — RAV final Linux artifact validation
- **Status:** IN_PROGRESS
- **User request:** Verify the manually built Desktop `LinuxServer` Unity Linux Dedicated Server artifact, confirm it postdates the namespace rebrand, inspect Unity build logs/warnings where available, prepare a consistent RAV-named staging copy for future deployment, audit final rebrand state, and update reports. Do not run WSL/runtime tests, start Stage 3B, alter production, or buy a VPS.
- **Scope:** Read-only inspection of Desktop build and logs; verify Unity runtime/data/scenes/dependencies; compare Unity build/profile/deployment naming and API/rebrand docs; create a separate non-destructive RAV staging copy only if the original build is valid and all naming dependencies align; update project/deployment docs and task journal.
- **Acceptance:** Establish artifact completeness and creation time; classify available build warnings; confirm Unity/API builds and final RAV references; retain Linux runtime status as NOT TESTED; close rebrand only if both builds are supported by evidence.
- **Initial state:** Prior RAV implementation checkpoint was blocked only on a fresh Unity Linux build. User now reports a successful manual Unity Editor build with Personal license and Linux Mono/Dedicated Server modules installed, 0 errors and 47 warnings. Existing dirty repository state and prior build outputs must be preserved.
- **Next step:** Inspect existing Git state and locate the Desktop `LinuxServer` artifact and Unity Editor logs.
### RAV final rebranding validation closeout — 2026-11-22
- **Status:** COMPLETED. Linux runtime acceptance remains **NOT TESTED**, as requested.
- **Unity build:** Confirmed in `C:\Users\TorasPC\AppData\Local\Unity\Editor\Editor.log`: Linux Server Build Profile completed successfully from 19:55:57 to 19:56:46 local time; `Build Finished, Result: Success.`; `Server.unity` and `Map.unity` loaded. The 64-bit x86_64 executable, UnityPlayer, Data folder, `Assembly-CSharp.dll`, `RAV.Gameplay.Shared.dll`, Mono runtime, and native plugin ELF64 libraries are present. Code/namespace changes predate this build. `ProjectSettings.asset` reports `productName: RAV`.
- **Warnings:** 47 nonfatal build warnings, zero errors: 37 CS0618 obsolete API warnings, four CS0414 unused fields in FishNet utility/demo code, one CS0162 unreachable CFXR vendor-effect code path, and five FishNet upgrade-helper advisories. No RAV-owned compiler warning appears in this set.
- **Prepared artifact:** Kept Desktop `LinuxServer` intact and created `D:\Programing\UnityProjects\war-of-machines-local-builds\RAVServer-20261122-1956`. Renamed the executable and matching Data directory together to `RAVServer.x86_64` and `RAVServer_Data`; excluded `RAV_BurstDebugInformation_DoNotShip`. SHA-256 compared all 189 retained files with zero missing, changed, or unexpected files. Eight bundled `.so` libraries are ELF64 x86_64.
- **Deployment alignment:** Package script validates executable, Unity data, managed assembly, Mono runtime and UnityPlayer; package excludes the DoNotShip Burst debug directory. Deploy script rejects archives missing required data/runtime files. `git diff --check` passed. Linux shell script execution and runtime linker resolution were not tested.
- **API:** `dotnet build RavApi.csproj --no-restore --nologo -v:minimal` passed with zero warnings and errors. Unity rebrand audit: 257 RAV namespace files; no old first-party Game namespaces or serialized `Assembly-CSharp::Game.*` references. API audit: 48 `RavApi.*` namespace files and no old `WarOfMachines.*` declarations/imports. Compose configuration checks remain static only.
- **Compatibility/safety:** Preserved production HTTPS/JWT identifiers, database schema/migrations/roles, WOM fallback variables, Unity script GUIDs and historical preferences. No database, production, WSL, VPS, or Stage 3B operation occurred.
- **Remaining tests:** Linux runtime startup and host shared-library resolution, UDP 7770, DTLS, FishNet registration, API/PostgreSQL runtime, deployment script execution, and PlayMode tests remain **NOT TESTED**. Stage 3A remains in progress; Stage 3B is not started.
## 2026-11-23 — RAV Stage 3B preparation continuation and closeout

- **Status:** IN_PROGRESS
- **Started:** 2026-11-23 (Asia/Manila)
- **User request:** Resume the interrupted RAV Stage 3B deployment-readiness task from the existing state; preserve changes, avoid duplicate artifacts, finish the package/documentation/static checks, and stop before VPS deployment or Stage 3B runtime.
- **Scope:** API repository `D:\Programing\ServerProjects\rav-api` and Unity repository `D:\Programing\UnityProjects\rav-unity`; verify the existing staging bundle, configs and docs, run only the final requested local static checks, and close the deployment journal. Do not check OVH, WSL or production; do not deploy, run migrations, reset/clean, or commit.
- **Acceptance criteria:** Existing Unity/API artifacts and package are intact; Compose and configuration checks are evidence-backed; deployment plan/progress/checklist are current; `git diff --check` is recorded for both repositories; runtime-only tests remain deferred; no duplicate files or changed user data.
- **Starting checkpoint:** Prior handoff reports a fresh RAV Linux server artifact embedded in the staging bundle, Release API build and Compose static validation passed, artifact SHA-256 `712af20149fd1ec75484f8a7dc984b02761d998972c88c3ebf9ba76522e39c3f`, and deployment docs updated. Confirm these persisted before changing any project files.
- **Next step:** Inspect current Git state and deployment docs, verify archive checksum/content and secret exclusion, run both diff whitespace checks, then append final results and close this entry.
### Final verification checkpoint — 2026-11-23
- **Working trees:** API has 523 status entries and Unity has 309; these pre-existing/uncommitted changes are preserved. `git -c core.safecrlf=false diff --check` passed in both repositories.
- **Bundle:** The final archive exists at `D:\Programing\ServerProjects\rav-api\deploy\staging\artifacts\RAV-Staging-3B-20261122T154549Z.tar.gz`; current SHA-256 matches `712af20149fd1ec75484f8a7dc984b02761d998972c88c3ebf9ba76522e39c3f`. Outer archive listing has 119 entries and includes Compose/Caddy, staging env template, API, Unity release archive, systemd units, bootstrap/deploy/rollback/backup/restore/smoke/performance scripts, and first-deploy docs.
- **Path correction:** The canonical deployment documents are under `RavApi\docs\deployment\`, not at the repository root. The first read-only path assertion used the wrong root; the corrected search located all three requested documents. No files were changed by the failed assertion.
- **Next step:** Run the final manifest/secret-name and Codex config byte-preservation checks, add a concise 2026-11-23 closeout note to the three canonical deployment documents, then close this journal entry. Runtime/VPS checks remain deferred.
### Stage 3B local preparation closeout — 2026-11-23
- **Status:** COMPLETED for local preparation only.
- **Result:** The existing authoritative bundle is intact and ready as an artifact; SHA-256 `712af20149fd1ec75484f8a7dc984b02761d998972c88c3ebf9ba76522e39c3f`, 119 archive entries, all required staging files present, no unsafe archive paths or secret-material filenames. No duplicate bundle was created.
- **Documentation:** `RavApi\docs\deployment\DEPLOYMENT_PLAN.md`, `DEPLOYMENT_PROGRESS.md`, and `DEPLOYMENT_CHECKLIST.md` now record the final archive, readiness gates, prior API/Compose pass evidence, current partial local static validation, and all deferred runtime tests.
- **Validation:** API Release build and Docker Compose static validation had passed during the original preparation (not rerun here). Final API and Unity `git diff --check` passed after documentation/journal updates. Existing working trees remain uncommitted and preserved.
- **Limitations:** Bash syntax was not parsed locally because the previous Git Bash launch returned `E_ACCESSDENIED`; that attempt was not repeated. The last owner-provided VPS state had no active endpoint/IP or SSH path; OVH was not rechecked. Linux host setup, API/PostgreSQL runtime, Unity headless startup, UDP 7770, FishNet, DTLS, registration/tickets, gameplay/load, backup/restore execution, and deployment/rollback remain **NOT TESTED**.
- **Readiness:** Bundle **READY**; local static validation **PARTIAL**; **READY FOR VPS DEPLOYMENT: NO** until VPS access is available and the syntax preflight passes. Stage 3B runtime **NOT STARTED**. No production, VPS, migration, or runtime operation occurred.
- **Completed:** 2026-11-23 (Asia/Manila).
## 2026-11-23 — RAV local deployment readiness resume

- **Status:** IN_PROGRESS
- **Started:** 2026-11-23 (Asia/Manila)
- **User request:** Restore context from local AGENTS/TASK/deployment progress/checklist, inspect both repositories, continue only unfinished local deployment-preparation work, and state actual session permission mode first. Do not edit Codex config or production, perform payment, or run destructive database operations.
- **Scope:** Read the current RAV deployment handoff, inspect current repository/package state, and resolve any remaining local static-validation gap only if an already available permitted tool can do so. Preserve all existing changes and artifacts. Do not check OVH, repeat WSL/Docker diagnostics, deploy, migrate, commit, reset, clean, or start Stage 3B runtime.
- **Acceptance criteria:** Current status and pending work are verified; only unfinished safe local work is attempted; deployment docs/journal reflect accurate evidence; remaining external/runtime gates are explicit.
- **Starting state:** The prior closeout marks the bundle READY, local static validation PARTIAL, VPS deployment not ready, and Stage 3B runtime NOT STARTED. Bash syntax parsing was deferred after an earlier Git Bash `E_ACCESSDENIED`; do not retry that executable.
- **Next step:** Read current API deployment progress/checklist, then inspect repository status and determine whether a different already-installed local Bash syntax checker is available without WSL/Docker or network installation.
### Resume inspection checkpoint — 2026-11-23
- **Repository state:** API status still has 523 entries; Unity has 309. All three deployment documents are untracked in the API repository, and `TASK.md` is the Unity journal. No reset, clean, commit, or project-code change occurred.
- **Bundle state:** The same archive remains present (77,732,985 bytes) with the previously recorded SHA-256; no duplicate was created.
- **Remaining local check:** `shellcheck`, `checkbashisms`, `sh`, and a global Node Bash parser are unavailable. The only `bash.exe` found is the WindowsApps alias; it was not executed because this task must not repeat WSL diagnostics. The earlier Git Bash executable was not retried.
- **Documentation validation:** Found two trailing spaces in the untracked checklist's existing header lines 3-4. Removed only those spaces and corrected the closeout note to state that `git diff --check` does not include untracked docs; final whitespace scanning remains to be run.
- **Next step:** Re-run `git diff --check` for tracked changes, scan all three untracked deployment docs for trailing whitespace, verify the current readiness note, then close this entry with Bash syntax still deferred to VPS preflight.
### Final resumed validation — 2026-11-23
- The authoritative bundle checksum and 119-entry archive listing remain unchanged.
- Tracked API and Unity changes pass `git diff --check`. The three deployment Markdown files are untracked; a separate whole-file whitespace scan now finds zero trailing whitespace after removing the two pre-existing checklist header spaces.
- No local `shellcheck`, `checkbashisms`, `sh`, or global npm Bash parser is available. Only the WindowsApps `bash.exe` alias was found and left uninvoked; the earlier Git Bash denial was not repeated, and no WSL diagnostic was run.
- Remaining local preparation is complete. The Linux `bash -n` gate remains the first non-mutating preflight on the future VPS before any host changes.
- **Next step:** Record this final tool/whitespace check in deployment progress and checklist, rerun final whitespace/Git checks, and close this entry. No config change, host operation, or production access.
### RAV local deployment preparation resume closeout — 2026-11-23
- **Status:** COMPLETED for the local preparation scope.
- **Result:** Existing staging bundle remains READY at the recorded path/hash; no duplicate package, production change, host operation, database operation, or config.toml change was made.
- **Documentation:** Appended the current-session tool/whitespace findings to `DEPLOYMENT_PROGRESS.md` and `DEPLOYMENT_CHECKLIST.md`; the plan retains the same current readiness gate. All three are untracked, were explicitly scanned, and now have zero trailing whitespace.
- **Validation:** Bundle SHA-256 still matches; `git diff --check` passed for tracked API/Unity changes; API Release and Compose static validation remain prior-session passes and were not repeated.
- **Remaining limitation:** No local Bash syntax parser is available; the WindowsApps `bash.exe` alias was not run and the earlier Git Bash `E_ACCESSDENIED` was not repeated. Keep the non-mutating `bash -n` preflight as the first VPS gate. Last owner-provided state had no active VPS endpoint/SSH path; OVH, WSL, Docker, production, and VPS were not checked or operated in this turn.
- **Final state:** Local bundle preparation complete; local static validation **PARTIAL**; VPS deployment **NOT READY** until access and preflight; Stage 3B runtime **NOT STARTED**.
- **Completed:** 2026-11-23 (Asia/Manila).
## 2026-11-23 — RAV Stage 3A order status and secure access

- **Status:** IN_PROGRESS
- **Started:** 2026-11-23 (Asia/Manila)
- **User request:** Check OVH order #23370514 once; if active, verify the VPS identity/specs/billing and, only with working authorized SSH access, complete secure Stage 3A setup. If pending, do not retry endlessly or create another order; report and stop. Verify the existing bundle and update deployment progress/checklist. Do not edit config.toml, touch production/database, perform paid actions, or start Stage 3B.
- **Scope:** Authenticated OVH Control Panel read-only inspection; local read-only verification of the existing RAV staging bundle. If the server is active and SSH access is valid, perform only explicitly authorized Stage 3A security/operations setup, preserving fallback access and avoiding public PostgreSQL exposure.
- **Acceptance criteria:** Actual order/service status is evidenced; pending branch stops after one check, or active branch confirms hostname/IP/region/Ubuntu/resources/billing and secure SSH before setup; bundle hash and required files are verified; deployment progress/checklist and this journal record the result; Stage 3B remains NOT STARTED.
- **Initial state:** Last recorded OVH checkpoint said order #23370514 payment validation was pending and no VPS/IP/SSH endpoint existed. This is historical evidence only; current status must be checked once in the authenticated panel.
- **Next step:** Read the latest deployment handoff, then inspect order #23370514 once in the current OVH browser tab.
### OVH order and bundle checkpoint — 2026-11-23
- **OVH:** Made one read-only inspection of order #23370514. The tracker step says “Your order has been processed,” but the latest order-history event is **“Checking your order”** (2026-11-22 21:53:48), requiring additional processing usually around 48 hours; the preceding event says **“Your payment is being validated.”** This does not confirm payment clearance, an active VPS, or provisioning.
- **Order summary shown:** VPS-3 2027 monthly fees, Linux distribution, 6 vCPU, 12 GB RAM, 100 GB disk, Local Storage and Automated Backup Standard. Actual installed Ubuntu, assigned region, hostname/IP, invoice/payment clearance and current service activation are not confirmed on this order page.
- **SSH:** Not tested; no active endpoint was confirmed. No key, account, firewall, SSH, or Docker changes were made.
- **Bundle:** Existing archive SHA-256 still matches the handoff; 119 entries and all required Compose/Caddy/API/Unity/backup/restore/smoke files are present. No rebuild or duplicate archive.
- **Next step:** Append this authoritative pending status and bundle verification to deployment progress/checklist, then stop awaiting OVH’s external order review. No second order, payment, production, WSL, or Stage 3B action.
### Stage 3A wait for OVH order review — 2026-11-23
- **Status:** BLOCKED on the external OVH order/payment review, per the user’s stop condition.
- **Result:** One authenticated read-only check found order #23370514 still at **Checking your order** with payment **being validated**; the order page does not prove an active VPS. No repeated status checks, duplicate order, SSH attempt, paid action, or host change occurred.
- **Bundle:** Existing archive verified with the recorded SHA-256 and required entries; no rebuild.
- **Documentation:** Updated `RavApi\docs\deployment\DEPLOYMENT_PROGRESS.md` and `DEPLOYMENT_CHECKLIST.md` with this timestamped status and explicit unverified fields.
- **Validation:** API and Unity tracked `git diff --check` passed; both updated untracked deployment docs have zero trailing whitespace.
- **Recovery action:** Wait for OVH to change the existing order to provisioned/active or for the owner to provide an update. Then resume Stage 3A with one status review and verified SSH/fingerprint checks. Do not start Stage 3B without separate authorization.
- **Stage status:** VPS **PENDING**; SSH **NOT TESTED**; Stage 3A **IN PROGRESS**; Stage 3B **NOT STARTED**.
### Stage 3A resumed after owner request — 2026-11-23
- **Status:** IN_PROGRESS, resumed from the prior external-order blocker.
- **User update:** The owner requests one fresh status check for order #23370514. The actual session permission mode is `danger-full-access` / unrestricted filesystem / `never`; do not change config.toml.
- **Scope reaffirmed:** If the order is still under review, do not attempt SSH or repeat status checks; verify the existing bundle, update progress/checklist, and stop. If active, verify service details before any authorized Stage 3A setup. No duplicate order, payment, production/database change, or Stage 3B.
- **Next step:** Read current deployment progress/checklist, then refresh the existing order page once and follow the pending/active branch.
### Fresh OVH status refresh — 2026-11-23
- **Current order page:** Reloaded the existing order URL exactly once. The progress indicator says “Your order has been processed,” but the latest history row still says **“Checking your order”** at 2026-11-22 21:53:48 and requests additional processing (usually around 48 hours). Payment clearance and active VPS provisioning are not confirmed.
- **Decision:** Pending branch applies. Do not open service inventory, probe SSH, or change the host. No new order or payment action.
- **Bundle:** Rechecked; 119 entries, expected checksum, and all required staging files present.
- **Next step:** Append the fresh order-page result and bundle verification to deployment progress/checklist, run whitespace/Git checks, then stop pending an OVH status change.
### Stage 3A refresh stop — 2026-11-23
- **Status:** BLOCKED on OVH order/payment review; the user’s pending-order stop condition is met.
- **Evidence:** One reload of order #23370514 still shows the latest history row **“Checking your order”** (2026-11-22 21:53:48; additional processing usually around 48 hours). The tracker’s “Your order has been processed” step does not establish a provisioned service. Cleared payment and active VPS are unconfirmed.
- **Bundle:** Existing 119-entry bundle SHA-256 matches; required staging files are present. No rebuild or duplicate.
- **Documentation/validation:** Progress and checklist updated. API and Unity tracked `git diff --check` passed; updated untracked documents have zero trailing whitespace.
- **No operations:** No service-list/SSH probe, key generation, host hardening, payment, second order, production/database, or Stage 3B action.
- **Recovery action:** Wait for OVH/account-owner notification or a verified order state change; then resume once. Until then VPS is **PENDING**, SSH **NOT TESTED**, Stage 3A **IN PROGRESS**, Stage 3B **NOT STARTED**.
### Owner asks payment-status check — 2026-11-23
- **Status:** BLOCKED on OVH payment validation and provisioning.
- **Result:** Read the current checkout page and the existing tracker for order #23370514. The tracker still says “Checking your order”; its preceding event says “Your payment is being validated.” No successful/cleared payment or active VPS is confirmed. The checkout page showed an out-of-stock error for the selected UK region; no payment was submitted.
- **Configuration decision:** Do not begin VPS setup yet. There is no confirmed active endpoint/IP; SSH remains **NOT TESTED**. Wait for OVH to confirm payment and provision the existing order, then resume Stage 3A with verified service details and host fingerprint.
- **Scope respected:** No repeated polling, payment, second order, SSH probe, host changes, production/database access, or Stage 3B.
## 2026-11-23 — RAV concise Game Design Document
- **Status:** IN_PROGRESS
- **Started:** 2026-11-23 22:39 (Asia/Manila)
- **User request:** Fully analyze the RAV Unity game and backend API, then write a concise, professional game design document for the user to review and correct.
- **Scope:** Read-only audit of Unity client/dedicated server and RAV API source, project documentation, and available configuration. Create one user-facing document under the current task's `outputs` directory. Do not change game/API implementation, production, database, deployment, or runtime state.
- **Acceptance criteria:** Describe the actual player experience, core loop, roles/vehicles, match flow, progression, multiplayer architecture, and current limits using verified project evidence; separate implemented behavior from planned or unverified behavior; keep the GDD concise and readable; render and inspect the final document.
- **Initial state:** RAV Unity project is `D:\Programing\UnityProjects\rav-unity`; API source is nested at `D:\Programing\ServerProjects\rav-api\RavApi`. Existing local task journals contain unrelated deployment tasks and user changes; preserve all existing entries and project state.
- **Next step:** Inspect repo status, nested instructions, source/docs inventories and the existing gameplay/API contract; then draft, render, visually inspect, and close this task entry.
### Source inspection checkpoint — 2026-11-23
- Both requested projects are present at the RAV paths. The Unity working tree contains extensive pre-existing modified and untracked gameplay, asset, API-client, and rebrand/deployment files; the API working tree contains extensive pre-existing rebrand/deployment changes. All are preserved and treated as the current source of truth.
- Unity documentation index maps the relevant menu/progression, combat, bots, matchmaking/spawn, maps, and FishNet transport owners. API documentation maps profile, catalog/research, server discovery, authentication, match result/reward, maps and leaderboard contracts.
- The only edits so far are this task's append-only journal entries in each repository. No game/API implementation, assets, scenes, configuration, or deployment state has been changed.
### Design audit findings — 2026-11-23
- Current Unity match configuration: 20 total slots, bots enabled to fill open slots, 300-second match timer, two MMR-balanced teams, and one playable map scene (`Map`). Current end rule awards victory when only one team has living vehicles; timeout currently ends as a draw even though the end screen reports team kill totals.
- Current API seed catalog: two factions (Iron Alliance tracked and Nova Syndicate biped), three vehicle classes (Scout, Guardian, Colossus), and a multi-level research tree. Twenty-one vehicle records are seeded; eight are marked visible and thirteen unavailable. New API registrations initialize 0 MMR, 50,000 Bolts, 5,000 Free XP, and an IA Skirmisher starter vehicle.
- API owns player/session authentication, one-time server-bound connection tickets, dedicated-server registry/discovery, catalog/research/ownership, match records, rewards, profile statistics, and leaderboards. `Adamant` is present in profile data but no active earn/spend loop was found in current controllers.
- Integration questions for the draft: API seeds `Demo Yard` and `Steel Arena`, while Unity currently lists only the `Map` gameplay scene; timeout-result policy needs owner confirmation. No RAV API social, campaign/lore, monetization, or release-platform system was found in the inspected current scope.
- No tests or runtime processes were launched. Source and project documentation remain unchanged; only append-only journals were updated.

#### Завершення — 2026-11-23
- Створено професійний короткий GDD українською: `C:\Users\TorasPC\Documents\Codex\2026-11-23\new-chat-4\outputs\RAV_Game_Design_Document_Draft_v0_1.docx`.
- Зафіксовано перевірені поточним кодом правила бою, бойову систему, фракції та класи, прогресію/економіку й межі інтеграції Unity–Dedicated Server–API; окремо винесено авторські рішення для підтвердження.
- Через відсутність LibreOffice DOCX експортовано у PDF встановленим Microsoft Word; обидві сторінки переглянуто як зображення. Після компонування фінальна версія має 2 сторінки без відірваного останнього пункту.
- Зміни у вихідний код гри й API не вносилися; тести не запускалися.
- Статус: COMPLETED.

### Edited GDD comparison checkpoint — 2026-11-23
- Compared the Desktop DOCX with the prior generated draft. It contains no Word comments or tracked insertions/deletions. Visible content changes are: (1) additional modes described only as future, (2) match duration changed from 5 to 10 minutes, (3) new-profile grant changed from 50,000 Bolts / 5,000 Free XP to 1,000 / 500, (4) Adamant described as a future donor currency for shop items, and (5) clarified names of the eight visible vehicles.
- User explicitly authorized API changes for the starting grant. Apply the concrete match duration, search wait, and API starting grant. Treat additional modes and shop/Adamant monetization as future roadmap language without enough rules to implement safely; do not create modes, store, payments, or purchase mechanics from the document alone.
- Existing user working-tree changes are extensive in Unity and API and were observed before changes; preserve them.
- Next: verify current settings/default owners in Unity and API source, then patch only those values and their system docs.

### Owner/source checkpoint — 2026-11-23
- Unity `ServerRoom.RunMatchmakingTimer` reads `ServerSettings.GetFindRoomSeconds()` and publishes the remaining seconds through `RoomController.UpdateTimer`; `MatchmakingStatusView` displays that synchronized value. `ServerSettings.prefab` currently serializes 1 second, while C# and remote-snapshot defaults are 60 seconds. Set the active prefab and all fallback defaults to 30 seconds; full real-player rooms still start immediately.
- Match duration is owned by `MatchSceneGlobalSettings.matchDurationSeconds` and `NetworkGameplayTimer`; the settings class and prefab currently use 300 seconds. Set both to 600 seconds. Update the Unity settings inspector examples and mechanics docs.
- API normal registration currently grants 50,000 Bolts / 5,000 Free XP; empty-database dev seed creates a separate testuser with 10,000 / 0. User approved API scope. Align both new-account grants to 1,000 / 500; keep the test profile's MMR 1000 and do not alter existing persisted accounts. Update auth/profile/seed docs.
- No source files have been edited yet. Next: apply targeted value changes and docs while preserving all existing project modifications.

### Implementation checkpoint — 2026-11-23
- Updated Unity defaults: matchmaking window `30` seconds in `ServerSettings`, `RemoteServerSettings`, and `ServerSettings.prefab`; match duration `600` seconds in the settings class default and serialized prefab. Existing full-player immediate-start rule remains intact; bot fill still occurs after the wait expires.
- Updated `ServerSettingsEditor` examples and `server-settings.md` / `match-lobby-spawn.md` to document 30-second maximum wait, 10-minute timer, and unchanged timeout-as-draw behavior.
- Updated API normal registration and empty-database test-user seed to 1,000 Bolts / 500 Free XP. Existing DB records are not changed. Updated auth/profile/EF seed docs; no schema/migration change.
- Did not implement the future extra modes or the future Adamant shop/economy because the edited GDD gives no mode mechanics, item catalog, prices, purchase rules, or payment flow.
- Next: verify targeted source/config/docs values and whitespace; do not run tests per task scope.


### Task closeout — 2026-11-23 23:26 (Asia/Manila)
- Applied the concrete GDD settings in Unity: matchmaking room wait 30 seconds, match duration 600 seconds (10 minutes). The wait is a maximum: a room still starts early if it reaches the real-player target; otherwise bot fill follows the 30-second timeout.
- Updated the ServerSettings C# defaults/fallbacks, remote snapshot fallback, serialized ServerSettings prefab, inspector examples, and the matchmaking/server-settings mechanic docs.
- Updated API new-registration and empty-database development seed grants to 1,000 Bolts / 500 Free XP; updated auth/profile/EF seed docs. No existing database account was touched.
- The edited DOCX's future mode and Adamant shop statements remain roadmap items; no mode rules, item catalog, prices, purchase flow, or payment design was provided, so no such features were invented.
- Validation: targeted source/config/doc value scans match 30, 600, 1,000, and 500; old account grant values no longer occur in API C#; Unity git diff --check reported no whitespace errors (only standard LF/CRLF warnings). No Unity/API tests, builds, runtime, or database actions were run.
- State note: the Unity prefab in the current checkout had findRoomSeconds=1 (not 3) before this change; C# fallback was 60. The source and prefab now consistently target 30 seconds. Existing working-tree changes were preserved.
- Status: COMPLETED.

## 2026-11-23 — RAV 10-second battle countdown and match timer start
- **Status:** COMPLETED
- **Started:** 2026-11-23 23:44 (Asia/Manila)
- **Request:** Set the pre-battle countdown to 10 seconds and start match-time consumption only after that countdown completes.
- **Changes:** Changed the serialized `Client` scene countdown from 3 to 10. `NetworkGameplayTimer` now initializes the display to the full match duration and holds that duration while the server countdown gate is active; it starts decrementing after all connected real players report countdown completion. Added a room-lock query to `BattleCountdownGate` and updated countdown/spawn documentation.
- **Validation:** Static scans confirm the scene value is 10 and the timer waits on the server countdown gate before decrementing; targeted `git diff --check` found no whitespace errors. Unity tests/build/runtime were not run.
- **Preservation:** Kept all unrelated pre-existing working-tree changes intact.

### Git fork sync after repository rename — 2026-11-23
- **Status:** IN_PROGRESS.
- **User request:** Restore/sync the Unity and API repositories in the fork after the repositories were renamed.
- **Safety scope:** Inspect existing branch/upstream/remotes and working-tree changes first. Preserve all local modifications. Do not commit, reset, rebase, clean, or discard anything. Fetch remote refs; only fast-forward a local branch if its upstream and working tree make that safe.
- **Acceptance:** Identify the configured fork/upstream repositories, determine whether the local clones are behind or point to obsolete names, sync both repos safely where possible, and report any unresolved fork-hosting issue.
- **Next:** Inspect Git status/remotes/current branch tracking without changing source files.
#### Fork sync findings — 2026-11-23
- GitHub HTTPS fetch succeeded for the existing origin repositories without changing the checked-out source. Both local main branches are exactly aligned with fetched origin/main (0 commits ahead, 0 behind); there was nothing to merge into either working tree.
- GitHub metadata identifies scorpioner2010/war-of-machines-unity and scorpioner2010/war-of-machines-API as ordinary repositories (fork=false), and the current canonical names are still those old slugs. Candidate names scorpioner2010/rav-unity and scorpioner2010/rav-api were not found.
- SSH fetch failed because this session has no usable GitHub public key. HTTPS fetch worked. No origin URL was changed.
- Existing worktrees already had 313 (Unity) and 523 (API) changed/untracked entries. No checkout, merge, rebase, commit, reset, clean, or source-file change was made; only the append-only task journal was updated.
- Status: BLOCKED for the requested fork sync. No fork remote is configured, and the intended fork URLs/owner are not identifiable from the available remotes. Recovery: provide the GitHub URL(s) or owner/name of the intended RAV fork(s); then fetch and fast-forward only if safe.
#### Interpretation correction and Fork app handoff — 2026-11-23
- The user meant the desktop Fork Git client, not a GitHub fork relationship. The earlier GitHub metadata check is not the intended outcome.
- Fork is open on the unrelated VR-Demo repository; the RAV Unity and API local clones are not among the open tabs observed.
- A UI interaction was interrupted by repeated “user input was detected in this window” notices. Stopped UI control to avoid interfering with the user's active Fork session. No repository was added or removed in the app.
- Existing local clones remain present and their main branches are synchronized with their configured origin/main. The only repo changes by this task are the task-journal additions; the fetch updated Git remote-tracking refs without touching source or worktree files.
- **Status: BLOCKED on the Fork window being free for UI control.** Resume by adding the existing folders D:\Programing\UnityProjects\rav-unity and D:\Programing\ServerProjects\rav-api as existing repositories in Fork; do not clone over them or replace their working directories.
### Completed — RAV repositories added to Fork — 2026-11-24
- Correctly interpreted “Fork” as the desktop Git client. Opened the existing local repositories using File > Open Repository; the Fork window now shows separate tabs for rav-unity and rav-api alongside the pre-existing VR-Demo tab.
- Verified the displayed repositories are the existing paths D:\Programing\UnityProjects\rav-unity and D:\Programing\ServerProjects\rav-api. No duplicate clones were created and no repository source files were edited.
- HTTPS fetch updated/checks the existing origin refs; both local main branches are at 0 ahead / 0 behind origin/main, so no commit needed merging. Existing working-tree changes remain preserved.
- **Status: COMPLETED** for making both RAV repositories visible/open in Fork.
## 2026-11-24 — RAV Stage 3A Hetzner CX33 provider switch
- **Status:** IN_PROGRESS.
- **User request:** Replace the canceled OVHcloud VPS order with a Hetzner Cloud CX33 staging VPS evaluation and prepare up to, but not including, payment.
- **Scope:** Verify current official CX33 availability, Germany/Finland regions, x86-64 specs, public IPv4, hourly/monthly price and applicable VAT; inspect account/identity/prepayment/refund requirements and future x86 resize options. Compare only available x86 plans if CX33 is unavailable, without automatic upgrade.
- **Safety:** Do not create a paid server/resource or authorize payment. Do not accept legally binding contract terms. Use only accurate owner-provided identity/address data; pause for user input where the true account details or personal verification are required. Do not start Stage 3B or change production/database.
- **Acceptance:** Provide official-source evidence, account/verification status and any exact owner action; reach the final checkout review only if possible without payment, account misrepresentation, or legal acceptance.
- **Initial known state:** OVHcloud order #23370514 was canceled per owner; the separate $14.50 refund case remains unresolved. Existing RAV deployment bundle is prepared; no active VPS endpoint is available.
- **Next:** Verify official Hetzner plan/pricing and account requirements; use the in-app browser for any needed sign-in or owner-controlled identity steps.### Hetzner evaluation checkpoint — 2026-11-24
- Hetzner’s official Cost-Optimized catalog currently marks CX33 unavailable; the listing specifies 4 shared x86 vCPU, 8 GB RAM, 80 GB NVMe, and eu-central sites NBG1 (Germany) / HEL1 (Finland).
- Published CX33 price: €8.49/month / €0.0136/hour before VAT. Required public Primary IPv4 adds €0.50/month / €0.0008/hour, total €8.99/month before VAT. €10.70/month is only an illustrative German 19% VAT total; account-specific tax remains to be confirmed.
- Public CX23 is also unavailable; no x86 alternative within budget was confirmed available. x86 rescaling is supported, but server location cannot be changed in place and disk cannot be reduced below the existing disk.
- The owner completed the browser security check and submitted the Ukrainian individual profile in Hetzner signup; EUR is selected. The account page supports English/German only and currently offers credit card. No card data, account-finalization action, payment, paid VPS, production, or Stage 3B operation was performed.
- Hetzner may request ID or card prepayment. Its published rule generally returns unused verification credit only after account closure and settlement of outstanding invoices; adding credit does not make unavailable products available.
- **Status:** Stage 3A IN PROGRESS, waiting for owner-controlled payment-method/account setup before account-specific availability and tax-inclusive checkout checks. Stage 3B NOT STARTED.
### Hetzner Console diagnosis — 2026-11-24

- Console access works; the Default project has 0 resources. No invoices/current transactions were found and the credit balance is USD $0.00; a pending card authorization was not checked. 2FA is only recommended. The account email page asks for an independent contact address and is not a rejection screen.
- Account billing currently uses USD, although EUR had been selected in the earlier signup form. Hetzner says currency cannot be changed after registration. The Ukrainian contact profile is not the apparent blocker.
- Cost-Optimized x86 options, including CX33, are disabled for this account in Helsinki, Nuremberg and Falkenstein. Regular Performance plans are selectable, but CPX12 has only 1 vCPU/2 GB RAM and CPX32 costs far above the target.
- No server was created and Create & Buy now was not pressed. Stage 3A awaits the owner’s decision about USD billing and waiting for CX stock versus considering a more expensive CPX32. Stage 3B remains NOT STARTED.

## 2026-11-25 — RAV Edgegap free-trial deployment evaluation
- **Status:** IN_PROGRESS.
- **User request:** Evaluate the existing Edgegap account/free trial and prepare/test a separate Linux AMD64 RAV Unity Dedicated Server deployment, using no paid resources and no production changes.
- **Scope:** Check Edgegap account/trial limits and European UDP deployment support; audit the existing RAV Unity Linux build and API integration; prepare a separate Edgegap container/deployment configuration only if it does not alter the production bundle; verify UDP 7770, FishNet/DTLS, endpoint/port mapping, and safe smoke tests within the free-trial allowance; update Edgegap/deployment docs.
- **Safety:** Do not create a new account, request or handle passwords/codes, bypass CAPTCHA/verification, add billing/card data, incur charges, disable TLS/DTLS validation, alter gameplay without need, modify production, or run tests beyond verified free limits. Stop deployments after authorized free tests. Preserve all pre-existing dirty files and journal history; no commit/reset/clean.
- **Acceptance:** Record account/trial status and actual limits; validate only existing artifacts; prepare and, if safely free and compatible, test an isolated Edgegap staging deployment; clearly mark anything blocked by credentials, plan limits, endpoint compatibility, or unavailable VPS/API staging.
- **Initial state:** No Edgegap-specific entry was present in this journal. Hetzner Stage 3A remains IN_PROGRESS in its existing separate entry; do not close or overwrite it.
- **Next:** Inspect Edgegap account in a new browser tab and verify free-trial terms before creating resources; inspect repository status/artifacts before any edits.## 2026-11-25 2026-11-25 02:52:41.000 +08:00 — RAV Altare free hosting preparation
- **Status:** IN_PROGRESS.
- **User request:** Inspect Altare free hosting and prepare the existing RAV Unity FishNet Dedicated Server for possible $0 deployment, without spending Bolts or changing production.
- **Scope:** Audit Altare account/products, free-plan access, Workstations availability, Docker deployment support, UDP 7770/DTLS/public endpoint, runtime behavior, regions and limits; inspect existing Unity/API repositories and build; prepare separate Altare deployment files only if technically suitable and free; document validation and blockers.
- **Safety:** Strict budget $0. Never upgrade, add a payment method, buy or consume Bolts, enable automatic billing, or create resources when cost is unclear. Do not change production auth/matchmaking/database, run production migrations, expose PostgreSQL, embed secrets, disable TLS/DTLS validation, or claim 20-slot support without load tests. Preserve working-tree changes and all task history; no commit/reset/clean.
- **Acceptance:** Verify platform capabilities from the authenticated dashboard and official docs; inspect actual RAV artifact/source paths and compatibility; create only isolated deployment artifacts within existing repositories if appropriate; run only safe local validations; report READY FOR DEPLOYMENT / READY FOR LIMITED TESTING / NOT SUITABLE based on verified UDP/DTLS and cost constraints.
- **Initial state:** Unity and API repositories already contain unrelated in-progress task journal entries, including Hetzner and Edgegap work; leave those entries untouched. No Altare-specific workflow was found in the Codex memory archive.
- **Next:** Open the official Altare console in a new in-app browser tab for the user’s login, then verify account/plan before inspecting or creating any platform resource.

#### Altare login handoff — 2026-11-25
- Opened the official console at `https://console.altare.gg/apps` in a new in-app browser tab; it redirected to the Altare login page.
- The user must authenticate in the browser. No username, password, passkey, or code was entered or requested in chat. No account or hosting settings were changed.
- The tab is marked for handoff and awaits the user returning after login.
- **Next:** Once the user confirms the console is open, inspect account/products and free-plan limits before any resource creation; continue the repository audit only after the account status is known.#### Altare free-plan/platform audit checkpoint — 2026-11-25
- Authenticated as Taras_Samarin in the Default team; team plan is **Always Free**, usage is 0/4 GiB memory, 0%/125% CPU, 0/40 GiB disk, 0/2 app slots, and no apps exist. The dashboard shows 50 Bolts; none were spent.
- Account → Experiments shows Workstations beta **All 30 seats taken; 2,408 on the waitlist**. No waitlist join was submitted. Official beta docs say early testers receive 2 shared vCPU, 4 GB RAM, 60 GB disk, up to two workstations, free within the included amount, with London as the only region; above that requires billing/card.
- Official Workstations networking docs support up to 10 raw TCP/UDP public ports, externally mapped from 46000–47999 to chosen internal ports on a per-workstation hostname. This would fit UDP 7770 in principle, but this account has no beta seat.
- Official Docker Apps docs explicitly say only TCP traffic reaches an app. GitHub App Hosting is web/API/worker oriented and listens on PORT; it is not a raw UDP server entry point.
- Sandboxes are persistent Linux systems, but not full VMs: no systemd or Docker inside; long-running processes need tmux/nohup. Their extra public ports require Bolts under the documented store/resource pricing. No Bolts may be used, so no public UDP allocation can be attempted.
- Altare Go provides 8 GB RAM, 4 CPUs, 40 GB disk for at most 12 hours, is started through CLI, and public exposure is HTTPS-only. It is not a persistent production server path and no Go machine was started because the task forbids resources of unclear cost.
- Console/status page reports the Nov 21 heavy-demand incident affecting app deployments, sandboxes, Altare Go and Console; dashboard showed 0/49 nodes up/unknown. No resource or app was created, and the app-creation wizard did not finish loading.
- **Decision so far:** No currently available $0 path is verified for the required always-on UDP/DTLS server: Docker/GitHub apps are TCP-only; the only documented free raw-UDP route is Workstations beta, for which this account is waitlisted. Do not create an app, sandbox, workstation or Go machine; do not consume the 50 Bolts.
- **Next:** Inspect actual Unity/API repos, existing Linux build(s), and deployment documentation; preserve all existing changes. Determine whether the existing build is current and what can be validated locally without an Altare deployment.


#### Altare hosting assessment closeout — 2026-11-25
- **Status:** COMPLETED for the requested platform feasibility assessment; deployment NOT STARTED.
- Account is Always Free with 0 usage and no apps; 50 Bolts remain untouched. Workstations beta is full and this account is waitlisted. The only documented free raw UDP path is therefore unavailable to this account. Docker/GitHub Apps are TCP-only; Sandboxes and Altare Go do not meet the persistent zero-cost UDP requirement.
- RAV statically configures FishyUnityTransport for UDP 7770 with encryption enabled. Existing registration advertises the same port used for the internal bind; no separate external mapped-port setting exists.
- The November 22 RAV Linux build predates Unity HEAD on November 24 and is not accepted as a current deployment artifact. No Docker image, headless/runtime, UDP, DTLS, API, or load test was run.
- No Unity code, production service, paid resource, or Bolt was changed or used. Deployment documentation was updated in the API repository.
- **Decision:** NOT SUITABLE for the current Altare account under the strict $0 constraint. Do not deploy. Re-evaluate only after actual Workstations beta access and port mapping compatibility are available.


## 2026-11-25 03:20:50 +08:00 — RAV AlienX Cloud Free Lite assessment
- **Status:** IN_PROGRESS.
- **User request:** Evaluate AlienX Cloud free Lite for RAV Unity Dedicated Server, use only clearly free resources, and prepare/deploy only if public UDP/DTLS and account access are suitable.
- **Scope:** Verify authenticated Cloud access and Lite quotas; audit current official UDP/port mapping, geographic policy, uptime and safety; inspect existing RAV Unity/API source and Linux build; prepare only the minimal deployment path if eligible.
- **Safety:** Strict $0. Never purchase boosts, add payment details, create a paid/unclear-cost resource, accept a paid tier, bypass region restrictions, disable DTLS, expose production/database credentials, alter gameplay without need, or claim 20-slot capacity without load tests. Preserve existing dirty files and journals; no commit/reset/clean.
- **Acceptance:** Confirm actual account/Cloud access, Lite specs, UDP semantics, endpoint stability, Ukraine/Russia connectivity rules, current artifact compatibility and what remains untested. Submit only a clearly free Cloud access request if required. Do not create a VM until access and zero cost are explicitly confirmed.
- **Initial state:** Prior deployment prep remains in progress in existing journal entries; Unity/API have existing uncommitted journal/documentation changes. Preserve them. AlienX account login completed by the owner; panel showed no servers and Cloud beta access request.
- **Start:** 2026-11-25 03:20:50 +08:00
- **Next:** Inspect current repository status and source/build artifacts; update deployment docs with AlienX findings and final decision.


### AlienX browser findings — 2026-11-25 03:21:55 +08:00
### AlienX account/network checkpoint
- The authenticated panel shows no existing server and Cloud access is invitation-only. A single free access request was submitted; panel confirms ticket #93 is waiting for staff. No VM or paid resource was created.
- The panel advertises Lite as free: 1 VM, 1 vCPU, 2 GB RAM, 30 GB disk, 2 projects, 10 WAN ports, 10 Mbit/s; available OS list includes Ubuntu 24.04 LTS.
- Official networking guide says ports may use TCP, UDP, or TCP/UDP; game mapping sends traffic to the same port number on the VM. The public address is shared and the provider may change assigned ports. RAV can set one runtime FishNet port and registers that same port.
- Current terms list Russia among IP-blocked countries; Ukraine is not listed. Do not bypass the block. Cloud is hobby beta on UK residential hardware, without an uptime promise; do not treat it as production/24x7 hosting.
- **Next:** Inspect working-tree status, existing deployment scripts, Linux build freshness, and API/Unity configuration. Do not create a VM until staff grants access and confirms the free tier.


#### AlienX local preparation checkpoint — 2026-11-25
- **Current status:** BLOCKED pending staff approval of AlienX Cloud access request ticket #93. Do not create a VM until free Lite access and zero-cost allocation are confirmed.
- Unity source HEAD is 3a050ee43598d5ff6aea16f17cd243e677043534; no source/gameplay changes were made. Existing build pipeline safely generated a fresh Linux x86-64 Development Dedicated Server build at D:\Programing\UnityProjects\war-of-machines-local-builds\LinuxServer-20261124-192541; Unity reported successful build and exit code 0, with nonfatal warnings.
- Required Unity/Mono files, x86-64 ELF header and Server/Map scene configuration were verified. A separate archive and SHA-256 manifest were created and checked; artifact details are recorded in API DEPLOYMENT_PROGRESS.md.
- Added provider-specific native/systemd runbook deploy/alienx/README.md; reused existing deploy/linux scripts rather than duplicating host tooling.
- Official AlienX documentation supports UDP but uses same-number public-to-VM mapping on a shared address; RAV can bind/register the assigned port through RAV_SERVER_PORT. No source change needed. Panel ticket #93 is still pending; no actual VM, assigned port, API endpoint, external UDP or DTLS test exists.
- Remaining recovery: after staff approval, confirm free allocation, Ubuntu/x86-64, endpoint, SSH fingerprint and UDP port; then use the runbook and test only against isolated staging API credentials. No production service, paid resource or database was touched.

## 2026-11-25 04:02:48 +08:00 — RAV full cloud deployment and multiplayer integration
- **Status:** IN_PROGRESS.
- **User request:** Build, configure and deploy the RAV Unity Dedicated Server to AlienX Cloud Lite, the .NET 10 API and staging PostgreSQL to Altare Always Free, then configure a separate Unity cloud-staging client and run a real end-to-end multiplayer test if account access and free quotas permit.
- **Scope:** Reuse current Unity/API repositories, the existing AlienX release/runbook, API Docker/deployment setup and existing deployment scripts. Configure only an isolated staging environment; preserve local and production configuration; validate the full client-to-server flow where access exists.
- **Safety:** Strict $0; no payments, cards, Bolts, paid services, production data/credentials, public database exposure, weakened HTTPS/JWT/DTLS/certificate checks, or unnecessary gameplay rewrites. Do not commit/reset/revert/clean; preserve prior changes. Follow provider restrictions and record only tests that actually ran.
- **Acceptance:** Record actual host/API/database endpoints and resource allocations, deploy/start components where access permits, keep Unity local config intact, run staged health/auth/ticket/registration/heartbeat/DTLS/gameplay checks, provide exact user launch steps, and mark missing tests/blockers honestly.
- **Initial state:** Unity has existing dirty TASK.md and untracked deploy/alienx documentation from prior work; backend has existing dirty TASK/deployment documents from earlier Altare work. AlienX ticket #93 was last recorded waiting for staff; no VM existed. Altare had no app or database, a $0 storage cap, no saved card, and auto top-up Off. Preserve all these states.
- **Start:** 2026-11-25 04:02:48 +08:00.
- **Next:** Check this task's providers and artifacts from the last recorded state; do not repeat completed broad audits. Inspect latest Unity/API revision and current local runtime availability, then build only what is stale or missing.
### Cloud and Unity artifact checkpoint — 2026-11-25 04:09:17 +08:00
- AlienX panel is still invite-only: Cloud request ticket #93 says “Your request has been sent to staff”; server count is 0. No VM/IP/UDP mapping is available, so remote Unity deployment is blocked.
- Altare still shows 0/2 apps, 0 RAM/CPU/storage use, 0/1 managed databases, and $0 this month; wallet balance is 50 Bolts. No cloud resources were created.
- Unity HEAD remains 3a050ee43598d5ff6aea16f17cd243e677043534, matching the recorded build source. The existing release archive hashes to 80216b46ac3e520bd6100543e9517ce26bba9dc81e4f88eff9414dc20c9bd030 and contains the required ELF executable, data folder and UnityPlayer.so; no rebuild is needed unless local integration detects a config mismatch.
- **Next:** Continue local-only API/DB and client preparation while preserving prior local database data; do not retry ticket #93.
### RAV cloud deployment — local validation checkpoint (2026-11-25)

- Preserved prior dirty state; no commit/reset/clean. Local source edits: explicit RAV_API_BASE is authoritative and fails closed instead of falling back to production; gameplay diagnostics support a configured file path; systemd directs diagnostics to /var/log/rav.
- Fresh Unity Linux x86-64 Development Dedicated Server build succeeded at D:\Programing\UnityProjects\war-of-machines-local-builds\LinuxServer-20261124-203532. Release archive and SHA-256 are recorded in deploy/alienx/README.md.
- Isolated Ubuntu 24.04 container verified headless startup, FishNet UDP 7770 listener, Server scene, API registration/heartbeat, and graceful shutdown/unregister. Compose host binding was restricted to loopback.
- Map scene was included in build configuration but not observed loading at runtime. No real client, DTLS handshake, external UDP, bots, gameplay match, reconnect, or 20-player load test was available.
- AlienX VM access remains gated by ticket #93; Altare API/database cloud resources remain uncreated. No cloud code upload, payment, Bolt use, production access, or data deletion occurred.

## 2026-11-25 04:49 +08:00 — RAV Altare staging and Unity client integration
- **Status:** IN_PROGRESS.
- **Direct user authorization:** Upload the private RAV API Docker image or necessary source to Altare and create free isolated API/PostgreSQL staging resources; no money/Bolts, payment details, production secrets/data, or production changes.
- **Scope:** Configure a separate cloud-staging Unity client profile while preserving local/production settings; connect it to the actual Altare staging API and, if AlienX remains unavailable, the local dedicated server for a real Unity Editor multiplayer test.
- **Preserve:** Existing user changes and journals; no commit/reset/revert/clean. Avoid gameplay changes unless required for compatibility.
- **Acceptance:** Record actual endpoint, changed files and client launch steps; do not claim client/DTLS/gameplay checks unless observed.
- **Start:** 2026-11-25 04:49 +08:00.
- **Next:** Review Unity network/config owners and required docs before any client configuration edit; first confirm actual cloud API endpoint and server availability.

### Altare deployment path checkpoint — 2026-11-25
- GitHub's Altare installation request includes read/write access to commit statuses and pull requests in addition to repository code/metadata. To honor the previously approved read-only scope, no grant was submitted.
- Altare App Hosting does not list .NET as a supported runtime; Docker Apps pull images from an external registry. The local API image is available but has not been uploaded. No cloud API endpoint exists yet, so no Unity cloud-staging configuration or client changes were made.
- **Next:** Resume Unity staging configuration only after an API HTTPS endpoint is available; preserve existing local, production, and working-tree configuration in the meantime.

## 2026-11-25 05:39 +08:00 — RAV cloud API and Unity staging continuation
- **Status:** IN_PROGRESS.
- **User request:** After the private RAV API image is deployed to a free Altare Docker App and connected to `rav-staging`, apply only staging migrations, verify the API contracts, then point the existing local Unity Dedicated Server at the cloud staging API and prepare the Unity Client for a first multiplayer test.
- **Safety:** Keep source/working-tree changes intact; do not install Altare's GitHub App, change production or Unity defaults, expose secrets, spend money/Bolts, or disable HTTPS/DTLS. Use only staging credentials and a separate runtime environment for cloud testing.
- **Acceptance:** Record verified staging HTTPS endpoint, API/server test results, any new client-side files/configuration, and exact Editor launch steps; do not claim client/DTLS/gameplay tests until observed. No Unity cloud configuration changes before the API endpoint and isolated credentials exist.
- **Initial state:** Existing local Linux Dedicated Server build and local-only test path are documented in prior entries. API deployment is currently blocked on private image transfer and Altare app configuration. Preserve the existing dirty changes.
- **Start/next:** 2026-11-25 05:39 +08:00. Review prior entries and repo status; wait for the API endpoint before editing client/server runtime settings.

## 2026-11-25 — Altare cloud staging API continuation checkpoint

- The API team is preparing private GHCR image `ghcr.io/scorpioner2010/rav-api:staging-20261125`; local image security audit and API Release build passed, but GHCR push is waiting for the owner's masked PUSH_TOKEN input.
- Unity API endpoint is not configured yet because the Altare app hostname has not been assigned. Keep `RAV_API_BASE` unset until the exact staging HTTPS host is known; the staging override must never fall back to the legacy production API.
- No Unity source/build/runtime changes in this checkpoint. Local Dedicated Server cloud registration and Unity Client staging login/multiplayer tests remain NOT TESTED.

## 2026-11-25 — Final cloud deployment and client preparation
- **Status:** IN_PROGRESS.
- **User request:** Continue the authorized staging deployment; use only actual staging API/server endpoints, prepare a separate Unity Client profile and launch instructions, and do not launch the user's Unity Client. Preserve local/production settings and existing dirty work.
- **Safety:** No production endpoint/credentials/data, no paid resources or Bolts, no Git reset/clean/commit, no weakened HTTPS/DTLS, no secret in source, no unsupported UDP claims. Keep current AlienX access gate and use a verified free persistent UDP host only.
- **Acceptance:** Client config points at real staging HTTPS/API and actual dedicated-server address/port when those exist; document all changed files and unverified tests. If endpoints are unavailable, leave client unchanged and record the exact blocker.
- **Start:** 2026-11-25 (Asia/Manila).
- **Initial handoff:** Unity Linux server local runtime smoke passed, but actual cloud VM/API endpoint and client-to-server DTLS/gameplay tests were pending. Existing Unity working tree contains deployment docs/journal changes; preserve them.
- **Next:** Check Git status and inspect current provider access before altering Unity settings.

### 2026-11-25 — Final deployment preparation checkpoint
- Current Unity HEAD remains 3a050ee43598d5ff6aea16f17cd243e677043534; existing dirty Unity changes and deployment notes are preserved. No Unity source/config was changed during this checkpoint.
- Copied the verified current Linux Dedicated Server archive to the requested Desktop deployment folder; SHA-256 matches repository runbook.
- Desktop documentation and PowerShell operations scripts now describe the current cloud blockers and are parser-validated.
- Actual cloud API and Dedicated Server endpoints still do not exist; do not edit staging client configuration or launch Unity Client until they are confirmed.
- AlienX ticket #93 remains last known pending; current panel was not checked because the owner is completing an Altare database credential reset.
### User continuation — defer staging DB password rotation — 2026-11-25
- The owner asked to postpone rav-staging password rotation until after staging configuration and testing. Preserve the existing test database and continue staging integration work; no credential reset is authorized or needed now.
- Do not change local/production Unity defaults. Wait for the real HTTPS API endpoint and a real UDP server endpoint before adding a separate client launch profile. Do not launch the user''s Unity Client.
- Next: Use the current Altare API endpoint if it becomes available; otherwise keep client configuration unchanged and continue only safe staging preparation.

### 2026-11-25 — Cloud staging checkpoint after Altare availability issue
- Owner deferred the test DB password rotation until after staging is configured and tested.
- Altare API App is not created and the database schema/backup status has not been verified; do not set a Unity staging API URL yet.
- No Unity Client, networking config or gameplay file changed in this checkpoint. Keep the client unlaunched until the real HTTPS API and public UDP/DTLS endpoint exist.
- Oracle Always Free AMD micro was researched as a low-capacity x86 UDP candidate only; no account/resource was created. Current AlienX ticket #93 status and UDP endpoint still need a live check.

### 2026-11-25 — Altare API app pending
- Altare verified the private RAV API image; a free API App creation request is pending in Stockholm under the provider's heavy-demand incident.
- Keep Unity Client configuration unchanged and do not launch the Unity Client until the HTTPS API hostname and a public UDP/DTLS server endpoint have been verified.
- No cloud app endpoint, API health, auth/ticket, registration/heartbeat, or multiplayer tests are confirmed yet. Database password rotation is deferred by the owner.

### 2026-11-25 — Altare has no available free app port
- The API app could not be created: automatic allocation failed and two Stockholm nodes reported no free ports. No HTTPS API hostname exists.
- No DB migration, cloud API test, Unity staging config, public UDP/DTLS or gameplay test occurred. Keep Unity Client settings unchanged and unlaunched.

### 2026-11-25 — Oracle Cloud ARM64 compatibility preflight
- **Status:** COMPLETED — READY WITH LIMITATIONS.
- **User request:** Inspect the existing Linux x86-64 Dedicated Server build and determine whether Unity 6000.3.13f1, FishNet 4.6.4, Mono, DTLS, and native dependencies support a separate Linux ARM64 Dedicated Server for Oracle Ampere A1; attempt a separate build and actual ARM64 runtime tests where locally possible.
- **Scope:** Read-only inspection plus a new isolated ARM64 build output only if the installed Unity version/modules/toolchain support it. Test API registration/heartbeat only against an already-configured staging endpoint; never allow production fallback.
- **Safety:** Preserve all existing dirty changes and x86-64 artifacts. Do not create Oracle resources/account, use trial credits, upgrade PAYG, pay, alter gameplay or production, or use emulation as a production solution.
- **Acceptance:** Save `ORACLE_ARM64_PREFLIGHT.md` to Desktop/War of Machines Deployment with build/ELF/dependencies/FishNet/DTLS/API/runtime findings, Oracle Always Free/deployment plan and remaining risks. Record tests honestly.
- **Start:** 2026-11-25 (Asia/Manila).
- **Completed:** Unity 6000.3.13f1 produced a separate Linux Dedicated Server ARM64 IL2CPP build at `D:/Programing/UnityProjects/war-of-machines-local-builds/RAV-Oracle-A1-Arm64-20261125`; BuildResult Succeeded, 0 errors, 30 warnings. `RAVServer`, `UnityPlayer.so`, `GameAssembly.so`, and Burst runtime are AArch64. Existing x86-64 artifact and project Mono settings were preserved; temporary build helper was removed.
- **Runtime:** Ran Ubuntu 24.04 ARM64 under local Docker/QEMU emulation, isolated with `--network none` and an ephemeral self-signed test certificate. Headless server startup, Server scene startup, FishNet initialization, UDP 7770 bind, and SIGTERM shutdown all passed; container exit code 0. This was not a native A1 test. `Map.unity` is in the build but was not loaded because no client/match was created. DTLS handshake and API registration/heartbeat remain NOT TESTED; no live staging endpoint exists and production fallback was blocked.
- **Compatibility findings:** Unity 6000.3 supports Ubuntu 24.04 ARM64 server players, but ARM64 is IL2CPP-only; the existing Mono x86-64 build remains separate. Two auxiliary x86-64 ELF libraries were included (`abci.so` from Alembic and `libdecor-cairo.so`/`libdecor-0.so.0`); `abci.so` failed to load while server startup continued. A runtime convex-mesh warning reported a partial hull for `tripo_node_b7d6146a`. Resolve/accept these limits and run native ARM64, real DTLS/client, Map/match, and API tests before relying on A1 for production.
- **Oracle:** The requested 2 OCPU / 12 GB A1 and 50 GB boot volume are within published Always Free compute/storage quotas when created in the tenancy home region. Frankfurt capacity and public IPv4 billing were not verifiable without an account/console; no account or resources were created. Always Free idle-reclamation and capacity risks remain.
- **Next:** Prefer a supported x86-64 Linux VPS for production readiness. If Oracle A1 is still considered, first do a native ARM64 staging deployment and complete the outstanding integration tests; do not describe the emulated run as native validation.

### 2026-11-25 — Oracle Cloud A1 real RAV Dedicated Server deployment
- **Status:** IN_PROGRESS.
- **User request:** Use `Desktop/War of Machines Deployment/ORACLE_ARM64_PREFLIGHT.md` as handoff, do not repeat completed local ARM64 build/emulation tests, and pursue a real zero-cost Oracle Always Free A1 deployment.
- **Scope:** Verify Oracle account/access, A1 European availability and $0 cost for VM, disk, public IPv4 and networking. Create Ubuntu 24.04 ARM64 2 OCPU/12 GB/50 GB only if every resource is explicitly verified free. Deploy the existing ARM64 artifact, configure SSH/firewall/UDP 7770/systemd/restart, and run native stability/integration/load tests where safe.
- **Safety:** No payment details, card authorization/hold, trial credits, PAYG, production API, weakened DTLS/authentication, or code/gameplay change without need. Use staging API only. Preserve existing user changes. Do not repeat preflight local tests.
- **Acceptance:** Real ARM64 VM remains active at zero cost; record real public IP, endpoint/port, resource billing evidence, services, CPU/RAM, 30–60 minute stability, networking/DTLS/API/gameplay/load results and exact untested items. Save deployment documentation under the existing Desktop deployment directory; credentials only in an approved encrypted vault.
- **Start:** 2026-11-25 (Asia/Manila).
- **Initial handoff:** ARM64 IL2CPP build passed local emulated smoke; native A1, Map/match, DTLS handshake, API registration/heartbeat, and Oracle IPv4 cost remain unverified. Altare API app was last recorded unavailable due no free ports. No Oracle account or resources were recorded.
- **Next:** Inspect live Oracle account/session in the browser and verify current free billing before any resource creation.

## RAV Netcup infrastructure deployment — IN_PROGRESS

- Started: 2026-11-25 18:30:05 +08:00 (Asia/Manila).
- Request: Resume previous Netcup preparation and deploy staging infrastructure for RAV: PostgreSQL 17, ASP.NET Core 10 API over HTTPS, Unity Linux x86-64 Dedicated Server on UDP 7770 with DTLS, and configure Unity Client. Search/reuse existing artifacts; do not repeat successful audits/tests.
- Acceptance: actual resources and integration only after provider/account state, exact billed amount, contract term, renewal/cancellation and IPv4 price are verified. Stop before paid order and obtain user's confirmation. No production API/database; no payment or legally binding order without explicit final authorization; no paid add-ons; preserve current work and do not expose secrets.
- Current status: IN_PROGRESS. No repository/source inspection or deployment action performed in this task yet. No cloud resource, order, payment, migration, or client endpoint change made.
- Next: read both project AGENTS.md and task journals; inspect existing Netcup docs/artifacts/status and browser/account session; continue only safe local preparation until purchase gate.
### Netcup discovery checkpoint — 2026-11-25
- Read project instructions and current journals before source edits. Both repositories already contain user changes; Unity working tree includes HttpLink/diagnostics and deployment docs/scripts, API working tree includes staging/Docker/config/migration-related changes. No commit/reset/clean performed.
- Desktop deployment folder contains current generic/Altare/AlienX deployment notes, a packaged x86-64 Unity server archive, scripts, and DPAPI vault. No dedicated Netcup deployment plan/config was found in tracked deploy files or current deployment docs.
- Do not read or expose secret values. A separate `secure-vault/alienx-rav-staging-ed25519` private-key file exists beside `credentials.dpapi.txt`; it is scoped by name to AlienX and will not be reused for Netcup without an explicit technical need. Inspect its ACL before any further key handling.
- Existing state recorded in documents is stale for the newly selected provider: Altare API app was unavailable; no current Netcup VPS/IP/SSH endpoint is confirmed yet.
- Next: verify official Netcup plan/order requirements and inspect the existing in-app browser/account session; preserve the current order until price/term/renewal/IPv4/cancellation are verified. Stop before final purchase and request explicit confirmation.## 2026-11-26 — Netcup staging client/server integration — IN_PROGRESS
- User request: deploy the current RAV Linux x86_64 Unity Dedicated Server with FishNet/DTLS on Netcup, configure the client for the new staging API/server endpoint, and run real external/network/match stability tests.
- Scope: staging only, no production fallback, preserve existing dirty files and gameplay, no commits/resets. OS selection must follow Unity's current Linux Server platform support.
- Acceptance: compatible Linux build deployed, UDP 7770/DTLS preserved, server registration/heartbeat/tickets validated, Unity client staging profile ready, and test gaps reported.
- Current checkpoint: Netcup SSH host fingerprint verified; read-only server check confirms a fresh Debian 13.8 x86_64 instance with no project data. No server artifact deployed. Unity artifact compatibility with current staging host/API allowlist remains to be verified.
- Status: IN_PROGRESS; next inspect current git state, deployment archive manifest, and RAV staging-host configuration.

## 2026-11-26 — Commit readiness review — IN_PROGRESS
- User asks whether the current pending API and Unity changes can be committed manually.
- Scope: inspect working tree and diff contents for intended changes, secrets, generated artifacts, and whitespace issues; do not commit, stage, reset, clean, or edit implementation.
- Acceptance: give a clear commit recommendation, safe scope, exclusions, and validation status.- Review result: branch main has no staged changes; five tracked paths are modified and deploy/alienx/README.md is untracked. High-confidence secret-pattern scan found no matches. The AlienX README is unrelated and stale for the current Netcup staging deployment; keep it out of the Netcup commit. Unity client Play/DTLS/gameplay validation remains pending, so a commit is not a claim of full multiplayer verification.
- Recommendation: the staging override, diagnostics path and Linux deployment documentation are commit candidates; do not include deploy/alienx/README.md in this change. No commit or staging action was performed.
- Status: COMPLETED.
## 2026-11-26 — RAV Unity Editor Development Control Panel — IN_PROGRESS
- User request: implement a compact Unity Editor control panel at RAV/Development Control Panel for Local/Netcup Database, API, Dedicated Server, client Play Mode environment switching, builds, status and logs. Use existing scripts/builders, preserve production settings and unrelated changes, and provide one short usage guide in Desktop deployment docs.
- Scope: Unity Editor tooling plus only the minimum supporting local configuration/helper integration needed for requested controls. No gameplay refactor, production changes, remote deployment, database destructive actions, Git commit/push/reset/clean, or deletion of existing scripts.
- Acceptance: supported combinations and blocked combinations are explicit; selected environment is applied before client Awake/Play Mode without Editor restart or cross-environment cached auth/tickets; local services are owned and stopped only by this panel; Netcup ordinary start is health/status-only; builds reuse LocalNetworkBuild; secrets are not logged or included in client artifacts; save one short usage guide; perform requested focused validation and record limitations.
- Start: 2026-11-26 (Asia/Manila).
- Status: IN_PROGRESS. Next inspect both repositories' current Git state and relevant existing launch/build/health helpers, then implement the smallest safe EditorWindow and profile application path.
- **Discovery checkpoint:** Read global/project instructions and the existing architecture discovery report before implementation. Unity worktree already has dirty `HttpLink.cs`, diagnostics, TMP fallback asset, deployment docs/service, root journal, and untracked `deploy/alienx/`; API worktree already has staging/Docker changes and a local journal. These are pre-existing and must remain intact. Discovery identifies unsafe production fallback for Development, staging's exact `RAV_API_BASE` requirement, a global-name Stop BAT, empty Desktop remote settings/stale systemd unit, and secrets files with broad ACLs. Next: inspect exact existing APIs/scripts and design only profile/runtime application plus owned local processes.
### Completion checkpoint — 2026-11-26 (Asia/Manila)
- Added `Assets/Editor/RAVDevelopmentControlPanel.cs` and its Unity `.meta`; added safe Editor-process API selection in `HttpLink.cs` and reusable timestamped Client/Server build entry points in `LocalNetworkBuild.cs`. The panel includes Local/Netcup presets, supported-combination checks, environment/cache isolation, panel-owned local processes, PostgreSQL/API/server lifecycle actions, staging status, client build/launch, logs, and separately confirmed remote restart actions.
- Added the short usage guide at `Desktop/War of Machines Deployment/RAV_DEVELOPMENT_CONTROL_PANEL.md`; corrected it to distinguish API/heartbeat checks from separate SSH/systemd actions. Netcup public DTLS CA and local `client-transport.env` exist. SSH identity reference is blank and must be configured before remote SSH actions.
- Validation: Unity Editor recompiled the Editor assembly after the final source change (Editor assembly timestamp is newer than the panel source; recent Editor log shows the compile/copy steps and no compiler errors). The Unity Editor menu/window had been observed. `git diff --check` passed; three deployment PowerShell scripts parsed with zero AST errors; deployment settings JSON parsed successfully.
- Not run: no local database/API/server or client was started or built; no Netcup SSH/network calls, remote restarts, migrations, multiplayer/DTLS handshake, or external reachability tests were performed. No service state or database volume was changed.
- Final status: implementation and focused static/Editor validation complete; runtime and remote integration remain unverified. Existing dirty files and unrelated `deploy/alienx/` were preserved. No commit/stage/reset/clean performed.
## 2026-11-26 — RAV Development Control Panel operational verification — IN_PROGRESS
- User request: finish machine-specific SSH/configuration and practically validate existing Full Local and Full Netcup panel flows, including process lifecycle, client build, status/heartbeat, remote logs, profile switching, and primary buttons. Keep Hybrid blocked.
- Scope: configure the existing control panel using the existing SSH key/vault and deployment settings; run only requested staging/local checks and fix root causes if found. No new features, service reinstall, DB reset/data deletion, production changes, load tests, or Git commit/push.
- Acceptance: all components and paths/URLs/ports/CA are preconfigured; Full Local start/check/build/launch/stop/restart-without-rebuild works; Full Netcup API/heartbeat/SSH/logs/profile application works; Local→Netcup→Local switches without Editor restart; primary buttons are exercised; report actual limitations.
- Start: 2026-11-26 (Asia/Manila).
- Initial state: previous panel implementation is compiled, but no local service/client runtime or Netcup SSH check was performed. Desktop deployment settings contain the current API/server endpoint and blank SSH key reference; a local secure-vault and existing SSH key are recorded in prior task notes. Preserve all dirty changes and do not print secret material.
- Next: identify the existing Netcup-capable SSH key/secure-vault path without opening secret values, configure the panel's machine-local SSH reference and other defaults, then exercise the already-open Unity Editor panel and record each result.- **Discovery:** The Editor panel is open with Full Local selected. Local PostgreSQL status was Ready, API Failed, server Stopped. Editor recent activity shows access denied on `LocalSecurity\local-security.env`; its protected DACL had zero ACEs. The existing Temp Netcup key's derived public key matches the public key stored in the DPAPI vault; SSH agent is stopped. The known-host fingerprint calculation was rerun with OpenSSH's standard Base64 format before trusting it.
- **Decision:** Restore only the LocalSecurity directory/file ACLs to current user, SYSTEM, and Administrators. Reuse the existing Netcup key file after matching it to the vault and restricting its ACL; do not generate another key.- **SSH trust result:** Direct `ravadmin` login was correctly blocked because default `known_hosts` held the pre-reinstall key. The new key is already confirmed by SCP and is in the dedicated `rav-netcup-known-hosts-verified` cache; after replacing only the stale `62.83.120.40` entry, key-only SSH succeeded and confirmed Ubuntu 24.04.5, x86_64, 4 vCPU, about 7.8 GiB RAM and 157 GiB root filesystem.
- **Configuration blocker:** The Unity floating panel is outside the original Editor window bounds, so computer-use input could not reach its text fields. Clicks did not modify settings. The single needed key-path preference will be written from the existing machine-local deployment settings and loaded by restarting Unity once; the requested environment switching will still be tested within one Editor session after that setup restart.
- The Netcup key was moved (not regenerated) from its existing Temp copy to the user's persistent .ssh directory with restricted ACL; the DPAPI vault copy remains untouched. The Desktop deployment settings and Unity EditorPrefs now reference the persistent key path. Unity was closed cleanly (no scene-save prompt) to load this one-time machine setting before runtime checks.
- **Runtime findings (2026-11-26):** Reused the existing Ed25519 key/vault and verified strict key-only SSH to Netcup. `https://api.staging.62-83-120-40.sslip.io/health/live` and `/health/ready` return 200. API status reports one online server, UDP 7770, and a fresh heartbeat; `rav-staging-dedicated-server.service` is active, and remote `ss` confirms `RAVServer.x86_64` bound to `0.0.0.0:7770`. The configured public DTLS CA parses and is currently valid.
- **Local builds/data:** `Build Selected` completed API build and created timestamped Windows server output `D:\Programing\UnityProjects\war-of-machines-local-builds\Server-20261125-230306-410-d9dd93fc`; `Build Client` produced `C:\Users\TorasPC\Desktop\RAV Builds\Client-20261125-230522-477-94c055c0\RAVClient.exe`. Docker Desktop and PostgreSQL started; PostgreSQL is healthy on loopback only. API started with the existing local DB; logs confirm EF migrations were already current. No database was cleared.
- **Root-cause fixes:** The Unity panel's `HttpClient` routed loopback API probes through the configured system proxy, causing false readiness failures. `CreateApiHttpClient` now disables proxy use only for loopback URLs; TLS validation remains enabled and remote URLs keep normal proxy behavior. Unity recompiled cleanly; the refreshed panel reports local DB/API Ready. The remote server log helper previously ran `journalctl` without privilege and returned no entries; it now uses `sudo -n journalctl`, and an actual Netcup log read passed.
- **Pending action:** Full Local subsequently reached a Windows Defender Firewall prompt for `ravserver.exe`. Per Computer-use restrictions I did not change Windows security settings; asked the owner to allow Private networks only (or Cancel if that choice is unavailable). Local server process and UDP 7770 startup are not yet verified. Netcup services were not restarted.
- **Verification update (2026-11-26):** Rechecked local health/live and health/ready (HTTP 200), PostgreSQL container healthy on loopback `127.0.0.1:5433`, standalone client build present, and local server process bound to UDP `0.0.0.0:7770`. The local API reports the server online on port 7770 with a fresh heartbeat. Netcup read-only checks remain passed: API health, SSH key-only access, active server/UDP listener, and remote logs. `git diff --check` passed for the Unity worktree (only existing LF/CRLF warnings); the API checkout path was not found under `D:\Programing\UnityProjects`, so no API diff check is claimed.
- **Still pending:** The Unity panel operation remains `Start Selected` and its displayed server status is stale (`Stopped`) while local process/API checks show the server online. A Windows Firewall prompt appeared for the local server executable. Per computer-use restrictions, no security setting was changed. Standalone client launch, panel stop/restart-without-build, Full Local → Full Netcup → Full Local switching in the same Editor session, and panel button verification are not yet complete.
### 2026-11-26 — Recovery after user resolved Firewall prompt
- User confirmed the Windows Firewall step is complete. Resume the existing `RAV Development Control Panel operational verification` entry; no new feature or architecture work.
- Current objective: diagnose why the panel's `Start Selected` stays busy and reports local Dedicated Server `Stopped` although the local process owns UDP 7770 and the local API reports a fresh online heartbeat. Then validate stop/restart, client launch, profile switching, and primary buttons without restarting Unity or touching Netcup services.
- Do not rerun confirmed API/DB/Netcup audits. Start with current repository/UI/process state and inspect only the relevant panel coroutine/status/process ownership paths. No commit/push.

### Continuation checkpoint — 2026-11-26 (Asia/Manila)
- Resume point: Unity Editor remained open; prior Start Selected run was still in progress after recompiling the latest panel changes. Earlier panel run had completed with Local DB/API/heartbeat Ready without rebuild; standalone Client launch and Local/Netcup preset switching had also succeeded.
- Root-cause fixes now in source: operation pumping is deferred until after GUI layout; run log uses one joined label to avoid variable IMGUI control counts; local PostgreSQL status uses `pg_isready`; Stop Local refreshes statuses before completing.
- Latest observed validation: after recompile, Refresh Status showed Database Stopped / API Failed / Dedicated Server Stopped when Local services were stopped. A second Start Selected started the existing local PostgreSQL container and was still progressing at handoff; no rebuild was requested, no volume was removed, and no Netcup service was changed.
- Next: finish the active Start Selected operation; verify automatic stop-state refresh and restart; finish on Full Local with services ready; record final outcomes and remaining Play-mode limitations. Preserve all dirty files; no commit/push.

### Final verification — 2026-11-26 (Asia/Manila)
- Fixed the remaining status inaccuracies: a short grace period avoids marking a newly started Unity Server as exited while its process metadata settles; local API status distinguishes a stopped listener from an unhealthy API. The existing asynchronous operation-pump/IMGUI corrections remain in place.
- After recompiling the Editor assembly, Refresh Status correctly reported Database/API/Server Stopped after Stop Local. A fresh Start Selected completed without rebuild: local PostgreSQL became healthy, HTTPS `/health/live` and `/health/ready` returned 200, and the API reported the local Dedicated Server online with a fresh heartbeat on UDP 7770. Panel statuses finished Ready; no database volume was deleted.
- Stop Local, no-rebuild restart, Full Local → Full Netcup → Full Local in one Editor session, and DB/API/Server Check buttons passed. Launch Client had already passed earlier in this session; client build was not repeated. Netcup remained read-only and unchanged; Hybrid remains unavailable. `git diff --check` passed (only existing LF/CRLF notices).
- Editor is left on Full Local with PostgreSQL, API, and Dedicated Server running, ready for ordinary Play Mode. A real user login/match in Play Mode was not performed; gameplay/auth flow and DTLS handshake remain unverified here. No load test, commit, or push was performed.
- The Desktop `Start War of Machines.bat` was inspected but not run: its companion Stop script uses image-wide `taskkill /IM` for `RAVServer.exe`, `RavApi.exe`, and `RAVClient.exe`, which could terminate unrelated or currently managed sessions. The panel workflow was verified instead; make the batch Stop script PID/path-scoped before claiming that legacy launcher is safe and verified.

## 2026-11-26 — Development Control Panel live operation status and log — IN_PROGRESS
- User request: add a compact `Live Operation Status & Log` section to the existing RAV EditorWindow. Show real operation/phase, completed stage count, elapsed time, honest progress, status, color-coded timestamped live log, auto-scroll, clear/copy/open full log, adjustable height, and persistence across window close/reopen. Cover existing lifecycle/build/health/SSH/profile operations with truthful skip/wait messages and no secrets.
- Scope: only the existing Unity Editor control panel implementation plus this journal. Preserve previous dirty work; no gameplay/API/DB/Netcup changes, no commits/push, and no long rebuild just to preview UI.
- Acceptance: operation progress is driven by actual stages (unknown build percentage stays indeterminate); background messages are marshalled safely to the UI; compilation succeeds and the visible panel demonstrates retained colored live log/status controls.
- Start: 2026-11-26 (Asia/Manila).
- Status: IN_PROGRESS. Next inspect the existing panel implementation and Unity compile/runtime state, then extend its current logging/progress primitives rather than introducing a separate architecture.

### Completion checkpoint — 2026-11-26 (Asia/Manila)
- Added the bottom `Live Operation Status & Log` section to the existing RAV EditorWindow: operation name/status, active phase, real completed-stage count/progress, elapsed time, spinner while busy, color-coded timestamped entries, auto-scroll preference, adjustable height, and Clear/Copy/Open Full Log controls.
- Routed background messages through a concurrent queue to the Editor update loop; retained the last 180 readable entries in `%LOCALAPPDATA%\RAV\DevelopmentControlPanel\Logs\operations.log` and detailed sanitized command output in `operations-full.log`. Recent entries survived script/domain reload and closing/reopening the EditorWindow.
- Added real stage/wait/skip/success/error messages across existing build, local service, health, registration/heartbeat, Netcup, SSH, and environment-switch operations. Unity BuildPipeline shows the active build name with an indeterminate progress bar instead of a fabricated percentage.
- Fixed two UI issues found during verification: color markup was being displayed literally (now each message uses a colorized GUI label), and accessing `EditorStyles` in `OnEnable` caused `NullReferenceException` during domain initialization (styles are now created lazily in `OnGUI`). Closing the window clears only its stale repaint reference; persisted logs and active operation state remain intact.
- Validation: Unity 6000.3.13f1 Editor assembly was newer than the changed source; Editor.log showed no C# compile errors after the latest refresh and no new panel `NullReferenceException`. In the visible panel, Refresh Status completed with PostgreSQL, API, and Dedicated Server `Ready`, and server registration/heartbeat verified. Closing/reopening the panel retained logs and settings. `git diff --check` passed; only existing line-ending notices remain.
- No component/player build was run, no service state was changed by the verification refresh, and no commit/push was performed. Existing unrelated dirty changes were preserved.
- Status: COMPLETED.

## 2026-11-26 — Move Live Operation Log to right column — IN_PROGRESS
- User request: move the Live Operation Status & Log area to the right side because part of the messages is currently hidden.
- Scope: adjust only the existing EditorWindow layout and any minimal sizing/wrapping needed for the live log. Preserve current logging behavior, progress tracking, settings, services, and unrelated working-tree changes.
- Acceptance: the operation status/log is visibly placed on the right side with readable wrapped messages and usable scrolling; Unity Editor recompiles; no long player/component build, commit, or push.
- Start: 2026-11-26 (Asia/Manila).
- Status: IN_PROGRESS. Next inspect the panel's IMGUI layout and current window dimensions, implement a right-side log column, then verify visually in the already-open Unity Editor.
- Discovery/implementation checkpoint: the log's auto-scroll was measuring wraps with the entire EditorWindow width, and the 190 px saved height exposed only a few rows. Split the EditorWindow into a controls column and a right-side log column, raised the minimum window width for legibility, and migrated the default log pane to 420 px while preserving user-set larger heights. Auto-scroll now measures the right column and accounts for label margins.

### Completion checkpoint — 2026-11-26 (Asia/Manila)
- Moved `Live Operation Status & Log` into a dedicated right-side column; all existing controls remain in the left column. Increased the window minimum width so the two columns and action labels stay readable.
- Expanded the log pane to 420 px by default and retained its height slider up to 720 px. Auto-scroll now estimates wrapped rows using the right-column width and includes each GUI label's vertical margins, so the last messages remain in view.
- Visual check confirmed the operation/log panel on the right with retained colored messages and all three service statuses Ready. Unity Editor assembly timestamp is newer than the updated panel source; Editor.log has no C# compiler errors after the refresh. `git diff --check` passed.
- The final scroll-margin-only adjustment compiled after the visual check. A follow-up screenshot could not be captured because Computer Use began targeting another app; no further UI input was sent. The layout itself had already been visibly confirmed before this final measurement adjustment.
- No builds, service restarts, or commits/pushes were performed. Pre-existing dirty files were preserved.
- Status: COMPLETED.

## 2026-11-26 — Green running-state buttons — IN_PROGRESS
- User request: make the existing Development Control Panel simply highlight buttons green when their corresponding service is running/ready, so current service state is immediately visible. Do not launch the Client.
- Scope: minimal presentation-only change to the existing EditorWindow and this journal. Use actual refreshed/verified component status, preserve all existing project changes and service state; no architecture refactor, builds, service starts/stops, client launch, commit, or push.
- Acceptance: green indicates the relevant service is confirmed Ready; inactive/not-ready buttons retain their normal appearance; `Start Selected` is green only when all selected components are Ready; Client is not launched.
- Start: 2026-11-26 (Asia/Manila).
- Status: IN_PROGRESS. Next inspect existing status/button drawing code, make the smallest suitable UI-only change, and perform focused source/compile validation if Unity has already refreshed the Editor assembly.
- Discovery: `ComponentStatus.Ready` is populated by actual PostgreSQL/API readiness and server registration/fresh heartbeat checks. Environment changes currently retain the previous cached statuses, so button tint must not reuse those after switching profiles.
- Decision: green-tint only the active, fully Ready `Full Local`/`Full Netcup` preset, `Start Selected` when all selected components are Ready, and Local Component Start buttons when their selected local component is Ready. On preset/component selection changes, invalidate the displayed cache and trigger the existing read-only status refresh. Leave Build/Client/stop buttons unchanged; do not launch Client.
### Completion checkpoint — 2026-11-26 (Asia/Manila)
- Added restrained green tint to `Full Local` / `Full Netcup` only when that exact profile is selected and every corresponding service check is Ready. `Start Selected` is green only while all selected components are Ready. Local DB/API/Dedicated Server Start buttons are green only when their corresponding selected local service is Ready. Build, stop, and Client buttons retain their normal appearance.
- On profile/location changes, cached states are reset to Unknown and the existing read-only status refresh is triggered, preventing stale green indicators from carrying across environments. No client was launched and no service state was intentionally changed.
- Validation: Unity Editor recompiled `Assembly-CSharp-Editor.dll` after the source change (DLL modified 20:09:48, source modified 20:07:56); Editor.log contains zero C# compiler errors. `git diff --check` passed; only existing LF/CRLF warnings were reported.
- Visual screenshot verification was not possible: Computer Use timed out twice while activating the Unity window, after the required single retry; no further UI actions were attempted. The compilation confirms the Editor script is accepted, but the actual button colors were not visually inspected.
- No player/server build, Client launch, service start/stop, commit, or push was performed. Existing unrelated working-tree changes were preserved.
- Status: COMPLETED.

## 2026-11-26 — Reset local RAV database to a fresh start — IN_PROGRESS
- User request: completely clear the local RAV database so development starts from zero, comparable to a fresh production deployment.
- Scope: identify the exact local RAV PostgreSQL database and rebuild only that database to the current schema with required system/game catalog seeds. Remove existing local player/user/game data. Do not touch production, remote Netcup, other databases/containers, deployment credentials, or project source unless the existing fresh-install process requires it.
- Acceptance: the active local RAV database is newly initialized from current EF Core migrations and required non-user seed data; no old user profiles/progress/history remain; API uses this same local database; result is verified without launching the Unity Client.
- Start: 2026-11-26 (Asia/Manila).
- Status: IN_PROGRESS. Next inspect local compose/API settings and seed behavior, verify the exact database/container target before any destructive SQL, then reset and reinitialize only that database.
- Discovery: the local PostgreSQL Compose project is `war-of-machines-local`, loopback-only on 5433, backed by `war-of-machines-local_war_of_machines_postgres_data`; the current RAV API Compose file uses the same exact project and named-volume identity. Its container is stopped and no local API/server process or listener is running. Other `rav-local-*` containers are unrelated staging artifacts and will not be touched.
- Fresh-start requirement: Development currently auto-runs migrations and catalog seed but also enables the demo account/match fixture. The fixture flag must be disabled so the empty database remains production-like on API startup.
- Implemented the local API Development fixture-seed default as false, preserving migrations/catalog seed so an API restart will not recreate a demo account or demo match.
- Next start only the verified `war-of-machines-local` Postgres service. Recreate its database in place (no volume deletion), then let the local API apply current migrations/catalog seed. Do not start the Unity Dedicated Server or Client.
### Completion checkpoint — 2026-11-26 (Asia/Manila)
- Cleared and reinitialized only the local RAV database `war_of_machines_local` on the loopback-only PostgreSQL service; kept its named Docker volume and all unrelated containers/databases intact.
- Current API applied 9 migrations and the required catalog seed. Vehicles/factions/maps/research references remain; all player, match, ownership, session, ticket, and server-registration tables are empty.
- Local API is running and both HTTPS liveness/readiness endpoints return 200. Development fixture seeding was disabled so a restart will not recreate the demo user or match. No Netcup/production change, Unity Server startup, or Client launch.
- Status: COMPLETED.

## 2026-11-26 — Simplify RAV Development Control Panel — IN_PROGRESS
- User request: simplify the existing Unity Editor panel around the Netcup shared PostgreSQL, Local/Netcup modes, API and Dedicated Server local/remote actions, combined Build/Deploy/Start/Stop operations, Client actions, compact status and existing Live Log.
- Scope: reuse the current panel, Builder, SSH/vault references, existing deployment scripts and rollback. Local API must use a secure SSH tunnel to Netcup PostgreSQL with TLS verification and migrations/fixtures disabled. No production fallback, public PostgreSQL, new DB, data clearing, unnecessary remote restart, client launch, commit, or push.
- Acceptance: compact English UI matching the provided reference; two modes only; correct mode switching without Editor restart; real status/log stages; build/deploy/start ordering and API readiness gate; Netcup mutation confirmation and existing rollback; focused compile/status/tunnel checks and `git diff --check`.
- Start: 2026-11-26 (Asia/Manila).
- Status: IN_PROGRESS. Next inspect current dirty state, panel controls/process routines, deployment helpers and the existing secure-vault/SSH tunnel configuration; preserve all existing changes.
### Completion checkpoint — 2026-11-26 (Asia/Manila)
- Removed duplicate Local API tunnel state/property, obsolete per-component environment aliases, and an unused PowerShell quoting helper. Kept the legacy preference read for one-time environment continuity.
- Fixed Unity main-thread access from the PostgreSQL background check; made API builds use isolated output directories; added strict ownership/tunnel checks before Local Dedicated Server start; isolated inherited server/API environment values; moved server archive hashing off the UI thread; and made stage progress expand when operations run more stages than estimated.
- Validation: Unity Editor assembly recompiled after the changes; API build passed in an isolated output folder (0 warnings/errors); Compose validation passed and PostgreSQL has no published host ports; Netcup HTTPS liveness/readiness returned 200; panel PostgreSQL SSH health returned Ready; `git diff --check` passed (line-ending notices only).
- Blocked runtime checks: an unmanaged `RavApi.exe` (PID 18232) owns localhost ports 7216/5220 and does not answer HTTPS readiness. It was left untouched. Local API/server start, Stop All, live Local/Netcup switching, and deployment actions were not exercised; Client was not launched.
- Status: PARTIALLY COMPLETED — safe code/config work is in place; Local runtime verification is blocked by the unowned API process.

## 2026-11-26 — Complete Local/Netcup control panel runtime validation — IN_PROGRESS
- User authorizes stopping PID 18232 only after confirming it is the old project `RavApi.exe` occupying localhost 7216/5220; user also authorizes project process/service start/stop and fixes. Preserve all uncommitted work; never touch PostgreSQL data, production, or Git history.
- Scope: verify/stop only that confirmed Local API process; start Local API through the panel using the verified Netcup PostgreSQL SSH tunnel; start Local Dedicated Server; verify status/heartbeat, Stop All, restart without rebuild, and Local/Netcup switching; remove only proven-dead panel code.
- Acceptance: practical component checks pass, code remains minimal/stable, existing API/Dedicated Server buttons function; no client launch, commit, or push.
- Prior checkpoint: panel and API code changes compile; isolated Local API build artifact exists; Compose validation and Netcup HTTPS health passed; panel PostgreSQL SSH health reported Ready. Local runtime was blocked by the unowned process; verify current PID/path/ports anew before stopping.
- Next: inspect current working trees and latest project journals/instructions, verify exact process identity and listeners, then continue from the existing isolated build (avoid a rebuild unless the artifact is missing/stale).
- Status: IN_PROGRESS.
- Verification checkpoint: PID 18232 is `RavApi.exe` at `D:\Programing\ServerProjects\rav-api\RavApi\bin\Debug\net10.0\RavApi.exe`, owned by the current Windows user, and is the listener for both 7216 and 5220. User explicitly authorized stopping this exact old API process. Next stop only PID 18232, confirm ports free, and launch the panel-managed API.
- Stopped only the verified old `RavApi.exe` PID 18232; confirmed listeners 7216/5220 are free. No database or other process was touched.
- Updated the panel to recover the latest isolated API build artifact automatically when no prepared-build preference exists. This lets Start reuse the previously verified DLL without a rebuild.
- Next: Unity recompile/refresh, start the Local API through the panel, then start and verify the Local Dedicated Server.

## 2026-11-26 — Resume Local/Netcup control-panel runtime validation — IN_PROGRESS
- User confirmed the Windows Firewall prompt was dismissed with Cancel and authorized completing Stop All, restart without rebuild, and Local/Netcup switching; leave panel in Local with API and Dedicated Server running. No Client launch, commit, or push.
- Resume from the existing verified Local state. Do not repeat successful health checks/builds; use the panel, verify stopped state, restart with Build disabled, switch Local → Netcup → Local, then leave Local API and Dedicated Server Ready.
- Start: 2026-11-26 (Asia/Manila). Status: IN_PROGRESS.
### Completion checkpoint — 2026-11-26 (Asia/Manila)
- Completed panel-driven Stop All. The operation reported the Local API and Dedicated Server stop sequence completed; it also stopped only the loopback SSH tunnel helper and explicitly left Netcup PostgreSQL running. API status refreshed to Stopped; no data/service changes were made remotely.
- Re-ran Execute All in Local with Build unchecked and Start checked. Live Log explicitly said the API build was skipped and the existing build was used. The protected loopback PostgreSQL tunnel restarted, API reached Ready, and the Windows Dedicated Server reached Ready with registration/heartbeat verification. No Client launch or rebuild occurred.
- Switched Local → Netcup → Local. Netcup refresh reported PostgreSQL, API, and Dedicated Server Ready; returning to Local applied the Local API/DTLS settings and the fresh panel status showed PostgreSQL, Local API, and Local Dedicated Server Ready. Unity Editor was not restarted.
- Final state: Local selected; API Ready; Dedicated Server Ready; PostgreSQL Netcup Ready; Build checkbox off; Client not launched. No implementation code changed in this continuation. No commit or push.
- Status: COMPLETED.

## 2026-11-26 — Clearer registration validation and server errors — IN_PROGRESS
- User request: set minimum login and password lengths to 4 characters, and make server responses explain the reason for failures in the Unity client instead of showing raw/unhelpful responses.
- Scope: trace registration DTO validation and Unity registration response handling; preserve existing auth/security behavior except the requested length floor; return/display safe, actionable validation messages. Do not expose secrets or change unrelated flows.
- Acceptance: login and password each accept 4+ characters and reject shorter values with a clear reason; API returns explanatory validation errors; Unity UI displays a human-readable cause for failed registration/login responses. Run focused API/client compilation or tests available without launching a multiplayer client.
- Start: 2026-11-26 (Asia/Manila). Status: IN_PROGRESS.
- Implementation checkpoint: API `RegisterRequest` and `LoginRequest` now require Login length 4–32 and Password length 4–128, with explicit validation messages; applied to canonical AuthController and staging API snapshot while preserving existing unrelated starter-data edits in the snapshot.
- Unity `RegisterManager` now converts ASP.NET validation `errors`, ProblemDetails `detail/message/title`, plain-text auth failures, and empty/network responses into concise human-readable messages instead of forwarding raw JSON or blank text to the popup.
- Changed files: `Assets/Game/Scripts/API/Endpoints/RegisterManager.cs`; `RavApi/Controllers/AuthController.cs`; `deploy/staging/api/RavApi/Controllers/AuthController.cs`. No schema, database, service, or gameplay changes.
- Next: inspect the focused diff, compile the API into an isolated output and confirm Unity Editor recompilation, then record results and remaining deployment limitation.
### Completion checkpoint — 2026-11-26 (Asia/Manila)
- Registration and login now enforce Login 4–32 characters and Password 4–128 characters; API validation text names the field and accepted range. The canonical API controller and staging source snapshot are aligned for validation; unrelated starter balances in the staging snapshot were preserved.
- Unity auth handling now extracts ASP.NET validation messages and ProblemDetails details, keeps plain server messages readable, and supplies concise HTTP/network explanations when the response has no usable body. Raw validation JSON is no longer passed to the auth popup.
- Validation: canonical API `dotnet build --no-restore` succeeded with 0 warnings/errors; `git diff --check` passed in both repositories with only existing line-ending notices. Unity Editor compilation and live registration were not verified: the Editor log predates the client edit and the active Windows view is a Google identity check, which was left untouched. No API deployment/restart or database write was performed; the running service needs the updated API build to use the new server-side minimum.
- Status: COMPLETED (source implementation complete; Unity compile/live activation remain unverified).

### Resume checkpoint — registration still fails to connect — IN_PROGRESS
- User reports the Unity Play Mode popup now says “Could not connect to the server. Check your connection and try again.” Screenshot shows Local mode, API/Server statuses Ready, and Play Mode prevents environment/service changes.
- Scope: diagnose the active Unity API URL/TLS/network path and distinguish a stale status indicator from an API transport/endpoint error. Do not submit registration, create accounts, change credentials, or stop/restart services unless a concrete safe fix requires it.
- Start: 2026-11-26 (Asia/Manila). Next inspect the configured API address without secrets, current listeners/health endpoints, and recent Unity/API logs; then fix the smallest root cause.
- Diagnostic finding: current Unity `Editor.log` records libcurl `Curl error 28` after exactly 2000 ms. `RegisterManager` configured a 2-second local auth timeout. Local API HTTPS `/health/live` and `/health/ready` return 200 (108 ms / 330 ms); a deliberately invalid registration is rejected with HTTP 400 in 21 ms, and a nonexistent-user login returns HTTP 401 in 676 ms without creating an account. The failure therefore occurs only on slower real registration work (player plus starter-data database calls through the remote PostgreSQL tunnel), not at API reachability or TLS health.
- Decision: raise only the local authentication request timeout to 20 seconds and map UnityWebRequest's timeout/certificate errors to clear popup messages. The local API is still running an older build, so rebuild/restart only the Local API through the existing panel after exiting Play Mode; do not touch Netcup services or create a user.

### 2026-11-26 follow-up — registration connection timeout
- The current Unity screenshot shows a generic connection failure while the Development Control Panel reports Local API and Dedicated Server Ready.
- Existing diagnostics: local `/health/live` and `/health/ready` returned HTTPS 200 (108 ms / 330 ms); a deliberately invalid short registration payload returned HTTP 400 in 21 ms; a nonexistent-user login returned HTTP 401 in 676 ms. No account was created and no database write was made.
- Unity `Editor.log` shows `Curl error 28` at exactly 2,000 ms during registration. This matches the old local request timeout and explains why the API's slower registration transaction over the protected Netcup PostgreSQL tunnel was cut off before a response.
- `RegisterManager.cs` now sets the Local request timeout to 20 seconds and maps timeout, TLS/certificate, and DNS failures to clearer messages. Existing source changes and unrelated user changes were preserved.
- The API source already enforces 4–128 character passwords and 4–32 character logins with explanatory validation messages. The currently running API predates that build and needs the Local API Build/Start operation to activate it.
- Remaining blocker: Unity is in Play Mode and its panel intentionally disables service changes. Computer-use could list Unity but could not attach to its native window (`Accessibility state unavailable`), so Play Mode was not stopped and the API was not restarted. No Netcup service was changed.
- Next: exit Play Mode, let Unity compile the script edit, then use the panel's Build API and Start API actions. Verify health plus only an invalid registration request (HTTP 400); do not create an account. Leave Local API running.
- Status: IN PROGRESS; no commit/push.
### 2026-11-26 follow-up — local registration path restored
- User exited Play Mode. Used the Unity Development Control Panel in Local mode to Build API and Start API; both operations completed. API is Ready and the panel-owned loopback-only PostgreSQL SSH tunnel was confirmed listening on port 15432.
- Active process is the newly built isolated API assembly under `%LOCALAPPDATA%\RAV\DevelopmentControlPanel\Logs\LocalApiBuilds\api-20261126T143121313\RavApi.dll`.
- Strict-TLS health checks returned `/health/live` HTTP 200 (95 ms) and `/health/ready` HTTP 200 (287 ms).
- A deliberately invalid short registration request returned HTTP 400 with the expected messages: login 4–32 characters; password 4–128 characters. No valid registration was submitted and no account was created.
- Unity Editor.log had no C# compiler errors in the recent tail. The client code now allows 20 seconds for Local auth requests and gives specific timeout/TLS/DNS explanations.
- Registration itself remains for the user to try in Play Mode; the short-input validation and healthy API path are verified. No Netcup service/database was changed. No commit/push.
- Status: local registration fix applied; end-to-end account creation intentionally NOT TESTED.