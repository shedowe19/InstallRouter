# InstallRouter

**InstallRouter** ist ein spezialisiertes C# / .NET 8 (Windows Forms) Tool, das entwickelt wurde, um widerspenstige Software-Installer zu bändigen. 

Viele moderne Windows-Programme (wie z. B. *VRChat Creator Companion*, *SteelSeries GG*, *Discord*, *Spotify* oder generell *Squirrel*-basierte Installer) zwingen den Nutzer dazu, das Programm auf dem Standard-Laufwerk in feste Ordner wie `C:\Program Files` oder `%LocalAppData%` zu installieren, ohne eine Möglichkeit zu bieten, ein anderes Laufwerk oder einen eigenen Zielpfad auszuwählen.

Hier kommt **InstallRouter** ins Spiel: Es leitet die Installation transparent und fehlerfrei auf dein gewünschtes Ziellaufwerk um, ohne die Software selbst zu manipulieren.

## 🚀 Wie es funktioniert (Symlink-Modus)

Die Magie von InstallRouter basiert auf automatisiertem Datei-Monitoring und symbolischen Verzeichnisverknüpfungen (Symbolic Links / Symlinks).

1. **Installer Auswählen**: Du wählst im Tool den heruntergeladenen Installer (.exe oder .msi) und legst ein beliebiges Zielverzeichnis fest (z.B. `U:\MeineProgramme\Software`). Auch Netzlaufwerke werden problemlos unterstützt!
2. **Hintergrund-Überwachung**: InstallRouter startet im Hintergrund einen unsichtbaren `FileSystemWatcher`, der alle Standard-Zielverzeichnisse von Windows überwacht (`AppData`, `Program Files`, etc.), während er temporäre Entpackungsordner (wie `Temp`) clever ignoriert.
3. **Installation abwarten**: Der Installer wird ganz normal ausgeführt. Er denkt, er installiert sich auf `C:\`.
4. **Intelligente Erkennung**: Sobald der Installer fertig ist, ermittelt InstallRouter anhand von Prioritäts-Heuristiken (Ordnernamen-Matching, Vorhandensein der `.exe`, Anzahl der geschriebenen Dateien) exakt, in welchen der überwachten Ordner das Programm gerade installiert wurde.
5. **Admin-Skript & Verschiebung**: Das Tool generiert dynamisch ein Administrator-PowerShell-Skript, welches:
   - Alle neu installierten Background-Dienste, die der Installer gestartet haben könnte, hart beendet.
   - Alle Programm-Prozesse killt, um Dateisperren ("File in Use") aufzuheben.
   - Den gesamten Installationsordner an das ursprünglich von dir gewünschte Ziel (selbst auf per UNC gemappte Netzlaufwerke) verschiebt.
6. **Symlink Erstellung**: Am alten, vom Installer gewählten Ort wird automatisch ein **Symbolischer Verzeichnislink (`mklink /D`)** erzeugt, der auf den neuen Ort zeigt. Zukünftige Updates oder Registry-Einträge der App gehen somit davon aus, dass alles ist, wie es war – landen physisch aber auf deinem Ziel-Laufwerk.

## 🛠 Features

- **Netzlaufwerk-Support (UNC)**: Löst via nativer Win32 API (`mpr.dll`) automatisch gemappte Laufwerksbuchstaben auf, sodass die UAC-geschützten Administrator-Skripte problemlos auf Netzlaufwerke zugreifen können.
- **Chromium Netzwerk-Patch (Sandbox-Fix)**: Für moderne "Web-Apps" auf Electron- oder CEF-Basis (Spotify, Discord, VS Code) bietet die UI eine automatische Patch-Funktion. Findet InstallRouter Startmenü- oder Desktop-Verknüpfungen (Shortcuts / `.lnk`) für diese neu verschobenen Dateien, injiziert das Tool automatisch das Start-Argument `--no-sandbox`. So starten diese Apps absturzfrei, auch wenn sie quer durchs Netzwerk geladen werden!
- **Symbolic Links (/D)**: Nutzt echte Symlinks anstelle von einfachen NTFS-Junctions, wodurch Umleitungen über verschiedene physikalische oder Netzwerk-Volumes hinweg reibungslos funktionieren.
- **Modernes Premium User Interface**: Ein wunderschönes Dark Mode UI, gestylt mit modernen TailwindCSS-Farbpaletten (Slate, Emerald, Indigo), fixen Proportionen und einem per Windows-API ("DwmSetWindowAttribute") nativ dunkel gefärbten Windows 11 Fenster-Header.
- **Auto-Kill für störende Prozesse**: Befreit gesperrte Dateien (wie Treiber oder Tray-Icons), bevor der Ordner verschoben wird.
- **Automatisierte CI/CD & Code Signing**: Jedes Release wird über GitHub Actions vollautomatisiert als handliche `Single-File Executable (.exe)` gebuildet und **sofort kryptografisch signiert** (Self-Signed inkl. Digicert Timestamp), um Windows SmartScreen glücklicher zu machen.

## 💻 Systemanforderungen & Nutzung

- **OS**: Windows 10 / Windows 11
- **Framework**: .NET 8.0 Desktop Runtime

**Nutzung:**
Lade dir die aktuellste `InstallRouter.exe` im [Releases-Reiter](https://github.com/shedowe19/InstallRouter/releases) herunter und starte sie. 

*Hinweis zum Zertifikat:* Im Release liegt eine `InstallRouter-Zertifikat.cer`-Datei bei. Da die App aus Kostengründen mit einem selbstsignierten Zertifikat kompiliert wurde, kannst du diese `.cer` in deinem PC unter **Vertrauenswürdige Stammzertifizierungsstellen** installieren. Tust du dies, wird die Applikation von Windows beim Starten absolut vertraut und die SmartScreen-Warnung beim ersten Ausführen entfällt!

## 🪲 Bekannte Bugs & Troubleshooting

- **Access to path denied**: Das Tool probiert blockierende Prozesse zu schließen. In seltenen Fällen, bspw. wenn Kernel-Treiber geladen wurden, muss der Ordner nach einem PC-Neustart manuell verschoben oder das Programm im Safe-Mode installiert werden.
- **Antivirus-Alarm (z.B. Avast / Defender)**: Da InstallRouter ein dynamisches PowerShell-Skript mit Administrator-Rechten generiert, um Prozesse brutal zu beenden und System-Ordner zu verschieben, schlagen viele Antiviren-Programme (wie Avast) Alarm. **Bitte deaktiviere deinen Virenschutz kurzzeitig** für den Moment der Installation oder füge InstallRouter als Ausnahme hinzu, ansonsten blockiert das Antivirus das Verschiebe-Skript!

---
*Gebaut, um den Nutzern die Kontrolle über ihre Festplatten zurückzugeben!*
