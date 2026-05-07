using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuickFileMarker.Configuration
{
    /// <summary>
    /// Configuration record, saved in a json file "incremental-id.json" in a "QuickFileMarker" standard application data directory.
    /// </summary>
    internal class IncrementalIdentifierRecord
    {
        public int LastIdentifier { get; set; }
    }
}
