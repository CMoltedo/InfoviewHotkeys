namespace InfoviewHotkeys
{
    using System.Collections.Generic;
    using Colossal;

    /// <summary>English strings for the options page and key binding labels.</summary>
    public class LocaleEN : IDictionarySource
    {
        private readonly Setting m_Setting;

        public LocaleEN(Setting setting)
        {
            m_Setting = setting;
        }

        public IEnumerable<KeyValuePair<string, string>> ReadEntries(IList<IDictionaryEntryError> errors, Dictionary<string, int> indexCounts)
        {
            var entries = new Dictionary<string, string>
            {
                { m_Setting.GetSettingsLocaleID(), "Info View Hotkeys" },
                { m_Setting.GetBindingMapLocaleID(), "Info View Hotkeys" },

                { m_Setting.GetOptionTabLocaleID(Setting.kHotkeysTab), "Info views" },
                { m_Setting.GetOptionTabLocaleID(Setting.kOtherTab), "Other (DLC & mods)" },
                { m_Setting.GetOptionTabLocaleID(Setting.kGeneralTab), "General" },

                { m_Setting.GetOptionGroupLocaleID(Setting.kOtherGroup), "Info views added by DLC or other mods" },
                { m_Setting.GetOptionGroupLocaleID(Setting.kBehaviorGroup), "Behavior" },
                { m_Setting.GetOptionGroupLocaleID(Setting.kMaintenanceGroup), "Reset" },
                { m_Setting.GetOptionGroupLocaleID(Setting.kAboutGroup), "About" },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.PressAgainToClose)), "Press again to close" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.PressAgainToClose)), "Pressing the hotkey of the info view that's already open closes it. Turn off to have the key only ever open its info view." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.CloseInfoviewKey)), "Close info view" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.CloseInfoviewKey)), "Optional key that closes whichever info view is open." },
                { m_Setting.GetBindingKeyLocaleID(Setting.kCloseActionName), "Close info view" },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.ClearOtherAssignments)), "Clear Other slot assignments" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.ClearOtherAssignments)), "Sets every slot on the Other tab back to (none). Key bindings are left alone." },
                { m_Setting.GetOptionWarningLocaleID(nameof(Setting.ClearOtherAssignments)), "Clear the info view picked for every Other slot?" },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.ResetKeys)), "Clear all key bindings" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.ResetKeys)), "Resets every Info View Hotkeys binding to its default, which is unbound." },
                { m_Setting.GetOptionWarningLocaleID(nameof(Setting.ResetKeys)), "Remove all Info View Hotkeys key bindings?" },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.Version)), "Version" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.Version)), "Installed version of Info View Hotkeys." },

                { Setting.kNoneLocaleId, "(none)" },
            };

            foreach ((string id, string name) in Setting.kBuiltinGroups)
            {
                entries[m_Setting.GetOptionGroupLocaleID(id)] = name;
            }

            foreach (Setting.BuiltinInfoview view in Setting.kBuiltinInfoviews)
            {
                entries[m_Setting.GetOptionLabelLocaleID(view.KeyProperty)] = view.EnglishName;
                entries[m_Setting.GetOptionDescLocaleID(view.KeyProperty)] =
                    $"Key that opens the {view.EnglishName} info view. Pressing it again closes it, unless turned off on the General tab.";
                entries[m_Setting.GetBindingKeyLocaleID(view.ActionName)] = view.EnglishName;
            }

            for (int slot = 1; slot <= Setting.kOtherSlotCount; slot++)
            {
                entries[m_Setting.GetOptionLabelLocaleID(Setting.OtherInfoviewProperty(slot))] = $"Slot {slot}: info view";
                entries[m_Setting.GetOptionDescLocaleID(Setting.OtherInfoviewProperty(slot))] =
                    "Info view this slot opens. Only info views that don't have their own row on the Info views tab are listed. Load a city to see the full list.";

                entries[m_Setting.GetOptionLabelLocaleID(Setting.OtherKeyProperty(slot))] = $"Slot {slot}: key";
                entries[m_Setting.GetOptionDescLocaleID(Setting.OtherKeyProperty(slot))] =
                    "Key that opens the info view picked above.";

                entries[m_Setting.GetBindingKeyLocaleID(Setting.OtherActionName(slot))] = $"Other info view {slot}";
            }

            return entries;
        }

        public void Unload()
        {
        }
    }
}
