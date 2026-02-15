namespace EmuEzInputConfig.ConfigWriters;

using EmuEzInputConfig.Models;
using EmuEzInputConfig.Util;
using static EmuEzInputConfig.Util.HotkeyFormatter;

/// <summary>
/// Writes DuckStation [Pad1] section using NeGcon controller type (PS1 racing controller).
/// NeGcon provides analog steering, gas (I), and brake (II) — ideal for racing wheels.
/// Target file: Emulators\DuckStation\settings.ini
/// </summary>
public class DuckStationConfigWriter : IConfigWriter
{
    public string EmulatorName => "DuckStation";

    // DInput axis name → DuckStation axis index (same as PCSX2)
    private static readonly Dictionary<string, int> AxisIndexMap = new()
    {
        ["X"] = 0, ["Y"] = 1, ["Z"] = 2,
        ["RotationX"] = 3, ["RotationY"] = 4, ["RotationZ"] = 5,
        ["Slider0"] = 6, ["Slider1"] = 7,
    };

    private static int? GetAxisIndex(InputMapping? m)
    {
        if (m?.Type != "axis" || m.Axis == null) return null;
        return AxisIndexMap.GetValueOrDefault(m.Axis, -1) is var idx && idx >= 0 ? idx : null;
    }

    private static int? GetButtonIndex(InputMapping? m)
    {
        if (m?.Type != "button") return null;
        return m.ButtonIndex;
    }

    public bool ConfigExists(string launchboxRoot)
    {
        string ini = Path.Combine(launchboxRoot, @"Emulators\DuckStation\settings.ini");
        return File.Exists(ini);
    }

    public Dictionary<string, string> GenerateBindings(InputConfig config)
    {
        var m = config.Mappings;
        int dinputIdx = config.Wheel?.DInputIndex ?? 1;
        string dev = $"DInput-{dinputIdx}";

        int? steerIdx = GetAxisIndex(m.GetValueOrDefault("steering"));
        int? gasIdx = GetAxisIndex(m.GetValueOrDefault("gas"));
        int? brakeIdx = GetAxisIndex(m.GetValueOrDefault("brake"));

        int? gearUpBtn = GetButtonIndex(m.GetValueOrDefault("gearUp"));
        int? gearDownBtn = GetButtonIndex(m.GetValueOrDefault("gearDown"));
        int? startBtn = GetButtonIndex(m.GetValueOrDefault("start"));

        int? btnA = GetButtonIndex(m.GetValueOrDefault("btnA"));
        int? btnB = GetButtonIndex(m.GetValueOrDefault("btnB"));

        string GetDpad(string stepName, string hatDir)
        {
            var mapping = m.GetValueOrDefault(stepName);
            if (mapping?.Type == "hat") return $"{dev}/Hat0 {hatDir}";
            if (mapping?.Type == "button" && mapping.ButtonIndex.HasValue)
                return $"{dev}/Button{mapping.ButtonIndex}";
            return "";
        }

        var pad = new Dictionary<string, string>
        {
            // NeGcon controller type — analog steering + gas + brake for racing
            ["Type"] = "NeGcon",

            // D-Pad
            ["Up"] = GetDpad("dpadUp", "Up"),
            ["Down"] = GetDpad("dpadDown", "Down"),
            ["Left"] = GetDpad("dpadLeft", "Left"),
            ["Right"] = GetDpad("dpadRight", "Right"),

            // Start (NeGcon has no Select button)
            ["Start"] = startBtn.HasValue ? $"{dev}/Button{startBtn}" : "",

            // Shoulder buttons — gear paddles
            ["L"] = gearDownBtn.HasValue ? $"{dev}/Button{gearDownBtn}" : "",
            ["R"] = gearUpBtn.HasValue ? $"{dev}/Button{gearUpBtn}" : "",

            // Steering axis — half-axes for left/right direction split
            ["SteeringRight"] = steerIdx.HasValue ? $"{dev}/+Axis{steerIdx}" : "",
            ["SteeringLeft"] = steerIdx.HasValue ? $"{dev}/-Axis{steerIdx}" : "",

            // Face buttons A and B
            ["A"] = btnA.HasValue ? $"{dev}/Button{btnA}" : "",
            ["B"] = btnB.HasValue ? $"{dev}/Button{btnB}" : "",

            // Analog gas (I) and brake (II) — FullAxis because pedals use full 0→65535 range
            // (SDL normalizes pedals to 0.0→1.0 positive half, but DInput reports raw 0→65535)
            ["I"] = gasIdx.HasValue ? $"{dev}/FullAxis{gasIdx}" : "",
            ["II"] = brakeIdx.HasValue ? $"{dev}/FullAxis{brakeIdx}" : "",

            // NeGcon tuning — no deadzone, full range, linear response
            ["SteeringDeadzone"] = "0.00",
            ["SteeringSaturation"] = "1.00",
            ["SteeringLinearity"] = "1.00",
            ["SteeringScaling"] = "Linear",
            ["IDeadzone"] = "0.00",
            ["ISaturation"] = "1.00",
            ["IIDeadzone"] = "0.00",
            ["IISaturation"] = "1.00",
        };

        return pad;
    }

    public void WriteConfig(string launchboxRoot, InputConfig config)
    {
        string iniPath = Path.Combine(launchboxRoot, @"Emulators\DuckStation\settings.ini");
        if (!File.Exists(iniPath)) return;

        var pad = GenerateBindings(config);
        var inputSources = new Dictionary<string, string>
        {
            ["Keyboard"] = "true",
            ["Mouse"] = "true",
            ["SDL"] = "true",
            ["SDLControllerEnhancedMode"] = "false",
            ["DInput"] = "true",
            ["XInput"] = "false",
            ["RawInput"] = "false",
        };

        var h = config.Hotkeys;
        var hotkeys = new Dictionary<string, string>
        {
            ["PowerOff"] = FormatQtKey(h.ExitKey, Keys.None),
            ["FastForward"] = FormatQtKey(h.FastForwardKey, Keys.None),
            ["Rewind"] = FormatQtKey(h.RewindKey, Keys.None),
            ["Reset"] = FormatQtKey(h.ResetKey, Keys.None),
            ["SaveSelectedSaveState"] = FormatQtKey(h.SaveStateKey, Keys.None),
            ["LoadSelectedSaveState"] = FormatQtKey(h.LoadStateKey, Keys.None),
            ["SelectPreviousSaveStateSlot"] = FormatQtKey(h.PreviousSaveSlotKey, h.PreviousSaveSlotModifier),
            ["SelectNextSaveStateSlot"] = FormatQtKey(h.NextSaveSlotKey, Keys.None),
            ["ToggleFullscreen"] = FormatQtKey(h.ToggleFullscreenKey, Keys.None),
            ["Screenshot"] = FormatQtKey(h.ScreenshotKey, Keys.None),
            ["TogglePause"] = FormatQtKey(h.TogglePauseKey, Keys.None),
        };

        IniEditor.BackupFile(iniPath);
        IniEditor.UpdateSection(iniPath, "Pad1", pad);
        IniEditor.UpdateSection(iniPath, "InputSources", inputSources);
        IniEditor.UpdateSection(iniPath, "Hotkeys", hotkeys);
    }
}
