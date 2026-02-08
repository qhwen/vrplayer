# Requirements Tracker

Last Updated: 2026-02-08

## Current Round Requirements

- R-001: On Android 13+/15, the app must provide an in-app permission request entry and trigger media-read permission request when user taps it.
- R-002: If the dialog cannot be shown (for example denied with "Don't ask again"), the app must guide user to system settings.
- R-003: After scanning and finding local videos, the UI must display the video list.
- R-004: Video list entries must be tappable/selectable and trigger playback.
- R-005: Keep this file as the dedicated requirement list for each round; after each change, verify implementation against all items.
- R-006: Local scanning scope on Android must be limited to `Movies` only.
- R-007: After user returns from system settings, the app must auto-refresh permission state and file list.
- R-008: Avoid broad startup auto-request behavior; permission request should be explicit from UI action to improve real-device popup reliability.
- R-009: Avoid long-running broad scans; scanning should not enumerate non-Movies libraries.

## Implementation Mapping

- R-001:
  - `unity_vr_player/Assets/Scripts/VideoBrowserUI.cs`
  - `OnGrantPermissionClicked()` explicitly triggers permission request flow.
- R-002:
  - `unity_vr_player/Assets/Scripts/LocalFileManager.cs`
  - Tracks denied and denied-with-dont-ask-again states.
  - `unity_vr_player/Assets/Scripts/VideoBrowserUI.cs` keeps `Open Settings` path and guidance text.
- R-003:
  - `unity_vr_player/Assets/Scripts/VideoBrowserUI.cs`
  - Rebuilds list items with deterministic layout and content height.
- R-004:
  - `unity_vr_player/Assets/Scripts/VideoBrowserUI.cs`
  - Each list item is a button bound to `PlayVideo(...)`.
  - `unity_vr_player/Assets/Scripts/VRVideoPlayer.cs` avoids drag interception on UI touches.
- R-005:
  - This file is maintained as the dedicated per-round checklist.
- R-006:
  - `unity_vr_player/Assets/Scripts/LocalFileManager.cs`
  - Android scan path now queries MediaStore with `relative_path` constrained to `Movies`.
- R-007:
  - `unity_vr_player/Assets/Scripts/VideoBrowserUI.cs`
  - `OnApplicationFocus(true)` refreshes after returning from settings.
- R-008:
  - `unity_vr_player/Assets/Scripts/VideoBrowserUI.cs`
  - Startup no longer auto-requests permission; request occurs on explicit tap.
- R-009:
  - `unity_vr_player/Assets/Scripts/LocalFileManager.cs`
  - Removes broad Android filesystem scan path and relies on Movies-scoped MediaStore query.

## Verification Checklist

- [x] R-001 covered in code.
- [x] R-002 covered in code.
- [x] R-003 covered in code.
- [x] R-004 covered in code.
- [x] R-005 covered in process/file.
- [x] R-006 covered in code.
- [x] R-007 covered in code.
- [x] R-008 covered in code.
- [x] R-009 covered in code.

## Round Validation (2026-02-08)

- Static code checks completed for changed files.
- Real device validation still required for permission popup behavior on Android 15 OEM variants (especially selected-media vs allow-all choices).
- CI/APK rebuild status: pending this round commit.
