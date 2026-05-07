using System;
using System.ComponentModel.Design;
using System.IO;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.TextManager.Interop;
using Newtonsoft.Json;
using QuickFileMarker.Configuration;
using QuickFileMarker.Markers;
using Task = System.Threading.Tasks.Task;

namespace QuickFileMarker
{
    internal sealed class QuickFileMarkerCommand
    {
        public const int CommandId = 0x0100;
        public static readonly Guid CommandSet = new Guid("85a06981-0a56-4b2f-98c5-6e01a886f4a8");

        private readonly AsyncPackage package;
        private ConfigurationRecord currentConfig;
        private OleMenuCommandService commandService;

        private QuickFileMarkerCommand(AsyncPackage package, OleMenuCommandService commandService)
        {
            this.package = package ?? throw new ArgumentNullException(nameof(package));
            this.commandService = commandService;
            
            this.currentConfig = QuickFileMarkerConfigurationManager.LoadConfiguration();

            for (int i = 0; i < currentConfig.MenuItems.Count; i++)
            {
                var menuConfig = currentConfig.MenuItems[i];
                var menuCommandID = new CommandID(CommandSet, CommandId + i);
                var menuItem = new OleMenuCommand((s, e) => this.Execute(menuConfig), menuCommandID);
                menuItem.Text = menuConfig.Label;
                
                menuItem.BeforeQueryStatus += (s, e) => {
                    ThreadHelper.ThrowIfNotOnUIThread();
                    var cmd = (OleMenuCommand)s;
                    var dte = ServiceProvider.GetService(typeof(DTE)) as DTE2;
                    cmd.Visible = dte?.ActiveDocument != null;
                };

                commandService.AddCommand(menuItem);
            }
        }

        public static QuickFileMarkerCommand Instance { get; private set; }
        private IServiceProvider ServiceProvider => this.package;

        public static async Task InitializeAsync(AsyncPackage package)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);
            var commandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
            Instance = new QuickFileMarkerCommand(package, commandService);
        }

        private void Execute(MenuRecord menuConfig)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var dte = ServiceProvider.GetService(typeof(DTE)) as DTE2;
            if (dte?.ActiveDocument == null) return;

            // Reload configuration to ensure we have latest cleanup parameters
            var liveConfig = QuickFileMarkerConfigurationManager.LoadConfiguration();

            // Clean up old markers
            QuickFileMarkerConfigurationManager.CleanUpTempDirectory(liveConfig);

            var selection = (TextSelection)dte.ActiveDocument.Selection;
            string filePath = dte.ActiveDocument.FullName;

            int startLine = selection.TopPoint.Line;
            int endLine = selection.BottomPoint.Line;
            string selectedText = selection.Text;
            
            // Get current line text
            selection.ActivePoint.CreateEditPoint().StartOfLine();
            var lineEditPoint = selection.ActivePoint.CreateEditPoint();
            lineEditPoint.StartOfLine();
            string lineText = lineEditPoint.GetText(lineEditPoint.LineLength);

            int carretLine = selection.ActivePoint.Line;
            int carretChar = selection.ActivePoint.LineCharOffset;

            var marker = new MarkerRecord
            {
                Flag = menuConfig.Flag,
                FilePath = filePath,
                SellectedText = selectedText,
                SellectedTextLine = lineText,
                CarretLine = carretLine.ToString(),
                CharPositionInCarretLine = carretChar.ToString(),
                SellectionStartLine = startLine,
                SellectionEndLine = endLine,
                TimeStamps = new TimeStempRecord
                {
                    Year = DateTime.Now.Year,
                    Month = DateTime.Now.Month,
                    Day = DateTime.Now.Day,
                    Hour = DateTime.Now.Hour,
                    Minute = DateTime.Now.Minute,
                    Second = DateTime.Now.Second
                }
            };

            string tempFolder = QuickFileMarkerConfigurationManager.GetTempMarkerFolder();

            string fileName;
            if (menuConfig.OverwriteLastMarker)
            {
                // Find last id without incrementing, or increment if none
                int lastId = 1;
                string incrementalIdPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "QuickFileMarker", "incremental-id.json");
                if (File.Exists(incrementalIdPath))
                {
                    try
                    {
                        var json = File.ReadAllText(incrementalIdPath);
                        var record = JsonConvert.DeserializeObject<IncrementalIdentifierRecord>(json);
                        if (record != null && record.LastIdentifier > 0)
                        {
                            lastId = record.LastIdentifier;
                        }
                        else
                        {
                            lastId = QuickFileMarkerConfigurationManager.GetNextIdentifier();
                        }
                    }
                    catch
                    {
                        lastId = QuickFileMarkerConfigurationManager.GetNextIdentifier();
                    }
                }
                else
                {
                    lastId = QuickFileMarkerConfigurationManager.GetNextIdentifier();
                }
                fileName = $"marker-{lastId:D7}.json";
            }
            else
            {
                int nextId = QuickFileMarkerConfigurationManager.GetNextIdentifier();
                fileName = $"marker-{nextId:D7}.json";
            }

            string fullMarkerPath = Path.Combine(tempFolder, fileName);
            File.WriteAllText(fullMarkerPath, JsonConvert.SerializeObject(marker, Formatting.Indented));
        }
    }
}
