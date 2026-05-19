using Game.Scripts.Diagnostics;
using UnityEditor;
using UnityEngine;

public static class DiagnosticsMenu
{
    [MenuItem("Tools/Diagnostics/Enable Diagnostics")]
    private static void EnableDiagnostics()
    {
        EditorPrefs.SetBool(DiagnosticsConfig.EditorPrefsDisabledKey, false);
        EditorPrefs.SetBool(DiagnosticsConfig.EditorPrefsEnabledKey, true);
        DiagnosticsConfig.Enabled = true;
        Debug.Log("[Diagnostics] Enabled via Tools/Diagnostics/Enable Diagnostics. Restart play mode if the bridge is not already running.");

        if (EditorApplication.isPlaying)
        {
            DiagnosticsConfig config = DiagnosticsConfig.LoadRuntime();
            config.IsEnabled = true;
            config.StateReason = "Unity Editor menu setting";
            DiagnosticsManager.EnsureStarted(config);
        }
    }

    [MenuItem("Tools/Diagnostics/Disable Diagnostics")]
    private static void DisableDiagnostics()
    {
        EditorPrefs.SetBool(DiagnosticsConfig.EditorPrefsEnabledKey, false);
        EditorPrefs.SetBool(DiagnosticsConfig.EditorPrefsDisabledKey, true);
        DiagnosticsConfig.Enabled = false;
        Debug.Log("[Diagnostics] Disabled via Tools/Diagnostics/Disable Diagnostics. Restart play mode to stop an already-running bridge.");
    }

    [MenuItem("Tools/Diagnostics/Start Diagnostics Now")]
    private static void StartDiagnosticsNow()
    {
        if (!EditorApplication.isPlaying)
        {
            Debug.Log("[Diagnostics] Start Diagnostics Now is available only while the Editor is in Play Mode.");
            return;
        }

        EditorPrefs.SetBool(DiagnosticsConfig.EditorPrefsDisabledKey, false);
        EditorPrefs.SetBool(DiagnosticsConfig.EditorPrefsEnabledKey, true);
        DiagnosticsConfig config = DiagnosticsConfig.LoadRuntime();
        config.IsEnabled = true;
        config.StateReason = "Unity Editor manual start";
        DiagnosticsManager.EnsureStarted(config);
    }
}
