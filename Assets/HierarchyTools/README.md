# Hierarchy Folders & Symbols

A Unity editor utility for keeping the Scene Hierarchy organized. It adds two things:

- **Folders** — lightweight grouping objects that tidy the hierarchy in the editor and are
  removed automatically before the game runs, so they cost nothing at runtime.
- **Symbols** — custom icons you can assign to any GameObject to replace the default
  cube/icon in the hierarchy row.

## Requirements

- Unity 6 (6000.0) or newer. (Lower versions may work but are untested; see the publishing
  notes if you intend to widen support.)

## Installation

Import the package from the Asset Store, or add it via the Package Manager. All scripts live
under the `HierarchyTools.Editor` and `HierarchyTools.Runtime` assemblies; the runtime
assembly contains only the small components needed for the prefab/runtime fallback.

## Folders

Create a folder with **GameObject > Folder**, the Hierarchy right-click menu, or
**Ctrl/Cmd + Shift + F**. Parent any objects under it to group them.

Folders are dissolved automatically:

- **Before a build or entering Play Mode**, a scene processor moves each folder's children up
  to the folder's parent (preserving sibling order and world transforms) and deletes the
  folder. Nested folders are unpacked deepest-first.
- **At runtime**, if a folder is baked into a prefab that you instantiate dynamically, the
  component unpacks itself in `Awake` before your scripts run, as a fallback.

Because the folder is fully removed, it leaves no component or hierarchy overhead in a build.

### UI / Canvas

A folder created while the context object is under a Canvas is given a stretched
`RectTransform` (full anchors, zero offsets). Since the folder then exactly matches its
parent's rect, its UI children keep their anchored positions when the folder is dissolved.

## Symbols

Right-click a GameObject and choose **Assign Symbol...** to open the picker. Pick an icon to
apply it, use the search field to filter, or choose **None** to clear. Multiple selected
objects are updated at once, and changes support Undo/Redo.

### Adding your own symbols

1. Create a folder named exactly **`HierarchySymbols`** anywhere in your project. Placing it
   inside an `Editor` folder is recommended so the icons stay out of player builds.
2. Drop `.png`, `.jpg`, or `.tga` files into it.
3. Each file's name (lowercased, without extension) becomes its key in the picker, so keep
   file names unique.

Symbols are editor-only metadata. Like folders, they are stripped before builds and Play
Mode and have a runtime self-removal fallback, so they never affect a build.

## Known limitations

- **Layout groups:** a folder placed directly inside a `HorizontalLayoutGroup`,
  `VerticalLayoutGroup`, or `GridLayoutGroup` is treated by the group as a single element
  while editing, so the layout preview will look wrong until the folder is stripped at
  build/Play time (where it resolves correctly). Avoid nesting folders directly inside layout
  groups if you need an accurate edit-time preview.
- **Prefab root:** do not use a folder as the root of a prefab. It deletes itself at runtime,
  which would break `Instantiate()` on that prefab. Wrap it in a normal GameObject instead;
  the tool warns you if you do this.
- **Alternating row colors:** if you enable alternating hierarchy row colors in Preferences,
  the icon background fill may not perfectly match every other row. This is cosmetic only.

## License

MIT. See `LICENSE.md`.

## Support

Issues and feature requests: REPLACE_WITH_YOUR_CONTACT_OR_REPO_URL