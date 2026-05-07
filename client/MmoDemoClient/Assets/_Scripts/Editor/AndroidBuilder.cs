using UnityEditor;
using UnityEngine;

namespace MmoDemo.Client.Editor
{
    public static class AndroidBuilder
    {
        [MenuItem("MmoDemo/Build Android APK")]
        public static void BuildAndroid()
        {
            var scenes = new[] { "Assets/_Scenes/Bootstrap.unity" };

            PlayerSettings.SetApplicationIdentifier(
                BuildTargetGroup.Android, "com.mmodemo.client");
            PlayerSettings.productName = "MMORPG Demo";
            PlayerSettings.bundleVersion = "0.9.0";
            PlayerSettings.Android.targetArchitectures =
                AndroidArchitecture.ARM64 | AndroidArchitecture.ARMv7;

            var path = EditorUtility.SaveFilePanel(
                "Save APK", "", "MMORPGDemo.apk", "apk");
            if (string.IsNullOrEmpty(path)) return;

            var report = BuildPipeline.BuildPlayer(
                scenes, path, BuildTarget.Android,
                BuildOptions.None);

            Debug.Log(report.summary.result ==
                UnityEditor.Build.Reporting.BuildResult.Succeeded
                ? $"[Android] Build success: {path}"
                : $"[Android] Build failed: {report.summary}");
        }
    }
}
