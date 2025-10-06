using FishNet.Object;
using FishNet.Observing;

namespace Game.Scripts.Conditions
{
    public class RoomConditionRebuilder : NetworkBehaviour
    {
        private string _roomID;
        
        public void SetupRoomID(string roomID)
        {
            _roomID = roomID;
        }
        
        public override void OnStartServer()
        {
            NetworkObserver networkObserver = GetComponent<NetworkObserver>();
            if (networkObserver != null)
            {
                ObserverCondition roomCondition = networkObserver.GetObserverCondition<RoomCondition>();
                if (roomCondition != null)
                {
                    (roomCondition as RoomCondition)?.SetObjectRoomId(_roomID);
                }
            }
        }
    }
}