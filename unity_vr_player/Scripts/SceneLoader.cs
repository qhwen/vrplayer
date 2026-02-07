using UnityEngine;
using UnityEngine.SceneManagement;

/// 主场景管理器
public class SceneLoader : MonoBehaviour
{
    public static SceneLoader Instance { get; private set; }
    
    [Header("场景设置")]
    [SerializeField] private string videoPlayerScene = "VideoPlayerScene";
    [SerializeField] private string settingsScene = "SettingsScene";
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        DontDestroyOnLoad(gameObject);
    }
    
    /// 加载视频播放器场景
    public void LoadVideoPlayerScene()
    {
        StartCoroutine(LoadSceneAsync(videoPlayerScene));
    }
    
    /// 加载设置场景
    public void LoadSettingsScene()
    {
        StartCoroutine(LoadSceneAsync(settingsScene));
    }
    
    /// 异步加载场景
    private IEnumerator LoadSceneAsync(string sceneName)
    {
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
        
        while (!asyncLoad.isDone)
        {
            Debug.Log("加载场景中: " + sceneName + " - " + asyncLoad.progress * 100 + "%");
            yield return null;
        }
        
        Debug.Log("场景加载完成: " + sceneName);
    }
}
