using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;

namespace VRPlayer.Editor
{
    /// <summary>
    /// 快速创建架构测试场景
    /// </summary>
    public class CreateTestScene : EditorWindow
    {
        [MenuItem("VR Player/Testing/Create Test Scene")]
        public static void ShowWindow()
        {
            var window = GetWindow<CreateTestScene>("Create Test Scene");
            window.minSize = new Vector2(400, 300);
        }

        private void OnGUI()
        {
            GUILayout.Label("架构测试场景生成器", EditorStyles.boldLabel);
            GUILayout.Space(10);

            GUILayout.Label("此工具将自动创建一个完整的测试场景，包含：", EditorStyles.label);
            GUILayout.Label("  - LibraryManager", EditorStyles.label);
            GUILayout.Label("  - PlaybackOrchestrator", EditorStyles.label);
            GUILayout.Label("  - ArchitectureTest", EditorStyles.label);
            GUILayout.Label("  - Directional Light", EditorStyles.label);
            GUILayout.Space(10);

            if (GUILayout.Button("创建测试场景", GUILayout.Height(40)))
            {
                CreateScene();
            }

            GUILayout.Space(10);
            
            EditorGUILayout.HelpBox(
                "场景将创建在 Assets/Scenes/ArchitectureTest.unity\n" +
                "创建完成后，点击 Play 按钮即可运行测试",
                MessageType.Info);
        }

        private void CreateScene()
        {
            // 创建新场景
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // 1. 创建 LibraryManager
            var libraryManagerGO = new GameObject("LibraryManager");
            var libraryManager = libraryManagerGO.AddComponent<LibraryManager>();
            
            // 设置初始配置
            var soLibrary = new SerializedObject(libraryManager);
            soLibrary.FindProperty("enableAutoScan").boolValue = false;
            soLibrary.FindProperty("scanOnStartup").boolValue = false;
            soLibrary.ApplyModifiedProperties();

            Debug.Log("✓ LibraryManager 已创建");

            // 2. 创建 PlaybackOrchestrator
            var orchestratorGO = new GameObject("PlaybackOrchestrator");
            var orchestrator = orchestratorGO.AddComponent<PlaybackOrchestrator>();
            
            // 设置初始配置
            var soPlayback = new SerializedObject(orchestrator);
            soPlayback.FindProperty("enableAutoCache").boolValue = true;
            soPlayback.FindProperty("autoPrepare").boolValue = true;
            soPlayback.ApplyModifiedProperties();

            Debug.Log("✓ PlaybackOrchestrator 已创建");

            // 3. 创建 ArchitectureTest
            var testGO = new GameObject("ArchitectureTest");
            var test = testGO.AddComponent<ArchitectureTest>();
            
            // 设置初始配置
            var soTest = new SerializedObject(test);
            soTest.FindProperty("autoStart").boolValue = true;
            soTest.FindProperty("testEventBus").boolValue = true;
            soTest.FindProperty("testLogger").boolValue = true;
            soTest.FindProperty("testConfig").boolValue = true;
            soTest.FindProperty("testPlaybackOrchestrator").boolValue = true;
            soTest.FindProperty("testLibraryManager").boolValue = true;
            soTest.ApplyModifiedProperties();

            Debug.Log("✓ ArchitectureTest 已创建");

            // 4. 创建 Directional Light
            var lightGO = new GameObject("Directional Light");
            var light = lightGO.AddComponent<Light>();
            light.type = LightType.Directional;
            light.shadows = LightShadows.Soft;
            light.intensity = 1.0f;

            Debug.Log("✓ Directional Light 已创建");

            // 5. 创建场景目录（如果不存在）
            var scenePath = "Assets/Scenes";
            if (!System.IO.Directory.Exists(scenePath))
            {
                System.IO.Directory.CreateDirectory(scenePath);
                Debug.Log("✓ Scenes 目录已创建");
            }

            // 6. 保存场景
            var sceneSavePath = $"{scenePath}/ArchitectureTest.unity";
            var saveSuccess = EditorSceneManager.SaveScene(scene, sceneSavePath);

            if (saveSuccess)
            {
                Debug.Log($"✅ 测试场景已创建并保存到: {sceneSavePath}");
                Debug.Log("点击 Play 按钮 ▶️ 开始测试");
                
                EditorUtility.DisplayDialog(
                    "场景创建成功！",
                    $"测试场景已创建并保存到:\n{sceneSavePath}\n\n点击 Play 按钮开始测试。",
                    "确定"
                );
            }
            else
            {
                Debug.LogError("❌ 保存场景失败");
            }
        }
    }

    /// <summary>
    /// 打开测试场景的快捷菜单
    /// </summary>
    public class OpenTestScene
    {
        [MenuItem("VR Player/Testing/Open Test Scene")]
        public static void OpenScene()
        {
            var scenePath = "Assets/Scenes/ArchitectureTest.unity";
            if (System.IO.File.Exists(scenePath))
            {
                EditorSceneManager.OpenScene(scenePath);
                Debug.Log("已打开测试场景");
            }
            else
            {
                if (EditorUtility.DisplayDialog(
                    "测试场景不存在",
                    "测试场景尚未创建。\n是否现在创建？",
                    "创建",
                    "取消"))
                {
                    CreateTestScene.ShowWindow();
                }
            }
        }
    }
}
