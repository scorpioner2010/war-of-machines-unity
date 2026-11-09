# Local Network Builds

Use this when testing the local API, dedicated server, and client together.

Owner file:
- `Assets/Editor/LocalNetworkBuild.cs`
  - Builds both sides from the same Unity project, package lock, and FishNet version.
  - Menu: `War Of Machines > Local Build > Windows Server and Client`.
  - Server uses `Server.unity` and `Map.unity` with `StandaloneBuildSubtarget.Server`.
  - Client uses `Client.unity` and `Map.unity` with `StandaloneBuildSubtarget.Player`.
  - Each build clears only its own generated output directory first, preventing a stale executable or data folder from being reused.
  - The `[InitializeOnLoad]` Editor watcher consumes `D:\Programing\UnityProjects\war-of-machines-local-builds\server-build-request.txt` or `client-build-request.txt`, builds the requested target through the already open Editor, and writes the matching `server-build-result.txt` or `client-build-result.txt` response.

Desktop launcher bridge:
- `C:\Users\TorasPC\Desktop\Start War of Machines.bat` builds the API, writes a unique token to the request file, and waits up to 30 minutes for the same token in the result file.
- The result format is `token|SUCCESS|` or `token|FAILURE|message`; result writes use a temporary file followed by an atomic replacement/move so the launcher does not read a partial response.
- The launcher must use the open Unity Editor for this project; it must not start a second Unity process for the server build.
- Before building the API, the launcher verifies Docker CLI and the local Compose file, starts Docker Desktop when the daemon is unavailable, closes only the Dashboard window so Docker remains in the system tray, waits up to five minutes for the Docker engine, runs `docker compose ... up -d --wait postgres`, and verifies PostgreSQL on `127.0.0.1:5433`.
- Docker Desktop itself must be installed; the launcher does not install Docker or change the named PostgreSQL volume. `Stop War of Machines.bat` stops only API/server processes and leaves PostgreSQL running for the next start.
- If the Editor has not recompiled `LocalNetworkBuild.cs`, the request file remains pending and the launcher waits until its timeout. The Unity Console/`Editor.log` contains the build details.

Generated outputs:
- Server: `D:\Programing\UnityProjects\war-of-machines-local-builds\Server\WarOfMachinesServer.exe`
- Client: `D:\Programing\UnityProjects\war-of-machines-local-builds\Client\WarOfMachinesClient.exe`

Both builds use `HttpLink.APIBase`, which defaults to `https://localhost:7216`. Start the API with its `local-postgres` profile before launching the server, then launch the client.
