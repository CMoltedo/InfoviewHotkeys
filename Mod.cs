// Info View Hotkeys - a Cities: Skylines II mod
// Lets you bind keyboard shortcuts (numpad by default) to the game's Info View overlays.

namespace InfoviewHotkeys
{
    using Colossal.IO.AssetDatabase;
    using Colossal.Logging;
    using Game;
    using Game.Modding;
    using Game.SceneFlow;

    public class Mod : IMod
    {
        public const string kModName = "InfoviewHotkeys";

        public static ILog Log { get; } = LogManager.GetLogger(kModName).SetShowsErrorsInUI(false);

        public static Setting Settings { get; private set; }

        public void OnLoad(UpdateSystem updateSystem)
        {
            Log.Info(nameof(OnLoad));

            if (GameManager.instance.modManager.TryGetExecutableAsset(this, out var asset))
            {
                Log.Info($"Loaded from {asset.path}");
            }

            // Order follows the official mod template: register UI + locale,
            // register key bindings, then load the saved settings over the defaults.
            Settings = new Setting(this);
            Settings.RegisterInOptionsUI();
            GameManager.instance.localizationManager.AddSource("en-US", new LocaleEN(Settings));
            Settings.RegisterKeyBindings();
            AssetDatabase.global.LoadSettings(kModName, Settings, new Setting(this));

            // The system grabs the registered actions in OnCreate, so it must be
            // registered after RegisterKeyBindings().
            updateSystem.UpdateAt<InfoviewHotkeySystem>(SystemUpdatePhase.UIUpdate);
        }

        public void OnDispose()
        {
            Log.Info(nameof(OnDispose));
            if (Settings != null)
            {
                Settings.UnregisterInOptionsUI();
                Settings = null;
            }
        }
    }
}
