using System.Collections.Generic;
using Game.Scripts.Diagnostics;
using Game.Scripts.Gameplay.Robots;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace Game.Scripts.Editor
{
    [InitializeOnLoad]
    public static class ArmorPrefabHighlighter
    {
        private const string EnabledPrefsKey = "WOM.ArmorPrefabHighlighter.Enabled";
        private const string MenuEnabled = "Tools/WOM/Armor Prefab Highlighter/Enabled";
        private const string MenuSelect = "Tools/WOM/Armor Prefab Highlighter/Select Armor Objects";

        private static readonly List<Transform> TransformBuffer = new List<Transform>(128);
        private static readonly List<Renderer> RendererBuffer = new List<Renderer>(64);
        private static readonly List<MeshCollider> MeshColliderBuffer = new List<MeshCollider>(64);
        private static readonly List<Object> SelectionBuffer = new List<Object>(64);
        private static readonly Color FillColor = new Color(1f, 0f, 0f, 0.22f);

        private static Material _highlightMaterial;
        private static bool _enabled;
        static ArmorPrefabHighlighter()
        {
            _enabled = EditorPrefs.GetBool(EnabledPrefsKey, true);
            SceneView.duringSceneGui -= OnSceneGUI;
            SceneView.duringSceneGui += OnSceneGUI;
        }

        [MenuItem(MenuEnabled)]
        private static void ToggleEnabled()
        {
            _enabled = !_enabled;
            EditorPrefs.SetBool(EnabledPrefsKey, _enabled);
            SceneView.RepaintAll();
        }

        [MenuItem(MenuEnabled, true)]
        private static bool ToggleEnabledValidate()
        {
            Menu.SetChecked(MenuEnabled, _enabled);
            return true;
        }

        [MenuItem(MenuSelect)]
        private static void SelectArmorObjects()
        {
            if (!TryGetPrefabRoot(out GameObject root))
            {
                return;
            }

            SelectionBuffer.Clear();
            TransformBuffer.Clear();
            root.GetComponentsInChildren(true, TransformBuffer);
            for (int i = 0; i < TransformBuffer.Count; i++)
            {
                Transform target = TransformBuffer[i];
                if (target != null && IsArmorGameObject(target.gameObject))
                {
                    SelectionBuffer.Add(target.gameObject);
                }
            }

            Selection.objects = SelectionBuffer.ToArray();
        }

        [MenuItem(MenuSelect, true)]
        private static bool SelectArmorObjectsValidate()
        {
            return TryGetPrefabRoot(out _);
        }

        private static void OnSceneGUI(SceneView sceneView)
        {
            using (ProfileScope.Measure("Editor.ArmorPrefabHighlighter.OnSceneGUI", DiagnosticsCategories.Editor))
            {
                if (!_enabled || Event.current == null || Event.current.type != EventType.Repaint)
                {
                    return;
                }

                if (!TryGetPrefabRoot(out GameObject root))
                {
                    return;
                }

                Material material = GetHighlightMaterial();
                if (material == null || !material.SetPass(0))
                {
                    return;
                }

                int drawnCount = 0;
                RendererBuffer.Clear();
                root.GetComponentsInChildren(true, RendererBuffer);
                for (int i = 0; i < RendererBuffer.Count; i++)
                {
                    Renderer renderer = RendererBuffer[i];
                    if (renderer == null || !IsArmorGameObject(renderer.gameObject))
                    {
                        continue;
                    }

                    drawnCount += DrawRenderer(renderer);
                }

                MeshColliderBuffer.Clear();
                root.GetComponentsInChildren(true, MeshColliderBuffer);
                for (int i = 0; i < MeshColliderBuffer.Count; i++)
                {
                    MeshCollider meshCollider = MeshColliderBuffer[i];
                    if (meshCollider == null
                        || meshCollider.sharedMesh == null
                        || meshCollider.GetComponent<Renderer>() != null
                        || !IsArmorGameObject(meshCollider.gameObject))
                    {
                        continue;
                    }

                    DrawMesh(meshCollider.sharedMesh, meshCollider.transform.localToWorldMatrix);
                    drawnCount++;
                }

                DrawSceneViewBadge(drawnCount);
            }
        }

        private static int DrawRenderer(Renderer renderer)
        {
            MeshRenderer meshRenderer = renderer as MeshRenderer;
            if (meshRenderer != null)
            {
                MeshFilter meshFilter = renderer.GetComponent<MeshFilter>();
                if (meshFilter == null || meshFilter.sharedMesh == null)
                {
                    return 0;
                }

                DrawMesh(meshFilter.sharedMesh, renderer.transform.localToWorldMatrix);
                return 1;
            }

            SkinnedMeshRenderer skinnedRenderer = renderer as SkinnedMeshRenderer;
            if (skinnedRenderer != null && skinnedRenderer.sharedMesh != null)
            {
                DrawMesh(skinnedRenderer.sharedMesh, renderer.transform.localToWorldMatrix);
                return 1;
            }

            return 0;
        }

        private static void DrawMesh(Mesh mesh, Matrix4x4 matrix)
        {
            int subMeshCount = Mathf.Max(1, mesh.subMeshCount);
            for (int i = 0; i < subMeshCount; i++)
            {
                Graphics.DrawMeshNow(mesh, matrix, i);
            }
        }

        private static bool IsArmorGameObject(GameObject target)
        {
            if (target == null)
            {
                return false;
            }

            ArmorMap armorMap = target.GetComponent<ArmorMap>();
            return armorMap != null
                   && armorMap.ArmorCollider != null
                   && armorMap.ArmorCollider.gameObject == target;
        }

        private static bool TryGetPrefabRoot(out GameObject root)
        {
            PrefabStage prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
            root = prefabStage != null ? prefabStage.prefabContentsRoot : null;
            return root != null;
        }

        private static Material GetHighlightMaterial()
        {
            if (_highlightMaterial != null)
            {
                return _highlightMaterial;
            }

            Shader shader = Shader.Find("Hidden/Internal-Colored");
            if (shader == null)
            {
                return null;
            }

            _highlightMaterial = new Material(shader)
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            _highlightMaterial.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
            _highlightMaterial.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
            _highlightMaterial.SetInt("_Cull", (int)CullMode.Off);
            _highlightMaterial.SetInt("_ZWrite", 0);
            _highlightMaterial.SetInt("_ZTest", (int)CompareFunction.Always);
            _highlightMaterial.SetColor("_Color", FillColor);
            return _highlightMaterial;
        }

        private static void DrawSceneViewBadge(int drawnCount)
        {
            Handles.BeginGUI();
            Rect rect = new Rect(8f, 8f, 210f, 24f);
            GUI.Box(rect, "Armor highlight: " + drawnCount);
            Handles.EndGUI();
        }
    }
}
