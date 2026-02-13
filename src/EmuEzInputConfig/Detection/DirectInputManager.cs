namespace EmuEzInputConfig.Detection;

using SharpDX.DirectInput;

/// <summary>
/// Manages DirectInput device enumeration and polling.
/// Wraps SharpDX.DirectInput to provide clean device access.
/// </summary>
public class DirectInputManager : IDisposable
{
    private readonly DirectInput _directInput;
    private readonly Dictionary<Guid, Joystick> _joysticks = new();

    public DirectInputManager()
    {
        _directInput = new DirectInput();
    }

    /// <summary>
    /// Enumerate all connected game controllers (joysticks, wheels, gamepads).
    /// </summary>
    public List<DeviceInstance> EnumerateDevices()
    {
        var devices = new List<DeviceInstance>();
        foreach (var device in _directInput.GetDevices(DeviceClass.GameControl, DeviceEnumerationFlags.AttachedOnly))
        {
            devices.Add(device);
        }
        return devices;
    }

    /// <summary>
    /// Acquire a joystick by its instance GUID for polling.
    /// </summary>
    public Joystick AcquireDevice(Guid instanceGuid)
    {
        if (_joysticks.TryGetValue(instanceGuid, out var existing))
            return existing;

        var joystick = new Joystick(_directInput, instanceGuid);
        joystick.Properties.BufferSize = 128;
        joystick.Acquire();
        _joysticks[instanceGuid] = joystick;
        return joystick;
    }

    /// <summary>
    /// Poll a joystick and return its current state.
    /// </summary>
    public JoystickState? PollDevice(Guid instanceGuid)
    {
        if (!_joysticks.TryGetValue(instanceGuid, out var joystick))
            return null;

        try
        {
            joystick.Poll();
            return joystick.GetCurrentState();
        }
        catch (SharpDX.SharpDXException)
        {
            // Device disconnected or lost
            return null;
        }
    }

    /// <summary>
    /// Get all axis values from a joystick state as a named dictionary.
    /// Includes ALL axes: X, Y, Z, RotationX, RotationY, RotationZ, Slider0, Slider1.
    /// </summary>
    public static Dictionary<string, int> GetAxisValues(JoystickState state)
    {
        return new Dictionary<string, int>
        {
            ["X"] = state.X,
            ["Y"] = state.Y,
            ["Z"] = state.Z,
            ["RotationX"] = state.RotationX,
            ["RotationY"] = state.RotationY,
            ["RotationZ"] = state.RotationZ,
            ["Slider0"] = state.Sliders[0],
            ["Slider1"] = state.Sliders[1],
        };
    }

    /// <summary>
    /// Get all button states from a joystick state.
    /// Returns indices of pressed buttons (0-indexed).
    /// </summary>
    public static List<int> GetPressedButtons(JoystickState state)
    {
        var pressed = new List<int>();
        var buttons = state.Buttons;
        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i])
                pressed.Add(i);
        }
        return pressed;
    }

    /// <summary>
    /// Get hat/POV direction from a joystick state.
    /// Returns null if centered, otherwise "up", "down", "left", "right",
    /// or compound like "up-right".
    /// </summary>
    public static string? GetHatDirection(JoystickState state, int hatIndex = 0)
    {
        var povs = state.PointOfViewControllers;
        if (hatIndex >= povs.Length)
            return null;

        int pov = povs[hatIndex];
        if (pov == -1)
            return null; // centered

        // POV values are in hundredths of degrees (0 = up, 9000 = right, etc.)
        return pov switch
        {
            >= 0 and < 4500 => "up",
            >= 4500 and < 13500 => "right",
            >= 13500 and < 22500 => "down",
            >= 22500 and < 31500 => "left",
            >= 31500 => "up",
            _ => null
        };
    }

    public void Dispose()
    {
        foreach (var joystick in _joysticks.Values)
        {
            try { joystick.Unacquire(); } catch { }
            joystick.Dispose();
        }
        _joysticks.Clear();
        _directInput.Dispose();
        GC.SuppressFinalize(this);
    }
}
