namespace EmuEzInputConfig.Models;

using System.Text.Json.Serialization;

/// <summary>
/// A single detected input mapping (axis, button, or hat).
/// </summary>
public class InputMapping
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "skipped"; // "axis", "button", "hat", "skipped"

    [JsonPropertyName("axis")]
    public string? Axis { get; set; } // e.g. "X", "Y", "Z", "RotationX", "Slider0"

    [JsonPropertyName("direction")]
    public string? Direction { get; set; } // "+" or "-"

    [JsonPropertyName("value")]
    public double? Value { get; set; }

    [JsonPropertyName("buttonIndex")]
    public int? ButtonIndex { get; set; } // 0-indexed DInput button

    [JsonPropertyName("hatDirection")]
    public string? HatDirection { get; set; } // "up", "down", "left", "right"

    [JsonPropertyName("deviceInstanceId")]
    public string? DeviceInstanceId { get; set; } // DInput device GUID

    [JsonPropertyName("deviceName")]
    public string? DeviceName { get; set; } // Product name from DInput
}

/// <summary>
/// Complete input configuration — saved as JSON, consumed by config writers.
/// </summary>
public class InputConfig
{
    [JsonPropertyName("version")]
    public int Version { get; set; } = 2;

    [JsonPropertyName("timestamp")]
    public string Timestamp { get; set; } = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

    [JsonPropertyName("wheel")]
    public DeviceInfo? Wheel { get; set; }

    [JsonPropertyName("shifter")]
    public DeviceInfo? Shifter { get; set; }

    [JsonPropertyName("gamepadToHide")]
    public string? GamepadToHide { get; set; }

    [JsonPropertyName("mappings")]
    public Dictionary<string, InputMapping> Mappings { get; set; } = new();

    [JsonPropertyName("hotkeys")]
    public HotkeyConfig Hotkeys { get; set; } = new();
}

/// <summary>
/// Information about a detected device.
/// </summary>
public class DeviceInfo
{
    [JsonPropertyName("instanceId")]
    public string InstanceId { get; set; } = "";

    [JsonPropertyName("productName")]
    public string ProductName { get; set; } = "";

    [JsonPropertyName("dinputIndex")]
    public int DInputIndex { get; set; } // Index assigned by DevReorder
}
