namespace EmuEzInputConfig.Detection;

using EmuEzInputConfig.Models;
using SharpDX.DirectInput;

/// <summary>
/// Detects a single input change from any connected device.
/// Uses baseline comparison with adaptive noise filtering.
/// </summary>
public class InputDetector
{
    private readonly DirectInputManager _manager;

    // Thresholds (DInput axes are 0-65535)
    private const int AxisChangeThreshold = 6000;   // ~9% of full range (~18% of one direction from center)
    private const int AxisDirectionMid = 32767;     // Midpoint
    private const int NoiseMultiplier = 3;
    private const int NoiseMeasureMs = 400;
    private const int DebounceCount = 3;

    public InputDetector(DirectInputManager manager)
    {
        _manager = manager;
    }

    /// <summary>
    /// Describes the result of a detection attempt.
    /// </summary>
    public class DetectionResult
    {
        public bool Success { get; set; }
        public bool TimedOut { get; set; }
        public bool Cancelled { get; set; }
        public InputMapping? Mapping { get; set; }
    }

    /// <summary>
    /// Capture baseline state for all devices (call while user is NOT touching controls).
    /// Returns baseline axis values and button states per device.
    /// </summary>
    public Dictionary<Guid, DeviceBaseline> CaptureBaselines(List<DeviceInstance> devices)
    {
        var baselines = new Dictionary<Guid, DeviceBaseline>();

        foreach (var device in devices)
        {
            var joystick = _manager.AcquireDevice(device.InstanceGuid);
            var state = _manager.PollDevice(device.InstanceGuid);
            if (state == null) continue;

            baselines[device.InstanceGuid] = new DeviceBaseline
            {
                DeviceName = device.ProductName,
                InstanceGuid = device.InstanceGuid,
                Axes = DirectInputManager.GetAxisValues(state),
                Buttons = state.Buttons.ToArray(),
                Hats = state.PointOfViewControllers.ToArray(),
            };
        }

        return baselines;
    }

    /// <summary>
    /// Measure noise on all axes for a period to set adaptive thresholds.
    /// Returns per-axis minimum thresholds.
    /// </summary>
    public Dictionary<string, int> MeasureNoise(
        Dictionary<Guid, DeviceBaseline> baselines,
        CancellationToken ct)
    {
        var axisMin = new Dictionary<string, int>();
        var axisMax = new Dictionary<string, int>();

        // Initialize from baselines
        foreach (var (guid, baseline) in baselines)
        {
            foreach (var (axisName, value) in baseline.Axes)
            {
                string key = $"{guid}:{axisName}";
                axisMin[key] = value;
                axisMax[key] = value;
            }
        }

        // Sample for NoiseMeasureMs
        var sw = System.Diagnostics.Stopwatch.StartNew();
        while (sw.ElapsedMilliseconds < NoiseMeasureMs && !ct.IsCancellationRequested)
        {
            foreach (var (guid, baseline) in baselines)
            {
                var state = _manager.PollDevice(guid);
                if (state == null) continue;

                var axes = DirectInputManager.GetAxisValues(state);
                foreach (var (axisName, value) in axes)
                {
                    string key = $"{guid}:{axisName}";
                    if (value < axisMin[key]) axisMin[key] = value;
                    if (value > axisMax[key]) axisMax[key] = value;
                }
            }
            Thread.Sleep(16);
        }

        // Compute thresholds
        var thresholds = new Dictionary<string, int>();
        foreach (var key in axisMin.Keys)
        {
            int jitter = axisMax[key] - axisMin[key];
            int noiseThreshold = jitter * NoiseMultiplier;
            thresholds[key] = Math.Max(AxisChangeThreshold, noiseThreshold);
        }

        // Update baselines to midpoint of observed range
        foreach (var (guid, baseline) in baselines)
        {
            foreach (var axisName in baseline.Axes.Keys.ToList())
            {
                string key = $"{guid}:{axisName}";
                baseline.Axes[axisName] = (axisMin[key] + axisMax[key]) / 2;
            }
        }

        return thresholds;
    }

