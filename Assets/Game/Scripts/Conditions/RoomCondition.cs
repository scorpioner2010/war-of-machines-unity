using FishNet.Connection;
using FishNet.Observing;
using Game.Scripts.Networking.Lobby;
using UnityEngine;

namespace Game.Scripts.Conditions
{
    [CreateAssetMenu(menuName = "FishNet/Observers/Room Condition", fileName = "New Room Condition")]
    public class RoomCondition : ObserverCondition
    {
        private string objectRoomId;

        public void SetObjectRoomId(string id)
        {
            objectRoomId = id;
        }

        public override bool ConditionMet(NetworkConnection connection, bool currentlyAdded, out bool notProcessed)
        {
            notProcessed = false;
            
            ServerRoom serverRoom = LobbyRooms.GetRoomByConnection(connection);
            if (serverRoom == null)
                return false;

            return serverRoom.roomId == objectRoomId;
        }

        public override ObserverConditionType GetConditionType() => ObserverConditionType.Timed;
       
    }
}