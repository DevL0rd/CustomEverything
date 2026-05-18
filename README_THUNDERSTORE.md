# CustomEverything

CustomEverything is a Resonite BepInEx mod for personal UI customization experiments.


## Install

1. Follow the Resonite modding setup instructions for Gale and BepisLoader:
https://modding.resonite.net/getting-started/installation/

2. Search for CustomEverything and enable the mod.

3. Launch Resonite with Gale.

GitHub release zips are the bleeding-edge manual install path and may update faster than Thunderstore while packages wait for review.


## Features

- Save an inspector panel to inventory, mark it as the active inspector, and new inspector panels will load from that saved object while keeping the live inspected target wired up.
- Save a ProtoFlux node browser to inventory, mark it as the active browser, and opening the ProtoFlux node browser will load your saved layout instead of the stock browser.
- Save a component selector to inventory, mark it as the active picker, and the inspector's attach component flow will open your saved picker.
- Scroll laser-targeted UI panels with controller stick or touchpad input.
- Keep custom UI selections stored in BepInEx config.
- Toggle laser scrolling with the enabled-by-default `EnableLaserScrolling` setting.


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
