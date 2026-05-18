using Game.Scripts.Client;
using UnityEditor;
using UnityEngine;

namespace Toras.Editor
{
    [CustomEditor(typeof(ClientSettings))]
    public class ClientSettingsEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.HelpBox(
                "Клієнтські налаштування керують локальним HUD, картою та автоприцілом. Сервер їх не читає і не синхронізує, тому на сцені Client має бути ClientSettings, а на VehicleTest - ClientSettings разом із ServerSettings.",
                MessageType.Info
            );

            EditorGUILayout.Space(4f);
            DrawPropertiesExcluding(serializedObject, "m_Script");

            serializedObject.ApplyModifiedProperties();
        }
    }
}
