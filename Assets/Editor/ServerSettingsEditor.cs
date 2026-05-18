using Game.Scripts.Server;
using UnityEditor;
using UnityEngine;

namespace Toras.Editor
{
    [CustomEditor(typeof(ServerSettings))]
    public class ServerSettingsEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.HelpBox(
                "Наведіть курсор на назву параметра, щоб побачити український опис його впливу. Тут залишені серверні налаштування матчу, ботів, руху, розкиду та балістики, які потрібні серверу або синхронізуються клієнтам.",
                MessageType.Info
            );

            EditorGUILayout.Space(4f);
            DrawPropertiesExcluding(serializedObject, "m_Script");

            serializedObject.ApplyModifiedProperties();
        }
    }
}
