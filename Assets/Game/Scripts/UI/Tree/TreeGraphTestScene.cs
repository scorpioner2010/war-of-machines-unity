using System.Collections.Generic;
using RAV.Scripts.API;
using RAV.Scripts.Client;
using UnityEngine;
using UnityEngine.UI;

namespace RAV.Scripts.UI.Tree
{
    public sealed class TreeGraphTestScene : MonoBehaviour
    {
        [SerializeField] private RectTransform treeRoot;
        [SerializeField] private TreeItem treeItemPrefab;
        [SerializeField] private Image segmentPrefab;
        [SerializeField] private Image arrowHeadPrefab;
        [SerializeField] private Vector2 nodeSize = new Vector2(284f, 100f);
        [SerializeField] private float horizontalGap = 32f;
        [SerializeField] private float verticalGap = 80f;
        [SerializeField] private bool initializeOnStart = true;

        private readonly TreeGraphLayoutPlanner _layoutPlanner = new TreeGraphLayoutPlanner();
        private readonly TreeArrowRoutePlanner _routePlanner = new TreeArrowRoutePlanner();
        private readonly TreeArrowRouteValidator _routeValidator = new TreeArrowRouteValidator();
        private readonly TreeArrowRouteRenderer _routeRenderer = new TreeArrowRouteRenderer();
        private readonly Dictionary<int, RectTransform> _nodeMap = new Dictionary<int, RectTransform>();
        private RectTransform _arrowsLayer;
        private RectTransform _nodesLayer;

        public bool ValidationPassed { get; private set; }
        public string LastValidationError { get; private set; }

        private void Start()
        {
            if (initializeOnStart)
            {
                Initialize();
            }
        }

        public void Initialize()
        {
            ValidationPassed = false;
            LastValidationError = string.Empty;
            RectTransform root = treeRoot != null ? treeRoot : GetComponent<RectTransform>();
            if (root == null || treeItemPrefab == null || segmentPrefab == null || arrowHeadPrefab == null)
            {
                LastValidationError = "Test scene is missing its tree root or visual prefabs.";
                Debug.LogError(LastValidationError, this);
                return;
            }

            ClearGeneratedTree(root);
            _nodeMap.Clear();
            _arrowsLayer = CreateLayer(root, "ArrowsLayer");
            _nodesLayer = CreateLayer(root, "NodesLayer");

            VehicleGraph graph = TreeGraphTestFixture.CreateNovaGraphSnapshot();
            TreeGraphLayout layout = _layoutPlanner.Plan(graph, nodeSize, horizontalGap, verticalGap);
            CreateNodes(layout);

            Canvas.ForceUpdateCanvases();
            IReadOnlyList<TreeArrowRoute> routes = _routePlanner.Plan(
                graph.edges,
                _nodeMap,
                _arrowsLayer,
                GameplayRuntimeSettingsProvider.Get().treeArrowItemOffset,
                GameplayRuntimeSettingsProvider.Get().treeArrowShaftThickness);

            ValidationPassed = _routeValidator.TryValidate(routes, _nodeMap, _arrowsLayer, out string validationError);
            LastValidationError = ValidationPassed ? "OK" : validationError;
            if (!ValidationPassed)
            {
                Debug.LogError($"Progression tree route validation failed: {validationError}", this);
            }
            else
            {
                Debug.Log($"Progression tree route validation passed: {routes.Count} routes, bottom-to-top graph depth.", this);
            }

            GameplayRuntimeSettings settings = GameplayRuntimeSettingsProvider.Get();
            _routeRenderer.Draw(
                routes,
                _arrowsLayer,
                segmentPrefab,
                arrowHeadPrefab,
                settings.treeArrowShaftThickness,
                settings.treeArrowColor,
                settings.treeArrowHeadColor,
                settings.treeArrowHeadHeight,
                settings.treeArrowHeadScale,
                settings.treeArrowHeadOffset);
        }

        private void CreateNodes(TreeGraphLayout layout)
        {
            foreach (KeyValuePair<int, TreeGraphNodePlacement> placement in layout.Placements)
            {
                VehicleNode node = placement.Value.Node;
                TreeItem item = Instantiate(treeItemPrefab, _nodesLayer);
                item.name = $"Vehicle_{node.code}";
                RectTransform itemTransform = item.rectTransform != null
                    ? item.rectTransform
                    : item.GetComponent<RectTransform>();
                itemTransform.anchorMin = itemTransform.anchorMax = itemTransform.pivot = new Vector2(0.5f, 0.5f);
                itemTransform.anchoredPosition = placement.Value.Position;
                itemTransform.sizeDelta = nodeSize;

                if (item.vehicleName != null)
                {
                    item.vehicleName.text = node.name;
                }

                if (item.level != null)
                {
                    item.level.text = $"L{node.level}  /  D{placement.Value.Depth}";
                }

                item.SetSelected(false);
                item.ApplyVisualState(0, 0, false, false, !node.isVisible, !node.isVisible);
                _nodeMap[node.id] = itemTransform;
            }
        }

        private static RectTransform CreateLayer(RectTransform parent, string layerName)
        {
            GameObject layerObject = new GameObject(layerName, typeof(RectTransform));
            RectTransform layer = layerObject.GetComponent<RectTransform>();
            layer.SetParent(parent, false);
            layer.anchorMin = Vector2.zero;
            layer.anchorMax = Vector2.one;
            layer.offsetMin = Vector2.zero;
            layer.offsetMax = Vector2.zero;
            layer.pivot = new Vector2(0.5f, 0.5f);
            return layer;
        }

        private void ClearGeneratedTree(RectTransform root)
        {
            for (int childIndex = root.childCount - 1; childIndex >= 0; childIndex--)
            {
                Transform child = root.GetChild(childIndex);
                if (Application.isPlaying)
                {
                    Destroy(child.gameObject);
                }
                else
                {
                    DestroyImmediate(child.gameObject);
                }
            }
        }
    }
}
