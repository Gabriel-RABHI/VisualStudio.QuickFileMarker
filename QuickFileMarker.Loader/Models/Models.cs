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

        private MarkerValidity _cachedValidity = MarkerValidity.MissingFile;
        private DateTime _lastCheckedTime = DateTime.MinValue;

        public MarkerValidity Validity
        {
            get
            {
                string path = Parent.FilePath;
                var fileInfo = new FileInfo(path);

                if (!fileInfo.Exists)
                {
                    _lastCheckedTime = DateTime.MinValue;
                    _cachedValidity = MarkerValidity.MissingFile;
                    return _cachedValidity;
                }

                if (fileInfo.LastWriteTimeUtc == _lastCheckedTime)
                {
                    return _cachedValidity;
                }

                try
                {
                    var lines = File.ReadAllLines(path);
                    if (!int.TryParse(CarretLine, out int lineIdx) || lineIdx < 1 || lineIdx > lines.Length)
                        _cachedValidity = MarkerValidity.RangeOverflow;
                    else if (!string.IsNullOrEmpty(SellectedTextLine) && !lines[lineIdx - 1].Contains(SellectedTextLine.Trim()))
                        _cachedValidity = MarkerValidity.SellectedTextLineMissing;
                    else if (!string.IsNullOrEmpty(SellectedText) && !string.Join("\n", lines).Contains(SellectedText))
                        _cachedValidity = MarkerValidity.SellectedTextMissing;
                    else
                        _cachedValidity = MarkerValidity.Valid;

                    _lastCheckedTime = fileInfo.LastWriteTimeUtc;
                    return _cachedValidity;
                }
                catch
                {
                    _lastCheckedTime = DateTime.MinValue;
                    _cachedValidity = MarkerValidity.MissingFile;
                    return _cachedValidity;
                }
            }
        }
    }
}
