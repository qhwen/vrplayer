using System;
using UnityEngine;

/// <summary>
/// Applies Android runtime display defaults for immersive landscape playback.
/// </summary>
public static class AndroidRuntimeConfigurator
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void ConfigureRuntime()
    {
        Screen.orientation = ScreenOrientation.LandscapeLeft;
        Screen.sleepTimeout = SleepTimeout.NeverSleep;

#if UNITY_ANDROID && !UNITY_EDITOR
        ApplyImmersiveMode();
#endif
    }

#if UNITY_ANDROID && !UNITY_EDITOR
    private static void ApplyImmersiveMode()
    {
        try
        {
            AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            AndroidJavaObject activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");

            activity.Call("runOnUiThread", new AndroidJavaRunnable(() =>
            {
                AndroidJavaObject window = activity.Call<AndroidJavaObject>("getWindow");
                AndroidJavaObject decorView = window.Call<AndroidJavaObject>("getDecorView");

                const int systemUiFlagLayoutStable = 0x00000100;
                const int systemUiFlagLayoutHideNavigation = 0x00000200;
                const int systemUiFlagLayoutFullscreen = 0x00000400;
                const int systemUiFlagHideNavigation = 0x00000002;
                const int systemUiFlagFullscreen = 0x00000004;
                const int systemUiFlagImmersiveSticky = 0x00001000;

                int flags = systemUiFlagLayoutStable
                            | systemUiFlagLayoutHideNavigation
                            | systemUiFlagLayoutFullscreen
                            | systemUiFlagHideNavigation
                            | systemUiFlagFullscreen
                            | systemUiFlagImmersiveSticky;

                decorView.Call("setSystemUiVisibility", flags);
            }));
        }
        catch (Exception e)
        {
            Debug.LogWarning("Failed to apply immersive mode: " + e.Message);
        }
    }
#endif
}
