namespace InfoviewHotkeys
{
    using System.Collections.Generic;
    using Colossal.IO.AssetDatabase;
    using Game.Input;
    using Game.Modding;
    using Game.Settings;
    using Game.UI.Localization;
    using Game.UI.Widgets;
    using Unity.Entities;

    /// <summary>
    /// Mod settings. The per-info-view bindings and the "Other" slots live in
    /// Setting.Generated.cs (made by tools/gen_settings.py); this file has the
    /// general options and the helpers the rest of the mod uses.
    /// </summary>
    [FileLocation("ModsSettings/" + Mod.kModName + "/" + Mod.kModName)]
    [SettingsUITabOrder(kHotkeysTab, kOtherTab, kGeneralTab)]
    [SettingsUIKeyboardAction(kCloseActionName, Usages.kDefaultUsage, Usages.kOverlayUsage, Usages.kToolUsage, Usages.kCancelableToolUsage, Usages.kEditorUsage)]
    public partial class Setting : ModSetting
    {
        public const string kHotkeysTab = "Hotkeys";
        public const string kOtherTab = "Other";
        public const string kGeneralTab = "General";

        public const string kOtherGroup = "OtherSlots";
        public const string kBehaviorGroup = "Behavior";
        public const string kMaintenanceGroup = "Maintenance";
        public const string kAboutGroup = "About";

        public const string kCloseActionName = "CloseInfoview";
        public const string kNoneLocaleId = "InfoviewHotkeys.NONE";

        // Bumped whenever the list of available info views may have changed
        // (e.g. after a save loads) so the dropdowns refresh.
        private static int s_InfoviewListVersion;

        private static HashSet<string> s_BuiltinNames;

        // Backing store for the OtherNNInfoview properties. Field initializers run
        // before the base constructor, so this is ready before anything touches it.
        private readonly string[] m_OtherInfoviews = new string[kOtherSlotCount];

        private DropdownItem<string>[] m_DropdownCache;
        private int m_DropdownCacheVersion = -1;

        public Setting(IMod mod)
            : base(mod)
        {
            SetDefaults();
        }

        // ---------------------------------------------------------------- General tab

        [SettingsUISection(kGeneralTab, kBehaviorGroup)]
        public bool PressAgainToClose { get; set; }

        [SettingsUISection(kGeneralTab, kBehaviorGroup)]
        [SettingsUIKeyboardBinding(kCloseActionName)]
        public ProxyBinding CloseInfoviewKey { get; set; }

        [SettingsUIButton]
        [SettingsUIConfirmation]
        [SettingsUISection(kGeneralTab, kMaintenanceGroup)]
        public bool ClearOtherAssignments
        {
            set
            {
                for (int i = 0; i < m_OtherInfoviews.Length; i++)
                {
                    m_OtherInfoviews[i] = string.Empty;
                }

                ApplyAndSave();
            }
        }

        [SettingsUIButton]
        [SettingsUIConfirmation]
        [SettingsUISection(kGeneralTab, kMaintenanceGroup)]
        public bool ResetKeys
        {
            set
            {
                ResetKeyBindings();
            }
        }

        [SettingsUISection(kGeneralTab, kAboutGroup)]
        public string Version => typeof(Mod).Assembly.GetName().Version.ToString(3);

        public override void SetDefaults()
        {
            PressAgainToClose = true;
            for (int i = 0; i < m_OtherInfoviews.Length; i++)
            {
                m_OtherInfoviews[i] = string.Empty;
            }
        }

        // ---------------------------------------------------------------- Helpers

        public static string OtherInfoviewProperty(int slot) => $"Other{slot:00}Infoview";

        public static string OtherKeyProperty(int slot) => $"Other{slot:00}Key";

        public static string OtherActionName(int slot) => $"OtherInfoview{slot:00}";

        public static bool IsBuiltin(string prefabName)
        {
            if (s_BuiltinNames == null)
            {
                var names = new HashSet<string>();
                foreach (BuiltinInfoview view in kBuiltinInfoviews)
                {
                    names.Add(view.PrefabName);
                }

                s_BuiltinNames = names;
            }

            return s_BuiltinNames.Contains(prefabName);
        }

        public static void NotifyInfoviewListChanged()
        {
            s_InfoviewListVersion++;
        }

        /// <summary>
        /// Version getter for [SettingsUIValueVersion]. The game resolves this by
        /// method name (like GraphicsSettings.GetResolutionItemsVersion), so it
        /// must be a parameterless method, not a property.
        /// </summary>
        public int GetInfoviewListVersion() => s_InfoviewListVersion;

        /// <summary>Info view prefab name assigned to an Other slot (1-based), or empty.</summary>
        public string GetOtherSlotInfoview(int slot)
        {
            return m_OtherInfoviews[slot - 1] ?? string.Empty;
        }

        /// <summary>
        /// Items for the Other slots' dropdowns: every info view the game knows about
        /// that doesn't already have its own row on the Info views tab (i.e. ones
        /// added by DLC, mods, or game updates newer than this mod).
        /// </summary>
        public DropdownItem<string>[] GetOtherInfoviewDropdownItems()
        {
            if (m_DropdownCache != null && m_DropdownCacheVersion == s_InfoviewListVersion)
            {
                return m_DropdownCache;
            }

            var items = new List<DropdownItem<string>>
            {
                new DropdownItem<string>
                {
                    value = string.Empty,
                    displayName = LocalizedString.IdWithFallback(kNoneLocaleId, "(none)"),
                },
            };

            var seen = new HashSet<string>();
            int found = 0;
            InfoviewHotkeySystem system = World.DefaultGameObjectInjectionWorld?.GetExistingSystemManaged<InfoviewHotkeySystem>();
            if (system != null)
            {
                foreach (InfoviewHotkeySystem.InfoviewChoice choice in system.GetSelectableInfoviews())
                {
                    found++;
                    if (!IsBuiltin(choice.Name) && seen.Add(choice.Name))
                    {
                        items.Add(new DropdownItem<string>
                        {
                            value = choice.Name,
                            displayName = choice.DisplayName,
                        });
                    }
                }
            }

            // Keep saved assignments visible even if that info view is missing
            // (e.g. it came from a DLC or mod that's no longer active).
            foreach (string assigned in m_OtherInfoviews)
            {
                if (!string.IsNullOrEmpty(assigned) && seen.Add(assigned))
                {
                    items.Add(new DropdownItem<string>
                    {
                        value = assigned,
                        displayName = LocalizedString.IdWithFallback($"InfoviewHotkeys.MISSING[{assigned}]", $"{assigned} (not found)"),
                    });
                }
            }

            DropdownItem<string>[] result = items.ToArray();

            // Only cache once the game has actually reported its info views.
            if (found > 0)
            {
                m_DropdownCache = result;
                m_DropdownCacheVersion = s_InfoviewListVersion;
            }

            return result;
        }

        private void SetOtherSlot(int slot, string value)
        {
            m_OtherInfoviews[slot - 1] = value ?? string.Empty;
        }

        /// <summary>A built-in info view with its own binding row.</summary>
        public sealed class BuiltinInfoview
        {
            public BuiltinInfoview(string prefabName, string actionName, string keyProperty, string group, string englishName)
            {
                PrefabName = prefabName;
                ActionName = actionName;
                KeyProperty = keyProperty;
                Group = group;
                EnglishName = englishName;
            }

            public string PrefabName { get; }

            public string ActionName { get; }

            public string KeyProperty { get; }

            public string Group { get; }

            public string EnglishName { get; }
        }
    }
}
