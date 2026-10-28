using System.Collections.Generic;
using Game.Scripts.UI.MainMenu;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Game.Scripts.Editor
{
    public static class ProfileStatisticsPopupRebuilder
    {
        private const string ClientScenePath = "Assets/Game/Scenes/Client.unity";
        private const string InfoPanelPath = "Assets/Game/Content/UI/Backgrounds/InfoPanel.png";
        private const string ButtonPath = "Assets/Game/Content/UI/Textures/Buttons/Button_Default.png";
        private const string FontPath = "Assets/Game/Content/Fonts/RobotoCondensed-Regular SDF.asset";

        [MenuItem("War Of Machines/UI/Rebuild Profile Statistics Popup")]
        public static void RebuildProfileStatisticsPopup()
        {
            Scene scene = EditorSceneManager.OpenScene(ClientScenePath, OpenSceneMode.Single);
            Canvas canvas = FindSceneObject<Canvas>("Canvas");
            MainMenu mainMenu = FindSceneObject<MainMenu>("MainMenu");
            if (canvas == null || mainMenu == null)
            {
                Debug.LogError("Profile popup rebuild requires Canvas and MainMenu in Client.unity.");
                return;
            }

            ProfileStatisticsPopup[] oldPopups = Object.FindObjectsByType<ProfileStatisticsPopup>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int index = 0; index < oldPopups.Length; index++)
            {
                if (oldPopups[index] != null)
                {
                    Object.DestroyImmediate(oldPopups[index].gameObject);
                }
            }

            Sprite infoPanelSprite = AssetDatabase.LoadAssetAtPath<Sprite>(InfoPanelPath);
            Sprite buttonSprite = AssetDatabase.LoadAssetAtPath<Sprite>(ButtonPath);
            TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);

            GameObject rootObject = new GameObject("ProfileStatisticsPopup", typeof(RectTransform), typeof(Image));
            rootObject.transform.SetParent(canvas.transform, false);
            RectTransform rootTransform = rootObject.transform as RectTransform;
            SetStretch(rootTransform);
            Image rootImage = rootObject.GetComponent<Image>();
            rootImage.color = new Color(0.015f, 0.025f, 0.035f, 0.78f);

            ProfileStatisticsPopup popup = rootObject.AddComponent<ProfileStatisticsPopup>();
            RectTransform window = CreateImage("ProfileStatisticsWindow", rootTransform, new Vector2(1180f, 720f), new Vector2(0f, 0f), infoPanelSprite, new Color(0.19f, 0.25f, 0.29f, 0.98f));

            TMP_Text title = CreateText("Title", window, new Vector2(0f, 320f), new Vector2(850f, 42f), "PROFILE // LIVE", font, 28f, new Color(0.94f, 0.84f, 0.52f, 1f), TextAlignmentOptions.Center);
            Button closeButton = CreateButton("Close", window, new Vector2(500f, 320f), new Vector2(125f, 42f), "CLOSE", buttonSprite, font, new Color(0.44f, 0.21f, 0.16f, 1f));

            List<TMP_Text> generalValues = new List<TMP_Text>();
            string[] generalLabels = { "BATTLES", "WINS", "LOSSES", "WIN RATE", "MMR RATING" };
            for (int index = 0; index < generalLabels.Length; index++)
            {
                float x = -420f + index * 210f;
                generalValues.Add(CreateStatCard(window, "GeneralStat_" + generalLabels[index], new Vector2(x, 245f), new Vector2(190f, 94f), generalLabels[index], font));
            }

            RectTransform playedPanel = CreateImage("RobotsPlayedPanel", window, new Vector2(-365f, -35f), new Vector2(350f, 470f), infoPanelSprite, new Color(0.10f, 0.22f, 0.21f, 0.98f));
            CreateText("RobotsPlayedHeader", playedPanel, new Vector2(0f, 204f), new Vector2(300f, 32f), "ROBOTS PLAYED", font, 20f, new Color(0.94f, 0.84f, 0.52f, 1f), TextAlignmentOptions.Center);

            GameObject viewportObject = new GameObject("RobotsPlayedViewport", typeof(RectTransform), typeof(Image), typeof(RectMask2D), typeof(ScrollRect));
            viewportObject.transform.SetParent(playedPanel, false);
            RectTransform viewport = viewportObject.transform as RectTransform;
            SetRect(viewport, new Vector2(318f, 392f), new Vector2(0f, -30f));
            Image viewportImage = viewportObject.GetComponent<Image>();
            viewportImage.color = new Color(0f, 0f, 0f, 0.12f);
            ScrollRect scrollRect = viewportObject.GetComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.inertia = true;
            scrollRect.scrollSensitivity = 35f;
            scrollRect.viewport = viewport;

            RectTransform content = CreateRect("RobotsPlayedContent", viewport);
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = new Vector2(0f, 0f);

            VerticalLayoutGroup layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(8, 8, 8, 8);
            layout.spacing = 6f;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            ContentSizeFitter fitter = content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scrollRect.content = content;

            RectTransform rowTemplateTransform = CreateImage("ProfileVehicleRowTemplate", content, new Vector2(0f, 0f), new Vector2(0f, 0f), buttonSprite, new Color(0.20f, 0.29f, 0.17f, 0.95f));
            rowTemplateTransform.anchorMin = new Vector2(0f, 1f);
            rowTemplateTransform.anchorMax = new Vector2(1f, 1f);
            rowTemplateTransform.pivot = new Vector2(0.5f, 1f);
            rowTemplateTransform.sizeDelta = new Vector2(0f, 54f);
            LayoutElement rowLayout = rowTemplateTransform.gameObject.AddComponent<LayoutElement>();
            rowLayout.minHeight = 54f;
            rowLayout.preferredHeight = 54f;
            rowLayout.flexibleHeight = 0f;
            Button rowButton = rowTemplateTransform.gameObject.AddComponent<Button>();
            Image rowImage = rowTemplateTransform.GetComponent<Image>();
            rowButton.targetGraphic = rowImage;
            rowButton.transition = Selectable.Transition.ColorTint;
            TMP_Text rowLabel = CreateText("Label", rowTemplateTransform, Vector2.zero, new Vector2(0f, 44f), "ROBOT", font, 17f, new Color(0.94f, 0.93f, 0.83f, 1f), TextAlignmentOptions.Center);
            rowLabel.raycastTarget = false;
            ProfileVehicleRow rowPresenter = rowTemplateTransform.gameObject.AddComponent<ProfileVehicleRow>();
            ConfigureRow(rowPresenter, rowButton, rowLabel, rowImage);
            rowTemplateTransform.gameObject.SetActive(false);

            RectTransform detailsPanel = CreateImage("VehicleStatisticsPanel", window, new Vector2(285f, -35f), new Vector2(870f, 470f), infoPanelSprite, new Color(0.12f, 0.17f, 0.25f, 0.98f));
            CreateText("VehicleStatisticsHeader", detailsPanel, new Vector2(0f, 204f), new Vector2(600f, 32f), "VEHICLE STATISTICS", font, 20f, new Color(0.94f, 0.84f, 0.52f, 1f), TextAlignmentOptions.Center);
            Image preview = CreateImage("VehiclePreview", detailsPanel, new Vector2(0f, 82f), new Vector2(600f, 170f), null, new Color(0.04f, 0.08f, 0.13f, 0.72f)).GetComponent<Image>();
            preview.enabled = false;
            TMP_Text vehicleName = CreateText("VehicleName", detailsPanel, new Vector2(0f, -18f), new Vector2(600f, 38f), "SELECT A ROBOT", font, 25f, Color.white, TextAlignmentOptions.Center);
            TMP_Text vehicleTier = CreateText("VehicleTier", detailsPanel, new Vector2(0f, -50f), new Vector2(300f, 28f), "", font, 16f, new Color(0.68f, 0.87f, 0.35f, 1f), TextAlignmentOptions.Center);

            List<TMP_Text> vehicleValues = new List<TMP_Text>();
            string[] vehicleLabels = { "BATTLES", "KILLS", "WIN RATE", "AVG DAMAGE" };
            for (int index = 0; index < vehicleLabels.Length; index++)
            {
                float x = -300f + index * 200f;
                vehicleValues.Add(CreateDetailCard(detailsPanel, "VehicleStat_" + vehicleLabels[index], new Vector2(x, -125f), new Vector2(180f, 72f), vehicleLabels[index], font));
            }

            RectTransform recordPanel = CreateImage("RecordPanel", window, new Vector2(0f, -300f), new Vector2(1040f, 76f), infoPanelSprite, new Color(0.17f, 0.24f, 0.16f, 0.98f));
            TMP_Text recordLabel = CreateText("RecordLabel", recordPanel, new Vector2(0f, 22f), new Vector2(420f, 24f), "RECORD", font, 15f, Color.white, TextAlignmentOptions.Center);
            TMP_Text winsLabel = CreateText("WinsLabel", recordPanel, new Vector2(-330f, -22f), new Vector2(220f, 22f), "0 WINS", font, 14f, new Color(0.55f, 0.95f, 0.36f, 1f), TextAlignmentOptions.Center);
            TMP_Text lossesLabel = CreateText("LossesLabel", recordPanel, new Vector2(330f, -22f), new Vector2(220f, 22f), "0 LOSSES", font, 14f, new Color(0.97f, 0.37f, 0.32f, 1f), TextAlignmentOptions.Center);
            Image recordBarTrack = CreateImage("RecordBarTrack", recordPanel, new Vector2(0f, -2f), new Vector2(400f, 14f), buttonSprite, new Color(0.04f, 0.07f, 0.06f, 0.96f)).GetComponent<Image>();
            recordBarTrack.raycastTarget = false;
            Image winsBar = CreateImage("WinsBar", recordPanel, new Vector2(0f, -2f), new Vector2(400f, 14f), buttonSprite, new Color(0.32f, 0.85f, 0.20f, 1f)).GetComponent<Image>();
            Image lossesBar = CreateImage("LossesBar", recordPanel, new Vector2(0f, -2f), new Vector2(400f, 14f), buttonSprite, new Color(0.85f, 0.22f, 0.18f, 1f)).GetComponent<Image>();
            ConfigureFilledBar(winsBar, Image.OriginHorizontal.Left);
            ConfigureFilledBar(lossesBar, Image.OriginHorizontal.Right);

            ConfigurePopup(popup, title, generalValues.ToArray(), rowPresenter, content, scrollRect, vehicleName, vehicleTier, vehicleValues.ToArray(), preview, recordLabel, winsBar, lossesBar, winsLabel, lossesLabel, closeButton);
            ConfigureMainMenu(mainMenu, rootObject, popup);
            rootObject.SetActive(false);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Selection.activeGameObject = rootObject;
            Debug.Log("Rebuilt ProfileStatisticsPopup in Client.unity.");
        }

        private static T FindSceneObject<T>(string objectName) where T : Component
        {
            T[] objects = Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int index = 0; index < objects.Length; index++)
            {
                if (objects[index] != null && objects[index].gameObject.name == objectName)
                {
                    return objects[index];
                }
            }

            return null;
        }

        private static RectTransform CreateRect(string objectName, Transform parent)
        {
            GameObject gameObject = new GameObject(objectName, typeof(RectTransform));
            gameObject.transform.SetParent(parent, false);
            return gameObject.transform as RectTransform;
        }

        private static RectTransform CreateImage(string objectName, Transform parent, Vector2 position, Vector2 size, Sprite sprite, Color color)
        {
            RectTransform rectTransform = CreateRect(objectName, parent);
            SetRect(rectTransform, size, position);
            Image image = rectTransform.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.type = sprite != null ? Image.Type.Sliced : Image.Type.Simple;
            return rectTransform;
        }

        private static TMP_Text CreateText(string objectName, Transform parent, Vector2 position, Vector2 size, string value, TMP_FontAsset font, float fontSize, Color color, TextAlignmentOptions alignment)
        {
            RectTransform rectTransform = CreateRect(objectName, parent);
            SetRect(rectTransform, size, position);
            TextMeshProUGUI text = rectTransform.gameObject.AddComponent<TextMeshProUGUI>();
            text.text = value;
            text.font = font;
            text.fontSize = fontSize;
            text.color = color;
            text.alignment = alignment;
            text.enableWordWrapping = false;
            text.raycastTarget = false;
            return text;
        }

        private static TMP_Text CreateStatCard(Transform parent, string objectName, Vector2 position, Vector2 size, string label, TMP_FontAsset font)
        {
            RectTransform card = CreateImage(objectName, parent, position, size, null, new Color(0.08f, 0.17f, 0.18f, 0.96f));
            CreateText("Label", card, new Vector2(0f, 19f), new Vector2(size.x - 12f, 22f), label, font, 13f, new Color(0.78f, 0.84f, 0.73f, 1f), TextAlignmentOptions.Center);
            return CreateText("Value", card, new Vector2(0f, -12f), new Vector2(size.x - 12f, 32f), "-", font, 23f, Color.white, TextAlignmentOptions.Center);
        }

        private static TMP_Text CreateDetailCard(Transform parent, string objectName, Vector2 position, Vector2 size, string label, TMP_FontAsset font)
        {
            RectTransform card = CreateImage(objectName, parent, position, size, null, new Color(0.10f, 0.16f, 0.25f, 0.98f));
            CreateText("Label", card, new Vector2(0f, 15f), new Vector2(size.x - 10f, 20f), label, font, 12f, new Color(0.72f, 0.82f, 0.92f, 1f), TextAlignmentOptions.Center);
            return CreateText("Value", card, new Vector2(0f, -12f), new Vector2(size.x - 10f, 30f), "-", font, 21f, Color.white, TextAlignmentOptions.Center);
        }

        private static Button CreateButton(string objectName, Transform parent, Vector2 position, Vector2 size, string label, Sprite sprite, TMP_FontAsset font, Color color)
        {
            RectTransform rectTransform = CreateImage(objectName, parent, position, size, sprite, color);
            Image image = rectTransform.GetComponent<Image>();
            Button button = rectTransform.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.transition = Selectable.Transition.ColorTint;
            CreateText("Label", rectTransform, Vector2.zero, size, label, font, 15f, Color.white, TextAlignmentOptions.Center);
            return button;
        }

        private static void ConfigureRow(ProfileVehicleRow row, Button button, TMP_Text label, Image background)
        {
            SerializedObject serializedObject = new SerializedObject(row);
            serializedObject.FindProperty("button").objectReferenceValue = button;
            serializedObject.FindProperty("label").objectReferenceValue = label;
            serializedObject.FindProperty("background").objectReferenceValue = background;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigurePopup(ProfileStatisticsPopup popup, TMP_Text title, TMP_Text[] generalValues, ProfileVehicleRow rowTemplate, RectTransform content, ScrollRect scrollRect, TMP_Text vehicleName, TMP_Text vehicleTier, TMP_Text[] vehicleValues, Image preview, TMP_Text recordLabel, Image winsBar, Image lossesBar, TMP_Text winsLabel, TMP_Text lossesLabel, Button closeButton)
        {
            SerializedObject serializedObject = new SerializedObject(popup);
            serializedObject.FindProperty("title").objectReferenceValue = title;
            SetObjectArray(serializedObject, "generalStatValues", generalValues);
            serializedObject.FindProperty("vehicleRowTemplate").objectReferenceValue = rowTemplate;
            serializedObject.FindProperty("vehicleRowContent").objectReferenceValue = content;
            serializedObject.FindProperty("vehicleScrollRect").objectReferenceValue = scrollRect;
            serializedObject.FindProperty("vehicleName").objectReferenceValue = vehicleName;
            serializedObject.FindProperty("vehicleTier").objectReferenceValue = vehicleTier;
            SetObjectArray(serializedObject, "vehicleStatValues", vehicleValues);
            serializedObject.FindProperty("vehiclePreviewPlaceholder").objectReferenceValue = preview;
            serializedObject.FindProperty("recordLabel").objectReferenceValue = recordLabel;
            serializedObject.FindProperty("winsBar").objectReferenceValue = winsBar;
            serializedObject.FindProperty("lossesBar").objectReferenceValue = lossesBar;
            serializedObject.FindProperty("winsLabel").objectReferenceValue = winsLabel;
            serializedObject.FindProperty("lossesLabel").objectReferenceValue = lossesLabel;
            serializedObject.FindProperty("closeButton").objectReferenceValue = closeButton;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigureMainMenu(MainMenu mainMenu, GameObject popupObject, ProfileStatisticsPopup popup)
        {
            SerializedObject serializedObject = new SerializedObject(mainMenu);
            serializedObject.FindProperty("profileStatisticsPopup").objectReferenceValue = popupObject;
            serializedObject.FindProperty("profileStatisticsPopupView").objectReferenceValue = popup;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetObjectArray(SerializedObject serializedObject, string propertyName, Object[] values)
        {
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            property.arraySize = values.Length;
            for (int index = 0; index < values.Length; index++)
            {
                property.GetArrayElementAtIndex(index).objectReferenceValue = values[index];
            }
        }

        private static void SetRect(RectTransform rectTransform, Vector2 size, Vector2 position)
        {
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = position;
            rectTransform.sizeDelta = size;
        }

        private static void SetStretch(RectTransform rectTransform)
        {
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
        }

        private static void ConfigureFilledBar(Image image, Image.OriginHorizontal origin)
        {
            image.type = Image.Type.Filled;
            image.fillMethod = Image.FillMethod.Horizontal;
            image.fillOrigin = (int)origin;
            image.fillAmount = 0f;
            image.raycastTarget = false;
        }
    }
}
