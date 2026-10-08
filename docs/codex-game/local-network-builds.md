# Local Network Builds

Use this when testing the local API, dedicated server, and client together.

Owner file:
- `Assets/Editor/LocalNetworkBuild.cs`
  - Builds both sides from the same Unity project, package lock, and FishNet version.
  - Menu: `War Of Machines > Local Build > Windows Server and Client`.
  - Server uses `Server.unity` and `Map.unity` with `StandaloneBuildSubtarget.Server`.
  - Client uses `Client.unity` and `Map.unity` with `StandaloneBuildSubtarget.Player`.
  - Each build clears only its own generated output directory first, preventing a stale executable or data folder from being reused.

Generated outputs:
- Server: `D:\Programing\UnityProjects\war-of-machines-local-builds\Server\WarOfMachinesServer.exe`
- Client: `D:\Programing\UnityProjects\war-of-machines-local-builds\Client\WarOfMachinesClient.exe`

Both builds use `HttpLink.APIBase`, which defaults to `https://localhost:7216`. Start the API with its `local-postgres` profile before launching the server, then launch the client.
