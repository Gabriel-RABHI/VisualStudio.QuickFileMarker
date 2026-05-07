# QuickFileMarker

## Purpose
QuickFileMarker is a lightweight, cross-platform-ready developer extension that allows users to rapidly generate contextual "marker" files directly from their IDE's text editor. 

Often, developers need a way to flag specific lines of code, save fragments, or temporarily bookmark active text selections to be processed by an external tool or daemon. QuickFileMarker streamlines this workflow by binding directly into the editor's context menu, enabling you to extract your file path, cursor position, selected text, and timestamps in one click—outputting structured JSON payloads into a unified temporary system folder.

## Supported IDEs
QuickFileMarker is available natively for two major IDEs, sharing the exact same unified configuration and output structure:

### 1. Visual Studio 2022 (C# / VSIX)
- Located in the `QuickFileMarker` folder.
- Injects dynamic commands into the right-click context menu.
- Binds global keyboard shortcuts dynamically via the IDE's DTE automation model.

### 2. Visual Studio Code (TypeScript)
- Located in the `QuickFileMarker.VSCode` folder.
- Adds a "Create Quick Marker..." action to the editor context menu.
- Triggers a native Quick Pick dropdown menu to select the marker flag.
- Default keyboard shortcut is `Ctrl+M, Ctrl+M` (double chord) to avoid extension conflicts.

## Technical Result
When invoked, QuickFileMarker captures context from the IDE (`DTE` in Visual Studio) and writes a timestamped JSON file (e.g., `marker-0000001.json`) to `%TMP%\FileMarkers` (or equivalent temporary path on Linux/macOS).

The generated `MarkerRecord` contains:
- **Flag**: The configurable category/type of the marker.
- **FilePath**: Absolute path to the active document.
- **SellectedText**: The raw text explicitly highlighted.
- **SellectedTextLine**: The full string of the line containing the caret.
- **CarretLine & CharPositionInCarretLine**: Exact pointer tracking.
- **TimeStamps**: Precision time logs (Year, Month, Day, Hour, Minute, Second).

The extension implements smart, automatic cleanup. Each time you trigger the extension, it evaluates the `FileMarkers` directory against your configuration constraints (`MaxMarkerFileCount` and `MarkerFileLifetimeInDays`) to safely remove stale files and prevent storage bloat.

## Usage & Configuration
Both the Visual Studio and Visual Studio Code extensions are fully synchronized and read from the exact same configuration file in real-time.

QuickFileMarker reads a portable configuration file `extention-config.json` located inside your system's Application Data folder (`~/.config/QuickFileMarker` on Linux/Mac, or `%APPDATA%\QuickFileMarker` on Windows). 

By modifying this JSON, you can dynamically control the available marker flags, labels, and file retention policies across both IDEs simultaneously without recompiling.

### Configuration Structure
```json
{
  "MenuItems": [
    {
      "Label": "New Marker",
      "Flag": "MARKER",
      "OverwriteLastMarker": false,
      "Shortcut": {
        "Key": "M",
        "PrimaryModifier": "Ctrl",
        "SecondaryModifier": "Shift"
      }
    }
  ],
  "MarkerFileLifetimeInDays": 30,
  "MaxMarkerFileCount": 1000
}
```

### Keystroke Shortcut Definition Patterns
The `ShortcutRecord` configuration object binds your customized context menu items to keyboard shortcuts specifically within the **Text Editor**. 

Visual Studio maps bindings by chaining modifiers and keys with the `+` character. Here is how to configure your `ShortcutRecord`:

- **Key**: The primary activation key (e.g., `"M"`, `"K"`, `"F12"`).
- **PrimaryModifier**: Typically `"Ctrl"` or `"Alt"`.
- **SecondaryModifier**: Typically `"Shift"` or left blank `""`.

*Examples:*
- **Ctrl+Shift+M**: `"PrimaryModifier": "Ctrl"`, `"SecondaryModifier": "Shift"`, `"Key": "M"`
- **Alt+X**: `"PrimaryModifier": "Alt"`, `"SecondaryModifier": "", "Key": "X"`
- **Ctrl+K**: `"PrimaryModifier": "Ctrl"`, `"SecondaryModifier": "", "Key": "K"`

> *Note: If you leave `"Key": ""`, no shortcut will be bound to that menu item.*

## The Loader (`QuickFileMarker.Loader`)
To consume the generated JSON markers within external console apps, background services, or other agentic tooling, this repository provides the `QuickFileMarker.Loader` class library.

The `QuickFileMarkerLoader` provides a robust, thread-safe consumption model:
- **`FileSystemWatcher` Integration**: Automatically detects and loads new/modified `.json` files in real-time.
- **Dynamic Clustering**: Instead of yielding independent files, it clusters markers created within a close time window (e.g., < 5 seconds) into an `IFileMarkerGroup`. This is exceptionally useful for grouping rapid consecutive user selections.
- **Intelligent Validation**: The `MarkerValidity` caching system dynamically checks if the original source code file still exists and verifies if the targeted string snippet (`SellectedText` / `SellectedTextLine`) remains unaltered on the disk—avoiding stale reads. Caching avoids high CPU loads on validation spam.
- **WeakReference Listeners**: Subscribe your logic via `IFileMarkerLoaderListener` without creating memory leak chains.
- **Filtering**: Automatically partitions markers into `MarkerGroups` and `OtherGroups` based on a configurable `MarkerFlags` array, and applies optional target file constraints using `RootPathFilters`.