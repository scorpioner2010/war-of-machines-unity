using FishNet.Object;
using UnityEngine;

namespace Game.Scripts.Gameplay.Robots
{
    public enum VehicleInitializationPhase
    {
        Server = 0,
        Owner = 1
    }

    public readonly struct VehicleInitializationContext
    {
        public VehicleInitializationContext(VehicleRoot root, VehicleInitializationPhase phase, bool isMenu)
        {
            Root = root;
            Phase = phase;
            IsMenu = isMenu;
        }

        public VehicleRoot Root { get; }
        public VehicleInitializationPhase Phase { get; }
        public bool IsMenu { get; }
        public bool IsServer => Phase == VehicleInitializationPhase.Server;
        public bool IsOwner => Phase == VehicleInitializationPhase.Owner;
    }

    public interface IVehicleRootAware
    {
        void SetVehicleRoot(VehicleRoot vehicleRoot);
    }

    public interface IVehicleInitializable
    {
        void OnVehicleInitialized(VehicleInitializationContext context);
    }

    public interface IVehicleStatsConsumer
    {
        void ApplyVehicleStats(VehicleRuntimeStats stats);
    }

    public abstract class VehicleBehaviour : MonoBehaviour, IVehicleRootAware
    {
        [SerializeField] private VehicleRoot vehicleRoot;

        protected VehicleRoot VehicleRoot => vehicleRoot;

        public virtual void SetVehicleRoot(VehicleRoot root)
        {
            vehicleRoot = root;
        }
    }

    public abstract class NetworkVehicleBehaviour : NetworkBehaviour, IVehicleRootAware
    {
        [SerializeField] private VehicleRoot vehicleRoot;

        protected VehicleRoot VehicleRoot => vehicleRoot;

        public virtual void SetVehicleRoot(VehicleRoot root)
        {
            vehicleRoot = root;
        }
    }
}
