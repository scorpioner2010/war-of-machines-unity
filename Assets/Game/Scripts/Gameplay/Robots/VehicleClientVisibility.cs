using Game.Scripts.Networking.Lobby;
using Game.Scripts.UI.HUD;
using UnityEngine;

namespace Game.Scripts.Gameplay.Robots
{
    [DisallowMultipleComponent]
    public sealed class VehicleClientVisibility : MonoBehaviour, IVehicleRootAware
    {
        public VehicleRoot vehicleRoot;
        public Renderer[] visualRenderers = System.Array.Empty<Renderer>();

        private bool _refreshRequested = true;
        private bool _clientStarted;
        private bool _hasAppliedVisibility;
        private bool _isVisible;

        public bool IsVisible => _hasAppliedVisibility && _isVisible;

        public void SetVehicleRoot(VehicleRoot root)
        {
            vehicleRoot = root;
            _refreshRequested = true;
        }

        public void OnVehicleClientStarted()
        {
            _clientStarted = true;
            _refreshRequested = false;
            RefreshVisibility();
        }

        public bool ReleaseRenderersForDeath()
        {
            bool visibleAtDeath = !_clientStarted || !_hasAppliedVisibility || _isVisible;
            _refreshRequested = false;
            _hasAppliedVisibility = true;
            _isVisible = visibleAtDeath;
            SetRenderersVisible(visibleAtDeath);

            if (vehicleRoot != null && vehicleRoot.hoverOutline != null)
            {
                vehicleRoot.hoverOutline.SetOutlined(false);
            }

            enabled = false;
            return visibleAtDeath;
        }

        private void OnEnable()
        {
            GameplayMapVisibilityState.Changed += HandleVisibilityChanged;
            VehicleRoot.LocalPlayerVehicleChanged += HandleLocalPlayerVehicleChanged;
            VehicleNetworkInitializer.ClientTeamChanged += HandleClientTeamChanged;
            _refreshRequested = true;
        }

        private void OnDisable()
        {
            GameplayMapVisibilityState.Changed -= HandleVisibilityChanged;
            VehicleRoot.LocalPlayerVehicleChanged -= HandleLocalPlayerVehicleChanged;
            VehicleNetworkInitializer.ClientTeamChanged -= HandleClientTeamChanged;
        }

        private void Update()
        {
            if (!_refreshRequested)
            {
                return;
            }

            _refreshRequested = false;
            RefreshVisibility();
        }

        private void HandleVisibilityChanged()
        {
            _refreshRequested = true;
        }

        private void HandleLocalPlayerVehicleChanged(VehicleRoot localPlayer)
        {
            _refreshRequested = true;
        }

        private void HandleClientTeamChanged()
        {
            _refreshRequested = true;
        }

        private void RefreshVisibility()
        {
            if (vehicleRoot == null)
            {
                return;
            }

            if (!_clientStarted)
            {
                return;
            }

            if (vehicleRoot.IsMenu)
            {
                ApplyVisibility(true);
                return;
            }

            VehicleRoot localPlayer = VehicleRoot.LocalPlayerVehicle;
            if (vehicleRoot.IsOwner || vehicleRoot == localPlayer)
            {
                ApplyVisibility(true);
                return;
            }

            if (localPlayer == null)
            {
                ApplyVisibility(false);
                return;
            }

            if (vehicleRoot.characterInit == null || localPlayer.characterInit == null)
            {
                ApplyVisibility(false);
                return;
            }

            MatchTeam localTeam = localPlayer.characterInit.Team.Value;
            MatchTeam targetTeam = vehicleRoot.characterInit.Team.Value;
            if (!MatchTeamUtility.IsAssigned(localTeam) || !MatchTeamUtility.IsAssigned(targetTeam))
            {
                ApplyVisibility(false);
                return;
            }

            if (MatchTeamUtility.AreSameAssignedTeam(localTeam, targetTeam))
            {
                ApplyVisibility(true);
                return;
            }

            int objectId = vehicleRoot.networkObject != null
                ? vehicleRoot.networkObject.ObjectId
                : -1;
            bool visible = GameplayMapVisibilityState.TryGetRelation(objectId, out _);
            ApplyVisibility(visible);
        }

        private void ApplyVisibility(bool visible)
        {
            if (_hasAppliedVisibility && _isVisible == visible)
            {
                return;
            }

            _hasAppliedVisibility = true;
            _isVisible = visible;
            SetRenderersVisible(visible);

            if (vehicleRoot != null && vehicleRoot.vehicleHUD != null)
            {
                vehicleRoot.vehicleHUD.SetMapVisible(visible);
            }

            if (!visible && vehicleRoot != null && vehicleRoot.hoverOutline != null)
            {
                vehicleRoot.hoverOutline.SetOutlined(false);
            }
        }

        private void SetRenderersVisible(bool visible)
        {
            if (visualRenderers != null)
            {
                for (int i = 0; i < visualRenderers.Length; i++)
                {
                    Renderer visualRenderer = visualRenderers[i];
                    if (visualRenderer != null)
                    {
                        visualRenderer.forceRenderingOff = !visible;
                    }
                }
            }
        }
    }
}
