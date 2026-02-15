namespace EmuEzInputConfig.Tests.ConfigWriters;

using EmuEzInputConfig.ConfigWriters;
using EmuEzInputConfig.Tests.TestHelpers;

public class SupermodelConfigWriterTests
{
    private readonly SupermodelConfigWriter _writer = new();

    [Fact]
    public void MozaR12_Steering_TripleFormat()
    {
        var config = InputConfigBuilder.MozaR12().Build();
        var b = _writer.GenerateBindings(config);
        Assert.Equal("JOY2_XAXIS", b["InputSteering"]);
        Assert.Equal("JOY2_XAXIS_NEG", b["InputSteeringLeft"]);
        Assert.Equal("JOY2_XAXIS_POS", b["InputSteeringRight"]);
    }

    [Fact]
    public void MozaR12_GasBrake_PositiveHalfAxis()
    {
        var config = InputConfigBuilder.MozaR12().Build();
        var b = _writer.GenerateBindings(config);
        Assert.Equal("JOY2_ZAXIS_POS", b["InputAccelerator"]);
        Assert.Equal("JOY2_RXAXIS_POS", b["InputBrake"]);
    }

    [Fact]
    public void MozaR12_Buttons_OneIndexed()
    {
        var config = InputConfigBuilder.MozaR12().Build();
        var b = _writer.GenerateBindings(config);
        Assert.Equal("JOY2_BUTTON14", b["InputGearShiftUp"]);    // 13+1
        Assert.Equal("JOY2_BUTTON13", b["InputGearShiftDown"]);  // 12+1
    }

    [Fact]
    public void MozaR12_StartCoin_WithKeyboardFallback()
    {
        var config = InputConfigBuilder.MozaR12().Build();
        var b = _writer.GenerateBindings(config);
        Assert.Equal("JOY2_BUTTON37,KEY_1", b["InputStart1"]);  // 36+1
        Assert.Equal("JOY2_BUTTON24,KEY_5", b["InputCoin1"]);   // 23+1
    }

    [Fact]
    public void DInputIndex2_UsesJOY3()
    {
        var config = InputConfigBuilder.MozaR12().WithDInputIndex(2).Build();
        var b = _writer.GenerateBindings(config);
        Assert.Equal("JOY3_XAXIS", b["InputSteering"]);
        Assert.Equal("JOY3_BUTTON14", b["InputGearShiftUp"]);
    }

    [Fact]
    public void MissingMapping_KeyAbsentFromDictionary()
    {
        var config = InputConfigBuilder.MinimalAxesOnly().Build();
        var b = _writer.GenerateBindings(config);
        Assert.False(b.ContainsKey("InputGearShiftUp"));
        Assert.False(b.ContainsKey("InputGearShiftDown"));
        Assert.False(b.ContainsKey("InputStart1"));
        Assert.False(b.ContainsKey("InputCoin1"));
    }

    [Fact]
    public void MinimalAxesOnly_SteeringStillPresent()
    {
        var config = InputConfigBuilder.MinimalAxesOnly().Build();
        var b = _writer.GenerateBindings(config);
        Assert.Equal("JOY2_XAXIS", b["InputSteering"]);
        Assert.Equal("JOY2_ZAXIS_POS", b["InputAccelerator"]);
        Assert.Equal("JOY2_RXAXIS_POS", b["InputBrake"]);
    }
}
