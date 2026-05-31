using FishNet.Object;
using FishNet.Observing;

namespace Game.Scripts.Conditions
{
    public class RoomConditionRebuilder : NetworkBehaviour
    {
        public NetworkObserver networkObserver;

        private string _roomID;
        
        public void SetupRoomID(string roomID)
        {
            _roomID = roomID;
        }
        
        public override void OnStartServer()
        {
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
