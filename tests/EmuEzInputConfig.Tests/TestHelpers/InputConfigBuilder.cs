namespace EmuEzInputConfig.Tests.TestHelpers;

using EmuEzInputConfig.Models;

public class InputConfigBuilder
{
    private readonly InputConfig _config = new()
    {
        Wheel = new DeviceInfo
        {
            ProductName = "MOZA Racing R12",
            InstanceId = "test-guid",
            DInputIndex = 1,
        },
    };

    /// <summary>
    /// Full Moza R12 config with all standard mappings:
    /// Steering=X, Gas=Z, Brake=RotationX, GearUp=13, GearDown=12,
    /// Start=36, Coin=23, BtnA=0, BtnB=1, BtnX=2, BtnY=3, Hat D-pad.
    /// </summary>
    public static InputConfigBuilder MozaR12() => new InputConfigBuilder()
        .WithAxis("steering", "X")
        .WithAxis("gas", "Z")
        .WithAxis("brake", "RotationX")
        .WithButton("gearUp", 13)
        .WithButton("gearDown", 12)
        .WithButton("start", 36)
        .WithButton("coin", 23)
        .WithButton("btnA", 0)
        .WithButton("btnB", 1)
        .WithButton("btnX", 2)
        .WithButton("btnY", 3)
        .WithHatDpad();

    /// <summary>Minimal config with only the required axes (steering, gas, brake).</summary>
    public static InputConfigBuilder MinimalAxesOnly() => new InputConfigBuilder()
        .WithAxis("steering", "X")
        .WithAxis("gas", "Z")
        .WithAxis("brake", "RotationX");

    public InputConfigBuilder WithAxis(string step, string axisName)
    {
        _config.Mappings[step] = new InputMapping
        {
            Type = "axis",
            Axis = axisName,
            Direction = "+",
        };
        return this;
    }

    public InputConfigBuilder WithButton(string step, int buttonIndex)
    {
        _config.Mappings[step] = new InputMapping
        {
            Type = "button",
            ButtonIndex = buttonIndex,
        };
        return this;
    }

    public InputConfigBuilder WithHatDpad()
    {
        foreach (var (step, dir) in new[] { ("dpadUp", "up"), ("dpadDown", "down"), ("dpadLeft", "left"), ("dpadRight", "right") })
        {
            _config.Mappings[step] = new InputMapping
            {
                Type = "hat",
                HatDirection = dir,
            };
        }
        return this;
    }

    public InputConfigBuilder WithButtonDpad(int up, int down, int left, int right)
    {
        WithButton("dpadUp", up);
        WithButton("dpadDown", down);
        WithButton("dpadLeft", left);
        WithButton("dpadRight", right);
        return this;
    }

    public InputConfigBuilder WithDInputIndex(int index)
    {
        _config.Wheel!.DInputIndex = index;
        return this;
    }

    public InputConfigBuilder WithProductName(string name)
    {
        _config.Wheel!.ProductName = name;
        return this;
    }

    public InputConfigBuilder WithGamepadToHide(string name)
    {
        _config.GamepadToHide = name;
        return this;
    }

    public InputConfigBuilder WithoutMapping(string step)
    {
        _config.Mappings.Remove(step);
        return this;
    }

    public InputConfig Build() => _config;
}
