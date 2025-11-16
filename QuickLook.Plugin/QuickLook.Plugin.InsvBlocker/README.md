# QuickLook.Plugin.InsvBlocker

This plugin prevents QuickLook from handling `.insv` files (Insta360 panoramic video files).

## Purpose

Insta360Studio has its own QuickLook application with the same name and activation method (pressing spacebar). When both applications are installed, pressing space on `.insv` files would cause both QuickLook windows to appear, creating a conflict.

This plugin solves that issue by having QuickLook claim the file (via high priority) but immediately close without displaying anything, allowing Insta360Studio's QuickLook to handle the file instead.

## Implementation

- **Priority**: `int.MaxValue` (highest priority, checked before all other plugins)
- **Behavior**: 
  - Returns `true` for `CanHandle()` on files with `.insv` extension
  - Sets minimal window size (1x1 pixels) in `Prepare()`
  - Closes the window immediately in `View()` using `DispatcherPriority.Send`

## Technical Details

The plugin prevents the QuickLook window from becoming visible by:
1. Matching `.insv` files with highest priority
2. Setting a minimal window size to reduce visual impact if window briefly appears
3. Closing the window immediately after content is set, before it becomes visible to the user

This approach ensures that Insta360Studio's QuickLook can handle the file without interference.
