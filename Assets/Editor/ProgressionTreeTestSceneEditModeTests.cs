using NUnit.Framework;
using RAV.Scripts.UI.Tree;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RAV.Editor.Tests
{
    public sealed class ProgressionTreeTestSceneEditModeTests
    {
        private const string ScenePath = "Assets/Game/Scenes/ProgressionTreeTest.unity";

        [Test]
        public void NovaTestSceneInitializesAndValidatesAllRoutes()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            try
            {
                TreeGraphTestScene testScene = Object.FindFirstObjectByType<TreeGraphTestScene>();
                Assert.That(testScene, Is.Not.Null, "ProgressionTreeTest.unity must contain TreeGraphTestScene.");

                testScene.Initialize();

                Assert.That(
                    testScene.ValidationPassed,
                    Is.True,
                    $"ProgressionTreeTest.unity route validation failed: {testScene.LastValidationError}");
            }
            finally
            {
                if (scene.IsValid())
                {
                    EditorSceneManager.CloseScene(scene, true);
                }
            }
        }
    }
}
