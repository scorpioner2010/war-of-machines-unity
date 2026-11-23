using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using RAV.Scripts.Diagnostics;
using RAV.Scripts.Networking.Lobby;
using UnityEngine;
using UEScene = UnityEngine.SceneManagement.Scene;

namespace RAV.Scripts.Gameplay.Robots
{
    public enum PlayerType
    {
        None,
        Player,
        Bot,
    }

    public class VehicleNetworkInitializer : NetworkBehaviour, IVehicleRootAware
    {
        public static event System.Action ClientTeamChanged;

        public VehicleRoot playerRoot;

        private readonly SyncVar<int> _amountPlayersInRoom = new ();
        public readonly SyncVar<string> LoginName = new ("");
        public readonly SyncVar<PlayerType> PlayerType = new(Robots.PlayerType.None);
        public readonly SyncVar<int> Kills = new();

        public void SetVehicleRoot(VehicleRoot vehicleRoot)
        {
            playerRoot = vehicleRoot;
        }
        public readonly SyncVar<MatchTeam> Team = new(MatchTeam.None);
        

        public UEScene currentScene;

        [Server]
        public void ServerInit(int amountPlayersInRoom, PlayerType playerType, string loginName, MatchTeam team, UEScene scene)
        {
            currentScene = scene;
            _amountPlayersInRoom.Value = amountPlayersInRoom;
            PlayerType.Value = playerType;
            LoginName.Value = loginName;
            Team.Value = team;
            Kills.Value = 0;
            GameplayRuntimeDiagnostics.Record(
                "vehicle_server_initialized",
                "vehicle", playerRoot != null ? playerRoot.name : name,
                "object_id", NetworkObject != null ? NetworkObject.ObjectId : -1,
                "player_type", playerType,
                "username", loginName,
                "team", team,
                "scene", scene.name,
                "room_size", amountPlayersInRoom);
        }

        [Server]
        public void ServerSetKills(int kills)
        {
            Kills.Value = Mathf.Max(0, kills);
        }

        public override void OnStartServer()
        {
        }

        public override void OnStartClient()
        {
            GameplayRuntimeDiagnostics.Record(
                "vehicle_identity_received",
                "vehicle", playerRoot != null ? playerRoot.name : name,
                "object_id", NetworkObject != null ? NetworkObject.ObjectId : -1,
                "owner", IsOwner,
                "position", transform.position);
            Team.OnChange += HandleTeamChanged;

            if (IsOwner)
            {
                playerRoot.Init();
                SetNickNameProcessAsync().Forget();
            }
        }

        public override void OnStopClient()
        {
            Team.OnChange -= HandleTeamChanged;
            base.OnStopClient();
        }

        private void HandleTeamChanged(MatchTeam previous, MatchTeam next, bool asServer)
        {
            ClientTeamChanged?.Invoke();
        }

        private async UniTask SetNickNameProcessAsync()
        {
            bool isActiveProcess = true;

            while (isActiveProcess)
            {
                await UniTask.Delay(500);

                bool allNicksSet = true;
                int vehicleCount = VehicleRoot.ActiveVehicleCount;
                for (int i = 0; i < vehicleCount; i++)
                {
                    VehicleRoot root = VehicleRoot.GetActiveVehicle(i);
                    if (root == null || root.characterInit == null || string.IsNullOrEmpty(root.characterInit.LoginName.Value))
                    {
                        allNicksSet = false;
                        break;
                    }
                }
                
                if (allNicksSet)
                {
                    if (CameraSync.In == null || CameraSync.In.gameplayCamera == null)
                    {
                        return;
                    }

                    Camera cam = CameraSync.In.gameplayCamera;

                    for (int i = 0; i < vehicleCount; i++)
                    {
                        VehicleRoot root = VehicleRoot.GetActiveVehicle(i);
                        if (root != null && OwnerId != root.OwnerId && root.vehicleHUD != null && root.characterInit != null)
                        {
                            root.vehicleHUD.SetCamera(cam);
                            root.vehicleHUD.SetNick(root.characterInit.LoginName.Value);
                        }
                    }

                    isActiveProcess = false;
                }
            }
        }
    }
}
