namespace EmuEzInputConfig.Tests.ConfigWriters;

using EmuEzInputConfig.ConfigWriters;
using EmuEzInputConfig.Tests.TestHelpers;

public class Pcsx2ConfigWriterTests
{
    private readonly Pcsx2ConfigWriter _writer = new();

    [Fact]
    public void MozaR12_Type_IsDualShock2()
    {
        var config = InputConfigBuilder.MozaR12().Build();
        var b = _writer.GenerateBindings(config);
        Assert.Equal("DualShock2", b["Type"]);
    }

    [Fact]
    public void MozaR12_Steering_CorrectHalfAxes()
    {
        var config = InputConfigBuilder.MozaR12().Build();
        var b = _writer.GenerateBindings(config);
        Assert.Equal("DInput-1/-Axis0", b["LLeft"]);
        Assert.Equal("DInput-1/+Axis0", b["LRight"]);
    }

    [Fact]
    public void MozaR12_GasBrake_CorrectAxes()
    {
        var config = InputConfigBuilder.MozaR12().Build();
        var b = _writer.GenerateBindings(config);
        Assert.Equal("DInput-1/+Axis2", b["R2"]);   // Gas = Z = index 2
        Assert.Equal("DInput-1/+Axis3", b["L2"]);   // Brake = RotationX = index 3
    }

    [Fact]
    public void MozaR12_GearPaddles_ZeroIndexedButtons()
    {
        var config = InputConfigBuilder.MozaR12().Build();
        var b = _writer.GenerateBindings(config);
        Assert.Equal("DInput-1/Button13", b["R1"]);  // Gear up (0-indexed)
        Assert.Equal("DInput-1/Button12", b["L1"]);  // Gear down (0-indexed)
    }

    [Fact]
    public void MozaR12_HatDpad_CorrectFormat()
    {
        var config = InputConfigBuilder.MozaR12().Build();
        var b = _writer.GenerateBindings(config);
        Assert.Equal("DInput-1/Hat0Up", b["Up"]);
        Assert.Equal("DInput-1/Hat0Down", b["Down"]);
        Assert.Equal("DInput-1/Hat0Left", b["Left"]);
        Assert.Equal("DInput-1/Hat0Right", b["Right"]);
    }

    [Fact]
    public void MozaR12_FaceButtons_DirectMapping()
    {
        var config = InputConfigBuilder.MozaR12().Build();
        var b = _writer.GenerateBindings(config);
        Assert.Equal("DInput-1/Button0", b["Cross"]);     // btnA
        Assert.Equal("DInput-1/Button1", b["Circle"]);    // btnB
        Assert.Equal("DInput-1/Button2", b["Square"]);    // btnX
        Assert.Equal("DInput-1/Button3", b["Triangle"]);  // btnY
    }

    [Fact]
    public void MozaR12_StartSelect_CorrectButtons()
    {
        var config = InputConfigBuilder.MozaR12().Build();
        var b = _writer.GenerateBindings(config);
        Assert.Equal("DInput-1/Button36", b["Start"]);
        Assert.Equal("DInput-1/Button23", b["Select"]);
    }

    [Fact]
    public void MozaR12_ForceFeedback_OnSteeringAxis()
    {
        var config = InputConfigBuilder.MozaR12().Build();
        var b = _writer.GenerateBindings(config);
        Assert.Equal("DInput-1/FullAxis0", b["LargeMotor"]);
    }

    [Fact]
    public void DInputIndex2_AllBindingsUseDInput2()
    {
        var config = InputConfigBuilder.MozaR12().WithDInputIndex(2).Build();
        var b = _writer.GenerateBindings(config);
        Assert.Equal("DInput-2/-Axis0", b["LLeft"]);
        Assert.Equal("DInput-2/+Axis0", b["LRight"]);
        Assert.Equal("DInput-2/+Axis2", b["R2"]);
        Assert.Equal("DInput-2/Button13", b["R1"]);
        Assert.Equal("DInput-2/Hat0Up", b["Up"]);
        Assert.Equal("DInput-2/Button0", b["Cross"]);
    }

    [Fact]
    public void NoBtnA_CrossFallsBackToGasAxis()
    {
        var config = InputConfigBuilder.MozaR12().WithoutMapping("btnA").Build();
        var b = _writer.GenerateBindings(config);
        Assert.Equal("DInput-1/+Axis2", b["Cross"]);  // Gas axis fallback
    }

    [Fact]
    public void NoBtnB_CircleFallsBackToStartButton()
    {
        var config = InputConfigBuilder.MozaR12().WithoutMapping("btnB").Build();
        var b = _writer.GenerateBindings(config);
        Assert.Equal("DInput-1/Button36", b["Circle"]);  // Start button fallback
    }

    [Fact]
    public void NoBtnX_SquareFallsBackToBrakeAxis()
    {
        var config = InputConfigBuilder.MozaR12().WithoutMapping("btnX").Build();
        var b = _writer.GenerateBindings(config);
        Assert.Equal("DInput-1/+Axis3", b["Square"]);  // Brake axis fallback
    }

    [Fact]
    public void NoBtnY_TriangleFallsBackToGearUpButton()
    {
        var config = InputConfigBuilder.MozaR12().WithoutMapping("btnY").Build();
        var b = _writer.GenerateBindings(config);
        Assert.Equal("DInput-1/Button13", b["Triangle"]);  // GearUp button fallback
    }

    [Fact]
    public void MinimalAxesOnly_UnmappedButtonsAreEmpty()
    {
        var config = InputConfigBuilder.MinimalAxesOnly().Build();
        var b = _writer.GenerateBindings(config);
        Assert.Equal("", b["R1"]);      // No gear up
        Assert.Equal("", b["L1"]);      // No gear down
        Assert.Equal("", b["Start"]);
        Assert.Equal("", b["Select"]);
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
