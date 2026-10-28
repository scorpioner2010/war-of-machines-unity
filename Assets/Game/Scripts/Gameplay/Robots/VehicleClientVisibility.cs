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
        private bool _serverVisualsHidden;
        private bool _hasVisibilityOverride;
        private bool _visibilityOverride;
        private bool _hasSpectatorVisibilityOverride;
        private bool _spectatorPreviousVisibility;
        private MatchTeam _localPlayerTeam = MatchTeam.None;

        public bool IsVisible => _hasAppliedVisibility && _isVisible;
        public bool ServerVisualsHidden => _serverVisualsHidden;

        public void SetVehicleRoot(VehicleRoot root)
        {
            vehicleRoot = root;
            VehicleColliderRegistry.RegisterRenderers(vehicleRoot, visualRenderers);
            _refreshRequested = true;
        }

        public void OnVehicleClientStarted()
        {
            _clientStarted = true;
            _refreshRequested = false;
            RefreshVisibility();
        }

        public void SetVisibilityOverride(bool enabledOverride, bool visible)
        {
            _hasVisibilityOverride = enabledOverride;
            _visibilityOverride = visible;
            _refreshRequested = true;

            if (_clientStarted)
            {
                RefreshVisibility();
            }
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

        public void SetSpectatorVisibility(bool visible)
        {
            if (visible)
            {
                if (!_hasSpectatorVisibilityOverride)
                {
                    _spectatorPreviousVisibility = _hasAppliedVisibility && _isVisible;
                }

                _hasSpectatorVisibilityOverride = true;
                ApplyVisibility(true);
                return;
            }

            if (!_hasSpectatorVisibilityOverride)
            {
                return;
            }

            _hasSpectatorVisibilityOverride = false;
            if (_clientStarted && enabled)
            {
                _refreshRequested = true;
                RefreshVisibility();
                return;
            }

            ApplyVisibility(_spectatorPreviousVisibility);
        }

        public void HideVisualObjectsForServer()
        {
            if (_serverVisualsHidden)
            {
                return;
            }

            _serverVisualsHidden = true;
            _refreshRequested = false;
            enabled = false;

            if (visualRenderers == null)
            {
                return;
            }

            for (int i = 0; i < visualRenderers.Length; i++)
            {
                Renderer visualRenderer = visualRenderers[i];
                if (visualRenderer == null)
                {
                    continue;
                }

                GameObject visualObject = visualRenderer.gameObject;
                if (visualObject == gameObject || !visualObject.transform.IsChildOf(transform))
                {
                    Debug.LogError(
                        $"{nameof(VehicleClientVisibility)} on {name} cannot strip renderer {visualRenderer.name} because it is not on a child visual object.",
                        this);
                    continue;
                }

                visualRenderer.enabled = false;
                visualRenderer.forceRenderingOff = true;
            }
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
            CacheLocalPlayerTeam(localPlayer);
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

            if (_hasSpectatorVisibilityOverride)
            {
                ApplyVisibility(true);
                return;
            }

            if (_hasVisibilityOverride)
            {
                ApplyVisibility(_visibilityOverride);
                return;
            }

            if (vehicleRoot.IsMenu)
            {
                ApplyVisibility(true);
                return;
            }

            VehicleRoot localPlayer = VehicleRoot.LocalPlayerVehicle;
            CacheLocalPlayerTeam(localPlayer);
            if (vehicleRoot.IsOwner || vehicleRoot == localPlayer)
            {
                ApplyVisibility(true);
                return;
            }

            if (!MatchTeamUtility.IsAssigned(_localPlayerTeam))
            {
                ApplyVisibility(false);
                return;
            }

            if (vehicleRoot.characterInit == null)
            {
                ApplyVisibility(false);
                return;
            }

            MatchTeam targetTeam = vehicleRoot.characterInit.Team.Value;
            if (!MatchTeamUtility.IsAssigned(targetTeam))
            {
                ApplyVisibility(false);
                return;
            }

            if (MatchTeamUtility.AreSameAssignedTeam(_localPlayerTeam, targetTeam))
            {
                ApplyVisibility(true);
                return;
            }

            int objectId = vehicleRoot.networkObject != null
                ? vehicleRoot.networkObject.ObjectId
                : -1;
            bool visible = GameplayMapVisibilityState.TryGetRelation(
                               objectId,
                               out MapVehicleVisibilityRelation relation)
                           && relation == MapVehicleVisibilityRelation.Enemy;
            ApplyVisibility(visible);
        }

        private void CacheLocalPlayerTeam(VehicleRoot localPlayer)
        {
            if (localPlayer == null || localPlayer.characterInit == null)
            {
                return;
            }

            MatchTeam team = localPlayer.characterInit.Team.Value;
            if (MatchTeamUtility.IsAssigned(team))
            {
                _localPlayerTeam = team;
            }
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
