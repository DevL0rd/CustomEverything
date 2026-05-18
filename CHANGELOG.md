## 0.1.0 - 2026-05-16

### Initial Inspector Slice
- Added a BepInEx Resonite plugin shell for CustomEverything.
- Added custom inspector selection from inventory items that contain a `SceneInspector`.
- Added custom inspector loading for newly spawned inspector panels while preserving the live inspector target references.
- Added local Gale profile build, deploy, and restart scripting.

### Custom ProtoFlux Browser Slice
- Added custom ProtoFlux browser selection from saved inventory node browsers.
- Added ProtoFlux node browser replacement when opening the ProtoFlux browser.
- Added BepInEx config storage for the selected ProtoFlux browser URI.

### Custom Component Picker Slice
- Added custom component picker selection from saved inventory component selectors.
- Added component picker replacement when attaching components from an inspector.
- Added BepInEx config storage for the selected component picker URI.

### Laser Scrolling Slice
- Added laser scrolling for UI panels that accept axis scrolling.
- Added an enabled-by-default `EnableLaserScrolling` setting.
