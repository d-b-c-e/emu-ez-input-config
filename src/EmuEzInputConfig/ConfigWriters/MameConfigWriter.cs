namespace EmuEzInputConfig.ConfigWriters;

using System.Text;
using EmuEzInputConfig.Models;
using EmuEzInputConfig.Util;

/// <summary>
/// Writes MAME racing input config using the ctrlr file approach (XML).
/// Generates EmuEzRacing.cfg in ctrlr/, resets default.cfg, and updates mame.ini.
/// MAME uses JOYCODE_1_XAXIS format with 1-indexed buttons.
/// Target directory: Emulators\mame
/// </summary>
public class MameConfigWriter : IConfigWriter
{
    public string EmulatorName => "MAME";

    // SharpDX axis name → MAME axis token
    private static readonly Dictionary<string, string> AxisNameMap = new()
    {
        ["X"] = "XAXIS", ["Y"] = "YAXIS", ["Z"] = "ZAXIS",
        ["RotationX"] = "RXAXIS", ["RotationY"] = "RYAXIS", ["RotationZ"] = "RZAXIS",
        ["Slider0"] = "SLIDER1", ["Slider1"] = "SLIDER2",
    };

    private static string? GetAxisJoycode(InputMapping? m)
    {
        if (m?.Type != "axis" || m.Axis == null) return null;
        if (!AxisNameMap.TryGetValue(m.Axis, out string? name)) return null;
        return $"JOYCODE_1_{name}";
    }

    private static string? GetButtonJoycode(InputMapping? m)
    {
        // MAME buttons are 1-indexed
        if (m?.Type != "button" || !m.ButtonIndex.HasValue) return null;
        return $"JOYCODE_1_BUTTON{m.ButtonIndex.Value + 1}";
    }

    private static string? GetHatJoycode(InputMapping? m, string direction)
    {
        if (m?.Type != "hat") return null;
        return $"JOYCODE_1_HAT1{direction.ToUpperInvariant()}";
    }

    public bool ConfigExists(string launchboxRoot)
    {
        string ini = Path.Combine(launchboxRoot, @"Emulators\mame\mame.ini");
        return File.Exists(ini);
    }

    public Dictionary<string, string> GenerateBindings(InputConfig config)
    {
        var m = config.Mappings;

        string? steerAxis = GetAxisJoycode(m.GetValueOrDefault("steering"));
        string? gasAxis = GetAxisJoycode(m.GetValueOrDefault("gas"));
        string? brakeAxis = GetAxisJoycode(m.GetValueOrDefault("brake"));

        string? gearUpBtn = GetButtonJoycode(m.GetValueOrDefault("gearUp"));
        string? gearDownBtn = GetButtonJoycode(m.GetValueOrDefault("gearDown"));
        string? startBtn = GetButtonJoycode(m.GetValueOrDefault("start"));
        string? coinBtn = GetButtonJoycode(m.GetValueOrDefault("coin"));

        string? btnA = GetButtonJoycode(m.GetValueOrDefault("btnA"));
        string? btnB = GetButtonJoycode(m.GetValueOrDefault("btnB"));
        string? btnX = GetButtonJoycode(m.GetValueOrDefault("btnX"));
        string? btnY = GetButtonJoycode(m.GetValueOrDefault("btnY"));

        string GetDpadJoycode(string stepName, string dir)
        {
            var mapping = m.GetValueOrDefault(stepName);
            return GetHatJoycode(mapping, dir)
                ?? GetButtonJoycode(mapping)
                ?? "";
        }

        var bindings = new Dictionary<string, string>
        {
            // Steering — both paddle (wheel games) and dial (spinner games)
            ["P1_PADDLE"] = steerAxis ?? "",
            ["P1_DIAL"] = steerAxis ?? "",

            // Pedals — full axis for dedicated pedal inputs
            ["P1_PEDAL"] = gasAxis ?? "",
            ["P1_PEDAL2"] = brakeAxis ?? "",

            // Buttons — gear paddles first, then face buttons
            ["P1_BUTTON1"] = gearUpBtn ?? "",
            ["P1_BUTTON2"] = gearDownBtn ?? "",
            ["P1_BUTTON3"] = btnA ?? "",
            ["P1_BUTTON4"] = btnB ?? "",
            ["P1_BUTTON5"] = btnX ?? "",
            ["P1_BUTTON6"] = btnY ?? "",

            // D-pad
            ["P1_JOYSTICK_UP"] = GetDpadJoycode("dpadUp", "UP"),
            ["P1_JOYSTICK_DOWN"] = GetDpadJoycode("dpadDown", "DOWN"),
            ["P1_JOYSTICK_LEFT"] = GetDpadJoycode("dpadLeft", "LEFT"),
            ["P1_JOYSTICK_RIGHT"] = GetDpadJoycode("dpadRight", "RIGHT"),

            // Start and coin
            ["START1"] = startBtn ?? "",
            ["COIN1"] = coinBtn != null ? $"KEYCODE_5 OR {coinBtn}" : "KEYCODE_5",
        };

        return bindings;
    }

