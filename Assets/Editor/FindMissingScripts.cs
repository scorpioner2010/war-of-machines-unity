using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Toras.Editor
{
    public class FindMissingScripts : MonoBehaviour
    {
        [MenuItem("TorasDeveloper/Find missing script in project")]
        private static void FindMissingScriptsInProject()
        {
            var c = AssetDatabase.GetAllAssetPaths()
                .Where(path => path.EndsWith(".prefab", System.StringComparison.OrdinalIgnoreCase)).ToArray();

            foreach (var path in c)
            {
                var pr = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                foreach (var component in pr.GetComponentsInChildren<Component>())
                {
                    if (component == null)
                    {
                        Debug.LogError("Have missing script: "+path, pr);
                        break;
                    }
                }
            }
        }

        [MenuItem("TorasDeveloper/Find missing script in scene")]
        private static void FindMissingInScene()
        {
            foreach (var gameObject in GameObject.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                foreach (var component in gameObject.GetComponentsInChildren<Component>())
                {
                    if (component == null)
                    {
                        Debug.LogError("Missing script: "+gameObject.name, gameObject);
                        break;
                    }
                }
            }
        }
    }
}
