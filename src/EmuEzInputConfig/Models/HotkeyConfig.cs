namespace EmuEzInputConfig.Models;

using System.Text.Json.Serialization;

/// <summary>
/// Configurable hotkey bindings applied across emulators that support native hotkeys.
/// Defaults match the unified LaunchBox Racing Redux hotkey standard.
/// Applies to: DuckStation, PCSX2, PPSSPP.
/// </summary>
public class HotkeyConfig
{
    // Standard hotkeys
    [JsonPropertyName("exitKey")]
    public Keys ExitKey { get; set; } = Keys.Escape;

    [JsonPropertyName("fastForwardKey")]
    public Keys FastForwardKey { get; set; } = Keys.Space;

    [JsonPropertyName("rewindKey")]
    public Keys RewindKey { get; set; } = Keys.R;

    [JsonPropertyName("resetKey")]
    public Keys ResetKey { get; set; } = Keys.F3;

    [JsonPropertyName("saveStateKey")]
    public Keys SaveStateKey { get; set; } = Keys.F5;

    [JsonPropertyName("loadStateKey")]
    public Keys LoadStateKey { get; set; } = Keys.F7;

    // Extended hotkeys
    [JsonPropertyName("previousSaveSlotKey")]
    public Keys PreviousSaveSlotKey { get; set; } = Keys.F3;

    [JsonPropertyName("previousSaveSlotModifier")]
    public Keys PreviousSaveSlotModifier { get; set; } = Keys.ShiftKey;

    [JsonPropertyName("nextSaveSlotKey")]
    public Keys NextSaveSlotKey { get; set; } = Keys.F4;

    [JsonPropertyName("toggleFullscreenKey")]
    public Keys ToggleFullscreenKey { get; set; } = Keys.F11;

    [JsonPropertyName("screenshotKey")]
    public Keys ScreenshotKey { get; set; } = Keys.F10;

    [JsonPropertyName("togglePauseKey")]
    public Keys TogglePauseKey { get; set; } = Keys.P;
}
