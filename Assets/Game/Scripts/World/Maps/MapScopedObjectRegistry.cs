using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UESceneManager = UnityEngine.SceneManagement.SceneManager;

namespace Game.Scripts.World.Maps
{
    public static class MapScopedObjectRegistry
    {
        private static readonly Dictionary<int, MapScopedObjectSet> ObjectSetsBySceneHandle = new Dictionary<int, MapScopedObjectSet>(4);
        private static readonly List<int> MapSceneHandles = new List<int>(4);
        private static bool _isSubscribed;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset()
        {
            ObjectSetsBySceneHandle.Clear();
            MapSceneHandles.Clear();
            UESceneManager.sceneUnloaded -= HandleSceneUnloaded;
            _isSubscribed = false;
        }

        public static void RegisterMapScene(Scene scene)
        {
            if (!scene.IsValid())
            {
                return;
            }

            EnsureSubscribed();

            int sceneHandle = scene.handle;
            for (int i = 0; i < MapSceneHandles.Count; i++)
            {
                if (MapSceneHandles[i] == sceneHandle)
                {
                    return;
                }
            }

            MapSceneHandles.Add(sceneHandle);
        }

        public static void Register(Scene scene, GameObject gameObject)
        {
            if (!scene.IsValid() || gameObject == null)
            {
                return;
            }

            EnsureSubscribed();

            int sceneHandle = scene.handle;
            if (!ObjectSetsBySceneHandle.TryGetValue(sceneHandle, out MapScopedObjectSet objectSet))
            {
                objectSet = new MapScopedObjectSet();
                ObjectSetsBySceneHandle.Add(sceneHandle, objectSet);
            }

            objectSet.Add(gameObject);
        }

        public static void MoveRootToScene(Scene scene, GameObject gameObject)
        {
            if (!scene.IsValid() || !scene.isLoaded || gameObject == null)
            {
                return;
            }

            if (gameObject.transform.parent != null)
            {
                return;
            }

            if (gameObject.scene == scene)
            {
                return;
            }

            UESceneManager.MoveGameObjectToScene(gameObject, scene);
        }

        public static Scene ResolveMapScene(Scene preferredScene)
        {
            if (preferredScene.IsValid() && IsRegisteredMapScene(preferredScene.handle))
            {
                return preferredScene;
            }

            if (TryGetSingleRegisteredMapScene(out Scene mapScene))
            {
                return mapScene;
            }

            return preferredScene;
        }

        public static void DestroyForScene(Scene scene)
        {
            if (!scene.IsValid())
            {
                return;
            }

            DestroyForSceneHandle(scene.handle);
        }

        private static void EnsureSubscribed()
        {
            if (_isSubscribed)
            {
                return;
            }

            UESceneManager.sceneUnloaded -= HandleSceneUnloaded;
            UESceneManager.sceneUnloaded += HandleSceneUnloaded;
            _isSubscribed = true;
        }

        private static void HandleSceneUnloaded(Scene scene)
        {
            DestroyForSceneHandle(scene.handle);
        }

        private static void DestroyForSceneHandle(int sceneHandle)
        {
            RemoveMapSceneHandle(sceneHandle);

            if (!ObjectSetsBySceneHandle.TryGetValue(sceneHandle, out MapScopedObjectSet objectSet))
            {
                return;
            }

            ObjectSetsBySceneHandle.Remove(sceneHandle);
            objectSet.DestroyAll();
        }

        private static bool IsRegisteredMapScene(int sceneHandle)
        {
            for (int i = 0; i < MapSceneHandles.Count; i++)
            {
                if (MapSceneHandles[i] == sceneHandle)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool TryGetSingleRegisteredMapScene(out Scene mapScene)
        {
            mapScene = default;
            int foundCount = 0;

            for (int i = MapSceneHandles.Count - 1; i >= 0; i--)
            {
                int sceneHandle = MapSceneHandles[i];
                if (!TryGetLoadedScene(sceneHandle, out Scene loadedScene))
                {
                    MapSceneHandles.RemoveAt(i);
                    continue;
                }

                foundCount++;
                mapScene = loadedScene;
                if (foundCount > 1)
                {
                    mapScene = default;
                    return false;
                }
            }

            return foundCount == 1;
        }

        private static bool TryGetLoadedScene(int sceneHandle, out Scene scene)
        {
            for (int i = 0; i < UESceneManager.sceneCount; i++)
            {
                Scene loadedScene = UESceneManager.GetSceneAt(i);
                if (loadedScene.handle == sceneHandle && loadedScene.IsValid())
                {
                    scene = loadedScene;
                    return true;
                }
            }

            scene = default;
            return false;
        }

        private static void RemoveMapSceneHandle(int sceneHandle)
        {
            for (int i = MapSceneHandles.Count - 1; i >= 0; i--)
            {
                if (MapSceneHandles[i] == sceneHandle)
                {
                    MapSceneHandles.RemoveAt(i);
                }
            }
        }

        private sealed class MapScopedObjectSet
        {
            private readonly List<GameObject> _objects = new List<GameObject>(32);
            private readonly HashSet<int> _objectIds = new HashSet<int>();

            public void Add(GameObject gameObject)
            {
                if (gameObject == null)
                {
                    return;
                }

                int objectId = gameObject.GetInstanceID();
                if (!_objectIds.Add(objectId))
                {
                    return;
                }

                _objects.Add(gameObject);
            }

            public void DestroyAll()
            {
                for (int i = _objects.Count - 1; i >= 0; i--)
                {
                    GameObject gameObject = _objects[i];
                    if (gameObject == null)
                    {
                        continue;
                    }

                    if (Application.isPlaying)
                    {
                        Object.Destroy(gameObject);
                    }
                    else
                    {
                        Object.DestroyImmediate(gameObject);
                    }
                }

                _objects.Clear();
                _objectIds.Clear();
            }
        }
    }
}
