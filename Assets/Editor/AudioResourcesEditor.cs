using System;
using System.Collections.Generic;
using Game.Scripts.Audio;
using UnityEditor;
using UnityEngine;

namespace Toras.Editor
{
    [CustomEditor(typeof(AudioResources))]
    internal sealed class AudioResourcesInspector : UnityEditor.Editor
    {
        private const string CommonRobotGroup = "Common";
        private const string IronAllianceRobotGroup = "Iron Alliance";
        private const string NovaSyndicateRobotGroup = "Nova Syndicate";
        private const string MissingRobotGroup = "Missing Target";

        private SerializedProperty _uiClips;
        private SerializedProperty _soundCategories;
        private SerializedProperty _musicClips;
        private RobotRegistry _robotRegistry;
        private RobotTargetOption[] _factionTargetOptions;
        private readonly Dictionary<string, RobotTargetOption[]> _robotTargetOptionsCache =
            new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, string> _robotTargetLabels =
            new(StringComparer.OrdinalIgnoreCase);
        private bool _robotTargetCacheDirty;

        internal sealed class RobotTargetOption
        {
            public RobotTargetOption(string targetId, string label, string group)
            {
                TargetId = targetId;
                Label = label;
                Group = group;
            }

            public string TargetId { get; }
            public string Label { get; }
            public string Group { get; }
        }

        private void OnEnable()
        {
            _uiClips = serializedObject.FindProperty("uiClips");
            _soundCategories = serializedObject.FindProperty("soundCategories");
            _musicClips = serializedObject.FindProperty("musicClips");

            EditorApplication.projectChanged += InvalidateRobotTargetCache;
            Undo.undoRedoPerformed += InvalidateRobotTargetCache;
            RefreshRobotTargetCache();
        }

        private void OnDisable()
        {
            EditorApplication.projectChanged -= InvalidateRobotTargetCache;
            Undo.undoRedoPerformed -= InvalidateRobotTargetCache;
        }

        private void InvalidateRobotTargetCache()
        {
            _robotTargetCacheDirty = true;
        }

        private void EnsureRobotTargetCache()
        {
            if (_robotTargetCacheDirty)
            {
                RefreshRobotTargetCache();
            }
        }

