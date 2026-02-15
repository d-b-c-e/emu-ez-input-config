namespace EmuEzInputConfig.Tests.ConfigWriters;

using EmuEzInputConfig.ConfigWriters;
using EmuEzInputConfig.Tests.TestHelpers;

public class Rpcs3ConfigWriterTests
{
    private readonly Rpcs3ConfigWriter _writer = new();

    [Fact]
    public void MozaR12_Steering_AxisFormat()
    {
        var config = InputConfigBuilder.MozaR12().Build();
        var b = _writer.GenerateBindings(config);
        Assert.Equal("+Axis X+", b["Left Stick Right"]);
        Assert.Equal("-Axis X-", b["Left Stick Left"]);
    }

    [Fact]
    public void MozaR12_GasBrake_AxisFormat()
    {
        var config = InputConfigBuilder.MozaR12().Build();
        var b = _writer.GenerateBindings(config);
        Assert.Equal("+Axis Z+", b["R2"]);    // Gas
        Assert.Equal("+Axis RX+", b["L2"]);   // Brake
    }

    [Fact]
    public void MozaR12_GearPaddles_OneIndexed()
    {
        var config = InputConfigBuilder.MozaR12().Build();
        var b = _writer.GenerateBindings(config);
        Assert.Equal("Button 14", b["R1"]);   // gearUp: 13+1
        Assert.Equal("Button 13", b["L1"]);   // gearDown: 12+1
    }

    [Fact]
    public void MozaR12_FaceButtons_OneIndexed_NoCrossFallback()
    {
        var config = InputConfigBuilder.MozaR12().Build();
        var b = _writer.GenerateBindings(config);
        Assert.Equal("Button 1", b["Cross"]);     // btnA: 0+1
        Assert.Equal("Button 2", b["Circle"]);    // btnB: 1+1 (independent, no start fallback)
        Assert.Equal("Button 3", b["Square"]);    // btnX: 2+1
        Assert.Equal("Button 4", b["Triangle"]);  // btnY: 3+1
    }

    [Fact]
    public void MozaR12_StartSelect_OneIndexed()
    {
        var config = InputConfigBuilder.MozaR12().Build();
        var b = _writer.GenerateBindings(config);
        Assert.Equal("Button 37", b["Start"]);    // 36+1
        Assert.Equal("Button 24", b["Select"]);   // 23+1
    }

    [Fact]
    public void MozaR12_DpadAlwaysEmpty()
    {
        var config = InputConfigBuilder.MozaR12().Build();
        var b = _writer.GenerateBindings(config);
        // RPCS3 doesn't map hat dpad — these are always empty
        Assert.Equal("", b["Up"]);
        Assert.Equal("", b["Down"]);
        Assert.Equal("", b["Left"]);
        Assert.Equal("", b["Right"]);
    }

    [Fact]
    public void MozaR12_UnusedSticks_Empty()
    {
        var config = InputConfigBuilder.MozaR12().Build();
        var b = _writer.GenerateBindings(config);
        Assert.Equal("", b["Left Stick Down"]);
        Assert.Equal("", b["Left Stick Up"]);
        Assert.Equal("", b["Right Stick Left"]);
        Assert.Equal("", b["Right Stick Right"]);
        Assert.Equal("", b["Right Stick Down"]);
        Assert.Equal("", b["Right Stick Up"]);
        Assert.Equal("", b["R3"]);
        Assert.Equal("", b["L3"]);
        Assert.Equal("", b["PS Button"]);
    }

    [Fact]
    public void MissingMapping_EmptyString()
    {
        var config = InputConfigBuilder.MinimalAxesOnly().Build();
        var b = _writer.GenerateBindings(config);
        Assert.Equal("", b["Cross"]);     // No btnA → empty (no fallback)
        Assert.Equal("", b["Circle"]);
        Assert.Equal("", b["R1"]);
        Assert.Equal("", b["L1"]);
        Assert.Equal("", b["Start"]);
        Assert.Equal("", b["Select"]);
    }

    [Fact]
    public void MinimalAxesOnly_AxesStillPresent()
    {
        var config = InputConfigBuilder.MinimalAxesOnly().Build();
        var b = _writer.GenerateBindings(config);
        Assert.Equal("+Axis X+", b["Left Stick Right"]);
        Assert.Equal("-Axis X-", b["Left Stick Left"]);
        Assert.Equal("+Axis Z+", b["R2"]);
        Assert.Equal("+Axis RX+", b["L2"]);
    }

    [Fact]
    public void AllKeysPresent_25Keys()
    {
        var config = InputConfigBuilder.MozaR12().Build();
        var b = _writer.GenerateBindings(config);
        // RPCS3 always outputs all 25 fixed keys
        Assert.Equal(25, b.Count);
    }
}
