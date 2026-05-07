using QuickFileMarker.Loader.Constants;
using QuickFileMarker.Loader.Contracts;
using QuickFileMarker.Loader.Models;
using QuickFileMarker.Markers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;

namespace QuickFileMarker.Loader
{
    public class QuickFileMarkerLoader : IFileMarkerLoader
    {
        private readonly List<WeakReference<IFileMarkerLoaderListener>> _listeners = new();
        private readonly object _lock = new();
        private string[] _rootPathFilters = Array.Empty<string>();
        private string[] _markerFlags = new[] { "MARKER" };

        private FileSystemWatcher? _watcher;
        private readonly string _tempFolder;

        private readonly Dictionary<string, MarkerRecord> _rawMarkers = new();
        private bool _isDisposed = false;

        public QuickFileMarkerLoader()
        {
            _tempFolder = Path.Combine(Path.GetTempPath(), "FileMarkers");
            if (!Directory.Exists(_tempFolder))
            {
                Directory.CreateDirectory(_tempFolder);
            }

            LoadInitialMarkers();
            
            _watcher = new FileSystemWatcher(_tempFolder, "*.json")
            {
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.CreationTime,
                EnableRaisingEvents = true
            };

            _watcher.Created += Watcher_Changed;
            _watcher.Changed += Watcher_Changed;
            _watcher.Deleted += Watcher_Deleted;
            _watcher.Renamed += Watcher_Renamed;
        }

        public string[] RootPathFilters
        {
            get => _rootPathFilters;
            set
            {
                lock (_lock)
                {
                    _rootPathFilters = value ?? Array.Empty<string>();
                }
            }
        }

        public string[] MarkerFlags
        {
            get => _markerFlags;
            set
            {
                lock (_lock)
                {
                    _markerFlags = value ?? new[] { "MARKER" };
                }
            }
        }

        public IEnumerable<IFileMarkerGroup> MarkerGroups
        {
            get
            {
                lock (_lock)
                {
                    return BuildGroups(_markerFlags, true);
                }
            }
        }

        public IEnumerable<IFileMarkerGroup> OtherGroups
        {
            get
            {
                lock (_lock)
                {
                    return BuildGroups(_markerFlags, false);
                }
            }
        }

        private IEnumerable<IFileMarkerGroup> BuildGroups(string[] flags, bool matchesFlags)
        {
            var filteredRecords = _rawMarkers.Values.Where(r => 
            {
                bool hasFlag = flags.Contains(r.Flag);
                if (matchesFlags && !hasFlag) return false;
                if (!matchesFlags && hasFlag) return false;

                if (_rootPathFilters.Length > 0)
                {
                    bool matchesPath = false;
                    foreach (var filter in _rootPathFilters)
                    {
                        if (r.FilePath != null && r.FilePath.StartsWith(filter, StringComparison.OrdinalIgnoreCase))
                        {
                            matchesPath = true;
                            break;
                        }
                    }
                    if (!matchesPath) return false;
                }

                return true;
            }).ToList();

            var groups = new List<FileMarkerGroup>();

            foreach (var record in filteredRecords.OrderBy(r => GetDateTime(r.TimeStamps)))
            {
                DateTime t = GetDateTime(record.TimeStamps);

                var group = groups.FirstOrDefault(g => Math.Abs((g.GroupTime - t).TotalSeconds) < 5);
                if (group == null)
                {
                    group = new FileMarkerGroup { GroupTime = t };
                    groups.Add(group);
                }

                var marker = group.markers.FirstOrDefault(m => string.Equals(m.FilePath, record.FilePath, StringComparison.OrdinalIgnoreCase)) as FileMarker;
                if (marker == null)
                {
                    marker = new FileMarker
                    {
                        Parent = group,
                        FilePath = record.FilePath
                    };
                    group.markers.Add(marker);
                }

                var section = new FileSection
                {
                    Parent = marker,
                    Flag = record.Flag,
                    Identifier = ParseIdentifierFromFileName(record),
                    SellectedText = record.SellectedText ?? string.Empty,
                    SellectedTextLine = record.SellectedTextLine ?? string.Empty,
                    CarretLine = record.CarretLine ?? string.Empty,
                    CharPositionInCarretLine = record.CharPositionInCarretLine ?? string.Empty,
                    SellectionStartLine = record.SellectionStartLine,
                    SellectionEndLine = record.SellectionEndLine,
                    TimeStamp = t
                };
                marker.sections.Add(section);
            }

            return groups;
        }

