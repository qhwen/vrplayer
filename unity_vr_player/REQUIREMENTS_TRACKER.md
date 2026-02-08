# Requirements Tracker

Last Updated: 2026-02-08

## Current Round Requirements

- R-001: Main Android flow must work even without granting storage/media permission first.
- R-002: App must provide a system picker entry (`Select Videos`) with single/multi-select support.
- R-003: Picker-selected videos must appear in list UI and be tappable for playback.
- R-004: Picker-selected videos must persist across app restarts.
- R-005: `Scan Settings` must trigger runtime permission request before asking users to go to Android settings.
- R-006: If permission is denied with "Don't ask again", app should route user to app settings.
- R-007: Auto scan scope must be limited to `Movies` folder only (optional subfolders), not full storage scan.
- R-008: Keep this file as the dedicated requirement list and re-check after each change.
- R-009: APK versionName/versionCode must be visibly incremented in each CI build to avoid installing stale package.

## Implementation Mapping

- R-001:
  - `unity_vr_player/Assets/Scripts/VideoBrowserUI.cs`
  - `unity_vr_player/Assets/Scripts/LocalFileManager.cs`
  - SAF (`Select Videos`) path remains available regardless of scan permission state.
- R-002:
  - `unity_vr_player/Assets/Plugins/Android/SAFPicker.androidlib/src/main/java/com/vrplayer/saf/SafPickerProxyActivity.java`
  - Uses `ACTION_OPEN_DOCUMENT` + `EXTRA_ALLOW_MULTIPLE`.
- R-003:
  - `unity_vr_player/Assets/Scripts/VideoBrowserUI.cs`
  - Dedicated overlay canvas + ensured `EventSystem`, list is rebuilt from `GetLocalVideos()` and clickable.
- R-004:
  - `unity_vr_player/Assets/Scripts/LocalFileManager.cs`
  - Persists picked videos via `PlayerPrefs` (`local_picked_videos_v1`).
- R-005:
  - `unity_vr_player/Assets/Scripts/VideoBrowserUI.cs`
  - `OnOpenSettingsClicked()` now starts `RequestMoviesPermissionFlow()`.
  - `unity_vr_player/Assets/Scripts/LocalFileManager.cs`
  - `RequestReadableMediaPermission()` requests Android runtime permissions.
- R-006:
  - `unity_vr_player/Assets/Scripts/VideoBrowserUI.cs`
  - When denied with "Don't ask again", open app settings automatically.
- R-007:
  - `unity_vr_player/Assets/Scripts/LocalFileManager.cs`
  - Android scan first uses direct `Movies` directory roots only; MediaStore is fallback for `Movies` scope only.
- R-008:
  - This file is maintained and updated in this round.
- R-009:
  - `unity_vr_player/Assets/Editor/BuildAndroid.cs`
  - Build metadata now always resolves non-empty versionCode/versionName (from env or fallback), then writes to PlayerSettings before build.

## Verification Checklist

- [x] R-001 covered in code.
- [x] R-002 covered in code.
- [x] R-003 covered in code.
- [x] R-004 covered in code.
- [x] R-005 covered in code.
- [x] R-006 covered in code.
- [x] R-007 covered in code.
- [x] R-008 covered in process/file.
- [x] R-009 covered in code.

## Round Validation (2026-02-08)

- Local changes include:
  - CI build metadata hardening: force non-empty versionName/versionCode assignment before build.
  - Permission request flow wiring for `Scan Settings`.
  - Android 14/15 partial media permission compatibility handling.
  - `Movies` directory-first scan path.
  - Dedicated overlay canvas + EventSystem guard for tappable list.
  - SAF activity manifest declaration added in Android plugin manifests.
- CI build: pending this commit push.
- Device validation focus:
  - `Scan Settings` should show runtime prompt on first request (or route to settings when blocked).
  - `Select Videos` should still work without granting scan permission.
  - `Movies` scan should stop at `Movies` scope and list entries should be tappable.



