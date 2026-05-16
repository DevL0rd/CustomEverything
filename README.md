# CustomEverything

<p align="center">
  <img src="icon.png" alt="CustomEverything icon" width="512">
</p>

CustomEverything is a Resonite BepInEx mod for personal UI customization experiments.

Current feature slices:

- Custom inspectors: save an inspector panel to inventory, mark it as the active inspector, and new inspector panels will load from that saved object while keeping the live inspected target wired up.
- Custom ProtoFlux browsers: save a ProtoFlux node browser to inventory, mark it as the active browser, and opening the ProtoFlux node browser will load your saved layout instead of the stock browser.

## Building

Install:

- .NET 10 SDK
- Windows SDK 10.0.26100.0 or newer

Build and deploy to the local Gale `Default` profile:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\build.ps1
```

Build, deploy, and restart Resonite:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\build.ps1 -Restart
```

Use a different Gale profile:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\build.ps1 -Restart -ProfileName MyProfile
```
