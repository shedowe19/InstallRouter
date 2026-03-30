# InstallRouter

**InstallRouter** ist ein spezialisiertes C# / .NET 8 (Windows Forms) Tool, das entwickelt wurde, um widerspenstige Software-Installer zu bändigen. 

Viele moderne Windows-Programme (wie z. B. *SteelSeries GG*, *Discord*, *Spotify* oder generell *Squirrel*-basierte Installer) zwingen den Nutzer dazu, das Programm auf dem Standard-Laufwerk in feste Ordner wie `C:\Program Files` oder `%LocalAppData%` zu installieren, ohne eine Möglichkeit zu bieten, ein anderes Laufwerk oder einen eigenen Zielpfad auszuwählen.

Hier kommt **InstallRouter** ins Spiel: Es leitet die Installation transparent und fehlerfrei auf dein gewünschtes Ziellaufwerk um, ohne die Software selbst zu manipulieren.

## 🚀 Wie es funktioniert (Symlink-Modus)

Die Magie von InstallRouter basiert auf automatisiertem Datei-Monitoring und NTFS-Junctions (Symlinks).

1. **Installer Auswählen**: Du wählst im Tool den heruntergeladenen Installer (.exe oder .msi) und legst ein beliebiges Zielverzeichnis fest (z.B. `D:\MeineProgramme\SteelSeriesGG`).
2. **Hintergrund-Überwachung**: InstallRouter startet im Hintergrund einen unsichtbaren `FileSystemWatcher`, der alle Standard-Zielverzeichnisse von Windows überwacht (`AppData`, `Program Files`, etc.), während er temporäre Entpackungsordner (wie `Temp`) clever ignoriert.
3. **Installation abwarten**: Der Installer wird ganz normal ausgeführt. Er denkt, er installiert sich auf `C:\`.
4. **Intelligente Erkennung**: Sobald der Installer fertig ist, ermittelt InstallRouter anhand von Prioritäts-Heuristiken (Ordnernamen-Matching, Vorhandensein der `.exe`, Anzahl der geschriebenen Dateien) exakt, in welchen der überwachten Ordner das Programm gerade installiert wurde.
5. **Admin-Skript & Verschiebung**: Das Tool generiert dynamisch ein Administrator-PowerShell-Skript, welches:
   - Alle neu installierten Background-Dienste, die der Installer gestartet haben könnte, hart beendet.
   - Alle Programm-Prozesse killt, um Dateisperren ("File in Use") aufzuheben.
   - Den gesamten Installationsordner an das ursprünglich von dir gewünschte Ziel (z.B. auf `D:\`) verschiebt.
6. **Symlink Erstellung**: Am alten, vom Installer gewählten Ort wird automatisch eine *Junction* (Symlink) erzeugt, die auf den neuen Ort zeigt. Zukünftige Updates oder Registry-Einträge der App gehen somit davon aus, dass alles ist, wie es war – landen physisch aber auf deinem Ziel-Laufwerk.

## 🛠 Features

- **Universelle Erkennung**: Identifiziert automatisch die Zielordner klassischer Installations-Frameworks (InnoSetup, NSIS, InstallShield, MSI) sowie moderner Squirrel-Installer.
- **Auto-Kill für störende Prozesse**: Befreit gesperrte Dateien (wie Treiber oder Tray-Icons), bevor der Ordner verschoben wird.
- **Abbrüche & Fehlerresistent**: Ordner, die bei der Installation extrem viel Müll generieren (wie `%Temp%`), werden absichtlich herausgefiltert, um falsche Ziel-Erkennungen zu verhindern.
- **Ein-Klick-Workflow**: Keine Frustration mit manuellem Suchen nach den versteckten AppData-Ordnern mehr.

## 💻 Systemanforderungen & Nutzung

- **OS**: Windows 10 / Windows 11
- **Framework**: .NET 8.0 Desktop Runtime

**Nutzung:**
Starte einfach die `InstallRouter.exe`. Es empfiehlt sich, das Programm als Standard-Nutzer auszuführen. Das Tool ruft nur für den finalen Verschiebe-Part kurz die UAC für Administrator-Rechte über eine PowerShell-Instanz auf.

## 🪲 Bekannte Bugs & Troubleshooting

- **Access to path denied**: Das Tool probiert blockierende Prozesse zu schließen. In seltenen Fällen, bspw. wenn Kernel-Treiber geladen wurden, muss der Ordner nach einem PC-Neustart manuell verschoben oder das Programm im Safe-Mode installiert werden.
- **Kein Ordner gefunden**: Falls das Programm den Ordner überhaupt nicht findet, wird es abbrechen und dich auffordern, den Ordner manuell zu verlinken (`mklink /J "Quelle" "Ziel"`).
- **Antivirus-Alarm (z.B. Avast / Defender)**: Da InstallRouter ein dynamisches PowerShell-Skript mit Administrator-Rechten generiert, um Prozesse brutal zu beenden und System-Ordner zu verschieben, schlagen viele Antiviren-Programme (wie Avast) Alarm. **Bitte deaktiviere deinen Virenschutz kurzzeitig** für den Moment der Installation oder füge InstallRouter als Ausnahme hinzu, ansonsten blockiert das Antivirus das Verschiebe-Skript!

---
*Gebaut, um den Nutzern die Kontrolle über ihre Festplatten zurückzugeben!*
