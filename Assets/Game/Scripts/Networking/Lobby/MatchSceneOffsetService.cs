using UnityEngine;
using UEScene = UnityEngine.SceneManagement.Scene;

namespace Game.Scripts.Networking.Lobby
{
    public sealed class MatchSceneOffsetService
    {
        public void ApplyOffset(UEScene scene, int offset)
        {
            GameObject[] rootObjects = scene.GetRootGameObjects();
            for (int i = 0; i < rootObjects.Length; i++)
            {
                GameObject rootObject = rootObjects[i];
                if (rootObject != null)
                {
                    rootObject.transform.position += Vector3.right * offset;
                }
            }
        }
    }
}
