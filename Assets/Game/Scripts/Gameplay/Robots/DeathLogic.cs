using System.Collections;
using System.Collections.Generic;
using Game.Scripts.Gameplay.Robots;
using Game.Scripts.World.Maps;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

public class DeathLogic : MonoBehaviour, IVehicleRootAware
{
    [System.Serializable]
    public sealed class DetachableVisual
    {
        public GameObject root;
        public Collider collider;
        public Rigidbody rigidbody;
        public Renderer renderer;
        public MeshCollider meshCollider;
        public Behaviour[] behavioursToDisable = System.Array.Empty<Behaviour>();
    }

    private const string DeathDebrisLayerName = "Chassis";
    private static readonly int BaseColorPropertyId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorPropertyId = Shader.PropertyToID("_Color");
    private static readonly int SurfacePropertyId = Shader.PropertyToID("_Surface");
    private static readonly int BlendPropertyId = Shader.PropertyToID("_Blend");
    private static readonly int AlphaClipPropertyId = Shader.PropertyToID("_AlphaClip");
    private static readonly int SrcBlendPropertyId = Shader.PropertyToID("_SrcBlend");
    private static readonly int DstBlendPropertyId = Shader.PropertyToID("_DstBlend");
    private static readonly int ZWritePropertyId = Shader.PropertyToID("_ZWrite");

    public DetachableVisual[] detachableVisuals = System.Array.Empty<DetachableVisual>();
    public Collider[] colliders;
    public Collider[] collidersToDisableOnDeath;
    public Rigidbody[] debrisRigidbodies;
    public Renderer[] debrisRenderers;
    public MeshCollider[] debrisMeshColliders;
    public VehicleRoot vehicleRoot;
    public GameObject[] forTurnOff;

    private readonly List<GameObject> _detachedDebrisObjects = new List<GameObject>(32);
    private readonly List<Collider> _detachedDebrisColliders = new List<Collider>(32);
    private readonly List<Renderer> _detachedDebrisRenderers = new List<Renderer>(32);
    private readonly List<FadeMaterial> _fadeMaterials = new List<FadeMaterial>(8);
    private bool _deathHandled;
    private bool _debrisDestroyed;

    public void SetVehicleRoot(VehicleRoot root)
    {
        vehicleRoot = root;
    }

    private void Start()
    {
        if (vehicleRoot == null || vehicleRoot.health == null)
        {
            enabled = false;
            return;
        }

        vehicleRoot.health.onDeath.AddListener(Death);
    }

    private void OnDestroy()
    {
        if (vehicleRoot != null && vehicleRoot.health != null)
        {
            vehicleRoot.health.onDeath.RemoveListener(Death);
        }

        DestroyDetachedDebris();
    }

    [Button]
    private void TurnOffConvex()
    {
        foreach (Collider col in colliders)
        {
            if (col != null)
            {
                col.enabled = false;
            }
        }
    }

    private void Death()
    {
        if (_deathHandled)
        {
            return;
        }

        _deathHandled = true;

        if (vehicleRoot != null && vehicleRoot.inputManager != null)
        {
            vehicleRoot.inputManager.SetControlsBlocked(true);
        }

        DisableVehicleAnimationControllers();
        ClearEditorSelectionForDyingVehicle();

        bool renderDebris = true;
        if (vehicleRoot != null && vehicleRoot.clientVisibility != null)
        {
            renderDebris = vehicleRoot.clientVisibility.ReleaseRenderersForDeath();
        }

        int debrisLayer = ResolveDeathDebrisLayer();
        if (vehicleRoot != null && vehicleRoot.armorController != null)
        {
            vehicleRoot.armorController.SetArmorCollidersEnabled(false);
        }

        DisableConfiguredColliders(collidersToDisableOnDeath);
        Scene mapScene = MapScopedObjectRegistry.ResolveMapScene(GetOwningScene());

        if (detachableVisuals != null && detachableVisuals.Length > 0)
        {
            DetachConfiguredVisuals(mapScene, debrisLayer, renderDebris);
        }
        else
        {
            DetachLegacyDebris(mapScene, debrisLayer, renderDebris);
        }

        if (forTurnOff != null)
        {
            foreach (GameObject obj in forTurnOff)
            {
                if (obj != null)
                {
                    obj.SetActive(false);
                }
            }
        }

        StartCoroutine(FadeAndDestroyDebris());
    }

