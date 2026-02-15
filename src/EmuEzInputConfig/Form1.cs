namespace EmuEzInputConfig;

using System.Text.Json;
using EmuEzInputConfig.Detection;
using EmuEzInputConfig.Models;
using EmuEzInputConfig.ConfigWriters;
using SharpDX.DirectInput;

public partial class Form1 : Form
{
    private DirectInputManager? _diManager;
    private InputDetector? _detector;
    private List<DeviceInstance>? _devices;
    private Dictionary<Guid, DeviceBaseline>? _baselines;
    private Dictionary<string, int>? _noiseThresholds;
    private CancellationTokenSource? _cts;

    private readonly InputConfig _config = new();
    private int _currentStep = -1;
    private bool _detecting = false;

    // UI Controls — Input Detection tab
    private Label _lblTitle = null!;
    private Label _lblStep = null!;
    private Label _lblPrompt = null!;
    private Label _lblStatus = null!;
    private Label _lblDevices = null!;
    private DoubleBufferedPanel _axisPanel = null!;
    private ListBox _lstResults = null!;
    private Button _btnStart = null!;
    private Button _btnSkip = null!;
    private Button _btnWriteConfigs = null!;
    private TextBox _txtLaunchboxRoot = null!;
    private Button _btnBrowse = null!;
    private ProgressBar _progressBar = null!;
    private System.Windows.Forms.Timer _pollTimer = null!;

    // UI Controls — Hotkeys tab
    private readonly Dictionary<string, HotkeyButton> _hotkeyButtons = new();

    public Form1()
    {
        InitializeComponent();
        SetupUI();
    }

    private void SetupUI()
    {
        Text = "EmuEz Input Config";
        Size = new Size(900, 700);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Color.FromArgb(30, 30, 30);
        ForeColor = Color.White;
        Font = new Font("Segoe UI", 10f);

        var tabControl = new TabControl
        {
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 10f),
        };

        // Tab 1: Input Detection (existing wizard UI)
        var tabDetection = new TabPage("Input Detection")
        {
            BackColor = Color.FromArgb(30, 30, 30),
            ForeColor = Color.White,
        };
        SetupDetectionTab(tabDetection);
        tabControl.TabPages.Add(tabDetection);

        // Tab 2: Hotkeys
        var tabHotkeys = new TabPage("Hotkeys")
        {
            BackColor = Color.FromArgb(30, 30, 30),
            ForeColor = Color.White,
            AutoScroll = true,
        };
        SetupHotkeysTab(tabHotkeys);
        tabControl.TabPages.Add(tabHotkeys);

        Controls.Add(tabControl);

