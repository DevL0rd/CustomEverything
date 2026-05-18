# CustomEverything

<p align="center">
  <img src="icon.png" alt="CustomEverything icon" width="512">
</p>

CustomEverything is a Resonite BepInEx mod for personal UI customization experiments.


## Install

### Easy Install

1. Follow the Resonite modding setup instructions for Gale and BepisLoader:
https://modding.resonite.net/getting-started/installation/

2. Search for CustomEverything and enable the mod.

3. Launch Resonite with Gale.

Thunderstore packages update more slowly because every release can require review.

### Manual Install

Manual GitHub release zips are the bleeding-edge path. They include the BepInEx plugin in one self-contained zip.

1. Download `CustomEverything-x.y.z.zip` from the latest [GitHub release](https://github.com/DevL0rd/CustomEverything/releases), then extract it into the correct root folder. The zip contains the `BepInEx` folder used by the manual install layout.

2. Choose install method:
For Gale, extract into the profile root:

```text
%APPDATA%\com.kesomannen.gale\resonite\profiles\Default
```

For another Gale profile, replace `Default` with that profile folder name.

For a manual BepisLoader install, extract into the Resonite install folder:

```text
C:\Program Files (x86)\Steam\steamapps\common\Resonite
```

For manual installs, launch Resonite with BepisLoader enabled, such as with `--hookfxr-enable`.

Install or enable these loader packages too:

- BepisLoader
- BepisResoniteWrapper


## Features

- Save an inspector panel to inventory, mark it as the active inspector, and new inspector panels will load from that saved object while keeping the live inspected target wired up.
- Save a ProtoFlux node browser to inventory, mark it as the active browser, and opening the ProtoFlux node browser will load your saved layout instead of the stock browser.
- Save a component selector to inventory, mark it as the active picker, and the inspector's attach component flow will open your saved picker.
- Scroll laser-targeted UI panels with controller stick or touchpad input.
- Keep custom UI selections stored in BepInEx config.
- Toggle laser scrolling with the enabled-by-default `EnableLaserScrolling` setting.


## Building

Install:

- .NET 10 SDK
- Windows SDK 10.0.26100.0 or newer

Build locally:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\build.ps1 -Restart
```

This builds the BepInEx plugin, deploys it into the local Gale profile named `Default` when present, and restarts Resonite through the root HookFxr loader with that profile's BepInEx target. Add `-Desktop` for desktop mode:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\build.ps1 -Restart -Desktop
```

Use a different Gale profile name with `-ProfileName`, or an exact profile path with `-ProfilePath`:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\build.ps1 -Restart -ProfileName MyProfile
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\build.ps1 -Restart -ProfilePath "$env:APPDATA\com.kesomannen.gale\resonite\profiles\MyProfile"
```

CI-style compile without deploy:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\build.ps1 -NoDeploy
```


## Packaging And Release

Thunderstore package metadata lives in `thunderstore.toml`, while `scripts\package.ps1` builds a clean GitHub release zip for manual installation into a Gale profile or manual BepisLoader root. `VERSION` is the source of truth for the plugin package. After changing it, run:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\sync-version.ps1
```

Create the manual GitHub release zip locally with:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\package.ps1
```

To build the Thunderstore package for manager/testing use, add `-ThunderstoreFormat`:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\package.ps1 -ThunderstoreFormat
```

GitHub Actions refreshes the manual release zip on pushes to the release branch, even when `VERSION` does not change. Thunderstore publishing is separate and only uploads exact package versions that do not already exist.


## Credits

Special thanks to the projects and libraries CustomEverything builds on.

| Project | What CustomEverything uses it for |
| --- | --- |
| [BepisLoader](https://thunderstore.io/c/resonite/p/ResoniteModding/BepisLoader/) | Game-side BepInEx loader |
| [BepisResoniteWrapper](https://github.com/ResoniteModding/BepisResoniteWrapper) | Resonite BepInEx plugin support |
| [Harmony](https://github.com/pardeike/Harmony) | Runtime patching |
| [CustomInspectors](https://github.com/art0007i/CustomInspectors) | Inspiration for custom inspector workflows |
| [InspectorScroll](https://github.com/art0007i/InspectorScroll) | Reference point for laser scrolling behavior |
| [SpecialItemsLib](https://github.com/art0007i/SpecialItemsLib) | Inspiration for inventory-driven custom UI selection |
| [CustomProtofluxBrowser](https://github.com/AlexW-578/CustomProtofluxBrowser) | Inspiration for custom ProtoFlux browser workflows |


## License

AGPL-3.0 - see [LICENSE](LICENSE).
