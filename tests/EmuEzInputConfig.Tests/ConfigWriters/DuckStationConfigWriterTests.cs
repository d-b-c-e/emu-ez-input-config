namespace EmuEzInputConfig.Tests.ConfigWriters;

using EmuEzInputConfig.ConfigWriters;
using EmuEzInputConfig.Tests.TestHelpers;

public class DuckStationConfigWriterTests
{
    private readonly DuckStationConfigWriter _writer = new();

    [Fact]
    public void MozaR12_Type_IsNeGcon()
    {
        var config = InputConfigBuilder.MozaR12().Build();
        var b = _writer.GenerateBindings(config);
        Assert.Equal("NeGcon", b["Type"]);
    }

    [Fact]
    public void MozaR12_Steering_HalfAxes()
    {
        var config = InputConfigBuilder.MozaR12().Build();
        var b = _writer.GenerateBindings(config);
        Assert.Equal("DInput-1/+Axis0", b["SteeringRight"]);
        Assert.Equal("DInput-1/-Axis0", b["SteeringLeft"]);
    }

    [Fact]
    public void MozaR12_GasBrake_FullAxis()
    {
        var config = InputConfigBuilder.MozaR12().Build();
        var b = _writer.GenerateBindings(config);
        Assert.Equal("DInput-1/FullAxis2", b["I"]);   // Gas = FullAxis (not +Axis)
        Assert.Equal("DInput-1/FullAxis3", b["II"]);  // Brake = FullAxis
    }

    [Fact]
    public void MozaR12_GearPaddles_ZeroIndexed()
    {
        var config = InputConfigBuilder.MozaR12().Build();
        var b = _writer.GenerateBindings(config);
        Assert.Equal("DInput-1/Button13", b["R"]);   // Gear up (0-indexed)
        Assert.Equal("DInput-1/Button12", b["L"]);   // Gear down (0-indexed)
    }

    [Fact]
    public void MozaR12_HatDpad_SpaceBeforeDirection()
    {
        var config = InputConfigBuilder.MozaR12().Build();
        var b = _writer.GenerateBindings(config);
        Assert.Equal("DInput-1/Hat0 Up", b["Up"]);
        Assert.Equal("DInput-1/Hat0 Down", b["Down"]);
        Assert.Equal("DInput-1/Hat0 Left", b["Left"]);
        Assert.Equal("DInput-1/Hat0 Right", b["Right"]);
    }

    [Fact]
    public void MozaR12_FaceButtons_ZeroIndexed()
    {
        var config = InputConfigBuilder.MozaR12().Build();
        var b = _writer.GenerateBindings(config);
        Assert.Equal("DInput-1/Button0", b["A"]);   // btnA
        Assert.Equal("DInput-1/Button1", b["B"]);   // btnB
    }

    [Fact]
    public void MozaR12_SteeringDeadzone_Zero()
    {
        var config = InputConfigBuilder.MozaR12().Build();
        var b = _writer.GenerateBindings(config);
        Assert.Equal("0.00", b["SteeringDeadzone"]);
        Assert.Equal("0.00", b["IDeadzone"]);
        Assert.Equal("0.00", b["IIDeadzone"]);
    }

    [Fact]
    public void DInputIndex2_AllBindingsUseDInput2()
    {
        var config = InputConfigBuilder.MozaR12().WithDInputIndex(2).Build();
        var b = _writer.GenerateBindings(config);
        Assert.Equal("DInput-2/+Axis0", b["SteeringRight"]);
        Assert.Equal("DInput-2/FullAxis2", b["I"]);
        Assert.Equal("DInput-2/Button13", b["R"]);
        Assert.Equal("DInput-2/Hat0 Up", b["Up"]);
    }

    [Fact]
    public void MissingFaceButtons_EmptyStrings()
    {
        var config = InputConfigBuilder.MinimalAxesOnly().Build();
        var b = _writer.GenerateBindings(config);
        Assert.Equal("", b["A"]);
        Assert.Equal("", b["B"]);
        Assert.Equal("", b["R"]);
        Assert.Equal("", b["L"]);
        Assert.Equal("", b["Start"]);
    }

    [Fact]
    public void ButtonDpad_UsesButtonFormat()
    {
        var config = InputConfigBuilder.MozaR12()
            .WithButtonDpad(up: 5, down: 6, left: 7, right: 8)
            .Build();
        var b = _writer.GenerateBindings(config);
        Assert.Equal("DInput-1/Button5", b["Up"]);
        Assert.Equal("DInput-1/Button6", b["Down"]);
        Assert.Equal("DInput-1/Button7", b["Left"]);
        Assert.Equal("DInput-1/Button8", b["Right"]);
    }
}
