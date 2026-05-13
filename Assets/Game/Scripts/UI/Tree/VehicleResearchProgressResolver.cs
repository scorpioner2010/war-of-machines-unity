using System.Collections.Generic;
using Game.Scripts.API;
using Game.Scripts.API.Models;
using Game.Scripts.Player.Data;

namespace Game.Scripts.UI.Tree
{
    public enum VehicleResearchStatus
    {
        Hidden,
        Owned,
        LockedByResearch,
        AvailableToResearch,
        AvailableToBuy
    }

    public class VehicleResearchProgress
    {
        public VehicleNode Node;
        public VehicleLite Vehicle;
        public VehicleResearchStatus Status;
        public bool IsOwned;
        public bool IsVisible;
        public bool IsResearched;
        public bool IsResearchUnlocked;
        public int PredecessorVehicleId;
        public int PurchaseCost;
        public int RequiredXp;
        public int CurrentXp;
        public int MissingXp;
        public string PredecessorName;
    }

    public class VehicleResearchProgressResolver
    {
        private readonly VehicleGraph _graph;
        private readonly Dictionary<int, OwnedVehicleDto> _ownedByVehicleId = new Dictionary<int, OwnedVehicleDto>();
        private readonly Dictionary<int, bool> _researchedByVehicleId = new Dictionary<int, bool>();
        private readonly Dictionary<int, VehicleLite> _vehiclesById = new Dictionary<int, VehicleLite>();

        public VehicleResearchProgressResolver(PlayerProfile profile, VehicleGraph graph, VehicleLite[] vehicles)
        {
            _graph = graph;
            IndexOwnedVehicles(profile);
            IndexResearchedVehicles(profile);
            IndexVehicles(vehicles);
        }

        public VehicleResearchProgress Resolve(VehicleNode node)
        {
            VehicleResearchProgress progress = new VehicleResearchProgress();
            progress.Node = node;

            if (node == null)
            {
                progress.Status = VehicleResearchStatus.Hidden;
                return progress;
            }

            VehicleLite vehicle = FindVehicle(node.id);
            progress.Vehicle = vehicle;
            progress.PurchaseCost = vehicle != null ? vehicle.purchaseCost : 0;
            progress.IsVisible = node.isVisible && (vehicle == null || vehicle.isVisible);
            progress.IsOwned = IsOwned(node.id);
            progress.IsResearched = IsResearched(node.id);

            if (!progress.IsVisible)
            {
                progress.Status = VehicleResearchStatus.Hidden;
                return progress;
            }

            if (progress.IsOwned)
            {
                progress.IsResearched = true;
                progress.Status = VehicleResearchStatus.Owned;
                return progress;
            }

            if (progress.IsResearched)
            {
                progress.Status = VehicleResearchStatus.AvailableToBuy;
                return progress;
            }

            ResolveResearchState(node, progress);
            return progress;
        }

