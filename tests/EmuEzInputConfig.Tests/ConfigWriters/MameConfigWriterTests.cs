namespace EmuEzInputConfig.Tests.ConfigWriters;

using EmuEzInputConfig.ConfigWriters;
using EmuEzInputConfig.Tests.TestHelpers;

public class MameConfigWriterTests
{
    private readonly MameConfigWriter _writer = new();

    [Fact]
    public void MozaR12_Steering_DualPaddleAndDial()
    {
        var config = InputConfigBuilder.MozaR12().Build();
        var b = _writer.GenerateBindings(config);
        Assert.Equal("JOYCODE_1_XAXIS", b["P1_PADDLE"]);
        Assert.Equal("JOYCODE_1_XAXIS", b["P1_DIAL"]);  // Same as paddle
    }

    [Fact]
    public void MozaR12_Pedals_CorrectAxes()
    {
        var config = InputConfigBuilder.MozaR12().Build();
        var b = _writer.GenerateBindings(config);
        Assert.Equal("JOYCODE_1_ZAXIS", b["P1_PEDAL"]);     // Gas
        Assert.Equal("JOYCODE_1_RXAXIS", b["P1_PEDAL2"]);   // Brake
    }

    [Fact]
    public void MozaR12_Buttons_OneIndexed()
    {
        var config = InputConfigBuilder.MozaR12().Build();
        var b = _writer.GenerateBindings(config);
        Assert.Equal("JOYCODE_1_BUTTON14", b["P1_BUTTON1"]);  // gearUp: 13+1
        Assert.Equal("JOYCODE_1_BUTTON13", b["P1_BUTTON2"]);  // gearDown: 12+1
        Assert.Equal("JOYCODE_1_BUTTON1", b["P1_BUTTON3"]);   // btnA: 0+1
        Assert.Equal("JOYCODE_1_BUTTON2", b["P1_BUTTON4"]);   // btnB: 1+1
        Assert.Equal("JOYCODE_1_BUTTON3", b["P1_BUTTON5"]);   // btnX: 2+1
        Assert.Equal("JOYCODE_1_BUTTON4", b["P1_BUTTON6"]);   // btnY: 3+1
    }

    [Fact]
    public void MozaR12_HatDpad_Hat1Format()
    {
        var config = InputConfigBuilder.MozaR12().Build();
        var b = _writer.GenerateBindings(config);
        Assert.Equal("JOYCODE_1_HAT1UP", b["P1_JOYSTICK_UP"]);
        Assert.Equal("JOYCODE_1_HAT1DOWN", b["P1_JOYSTICK_DOWN"]);
        Assert.Equal("JOYCODE_1_HAT1LEFT", b["P1_JOYSTICK_LEFT"]);
        Assert.Equal("JOYCODE_1_HAT1RIGHT", b["P1_JOYSTICK_RIGHT"]);
    }

    [Fact]
    public void MozaR12_Start_ButtonOnly()
    {
        var config = InputConfigBuilder.MozaR12().Build();
        var b = _writer.GenerateBindings(config);
        Assert.Equal("JOYCODE_1_BUTTON37", b["START1"]);  // 36+1
    }

    [Fact]
    public void MozaR12_Coin_KeyboardOrJoycode()
    {
        var config = InputConfigBuilder.MozaR12().Build();
        var b = _writer.GenerateBindings(config);
        Assert.Equal("KEYCODE_5 OR JOYCODE_1_BUTTON24", b["COIN1"]);  // 23+1
    }

    [Fact]
    public void NoCoinButton_KeyboardOnly()
    {
        var config = InputConfigBuilder.MozaR12().WithoutMapping("coin").Build();
        var b = _writer.GenerateBindings(config);
        Assert.Equal("KEYCODE_5", b["COIN1"]);
    }

    [Fact]
    public void MinimalAxesOnly_EmptyButtons()
    {
        var config = InputConfigBuilder.MinimalAxesOnly().Build();
        var b = _writer.GenerateBindings(config);
        Assert.Equal("", b["P1_BUTTON1"]);
        Assert.Equal("", b["P1_BUTTON2"]);
        Assert.Equal("", b["START1"]);
        Assert.Equal("KEYCODE_5", b["COIN1"]);  // Always has keyboard fallback
    }

    [Fact]
    public void ButtonDpad_UsesButtonJoycodes()
    {
        var config = InputConfigBuilder.MozaR12()
            .WithButtonDpad(up: 5, down: 6, left: 7, right: 8)
            .Build();
        var b = _writer.GenerateBindings(config);
        Assert.Equal("JOYCODE_1_BUTTON6", b["P1_JOYSTICK_UP"]);     // 5+1
        Assert.Equal("JOYCODE_1_BUTTON7", b["P1_JOYSTICK_DOWN"]);   // 6+1
        Assert.Equal("JOYCODE_1_BUTTON8", b["P1_JOYSTICK_LEFT"]);   // 7+1
        Assert.Equal("JOYCODE_1_BUTTON9", b["P1_JOYSTICK_RIGHT"]);  // 8+1
    }
}
