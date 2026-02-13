namespace EmuEzInputConfig.Util;

/// <summary>
/// INI file editor — reads/writes key=value pairs within [Section] blocks.
/// Preserves all content outside the target section.
/// </summary>
public static class IniEditor
{
    /// <summary>
    /// Create a timestamped .bak copy of a file before modifying.
    /// </summary>
    public static void BackupFile(string filePath)
    {
        if (!File.Exists(filePath)) return;
        string bakPath = $"{filePath}.bak.{DateTime.Now:yyyyMMdd-HHmmss}";
        File.Copy(filePath, bakPath, overwrite: true);
    }

    /// <summary>
    /// Update key=value pairs within a specific [Section] of an INI file.
    /// Keys not found in the section are appended before the next section header.
    /// Keys not in the values dictionary are left unchanged.
    /// </summary>
    public static void UpdateSection(string filePath, string section, Dictionary<string, string> values)
    {
        var lines = File.ReadAllLines(filePath).ToList();
        var output = new List<string>();
        bool inSection = false;
        bool sectionDone = false;
        var keysWritten = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var line in lines)
        {
            // Detect section headers
            if (line.TrimStart().StartsWith('['))
            {
                if (inSection && !sectionDone)
                {
                    // Leaving target section — append any unwritten keys
                    foreach (var kv in values)
                    {
                        if (!keysWritten.Contains(kv.Key))
                        {
                            output.Add($"{kv.Key} = {kv.Value}");
                            keysWritten.Add(kv.Key);
                        }
                    }
                    sectionDone = true;
                }

                string sectionName = line.Trim().TrimStart('[').TrimEnd(']').Trim();
                inSection = sectionName.Equals(section, StringComparison.OrdinalIgnoreCase);
            }

            if (inSection && !sectionDone)
            {
                // Try to match key = value
                int eqIdx = line.IndexOf('=');
                if (eqIdx > 0 && !line.TrimStart().StartsWith('['))
                {
                    string key = line[..eqIdx].Trim();
                    if (values.ContainsKey(key))
                    {
                        output.Add($"{key} = {values[key]}");
                        keysWritten.Add(key);
                        continue;
                    }
                }
            }

            output.Add(line);
        }

        // If section was the last in file, append remaining keys
        if (inSection && !sectionDone)
        {
            foreach (var kv in values)
            {
                if (!keysWritten.Contains(kv.Key))
                    output.Add($"{kv.Key} = {kv.Value}");
            }
        }

        File.WriteAllLines(filePath, output);
    }

    /// <summary>
    /// Update key=value lines anywhere in the file (no section awareness).
    /// Used for Supermodel which has flat key=value format.
    /// </summary>
    public static void UpdateValues(string filePath, Dictionary<string, string> values)
    {
        var lines = File.ReadAllLines(filePath).ToList();
        var output = new List<string>();
        var keysWritten = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var line in lines)
        {
            int eqIdx = line.IndexOf('=');
            if (eqIdx > 0 && !line.TrimStart().StartsWith(';') && !line.TrimStart().StartsWith('#'))
            {
                string key = line[..eqIdx].Trim();
                if (values.TryGetValue(key, out string? newVal))
                {
                    output.Add($"{key} = {newVal}");
                    keysWritten.Add(key);
                    continue;
                }
            }
            output.Add(line);
        }

        File.WriteAllLines(filePath, output);
    }
}
