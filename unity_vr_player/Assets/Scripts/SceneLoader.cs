using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 主场景管理器。
/// </summary>
public class SceneLoader : MonoBehaviour
{
    public static SceneLoader Instance { get; private set; }

    [Header("场景设置")]
    [SerializeField] private string videoPlayerScene = "VideoPlayerScene";
    [SerializeField] private string settingsScene = "SettingsScene";

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            return;
        }

        if (Instance != this)
        {
            Destroy(gameObject);
        }
    }

    public void LoadVideoPlayerScene()
    {
        StartCoroutine(LoadSceneAsync(videoPlayerScene));
    }

    public void LoadSettingsScene()
    {
        StartCoroutine(LoadSceneAsync(settingsScene));
    }

    private IEnumerator LoadSceneAsync(string sceneName)
    {
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
        if (asyncLoad == null)
        {
            Debug.LogError("场景加载失败，场景不存在: " + sceneName);
            yield break;
        }

        while (!asyncLoad.isDone)
        {
            Debug.Log("加载场景中: " + sceneName + " - " + (asyncLoad.progress * 100f).ToString("F1") + "%");
            yield return null;
        }

        Debug.Log("场景加载完成: " + sceneName);
    }
}
