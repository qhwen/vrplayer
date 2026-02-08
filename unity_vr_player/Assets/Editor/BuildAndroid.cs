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
                throw new Exception("Switching Android build target failed.");
            }

            string outputDir = GetFirstEnvironmentVariable("UNITY_ANDROID_OUTPUT_DIR");
            if (string.IsNullOrWhiteSpace(outputDir))
            {
                outputDir = DefaultOutputDir;
            }

            string apkName = GetFirstEnvironmentVariable("UNITY_ANDROID_APK_NAME");
            if (string.IsNullOrWhiteSpace(apkName))
            {
                apkName = DefaultApkName;
            }

            Directory.CreateDirectory(outputDir);
            string outputPath = Path.GetFullPath(Path.Combine(outputDir, apkName));

            ApplyBuildMetadata();

            BuildOptions buildOptions = BuildOptions.None;
            string developmentBuild = GetFirstEnvironmentVariable("UNITY_DEVELOPMENT_BUILD");
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
                throw new Exception("No enabled scenes in Build Settings.");
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
                throw new Exception("Android build failed. Check Editor log for details.");
            }

            Debug.Log("Android build succeeded: " + outputPath);
            EditorApplication.Exit(0);
        }
        catch (Exception ex)
        {
            Debug.LogError("BuildAndroid.PerformBuild failed: " + ex.Message);
            EditorApplication.Exit(1);
        }
    }

    private static void ApplyBuildMetadata()
    {
        string productName = GetFirstEnvironmentVariable("UNITY_PRODUCT_NAME");
        if (!string.IsNullOrWhiteSpace(productName))
        {
            PlayerSettings.productName = productName;
        }

        string companyName = GetFirstEnvironmentVariable("UNITY_COMPANY_NAME");
        if (!string.IsNullOrWhiteSpace(companyName))
        {
            PlayerSettings.companyName = companyName;
        }

        string bundleIdentifier = GetFirstEnvironmentVariable("UNITY_BUNDLE_IDENTIFIER");
        if (!string.IsNullOrWhiteSpace(bundleIdentifier))
        {
            PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, bundleIdentifier);
        }

        int versionCode = ResolveVersionCode();
        string versionName = ResolveVersionName(versionCode);

        PlayerSettings.Android.bundleVersionCode = versionCode;
        PlayerSettings.bundleVersion = versionName;

        Debug.Log("Resolved build metadata: versionName=" + versionName + ", versionCode=" + versionCode);
    }

    private static int ResolveVersionCode()
    {
        string versionCodeRaw = GetFirstEnvironmentVariable("UNITY_VERSION_CODE", "GITHUB_RUN_NUMBER", "BUILD_NUMBER");
        if (int.TryParse(versionCodeRaw, out int versionCode) && versionCode > 0)
        {
            return versionCode;
        }

        long unixMinutes = DateTimeOffset.UtcNow.ToUnixTimeSeconds() / 60L;
        if (unixMinutes < 1L)
        {
            unixMinutes = 1L;
        }

        if (unixMinutes > int.MaxValue)
        {
            return int.MaxValue;
        }

        return (int)unixMinutes;
    }

    private static string ResolveVersionName(int versionCode)
    {
        string versionName = GetFirstEnvironmentVariable("UNITY_VERSION_NAME", "VERSION_NAME");
        if (!string.IsNullOrWhiteSpace(versionName))
        {
            return versionName;
        }

        return "1.0." + versionCode;
    }

    private static string GetFirstEnvironmentVariable(params string[] names)
    {
        if (names == null)
        {
            return string.Empty;
        }

        for (int i = 0; i < names.Length; i++)
        {
            string name = names[i];
            if (string.IsNullOrWhiteSpace(name))
            {
                continue;
            }

            string value = Environment.GetEnvironmentVariable(name);
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value.Trim();
            }
        }

        return string.Empty;
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
