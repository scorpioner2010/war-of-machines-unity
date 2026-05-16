using System.Collections.Generic;

namespace Game.Scripts.Networking.Lobby
{
    public sealed class MatchSceneSlotAllocator
    {
        private readonly Stack<int> _freeSlots = new Stack<int>();
        private readonly HashSet<int> _allocatedSlots = new HashSet<int>();
        private int _nextSlot;

        public MatchSceneSlotAllocator(int spacing)
        {
            Spacing = spacing;
        }

        public int Spacing { get; }

        public int ReserveSlot()
        {
            int slot;
            if (_freeSlots.Count > 0)
            {
                slot = _freeSlots.Pop();
            }
            else
            {
                slot = _nextSlot;
                _nextSlot++;
            }

            _allocatedSlots.Add(slot);
            return slot;
        }

        public bool ReleaseSlot(int slot)
        {
            if (slot < 0 || !_allocatedSlots.Remove(slot))
            {
                return false;
            }

            _freeSlots.Push(slot);
            return true;
        }

        public int GetOffset(int slot)
        {
            return slot * Spacing;
        }

        public void Clear()
        {
            _freeSlots.Clear();
            _allocatedSlots.Clear();
            _nextSlot = 0;
        }
    }
}
