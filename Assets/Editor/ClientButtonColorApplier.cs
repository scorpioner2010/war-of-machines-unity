using Game.Scripts.Audio;
using Game.Scripts.Client;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Toras.Editor
{
    internal static class ClientButtonColorApplier
    {
        private const string ClientScenePath = "Assets/Game/Scenes/Client.unity";

        public static void ApplyToGameUiButtons(ClientButtonColorSettings buttonColors)
        {
            if (buttonColors == null)
            {
                EditorUtility.DisplayDialog("UI Button Colors", "Assign Button Colors in ClientSettings before applying them.", "OK");
                return;
            }

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            buttonColors.Validate();
            SceneSetup[] originalSceneSetup = EditorSceneManager.GetSceneManagerSetup();
            int buttonCount = 0;

            try
            {
                ApplyToGamePrefabs(buttonColors, ref buttonCount);
                ApplyToClientScene(buttonColors, ref buttonCount);
                AssetDatabase.SaveAssets();
            }
            finally
            {
                EditorSceneManager.RestoreSceneManagerSetup(originalSceneSetup);
            }

            EditorUtility.DisplayDialog("UI Button Colors", $"Applied colors to {buttonCount} UI buttons.", "OK");
        }

        private static void ApplyToGamePrefabs(ClientButtonColorSettings buttonColors, ref int buttonCount)
        {
            string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Game" });

            for (int index = 0; index < prefabGuids.Length; index++)
            {
                string prefabPath = AssetDatabase.GUIDToAssetPath(prefabGuids[index]);
                GameObject prefabRoot = PrefabUtility.LoadPrefabContents(prefabPath);

                try
                {
                    Button[] buttons = prefabRoot.GetComponentsInChildren<Button>(true);
                    if (buttons.Length == 0)
                    {
                        continue;
                    }

                    ConfigureButtons(buttons, buttonColors, ref buttonCount);
                    PrefabUtility.SaveAsPrefabAsset(prefabRoot, prefabPath);
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(prefabRoot);
                }
            }
        }

        private static void ApplyToClientScene(ClientButtonColorSettings buttonColors, ref int buttonCount)
        {
            Scene clientScene = EditorSceneManager.OpenScene(ClientScenePath, OpenSceneMode.Single);
            GameObject[] roots = clientScene.GetRootGameObjects();

            for (int index = 0; index < roots.Length; index++)
            {
                Button[] buttons = roots[index].GetComponentsInChildren<Button>(true);
                ConfigureButtons(buttons, buttonColors, ref buttonCount);
            }

            EditorSceneManager.SaveScene(clientScene);
        }

        private static void ConfigureButtons(Button[] buttons, ClientButtonColorSettings buttonColors, ref int buttonCount)
        {
            for (int index = 0; index < buttons.Length; index++)
            {
                Button button = buttons[index];
                ColorBlock colors = button.colors;
                buttonColors.ApplyTo(ref colors);
                button.colors = colors;

                ButtonComponent buttonComponent = button.GetComponent<ButtonComponent>();
                if (buttonComponent == null)
                {
                    buttonComponent = button.gameObject.AddComponent<ButtonComponent>();
                }

                buttonComponent.ConfigureForEditor(button);
                EditorUtility.SetDirty(button);
                EditorUtility.SetDirty(buttonComponent);
                buttonCount++;
            }
        }
    }
}
