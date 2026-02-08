package com.vrplayer.saf;

import android.app.Activity;
import android.content.Intent;

public final class SafPickerBridge {
    private SafPickerBridge() {
    }

    public static void launchVideoPicker(final String receiverObject, final String callbackMethod) {
        final Activity activity = getUnityActivity();
        if (activity == null) {
            return;
        }

        activity.runOnUiThread(new Runnable() {
            @Override
            public void run() {
                Intent intent = new Intent(activity, SafPickerProxyActivity.class);
                intent.putExtra(SafPickerProxyActivity.EXTRA_RECEIVER_OBJECT, receiverObject);
                intent.putExtra(SafPickerProxyActivity.EXTRA_CALLBACK_METHOD, callbackMethod);
                activity.startActivity(intent);
            }
        });
    }

    private static Activity getUnityActivity() {
        try {
            Class<?> unityPlayerClass = Class.forName("com.unity3d.player.UnityPlayer");
            Object activity = unityPlayerClass.getField("currentActivity").get(null);
            if (activity instanceof Activity) {
                return (Activity) activity;
            }
        } catch (Exception ignored) {
            // Ignore and return null.
        }

        return null;
    }
}
