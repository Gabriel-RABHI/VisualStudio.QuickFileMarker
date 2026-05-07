using QuickFileMarker.Loader;
using QuickFileMarker.Loader.Contracts;
using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using Xunit;

namespace QuickFileMarker.Loader.Tests
{
    public class LoaderTests : IDisposable
    {
        private string _tempFolder;
        private string _testFile;

        public LoaderTests()
        {
            _tempFolder = Path.Combine(Path.GetTempPath(), "FileMarkers");
            if (!Directory.Exists(_tempFolder))
                Directory.CreateDirectory(_tempFolder);

            // Clean up existing markers for testing
            foreach (var file in Directory.GetFiles(_tempFolder, "*.json"))
            {
                try { File.Delete(file); } catch { }
            }

            _testFile = Path.GetTempFileName();
            File.WriteAllText(_testFile, "public class Test { \n  public void Method() { } \n}");
        }

        public void Dispose()
        {
            if (File.Exists(_testFile))
                File.Delete(_testFile);

            foreach (var file in Directory.GetFiles(_tempFolder, "*.json"))
            {
                try { File.Delete(file); } catch { }
            }
        }

        [Fact]
        public void Loader_CatchesNewMarkerFile_AndValidatesIt()
        {
            using var loader = new QuickFileMarkerLoader();
            var listener = new TestListener();
            loader.AddListener(listener);

            var markerPath = Path.Combine(_tempFolder, "marker-0000001.json");
            var markerData = new
            {
                Flag = "MARKER",
                FilePath = _testFile,
                SellectedText = "public void Method()",
                SellectedTextLine = "public void Method() { }",
                CarretLine = "2",
                CharPositionInCarretLine = "4",
                SellectionStartLine = 2,
                SellectionEndLine = 2,
                TimeStamps = new
                {
                    Year = DateTime.Now.Year,
                    Month = DateTime.Now.Month,
                    Day = DateTime.Now.Day,
                    Hour = DateTime.Now.Hour,
                    Minute = DateTime.Now.Minute,
                    Second = DateTime.Now.Second
                }
            };

            File.WriteAllText(markerPath, JsonSerializer.Serialize(markerData));

            // Wait for FileSystemWatcher to pick it up
            Thread.Sleep(1000);

            Assert.True(listener.ReceivedEvent);
            
            var groups = loader.MarkerGroups.ToList();
            Assert.Single(groups);

            var group = groups[0];
            Assert.Single(group.Markers);

            var marker = group.Markers.First();
            Assert.Equal(_testFile, marker.FilePath);
            Assert.Equal(QuickFileMarker.Loader.Constants.MarkerValidity.Valid, marker.Validity);
            
            Assert.Single(marker.Sections);
            var section = marker.Sections.First();
            Assert.Equal("public void Method()", section.SellectedText);
        }

        [Fact]
        public void Loader_ClustersGroupsByTimestamp()
        {
            using var loader = new QuickFileMarkerLoader();
            
            var baseTime = DateTime.Now.AddDays(-1);

            // Group 1: 3 markers close to each other
            CreateMarker("marker-0000001.json", _testFile, baseTime);
            CreateMarker("marker-0000002.json", _testFile, baseTime.AddSeconds(2));
            CreateMarker("marker-0000003.json", _testFile, baseTime.AddSeconds(4));

            // Group 2: Long delay
            CreateMarker("marker-0000004.json", _testFile, baseTime.AddSeconds(20));

            Thread.Sleep(1000); // give watcher time
            
            var groups = loader.MarkerGroups.OrderBy(g => g.Markers.First().Sections.First().TimeStamp).ToList();
            
            Assert.Equal(2, groups.Count);
            Assert.Equal(3, groups[0].Markers.First().Sections.Count());
            Assert.Single(groups[1].Markers.First().Sections);
        }

        [Fact]
        public void Loader_CombinesSectionsForSameFile()
        {
            using var loader = new QuickFileMarkerLoader();
            var baseTime = DateTime.Now;

            // Two markers for the same file in the same group
            CreateMarker("marker-0000005.json", _testFile, baseTime, "Method1");
            CreateMarker("marker-0000006.json", _testFile, baseTime.AddSeconds(1), "Method2");

            // One marker for a different file
            string otherFile = Path.GetTempFileName();
            File.WriteAllText(otherFile, "class Other {}");
            CreateMarker("marker-0000007.json", otherFile, baseTime.AddSeconds(2), "Other");

            Thread.Sleep(1000);

            var groups = loader.MarkerGroups.ToList();
            Assert.Single(groups);

            var group = groups[0];
            Assert.Equal(2, group.Markers.Count());

            var markerForTestFile = group.Markers.FirstOrDefault(m => m.FilePath == _testFile);
            Assert.NotNull(markerForTestFile);
            Assert.Equal(2, markerForTestFile.Sections.Count());

            var markerForOtherFile = group.Markers.FirstOrDefault(m => m.FilePath == otherFile);
            Assert.NotNull(markerForOtherFile);
            Assert.Single(markerForOtherFile.Sections);

            try { File.Delete(otherFile); } catch { }
        }

        private void CreateMarker(string fileName, string filePath, DateTime time, string text = "public void Method()")
        {
            var markerPath = Path.Combine(_tempFolder, fileName);
            var markerData = new
            {
                Flag = "MARKER",
                FilePath = filePath,
                SellectedText = text,
                SellectedTextLine = text + " { }",
                CarretLine = "2",
                CharPositionInCarretLine = "4",
                SellectionStartLine = 2,
                SellectionEndLine = 2,
                TimeStamps = new
                {
                    Year = time.Year,
                    Month = time.Month,
                    Day = time.Day,
                    Hour = time.Hour,
                    Minute = time.Minute,
                    Second = time.Second
                }
            };
            File.WriteAllText(markerPath, JsonSerializer.Serialize(markerData));
        }

        class TestListener : IFileMarkerLoaderListener
        {
            public bool ReceivedEvent { get; private set; }

            public void MarkerAddedOrUpdated(IFileMarkerGroup group)
            {
                ReceivedEvent = true;
            }

            public void ValidityChanged(IFileMarkerGroup group)
            {
            }
        }
    }
}
