using UnityEngine;
using UnityEngine.SceneManagement;

namespace RAV.Scripts.World.Spawns
{
    public static class MatchSpawnOrientationResolver
    {
        private const string MapCenterObjectName = "RespawnTrigger";
        private const float MinimumDirectionSqrMagnitude = 0.0001f;

        public static Quaternion ResolveRotation(Scene scene, Vector3 spawnPosition, Quaternion fallbackRotation)
        {
            if (TryResolveCenterRotation(scene, spawnPosition, out Quaternion centerRotation)
                || TryResolveLoadedSceneCenterRotation(spawnPosition, out centerRotation))
            {
                return centerRotation;
            }

            return fallbackRotation;
        }

        public static bool ApplyMapCenterRotation(Scene scene, Transform vehicleTransform)
        {
            if (vehicleTransform == null)
            {
                return false;
            }

            if (!TryResolveCenterRotation(scene, vehicleTransform.position, out Quaternion centerRotation)
                && !TryResolveLoadedSceneCenterRotation(vehicleTransform.position, out centerRotation))
            {
                return false;
            }

            vehicleTransform.rotation = centerRotation;
            return true;
        }

        public static bool TryGetMapCenter(Scene scene, out Vector3 mapCenter)
        {
            mapCenter = Vector3.zero;
            if (!scene.IsValid() || !scene.isLoaded)
            {
                return false;
            }

            GameObject[] rootObjects = scene.GetRootGameObjects();
            for (int i = 0; i < rootObjects.Length; i++)
            {
                Transform centerTransform = FindMapCenter(rootObjects[i].transform, requireActiveCenter: false);
                if (centerTransform == null)
                {
                    continue;
                }

                mapCenter = centerTransform.position;
                return true;
            }

            return false;
        }

        private static bool TryResolveLoadedSceneCenterRotation(Vector3 spawnPosition, out Quaternion centerRotation)
        {
            centerRotation = Quaternion.identity;
            if (TryGetLoadedSceneCenter(spawnPosition, requireActiveCenter: true, out centerRotation))
            {
                return true;
            }

            return TryGetLoadedSceneCenter(spawnPosition, requireActiveCenter: false, out centerRotation);
        }

        private static bool TryGetLoadedSceneCenter(
            Vector3 spawnPosition,
            bool requireActiveCenter,
            out Quaternion centerRotation)
        {
            centerRotation = Quaternion.identity;
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene loadedScene = SceneManager.GetSceneAt(i);
                if (!loadedScene.IsValid()
                    || !loadedScene.isLoaded
                    || !TryGetMapCenter(loadedScene, requireActiveCenter, out Vector3 mapCenter))
                {
                    continue;
                }

                Vector3 direction = mapCenter - spawnPosition;
                direction.y = 0f;
                if (direction.sqrMagnitude <= MinimumDirectionSqrMagnitude)
                {
                    continue;
                }

                centerRotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
                return true;
            }

            return false;
        }

        private static bool TryGetMapCenter(Scene scene, bool requireActiveCenter, out Vector3 mapCenter)
        {
            mapCenter = Vector3.zero;
            if (!scene.IsValid() || !scene.isLoaded)
            {
                return false;
            }

            GameObject[] rootObjects = scene.GetRootGameObjects();
            for (int i = 0; i < rootObjects.Length; i++)
            {
                Transform centerTransform = FindMapCenter(rootObjects[i].transform, requireActiveCenter);
                if (centerTransform == null)
                {
                    continue;
                }

                mapCenter = centerTransform.position;
                return true;
            }

            return false;
        }

        private static bool TryResolveCenterRotation(Scene scene, Vector3 spawnPosition, out Quaternion centerRotation)
        {
            centerRotation = Quaternion.identity;
            if (!TryGetMapCenter(scene, out Vector3 mapCenter))
            {
                return false;
            }

            Vector3 direction = mapCenter - spawnPosition;
            direction.y = 0f;
            if (direction.sqrMagnitude <= MinimumDirectionSqrMagnitude)
            {
                return false;
            }

            centerRotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
            return true;
        }

        private static Transform FindMapCenter(Transform currentTransform, bool requireActiveCenter)
        {
            if (currentTransform.name == MapCenterObjectName
                && (!requireActiveCenter || currentTransform.gameObject.activeInHierarchy))
            {
                return currentTransform;
            }

            for (int i = 0; i < currentTransform.childCount; i++)
            {
                Transform centerTransform = FindMapCenter(currentTransform.GetChild(i), requireActiveCenter);
                if (centerTransform != null)
                {
                    return centerTransform;
                }
            }

            return null;
        }
    }
}
