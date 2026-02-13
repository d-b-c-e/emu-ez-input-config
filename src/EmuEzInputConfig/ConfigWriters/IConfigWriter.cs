namespace EmuEzInputConfig.ConfigWriters;

using EmuEzInputConfig.Models;

/// <summary>
/// Interface for emulator config writers.
/// Each writer knows how to translate InputConfig into a specific emulator's format.
/// </summary>
public interface IConfigWriter
{
    string EmulatorName { get; }

    /// <summary>
    /// Check if the emulator's config file exists at the expected path.
    /// </summary>
    bool ConfigExists(string launchboxRoot);

    /// <summary>
    /// Generate a preview of what would be written (for dry-run / UI display).
    /// </summary>
    Dictionary<string, string> GenerateBindings(InputConfig config);

    /// <summary>
    /// Write the config to disk. Creates a .bak backup first.
    /// </summary>
    void WriteConfig(string launchboxRoot, InputConfig config);
}