    private void DetachConfiguredVisuals(Scene mapScene, int debrisLayer, bool renderDebris)
    {
        for (int i = 0; i < detachableVisuals.Length; i++)
        {
            DetachableVisual visual = detachableVisuals[i];
            if (visual == null || visual.root == null)
            {
                if (HasConfiguredDetachableVisualReferences(visual))
                {
                    Debug.LogError($"{nameof(DeathLogic)} on {name} has an unconfigured detachable visual at index {i}.", this);
                }

                continue;
            }

            if (visual.collider == null || visual.rigidbody == null)
            {
                Debug.LogError($"{nameof(DeathLogic)} on {name} has no collider or Rigidbody configured for {visual.root.name}.", this);
                continue;
            }

            if (!IsConfiguredForDynamicRigidbody(visual.collider, visual.meshCollider))
            {
                Debug.LogError($"{nameof(DeathLogic)} on {name} has non-convex debris MeshCollider {visual.collider.name}. Configure it as convex in the prefab before using it with a dynamic Rigidbody.", this);
                continue;
            }

            visual.root.transform.SetParent(null, true);
            MapScopedObjectRegistry.Register(mapScene, visual.root);
            MapScopedObjectRegistry.MoveRootToScene(mapScene, visual.root);
            SetLayerRecursively(visual.root.transform, debrisLayer);

            RegisterDetachedDebris(visual.root, visual.collider, visual.renderer);

            DisableConfiguredBehaviours(visual.behavioursToDisable);
            visual.rigidbody.isKinematic = false;
            visual.collider.enabled = true;

            if (visual.renderer != null)
            {
                visual.renderer.enabled = true;
                visual.renderer.forceRenderingOff = !renderDebris;
            }
        }
    }

    private static bool HasConfiguredDetachableVisualReferences(DetachableVisual visual)
    {
        if (visual == null)
        {
            return false;
        }

        if (visual.collider != null || visual.rigidbody != null || visual.renderer != null || visual.meshCollider != null)
        {
            return true;
        }

        Behaviour[] configuredBehaviours = visual.behavioursToDisable;
        if (configuredBehaviours == null)
        {
            return false;
        }

        for (int i = 0; i < configuredBehaviours.Length; i++)
        {
            if (configuredBehaviours[i] != null)
            {
                return true;
            }
        }

        return false;
    }

    private void DetachLegacyDebris(Scene mapScene, int debrisLayer, bool renderDebris)
    {
        if (colliders == null)
        {
            return;
        }

        for (int i = 0; i < colliders.Length; i++)
        {
            Collider coll = colliders[i];
            if (coll == null)
            {
                continue;
            }

            Rigidbody rigidbody = debrisRigidbodies != null && i < debrisRigidbodies.Length
                ? debrisRigidbodies[i]
                : null;
            if (rigidbody == null)
            {
                Debug.LogError($"{nameof(DeathLogic)} on {name} has no configured debris Rigidbody for {coll.name}.", this);
                continue;
            }

            MeshCollider meshCollider = debrisMeshColliders != null && i < debrisMeshColliders.Length
                ? debrisMeshColliders[i]
                : null;
            if (!IsConfiguredForDynamicRigidbody(coll, meshCollider))
            {
                Debug.LogError($"{nameof(DeathLogic)} on {name} has non-convex debris MeshCollider {coll.name}. Configure it as convex in the prefab before using it with a dynamic Rigidbody.", this);
                continue;
            }

            GameObject debrisObject = coll.gameObject;
            coll.transform.SetParent(null, true);
            MapScopedObjectRegistry.Register(mapScene, debrisObject);
            MapScopedObjectRegistry.MoveRootToScene(mapScene, debrisObject);
            SetLayerRecursively(coll.transform, debrisLayer);

            rigidbody.isKinematic = false;
            coll.enabled = true;

            Renderer obj = debrisRenderers != null && i < debrisRenderers.Length
                ? debrisRenderers[i]
                : null;
            RegisterDetachedDebris(debrisObject, coll, obj);

            if (obj != null)
            {
                obj.enabled = true;
                obj.forceRenderingOff = !renderDebris;
            }
        }
    }

    private IEnumerator FadeAndDestroyDebris()
    {
        yield return new WaitForSecondsRealtime(VehicleHealth.DeathDebrisDelaySeconds);

        PrepareFadeMaterials();

        float elapsedSeconds = 0f;
        while (elapsedSeconds < VehicleHealth.DeathDebrisFadeDurationSeconds)
        {
            elapsedSeconds += Time.unscaledDeltaTime;
            ApplyFade(1f - Mathf.Clamp01(elapsedSeconds / VehicleHealth.DeathDebrisFadeDurationSeconds));
            yield return null;
        }

        ApplyFade(0f);
        DestroyDetachedDebris();
    }

