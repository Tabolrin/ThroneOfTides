# Changelog

All notable changes to this package are documented in this file.
The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.0.0] - 2026-06-18

### Added
- Virtual hierarchy folders that group GameObjects in the editor and are removed
  automatically before builds and Play Mode, leaving no runtime component or overhead.
- Runtime fallback that unpacks folders baked into prefabs which are instantiated
  dynamically, before any user scripts run.
- Canvas-aware folder creation: a folder created under a Canvas receives a stretched
  RectTransform so its UI children keep their positions when the folder is dissolved.
- Custom row icons ("symbols") that can be assigned to any GameObject from a searchable
  popup picker, with multi-selection support and Undo/Redo.
- Hierarchy overlay that draws folder and symbol icons using cached lookups to keep
  per-row drawing inexpensive in large scenes.
- "Folder" creation shortcut (Ctrl/Cmd+Shift+F) and a right-click "Assign Symbol..."
  context-menu entry.