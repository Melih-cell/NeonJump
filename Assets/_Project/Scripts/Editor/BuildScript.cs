using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public class BuildScript
{
    [MenuItem("Build/Build Android APK")]
    public static void BuildAndroid()
    {
        // Sahneleri ayarla
        string[] scenes = new string[]
        {
            "Assets/_Project/Scenes/MainMenu.unity",
            "Assets/_Project/Scenes/1.unity"
        };

        // Build ayarlari
        BuildPlayerOptions buildOptions = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = "Builds/Android/NeonJump.apk",
            target = BuildTarget.Android,
            options = BuildOptions.None
        };

        // Platform ayarlari
        EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);

        // Android ayarlari
        PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel25;
        PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;
        PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;

        // Uygulama bilgileri
        PlayerSettings.companyName = "NeonJump";
        PlayerSettings.productName = "Neon Jump";
        PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, "com.neonjump.game");
        PlayerSettings.bundleVersion = "1.0.0";
        PlayerSettings.Android.bundleVersionCode = 1;

        // Ekran ayarlari
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;

        // Build!
        BuildReport report = BuildPipeline.BuildPlayer(buildOptions);
        BuildSummary summary = report.summary;

        if (summary.result == BuildResult.Succeeded)
        {
            Debug.Log($"BUILD BASARILI! Boyut: {summary.totalSize / (1024 * 1024)} MB, Sure: {summary.totalTime}");
            Debug.Log($"APK: {buildOptions.locationPathName}");
        }
        else
        {
            Debug.LogError($"BUILD BASARISIZ: {summary.result}");
            foreach (var step in report.steps)
            {
                foreach (var msg in step.messages)
                {
                    if (msg.type == LogType.Error)
                        Debug.LogError(msg.content);
                }
            }
        }
    }
}
