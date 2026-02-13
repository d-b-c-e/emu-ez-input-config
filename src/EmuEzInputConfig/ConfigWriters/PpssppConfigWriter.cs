namespace EmuEzInputConfig.ConfigWriters;

using EmuEzInputConfig.Models;
using EmuEzInputConfig.Util;

/// <summary>
/// Writes PPSSPP [ControlMapping] section using device-code pairs.
/// Format: {deviceId}-{keyCode} where deviceId = 10 + DInputIndex.
/// Target file: Emulators\ppsspp\memstick\PSP\SYSTEM\controls.ini
/// </summary>
public class PpssppConfigWriter : IConfigWriter
{
    public string EmulatorName => "PPSSPP";

    // DInput axis name → Android JOYSTICK_AXIS_* constant
    // Used to calculate axis key codes: positive = 4000 + axisId * 2, negative = 4000 + axisId * 2 + 1
    private static readonly Dictionary<string, int> AndroidAxisIdMap = new()
    {
        ["X"] = 0,           // JOYSTICK_AXIS_X
        ["Y"] = 1,           // JOYSTICK_AXIS_Y
        ["Z"] = 11,          // JOYSTICK_AXIS_Z
        ["RotationX"] = 12,  // JOYSTICK_AXIS_RX
        ["RotationY"] = 13,  // JOYSTICK_AXIS_RY
        ["RotationZ"] = 14,  // JOYSTICK_AXIS_RZ
    };

    // PPSSPP D-pad hat codes (NKCODE_DPAD_*)
    private const int NKCODE_DPAD_UP = 19;
    private const int NKCODE_DPAD_DOWN = 20;
    private const int NKCODE_DPAD_LEFT = 21;
    private const int NKCODE_DPAD_RIGHT = 22;

    // First DInput button maps to NKCODE_BUTTON_1 = 188
    private const int NKCODE_BUTTON_1 = 188;

    /// <summary>
    /// Get the PPSSPP axis key code for positive or negative direction.
    /// Returns null if the mapping is not an axis or axis is unknown.
    /// </summary>
    private static int? GetAxisCode(InputMapping? m, bool positive)
    {
        if (m?.Type != "axis" || m.Axis == null) return null;
        if (!AndroidAxisIdMap.TryGetValue(m.Axis, out int axisId)) return null;
        return positive ? 4000 + axisId * 2 : 4000 + axisId * 2 + 1;
    }

    /// <summary>
    /// Get the PPSSPP button key code (NKCODE_BUTTON_1 + buttonIndex).
    /// </summary>
    private static int? GetButtonCode(InputMapping? m)
    {
        if (m?.Type != "button" || !m.ButtonIndex.HasValue) return null;
        return NKCODE_BUTTON_1 + m.ButtonIndex.Value;
    }

    /// <summary>
    /// Get the PPSSPP hat/dpad key code.
    /// </summary>
    private static int? GetHatCode(InputMapping? m, string direction)
    {
        if (m?.Type != "hat") return null;
        return direction switch
        {
            "up" => NKCODE_DPAD_UP,
            "down" => NKCODE_DPAD_DOWN,
            "left" => NKCODE_DPAD_LEFT,
            "right" => NKCODE_DPAD_RIGHT,
            _ => null,
        };
    }

    public bool ConfigExists(string launchboxRoot)
    {
        string ini = Path.Combine(launchboxRoot, @"Emulators\ppsspp\memstick\PSP\SYSTEM\controls.ini");
        return File.Exists(ini);
    }

    public Dictionary<string, string> GenerateBindings(InputConfig config)
    {
        var m = config.Mappings;
        // PPSSPP device ID: first DInput device = 10, second = 11, etc.
        int deviceId = 10 + (config.Wheel?.DInputIndex ?? 1);
        string dev = deviceId.ToString();

        // Build multi-binding lists per PSP control
        // PPSSPP supports comma-separated bindings: "Cross = 11-189,11-4022"

        var bindings = new Dictionary<string, List<string>>();
        void Add(string pspKey, int? code)
        {
            if (!code.HasValue) return;
            if (!bindings.ContainsKey(pspKey)) bindings[pspKey] = [];
            string binding = $"{dev}-{code.Value}";
            if (!bindings[pspKey].Contains(binding))
                bindings[pspKey].Add(binding);
        }

        // Steering → Analog stick
        var steer = m.GetValueOrDefault("steering");
        Add("An.Right", GetAxisCode(steer, positive: true));
        Add("An.Left", GetAxisCode(steer, positive: false));

        // Gas → Cross (most PSP racing games use Cross for gas)
        var gas = m.GetValueOrDefault("gas");
        Add("Cross", GetAxisCode(gas, positive: true));

        // Brake → Square (common brake button in PSP racing)
        var brake = m.GetValueOrDefault("brake");
        Add("Square", GetAxisCode(brake, positive: true));

        // Gear paddles → R/L shoulder buttons
        Add("R", GetButtonCode(m.GetValueOrDefault("gearUp")));
        Add("L", GetButtonCode(m.GetValueOrDefault("gearDown")));

        // Start/Select
        Add("Start", GetButtonCode(m.GetValueOrDefault("start")));
        Add("Select", GetButtonCode(m.GetValueOrDefault("coin")));

        // Face buttons (multi-bind with gas/brake axis if both detected)
        Add("Cross", GetButtonCode(m.GetValueOrDefault("btnA")));
        Add("Circle", GetButtonCode(m.GetValueOrDefault("btnB")));
        Add("Square", GetButtonCode(m.GetValueOrDefault("btnX")));
        Add("Triangle", GetButtonCode(m.GetValueOrDefault("btnY")));

        // D-Pad
        var dpadUp = m.GetValueOrDefault("dpadUp");
        var dpadDown = m.GetValueOrDefault("dpadDown");
        var dpadLeft = m.GetValueOrDefault("dpadLeft");
        var dpadRight = m.GetValueOrDefault("dpadRight");

        Add("Up", GetHatCode(dpadUp, "up"));
        Add("Up", GetButtonCode(dpadUp));
        Add("Down", GetHatCode(dpadDown, "down"));
        Add("Down", GetButtonCode(dpadDown));
        Add("Left", GetHatCode(dpadLeft, "left"));
        Add("Left", GetButtonCode(dpadLeft));
        Add("Right", GetHatCode(dpadRight, "right"));
        Add("Right", GetButtonCode(dpadRight));

        // Convert to comma-separated strings
        var result = new Dictionary<string, string>();
        foreach (var (key, codes) in bindings)
        {
            result[key] = string.Join(",", codes);
        }
        return result;
    }

    public void WriteConfig(string launchboxRoot, InputConfig config)
    {
        string iniPath = Path.Combine(launchboxRoot, @"Emulators\ppsspp\memstick\PSP\SYSTEM\controls.ini");
        if (!File.Exists(iniPath)) return;

        var bindings = GenerateBindings(config);
        IniEditor.BackupFile(iniPath);
        IniEditor.UpdateSection(iniPath, "ControlMapping", bindings);
    }
}
