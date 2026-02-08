# Requirements Tracker

Last Updated: 2026-02-08

## Current Round Requirements

- R-001: On Android 13+/15, the app should actively request media read permission in-app (popup should appear when system allows).
- R-002: If permission dialog cannot be shown (for example, denied with "Don't ask again"), the app must clearly guide user to system settings.
- R-003: After scanning and finding local videos, the UI must display the video list.
- R-004: Video list entries must be tappable/selectable and trigger playback.
- R-005: Keep this file as the dedicated requirement list for each round; after each change, verify implementation against all items.

## Implementation Mapping

- R-001:
  - `unity_vr_player/Assets/Scripts/LocalFileManager.cs`
  - Uses SDK-aware permission checks for Android 13+/14+.
  - Requests `READ_MEDIA_VIDEO` (and `READ_MEDIA_VISUAL_USER_SELECTED` fallback on Android 14+).
- R-002:
  - `unity_vr_player/Assets/Scripts/LocalFileManager.cs`
  - Tracks denied and denied-with-dont-ask-again states.
  - `unity_vr_player/Assets/Scripts/VideoBrowserUI.cs` shows fallback hints and keeps "Open Settings" action.
- R-003:
  - `unity_vr_player/Assets/Scripts/VideoBrowserUI.cs`
  - Uses deterministic manual item layout in scroll content and explicit content height update.
- R-004:
  - `unity_vr_player/Assets/Scripts/VideoBrowserUI.cs`
  - Each list item is a button with click callback to `PlayVideo(...)`.
  - `unity_vr_player/Assets/Scripts/AppRuntimeBootstrap.cs` ensures `EventSystem` + `StandaloneInputModule`.
  - `unity_vr_player/Assets/Scripts/VRVideoPlayer.cs` improves UI hit detection and avoids drag-intercept on UI touches.
- R-005:
  - This file is the dedicated per-round tracker.

## Verification Checklist

- [x] R-001 covered in code.
- [x] R-002 covered in code.
- [x] R-003 covered in code.
- [x] R-004 covered in code.
- [x] R-005 covered in process/file.

## Round Validation (2026-02-08)

- Commit: `7df76ca`
- CI build: `https://github.com/qhwen/vrplayer/actions/runs/21797566828` (success)
- Built APK: `downloads/VRVideoPlayer.apk`
- Manifest permission check: contains `READ_MEDIA_VIDEO`, `READ_MEDIA_VISUAL_USER_SELECTED`, and legacy `READ_EXTERNAL_STORAGE` (maxSdkVersion=32).
- Note: popup timing and touch interaction require real-device verification; CI can only validate build/package integrity.
