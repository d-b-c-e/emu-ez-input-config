namespace EmuEzInputConfig.Tests.Util;

using System.Windows.Forms;
using EmuEzInputConfig.Util;

public class HotkeyFormatterTests
{
    // --- FormatQtKey tests ---

    [Fact]
    public void FormatQtKey_Escape_NoModifier()
    {
        string result = HotkeyFormatter.FormatQtKey(Keys.Escape, Keys.None);
        Assert.Equal("Keyboard/Escape", result);
    }

    [Fact]
    public void FormatQtKey_Space_NoModifier()
    {
        string result = HotkeyFormatter.FormatQtKey(Keys.Space, Keys.None);
        Assert.Equal("Keyboard/Space", result);
    }

    [Fact]
    public void FormatQtKey_F3_ShiftModifier()
    {
        string result = HotkeyFormatter.FormatQtKey(Keys.F3, Keys.ShiftKey);
        Assert.Equal("Keyboard/Shift & Keyboard/F3", result);
    }

    [Fact]
    public void FormatQtKey_P_NoModifier()
    {
        string result = HotkeyFormatter.FormatQtKey(Keys.P, Keys.None);
        Assert.Equal("Keyboard/P", result);
    }

    [Fact]
    public void FormatQtKey_F5_NoModifier()
    {
        string result = HotkeyFormatter.FormatQtKey(Keys.F5, Keys.None);
        Assert.Equal("Keyboard/F5", result);
    }

    [Fact]
    public void FormatQtKey_F11_NoModifier()
    {
        string result = HotkeyFormatter.FormatQtKey(Keys.F11, Keys.None);
        Assert.Equal("Keyboard/F11", result);
    }

    [Fact]
    public void FormatQtKey_R_ControlModifier()
    {
        string result = HotkeyFormatter.FormatQtKey(Keys.R, Keys.ControlKey);
        Assert.Equal("Keyboard/Control & Keyboard/R", result);
    }

    [Fact]
    public void FormatQtKey_Number_StripsD()
    {
        string result = HotkeyFormatter.FormatQtKey(Keys.D0, Keys.None);
        Assert.Equal("Keyboard/0", result);
    }

    // --- FormatPpssppKey tests ---

    [Fact]
    public void FormatPpssppKey_Space()
    {
        string result = HotkeyFormatter.FormatPpssppKey(Keys.Space);
        Assert.Equal("1-62", result);
    }

    [Fact]
    public void FormatPpssppKey_Escape()
    {
        string result = HotkeyFormatter.FormatPpssppKey(Keys.Escape);
        Assert.Equal("1-111", result);
    }

    [Fact]
    public void FormatPpssppKey_F5()
    {
        string result = HotkeyFormatter.FormatPpssppKey(Keys.F5);
        Assert.Equal("1-135", result);
    }

    [Fact]
    public void FormatPpssppKey_R()
    {
        string result = HotkeyFormatter.FormatPpssppKey(Keys.R);
        Assert.Equal("1-46", result);  // KEYCODE_R = 29 + 17
    }

    [Fact]
    public void FormatPpssppKey_P()
    {
        string result = HotkeyFormatter.FormatPpssppKey(Keys.P);
        Assert.Equal("1-44", result);  // KEYCODE_P = 29 + 15
    }

    [Fact]
    public void FormatPpssppKey_F7()
    {
        string result = HotkeyFormatter.FormatPpssppKey(Keys.F7);
        Assert.Equal("1-137", result);
    }

    [Fact]
    public void FormatPpssppKey_Tab()
    {
        string result = HotkeyFormatter.FormatPpssppKey(Keys.Tab);
        Assert.Equal("1-61", result);
    }

    [Fact]
    public void FormatPpssppKey_UnmappedKey_EmptyString()
    {
        // Keys without Android keycode mapping should return ""
        string result = HotkeyFormatter.FormatPpssppKey(Keys.PrintScreen);
        Assert.Equal("", result);
    }

    [Fact]
    public void FormatPpssppKey_Letters_SequentialCodes()
    {
        // A=29, B=30, ..., Z=54
        Assert.Equal("1-29", HotkeyFormatter.FormatPpssppKey(Keys.A));
        Assert.Equal("1-54", HotkeyFormatter.FormatPpssppKey(Keys.Z));
    }

    [Fact]
    public void FormatPpssppKey_Numbers_SequentialCodes()
    {
        // 0=7, 1=8, ..., 9=16
        Assert.Equal("1-7", HotkeyFormatter.FormatPpssppKey(Keys.D0));
        Assert.Equal("1-16", HotkeyFormatter.FormatPpssppKey(Keys.D9));
    }

    [Fact]
    public void FormatPpssppKey_FunctionKeys_SequentialCodes()
    {
        // F1=131, F2=132, ..., F12=142
        Assert.Equal("1-131", HotkeyFormatter.FormatPpssppKey(Keys.F1));
        Assert.Equal("1-142", HotkeyFormatter.FormatPpssppKey(Keys.F12));
    }
}