        private void RefreshRobotTargetCache()
        {
            _robotRegistry = FindRobotRegistry();
            _factionTargetOptions = BuildFactionTargetOptions();
            _robotTargetOptionsCache.Clear();
            _robotTargetLabels.Clear();
            _robotTargetCacheDirty = false;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EnsureRobotTargetCache();

            EditorGUILayout.HelpBox(
                "Organized audio resources. Use the group headers to find sounds quickly, then edit volume, delay, clips, and preview them with Play.",
                MessageType.Info);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Set Missing Names"))
            {
                RunResourceAction("Set audio resource names", resource => resource.SetNames());
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.LabelField("UI Sounds", EditorStyles.boldLabel);
            DrawCollection(
                _uiClips,
                "UI Clips");

            EditorGUILayout.LabelField("Robot Sounds", EditorStyles.boldLabel);
            DrawCategoryCollection(_soundCategories);

            EditorGUILayout.LabelField("Music", EditorStyles.boldLabel);
            DrawCollection(
                _musicClips,
                "Music");

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawCollection(
            SerializedProperty collection,
            string title)
        {
            if (collection == null)
            {
                return;
            }

            string collectionKey = "AudioResources." + collection.propertyPath;
            bool storedExpanded = SessionState.GetBool(collectionKey, true);
            bool expanded = storedExpanded;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            expanded = EditorGUILayout.Foldout(
                expanded,
                new GUIContent(title + " (" + collection.arraySize + ")"),
                true);
            if (expanded != storedExpanded)
            {
                SessionState.SetBool(collectionKey, expanded);
            }

            if (expanded)
            {
                DrawArraySize(collection);

                DrawElements(collection, title);
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(EditorGUIUtility.standardVerticalSpacing);
        }

        private static void DrawArraySize(SerializedProperty collection)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Entries", GUILayout.Width(58f));
            int newSize = EditorGUILayout.IntField(collection.arraySize);
            if (newSize != collection.arraySize)
            {
                collection.arraySize = Mathf.Max(0, newSize);
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawCategoryCollection(SerializedProperty categories)
        {
            if (categories == null)
            {
                return;
            }

            string collectionKey = "AudioResources." + categories.propertyPath;
            bool storedExpanded = SessionState.GetBool(collectionKey, true);
            bool expanded = storedExpanded;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            expanded = EditorGUILayout.Foldout(
                expanded,
                new GUIContent("Robot Sound Categories (" + categories.arraySize + ")"),
                true);
            if (expanded != storedExpanded)
            {
                SessionState.SetBool(collectionKey, expanded);
            }

            if (expanded)
            {
                DrawArraySize(categories, "Categories");
                using (new EditorGUI.IndentLevelScope(1))
                {
                    for (int i = 0; i < categories.arraySize; i++)
                    {
                        DrawCategory(categories, i);
                    }
                }
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(EditorGUIUtility.standardVerticalSpacing);
        }

        private void DrawCategory(SerializedProperty categories, int index)
        {
            SerializedProperty category = categories.GetArrayElementAtIndex(index);
            SerializedProperty name = category.FindPropertyRelative("name");
            SerializedProperty factionTarget = category.FindPropertyRelative("factionTarget");
            SerializedProperty robotGroups = category.FindPropertyRelative("robotGroups");
            string categoryName = name != null && !string.IsNullOrWhiteSpace(name.stringValue)
                ? name.stringValue
                : "Category " + (index + 1);
            string categoryKey = "AudioResources." + category.propertyPath;
            bool storedExpanded = SessionState.GetBool(categoryKey, true);
            bool expanded = storedExpanded;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            expanded = EditorGUILayout.Foldout(
                expanded,
                new GUIContent(categoryName + " (" + (robotGroups == null ? 0 : robotGroups.arraySize) + " robot groups)"),
                true);
            if (expanded != storedExpanded)
            {
                SessionState.SetBool(categoryKey, expanded);
            }

            if (expanded)
            {
                EditorGUILayout.PropertyField(name, new GUIContent("Category Name"));
                if (factionTarget != null)
                {
                    DrawFactionTarget(factionTarget);
                }

                if (robotGroups != null)
                {
                    DrawArraySize(robotGroups, "Robot Groups");
                    using (new EditorGUI.IndentLevelScope(1))
                    {
                        for (int i = 0; i < robotGroups.arraySize; i++)
                        {
                            DrawRobotGroup(robotGroups, i, factionTarget, categoryName);
                        }
                    }
                }
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(EditorGUIUtility.standardVerticalSpacing);
        }

        private void DrawRobotGroup(
            SerializedProperty groups,
            int index,
            SerializedProperty factionTarget,
            string categoryName)
        {
            SerializedProperty group = groups.GetArrayElementAtIndex(index);
            SerializedProperty robotTarget = group.FindPropertyRelative("robotTarget");
            SerializedProperty clips = group.FindPropertyRelative("clips");
            string targetLabel = GetRobotTargetLabel(
                factionTarget == null ? AudioRobotTargetIds.Common : factionTarget.stringValue,
                robotTarget == null ? string.Empty : robotTarget.stringValue);
            string groupKey = "AudioResources." + group.propertyPath;
            bool storedExpanded = SessionState.GetBool(groupKey, true);
            bool expanded = storedExpanded;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            expanded = EditorGUILayout.Foldout(
                expanded,
                new GUIContent(targetLabel + " (" + (clips == null ? 0 : clips.arraySize) + " sounds)"),
                true);
            if (expanded != storedExpanded)
            {
                SessionState.SetBool(groupKey, expanded);
            }

            if (expanded)
            {
                if (robotTarget != null)
                {
                    DrawRobotTarget(factionTarget, robotTarget);
                }

                if (clips != null)
                {
                    DrawArraySize(clips, "Sounds");
                    DrawElements(clips, categoryName + " / " + targetLabel);
                }
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(EditorGUIUtility.standardVerticalSpacing);
        }

        private void DrawFactionTarget(SerializedProperty factionTarget)
        {
            if (string.IsNullOrWhiteSpace(factionTarget.stringValue))
            {
                factionTarget.stringValue = AudioRobotTargetIds.Common;
            }

            RobotTargetOption[] options = _factionTargetOptions;
            string[] labels = new string[options.Length];
            int selectedIndex = 0;
            for (int i = 0; i < options.Length; i++)
            {
                labels[i] = options[i].Label;
                if (options[i].TargetId == factionTarget.stringValue)
                {
                    selectedIndex = i;
                }
            }

            int nextIndex = EditorGUILayout.Popup("Faction", selectedIndex, labels);
            if (nextIndex >= 0
                && nextIndex < options.Length
                && !string.Equals(
                    factionTarget.stringValue,
                    options[nextIndex].TargetId,
                    StringComparison.Ordinal))
            {
                factionTarget.stringValue = options[nextIndex].TargetId;
            }
        }

        private void DrawRobotTarget(
            SerializedProperty factionTarget,
            SerializedProperty robotTarget)
        {
            string faction = factionTarget == null || string.IsNullOrWhiteSpace(factionTarget.stringValue)
                ? AudioRobotTargetIds.Common
                : factionTarget.stringValue;
            if (string.IsNullOrWhiteSpace(robotTarget.stringValue))
            {
                robotTarget.stringValue = faction;
            }

            RobotTargetOption[] options = GetRobotTargetOptions(faction, robotTarget.stringValue);
            string[] labels = new string[options.Length];
            int selectedIndex = 0;
            for (int i = 0; i < options.Length; i++)
            {
                labels[i] = options[i].Label;
                if (options[i].TargetId == robotTarget.stringValue)
                {
                    selectedIndex = i;
                }
            }

            int nextIndex = EditorGUILayout.Popup("Robot", selectedIndex, labels);
            if (nextIndex >= 0
                && nextIndex < options.Length
                && !string.Equals(
                    robotTarget.stringValue,
                    options[nextIndex].TargetId,
                    StringComparison.Ordinal))
            {
                robotTarget.stringValue = options[nextIndex].TargetId;
            }
        }

        private static void DrawArraySize(SerializedProperty collection, string label)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(label, GUILayout.Width(72f));
            int newSize = EditorGUILayout.IntField(collection.arraySize);
            if (newSize != collection.arraySize)
            {
                collection.arraySize = Mathf.Max(0, newSize);
            }

            EditorGUILayout.EndHorizontal();
        }

        private static void DrawElements(
            SerializedProperty collection,
            string groupName)
        {
            using (new EditorGUI.IndentLevelScope(1))
            {
                bool removedElement = false;
                for (int i = 0; i < collection.arraySize; i++)
                {
                    SerializedProperty element = collection.GetArrayElementAtIndex(i);
                    string elementName = GetElementName(element, i);
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.PropertyField(
                        element,
                        new GUIContent(elementName + "  [" + groupName + "]"),
                        true);
                    if (GUILayout.Button("Remove", GUILayout.Width(70f)))
                    {
                        collection.DeleteArrayElementAtIndex(i);
                        removedElement = true;
                    }

                    EditorGUILayout.EndHorizontal();
                    if (removedElement)
                    {
                        break;
                    }
                }
            }
        }

        private void RunResourceAction(string actionName, Action<AudioResources> action)
        {
            serializedObject.ApplyModifiedProperties();

            for (int i = 0; i < targets.Length; i++)
            {
                if (targets[i] is not AudioResources resource)
                {
                    continue;
                }

                Undo.RecordObject(resource, actionName);
                action(resource);
                EditorUtility.SetDirty(resource);
            }

            serializedObject.Update();
        }

        private RobotTargetOption[] BuildFactionTargetOptions()
        {
            List<RobotTargetOption> options = new List<RobotTargetOption>
            {
                new RobotTargetOption(
                    AudioRobotTargetIds.Common,
                    "Common — All Robots",
                    CommonRobotGroup),
                new RobotTargetOption(
                    AudioRobotTargetIds.IronAllianceAll,
                    "Iron Alliance — All Robots",
                    IronAllianceRobotGroup),
                new RobotTargetOption(
                    AudioRobotTargetIds.NovaSyndicateAll,
                    "Nova Syndicate — All Robots",
                    NovaSyndicateRobotGroup)
            };
            HashSet<string> knownFactions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "ia",
                "nv"
            };

            if (_robotRegistry != null && _robotRegistry.items != null)
            {
                for (int i = 0; i < _robotRegistry.items.Count; i++)
                {
                    RobotRegistry.Item item = _robotRegistry.items[i];
                    if (item == null
                        || item.isDevelopmentOnly
                        || string.IsNullOrWhiteSpace(item.code)
                        || item.prefab == null)
                    {
                        continue;
                    }

                    string factionKey = GetRobotFactionKey(item.code);
                    if (!string.IsNullOrEmpty(factionKey) && knownFactions.Add(factionKey))
                    {
                        options.Add(new RobotTargetOption(
                            GetRobotFactionTargetId(factionKey),
                            GetRobotFactionLabel(factionKey),
                            factionKey));
                    }

                }
            }

            return options.ToArray();
        }

        private RobotTargetOption[] GetRobotTargetOptions(
            string factionTarget,
            string currentTarget)
        {
            string cacheKey = factionTarget ?? string.Empty;
            if (!_robotTargetOptionsCache.TryGetValue(cacheKey, out RobotTargetOption[] options))
            {
                string factionKey = GetRobotFactionKeyFromTarget(factionTarget);
                List<RobotTargetOption> targetOptions = new List<RobotTargetOption>();

                if (string.IsNullOrEmpty(factionKey))
                {
                    targetOptions.Add(new RobotTargetOption(
                        AudioRobotTargetIds.Common,
                        "All Robots",
                        CommonRobotGroup));
                }
                else
                {
                    targetOptions.Add(new RobotTargetOption(
                        factionTarget,
                        GetRobotFactionLabel(factionKey),
                        factionKey));

                    if (_robotRegistry != null && _robotRegistry.items != null)
                    {
                        for (int i = 0; i < _robotRegistry.items.Count; i++)
                        {
                            RobotRegistry.Item item = _robotRegistry.items[i];
                            if (item == null
                                || item.isDevelopmentOnly
                                || string.IsNullOrWhiteSpace(item.code)
                                || item.prefab == null
                                || !string.Equals(GetRobotFactionKey(item.code), factionKey, StringComparison.OrdinalIgnoreCase))
                            {
                                continue;
                            }

                            targetOptions.Add(new RobotTargetOption(
                                item.code,
                                GetRobotDisplayName(item.code),
                                factionKey));
                        }
                    }
                }

                options = targetOptions.ToArray();
                _robotTargetOptionsCache.Add(cacheKey, options);
            }

            if (string.IsNullOrWhiteSpace(currentTarget)
                || ContainsRobotTarget(options, currentTarget))
            {
                return options;
            }

            RobotTargetOption[] optionsWithMissingTarget = new RobotTargetOption[options.Length + 1];
            Array.Copy(options, optionsWithMissingTarget, options.Length);
            optionsWithMissingTarget[options.Length] = new RobotTargetOption(
                    currentTarget,
                    "Missing Robot — " + currentTarget,
                    MissingRobotGroup);
            return optionsWithMissingTarget;
        }

        private string GetRobotTargetLabel(string factionTarget, string currentTarget)
        {
            RobotTargetOption[] options = GetRobotTargetOptions(factionTarget, currentTarget);
            for (int i = 0; i < options.Length; i++)
            {
                if (options[i].TargetId == currentTarget)
                {
                    return options[i].Label;
                }
            }

            return string.IsNullOrWhiteSpace(currentTarget) ? "All Robots" : currentTarget;
        }

        private static string GetRobotFactionKeyFromTarget(string target)
        {
            if (string.IsNullOrWhiteSpace(target)
                || target == AudioRobotTargetIds.Common)
            {
                return string.Empty;
            }

            if (target == AudioRobotTargetIds.IronAllianceAll)
            {
                return "ia";
            }

            if (target == AudioRobotTargetIds.NovaSyndicateAll)
            {
                return "nv";
            }

            const string factionPrefix = "faction:";
            return target.StartsWith(factionPrefix, StringComparison.OrdinalIgnoreCase)
                ? target.Substring(factionPrefix.Length)
                : string.Empty;
        }

        private static string GetRobotFactionKey(string robotCode)
        {
            if (string.IsNullOrWhiteSpace(robotCode))
            {
                return string.Empty;
            }

            int separatorIndex = robotCode.IndexOf('_');
            return separatorIndex > 0
                ? robotCode.Substring(0, separatorIndex).ToLowerInvariant()
                : string.Empty;
        }

        private static string GetRobotFactionTargetId(string factionKey)
        {
            if (string.Equals(factionKey, "ia", StringComparison.OrdinalIgnoreCase))
            {
                return AudioRobotTargetIds.IronAllianceAll;
            }

            if (string.Equals(factionKey, "nv", StringComparison.OrdinalIgnoreCase))
            {
                return AudioRobotTargetIds.NovaSyndicateAll;
            }

            return "faction:" + factionKey;
        }

        private static string GetRobotFactionLabel(string factionKey)
        {
            if (string.Equals(factionKey, "ia", StringComparison.OrdinalIgnoreCase))
            {
                return "Iron Alliance — All Robots";
            }

            if (string.Equals(factionKey, "nv", StringComparison.OrdinalIgnoreCase))
            {
                return "Nova Syndicate — All Robots";
            }

            return ObjectNames.NicifyVariableName(factionKey) + " — All Robots";
        }

        private static RobotRegistry FindRobotRegistry()
        {
            string[] registryGuids = AssetDatabase.FindAssets("t:RobotRegistry");
            for (int i = 0; i < registryGuids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(registryGuids[i]);
                RobotRegistry registry = AssetDatabase.LoadAssetAtPath<RobotRegistry>(path);
                if (registry != null)
                {
                    return registry;
                }
            }

            return null;
        }

        private static bool ContainsRobotTarget(
            IList<RobotTargetOption> options,
            string targetId)
        {
            for (int i = 0; i < options.Count; i++)
            {
                if (options[i].TargetId == targetId)
                {
                    return true;
                }
            }

            return false;
        }

        private static string GetRobotDisplayName(string code)
        {
            int separatorIndex = code.IndexOf('_');
            string factionPrefix = separatorIndex > 0
                ? code.Substring(0, separatorIndex).ToUpperInvariant() + " · "
                : string.Empty;
            string shortCode = separatorIndex > 0 && separatorIndex + 1 < code.Length
                ? code.Substring(separatorIndex + 1)
                : code;
            return factionPrefix + ObjectNames.NicifyVariableName(shortCode);
        }

        private static string GetElementName(SerializedProperty element, int index)
        {
            SerializedProperty name = element.FindPropertyRelative("name");
            return name != null && !string.IsNullOrWhiteSpace(name.stringValue)
                ? name.stringValue
                : "Entry " + (index + 1);
        }
    }

    [CustomPropertyDrawer(typeof(AudionElement))]
    internal sealed class AudioElementPropertyDrawer : PropertyDrawer
    {
        private const float PreviewButtonWidth = 58f;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float lineHeight = EditorGUIUtility.singleLineHeight;
            if (!property.isExpanded)
            {
                return lineHeight;
            }

            int controlLines = 3;
            float spacing = EditorGUIUtility.standardVerticalSpacing;
            float controlsHeight = lineHeight * controlLines + spacing * (controlLines - 1);
            SerializedProperty clips = property.FindPropertyRelative("clips");
            float clipsHeight = clips == null ? 0f : EditorGUI.GetPropertyHeight(clips, true);
            return lineHeight + spacing + controlsHeight + spacing + clipsHeight;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            float lineHeight = EditorGUIUtility.singleLineHeight;
            float spacing = EditorGUIUtility.standardVerticalSpacing;
            Rect headerRect = new Rect(position.x, position.y, position.width, lineHeight);
            property.isExpanded = EditorGUI.Foldout(headerRect, property.isExpanded, label, true);

            if (!property.isExpanded)
            {
                EditorGUI.EndProperty();
                return;
            }

            SerializedProperty name = property.FindPropertyRelative("name");
            SerializedProperty volume = property.FindPropertyRelative("volume");
            SerializedProperty delay = property.FindPropertyRelative("delay");
            SerializedProperty clips = property.FindPropertyRelative("clips");

            int controlLines = 3;
            float controlsHeight = lineHeight * controlLines + spacing * (controlLines - 1);
            Rect controlsRect = new Rect(
                position.x,
                headerRect.yMax + spacing,
                position.width,
                controlsHeight);

            DrawResourceControls(
                controlsRect,
                name,
                volume,
                delay,
                property);

            if (clips != null)
            {
                Rect clipsRect = new Rect(
                    position.x,
                    controlsRect.yMax + spacing,
                    position.width,
                    EditorGUI.GetPropertyHeight(clips, true));
                EditorGUI.PropertyField(clipsRect, clips, new GUIContent("Clips"), true);
            }

            EditorGUI.EndProperty();
        }

        private static void DrawResourceControls(
            Rect position,
            SerializedProperty name,
            SerializedProperty volume,
            SerializedProperty delay,
            SerializedProperty resource)
        {
            float spacing = EditorGUIUtility.standardVerticalSpacing;
            float buttonWidth = Mathf.Min(PreviewButtonWidth, position.width * 0.2f);
            Rect buttonRect = new Rect(
                position.xMax - buttonWidth,
                position.y,
                buttonWidth,
                position.height);
            Rect fieldsRect = new Rect(
                position.x,
                position.y,
                position.width - buttonWidth - spacing,
                position.height);

            float lineHeight = EditorGUIUtility.singleLineHeight;
            Rect currentRect = new Rect(fieldsRect.x, fieldsRect.y, fieldsRect.width, lineHeight);
            DrawLabeledProperty(currentRect, name, "Name");
            currentRect.y += lineHeight + spacing;
            DrawLabeledSlider(currentRect, volume, "Volume", 0f, 1f);
            currentRect.y += lineHeight + spacing;
            DrawLabeledSlider(currentRect, delay, "Delay", 0f, 1f);

            using (new EditorGUI.DisabledScope(!TryGetPreviewData(resource, out AudioClip clip, out float previewVolume)))
            {
                if (GUI.Button(
                    buttonRect,
                    new GUIContent("Play", "Preview the first assigned clip at its configured volume")))
                {
                    AudioResourcesPreview.Play(clip, previewVolume);
                }
            }
        }

        private static void DrawLabeledProperty(
            Rect position,
            SerializedProperty property,
            string label)
        {
            if (property == null)
            {
                return;
            }

            float labelWidth = Mathf.Min(72f, position.width * 0.42f);
            Rect labelRect = new Rect(position.x, position.y, labelWidth, position.height);
            Rect fieldRect = new Rect(
                labelRect.xMax,
                position.y,
                position.width - labelWidth,
                position.height);
            EditorGUI.LabelField(labelRect, label, EditorStyles.miniLabel);
            EditorGUI.PropertyField(fieldRect, property, GUIContent.none);
        }

        private static void DrawLabeledSlider(
            Rect position,
            SerializedProperty property,
            string label,
            float min,
            float max)
        {
            if (property == null)
            {
                return;
            }

            float labelWidth = Mathf.Min(72f, position.width * 0.42f);
            Rect labelRect = new Rect(position.x, position.y, labelWidth, position.height);
            Rect fieldRect = new Rect(
                labelRect.xMax,
                position.y,
                position.width - labelWidth,
                position.height);
            EditorGUI.LabelField(labelRect, label, EditorStyles.miniLabel);
            float value = Mathf.Clamp(property.floatValue, min, max);
            EditorGUI.BeginChangeCheck();
            float nextValue = EditorGUI.Slider(fieldRect, GUIContent.none, value, min, max);
            if (EditorGUI.EndChangeCheck())
            {
                property.floatValue = nextValue;
            }
        }

        private static bool TryGetPreviewData(
            SerializedProperty resource,
            out AudioClip clip,
            out float volume)
        {
            clip = null;
            volume = 1f;

            SerializedProperty clips = resource.FindPropertyRelative("clips");
            if (clips == null || !clips.isArray)
            {
                return false;
            }

            SerializedProperty resourceVolume = resource.FindPropertyRelative("volume");
            float configuredVolume = resourceVolume == null ? 1f : Mathf.Clamp01(resourceVolume.floatValue);
            for (int i = 0; i < clips.arraySize; i++)
            {
                SerializedProperty entry = clips.GetArrayElementAtIndex(i);
                SerializedProperty clipProperty = entry.FindPropertyRelative("clip");
                if (clipProperty == null || !(clipProperty.objectReferenceValue is AudioClip candidate))
                {
                    continue;
                }

                SerializedProperty clipVolume = entry.FindPropertyRelative("volume");
                float entryVolume = clipVolume == null ? 1f : Mathf.Clamp01(clipVolume.floatValue);
                clip = candidate;
                volume = configuredVolume * entryVolume;
                return true;
            }

            return false;
        }
    }

    [InitializeOnLoad]
    internal static class AudioResourcesPreview
    {
        private static GameObject _previewObject;
        private static AudioSource _previewSource;
        private static double _cleanupAllowedAt;
        private static bool _updateRegistered;

        static AudioResourcesPreview()
        {
            EditorApplication.quitting += Stop;
        }

        private static void StartUpdateLoop()
        {
            if (_updateRegistered)
            {
                return;
            }

            EditorApplication.update += Update;
            _updateRegistered = true;
        }

        private static void StopUpdateLoop()
        {
            if (!_updateRegistered)
            {
                return;
            }

            EditorApplication.update -= Update;
            _updateRegistered = false;
        }

        public static void Play(AudioClip clip, float volume)
        {
            Stop();
            if (clip == null)
            {
                return;
            }

            _previewObject = new GameObject("AudioResources Preview")
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            _previewSource = _previewObject.AddComponent<AudioSource>();
            _previewSource.hideFlags = HideFlags.HideAndDontSave;
            _previewSource.playOnAwake = false;
            _previewSource.loop = false;
            _previewSource.spatialBlend = 0f;
            _previewSource.volume = Mathf.Clamp01(volume);
            _previewSource.clip = clip;
            _previewSource.Play();
            _cleanupAllowedAt = EditorApplication.timeSinceStartup + 0.1d;
            StartUpdateLoop();
        }

        private static void Update()
        {
            if (_previewSource == null
                || EditorApplication.timeSinceStartup < _cleanupAllowedAt
                || _previewSource.isPlaying)
            {
                return;
            }

            CleanupPreviewSource();
        }

        private static void Stop()
        {
            if (_previewSource != null)
            {
                _previewSource.Stop();
            }

            CleanupPreviewSource();
        }

        private static void CleanupPreviewSource()
        {
            StopUpdateLoop();

            if (_previewObject != null)
            {
                UnityEngine.Object.DestroyImmediate(_previewObject);
            }

            _previewSource = null;
            _previewObject = null;
        }
    }
}
