using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace VRPlayer.Editor
{
    /// <summary>
    /// 快速创建当前架构可运行的测试场景。
    /// </summary>
    public class CreateTestScene : EditorWindow
    {
        [MenuItem("VR Player/Testing/Create Test Scene")]
        public static void ShowWindow()
        {
            CreateTestScene window = GetWindow<CreateTestScene>("Create Test Scene");
            window.minSize = new Vector2(420f, 280f);
        }

        private void OnGUI()
        {
            GUILayout.Label("运行时测试场景生成器", EditorStyles.boldLabel);
            GUILayout.Space(10f);

            GUILayout.Label("将创建以下对象：", EditorStyles.label);
            GUILayout.Label("  - LocalFileManager", EditorStyles.label);
            GUILayout.Label("  - WebDAVManager", EditorStyles.label);
            GUILayout.Label("  - VRVideoPlayer (含 UnityVideoPlaybackService)", EditorStyles.label);
            GUILayout.Label("  - VideoBrowserUI", EditorStyles.label);
            GUILayout.Label("  - Directional Light", EditorStyles.label);
            GUILayout.Space(10f);

            if (GUILayout.Button("创建测试场景", GUILayout.Height(40f)))
            {
                CreateScene();
            }

            GUILayout.Space(8f);
            EditorGUILayout.HelpBox(
                "场景会保存到 Assets/Scenes/RuntimeTest.unity。\n创建完成后可直接 Play 验证本地视频浏览与播放流程。",
                MessageType.Info);
        }

        private static void CreateScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            GameObject localFileManagerGO = new GameObject("LocalFileManager");
            localFileManagerGO.AddComponent<LocalFileManager>();
            Debug.Log("✓ LocalFileManager 已创建");

            GameObject webDavManagerGO = new GameObject("WebDAVManager");
            webDavManagerGO.AddComponent<WebDAVManager>();
            Debug.Log("✓ WebDAVManager 已创建");

            GameObject playerGO = new GameObject("VRVideoPlayer");
            playerGO.AddComponent<VRVideoPlayer>();
            Debug.Log("✓ VRVideoPlayer 已创建");

            GameObject uiGO = new GameObject("VideoBrowserUI");
            uiGO.AddComponent<VideoBrowserUI>();
            Debug.Log("✓ VideoBrowserUI 已创建");

            GameObject lightGO = new GameObject("Directional Light");
            Light light = lightGO.AddComponent<Light>();
            light.type = LightType.Directional;
            light.shadows = LightShadows.Soft;
            light.intensity = 1.0f;
            Debug.Log("✓ Directional Light 已创建");

            const string sceneDirectory = "Assets/Scenes";
            if (!System.IO.Directory.Exists(sceneDirectory))
            {
                System.IO.Directory.CreateDirectory(sceneDirectory);
            }

            string savePath = sceneDirectory + "/RuntimeTest.unity";
            bool saveSuccess = EditorSceneManager.SaveScene(scene, savePath);

            if (!saveSuccess)
            {
                Debug.LogError("❌ 保存场景失败");
                return;
            }

            Debug.Log("✅ 测试场景已保存: " + savePath);
            EditorUtility.DisplayDialog(
                "场景创建成功",
                "测试场景已创建并保存到:\n" + savePath + "\n\n可点击 Play 进行验证。",
                "确定");
        }
    }

    /// <summary>
    /// 打开测试场景快捷入口。
    /// </summary>
    public static class OpenTestScene
    {
        [MenuItem("VR Player/Testing/Open Test Scene")]
        public static void OpenScene()
        {
            const string scenePath = "Assets/Scenes/RuntimeTest.unity";
            if (System.IO.File.Exists(scenePath))
            {
                EditorSceneManager.OpenScene(scenePath);
                Debug.Log("已打开测试场景: " + scenePath);
                return;
            }

            bool shouldCreate = EditorUtility.DisplayDialog(
                "测试场景不存在",
                "RuntimeTest 场景尚未创建，是否现在创建？",
                "创建",
                "取消");

            if (shouldCreate)
            {
                CreateTestScene.ShowWindow();
            }
        }
    }
}