    private void RegisterDetachedDebris(GameObject debrisObject, Collider debrisCollider, Renderer debrisRenderer)
    {
        if (debrisObject != null && !_detachedDebrisObjects.Contains(debrisObject))
        {
            _detachedDebrisObjects.Add(debrisObject);
        }

        if (debrisCollider != null && !_detachedDebrisColliders.Contains(debrisCollider))
        {
            _detachedDebrisColliders.Add(debrisCollider);
        }

        if (debrisRenderer != null && !_detachedDebrisRenderers.Contains(debrisRenderer))
        {
            _detachedDebrisRenderers.Add(debrisRenderer);
        }
    }

    private void PrepareFadeMaterials()
    {
        for (int rendererIndex = 0; rendererIndex < _detachedDebrisRenderers.Count; rendererIndex++)
        {
            Renderer debrisRenderer = _detachedDebrisRenderers[rendererIndex];
            if (debrisRenderer == null || debrisRenderer.forceRenderingOff)
            {
                continue;
            }

            Material[] sourceMaterials = debrisRenderer.sharedMaterials;
            if (sourceMaterials == null || sourceMaterials.Length == 0)
            {
                continue;
            }

            Material[] fadeRendererMaterials = new Material[sourceMaterials.Length];
            for (int materialIndex = 0; materialIndex < sourceMaterials.Length; materialIndex++)
            {
                Material sourceMaterial = sourceMaterials[materialIndex];
                fadeRendererMaterials[materialIndex] = sourceMaterial == null
                    ? null
                    : GetOrCreateFadeMaterial(sourceMaterial).material;
            }

            debrisRenderer.sharedMaterials = fadeRendererMaterials;
        }
    }

    private FadeMaterial GetOrCreateFadeMaterial(Material sourceMaterial)
    {
        for (int materialIndex = 0; materialIndex < _fadeMaterials.Count; materialIndex++)
        {
            FadeMaterial fadeMaterial = _fadeMaterials[materialIndex];
            if (fadeMaterial.sourceMaterial == sourceMaterial)
            {
                return fadeMaterial;
            }
        }

        Material material = new Material(sourceMaterial);
        int colorPropertyId = GetColorPropertyId(material);
        Color initialColor = colorPropertyId >= 0 ? material.GetColor(colorPropertyId) : Color.white;
        ConfigureMaterialForFade(material);

        FadeMaterial createdFadeMaterial = new FadeMaterial(sourceMaterial, material, colorPropertyId, initialColor);
        _fadeMaterials.Add(createdFadeMaterial);
        return createdFadeMaterial;
    }

    private static int GetColorPropertyId(Material material)
    {
        if (material.HasProperty(BaseColorPropertyId))
        {
            return BaseColorPropertyId;
        }

        if (material.HasProperty(ColorPropertyId))
        {
            return ColorPropertyId;
        }

        return -1;
    }

    private static void ConfigureMaterialForFade(Material material)
    {
        if (!material.HasProperty(SurfacePropertyId))
        {
            return;
        }

        material.SetFloat(SurfacePropertyId, 1f);
        if (material.HasProperty(BlendPropertyId))
        {
            material.SetFloat(BlendPropertyId, 0f);
        }

        if (material.HasProperty(AlphaClipPropertyId))
        {
            material.SetFloat(AlphaClipPropertyId, 0f);
        }

        if (material.HasProperty(SrcBlendPropertyId))
        {
            material.SetInt(SrcBlendPropertyId, (int)BlendMode.SrcAlpha);
        }

        if (material.HasProperty(DstBlendPropertyId))
        {
            material.SetInt(DstBlendPropertyId, (int)BlendMode.OneMinusSrcAlpha);
        }

        if (material.HasProperty(ZWritePropertyId))
        {
            material.SetInt(ZWritePropertyId, 0);
        }

        material.SetOverrideTag("RenderType", "Transparent");
        material.DisableKeyword("_ALPHATEST_ON");
        material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        material.DisableKeyword("_ALPHAMODULATE_ON");
        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        material.SetShaderPassEnabled("DepthOnly", false);
        material.renderQueue = (int)RenderQueue.Transparent;
    }

    private void ApplyFade(float opacity)
    {
        for (int materialIndex = 0; materialIndex < _fadeMaterials.Count; materialIndex++)
        {
            FadeMaterial fadeMaterial = _fadeMaterials[materialIndex];
            if (fadeMaterial.material == null || fadeMaterial.colorPropertyId < 0)
            {
                continue;
            }

            Color color = fadeMaterial.initialColor;
            color.a *= opacity;
            fadeMaterial.material.SetColor(fadeMaterial.colorPropertyId, color);
        }
    }

