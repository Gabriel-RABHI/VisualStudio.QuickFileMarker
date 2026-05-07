using QuickFileMarker.Loader.Constants;
using QuickFileMarker.Loader.Contracts;
using QuickFileMarker.Markers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace QuickFileMarker.Loader.Models
{
    public class FileMarkerGroup : IFileMarkerGroup
    {
        public List<IFileMarker> markers = new();
        public IEnumerable<IFileMarker> Markers => markers;

        public object? ClientToken { get; set; }

        public DateTime GroupTime { get; set; }
    }

    public class FileMarker : IFileMarker
    {
        public IFileMarkerGroup Parent { get; set; } = null!;
        public string FilePath { get; set; } = string.Empty;

        public List<IFileSection> sections = new();
        public IEnumerable<IFileSection> Sections => sections;

        public MarkerValidity Validity
        {
            get
            {
                if (!File.Exists(FilePath))
                    return MarkerValidity.MissingFile;

                // A marker is valid if at least one section is valid, or evaluate worst case?
                // The interface states "Compute if the Marker is valid" without strict rules.
                // We return the "worst" validity of sections or MissingFile.
                MarkerValidity result = MarkerValidity.Valid;
                foreach (var section in Sections)
                {
                    var sv = section.Validity;
                    if (sv != MarkerValidity.Valid)
                    {
                        if (result == MarkerValidity.Valid || sv < result)
                            result = sv; // Just take any non-valid
                    }
                }
                return result;
            }
        }

        public object? ClientToken { get; set; }
    }

    public class FileSection : IFileSection
    {
        public IFileMarker Parent { get; set; } = null!;
        public string Flag { get; set; } = string.Empty;
        public int Identifier { get; set; }
        public string SellectedText { get; set; } = string.Empty;
        public string SellectedTextLine { get; set; } = string.Empty;
        public string CarretLine { get; set; } = string.Empty;
        public string CharPositionInCarretLine { get; set; } = string.Empty;
        public int SellectionStartLine { get; set; }
        public int SellectionEndLine { get; set; }
        public DateTime TimeStamp { get; set; }
        public object? ClientToken { get; set; }

        public MarkerValidity Validity
        {
            get
            {
                string path = Parent.FilePath;
                if (!File.Exists(path)) return MarkerValidity.MissingFile;

                try
                {
                    var lines = File.ReadAllLines(path);
                    if (!int.TryParse(CarretLine, out int lineIdx) || lineIdx < 1 || lineIdx > lines.Length)
                        return MarkerValidity.RangeOverflow;

                    // 1-based line index from VS typically
                    string fileLine = lines[lineIdx - 1];

                    if (!string.IsNullOrEmpty(SellectedTextLine) && !fileLine.Contains(SellectedTextLine.Trim()))
                        return MarkerValidity.SellectedTextLineMissing;

                    if (!string.IsNullOrEmpty(SellectedText))
                    {
                        string allText = File.ReadAllText(path);
                        if (!allText.Contains(SellectedText))
                            return MarkerValidity.SellectedTextMissing;
                    }

                    return MarkerValidity.Valid;
                }
                catch
                {
                    return MarkerValidity.MissingFile;
                }
            }
        }
    }
}
