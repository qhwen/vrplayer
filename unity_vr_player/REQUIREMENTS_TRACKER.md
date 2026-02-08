# Requirements Tracker

Last Updated: 2026-02-08

## Current Round Requirements

- R-001: Main Android flow must work without relying on storage permission popup.
- R-002: App must provide a system picker entry (`Select Videos`) that supports selecting one or multiple video files.
- R-003: Picker-selected videos must appear in the in-app list and be tappable for playback.
- R-004: Picker-selected videos should persist across app restarts.
- R-005: `Movies` auto-scan remains optional enhancement; it should run only when media permission is granted.
- R-006: Keep this file as the dedicated per-round requirement list and verify after each change.

## Implementation Mapping

- R-001:
  - `unity_vr_player/Assets/Plugins/Android/SAFPicker.androidlib/src/main/java/com/vrplayer/saf/SafPickerProxyActivity.java`
  - Uses `ACTION_OPEN_DOCUMENT` and returns selected `content://` URIs.
- R-002:
  - `unity_vr_player/Assets/Scripts/VideoBrowserUI.cs`
  - `Select Videos` button triggers `LocalFileManager.OpenFilePicker()`.
  - `unity_vr_player/Assets/Scripts/LocalFileManager.cs`
  - Calls `SafPickerBridge.launchVideoPicker(...)`.
- R-003:
  - `unity_vr_player/Assets/Scripts/LocalFileManager.cs`
  - `OnAndroidVideoPickerResult(...)` parses picker payload and appends picked videos.
  - `unity_vr_player/Assets/Scripts/VideoBrowserUI.cs`
  - Subscribes to `LocalVideoLibraryChanged` and refreshes list immediately.
- R-004:
  - `unity_vr_player/Assets/Scripts/LocalFileManager.cs`
  - Persists picked videos in `PlayerPrefs` (`local_picked_videos_v1`) and reloads at startup.
- R-005:
  - `unity_vr_player/Assets/Scripts/LocalFileManager.cs`
  - `RefreshLocalVideos()` always includes picked videos; only scans `Movies` via MediaStore when permission exists.
- R-006:
  - This file is maintained and updated in this round.

## Verification Checklist

- [x] R-001 covered in code.
- [x] R-002 covered in code.
- [x] R-003 covered in code.
- [x] R-004 covered in code.
- [x] R-005 covered in code.
- [x] R-006 covered in process/file.

## Round Validation (2026-02-08)

- Commits: `8bc45b9`, `99c309d`, `11001c0`.
- CI build: `https://github.com/qhwen/vrplayer/actions/runs/21799361879` (success).
- Built APK: `downloads/VRVideoPlayer.apk`.
- Device validation focus:
  - Verify `Select Videos` works on Android 15 without granting media permission.
  - Verify selected entries persist after process restart.
