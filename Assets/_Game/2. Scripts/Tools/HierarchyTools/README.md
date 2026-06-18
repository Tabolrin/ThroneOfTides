# Hierarchy Folders & Symbols

An ultra-optimized editor utility tool developed to streamline complex Unity scene organization. 

## Key Features
- **Virtual Folders**: Organize nodes instantly. Folders automatically flatten child vectors and destroy themselves at runtime for $0\text{ms}$ engine overhead.
- **Icon Symbols**: Swap default GameObject cube indicators for custom graphics stored inside nested subdirectories.
- **Zero-Allocation Pipeline**: The editor drawing loop uses $O(1)$ dictionary lookups to eliminate Garbage Collection stutters.

## Setup Instructions
1. Drop your custom `.png`, `.jpg`, or `.tga` files anywhere inside the `Editor/Resources/HierarchySymbols/` subdirectory.
2. Right-click any GameObject in the hierarchy and choose **Assign Symbol...** to select your icon.