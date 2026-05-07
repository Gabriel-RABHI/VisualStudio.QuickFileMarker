using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace QuickFileMarker.Configuration
{
    internal class ConfigurationRecord
    {
        public List<MenuRecord> Menus { get; set; } = new List<MenuRecord> {
            new MenuRecord() { Label = "Create Marker" },
            new MenuRecord() { Label = "Replace last Marker", OverwriteLast = true },
            new MenuRecord() { Label = "Show Marker", Flag = "SHOW" }
        };

        public int MarkerLifetimeInDays { get; set; } = 30;

        public int MaxMarkerCount { get; set; } = 10_000;
    }

    internal class MenuRecord
    {
        public string Label { get; set; } = "Title";

        public string Flag { get; set; } = "MARKER";

        public string Shortcut { get; set; } = "";

        public bool OverwriteLast { get; set; } = false;
    }
}
