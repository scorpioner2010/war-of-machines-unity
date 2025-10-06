using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Toras.Editor
{
    public class FindMissingsScripts : MonoBehaviour
    {
        [MenuItem("TorasDeveloper/Fund missing script in project")]
        static void FundMissingScriptsInProject()
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

        [MenuItem("TorasDeveloper/Fund missing script in scene")]
        static void FindMissingInScene()
        {
            foreach (var gameObject in GameObject.FindObjectsOfType<GameObject>(true))
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
