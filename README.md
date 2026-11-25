# Info View Hotkeys (Cities: Skylines II)

Rebindable hotkeys for the game's Info View overlays (Natural Resources, Healthcare & Deathcare, Happiness, and the rest).

- **Info views tab:** every base-game info view has its own key binding row, labelled with the game's name and grouped in the same order as the in-game Info Views menu. All start unbound.
- **Other (DLC & mods) tab:** 5 slots, each a dropdown plus a key, for info views added by DLC, other mods, or game updates newer than this mod. The dropdown lists only info views that don't already have their own row.
- Pressing the key for the info view that's already open closes it. You can turn this off.
- There's an optional **Close info view** key.
- Info views that are still milestone-locked are ignored, the same as in the game's menu.
- Bindings go through the game's own key binding system. Conflicts show in Options like any other binding, and modifiers (Shift/Ctrl/Alt) work.

## Why some info views use a dropdown

The game only lets a mod create key bindings at startup, from attributes compiled into the mod (`ModSetting.RegisterKeyBindings()`). Info views from DLC and other mods load later, so they can't each get a row of their own. Base-game info views are known ahead of time, so they're hard-coded. Everything else goes through the Other slots.

## Build

1. In the game, go to **Options → Modding** and install the modding toolchain. It sets the `CSII_*` environment variables the `.csproj` imports from. Restart your terminal or IDE afterward.
2. Close the game. It only loads mods at startup and can lock the DLL.
3. From this folder, run:
   ```
   dotnet build -c Release
   ```
   The toolchain copies the build into `%USERPROFILE%\AppData\LocalLow\Colossal Order\Cities Skylines II\Mods\InfoviewHotkeys`. The game loads local mods from there automatically.
4. Launch the game.

## Use

**Options → Info View Hotkeys**

- **Info views:** click the binding next to an info view and press a key.
- **Other (DLC & mods):** pick an info view in a slot's dropdown, then set its key. Load a city first so the list is complete.
- **General:** the "press again to close" toggle, the close key, and the reset buttons.

## Files

| File | What it does |
|---|---|
| `Mod.cs` | Entry point. Registers settings, the English locale, and key bindings, then adds the system. |
| `Setting.cs` | General options, the Other-slot dropdown builder, and helpers. |
| `Setting.Generated.cs` | The built-in info view bindings, Other slots, and their action declarations. Generated, don't edit. |
| `tools/gen_settings.py` | Holds the built-in info view table (prefab name, section, English name) and the Other slot count. Run `python tools/gen_settings.py` after editing it, then rebuild. |
| `LocaleEN.cs` | English strings for the options page. |
| `InfoviewHotkeySystem.cs` | Polls the actions each frame and sets `ToolSystem.infoview`. That's the same property the game's Info Views menu sets. |

### Adding a built-in info view after a game update

New base-game info views show up in the Other dropdown automatically. To give one its own row, get its prefab name from the log (below), add it to `GROUPS` in `tools/gen_settings.py`, regenerate, and rebuild. The English name is the game's `Infoviews.INFOVIEW[<prefab name>]` string.

## Troubleshooting

- **Log:** `%USERPROFILE%\AppData\LocalLow\Colossal Order\Cities Skylines II\Logs\InfoviewHotkeys.log`. Each time a city loads, the mod writes every info view it found (prefab name | group | priority | built-in or Other tab). It also warns if a built-in info view is missing from the game.
- **A key does nothing:** look in Options for a red conflict marker on that binding. The game turns off mod bindings that conflict with built-in ones.
- **Other dropdown is empty:** load a city, then reopen Options. The game only reports its info views once prefabs have loaded.

### API compatibility

Built and tested against game version 1.6.4f1. If a game update breaks the build, these members are the most likely to have moved:

- `InfoviewInitializeSystem.infoviews`
- `InfoviewPrefab.m_Group`, `m_Priority`, `isValid`
- `ToolSystem.infoview` and `ToolSystem.activeInfoview`
- `Usages.kOverlayUsage` and `Usages.kCancelableToolUsage`. If either is missing, remove it from the `SettingsUIKeyboardAction` lines.
