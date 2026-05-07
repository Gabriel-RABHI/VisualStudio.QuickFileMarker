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
