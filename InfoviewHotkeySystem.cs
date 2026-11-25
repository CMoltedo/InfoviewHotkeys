namespace InfoviewHotkeys
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using Colossal.Entities;
    using Colossal.Serialization.Entities;
    using Game;
    using Game.Input;
    using Game.Prefabs;
    using Game.Tools;
    using Game.UI.Localization;
    using Unity.Entities;

    /// <summary>
    /// Watches the hotkey actions and switches the active info view.
    /// Uses ToolSystem.infoview, which is exactly what the game's own
    /// Info Views menu sets.
    /// </summary>
    public partial class InfoviewHotkeySystem : GameSystemBase
    {
        private ToolSystem m_ToolSystem;
        private PrefabSystem m_PrefabSystem;
        private InfoviewInitializeSystem m_InfoviewInitializeSystem;

        // One per Setting.kBuiltinInfoviews entry, same order.
        private ProxyAction[] m_BuiltinActions;

        private ProxyAction[] m_OtherActions;
        private bool[] m_OtherEnabled;

        private ProxyAction m_CloseAction;

        // Built-in and close actions have a fixed purpose, so they're on whenever
        // a city (or the editor) is loaded.
        private bool m_FixedActionsEnabled;

        private bool m_InGame;
        private readonly HashSet<string> m_WarnedNames = new HashSet<string>();

        /// <summary>An info view the player can pick in an Other slot dropdown.</summary>
        public readonly struct InfoviewChoice
        {
            public InfoviewChoice(string name, LocalizedString displayName)
            {
                Name = name;
                DisplayName = displayName;
            }

            public string Name { get; }

            public LocalizedString DisplayName { get; }
        }

        /// <summary>
        /// All valid info views, sorted like the in-game menu (group, priority, name).
        /// Display names use the game's own localization key for the menu entry, and
        /// fall back to a prettified prefab name if that key isn't found.
        /// </summary>
        public List<InfoviewChoice> GetSelectableInfoviews()
        {
            var result = new List<InfoviewChoice>();
            foreach (InfoviewPrefab prefab in GetSortedInfoviews())
            {
                result.Add(new InfoviewChoice(
                    prefab.name,
                    LocalizedString.IdWithFallback($"Infoviews.INFOVIEW[{prefab.name}]", Prettify(prefab.name))));
            }

            return result;
        }

        protected override void OnCreate()
        {
            base.OnCreate();

            m_ToolSystem = World.GetOrCreateSystemManaged<ToolSystem>();
            m_PrefabSystem = World.GetOrCreateSystemManaged<PrefabSystem>();
            m_InfoviewInitializeSystem = World.GetOrCreateSystemManaged<InfoviewInitializeSystem>();

            Setting settings = Mod.Settings;

            m_BuiltinActions = new ProxyAction[Setting.kBuiltinInfoviews.Length];
            for (int i = 0; i < m_BuiltinActions.Length; i++)
            {
                m_BuiltinActions[i] = settings.GetAction(Setting.kBuiltinInfoviews[i].ActionName);
            }

            m_OtherActions = new ProxyAction[Setting.kOtherSlotCount];
            m_OtherEnabled = new bool[Setting.kOtherSlotCount];
            for (int slot = 1; slot <= Setting.kOtherSlotCount; slot++)
            {
                m_OtherActions[slot - 1] = settings.GetAction(Setting.OtherActionName(slot));
            }

            m_CloseAction = settings.GetAction(Setting.kCloseActionName);

            SetAllEnabled(false);
            Mod.Log.Info($"{nameof(InfoviewHotkeySystem)} created with {m_BuiltinActions.Length} built-in info views and {Setting.kOtherSlotCount} other slots.");
        }

        protected override void OnGameLoadingComplete(Purpose purpose, GameMode mode)
        {
            base.OnGameLoadingComplete(purpose, mode);

            m_InGame = mode.IsGameOrEditor();
            if (!m_InGame)
            {
                SetAllEnabled(false);
            }

            // Info view list can differ per session (DLC, other mods), so refresh the dropdowns.
            Setting.NotifyInfoviewListChanged();

            if (m_InGame)
            {
                LogAvailableInfoviews();
            }
        }

        protected override void OnUpdate()
        {
            if (!m_InGame)
            {
                return;
            }

            Setting settings = Mod.Settings;
            if (settings == null)
            {
                return;
            }

            if (!m_FixedActionsEnabled)
            {
                foreach (ProxyAction action in m_BuiltinActions)
                {
                    action.shouldBeEnabled = true;
                }

                m_CloseAction.shouldBeEnabled = true;
                m_FixedActionsEnabled = true;
            }

            for (int i = 0; i < m_BuiltinActions.Length; i++)
            {
                if (m_BuiltinActions[i].WasPerformedThisFrame())
                {
                    ToggleInfoview(Setting.kBuiltinInfoviews[i].PrefabName, settings.PressAgainToClose);
                }
            }

            for (int slot = 1; slot <= Setting.kOtherSlotCount; slot++)
            {
                ProxyAction action = m_OtherActions[slot - 1];
                string target = settings.GetOtherSlotInfoview(slot);

                // Only claim the key while the slot actually has an info view assigned.
                bool wantEnabled = !string.IsNullOrEmpty(target);
                if (m_OtherEnabled[slot - 1] != wantEnabled)
                {
                    action.shouldBeEnabled = wantEnabled;
                    m_OtherEnabled[slot - 1] = wantEnabled;
                }

                if (wantEnabled && action.WasPerformedThisFrame())
                {
                    ToggleInfoview(target, settings.PressAgainToClose);
                }
            }

            if (m_CloseAction.WasPerformedThisFrame() && m_ToolSystem.activeInfoview != null)
            {
                m_ToolSystem.infoview = null;
            }
        }

        private void ToggleInfoview(string prefabName, bool pressAgainToClose)
        {
            if (!m_PrefabSystem.TryGetPrefab(new PrefabID(nameof(InfoviewPrefab), prefabName), out PrefabBase prefabBase)
                || !(prefabBase is InfoviewPrefab infoview))
            {
                WarnOnce(prefabName, $"Info view '{prefabName}' was not found.");
                return;
            }

            if (m_ToolSystem.activeInfoview == infoview)
            {
                if (pressAgainToClose)
                {
                    m_ToolSystem.infoview = null;
                }

                return;
            }

            // Respect the game's milestone locks, same as the Info Views menu does.
            if (m_PrefabSystem.TryGetEntity(infoview, out Entity entity)
                && EntityManager.HasEnabledComponent<Locked>(entity))
            {
                Mod.Log.Info($"Info view '{prefabName}' is still locked; ignoring hotkey.");
                return;
            }

            m_ToolSystem.infoview = infoview;
        }

        private IEnumerable<InfoviewPrefab> GetSortedInfoviews()
        {
            List<InfoviewPrefab> infoviews;
            try
            {
                infoviews = m_InfoviewInitializeSystem.infoviews
                    .Where(p => p != null && p.isValid)
                    .ToList();
            }
            catch (Exception e)
            {
                Mod.Log.Warn($"Could not read info view list: {e.Message}");
                return Enumerable.Empty<InfoviewPrefab>();
            }

            return infoviews
                .OrderBy(p => p.m_Group)
                .ThenBy(p => p.m_Priority)
                .ThenBy(p => p.name, StringComparer.Ordinal);
        }

        private void LogAvailableInfoviews()
        {
            var sb = new StringBuilder("Available info views (prefab name | group | priority | row):");
            int count = 0;
            var present = new HashSet<string>();
            foreach (InfoviewPrefab prefab in GetSortedInfoviews())
            {
                string row = Setting.IsBuiltin(prefab.name) ? "built-in" : "Other tab";
                sb.Append($"\n  {prefab.name} | {prefab.m_Group} | {prefab.m_Priority} | {row}");
                present.Add(prefab.name);
                count++;
            }

            sb.Append($"\n  ({count} total)");

            if (count > 0)
            {
                foreach (Setting.BuiltinInfoview view in Setting.kBuiltinInfoviews)
                {
                    if (!present.Contains(view.PrefabName))
                    {
                        sb.Append($"\n  Built-in info view '{view.PrefabName}' wasn't found in this game version; its hotkey won't do anything.");
                    }
                }
            }

            Mod.Log.Info(sb.ToString());
        }

        private void SetAllEnabled(bool enabled)
        {
            foreach (ProxyAction action in m_BuiltinActions)
            {
                action.shouldBeEnabled = enabled;
            }

            for (int i = 0; i < m_OtherActions.Length; i++)
            {
                m_OtherActions[i].shouldBeEnabled = enabled;
                m_OtherEnabled[i] = enabled;
            }

            m_CloseAction.shouldBeEnabled = enabled;
            m_FixedActionsEnabled = enabled;
        }

        private void WarnOnce(string key, string message)
        {
            if (m_WarnedNames.Add(key))
            {
                Mod.Log.Warn(message);
            }
        }

        // "OutsideConnections" -> "Outside Connections"
        private static string Prettify(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return name;
            }

            var sb = new StringBuilder(name.Length + 8);
            for (int i = 0; i < name.Length; i++)
            {
                char c = name[i];
                if (i > 0 && char.IsUpper(c) && !char.IsUpper(name[i - 1]) && name[i - 1] != ' ')
                {
                    sb.Append(' ');
                }

                sb.Append(c);
            }

            return sb.ToString();
        }
    }
}
