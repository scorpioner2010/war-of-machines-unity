using NaughtyAttributes;
using UnityEngine;

namespace Game.Scripts.World.Spawns
{
    public class AutomaticPositionSpawnpoints : MonoBehaviour
    {
        public int gridWidth = 10;
        public float spacing = 1.0f;

        [Button]
        private void SetPosition()
        {
            int count = 0;

            foreach (Transform child in transform)
            {
                int x = count % gridWidth;
                int z = count / gridWidth;

                child.localPosition = new Vector3((x + 1) * spacing, 0, z * spacing);
                count++;
            }
        }
    }
}