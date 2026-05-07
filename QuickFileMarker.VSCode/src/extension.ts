import * as vscode from 'vscode';
import { MarkerGenerator, MarkerRecord } from './markerGenerator';

// Interface removed as it's now in markerGenerator

export function activate(context: vscode.ExtensionContext) {

    let disposable = vscode.commands.registerCommand('quickFileMarker.createMarker', async () => {
        
        const editor = vscode.window.activeTextEditor;
        if (!editor) {
            vscode.window.showInformationMessage('No active editor to create a marker from.');
            return;
        }

        // 1. Read Configuration from the shared AppData folder!
        const config = MarkerGenerator.loadConfiguration();
        const menuItems = config.MenuItems || [];
        const lifetime = config.MarkerFileLifetimeInDays || 30;
        const maxCount = config.MaxMarkerFileCount || 1000;

        if (!menuItems || menuItems.length === 0) {
            vscode.window.showWarningMessage('No QuickFileMarker menu items configured. Please check your settings.');
            return;
        }

        // 2. Prepare Quick Pick Items
        const quickPickItems: vscode.QuickPickItem[] = menuItems.map(item => ({
            label: item.Label,
            description: `Flag: ${item.Flag}`,
            // We store the original item for later use
            menuItem: item
        })) as any;

        // 3. Show Quick Pick
        const selected = await vscode.window.showQuickPick(quickPickItems, {
            placeHolder: 'Select the type of marker to create'
        });

        if (!selected) {
            // User cancelled
            return;
        }

        const selectedMenuConfig = (selected as any).menuItem;

        // 4. Clean up temp directory in the background
        MarkerGenerator.cleanUpTempDirectory(lifetime, maxCount);

        // 5. Gather Editor Context
        const document = editor.document;
        const selection = editor.selection;
        
        const filePath = document.uri.fsPath;
        const startLine = selection.start.line + 1; // 1-indexed for C# compatibility
        const endLine = selection.end.line + 1; // 1-indexed for C# compatibility
        const selectedText = document.getText(selection);
        
        const activeLine = selection.active.line;
        const lineText = document.lineAt(activeLine).text;
        
        const carretLine = (activeLine + 1).toString();
        const carretChar = (selection.active.character + 1).toString(); // Assuming 1-indexed characters for COM EditPoint offset compatibility, need to test

        const now = new Date();

        const marker: MarkerRecord = {
            Flag: selectedMenuConfig.Flag,
            FilePath: filePath,
            SellectedText: selectedText,
            SellectedTextLine: lineText,
            CarretLine: carretLine,
            CharPositionInCarretLine: carretChar,
            SellectionStartLine: startLine,
            SellectionEndLine: endLine,
            TimeStamps: {
                Year: now.getFullYear(),
                Month: now.getMonth() + 1, // JavaScript months are 0-indexed
                Day: now.getDate(),
                Hour: now.getHours(),
                Minute: now.getMinutes(),
                Second: now.getSeconds()
            }
        };

        // 6. Save Marker
        try {
            MarkerGenerator.saveMarker(marker, selectedMenuConfig.OverwriteLastMarker === true);
            vscode.window.showInformationMessage(`QuickFileMarker: Created '${selectedMenuConfig.Label}' marker.`);
        } catch (err: any) {
            vscode.window.showErrorMessage(`Failed to create marker: ${err.message}`);
        }
    });

    context.subscriptions.push(disposable);
}

export function deactivate() {}
