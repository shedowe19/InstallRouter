using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace InstallRouter
{
    public class MainForm : Form
    {
        // ── Controls ──────────────────────────────────────────────────────────
        private Label       lblTitle       = null!;
        private Label       lblInstaller   = null!;
        private TextBox     txtInstaller   = null!;
        private Button      btnBrowse      = null!;
        private Label       lblTarget      = null!;
        private TextBox     txtTarget      = null!;
        private Button      btnTargetBrowse= null!;
        private Label       lblArgs        = null!;
        private TextBox     txtArgs        = null!;
        private CheckBox    chkSilent      = null!;
        private Button      btnStart       = null!;
        private Button      btnDone        = null!;   // "Installer fertig"-Knopf
        private volatile bool _manualDone  = false;   // Flag: User hat manuell signalisiert
        private RichTextBox rtbLog         = null!;
        private StatusStrip statusStrip    = null!;
        private ToolStripStatusLabel lblStatus = null!;

        public MainForm()
        {
            InitializeUI();
        }

        // ── UI aufbauen ───────────────────────────────────────────────────────
        private void InitializeUI()
        {
            this.Text            = "InstallRouter  –  Installer-Pfad umleiten";
            this.Size            = new Size(740, 620);
            this.MinimumSize     = new Size(680, 520);
            this.StartPosition   = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.Sizable;
            this.BackColor       = Color.FromArgb(15, 23, 42);   // slate-900 tiefer Dunkelmodus
            this.ForeColor       = Color.FromArgb(241, 245, 249); // slate-50
            this.Font            = new Font("Segoe UI", 9.5f);

            // ── Titel-Banner ──────────────────────────────────────────────────
            var pnlHeader = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = 70,
                BackColor = Color.FromArgb(2, 6, 23) // slate-950
            };
            lblTitle = new Label
            {
                Text      = "⚡ InstallRouter",
                ForeColor = Color.FromArgb(56, 189, 248), // sky-400 (sattes Neon-Blau)
                Font      = new Font("Segoe UI Semibold", 20f),
                AutoSize  = true,
                Location  = new Point(20, 16)
            };
            var lblSub = new Label
            {
                Text      = "Bändigt widerspenstige Windows-Installer.",
                ForeColor = Color.FromArgb(100, 116, 139), // slate-500
                Font      = new Font("Segoe UI", 9.5f),
                AutoSize  = true,
                Location  = new Point(230, 26)
            };
            pnlHeader.Controls.Add(lblTitle);
            pnlHeader.Controls.Add(lblSub);
            // pnlHeader wird ZULETZT hinzugefügt (nach pnl + statusStrip), damit Docking korrekt funktioniert

            // ── Haupt-Panel ───────────────────────────────────────────────────
            var pnl = new TableLayoutPanel
            {
                Dock        = DockStyle.Fill,
                Padding     = new Padding(24, 20, 24, 16),
                ColumnCount = 3,
                RowCount    = 6,
                AutoSize    = false
            };
            pnl.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 160));
            pnl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            pnl.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110));
            for (int i = 0; i < 6; i++)
                pnl.RowStyles.Add(new RowStyle(SizeType.Absolute, i == 5 ? 120 : 42));

            // Zeile 0: Installer-Datei
            lblInstaller = MakeLabel("Installer (.exe/.msi):");
            txtInstaller = MakeTextBox("Datei auswählen oder einfach per Drag & Drop hierher ziehen…");
            txtInstaller.TextChanged += (s, e) => AutoDetectType();
            btnBrowse    = MakeButton("Durchsuchen");
            btnBrowse.Click += BtnBrowse_Click;
            pnl.Controls.Add(lblInstaller,  0, 0);
            pnl.Controls.Add(txtInstaller,  1, 0);
            pnl.Controls.Add(btnBrowse,     2, 0);

            // Zeile 1: Zielpfad
            lblTarget = MakeLabel("Wunsch-Zielpfad:");
            txtTarget = MakeTextBox(@"z.B.  D:\Programme\MeineApp");
            btnTargetBrowse = MakeButton("Auswählen");
            btnTargetBrowse.Click += BtnTargetBrowse_Click;
            pnl.Controls.Add(lblTarget,       0, 1);
            pnl.Controls.Add(txtTarget,       1, 1);
            pnl.Controls.Add(btnTargetBrowse, 2, 1);

            // Zeile 2: Zusätzliche Argumente
            lblArgs = MakeLabel("Zusatz-Argumente:");
            txtArgs = MakeTextBox("optional – werden unsichtbar an den Installer drangehängt");
            pnl.Controls.Add(lblArgs,  0, 2);
            pnl.Controls.Add(txtArgs,  1, 2);
            pnl.Controls.Add(new Label(), 2, 2);

            // Zeile 3: Checkboxen + Start-Button
            var chkPanel = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, AutoSize = false };
            chkSilent  = new CheckBox { Text = "Möglichst lautlos installieren (/S, /silent, /q)", AutoSize = true, Checked = false, Margin = new Padding(0, 8, 0, 0) };
            chkPanel.Controls.Add(chkSilent);
            
            btnStart = new Button
            {
                Text      = "▶  INSTALLATION STARTEN",
                Dock      = DockStyle.Fill,
                BackColor = Color.FromArgb(79, 70, 229), // indigo-600
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font      = new Font("Segoe UI", 10.5f, FontStyle.Bold),
                Cursor    = Cursors.Hand,
                Margin    = new Padding(0, 4, 0, 8)
            };
            btnStart.FlatAppearance.BorderSize = 0;
            btnStart.MouseEnter += (s, e) => { if (btnStart.Enabled) btnStart.BackColor = Color.FromArgb(99, 102, 241); }; // indigo-500
            btnStart.MouseLeave += (s, e) => { if (btnStart.Enabled) btnStart.BackColor = Color.FromArgb(79, 70, 229); };
            btnStart.Click += BtnStart_Click;
            
            pnl.Controls.Add(chkPanel, 0, 3);
            pnl.SetColumnSpan(chkPanel, 2);
            pnl.Controls.Add(btnStart, 2, 3);

            // Zeile 4: Log-Header + "Installer fertig"-Knopf
            var lblLogHeader = MakeLabel("Aktivitäts-Protokoll:");
            lblLogHeader.Font = new Font("Segoe UI Semibold", 9.5f);
            
            btnDone = new Button
            {
                Text      = "✔  INSTALLER FERTIG",
                Dock      = DockStyle.Fill,
                BackColor = Color.FromArgb(16, 185, 129), // emerald-500
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font      = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                Cursor    = Cursors.Hand,
                Visible   = false,
                Margin    = new Padding(0, 4, 0, 8)
            };
            btnDone.FlatAppearance.BorderSize = 0;
            btnDone.MouseEnter += (s, e) => { if (btnDone.Enabled) btnDone.BackColor = Color.FromArgb(52, 211, 153); }; // emerald-400
            btnDone.MouseLeave += (s, e) => { if (btnDone.Enabled) btnDone.BackColor = Color.FromArgb(16, 185, 129); };
            btnDone.Click += (s, e) =>
            {
                _manualDone = true;
                btnDone.Enabled = false;
                btnDone.Text    = "✔  Signal gesendet…";
                Log("   ► Manuell signalisiert: Installer fertig.", Color.FromArgb(52, 211, 153));
            };
            
            pnl.Controls.Add(lblLogHeader, 0, 4);
            pnl.Controls.Add(btnDone,      1, 4);
            pnl.Controls.Add(new Label(),  2, 4);

            // Zeile 5: Log-Box
            rtbLog = new RichTextBox
            {
                Dock        = DockStyle.Fill,
                ReadOnly    = true,
                BackColor   = Color.FromArgb(2, 6, 23), // slate-950
                ForeColor   = Color.FromArgb(148, 163, 184), // slate-400
                Font        = new Font("Consolas", 9.5f),
                BorderStyle = BorderStyle.None,
                Margin      = new Padding(0, 4, 0, 0)
            };
            pnl.Controls.Add(rtbLog, 0, 5);
            pnl.SetColumnSpan(rtbLog, 3);

            // ── Status-Leiste ─────────────────────────────────────────────────
            statusStrip = new StatusStrip() { BackColor = Color.FromArgb(15, 23, 42), ForeColor = Color.FromArgb(148, 163, 184) };
            statusStrip.SizingGrip = false;
            statusStrip.Renderer = new ToolStripProfessionalRenderer(new CustomColorTable());
            lblStatus   = new ToolStripStatusLabel("Bereit.") { Spring = true, TextAlign = ContentAlignment.MiddleLeft };
            var lblBrand = new ToolStripStatusLabel("Powered by Shedowe")
            {
                ForeColor = Color.FromArgb(56, 189, 248), // sky-400
                Font      = new Font("Segoe UI Semibold", 9f),
                Alignment = ToolStripItemAlignment.Right
            };
            statusStrip.Items.Add(lblStatus);
            statusStrip.Items.Add(lblBrand);

            // Reihenfolge wichtig für WinForms-Docking: Fill → Bottom → Top
            this.Controls.Add(pnl);
            this.Controls.Add(statusStrip);
            this.Controls.Add(pnlHeader);

            // Drag & Drop für Installer-Textbox
            txtInstaller.AllowDrop = true;
            txtInstaller.DragEnter += (s, e) =>
            {
                if (e.Data!.GetDataPresent(DataFormats.FileDrop)) e.Effect = DragDropEffects.Copy;
            };
            txtInstaller.DragDrop += (s, e) =>
            {
                var files = (string[])e.Data!.GetData(DataFormats.FileDrop)!;
                if (files.Length > 0) { txtInstaller.Text = files[0]; AutoDetectType(); }
            };
        }

        // ── Hilfsmethoden ─────────────────────────────────────────────────────
        private static Label MakeLabel(string text) =>
            new Label { Text = text, TextAlign = ContentAlignment.MiddleRight, Dock = DockStyle.Fill, Margin = new Padding(0, 8, 16, 8) };

        private static TextBox MakeTextBox(string placeholder) =>
            new TextBox 
            { 
                Dock = DockStyle.Fill, 
                PlaceholderText = placeholder,
                BackColor = Color.FromArgb(30, 41, 59), // slate-800
                ForeColor = Color.FromArgb(241, 245, 249), // slate-50
                BorderStyle = BorderStyle.FixedSingle,
                Margin = new Padding(0, 8, 16, 8)
            };

        private static Button MakeButton(string text)
        {
            var btn = new Button 
            { 
                Text = text, 
                Dock = DockStyle.Fill, 
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(51, 65, 85), // slate-700
                ForeColor = Color.White,
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 6, 0, 8)
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.MouseEnter += (s, e) => { if (btn.Enabled) btn.BackColor = Color.FromArgb(71, 85, 105); }; // slate-600
            btn.MouseLeave += (s, e) => { if (btn.Enabled) btn.BackColor = Color.FromArgb(51, 65, 85); };
            return btn;
        }

        public class CustomColorTable : ProfessionalColorTable
        {
            public override Color ToolStripBorder => Color.Transparent;
            public override Color StatusStripBorder => Color.Transparent;
        }

        private void Log(string msg, Color? color = null)
        {
            if (rtbLog.InvokeRequired) { rtbLog.Invoke(() => Log(msg, color)); return; }
            rtbLog.SelectionStart  = rtbLog.TextLength;
            rtbLog.SelectionLength = 0;
            
            // Map standard colors to more modern equivalents if requested
            Color finalColor = color ?? Color.FromArgb(186, 230, 253); // sky-200 (Default)
            if (color == Color.LightGreen) finalColor = Color.FromArgb(52, 211, 153); // emerald-400
            else if (color == Color.Cyan) finalColor = Color.FromArgb(56, 189, 248); // sky-400
            else if (color == Color.Orange) finalColor = Color.FromArgb(251, 146, 60); // orange-400
            else if (color == Color.Red) finalColor = Color.FromArgb(248, 113, 113); // red-400
            else if (color == Color.Gray) finalColor = Color.FromArgb(148, 163, 184); // slate-400
            else if (color == Color.Yellow) finalColor = Color.FromArgb(250, 204, 21); // yellow-400
            else if (color == Color.White) finalColor = Color.FromArgb(241, 245, 249); // slate-50
            
            rtbLog.SelectionColor  = finalColor;
            rtbLog.AppendText($"[{DateTime.Now:HH:mm:ss}]  {msg}{Environment.NewLine}");
            rtbLog.ScrollToCaret();
        }

        private void SetStatus(string msg) =>
            lblStatus.Text = msg;

        // ── Typ auto-erkennen ─────────────────────────────────────────────────
        private void AutoDetectType()
        {
            var path = txtInstaller.Text;
            if (!File.Exists(path)) return;

            // Squirrel erkennen
            if (IsSquirrelInstaller(path))
            {
                Log("⚠  Squirrel-Installer erkannt (Discord, Slack, etc.)", Color.Orange);
                Log("   Squirrel ignoriert versteckte Argumente – der Ordner wird NACH Installation verschoben.", Color.Yellow);
            }
        }

        // ── Button: Installer-Datei wählen ────────────────────────────────────
        private void BtnBrowse_Click(object? sender, EventArgs e)
        {
            using var dlg = new OpenFileDialog
            {
                Title  = "Installer wählen",
                Filter = "Installer-Dateien|*.exe;*.msi|Alle Dateien|*.*"
            };
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                txtInstaller.Text = dlg.FileName;
                AutoDetectType();
            }
        }

        // ── Button: Zielpfad wählen ───────────────────────────────────────────
        private void BtnTargetBrowse_Click(object? sender, EventArgs e)
        {
            using var dlg = new FolderBrowserDialog
            {
                Description            = "Zielordner für die Installation wählen",
                UseDescriptionForTitle = true,
                ShowNewFolderButton    = true
            };
            if (dlg.ShowDialog() == DialogResult.OK)
                txtTarget.Text = dlg.SelectedPath;
        }

        // ── Squirrel erkennen ─────────────────────────────────────────────────
        private static bool IsSquirrelInstaller(string path)
        {
            try
            {
                // 1. FileVersionInfo prüfen
                var info = FileVersionInfo.GetVersionInfo(path);
                string all = ((info.FileDescription ?? "") + (info.ProductName ?? "") + (info.CompanyName ?? "")).ToLower();
                if (all.Contains("squirrel")) return true;

                // 2. Binär-Scan der gesamten Datei nach Squirrel-Signaturen
                //    Squirrel-Installer betten immer Update.exe, squirrel.windows.* und RELEASES ein
                long fileSize  = new FileInfo(path).Length;
                int  chunkSize = 64 * 1024; // 64 KB Chunks lesen
                byte[] chunk   = new byte[chunkSize];
                string carry   = ""; // Überlapp zwischen Chunks (für Strings an Chunk-Grenzen)

                using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
                while (fs.Position < fs.Length)
                {
                    int read = fs.Read(chunk, 0, chunkSize);

                    // ASCII-Scan
                    string ascii = carry + System.Text.Encoding.ASCII.GetString(chunk, 0, read);
                    // UTF-16-Scan (.NET-Binaries speichern Strings als Unicode)
                    string utf16 = System.Text.Encoding.Unicode.GetString(chunk, 0, read);

                    foreach (string text in new[] { ascii, utf16 })
                    {
                        if (text.Contains("Squirrel.Windows")    ||
                            text.Contains("squirrel.windows")    ||
                            text.Contains("SquirrelTemp")         ||
                            text.Contains("Squirrel.Update")      ||
                            text.Contains("squirrel-install")     ||
                            text.Contains("squirrel-firstrun")    ||
                            text.Contains("clowd.squirrel")       ||
                            text.Contains("NuGet.Squirrel")       ||
                            (text.Contains("RELEASES") && text.Contains("Update.exe")))
                            return true;
                    }

                    carry = ascii.Length > 128 ? ascii[^128..] : ascii;
                }
            }
            catch { }
            return false;
        }

        // ── Hauptlogik: Installer starten ─────────────────────────────────────
        private async void BtnStart_Click(object? sender, EventArgs e)
        {
            // ── Eingaben validieren ───────────────────────────────────────────
            if (string.IsNullOrWhiteSpace(txtInstaller.Text) || !File.Exists(txtInstaller.Text))
            {
                MessageBox.Show("Bitte eine gültige Installer-Datei auswählen.", "Fehlende Eingabe",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (string.IsNullOrWhiteSpace(txtTarget.Text))
            {
                MessageBox.Show("Bitte einen Zielpfad angeben.", "Fehlender Zielpfad",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            btnStart.Enabled = false;
            rtbLog.Clear();
            var installerPath = txtInstaller.Text.Trim();
            var targetPath    = txtTarget.Text.Trim();
            var ext           = Path.GetExtension(installerPath).ToLower();
            var silent        = chkSilent.Checked;
            var extraArgs     = txtArgs.Text.Trim();
            int selectedType  = DetectExeType(installerPath);

            Log($"Installer : {installerPath}");
            Log($"Zielpfad  : {targetPath}");

            // Zielpfad erstellen, falls nicht vorhanden
            try
            {
                if (!Directory.Exists(targetPath))
                {
                    Directory.CreateDirectory(targetPath);
                    Log($"Ordner erstellt: {targetPath}");
                }
            }
            catch (Exception ex)
            {
                Log($"FEHLER beim Erstellen des Ordners: {ex.Message}", Color.Red);
                btnStart.Enabled = true;
                return;
            }

            // ── Squirrel-Sonderfall ───────────────────────────────────────────
            if (IsSquirrelInstaller(installerPath))
            {
                await HandleSquirrelInstall(installerPath, targetPath, silent, extraArgs);
                btnStart.Enabled = true;
                return;
            }

            // ── Universeller Symlink-Modus ─────────────────────────────────────
            await HandleSymlinkInstall(installerPath, targetPath, silent, extraArgs, selectedType, ext);
            btnStart.Enabled = true;
            btnStart.Enabled = true;
        }

        // ── Universal: Installer starten → EXE-Ort erkennen → Verschieben → Junction ──
        private async Task HandleSymlinkInstall(string installerPath, string targetPath,
            bool silent, string extraArgs, int selectedType, string ext)
        {
            Log("═══ SYMLINK-MODUS ═════════════════════════════════════════════", Color.Cyan);
            Log("   FileSystemWatcher überwacht alle Standard-Installationspfade…", Color.Gray);
            Log("   Sobald der Installer fertig ist → '✔ Installer fertig' klicken.", Color.Gray);
            SetStatus("Installer läuft (Symlink-Modus)…");

            _manualDone = false;
            btnDone.Invoke(() => { btnDone.Visible = true; btnDone.Enabled = true; btnDone.Text = "✔  Installer fertig"; });

            // Alle relevanten Installations-Basisordner überwachen
            var watchRoots = new[]
            {
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs"),
            }.Where(Directory.Exists).Distinct().ToArray();

            var writeCount2 = new System.Collections.Concurrent.ConcurrentDictionary<string, int>();

            var watchers = watchRoots.Select(root =>
            {
                var w = new FileSystemWatcher(root)
                {
                    IncludeSubdirectories = true,
                    NotifyFilter          = NotifyFilters.FileName | NotifyFilters.DirectoryName,
                    EnableRaisingEvents   = true
                };
                w.Created += (_, ev) =>
                {
                    string rel = Path.GetRelativePath(root, ev.FullPath);
                    string top = rel.Split(Path.DirectorySeparatorChar)[0];
                    if (top.Length > 0 && top != ".")
                    {
                        var tl = top.ToLowerInvariant();
                        if (tl == "temp" || tl == "tmp" || tl == "microsoft" || tl == "common files" || tl == "packages" || tl == "crashdumps" || tl == "google")
                            return;
                        writeCount2.AddOrUpdate(Path.Combine(root, top), 1, (_, c) => c + 1);
                    }
                };
                return w;
            }).ToList();

            // Installer starten (normal, mit Admin-Elevation)
            string arguments = BuildInstallerArgs(installerPath, targetPath, silent, extraArgs, selectedType, ext);
            Log($"Aufruf: {installerPath} {arguments}");

            await RunProcess(installerPath, arguments,
                Path.GetDirectoryName(installerPath) ?? "", elevated: true);

            // Auf "Installer fertig"-Klick warten (max 10 Minuten)
            var deadline = DateTime.Now.AddMinutes(10);
            while (!_manualDone && DateTime.Now < deadline)
                await Task.Delay(300);

            // Watcher stoppen
            foreach (var w in watchers) { w.EnableRaisingEvents = false; w.Dispose(); }
            btnDone.Invoke(() => { btnDone.Visible = false; _manualDone = false; });

            // App-Name ableiten (SteelSeriesGG107.0.0Setup → SteelSeriesGG)
            string baseName = System.Text.RegularExpressions.Regex.Replace(
                Path.GetFileNameWithoutExtension(installerPath), @"[\d\.\-_]*(Setup|Installer|Install)?$", "",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase).Trim();

            Log("Schritt 2: Suche EXE in überwachten Ordnern…", Color.Cyan);

            var watchedDirs = writeCount2
                .Where(kv => Directory.Exists(kv.Key))
                .OrderByDescending(kv => kv.Value)
                .Select(kv => kv.Key)
                .ToList();

            string? installSource = null;

            // Priorität 1: Ordnername ist sehr ähnlich und es gibt eine EXE (im Top-Verzeichnis)
            installSource = watchedDirs.FirstOrDefault(dir => {
                string nm = Path.GetFileName(dir).ToLowerInvariant();
                string bn = baseName.ToLowerInvariant();
                return (bn.Contains(nm) || nm.Contains(bn)) &&
                       SafeHasExe(dir, true);
            });

            // Priorität 2: Ordner enthält EXE mit sehr ähnlichem Namen
            installSource ??= watchedDirs.FirstOrDefault(dir => SafeHasMatchingExe(dir, baseName.ToLowerInvariant()));

            // Priorität 3: Notnagel - Der Ordner mit den meisten Datei-Änderungen, der eine EXE enthält
            installSource ??= watchedDirs.FirstOrDefault(dir => SafeHasExe(dir, false));

            if (installSource == null)
            {
                Log("⚠  Kein Installations-Ordner erkannt.", Color.Orange);
                Log("   Bitte manuell den Ordner nach Installation angeben und Junction erstellen.", Color.Yellow);
                SetStatus("Manuell fortfahren."); return;
            }

            var exeFound = SafeGetFirstExe(installSource);
            Log($"   Installations-Ordner: {installSource}", Color.LightGreen);
            if (exeFound != null) Log($"   Haupt-EXE: {Path.GetFileName(exeFound)}", Color.LightGreen);

                // Schritt 3 & 4: Verschieben & Symlink (Elevated!)
            await FinalizeInstallation(installSource, targetPath);
            SetStatus("Fertig.");
        }

        // ── Installer-Argumente zusammenbauen (für Symlink-Modus) ─────────────
        private static string BuildInstallerArgs(string path, string target, bool silent, string extra, int type, string ext)
        {
            if (ext == ".msi") return $"/i \"{path}\" {(silent ? "/qb" : "")} {extra}".Trim();
            return type switch
            {
                1 => $"{(silent ? "/S" : "")} {extra}".Trim(),
                2 => $"{(silent ? "/SILENT /SP-" : "")} {extra}".Trim(),
                3 => $"{(silent ? "/s /v\"/qn\"" : "")} {extra}".Trim(),
                _ => extra
            };
        }

        // ── Squirrel: Installieren → Verschieben → Symlink ────────────────────
        private async Task HandleSquirrelInstall(string installerPath, string targetPath, bool silent, string extraArgs)
        {
            Log("═══ SQUIRREL-MODUS ════════════════════════════════════════════", Color.Cyan);
            Log("Schritt 1: Normalen Squirrel-Installer ausführen…", Color.Cyan);
            Log("           (Squirrel installiert zuerst in %LocalAppData%)", Color.Yellow);
            Log("           Sobald der Installer fertig ist → '✔ Installer fertig' klicken.", Color.Gray);
            SetStatus("Squirrel-Installer läuft…");

            // "Installer fertig"-Knopf einblenden
            _manualDone = false;
            btnDone.Invoke(() => { btnDone.Visible = true; btnDone.Enabled = true; btnDone.Text = "✔  Installer fertig"; });

            string? squirrelDefaultPath = null;
            string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string roamingData  = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

            // ── FileSystemWatcher: beobachte wo der Installer Dateien hinschreibt ──
            // Zählt Schreibvorgänge pro Top-Level-Ordner in AppData (Roaming + Local)
            var writeCount = new System.Collections.Concurrent.ConcurrentDictionary<string, int>();

            FileSystemWatcher MakeWatcher(string root)
            {
                var w = new FileSystemWatcher(root)
                {
                    IncludeSubdirectories = true,
                    NotifyFilter          = NotifyFilters.FileName | NotifyFilters.DirectoryName | NotifyFilters.LastWrite,
                    EnableRaisingEvents   = true
                };
                void Track(object _, FileSystemEventArgs e)
                {
                    // Top-Level-Unterordner in AppData bestimmen (z.B. "Spotify" aus "AppData\Roaming\Spotify\...")
                    string rel = Path.GetRelativePath(root, e.FullPath);
                    string top = rel.Split(Path.DirectorySeparatorChar)[0];
                    if (top.Length > 0 && top != ".")
                    {
                        var tl = top.ToLowerInvariant();
                        if (tl == "temp" || tl == "tmp" || tl == "microsoft" || tl == "common files" || tl == "packages" || tl == "crashdumps" || tl == "google")
                            return;
                        writeCount.AddOrUpdate(Path.Combine(root, top), 1, (_, c) => c + 1);
                    }
                }
                w.Created += Track;
                w.Changed += Track;
                return w;
            }

            using var watcherRoaming = MakeWatcher(roamingData);
            using var watcherLocal   = MakeWatcher(localAppData);
            Log("   FileSystemWatcher aktiv – beobachte AppData\\Roaming + AppData\\Local…", Color.Gray);

            // Installer starten (ohne Pfad-Parameter – Squirrel ignoriert sie sowieso)
            string silentArg = silent ? "--silent" : "";
            string installArgs = $"{silentArg} {extraArgs}".Trim();

            // Vor dem Start: alle laufenden Prozess-PIDs merken (um nach der Installation neu gestartete zu erkennen)
            string appBaseName = Path.GetFileNameWithoutExtension(installerPath)
                .Replace("Setup", "").Replace("setup", "").Replace("Canary", "").Replace("PTB", "").Trim();

            var updatePidsBefore = await Task.Run(() =>
                new HashSet<int>(Process.GetProcessesByName("Update").Select(p => p.Id)));

            // Alle laufenden Prozesse vor dem Install merken (zum späteren Killen des Auto-Starts)
            var allPidsBefore = await Task.Run(() =>
                new HashSet<int>(Process.GetProcesses().Select(p => p.Id)));

            // Squirrel-Installer OHNE Admin starten – Squirrel installiert in %LocalAppData% (kein Admin nötig)
            // und blockt aktiv den Start als Administrator (z.B. Spotify, Discord)
            Log("   Starte ohne Admin-Rechte (Squirrel benötigt das)…", Color.Yellow);
            int exitCode = await RunProcess(installerPath, installArgs, Path.GetDirectoryName(installerPath) ?? "", elevated: false);

            Log("✔  Installer gestartet (ohne Admin-Rechte).", Color.LightGreen);

            // Warte bis der Installer-Prozess selbst erscheint und wieder verschwindet
            await Task.Run(async () =>
            {
                string installerName = Path.GetFileNameWithoutExtension(installerPath);
                await Task.Delay(1500);

                int installerPid = -1;
                var appear = DateTime.Now.AddSeconds(15);
                while (DateTime.Now < appear && !_manualDone)
                {
                    var found = Process.GetProcessesByName(installerName)
                        .Where(p => !allPidsBefore.Contains(p.Id)).FirstOrDefault();
                    if (found != null) { installerPid = found.Id; break; }
                    await Task.Delay(300);
                }

                if (installerPid > 0)
                {
                    Log($"   Installer-Prozess PID {installerPid} – warte auf Abschluss…", Color.Gray);
                    var pidGone = DateTime.Now.AddSeconds(300);
                    while (DateTime.Now < pidGone && !_manualDone)
                    {
                        await Task.Delay(500);
                        if (!Process.GetProcesses().Any(p => p.Id == installerPid)) break;
                    }
                    if (!_manualDone) Log("   Installer-Prozess beendet.", Color.Gray);
                }
            });

            if (_manualDone) goto SkipUpdateWait;

            Log("   Warte auf Squirrel Update.exe im Hintergrund…", Color.Yellow);

            // Warte bis zu 60 Sekunden auf die vom Installer gestartete Update.exe
            await Task.Run(async () =>
            {
                var deadline = DateTime.Now.AddSeconds(60);
                Process? updateProc = null;

                await Task.Delay(1500);

                while (DateTime.Now < deadline && !_manualDone)
                {
                    var current = Process.GetProcessesByName("Update")
                        .Where(p => !updatePidsBefore.Contains(p.Id)).ToArray();

                    if (current.Length > 0)
                    {
                        updateProc = current[0];
                        int watchPid = updateProc.Id;
                        Log($"   Update.exe gefunden (PID {watchPid}) – überwache PID…", Color.Cyan);

                        var pidDeadline = DateTime.Now.AddSeconds(300);
                        while (DateTime.Now < pidDeadline && !_manualDone)
                        {
                            await Task.Delay(500);
                            if (!Process.GetProcesses().Any(p => p.Id == watchPid)) break;
                        }

                        if (!_manualDone) Log($"   PID {watchPid} beendet.", Color.LightGreen);
                        break;
                    }

                    string squirrelTemp = Path.Combine(localAppData, "SquirrelTemp");
                    if (!Directory.Exists(squirrelTemp) && updateProc == null)
                    {
                        break;
                    }

                    await Task.Delay(500);
                }

                // Noch 1 Sekunde Puffer damit Dateisystem-Schreibvorgänge abgeschlossen sind
                await Task.Delay(1000);
            });

            SkipUpdateWait:
            Log("✔  Squirrel-Installation abgeschlossen.", Color.LightGreen);

            // "Installer fertig"-Knopf wieder ausblenden
            btnDone.Invoke(() => { btnDone.Visible = false; _manualDone = false; });

            // Auto-gestartete App-Prozesse beenden (Squirrel startet die App automatisch nach Installation)
            await Task.Run(() =>
            {
                var newProcs = Process.GetProcesses()
                    .Where(p => !allPidsBefore.Contains(p.Id))
                    .Where(p => { try { return p.ProcessName.ToLower().Contains(appBaseName.ToLower()); } catch { return false; } })
                    .ToArray();

                foreach (var p in newProcs)
                {
                    try
                    {
                        Log($"   Auto-Start beendet: {p.ProcessName} (PID {p.Id})", Color.Yellow);
                        p.Kill();
                    }
                    catch { }
                }
            });

            // Watcher stoppen
            watcherRoaming.EnableRaisingEvents = false;
            watcherLocal.EnableRaisingEvents   = false;

            // ── Schritt 2: Installer-Zielordner aus Watcher-Daten bestimmen ──────
            Log("Schritt 2: Suche EXE in den vom Installer beschriebenen Ordnern…", Color.Cyan);

            // App-Name aus Installer-Dateiname ableiten
            string appName2 = System.Text.RegularExpressions.Regex.Replace(
                Path.GetFileNameWithoutExtension(installerPath), @"[\d\.\-_]*(Setup|Installer|Install)?$", "",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase).Trim();

            // Alle Ordner die der Watcher gesehen hat, sortiert nach Schreibvorgängen
            var watchedDirs = writeCount
                .Where(kv => Directory.Exists(kv.Key))
                .OrderByDescending(kv => kv.Value)
                .Select(kv => kv.Key)
                .ToList();

            // Prio 1: Ordnername passt + EXE vorhanden
            squirrelDefaultPath = watchedDirs.FirstOrDefault(dir => {
                string nm = Path.GetFileName(dir).ToLowerInvariant();
                string bn = appName2.ToLowerInvariant();
                return (bn.Contains(nm) || nm.Contains(bn)) && SafeHasExe(dir, true);
            });

            // Prio 2: EXE mit passendem Namen in Unterordnern vorhanden
            squirrelDefaultPath ??= watchedDirs.FirstOrDefault(dir => SafeHasMatchingExe(dir, appName2.ToLowerInvariant()));

            // Prio 3: Ordner der irgendeine .exe enthält
            squirrelDefaultPath ??= watchedDirs.FirstOrDefault(dir => SafeHasExe(dir, false));

            if (squirrelDefaultPath != null)
            {
                // Zeige welche EXE gefunden wurde
                var exeFound = SafeGetFirstExe(squirrelDefaultPath);
                Log($"   Installations-Ordner erkannt: {squirrelDefaultPath}", Color.LightGreen);
                if (exeFound != null)
                    Log($"   Haupt-EXE gefunden: {Path.GetFileName(exeFound)}", Color.LightGreen);
            }
            else
            {
                Log("⚠  Keine EXE in den vom Installer beschriebenen Ordnern gefunden.", Color.Orange);
                Log($"   Bitte manuell in %AppData% oder %LocalAppData% nachsehen.", Color.Yellow);
                Log($"   Dann Junction erstellen: mklink /J \"<AppOrdner>\" \"{targetPath}\"", Color.Yellow);
                SetStatus("Manuell fortfahren erforderlich.");
                return;
            }

            // Schritt 3 & 4: Verschieben & Symlink (Elevated!)
            await FinalizeInstallation(squirrelDefaultPath, targetPath);
            SetStatus("Fertig.");
        }

        // ── Zentrales Finalisieren (Admin): Kill, Move, Symlink ─────────────
        private async Task<bool> FinalizeInstallation(string installSource, string targetPath)
        {
            Log($"Schritt 3 & 4: Beende Programme, verschiebe Ordner und erstelle Symlink...", Color.Cyan);
            Log($"   (PowerShell fordert jetzt Admin-Rechte an!)", Color.Yellow);

            string script = $@"
$src = '{installSource}'
$dst = '{targetPath}'

# 1. Dienste stoppen
Try {{
    Get-CimInstance Win32_Service | Where-Object {{ $_.PathName -match [regex]::Escape($src) }} | Stop-Service -Force
}} Catch {{ }}

# 2. Prozesse killen
Try {{
    Get-Process | Where-Object {{ $_.Path -ne $null -and $_.Path.StartsWith($src, [StringComparison]::OrdinalIgnoreCase) }} | Stop-Process -Force
}} Catch {{ }}

Start-Sleep -Seconds 2

# 3. Verschieben
Try {{
    if (Test-Path $dst) {{
        Copy-Item -Path ""$src\*"" -Destination $dst -Recurse -Force
        Remove-Item -Path $src -Recurse -Force
    }} else {{
        Move-Item -Path $src -Destination $dst -Force
    }}
}} Catch {{
    Write-Host 'Fehler beim Verschieben:' $_.Exception.Message
    exit 1
}}

# 4. Symlink
cmd.exe /c mklink /J ""$src"" ""$dst""
if ($LASTEXITCODE -ne 0) {{ exit 2 }}
";
            string b64 = Convert.ToBase64String(System.Text.Encoding.Unicode.GetBytes(script));
            int exit = await RunProcess("powershell.exe", $"-NoProfile -ExecutionPolicy Bypass -WindowStyle Hidden -EncodedCommand {b64}", "", elevated: true);

            if (exit == 0)
            {
                Log($"✔  Ordner erfolgreich verschoben und Symlink gesetzt.", Color.LightGreen);
                Log("═══ FERTIG ═══════════════════════════════════════════════════", Color.Cyan);
                Log($"   App liegt auf: {targetPath}", Color.LightGreen);
                Log($"   Software sieht weiterhin: {installSource}", Color.LightGreen);
                return true;
            }
            else
            {
                Log($"⚠  Verschieben oder Symlink fehlgeschlagen (Exit-Code: {exit}).", Color.Red);
                return false;
            }
        }

        // ── Prozess starten (mit oder ohne Admin-Elevation) ───────────────────
        private Task<int> RunProcess(string exe, string args, string workDir, bool elevated = true)
        {
            return Task.Run(() =>
            {
                try
                {
                    ProcessStartInfo psi;

                    if (!elevated)
                    {
                        // Squirrel braucht keinen Admin – normal starten (kein runas)
                        // Da das Manifest jetzt asInvoker ist, läuft InstallRouter selbst
                        // ohne Admin → Kind-Prozesse erben keinen Admin-Token
                        psi = new ProcessStartInfo
                        {
                            FileName         = exe,
                            Arguments        = args,
                            UseShellExecute  = true,
                            WorkingDirectory = workDir
                        };
                    }
                    else
                    {
                        psi = new ProcessStartInfo
                        {
                            FileName         = exe,
                            Arguments        = args,
                            UseShellExecute  = true,
                            Verb             = "runas",
                            WorkingDirectory = workDir
                        };
                    }

                    using var proc = Process.Start(psi);
                    if (proc == null) return -1;
                    Log($"   PID: {proc.Id}", Color.Gray);
                    proc.WaitForExit();
                    return proc.ExitCode;
                }
                catch (Exception ex)
                {
                    Log($"FEHLER: {ex.Message}", Color.Red);
                    return -1;
                }
            });
        }

        // ── EXE-Typ auto-erkennen ─────────────────────────────────────────────
        private static int DetectExeType(string path)
        {
            try
            {
                var info = FileVersionInfo.GetVersionInfo(path);
                string desc = ((info.FileDescription ?? "") + (info.ProductName ?? "") + (info.Comments ?? "")).ToLower();
                if (desc.Contains("nullsoft") || desc.Contains("nsis")) return 1;
                if (desc.Contains("inno"))                              return 2;
                if (desc.Contains("installshield"))                     return 3;
            }
            catch { }
            return 0;
        }

        // ── Sichere Dateisuche ohne Crash bei Rechteproblemen ─────────────────
        private static bool SafeHasExe(string dir, bool topOnly)
        {
            try { return Directory.EnumerateFiles(dir, "*.exe", topOnly ? SearchOption.TopDirectoryOnly : SearchOption.AllDirectories).Any(); }
            catch { try { return Directory.EnumerateFiles(dir, "*.exe", SearchOption.TopDirectoryOnly).Any(); } catch { return false; } }
        }

        private static bool SafeHasMatchingExe(string dir, string match)
        {
            try { return Directory.EnumerateFiles(dir, "*.exe", SearchOption.AllDirectories).Any(f => Path.GetFileNameWithoutExtension(f).ToLowerInvariant().Contains(match)); }
            catch { try { return Directory.EnumerateFiles(dir, "*.exe", SearchOption.TopDirectoryOnly).Any(f => Path.GetFileNameWithoutExtension(f).ToLowerInvariant().Contains(match)); } catch { return false; } }
        }

        private static string? SafeGetFirstExe(string dir)
        {
            try { return Directory.GetFiles(dir, "*.exe", SearchOption.TopDirectoryOnly).FirstOrDefault() ?? Directory.GetFiles(dir, "*.exe", SearchOption.AllDirectories).FirstOrDefault(); }
            catch { try { return Directory.GetFiles(dir, "*.exe", SearchOption.TopDirectoryOnly).FirstOrDefault(); } catch { return null; } }
        }
    }
}
