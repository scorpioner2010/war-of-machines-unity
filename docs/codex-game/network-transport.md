# Network Transport

Read this before changing FishNet transports, Unity Transport packages, connection ports, or client/server build compatibility.

Current transport:
- `Assets/Game/Content/Networking/Prefabs/NetworkManager.prefab` owns the FishNet `NetworkManager`, the Unity Transport component, and its address/port values.
- The transport listens on `0.0.0.0:7770`; its local client address is `127.0.0.1:7770`.
- `Packages/manifest.json` pins `com.alven.fishnet.unitytransport` to commit `08756d9733a556018041e52fcb9d2a6035346aeb`.
- The pinned transport fixes Unity Transport 2.x queue pointer handling. The project resolves `com.unity.transport` `2.7.2`; older FishyUnityTransport revisions corrupt the FishNet version handshake between the client and dedicated server.
- The Windows `Server` target uses the Mono scripting backend. The current FishNet/Unity Transport combination corrupts the version handshake when the dedicated server is built with IL2CPP while the Windows client uses Mono.

Local verification:
1. Build both applications through `War Of Machines > Local Build > Windows Server and Client`.
2. Start the local API, then `WarOfMachinesServer.exe`.
3. Confirm `GET https://localhost:7216/unity-server/status` returns `isOnline: true`.
4. Start `WarOfMachinesClient.exe` and check that neither log contains `Size of -6 is invalid` or `has been kicked for being on FishNet version`.
