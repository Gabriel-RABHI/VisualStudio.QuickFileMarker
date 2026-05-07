using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace QuickFileMarker.Configuration
{
    /// <summary>
    /// Configuration record, saved in a json file "extention-config.json" in a "QuickFileMarker" standard application data directory.
    /// </summary>
    internal class ConfigurationRecord
    {
        public List<MenuRecord> MenuItems { get; set; } = new List<MenuRecord> {
            new MenuRecord() { Label = "New Marker", Shortcut = new ShortcutRecord() { Key = "M", PrimaryModifier = "Ctrl", SecondaryModifier = "Shift" } },
            new MenuRecord() { Label = "Replace last Marker", OverwriteLastMarker = true },
            new MenuRecord() { Label = "Show Marker", Flag = "SHOW" }
        };

        public int MarkerFileLifetimeInDays { get; set; } = 30;

        public int MaxMarkerFileCount { get; set; } = 1_000;
    }

    internal class MenuRecord
    {
        public string Label { get; set; } = "Menu label";

        public string Flag { get; set; } = "MARKER";

        public ShortcutRecord Shortcut { get; set; } = new ShortcutRecord();

        public bool OverwriteLastMarker { get; set; } = false;
    }

    internal class ShortcutRecord
    {
        public string Key { get; set; } = "";

        public string PrimaryModifier { get; set; } = "";

        public string SecondaryModifier { get; set; } = "";
    }
}
