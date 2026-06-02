using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Toras.Editor
{
    internal static class DocumentedSettingsInspector
    {
        private const string MissingDocumentation =
            "Опис для цього параметра ще не додано в editor-інспектор. Додайте українське пояснення з прикладом перед зміною значення.";

        public static void Draw(
            SerializedObject serializedObject,
            string introduction,
            IReadOnlyDictionary<string, string> documentation)
        {
            serializedObject.Update();

            EditorGUILayout.HelpBox(introduction, MessageType.Info);
            EditorGUILayout.Space(4f);

            SerializedProperty iterator = serializedObject.GetIterator();
            bool enterChildren = true;
            while (iterator.NextVisible(enterChildren))
            {
                enterChildren = false;
                if (iterator.depth == 0)
                {
                    DrawProperty(iterator.Copy(), documentation);
                }
            }

            serializedObject.ApplyModifiedProperties();
        }

        private static void DrawProperty(
            SerializedProperty property,
            IReadOnlyDictionary<string, string> documentation)
        {
            if (property.propertyPath == "m_Script")
            {
                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUILayout.PropertyField(property);
                }

                return;
            }

            bool isArray = property.isArray && property.propertyType != SerializedPropertyType.String;
            bool isGroup = property.hasVisibleChildren && property.propertyType == SerializedPropertyType.Generic;
            string description = GetDescription(property, documentation);
            GUIContent label = new GUIContent(property.displayName, description);

            EditorGUILayout.PropertyField(property, label, isArray);

            if (!isGroup || isArray || !property.isExpanded)
            {
                return;
            }

            SerializedProperty child = property.Copy();
            SerializedProperty end = child.GetEndProperty();
            bool enterChildren = true;

            EditorGUI.indentLevel++;
            while (child.NextVisible(enterChildren) && !SerializedProperty.EqualContents(child, end))
            {
                enterChildren = false;
                if (child.depth == property.depth + 1)
                {
                    DrawProperty(child.Copy(), documentation);
                }
            }

            EditorGUI.indentLevel--;
        }

        private static string GetDescription(
            SerializedProperty property,
            IReadOnlyDictionary<string, string> documentation)
        {
            if (documentation != null
                && documentation.TryGetValue(property.propertyPath, out string description)
                && !string.IsNullOrWhiteSpace(description))
            {
                return description;
            }

            return MissingDocumentation;
        }
    }
}
