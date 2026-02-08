package com.vrplayer.saf;

import android.app.Activity;
import android.content.ActivityNotFoundException;
import android.content.ClipData;
import android.content.ContentResolver;
import android.content.Intent;
import android.database.Cursor;
import android.net.Uri;
import android.os.Bundle;
import android.provider.OpenableColumns;

import org.json.JSONArray;
import org.json.JSONObject;

import java.util.LinkedHashSet;
import java.util.Set;

public class SafPickerProxyActivity extends Activity {
    public static final String EXTRA_RECEIVER_OBJECT = "receiver_object";
    public static final String EXTRA_CALLBACK_METHOD = "callback_method";

    private static final int REQUEST_PICK_VIDEO = 41027;
    private static final String STATE_LAUNCHED = "launched";

    private String receiverObject = "VRAppRuntimeRoot";
    private String callbackMethod = "OnAndroidVideoPickerResult";
    private boolean launched;

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);

        Intent sourceIntent = getIntent();
        if (sourceIntent != null) {
            String incomingObject = sourceIntent.getStringExtra(EXTRA_RECEIVER_OBJECT);
            String incomingMethod = sourceIntent.getStringExtra(EXTRA_CALLBACK_METHOD);

            if (incomingObject != null && incomingObject.length() > 0) {
                receiverObject = incomingObject;
            }

            if (incomingMethod != null && incomingMethod.length() > 0) {
                callbackMethod = incomingMethod;
            }
        }

        if (savedInstanceState != null) {
            launched = savedInstanceState.getBoolean(STATE_LAUNCHED, false);
        }

        if (!launched) {
            launched = true;
            openSystemPicker();
        }
    }

    @Override
    protected void onSaveInstanceState(Bundle outState) {
        super.onSaveInstanceState(outState);
        outState.putBoolean(STATE_LAUNCHED, launched);
    }

    private void openSystemPicker() {
        Intent pickerIntent = new Intent(Intent.ACTION_OPEN_DOCUMENT);
        pickerIntent.addCategory(Intent.CATEGORY_OPENABLE);
        pickerIntent.setType("video/*");
        pickerIntent.putExtra(Intent.EXTRA_ALLOW_MULTIPLE, true);
        pickerIntent.addFlags(Intent.FLAG_GRANT_READ_URI_PERMISSION);
        pickerIntent.addFlags(Intent.FLAG_GRANT_PERSISTABLE_URI_PERMISSION);

        try {
            startActivityForResult(pickerIntent, REQUEST_PICK_VIDEO);
        } catch (ActivityNotFoundException exception) {
            sendError("System picker unavailable: " + exception.getMessage());
            finish();
        }
    }

    @Override
    protected void onActivityResult(int requestCode, int resultCode, Intent data) {
        super.onActivityResult(requestCode, resultCode, data);

        if (requestCode != REQUEST_PICK_VIDEO) {
            finish();
            return;
        }

        if (resultCode != RESULT_OK || data == null) {
            sendCancelled();
            finish();
            return;
        }

        try {
            sendSelectedVideos(data);
        } catch (Exception exception) {
            sendError("Failed to read picker result: " + exception.getMessage());
        }

        finish();
    }

    private void sendSelectedVideos(Intent data) throws Exception {
        Set<Uri> uniqueUris = new LinkedHashSet<Uri>();
        Uri singleUri = data.getData();
        if (singleUri != null) {
            uniqueUris.add(singleUri);
        }

        ClipData clipData = data.getClipData();
        if (clipData != null) {
            for (int i = 0; i < clipData.getItemCount(); i++) {
                ClipData.Item item = clipData.getItemAt(i);
                if (item == null) {
                    continue;
                }

                Uri uri = item.getUri();
                if (uri != null) {
                    uniqueUris.add(uri);
                }
            }
        }

        if (uniqueUris.isEmpty()) {
            sendCancelled();
            return;
        }

        JSONArray videosArray = new JSONArray();
        ContentResolver resolver = getContentResolver();

        for (Uri uri : uniqueUris) {
            if (uri == null) {
                continue;
            }

            try {
                resolver.takePersistableUriPermission(uri, Intent.FLAG_GRANT_READ_URI_PERMISSION);
            } catch (Exception ignored) {
                // Persistable grant can fail on some providers; playback may still work for current session.
            }

            String name = null;
            long size = 0L;

            Cursor cursor = null;
            try {
                cursor = resolver.query(uri, new String[] { OpenableColumns.DISPLAY_NAME, OpenableColumns.SIZE }, null, null, null);
                if (cursor != null && cursor.moveToFirst()) {
                    int nameIndex = cursor.getColumnIndex(OpenableColumns.DISPLAY_NAME);
                    int sizeIndex = cursor.getColumnIndex(OpenableColumns.SIZE);
                    if (nameIndex >= 0) {
                        name = cursor.getString(nameIndex);
                    }

                    if (sizeIndex >= 0 && !cursor.isNull(sizeIndex)) {
                        size = cursor.getLong(sizeIndex);
                    }
                }
            } finally {
                if (cursor != null) {
                    cursor.close();
                }
            }

            if (name == null || name.length() == 0) {
                String segment = uri.getLastPathSegment();
                name = segment == null || segment.length() == 0 ? "selected_video" : segment;
            }

            JSONObject video = new JSONObject();
            video.put("uri", uri.toString());
            video.put("name", name);
            video.put("size", size);
            videosArray.put(video);
        }

        JSONObject result = new JSONObject();
        result.put("cancelled", false);
        result.put("error", "");
        result.put("videos", videosArray);

        sendUnityMessage(result.toString());
    }

    private void sendCancelled() {
        try {
            JSONObject result = new JSONObject();
            result.put("cancelled", true);
            result.put("error", "");
            result.put("videos", new JSONArray());
            sendUnityMessage(result.toString());
        } catch (Exception ignored) {
            sendUnityMessage("");
        }
    }

    private void sendError(String message) {
        try {
            JSONObject result = new JSONObject();
            result.put("cancelled", false);
            result.put("error", message == null ? "unknown" : message);
            result.put("videos", new JSONArray());
            sendUnityMessage(result.toString());
        } catch (Exception ignored) {
            sendUnityMessage("");
        }
    }

    private void sendUnityMessage(String payload) {
        try {
            Class<?> unityPlayerClass = Class.forName("com.unity3d.player.UnityPlayer");
            unityPlayerClass.getMethod("UnitySendMessage", String.class, String.class, String.class)
                .invoke(null, receiverObject, callbackMethod, payload);
        } catch (Exception ignored) {
            // Ignore to avoid crash when Unity bridge is unavailable.
        }
    }
}
