# RAV brand and compatibility map

## Canonical identity

- **Name:** RAV
- **Full name:** Robotic Assault Vehicles
- **Tagline:** Tactical Multiplayer Combat

## Unity names

- PlayerSettings product name is `RAV`.
- First-party C# namespaces now use `RAV.*`; the two first-party assemblies are `RAV.Gameplay.Shared` and `RAV.PlayMode.Tests`.
- Unity's default `Assembly-CSharp` identity remains unchanged. Serialized Unity identifiers were updated to the new namespaces while script `.meta` GUIDs were preserved.
- Windows build code emits `RAVServer.exe` and `RAVClient.exe`; Linux build code targets `RAVServer.x86_64` in a new timestamped folder.
- The final manual Unity Editor Linux Server Build Profile build succeeded on 2026-11-22 from 19:55:57 to 19:56:46 (49 seconds); `Editor.log` reports `Build Finished, Result: Success.` and shows `Server.unity` and `Map.unity` loaded. The build occurred after the RAV source/namespace edits and compiled `Assembly-CSharp.dll` plus `RAV.Gameplay.Shared.dll`.

## RAV-first runtime settings

Unity prefers `RAV_API_BASE`, `RAV_SERVER_ID`, `RAV_SERVER_CREDENTIAL`, `RAV_SERVER_REGION`, `RAV_SERVER_ADDRESS`, `RAV_SERVER_PORT`, and `RAV_UTP_*`. Existing `WOM_*` values remain fallback aliases while old local or server environments are migrated. DTLS remains enabled and missing server secrets still fail closed.

## Intentional legacy identifiers in active Unity files

| File | Remaining identifier | Why it stays |
| --- | --- | --- |
| `Assets/Resources/production-api-hosts.txt`; `Assets/Game/Scripts/API/HttpLink.cs` | `war-of-machines-api.onrender.com` | Existing external production HTTPS endpoint; changing it would break current clients and requires a separately coordinated endpoint migration. |
| `Assets/Game/Scripts/API/HttpLink.cs` | `WOM_API_BASE` | RAV-first fallback for existing launchers and server environments. |
| `Assets/Game/Scripts/Networking/Authentication/ConnectionTicketAuthenticator.cs`; `Assets/Game/Scripts/API/Endpoints/MatchesManager.cs` | `WOM_SERVER_ID`, `WOM_SERVER_CREDENTIAL` | Compatibility for existing dedicated-server registrations and credentials. |
| `Assets/Game/Scripts/UI/Helpers/StartServerButtons.cs` | `WOM_SERVER_*` | RAV-first fallback for existing local launcher/server settings. |
| `Assets/Game/Scripts/Networking/Authentication/SecureFishNetTransportConfiguration.cs` | `WOM_UTP_*` | RAV-first fallback for existing DTLS certificate, key, common-name, and CA settings; security remains fail-closed. |
| `deploy/linux/scripts/health-check.sh` | `WOM_SYSTEMD_SERVICE`, `WOM_SERVER_PORT`, `WOM_API_HEALTH_URL` | Allows an existing installed service to keep using its prior environment file during migration. |
| `Assets/Game/Scripts/Diagnostics/DiagnosticsConfig.cs` | `WarOfMachines.Diagnostics.*` | Preserves stored Unity EditorPrefs switches. |
| `Assets/Editor/Game/ArmorPrefabHighlighter.cs` | `WOM.ArmorPrefabHighlighter.Enabled` | Preserves the user's editor preference. |
| `Assets/Editor/LocalNetworkBuild.cs` | `war-of-machines-local-builds` | Existing build artifacts and local security files are stored there; new builds use unique subdirectories and do not overwrite prior output. |
| Desktop `Start War of Machines.bat` and `Stop War of Machines.bat` | Legacy shortcut filenames | Existing desktop shortcuts remain valid; their contents target the RAV project and executables. |
| `docs/codex-game/task-history.md`; `docs/codex-game/task-handoff.md`; `docs/codex-game/performance-audit-report.md` | Historical War of Machines names and `Game.*` test identifiers | These are append-only historical records of earlier project state and test results. Current implementation docs were updated separately. |

## Deployment and data safety

Linux deployment templates use RAV service, release, and executable names. They are templates only; this work did not deploy or change a running service. No database schema, data, production credential, HTTPS/DTLS setting, gameplay behavior, scene layout, or script GUID was changed.
## Final local artifact and warnings

The Desktop build was produced after the RAV source edits. Its original folder remains untouched. The prepared deployment copy is `D:\Programing\UnityProjects\war-of-machines-local-builds\RAVServer-20261122-1956`, with matched `RAVServer.x86_64` and `RAVServer_Data` names. It contains the player/data/Mono/native plugin files, excludes `RAV_BurstDebugInformation_DoNotShip`, and all 189 retained files passed SHA-256 comparison against the original. The Desktop build and prepared copy are build artifacts only; Linux runtime and dynamic host-library resolution remain **NOT TESTED**.

The successful Editor build had zero errors and 47 non-fatal warnings: 37 obsolete API calls, four unused FishNet utility/demo fields, one unreachable CFXR vendor-effect branch, and five FishNet upgrade-helper advisories. No RAV-owned compiler warning was found in the build warning set.