    public void WriteConfig(string launchboxRoot, InputConfig config)
    {
        string mameDir = Path.Combine(launchboxRoot, @"Emulators\mame");
        string mameIni = Path.Combine(mameDir, "mame.ini");
        if (!File.Exists(mameIni)) return;

        var bindings = GenerateBindings(config);
        string productName = config.Wheel?.ProductName ?? "Racing Wheel";

        // 1. Write ctrlr file
        string ctrlrDir = Path.Combine(mameDir, "ctrlr");
        Directory.CreateDirectory(ctrlrDir);
        string ctrlrPath = Path.Combine(ctrlrDir, "EmuEzRacing.cfg");
        if (File.Exists(ctrlrPath)) IniEditor.BackupFile(ctrlrPath);
        string ctrlrXml = BuildCtrlrXml(productName, bindings);
        File.WriteAllText(ctrlrPath, ctrlrXml);

        // 2. Reset default.cfg to clear stale XInput mappings
        string defaultCfg = Path.Combine(mameDir, "cfg", "default.cfg");
        if (File.Exists(defaultCfg))
        {
            IniEditor.BackupFile(defaultCfg);
            File.WriteAllText(defaultCfg, BuildEmptyDefaultCfg());
        }

        // 3. Update mame.ini settings
        IniEditor.BackupFile(mameIni);
        UpdateMameIni(mameIni, new Dictionary<string, string>
        {
            ["ctrlr"] = "EmuEzRacing",
            ["joystick"] = "1",
            ["joystick_deadzone"] = "0.05",
            ["joystick_saturation"] = "0.95",
            ["paddle_device"] = "joystick",
            ["pedal_device"] = "joystick",
        });
    }

    private static string BuildCtrlrXml(string productName, Dictionary<string, string> bindings)
    {
        var sb = new StringBuilder();
        sb.AppendLine("<?xml version=\"1.0\"?>");
        sb.AppendLine("<mameconfig version=\"10\">");
        sb.AppendLine("    <system name=\"default\">");
        sb.AppendLine("        <input>");

        // Pin the wheel to JOYCODE_1 by product name
        string escapedName = productName.Replace("&", "&amp;").Replace("\"", "&quot;");
        sb.AppendLine($"            <mapdevice device=\"{escapedName}\" controller=\"JOYCODE_1\" />");

        // Write port bindings
        foreach (var (portType, joycode) in bindings)
        {
            if (string.IsNullOrEmpty(joycode)) continue;
            sb.AppendLine($"            <port type=\"{portType}\">");
            sb.AppendLine($"                <newseq type=\"standard\">{joycode}</newseq>");
            sb.AppendLine("            </port>");
        }

        sb.AppendLine("        </input>");
        sb.AppendLine("    </system>");
        sb.AppendLine("</mameconfig>");
        return sb.ToString();
    }

    private static string BuildEmptyDefaultCfg()
    {
        var sb = new StringBuilder();
        sb.AppendLine("<?xml version=\"1.0\"?>");
        sb.AppendLine("<mameconfig version=\"10\">");
        sb.AppendLine("    <system name=\"default\">");
        sb.AppendLine("        <input />");
        sb.AppendLine("    </system>");
        sb.AppendLine("</mameconfig>");
        return sb.ToString();
    }

    /// <summary>
    /// Updates flat key-value pairs in mame.ini (space-padded format, no sections).
    /// Format: "key                       value"
    /// </summary>
    private static void UpdateMameIni(string iniPath, Dictionary<string, string> updates)
    {
        var lines = File.ReadAllLines(iniPath);
        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i];
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith('#'))
                continue;

            // Parse key from the line (first non-whitespace token)
            string trimmed = line.TrimStart();
            int spaceIdx = trimmed.IndexOfAny([' ', '\t']);
            if (spaceIdx <= 0) continue;

            string key = trimmed[..spaceIdx];
            if (updates.TryGetValue(key, out string? newValue))
            {
                // Preserve MAME's space-padded format (pad key to 26 chars)
                lines[i] = $"{key,-26}{newValue}";
            }
        }
        File.WriteAllLines(iniPath, lines);
    }
}