        private DateTime GetDateTime(TimeStempRecord t)
        {
            if (t == null) return DateTime.MinValue;
            try { return new DateTime(t.Year, t.Month, t.Day, t.Hour, t.Minute, t.Second); }
            catch { return DateTime.MinValue; }
        }

        private int ParseIdentifierFromFileName(MarkerRecord record)
        {
            var kvp = _rawMarkers.FirstOrDefault(x => x.Value == record);
            if (kvp.Key != null)
            {
                var name = Path.GetFileNameWithoutExtension(kvp.Key);
                if (name.StartsWith("marker-") && int.TryParse(name.Substring(7), out int id))
                {
                    return id;
                }
            }
            return 0;
        }

        public void AddListener(IFileMarkerLoaderListener listener)
        {
            lock (_lock)
            {
                _listeners.Add(new WeakReference<IFileMarkerLoaderListener>(listener));
            }
        }

        public void Dispose()
        {
            if (_isDisposed) return;
            _isDisposed = true;
            if (_watcher != null)
            {
                _watcher.EnableRaisingEvents = false;
                _watcher.Dispose();
            }
        }

        private void LoadInitialMarkers()
        {
            lock (_lock)
            {
                foreach (var file in Directory.GetFiles(_tempFolder, "*.json"))
                {
                    TryLoadFile(file);
                }
            }
        }

        private void Watcher_Changed(object sender, FileSystemEventArgs e)
        {
            bool added;
            MarkerRecord? record;
            lock (_lock)
            {
                added = TryLoadFile(e.FullPath, out record);
            }
            if (added && record != null) NotifyListeners(record);
        }

        private void Watcher_Renamed(object sender, RenamedEventArgs e)
        {
            MarkerRecord? record;
            lock (_lock)
            {
                _rawMarkers.Remove(e.OldFullPath);
                TryLoadFile(e.FullPath, out record);
            }
            if (record != null) NotifyListeners(record);
        }

        private void Watcher_Deleted(object sender, FileSystemEventArgs e)
        {
            bool removed;
            lock (_lock)
            {
                removed = _rawMarkers.Remove(e.FullPath);
            }
            if (removed) NotifyListeners(null); // Simple broadcast
        }

        private bool TryLoadFile(string path, out MarkerRecord? loadedRecord)
        {
            loadedRecord = null;
            try
            {
                for (int i = 0; i < 3; i++)
                {
                    try
                    {
                        string json = File.ReadAllText(path);
                        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                        var record = JsonSerializer.Deserialize<MarkerRecord>(json, options);
                        if (record != null)
                        {
                            _rawMarkers[path] = record;
                            loadedRecord = record;
                            return true;
                        }
                        break;
                    }
                    catch (IOException)
                    {
                        Thread.Sleep(50);
                    }
                }
            }
            catch { }
            return false;
        }

        private bool TryLoadFile(string path) => TryLoadFile(path, out _);

        private void NotifyListeners(MarkerRecord? triggerRecord)
        {
            List<IFileMarkerLoaderListener> activeListeners = new();
            lock (_lock)
            {
                for (int i = _listeners.Count - 1; i >= 0; i--)
                {
                    if (_listeners[i].TryGetTarget(out var listener))
                    {
                        activeListeners.Add(listener);
                    }
                    else
                    {
                        _listeners.RemoveAt(i);
                    }
                }
            }

            if (activeListeners.Count == 0) return;

            // Simple broadcast of all affected groups. In a real-world scenario, we'd only broadcast the changed group.
            var allGroups = MarkerGroups.Concat(OtherGroups).ToList();

            foreach (var listener in activeListeners)
            {
                foreach (var group in allGroups)
                {
                    listener.MarkerAddedOrUpdated(group);
                }
            }
        }
    }
}
