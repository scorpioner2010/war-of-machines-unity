using System.Collections.Generic;

namespace Game.Scripts.Networking.Lobby
{
    public static class PendingBattleResults
    {
        private static readonly Dictionary<int, Queue<PlayerBattleResult>> ResultsByUserId = new Dictionary<int, Queue<PlayerBattleResult>>();

        public static void Enqueue(int userId, PlayerBattleResult result)
        {
            if (userId <= 0)
            {
                return;
            }

            if (!ResultsByUserId.TryGetValue(userId, out Queue<PlayerBattleResult> queue))
            {
                queue = new Queue<PlayerBattleResult>();
                ResultsByUserId[userId] = queue;
            }

            queue.Enqueue(result);
        }

        public static bool TryTakeAll(int userId, List<PlayerBattleResult> results)
        {
            if (userId <= 0 || results == null)
            {
                return false;
            }

            if (!ResultsByUserId.TryGetValue(userId, out Queue<PlayerBattleResult> queue))
            {
                return false;
            }

            while (queue.Count > 0)
            {
                results.Add(queue.Dequeue());
            }

            ResultsByUserId.Remove(userId);
            return results.Count > 0;
        }

        public static void Clear()
        {
            ResultsByUserId.Clear();
        }
    }
}
