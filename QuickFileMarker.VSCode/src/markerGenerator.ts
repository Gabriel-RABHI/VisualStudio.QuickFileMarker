import * as fs from 'fs';
import * as path from 'path';
import * as os from 'os';

export interface TimeStampRecord {
    Year: number;
    Month: number;
    Day: number;
    Hour: number;
    Minute: number;
    Second: number;
}

export interface MarkerRecord {
    Flag: string;
    FilePath: string;
    SellectedText: string;
    SellectedTextLine: string;
    CarretLine: string;
    CharPositionInCarretLine: string;
    SellectionStartLine: number;
    SellectionEndLine: number;
    TimeStamps: TimeStampRecord;
}

export interface IncrementalIdentifierRecord {
    LastIdentifier: number;
}

export interface MenuRecord {
    Label: string;
    Flag: string;
    OverwriteLastMarker?: boolean;
    Shortcut?: any;
}

export interface ConfigurationRecord {
    MenuItems: MenuRecord[];
    MarkerFileLifetimeInDays: number;
    MaxMarkerFileCount: number;
}

export class MarkerGenerator {
    private static get AppDataFolder(): string {
        const appDataPath = process.env.APPDATA || (process.platform === 'darwin' ? process.env.HOME + '/Library/Application Support' : process.env.HOME + '/.config');
        const folder = path.join(appDataPath, 'QuickFileMarker');
        if (!fs.existsSync(folder)) {
            fs.mkdirSync(folder, { recursive: true });
        }
        return folder;
    }

    private static get TempMarkerFolder(): string {
        const folder = path.join(os.tmpdir(), 'FileMarkers');
        if (!fs.existsSync(folder)) {
            fs.mkdirSync(folder, { recursive: true });
        }
        return folder;
    }

    private static get IncrementalIdFilePath(): string {
        return path.join(this.AppDataFolder, 'incremental-id.json');
    }

    public static loadConfiguration(): ConfigurationRecord {
        const configPath = path.join(this.AppDataFolder, 'extention-config.json');
        if (fs.existsSync(configPath)) {
            try {
                return JSON.parse(fs.readFileSync(configPath, 'utf8'));
            } catch (e) {
                console.error("Failed to parse extention-config.json", e);
            }
        }
        // Default configuration if file is missing or invalid
        return {
            MenuItems: [
                { Label: "New Marker", Flag: "MARKER", OverwriteLastMarker: false },
                { Label: "Replace last Marker", Flag: "MARKER", OverwriteLastMarker: true },
                { Label: "Show", Flag: "SHOW", OverwriteLastMarker: true }
            ],
            MarkerFileLifetimeInDays: 30,
            MaxMarkerFileCount: 1000
        };
    }

    public static getNextIdentifier(): number {
        let record: IncrementalIdentifierRecord = { LastIdentifier: 0 };
        const idFilePath = this.IncrementalIdFilePath;
        
        if (fs.existsSync(idFilePath)) {
            try {
                const json = fs.readFileSync(idFilePath, 'utf8');
                record = JSON.parse(json);
            } catch (e) {
                // Ignore parsing errors
            }
        }

        record.LastIdentifier++;
        fs.writeFileSync(idFilePath, JSON.stringify(record, null, 2), 'utf8');

        return record.LastIdentifier;
    }

    public static getLastIdentifier(): number {
        let lastId = 1;
        const idFilePath = this.IncrementalIdFilePath;

        if (fs.existsSync(idFilePath)) {
            try {
                const json = fs.readFileSync(idFilePath, 'utf8');
                const record: IncrementalIdentifierRecord = JSON.parse(json);
                if (record && record.LastIdentifier > 0) {
                    lastId = record.LastIdentifier;
                } else {
                    lastId = this.getNextIdentifier();
                }
            } catch (e) {
                lastId = this.getNextIdentifier();
            }
        } else {
            lastId = this.getNextIdentifier();
        }

        return lastId;
    }

    public static saveMarker(marker: MarkerRecord, overwriteLastMarker: boolean): void {
        let fileName: string;
        if (overwriteLastMarker) {
            const lastId = this.getLastIdentifier();
            fileName = `marker-${String(lastId).padStart(7, '0')}.json`;
        } else {
            const nextId = this.getNextIdentifier();
            fileName = `marker-${String(nextId).padStart(7, '0')}.json`;
        }

        const fullMarkerPath = path.join(this.TempMarkerFolder, fileName);
        fs.writeFileSync(fullMarkerPath, JSON.stringify(marker, null, 2), 'utf8');
    }

    public static cleanUpTempDirectory(lifetimeInDays: number, maxCount: number): void {
        const tempFolder = this.TempMarkerFolder;
        if (!fs.existsSync(tempFolder)) return;

        try {
            const files = fs.readdirSync(tempFolder)
                .filter(f => f.endsWith('.json'))
                .map(f => {
                    const filePath = path.join(tempFolder, f);
                    return {
                        filePath,
                        stat: fs.statSync(filePath)
                    };
                })
                .sort((a, b) => a.stat.birthtimeMs - b.stat.birthtimeMs); // Oldest first

            const threshold = new Date();
            threshold.setDate(threshold.getDate() - lifetimeInDays);
            const thresholdMs = threshold.getTime();

            // 1. Remove files older than lifetime
            const remainingFiles = [];
            for (const file of files) {
                if (file.stat.birthtimeMs < thresholdMs) {
                    try { fs.unlinkSync(file.filePath); } catch (e) {}
                } else {
                    remainingFiles.push(file);
                }
            }

            // 2. If still more than max count, remove oldest
            if (remainingFiles.length > maxCount) {
                const toRemove = remainingFiles.length - maxCount;
                for (let i = 0; i < toRemove; i++) {
                    try { fs.unlinkSync(remainingFiles[i].filePath); } catch (e) {}
                }
            }
        } catch (e) {
            console.error("Error during cleanup:", e);
        }
    }
}
