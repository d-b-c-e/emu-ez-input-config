namespace EmuEzInputConfig.Tests.Util;

using EmuEzInputConfig.Util;

public class IniEditorTests : IDisposable
{
    private readonly string _tempDir;

    public IniEditorTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"EmuEzTests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    private string CreateTempIni(string content)
    {
        string path = Path.Combine(_tempDir, $"{Guid.NewGuid():N}.ini");
        File.WriteAllText(path, content);
        return path;
    }

    [Fact]
    public void UpdateSection_ExistingKey_ValueUpdated()
    {
        string path = CreateTempIni("[Pad1]\nType = AnalogController\nDeadzone = 0.10\n");
        IniEditor.UpdateSection(path, "Pad1", new Dictionary<string, string>
        {
            ["Type"] = "DualShock2",
            ["Deadzone"] = "0",
        });

        string result = File.ReadAllText(path);
        Assert.Contains("Type = DualShock2", result);
        Assert.Contains("Deadzone = 0", result);
        Assert.DoesNotContain("AnalogController", result);
    }

    [Fact]
    public void UpdateSection_MissingKey_AppendedBeforeNextSection()
    {
        string path = CreateTempIni("[Pad1]\nType = DualShock2\n\n[InputSources]\nSDL = true\n");
        IniEditor.UpdateSection(path, "Pad1", new Dictionary<string, string>
        {
            ["LLeft"] = "DInput-1/-Axis0",
        });

        string result = File.ReadAllText(path);
        Assert.Contains("LLeft = DInput-1/-Axis0", result);
        // LLeft should appear before [InputSources]
        int lleftPos = result.IndexOf("LLeft");
        int inputSourcesPos = result.IndexOf("[InputSources]");
        Assert.True(lleftPos < inputSourcesPos);
    }

    [Fact]
    public void UpdateSection_OtherSections_Preserved()
    {
        string path = CreateTempIni("[General]\nFoo = Bar\n\n[Pad1]\nType = Old\n\n[Audio]\nVolume = 100\n");
        IniEditor.UpdateSection(path, "Pad1", new Dictionary<string, string>
        {
            ["Type"] = "DualShock2",
        });

        string result = File.ReadAllText(path);
        Assert.Contains("Foo = Bar", result);
        Assert.Contains("Volume = 100", result);
        Assert.Contains("Type = DualShock2", result);
    }

    [Fact]
    public void UpdateSection_LastSection_AppendsAtEnd()
    {
        string path = CreateTempIni("[Pad1]\nType = Old\n");
        IniEditor.UpdateSection(path, "Pad1", new Dictionary<string, string>
        {
            ["Type"] = "DualShock2",
            ["NewKey"] = "NewValue",
        });

        string result = File.ReadAllText(path);
        Assert.Contains("Type = DualShock2", result);
        Assert.Contains("NewKey = NewValue", result);
    }

    [Fact]
    public void UpdateValues_FlatFormat_KeyUpdated()
    {
        string path = CreateTempIni("InputSteering = NONE\nInputAccelerator = NONE\n; comment line\n");
        IniEditor.UpdateValues(path, new Dictionary<string, string>
        {
            ["InputSteering"] = "JOY2_XAXIS",
            ["InputAccelerator"] = "JOY2_ZAXIS_POS",
        });

        string result = File.ReadAllText(path);
        Assert.Contains("InputSteering = JOY2_XAXIS", result);
        Assert.Contains("InputAccelerator = JOY2_ZAXIS_POS", result);
        Assert.Contains("; comment line", result);  // Comments preserved
    }

    [Fact]
    public void UpdateValues_CommentLines_Preserved()
    {
        string path = CreateTempIni("; This is a comment\n# This too\nKey = Value\n");
        IniEditor.UpdateValues(path, new Dictionary<string, string>
        {
            ["Key"] = "NewValue",
        });

        string result = File.ReadAllText(path);
        Assert.Contains("; This is a comment", result);
        Assert.Contains("# This too", result);
        Assert.Contains("Key = NewValue", result);
    }

    [Fact]
    public void BackupFile_CreatesBackupWithTimestamp()
    {
        string path = CreateTempIni("test content");
        IniEditor.BackupFile(path);

        var backups = Directory.GetFiles(_tempDir, "*.bak.*");
        Assert.Single(backups);
        Assert.Equal("test content", File.ReadAllText(backups[0]));
    }

    [Fact]
    public void BackupFile_NonExistentFile_NoOp()
    {
        string path = Path.Combine(_tempDir, "nonexistent.ini");
        IniEditor.BackupFile(path);  // Should not throw

        var files = Directory.GetFiles(_tempDir);
        Assert.Empty(files);
    }
}