        // Poll timer for axis visualization
        _pollTimer = new System.Windows.Forms.Timer { Interval = 33 }; // ~30fps
        _pollTimer.Tick += PollTimer_Tick;
    }

    private void SetupDetectionTab(TabPage tab)
    {
        var mainLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 6,
            ColumnCount = 2,
            Padding = new Padding(12),
        };
        mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60));
        mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40));
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));  // Title
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));  // LaunchBox root
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 80));  // Step + prompt
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));  // Axis panel + results
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));  // Progress bar
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));  // Buttons

        // Row 0: Title
        _lblTitle = new Label
        {
            Text = "EmuEz Input Config",
            Font = new Font("Segoe UI", 16f, FontStyle.Bold),
            ForeColor = Color.FromArgb(0, 200, 100),
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
        };
        mainLayout.Controls.Add(_lblTitle, 0, 0);
        mainLayout.SetColumnSpan(_lblTitle, 2);

        // Row 1: LaunchBox root path
        var rootPanel = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false, AutoSize = true };
        rootPanel.Controls.Add(new Label { Text = "LaunchBox Root:", AutoSize = true, ForeColor = Color.Gray, Margin = new Padding(0, 4, 4, 0) });
        _txtLaunchboxRoot = new TextBox
        {
            Text = @"H:\RDriveRedux\Launchbox-Racing",
            Width = 400,
            BackColor = Color.FromArgb(50, 50, 50),
            ForeColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle,
        };
        rootPanel.Controls.Add(_txtLaunchboxRoot);
        _btnBrowse = new Button { Text = "...", Width = 30, Height = 24, FlatStyle = FlatStyle.Flat };
        _btnBrowse.Click += (s, e) =>
        {
            using var dlg = new FolderBrowserDialog { SelectedPath = _txtLaunchboxRoot.Text };
            if (dlg.ShowDialog() == DialogResult.OK) _txtLaunchboxRoot.Text = dlg.SelectedPath;
        };
        rootPanel.Controls.Add(_btnBrowse);
        mainLayout.Controls.Add(rootPanel, 0, 1);
        mainLayout.SetColumnSpan(rootPanel, 2);

        // Row 2: Step + prompt
        var promptPanel = new Panel { Dock = DockStyle.Fill };
        _lblStep = new Label
        {
            Text = "",
            Font = new Font("Segoe UI", 9f),
            ForeColor = Color.Gray,
            Location = new Point(0, 0),
            AutoSize = true,
        };
        promptPanel.Controls.Add(_lblStep);
        _lblPrompt = new Label
        {
            Text = "Click 'Start Detection' to begin the input wizard.\nMake sure your wheel is powered on and centered.",
            Font = new Font("Segoe UI", 12f, FontStyle.Bold),
            ForeColor = Color.FromArgb(255, 220, 100),
            Location = new Point(0, 20),
            AutoSize = true,
            MaximumSize = new Size(820, 0),
        };
        promptPanel.Controls.Add(_lblPrompt);
        _lblStatus = new Label
        {
            Text = "",
            ForeColor = Color.FromArgb(100, 200, 255),
            Location = new Point(0, 55),
            AutoSize = true,
        };
        promptPanel.Controls.Add(_lblStatus);
        mainLayout.Controls.Add(promptPanel, 0, 2);
        mainLayout.SetColumnSpan(promptPanel, 2);

        // Row 3 left: Axis visualization panel (double-buffered to prevent flicker)
        _axisPanel = new DoubleBufferedPanel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(40, 40, 40),
            BorderStyle = BorderStyle.FixedSingle,
        };
        _axisPanel.Paint += AxisPanel_Paint;
        mainLayout.Controls.Add(_axisPanel, 0, 3);

        // Row 3 right: Device list + results
        var rightPanel = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2 };
        rightPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 80));
        rightPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        _lblDevices = new Label
        {
            Text = "Devices: (not scanned)",
            Dock = DockStyle.Fill,
            ForeColor = Color.LightGray,
            Font = new Font("Consolas", 8.5f),
        };
        rightPanel.Controls.Add(_lblDevices, 0, 0);

        _lstResults = new ListBox
        {
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(40, 40, 40),
            ForeColor = Color.LightGreen,
            Font = new Font("Consolas", 9f),
            BorderStyle = BorderStyle.FixedSingle,
        };
        rightPanel.Controls.Add(_lstResults, 0, 1);
        mainLayout.Controls.Add(rightPanel, 1, 3);

        // Row 4: Progress bar
        _progressBar = new ProgressBar
        {
            Dock = DockStyle.Fill,
            Maximum = DetectionWizard.Steps.Length,
            Style = ProgressBarStyle.Continuous,
        };
        mainLayout.Controls.Add(_progressBar, 0, 4);
        mainLayout.SetColumnSpan(_progressBar, 2);

        // Row 5: Buttons
        var btnPanel = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight };
        _btnStart = CreateButton("Start Detection", Color.FromArgb(0, 150, 80));
        _btnStart.Click += BtnStart_Click;
        btnPanel.Controls.Add(_btnStart);

        _btnSkip = CreateButton("Skip Step", Color.FromArgb(120, 120, 40));
        _btnSkip.Enabled = false;
        _btnSkip.Click += BtnSkip_Click;
        btnPanel.Controls.Add(_btnSkip);

        _btnWriteConfigs = CreateButton("Write Configs", Color.FromArgb(40, 100, 180));
        _btnWriteConfigs.Enabled = false;
        _btnWriteConfigs.Click += BtnWriteConfigs_Click;
        btnPanel.Controls.Add(_btnWriteConfigs);
        mainLayout.Controls.Add(btnPanel, 0, 5);
        mainLayout.SetColumnSpan(btnPanel, 2);

        tab.Controls.Add(mainLayout);
    }

    private void SetupHotkeysTab(TabPage tab)
    {
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            AutoSize = true,
            Padding = new Padding(20),
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 180)); // Label
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 200)); // HotkeyButton
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 70));  // Reset

        int row = 0;

        // Header
        var header = new Label
        {
            Text = "Hotkey Configuration",
            Font = new Font("Segoe UI", 14f, FontStyle.Bold),
            ForeColor = Color.FromArgb(0, 200, 100),
            AutoSize = true,
            Margin = new Padding(0, 0, 0, 8),
        };
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.Controls.Add(header, 0, row);
        layout.SetColumnSpan(header, 3);
        row++;

        // Standard section header
        row = AddSectionHeader(layout, row, "Standard");

        // Standard hotkeys
        row = AddHotkeyRow(layout, row, "Exit Game", "exitKey",
            _config.Hotkeys.ExitKey, Keys.None);
        row = AddHotkeyRow(layout, row, "Fast Forward", "fastForwardKey",
            _config.Hotkeys.FastForwardKey, Keys.None);
        row = AddHotkeyRow(layout, row, "Rewind", "rewindKey",
            _config.Hotkeys.RewindKey, Keys.None);
        row = AddHotkeyRow(layout, row, "Reset", "resetKey",
            _config.Hotkeys.ResetKey, Keys.None);
        row = AddHotkeyRow(layout, row, "Save State", "saveStateKey",
            _config.Hotkeys.SaveStateKey, Keys.None);
        row = AddHotkeyRow(layout, row, "Load State", "loadStateKey",
            _config.Hotkeys.LoadStateKey, Keys.None);

        // Extended section header
        row = AddSectionHeader(layout, row, "Extended");

        // Extended hotkeys
        row = AddHotkeyRow(layout, row, "Prev Save Slot", "previousSaveSlotKey",
            _config.Hotkeys.PreviousSaveSlotKey, _config.Hotkeys.PreviousSaveSlotModifier);
        row = AddHotkeyRow(layout, row, "Next Save Slot", "nextSaveSlotKey",
            _config.Hotkeys.NextSaveSlotKey, Keys.None);
        row = AddHotkeyRow(layout, row, "Toggle Fullscreen", "toggleFullscreenKey",
            _config.Hotkeys.ToggleFullscreenKey, Keys.None);
        row = AddHotkeyRow(layout, row, "Screenshot", "screenshotKey",
            _config.Hotkeys.ScreenshotKey, Keys.None);
        row = AddHotkeyRow(layout, row, "Toggle Pause", "togglePauseKey",
            _config.Hotkeys.TogglePauseKey, Keys.None);

        // Info label
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        var infoLabel = new Label
        {
            Text = "Applies to: DuckStation, PCSX2, PPSSPP",
            ForeColor = Color.Gray,
            Font = new Font("Segoe UI", 9f, FontStyle.Italic),
            AutoSize = true,
            Margin = new Padding(0, 16, 0, 8),
        };
        layout.Controls.Add(infoLabel, 0, row);
        layout.SetColumnSpan(infoLabel, 3);
        row++;

        // Reset All button
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        var btnResetAll = new Button
        {
            Text = "Reset All to Defaults",
            Width = 180,
            Height = 32,
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(100, 50, 50),
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 9f, FontStyle.Bold),
            Margin = new Padding(0, 4, 0, 0),
        };
        btnResetAll.Click += (s, e) => ResetAllHotkeys();
        layout.Controls.Add(btnResetAll, 0, row);
        layout.SetColumnSpan(btnResetAll, 3);

        layout.RowCount = row + 1;
        tab.Controls.Add(layout);
    }

    private int AddSectionHeader(TableLayoutPanel layout, int row, string text)
    {
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        var label = new Label
        {
            Text = $"-- {text} --",
            ForeColor = Color.FromArgb(150, 150, 150),
            Font = new Font("Segoe UI", 9f, FontStyle.Bold),
            AutoSize = true,
            Margin = new Padding(0, 12, 0, 4),
        };
        layout.Controls.Add(label, 0, row);
        layout.SetColumnSpan(label, 3);
        return row + 1;
    }

    private int AddHotkeyRow(TableLayoutPanel layout, int row, string label, string id,
        Keys defaultKey, Keys defaultModifier)
    {
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        // Label
        var lbl = new Label
        {
            Text = label + ":",
            ForeColor = Color.LightGray,
            Font = new Font("Segoe UI", 10f),
            AutoSize = true,
            Margin = new Padding(0, 6, 0, 0),
            TextAlign = ContentAlignment.MiddleLeft,
        };
        layout.Controls.Add(lbl, 0, row);

        // Hotkey capture button
        var btn = new HotkeyButton
        {
            HotkeyKey = defaultKey,
            HotkeyModifier = defaultModifier,
            DefaultKey = defaultKey,
            DefaultModifier = defaultModifier,
            Width = 180,
            Height = 30,
            Margin = new Padding(0, 2, 0, 2),
        };
        _hotkeyButtons[id] = btn;
        layout.Controls.Add(btn, 1, row);

        // Reset button
        var btnReset = new Button
        {
            Text = "Reset",
            Width = 60,
            Height = 28,
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(60, 60, 60),
            ForeColor = Color.Gray,
            Font = new Font("Segoe UI", 8f),
            Margin = new Padding(4, 3, 0, 0),
        };
        btnReset.Click += (s, e) => btn.ResetToDefault();
        layout.Controls.Add(btnReset, 2, row);

        return row + 1;
    }

    private void ResetAllHotkeys()
    {
        foreach (var btn in _hotkeyButtons.Values)
            btn.ResetToDefault();
    }

    /// <summary>
    /// Read hotkey button states into the config model before writing.
    /// </summary>
    private void SyncHotkeysToConfig()
    {
        var h = _config.Hotkeys;

        if (_hotkeyButtons.TryGetValue("exitKey", out var b)) h.ExitKey = b.HotkeyKey;
        if (_hotkeyButtons.TryGetValue("fastForwardKey", out b)) h.FastForwardKey = b.HotkeyKey;
        if (_hotkeyButtons.TryGetValue("rewindKey", out b)) h.RewindKey = b.HotkeyKey;
        if (_hotkeyButtons.TryGetValue("resetKey", out b)) h.ResetKey = b.HotkeyKey;
        if (_hotkeyButtons.TryGetValue("saveStateKey", out b)) h.SaveStateKey = b.HotkeyKey;
        if (_hotkeyButtons.TryGetValue("loadStateKey", out b)) h.LoadStateKey = b.HotkeyKey;

        if (_hotkeyButtons.TryGetValue("previousSaveSlotKey", out b))
        {
            h.PreviousSaveSlotKey = b.HotkeyKey;
            h.PreviousSaveSlotModifier = b.HotkeyModifier;
        }
        if (_hotkeyButtons.TryGetValue("nextSaveSlotKey", out b)) h.NextSaveSlotKey = b.HotkeyKey;
        if (_hotkeyButtons.TryGetValue("toggleFullscreenKey", out b)) h.ToggleFullscreenKey = b.HotkeyKey;
        if (_hotkeyButtons.TryGetValue("screenshotKey", out b)) h.ScreenshotKey = b.HotkeyKey;
        if (_hotkeyButtons.TryGetValue("togglePauseKey", out b)) h.TogglePauseKey = b.HotkeyKey;
    }

    private static Button CreateButton(string text, Color backColor)
    {
        return new Button
        {
            Text = text,
            Width = 140,
            Height = 36,
            FlatStyle = FlatStyle.Flat,
            BackColor = backColor,
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            Margin = new Padding(4),
        };
    }

    // Stores live axis values for painting
    private Dictionary<Guid, Dictionary<string, int>> _liveAxes = new();

    private void AxisPanel_Paint(object? sender, PaintEventArgs e)
    {
        var g = e.Graphics;
        g.Clear(Color.FromArgb(40, 40, 40));

        if (_liveAxes.Count == 0 || _baselines == null)
        {
            g.DrawString("Axis visualization will appear during detection",
                Font, Brushes.Gray, 10, 10);
            return;
        }

        int y = 5;
        int barHeight = 14;
        int barMaxWidth = _axisPanel.Width - 140;

        foreach (var (guid, axes) in _liveAxes)
        {
            string devName = _baselines.TryGetValue(guid, out var bl)
                ? bl.DeviceName : guid.ToString()[..8];

            g.DrawString(devName, new Font("Segoe UI", 7.5f, FontStyle.Bold),
                Brushes.LightGray, 5, y);
            y += 16;

            foreach (var (axisName, value) in axes)
            {
                // Draw axis name
                g.DrawString(axisName, new Font("Consolas", 7.5f), Brushes.Gray, 5, y);

                // Draw bar background
                int barX = 90;
                g.FillRectangle(new SolidBrush(Color.FromArgb(60, 60, 60)),
                    barX, y, barMaxWidth, barHeight);

                // Draw bar fill (0-65535 range → 0-barMaxWidth)
                float pct = value / 65535f;
                int fillWidth = (int)(pct * barMaxWidth);
                var barColor = pct > 0.3f && pct < 0.7f
                    ? Color.FromArgb(60, 100, 60)  // near center = dim
                    : Color.FromArgb(0, 200, 100);  // moving = bright

                g.FillRectangle(new SolidBrush(barColor), barX, y, fillWidth, barHeight);

                // Draw value
                g.DrawString(value.ToString(), new Font("Consolas", 7f),
                    Brushes.White, barX + barMaxWidth + 4, y);

                y += barHeight + 2;
            }
            y += 6;
        }
    }

    private void PollTimer_Tick(object? sender, EventArgs e)
    {
        if (_diManager == null || _baselines == null) return;

        var live = new Dictionary<Guid, Dictionary<string, int>>();
        foreach (var guid in _baselines.Keys)
        {
            var state = _diManager.PollDevice(guid);
            if (state != null)
                live[guid] = DirectInputManager.GetAxisValues(state);
        }
        _liveAxes = live;
        _axisPanel.Invalidate();
    }

    private async void BtnStart_Click(object? sender, EventArgs e)
    {
        _btnStart.Enabled = false;
        _lstResults.Items.Clear();
        _config.Mappings.Clear();

        try
        {
            _diManager = new DirectInputManager();
            _detector = new InputDetector(_diManager);
            _devices = _diManager.EnumerateDevices();

            if (_devices.Count == 0)
            {
                _lblStatus.Text = "No game controllers found!";
                _lblStatus.ForeColor = Color.Red;
                _btnStart.Enabled = true;
                return;
            }

            // Show devices
            _lblDevices.Text = $"Devices ({_devices.Count}):\n" +
                string.Join("\n", _devices.Select(d => $"  {d.ProductName}"));

            // Calibrate
            _lblPrompt.Text = "Calibrating... Do NOT touch any controls.";
            _lblStatus.Text = "Measuring device noise...";
            await Task.Delay(200);

            _baselines = _detector.CaptureBaselines(_devices);
            _cts = new CancellationTokenSource();
            _noiseThresholds = await Task.Run(() => _detector.MeasureNoise(_baselines, _cts.Token));

            _lblStatus.Text = "Calibration complete.";
            _pollTimer.Start();

            // Run wizard steps
            _currentStep = 0;
            await RunNextStep();
        }
        catch (Exception ex)
        {
            _lblStatus.Text = $"Error: {ex.Message}";
            _lblStatus.ForeColor = Color.Red;
            _btnStart.Enabled = true;
        }
    }

    private async Task RunNextStep()
    {
        if (_currentStep >= DetectionWizard.Steps.Length)
        {
            // All steps done
            FinishWizard();
            return;
        }

        var step = DetectionWizard.Steps[_currentStep];
        string tag = step.Required ? "REQUIRED" : "optional";
        _lblStep.Text = $"Step {_currentStep + 1}/{DetectionWizard.Steps.Length}  [{tag}]";
        _lblPrompt.Text = step.Label;
        _lblStatus.Text = "Waiting for input...";
        _lblStatus.ForeColor = Color.FromArgb(100, 200, 255);
        _progressBar.Value = _currentStep;
        _btnSkip.Enabled = !step.Required;
        _detecting = true;

        _cts = new CancellationTokenSource();

        var result = await Task.Run(() =>
            _detector!.WaitForInput(_baselines!, _noiseThresholds!, 12000, _cts.Token,
                axes => { _liveAxes = axes; BeginInvoke(() => _axisPanel.Invalidate()); }));

        _detecting = false;

        if (result.Cancelled)
        {
            _lblStatus.Text = "Cancelled.";
            return;
        }

        if (result.TimedOut)
        {
            if (step.Required)
            {
                _lblStatus.Text = "Timed out — try again...";
                _lblStatus.ForeColor = Color.Orange;
                // Don't advance, user can try again or skip will be enabled
                _btnSkip.Enabled = true;
                await Task.Delay(500);
                await RunNextStep(); // retry same step
                return;
            }
            else
            {
                _config.Mappings[step.Name] = new InputMapping { Type = "skipped" };
                _lstResults.Items.Add($"{step.Name}: (skipped)");
                AdvanceStep();
                return;
            }
        }

        if (result.Success && result.Mapping != null)
        {
            _config.Mappings[step.Name] = result.Mapping;

            // Track wheel device from first detection
            if (_config.Wheel == null && result.Mapping.DeviceName != null)
            {
                _config.Wheel = new DeviceInfo
                {
                    ProductName = result.Mapping.DeviceName,
                    InstanceId = result.Mapping.DeviceInstanceId ?? "",
                    DInputIndex = 1, // DevReorder wheel position
                };
            }

            string detail = result.Mapping.Type switch
            {
                "axis" => $"Axis {result.Mapping.Axis} ({result.Mapping.Direction}) [{result.Mapping.DeviceName}]",
                "button" => $"Button {result.Mapping.ButtonIndex} [{result.Mapping.DeviceName}]",
                "hat" => $"Hat {result.Mapping.HatDirection} [{result.Mapping.DeviceName}]",
                _ => result.Mapping.Type,
            };
            _lstResults.Items.Add($"{step.Name}: {detail}");
            _lblStatus.Text = $"Detected: {detail}";
            _lblStatus.ForeColor = Color.LightGreen;

            // Re-capture baselines for next step
            // Axes (steering/gas/brake) need time to return to neutral; buttons/hats do not
            bool needsNeutral = result.Mapping.Type == "axis";
            if (needsNeutral)
            {
                _lblPrompt.Text = "Return controls to neutral...";
                await Task.Delay(3000);
            }
            else
            {
                await Task.Delay(500); // brief pause so user sees the result
            }
            _baselines = _detector!.CaptureBaselines(_devices!);
            _noiseThresholds = await Task.Run(() => _detector.MeasureNoise(_baselines, CancellationToken.None));

            AdvanceStep();
        }
    }

    private void AdvanceStep()
    {
        _currentStep++;
        _ = RunNextStep();
    }

    private void BtnSkip_Click(object? sender, EventArgs e)
    {
        if (_detecting)
        {
            _cts?.Cancel();
        }

        var step = DetectionWizard.Steps[_currentStep];
        _config.Mappings[step.Name] = new InputMapping { Type = "skipped" };
        _lstResults.Items.Add($"{step.Name}: (skipped)");
        _detecting = false;
        AdvanceStep();
    }

    private void FinishWizard()
    {
        _pollTimer.Stop();
        _progressBar.Value = _progressBar.Maximum;
        _lblStep.Text = "Complete!";
        _lblPrompt.Text = "All inputs detected. Review results and click 'Write Configs'.";
        _lblPrompt.ForeColor = Color.LightGreen;
        _lblStatus.Text = $"Wheel: {_config.Wheel?.ProductName ?? "unknown"}";
        _btnSkip.Enabled = false;
        _btnWriteConfigs.Enabled = true;
        _btnStart.Text = "Re-detect";
        _btnStart.Enabled = true;

        // Save config JSON
        _config.Timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        string configPath = Path.Combine(_txtLaunchboxRoot.Text, "scripts", "input-config.json");
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(configPath)!);
            var json = JsonSerializer.Serialize(_config, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(configPath, json);
            _lstResults.Items.Add($"--- Saved: {configPath}");
        }
        catch (Exception ex)
        {
            _lstResults.Items.Add($"--- Save error: {ex.Message}");
        }
    }

    private void BtnWriteConfigs_Click(object? sender, EventArgs e)
    {
        // Sync hotkey UI state into config before writing
        SyncHotkeysToConfig();

        string root = _txtLaunchboxRoot.Text;
        _lstResults.Items.Add("--- Writing emulator configs ---");

        IConfigWriter[] writers =
        [
            new Pcsx2ConfigWriter(),
            new SupermodelConfigWriter(),
            new DuckStationConfigWriter(),
            new PpssppConfigWriter(),
            new Rpcs3ConfigWriter(),
            new MameConfigWriter(),
        ];

        foreach (var writer in writers)
        {
            try
            {
                if (!writer.ConfigExists(root))
                {
                    _lstResults.Items.Add($"{writer.EmulatorName}: config not found (skipped)");
                    continue;
                }

                // Show preview
                var bindings = writer.GenerateBindings(_config);
                _lstResults.Items.Add($"{writer.EmulatorName}: writing {bindings.Count} bindings...");

                writer.WriteConfig(root, _config);
                _lstResults.Items.Add($"{writer.EmulatorName}: OK");
            }
            catch (Exception ex)
            {
                _lstResults.Items.Add($"{writer.EmulatorName}: ERROR - {ex.Message}");
            }
        }

        // Deploy DevReorder
        try
        {
            var results = DevReorderDeployer.Deploy(root, _config);
            foreach (var (target, success, msg) in results)
            {
                _lstResults.Items.Add($"DevReorder -> {target}: {(success ? "OK" : "FAIL")} - {msg}");
            }
        }
        catch (Exception ex)
        {
            _lstResults.Items.Add($"DevReorder: ERROR - {ex.Message}");
        }

        _lstResults.Items.Add("--- Done! ---");
        _btnWriteConfigs.Enabled = false;
        _lblPrompt.Text = "Configs written! Launch a game to test.";
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        _cts?.Cancel();
        _pollTimer?.Stop();
        _diManager?.Dispose();
        base.OnFormClosing(e);
    }
}

