# Local Network Builds

Use this when testing the local API, dedicated server, and client together.

Owner file:
- `Assets/Editor/LocalNetworkBuild.cs`
  - Builds both sides from the same RAV Unity project, package lock, and FishNet version.
  - Windows menu: `RAV > Local Build > Windows Server and Client`; separate server and client commands are also available.
  - Linux Dedicated Server menu: `RAV > Local Build > Linux Dedicated Server (Mono)`.
  - Server uses `Server.unity` and `Map.unity` with `StandaloneBuildSubtarget.Server`.
  - Client uses `Client.unity` and `Map.unity` with `StandaloneBuildSubtarget.Player`.
  - Each build writes to a unique timestamped directory; it does not clear or overwrite prior build output.
  - Linux output is `RAVServer.x86_64` in a new `LinuxServer-<UTC timestamp>` directory.
  - The `[InitializeOnLoad]` Editor watcher consumes the existing request files in `D:\Programing\UnityProjects\war-of-machines-local-builds`, builds through the already open Editor, and writes the matching result token. The historical build-root name is retained to preserve existing artifacts and local security files.

Desktop launcher bridge:
- `C:\Users\TorasPC\Desktop\Start War of Machines.bat` and `Stop War of Machines.bat` remain legacy shortcut filenames; their contents target the RAV Unity project, API, and executable names.
- The start launcher builds the API, writes a unique token to the request file, and waits up to 30 minutes for the same token in the result file.
- The result format is `token|SUCCESS|` or `token|FAILURE|message`; result writes use a temporary file followed by an atomic replacement/move so the launcher does not read a partial response.
- The launcher uses the open Unity Editor for the client/server build; it does not start a second Unity process for that build.
- Before building the API, it verifies Docker CLI and the local Compose file, starts Docker Desktop when the daemon is unavailable, closes only the Dashboard window so Docker remains in the system tray, waits up to five minutes for the Docker engine, runs the local Compose PostgreSQL service, and verifies PostgreSQL on `127.0.0.1:5433`.
- Docker Desktop must already be installed; the launcher does not install Docker or change the named PostgreSQL volume. The stop launcher stops only API/server processes and leaves PostgreSQL running for the next start.
- If the Editor has not recompiled `LocalNetworkBuild.cs`, the request remains pending and the launcher waits until its timeout. Unity Console/`Editor.log` contains build details.

Generated outputs under the preserved build root:
- Server: `Server-<UTC timestamp>-<id>\RAVServer.exe`
- Client: `Client-<UTC timestamp>-<id>\RAVClient.exe`
- Linux Dedicated Server: `LinuxServer-<UTC timestamp>\RAVServer.x86_64`

Windows client/server builds use `HttpLink.APIBase`, which defaults to `https://localhost:7216`. Start the API with its `local-postgres` profile before launching the server, then launch the client. Final manual Linux Build Profile output was validated on 2026-11-22. The Desktop folder used `LinuxServer.x86_64` and `LinuxServer_Data`; a separate deployment staging copy renames both to `RAVServer.x86_64` and `RAVServer_Data`, matching the build and Linux deployment scripts. The staging copy excludes `RAV_BurstDebugInformation_DoNotShip`; the original Desktop build remains unchanged. Linux runtime execution is still NOT TESTED.