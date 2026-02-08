#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class BuildAndroid
{
    private const string DefaultOutputDir = "builds/Android";
    private const string DefaultApkName = "VRVideoPlayer.apk";

    [MenuItem("Build/Build Android APK")]
    public static void PerformBuildMenu()
    {
        PerformBuild();
    }

    public static void PerformBuild()
    {
        try
        {
            EnsureBuildSceneExists();

            if (!EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android))
            {
                throw new Exception("切换 Android 构建目标失败。");
            }

            string outputDir = Environment.GetEnvironmentVariable("UNITY_ANDROID_OUTPUT_DIR");
            if (string.IsNullOrWhiteSpace(outputDir))
            {
                outputDir = DefaultOutputDir;
            }

            string apkName = Environment.GetEnvironmentVariable("UNITY_ANDROID_APK_NAME");
            if (string.IsNullOrWhiteSpace(apkName))
            {
                apkName = DefaultApkName;
            }

            Directory.CreateDirectory(outputDir);
            string outputPath = Path.GetFullPath(Path.Combine(outputDir, apkName));

            string productName = Environment.GetEnvironmentVariable("UNITY_PRODUCT_NAME");
            if (!string.IsNullOrWhiteSpace(productName))
            {
                PlayerSettings.productName = productName;
            }

            string companyName = Environment.GetEnvironmentVariable("UNITY_COMPANY_NAME");
            if (!string.IsNullOrWhiteSpace(companyName))
            {
                PlayerSettings.companyName = companyName;
            }

            string bundleIdentifier = Environment.GetEnvironmentVariable("UNITY_BUNDLE_IDENTIFIER");
            if (!string.IsNullOrWhiteSpace(bundleIdentifier))
            {
                PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, bundleIdentifier);
            }

            string versionName = Environment.GetEnvironmentVariable("UNITY_VERSION_NAME");
            if (!string.IsNullOrWhiteSpace(versionName))
            {
                PlayerSettings.bundleVersion = versionName;
            }

            string versionCodeRaw = Environment.GetEnvironmentVariable("UNITY_VERSION_CODE");
            if (int.TryParse(versionCodeRaw, out int versionCode) && versionCode > 0)
            {
                PlayerSettings.Android.bundleVersionCode = versionCode;
            }

            BuildOptions buildOptions = BuildOptions.None;
            string developmentBuild = Environment.GetEnvironmentVariable("UNITY_DEVELOPMENT_BUILD");
            if (string.Equals(developmentBuild, "true", StringComparison.OrdinalIgnoreCase))
            {
                buildOptions |= BuildOptions.Development;
            }

            string[] scenes = EditorBuildSettings.scenes
                .Where(scene => scene.enabled)
                .Select(scene => scene.path)
                .ToArray();

            if (scenes.Length == 0)
            {
                throw new Exception("Build Settings 中没有启用场景。");
            }

            BuildPlayerOptions playerOptions = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = outputPath,
                target = BuildTarget.Android,
                options = buildOptions
            };

            BuildReport report = BuildPipeline.BuildPlayer(playerOptions);
            if (report.summary.result != BuildResult.Succeeded)
            {
                throw new Exception("Android 构建失败，详情见 Editor log。");
            }

            Debug.Log("Android 构建成功: " + outputPath);
            EditorApplication.Exit(0);
        }
        catch (Exception ex)
        {
            Debug.LogError("BuildAndroid.PerformBuild 失败: " + ex.Message);
            EditorApplication.Exit(1);
        }
    }

    private static void EnsureBuildSceneExists()
    {
        if (EditorBuildSettings.scenes.Length > 0)
        {
            return;
        }

        const string scenePath = "Assets/Scenes/Bootstrap.unity";

        string sceneDir = Path.GetDirectoryName(scenePath);
        if (!string.IsNullOrWhiteSpace(sceneDir) && !Directory.Exists(sceneDir))
        {
            Directory.CreateDirectory(sceneDir);
        }

        if (!File.Exists(scenePath))
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            EditorSceneManager.SaveScene(scene, scenePath);
        }

        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene(scenePath, true)
        };

        AssetDatabase.Refresh();
    }
}
#endif