/// <summary>
/// Button that captures keyboard hotkey input on click.
/// Click to enter capture mode, then press a key combo to bind it.
/// </summary>
internal class HotkeyButton : Button
{
    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    public Keys HotkeyKey { get; set; }
    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    public Keys HotkeyModifier { get; set; }
    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    public Keys DefaultKey { get; set; }
    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    public Keys DefaultModifier { get; set; }

    private bool _capturing;

    public HotkeyButton()
    {
        FlatStyle = FlatStyle.Flat;
        BackColor = Color.FromArgb(50, 50, 50);
        ForeColor = Color.White;
        Font = new Font("Consolas", 10f, FontStyle.Bold);
        TextAlign = ContentAlignment.MiddleCenter;
        Cursor = Cursors.Hand;
    }

    protected override void OnCreateControl()
    {
        base.OnCreateControl();
        UpdateDisplay();
    }

    protected override void OnClick(EventArgs e)
    {
        if (_capturing)
        {
            // Cancel capture on second click
            _capturing = false;
            UpdateDisplay();
            return;
        }

        _capturing = true;
        Text = "Press a key...";
        BackColor = Color.FromArgb(80, 60, 20);
        ForeColor = Color.FromArgb(255, 220, 100);
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if (!_capturing) return base.ProcessCmdKey(ref msg, keyData);

        // Extract modifier and base key
        Keys modifiers = keyData & Keys.Modifiers;
        Keys baseKey = keyData & Keys.KeyCode;

        // Ignore standalone modifier presses — wait for the actual key
        if (baseKey is Keys.ShiftKey or Keys.ControlKey or Keys.Menu
            or Keys.LShiftKey or Keys.RShiftKey
            or Keys.LControlKey or Keys.RControlKey
            or Keys.LMenu or Keys.RMenu)
        {
            return true; // consume but don't finalize
        }

        // Normalize modifier to canonical form
        Keys mod = Keys.None;
        if ((modifiers & Keys.Shift) != 0) mod = Keys.ShiftKey;
        else if ((modifiers & Keys.Control) != 0) mod = Keys.ControlKey;
        else if ((modifiers & Keys.Alt) != 0) mod = Keys.Menu;

        HotkeyKey = baseKey;
        HotkeyModifier = mod;
        _capturing = false;
        UpdateDisplay();
        return true; // consume the key
    }

