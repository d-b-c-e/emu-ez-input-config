namespace EmuEzInputConfig.Tests.ConfigWriters;

using EmuEzInputConfig.ConfigWriters;
using EmuEzInputConfig.Tests.TestHelpers;

public class PpssppConfigWriterTests
{
    private readonly PpssppConfigWriter _writer = new();

    [Fact]
    public void MozaR12_Steering_CorrectAxisCodes()
    {
        var config = InputConfigBuilder.MozaR12().Build();
        var b = _writer.GenerateBindings(config);
        // X axis: AndroidAxisId=0, positive=4000+0*2=4000, negative=4001
        Assert.Equal("11-4000", b["An.Right"]);
        Assert.Equal("11-4001", b["An.Left"]);
    }

    [Fact]
    public void MozaR12_Cross_MultiBinding_GasAxisPlusBtnA()
    {
        var config = InputConfigBuilder.MozaR12().Build();
        var b = _writer.GenerateBindings(config);
        // Gas Z axis code=4000+11*2=4022, btnA button code=188+0=188
        Assert.Equal("11-4022,11-188", b["Cross"]);
    }

    [Fact]
    public void MozaR12_Square_MultiBinding_BrakeAxisPlusBtnX()
    {
        var config = InputConfigBuilder.MozaR12().Build();
        var b = _writer.GenerateBindings(config);
        // Brake RotationX axis code=4000+12*2=4024, btnX button code=188+2=190
        Assert.Equal("11-4024,11-190", b["Square"]);
    }

    [Fact]
    public void MozaR12_GearPaddles_ButtonCodes()
    {
        var config = InputConfigBuilder.MozaR12().Build();
        var b = _writer.GenerateBindings(config);
        Assert.Equal("11-201", b["R"]);  // gearUp: 188+13=201
        Assert.Equal("11-200", b["L"]);  // gearDown: 188+12=200
    }

    [Fact]
    public void MozaR12_StartSelect_ButtonCodes()
    {
        var config = InputConfigBuilder.MozaR12().Build();
        var b = _writer.GenerateBindings(config);
        Assert.Equal("11-224", b["Start"]);   // 188+36=224
        Assert.Equal("11-211", b["Select"]);  // 188+23=211
    }

    [Fact]
    public void MozaR12_CircleTriangle_ButtonCodes()
    {
        var config = InputConfigBuilder.MozaR12().Build();
        var b = _writer.GenerateBindings(config);
        Assert.Equal("11-189", b["Circle"]);    // btnB: 188+1=189
        Assert.Equal("11-191", b["Triangle"]);  // btnY: 188+3=191
    }

    [Fact]
    public void MozaR12_HatDpad_NkcodeCodes()
    {
        var config = InputConfigBuilder.MozaR12().Build();
        var b = _writer.GenerateBindings(config);
        Assert.Equal("11-19", b["Up"]);     // NKCODE_DPAD_UP
        Assert.Equal("11-20", b["Down"]);   // NKCODE_DPAD_DOWN
        Assert.Equal("11-21", b["Left"]);   // NKCODE_DPAD_LEFT
        Assert.Equal("11-22", b["Right"]);  // NKCODE_DPAD_RIGHT
    }

    [Fact]
    public void MozaR12_Hotkeys_DefaultKeyCodes()
    {
        var config = InputConfigBuilder.MozaR12().Build();
        var b = _writer.GenerateBindings(config);
        Assert.Equal("1-62", b["Fast-forward"]);   // Space=62
        Assert.Equal("1-44", b["Pause"]);           // P=29+15=44
        Assert.Equal("1-46", b["Rewind"]);          // R=29+17=46
        Assert.Equal("1-135", b["Save State"]);     // F5=131+4=135
        Assert.Equal("1-137", b["Load State"]);     // F7=131+6=137
    }

    [Fact]
    public void DInputIndex2_DeviceId12()
    {
        var config = InputConfigBuilder.MozaR12().WithDInputIndex(2).Build();
        var b = _writer.GenerateBindings(config);
        Assert.Equal("12-4000", b["An.Right"]);  // deviceId=10+2=12
        Assert.Equal("12-201", b["R"]);
    }

    [Fact]
    public void MinimalAxesOnly_NoButtonBindings()
    {
        var config = InputConfigBuilder.MinimalAxesOnly().Build();
        var b = _writer.GenerateBindings(config);
        Assert.False(b.ContainsKey("R"));       // No gear up
        Assert.False(b.ContainsKey("L"));       // No gear down
        Assert.False(b.ContainsKey("Start"));
        Assert.False(b.ContainsKey("Select"));
    }

    [Fact]
    public void MinimalAxesOnly_CrossIsSingleBinding()
    {
        var config = InputConfigBuilder.MinimalAxesOnly().Build();
        var b = _writer.GenerateBindings(config);
        // Only gas axis, no btnA
        Assert.Equal("11-4022", b["Cross"]);
    }

    [Fact]
    public void ButtonDpad_UsesButtonCodesInsteadOfHat()
    {
        var config = InputConfigBuilder.MozaR12()
            .WithButtonDpad(up: 5, down: 6, left: 7, right: 8)
            .Build();
        var b = _writer.GenerateBindings(config);
        // Hat codes should NOT be present; button codes instead
        // Button 5 → 188+5=193, etc.
        Assert.Equal("11-193", b["Up"]);
        Assert.Equal("11-194", b["Down"]);
        Assert.Equal("11-195", b["Left"]);
        Assert.Equal("11-196", b["Right"]);
    }
}