    private void DestroyDetachedDebris()
    {
        if (_debrisDestroyed)
        {
            return;
        }

        _debrisDestroyed = true;

        for (int colliderIndex = 0; colliderIndex < _detachedDebrisColliders.Count; colliderIndex++)
        {
            Collider debrisCollider = _detachedDebrisColliders[colliderIndex];
            if (debrisCollider != null)
            {
                debrisCollider.enabled = false;
            }
        }

        for (int debrisIndex = 0; debrisIndex < _detachedDebrisObjects.Count; debrisIndex++)
        {
            GameObject debrisObject = _detachedDebrisObjects[debrisIndex];
            if (debrisObject != null)
            {
                Destroy(debrisObject);
            }
        }

        for (int materialIndex = 0; materialIndex < _fadeMaterials.Count; materialIndex++)
        {
            Material material = _fadeMaterials[materialIndex].material;
            if (material != null)
            {
                Destroy(material);
            }
        }

        _detachedDebrisObjects.Clear();
        _detachedDebrisColliders.Clear();
        _detachedDebrisRenderers.Clear();
        _fadeMaterials.Clear();
    }

    private sealed class FadeMaterial
    {
        public readonly Material sourceMaterial;
        public readonly Material material;
        public readonly int colorPropertyId;
        public readonly Color initialColor;

        public FadeMaterial(Material sourceMaterial, Material material, int colorPropertyId, Color initialColor)
        {
            this.sourceMaterial = sourceMaterial;
            this.material = material;
            this.colorPropertyId = colorPropertyId;
            this.initialColor = initialColor;
        }
    }

    private Scene GetOwningScene()
    {
        if (vehicleRoot != null)
        {
            Scene vehicleScene = vehicleRoot.gameObject.scene;
            if (vehicleScene.IsValid())
            {
                return vehicleScene;
            }
        }

        return gameObject.scene;
    }

    private static void DisableConfiguredColliders(Collider[] configuredColliders)
    {
        if (configuredColliders == null)
        {
            return;
        }

        for (int i = 0; i < configuredColliders.Length; i++)
        {
            Collider collider = configuredColliders[i];
            if (collider != null)
            {
                collider.enabled = false;
            }
        }
    }

    private static void DisableConfiguredBehaviours(Behaviour[] configuredBehaviours)
    {
        if (configuredBehaviours == null)
        {
            return;
        }

        for (int i = 0; i < configuredBehaviours.Length; i++)
        {
            Behaviour configuredBehaviour = configuredBehaviours[i];
            if (configuredBehaviour != null)
            {
                configuredBehaviour.enabled = false;
            }
        }
    }

    private void DisableVehicleAnimationControllers()
    {
        if (vehicleRoot == null)
        {
            return;
        }

        DisableBehaviour(vehicleRoot.trackedVehicleAnimator);
        DisableBehaviour(vehicleRoot.suspensionController);
        DisableBehaviour(vehicleRoot.walkerAnimationController);
        DisableBehaviour(vehicleRoot.groundAlignmentController);
        DisableBehaviour(vehicleRoot.bodySwayController);
        DisableBehaviour(vehicleRoot.bodyVerticalSpringController);
    }

    private static void DisableBehaviour(Behaviour behaviour)
    {
        if (behaviour != null)
        {
            behaviour.enabled = false;
        }
    }

    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    private void ClearEditorSelectionForDyingVehicle()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying || vehicleRoot == null)
        {
            return;
        }

        GameObject[] selectedObjects = UnityEditor.Selection.gameObjects;
        if (selectedObjects == null || selectedObjects.Length == 0)
        {
            return;
        }

        Transform vehicleTransform = vehicleRoot.transform;
        for (int i = 0; i < selectedObjects.Length; i++)
        {
            GameObject selectedObject = selectedObjects[i];
            if (selectedObject == null)
            {
                continue;
            }

            Transform selectedTransform = selectedObject.transform;
            if (selectedTransform == vehicleTransform || selectedTransform.IsChildOf(vehicleTransform))
            {
                UnityEditor.Selection.objects = System.Array.Empty<UnityEngine.Object>();
                return;
            }
        }
#endif
    }

    private static int ResolveDeathDebrisLayer()
    {
        int layer = LayerMask.NameToLayer(DeathDebrisLayerName);
        if (layer >= 0)
        {
            return layer;
        }

        return LayerMask.NameToLayer("Ignore Raycast");
    }

    private static void SetLayerRecursively(Transform root, int layer)
    {
        if (root == null || layer < 0)
        {
            return;
        }

        root.gameObject.layer = layer;
        for (int i = 0; i < root.childCount; i++)
        {
            SetLayerRecursively(root.GetChild(i), layer);
        }
    }

    private static bool IsConfiguredForDynamicRigidbody(Collider collider, MeshCollider configuredMeshCollider)
    {
        MeshCollider meshCollider = configuredMeshCollider;
        if (meshCollider == null)
        {
            meshCollider = collider as MeshCollider;
        }

        if (meshCollider == null)
        {
            return true;
        }

        return meshCollider.convex;
    }
}