        private void ResolveResearchState(VehicleNode node, VehicleResearchProgress progress)
        {
            VehicleEdge[] edges = _graph != null ? _graph.edges : null;
            if (edges == null || edges.Length == 0)
            {
                progress.IsResearched = true;
                progress.Status = VehicleResearchStatus.AvailableToBuy;
                return;
            }

            bool hasIncomingLink = false;
            bool hasAffordableLink = false;
            int bestMissingXp = int.MaxValue;
            int bestAffordableXp = int.MaxValue;

            for (int i = 0; i < edges.Length; i++)
            {
                VehicleEdge edge = edges[i];
                if (edge == null || edge.toId != node.id)
                {
                    continue;
                }

                hasIncomingLink = true;
                int requiredXp = edge.requiredXp;
                OwnedVehicleDto predecessor = FindOwned(edge.fromId);
                int currentXp = predecessor != null ? predecessor.xp : 0;
                int missingXp = requiredXp - currentXp;

                if (missingXp <= 0 && predecessor != null)
                {
                    if (requiredXp >= bestAffordableXp)
                    {
                        continue;
                    }

                    hasAffordableLink = true;
                    bestAffordableXp = requiredXp;
                    progress.IsResearchUnlocked = true;
                    progress.RequiredXp = requiredXp;
                    progress.CurrentXp = currentXp;
                    progress.MissingXp = 0;
                    progress.PredecessorVehicleId = edge.fromId;
                    progress.PredecessorName = ResolveVehicleName(edge.fromId, predecessor.name);
                    continue;
                }

                if (!hasAffordableLink && missingXp < bestMissingXp)
                {
                    bestMissingXp = missingXp;
                    progress.RequiredXp = requiredXp;
                    progress.CurrentXp = currentXp;
                    progress.MissingXp = missingXp > 0 ? missingXp : 0;
                    progress.PredecessorVehicleId = predecessor != null ? edge.fromId : 0;
                    progress.PredecessorName = ResolveVehicleName(edge.fromId, predecessor != null ? predecessor.name : null);
                }
            }

            if (hasAffordableLink)
            {
                progress.Status = VehicleResearchStatus.AvailableToResearch;
                return;
            }

            if (!hasIncomingLink)
            {
                progress.IsResearched = true;
                progress.Status = VehicleResearchStatus.AvailableToBuy;
                return;
            }

            progress.Status = VehicleResearchStatus.LockedByResearch;
        }

        private void IndexOwnedVehicles(PlayerProfile profile)
        {
            if (profile == null || profile.ownedVehicles == null)
            {
                return;
            }

            for (int i = 0; i < profile.ownedVehicles.Length; i++)
            {
                OwnedVehicleDto dto = profile.ownedVehicles[i];
                if (dto == null || dto.vehicleId <= 0)
                {
                    continue;
                }

                _ownedByVehicleId[dto.vehicleId] = dto;
                _researchedByVehicleId[dto.vehicleId] = true;
            }
        }

        private void IndexResearchedVehicles(PlayerProfile profile)
        {
            if (profile == null || profile.researchedVehicles == null)
            {
                return;
            }

            for (int i = 0; i < profile.researchedVehicles.Length; i++)
            {
                ResearchedVehicleDto dto = profile.researchedVehicles[i];
                if (dto == null || dto.vehicleId <= 0)
                {
                    continue;
                }

                _researchedByVehicleId[dto.vehicleId] = true;
            }
        }

        private void IndexVehicles(VehicleLite[] vehicles)
        {
            if (vehicles == null)
            {
                return;
            }

            for (int i = 0; i < vehicles.Length; i++)
            {
                VehicleLite vehicle = vehicles[i];
                if (vehicle == null || vehicle.id <= 0)
                {
                    continue;
                }

                _vehiclesById[vehicle.id] = vehicle;
            }
        }

        private bool IsOwned(int vehicleId)
        {
            return FindOwned(vehicleId) != null;
        }

        private bool IsResearched(int vehicleId)
        {
            return _researchedByVehicleId.ContainsKey(vehicleId);
        }

        private OwnedVehicleDto FindOwned(int vehicleId)
        {
            OwnedVehicleDto dto;
            if (_ownedByVehicleId.TryGetValue(vehicleId, out dto))
            {
                return dto;
            }

            return null;
        }

        private VehicleLite FindVehicle(int vehicleId)
        {
            VehicleLite vehicle;
            if (_vehiclesById.TryGetValue(vehicleId, out vehicle))
            {
                return vehicle;
            }

            return null;
        }

        private string ResolveVehicleName(int vehicleId, string fallback)
        {
            if (!string.IsNullOrEmpty(fallback))
            {
                return fallback;
            }

            VehicleLite vehicle = FindVehicle(vehicleId);
            if (vehicle != null && !string.IsNullOrEmpty(vehicle.name))
            {
                return vehicle.name;
            }

            return "previous vehicle";
        }
    }
}