    public void ResetToDefault()
    {
        HotkeyKey = DefaultKey;
        HotkeyModifier = DefaultModifier;
        _capturing = false;
        UpdateDisplay();
    }

    private void UpdateDisplay()
    {
        Text = FormatHotkey(HotkeyKey, HotkeyModifier);
        BackColor = Color.FromArgb(50, 50, 50);
        ForeColor = Color.White;
    }

    public static string FormatHotkey(Keys key, Keys modifier)
    {
        string modText = modifier switch
        {
            Keys.ShiftKey => "Shift+",
            Keys.ControlKey => "Ctrl+",
            Keys.Menu => "Alt+",
            _ => "",
        };
        string keyText = KeyToDisplayName(key);
        return modText + keyText;
    }

    private static string KeyToDisplayName(Keys key)
    {
        return key switch
        {
            Keys.Escape => "Esc",
            Keys.Space => "Space",
            Keys.Back => "Backspace",
            Keys.Delete => "Delete",
            Keys.Return => "Enter",
            Keys.Tab => "Tab",
            Keys.Up => "Up",
            Keys.Down => "Down",
            Keys.Left => "Left",
            Keys.Right => "Right",
            >= Keys.F1 and <= Keys.F24 => key.ToString(),
            >= Keys.D0 and <= Keys.D9 => key.ToString()[1..], // "D0" → "0"
            _ => key.ToString(),
        };
    }
}

/// <summary>
/// Panel with double buffering enabled to prevent flicker during rapid repaints.
/// </summary>
internal class DoubleBufferedPanel : Panel
{
    public DoubleBufferedPanel()
    {
        SetStyle(
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.UserPaint,
            true);
    }
}