    /// <summary>
    /// Wait for a single input change from any device.
    /// Returns when an axis, button, or hat change is detected.
    /// </summary>
    public DetectionResult WaitForInput(
        Dictionary<Guid, DeviceBaseline> baselines,
        Dictionary<string, int> noiseThresholds,
        int timeoutMs,
        CancellationToken ct,
        Action<Dictionary<Guid, Dictionary<string, int>>>? onPoll = null)
    {
        var debounce = new Dictionary<string, int>();
        var sw = System.Diagnostics.Stopwatch.StartNew();

        while (!ct.IsCancellationRequested)
        {
            if (sw.ElapsedMilliseconds > timeoutMs)
                return new DetectionResult { TimedOut = true };

            // Collect live axis values for UI visualization
            var liveAxes = new Dictionary<Guid, Dictionary<string, int>>();

            foreach (var (guid, baseline) in baselines)
            {
                var state = _manager.PollDevice(guid);
                if (state == null) continue;

                var axes = DirectInputManager.GetAxisValues(state);
                liveAxes[guid] = axes;

                // Check axes
                foreach (var (axisName, currentVal) in axes)
                {
                    string key = $"{guid}:{axisName}";
                    int baseVal = baseline.Axes.GetValueOrDefault(axisName, 32767);
                    int threshold = noiseThresholds.GetValueOrDefault(key, AxisChangeThreshold);
                    int delta = Math.Abs(currentVal - baseVal);

                    if (delta > threshold)
                    {
                        debounce.TryGetValue(key, out int count);
                        debounce[key] = count + 1;

                        if (debounce[key] >= DebounceCount)
                        {
                            string direction = currentVal > AxisDirectionMid ? "+" : "-";
                            return new DetectionResult
                            {
                                Success = true,
                                Mapping = new InputMapping
                                {
                                    Type = "axis",
                                    Axis = axisName,
                                    Direction = direction,
                                    Value = currentVal,
                                    DeviceInstanceId = guid.ToString(),
                                    DeviceName = baseline.DeviceName,
                                }
                            };
                        }
                    }
                    else
                    {
                        debounce[key] = 0;
                    }
                }

                // Check buttons
                var buttons = state.Buttons;
                for (int i = 0; i < buttons.Length; i++)
                {
                    if (buttons[i] && (i >= baseline.Buttons.Length || !baseline.Buttons[i]))
                    {
                        return new DetectionResult
                        {
                            Success = true,
                            Mapping = new InputMapping
                            {
                                Type = "button",
                                ButtonIndex = i,
                                DeviceInstanceId = guid.ToString(),
                                DeviceName = baseline.DeviceName,
                            }
                        };
                    }
                }

                // Check hats/POV
                var hats = state.PointOfViewControllers;
                for (int i = 0; i < hats.Length; i++)
                {
                    int baseHat = i < baseline.Hats.Length ? baseline.Hats[i] : -1;
                    if (hats[i] != baseHat && hats[i] != -1)
                    {
                        string? dir = DirectInputManager.GetHatDirection(state, i);
                        if (dir != null)
                        {
                            return new DetectionResult
                            {
                                Success = true,
                                Mapping = new InputMapping
                                {
                                    Type = "hat",
                                    HatDirection = dir,
                                    DeviceInstanceId = guid.ToString(),
                                    DeviceName = baseline.DeviceName,
                                }
                            };
                        }
                    }
                }
            }

            // Notify UI with live axis values for visualization
            onPoll?.Invoke(liveAxes);

            Thread.Sleep(16); // ~60Hz
        }

        return new DetectionResult { Cancelled = true };
    }
}

/// <summary>
/// Snapshot of a device's resting state.
/// </summary>
public class DeviceBaseline
{
    public string DeviceName { get; set; } = "";
    public Guid InstanceGuid { get; set; }
    public Dictionary<string, int> Axes { get; set; } = new();
    public bool[] Buttons { get; set; } = Array.Empty<bool>();
    public int[] Hats { get; set; } = Array.Empty<int>();
}
