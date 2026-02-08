package com.vrplayer.saf;

import android.app.Activity;
import android.content.Intent;

import com.unity3d.player.UnityPlayer;

public final class SafPickerBridge {
    private SafPickerBridge() {
    }

    public static void launchVideoPicker(final String receiverObject, final String callbackMethod) {
        final Activity activity = UnityPlayer.currentActivity;
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
}
