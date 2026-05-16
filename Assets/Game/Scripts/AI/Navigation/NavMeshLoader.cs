using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;

namespace Game.Scripts.AI.Navigation
{
    public class NavMeshLoader : MonoBehaviour
    {
        public NavMeshSurface navMeshSurface;
        public NavMeshData navMeshData;

        private void Start()
        {
            Reload();
        }

        public void Reload()
        {
            if (navMeshSurface != null && navMeshData != null)
            {
                navMeshSurface.RemoveData();
                navMeshSurface.navMeshData = navMeshData;
                navMeshSurface.AddData();
            }
        }
    }
}
