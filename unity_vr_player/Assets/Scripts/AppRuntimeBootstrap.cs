using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Ensures the app has a usable runtime setup even when the build scene is empty.
/// </summary>
public static class AppRuntimeBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        EnsureEventSystem();
        EnsureMainCamera();

        if (UnityEngine.Object.FindObjectOfType<VRVideoPlayer>() != null)
        {
            return;
        }

        GameObject root = new GameObject("VRAppRuntimeRoot");
        root.AddComponent<LocalFileManager>();
        root.AddComponent<WebDAVManager>();
        root.AddComponent<VRVideoPlayer>();
        root.AddComponent<VideoBrowserUI>();
    }

    private static void EnsureEventSystem()
    {
        if (UnityEngine.Object.FindObjectOfType<EventSystem>() != null)
        {
            return;
        }

        GameObject eventSystemObject = new GameObject("EventSystem");
        eventSystemObject.AddComponent<EventSystem>();
        eventSystemObject.AddComponent<StandaloneInputModule>();
    }

    private static void EnsureMainCamera()
    {
        if (UnityEngine.Object.FindObjectOfType<Camera>() != null)
        {
            return;
        }

        GameObject cameraObject = new GameObject("Main Camera");
        cameraObject.tag = "MainCamera";

        Camera camera = cameraObject.AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = Color.black;

        Transform transform = cameraObject.transform;
        transform.position = Vector3.zero;
        transform.rotation = Quaternion.identity;
    }
}
