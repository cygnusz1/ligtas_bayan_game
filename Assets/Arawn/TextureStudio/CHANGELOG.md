# Texture Studio – Changelog

## [Unreleased] – 2026-02-12

### Added
- **Persistent Material Converter settings** – All window settings are now saved via `EditorPrefs` and restored when the window is reopened or Unity is restarted. Previously, settings such as Source/Target Pipeline, Conversion Mode, Advanced Options, and Texture Generation Options would reset to defaults every time the Material Converter window was opened.
  - Added `SaveSettings()` and `LoadSettings()` methods using `EditorPrefs` (prefix: `TextureStudio_MaterialConverter_`).
  - Added `OnDisable()` to save settings when the window closes.
  - `OnGUI` now detects changes and auto-saves settings on every modification.
  - On first launch (no saved prefs), the Target Pipeline still defaults to the active render pipeline.

### Changed
- `OnEnable()` no longer unconditionally resets `_targetRP` to the detected pipeline. It now loads previously saved settings first.

### Persisted Settings
- Source & Target Pipeline (including custom mapping indices)
- Auto-Detect Source toggle
- Conversion Mode
- Advanced Options panel state, Create Backup, Preserve Render Queue, Log Conversion Details
- Texture Generation Options panel state and Auto-Generate toggle
- All per-map generation toggles (Normal, Metallic, Height, Occlusion, Emission, Bent Normal, Detail, Coat Mask)
- All per-map parameters (strengths, scales, values, colors, algorithms, feather, coverage, mask flags)
