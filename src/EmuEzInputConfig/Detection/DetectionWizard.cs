namespace EmuEzInputConfig.Detection;

/// <summary>
/// Defines the steps for the input detection wizard.
/// </summary>
public static class DetectionWizard
{
    public static readonly WizardStep[] Steps =
    [
        // Axes (analog controls)
        new("steering",  "Turn your STEERING WHEEL to the LEFT and hold",    "axis",   true),
        new("gas",       "Press your GAS PEDAL fully and hold",              "axis",   true),
        new("brake",     "Press your BRAKE PEDAL fully and hold",            "axis",   true),
        new("handbrake", "Pull HANDBRAKE lever and hold (skip if none)",     "axis",   false),

        // Gear paddles
        new("gearUp",    "Press GEAR SHIFT UP (right paddle)",               "button", true),
        new("gearDown",  "Press GEAR SHIFT DOWN (left paddle)",              "button", true),

        // System buttons
        new("start",     "Press your START button",                          "button", true),
        new("coin",      "Press your SELECT / COIN button (skip if none)",   "button", false),

        // Face buttons (for console menu navigation)
        new("btnA",      "Press button for A / Cross (confirm)",             "button", false),
        new("btnB",      "Press button for B / Circle (cancel)",             "button", false),
        new("btnX",      "Press button for X / Square (skip if none)",       "button", false),
        new("btnY",      "Press button for Y / Triangle (skip if none)",     "button", false),

        // D-Pad
        new("dpadUp",    "Press D-PAD UP",                                   "hat",    false),
        new("dpadDown",  "Press D-PAD DOWN",                                 "hat",    false),
        new("dpadLeft",  "Press D-PAD LEFT",                                 "hat",    false),
        new("dpadRight", "Press D-PAD RIGHT",                                "hat",    false),
    ];
}

public record WizardStep(
    string Name,
    string Label,
    string ExpectedType,
    bool Required
);
