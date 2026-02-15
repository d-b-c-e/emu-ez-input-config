namespace EmuEzInputConfig.Util;

/// <summary>
/// Translates System.Windows.Forms.Keys to emulator-specific hotkey format strings.
/// </summary>
public static class HotkeyFormatter
{
    /// <summary>
    /// Format a key for DuckStation/PCSX2 (Qt-based): "Keyboard/{Key}" or "Keyboard/Shift &amp; Keyboard/{Key}"
    /// </summary>
    public static string FormatQtKey(Keys key, Keys modifier)
    {
        string keyName = KeyToQtName(key);
        if (modifier == Keys.None)
            return $"Keyboard/{keyName}";

        string modName = modifier switch
        {
            Keys.ShiftKey => "Shift",
            Keys.ControlKey => "Control",
            Keys.Menu => "Alt",
            _ => "",
        };

        return string.IsNullOrEmpty(modName)
            ? $"Keyboard/{keyName}"
            : $"Keyboard/{modName} & Keyboard/{keyName}";
    }

    /// <summary>
    /// Format a key for PPSSPP: "1-{androidKeycode}" (device 1 = keyboard)
    /// </summary>
    public static string FormatPpssppKey(Keys key)
    {
        int? code = KeyToAndroidKeycode(key);
        return code.HasValue ? $"1-{code.Value}" : "";
    }

    private static string KeyToQtName(Keys key)
    {
        return key switch
        {
            Keys.Escape => "Escape",
            Keys.Space => "Space",
            Keys.Return => "Return",
            Keys.Tab => "Tab",
            Keys.Back => "Backspace",
            Keys.Delete => "Delete",
            Keys.Up => "Up",
            Keys.Down => "Down",
            Keys.Left => "Left",
            Keys.Right => "Right",
            Keys.Insert => "Insert",
            Keys.Home => "Home",
            Keys.End => "End",
            Keys.PageUp => "PageUp",
            Keys.PageDown => "PageDown",
            Keys.OemPeriod => "Period",
            Keys.Oemcomma => "Comma",
            Keys.Oemplus => "Plus",
            Keys.OemMinus => "Minus",
            >= Keys.F1 and <= Keys.F24 => key.ToString(),
            >= Keys.A and <= Keys.Z => key.ToString(),
            >= Keys.D0 and <= Keys.D9 => key.ToString()[1..],
            _ => key.ToString(),
        };
    }

    /// <summary>
    /// Maps Windows Forms Keys to Android KEYCODE_* constants.
    /// </summary>
    private static int? KeyToAndroidKeycode(Keys key)
    {
        return key switch
        {
            // Letters: KEYCODE_A=29 through KEYCODE_Z=54
            >= Keys.A and <= Keys.Z => 29 + (key - Keys.A),

            // Numbers: KEYCODE_0=7 through KEYCODE_9=16
            >= Keys.D0 and <= Keys.D9 => 7 + (key - Keys.D0),

            // Function keys: KEYCODE_F1=131 through KEYCODE_F12=142
            >= Keys.F1 and <= Keys.F12 => 131 + (key - Keys.F1),

            Keys.Escape => 111,  // KEYCODE_ESCAPE
            Keys.Space => 62,    // KEYCODE_SPACE
            Keys.Return => 66,   // KEYCODE_ENTER
            Keys.Tab => 61,      // KEYCODE_TAB
            Keys.Back => 67,     // KEYCODE_DEL (backspace)
            Keys.Delete => 112,  // KEYCODE_FORWARD_DEL
            Keys.Up => 19,       // KEYCODE_DPAD_UP
            Keys.Down => 20,     // KEYCODE_DPAD_DOWN
            Keys.Left => 21,     // KEYCODE_DPAD_LEFT
            Keys.Right => 22,    // KEYCODE_DPAD_RIGHT

            _ => null,
        };
    }
}
