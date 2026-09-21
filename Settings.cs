using System.Text.Json;

namespace AudioConverter;

public sealed class Settings
{
    public int Bitrate { get; set; } = 192;
    public int Channels { get; set; }
    public int SampleRate { get; set; }
    public bool Normalize { get; set; }
    public bool CustomOutput { get; set; }
    public string OutputFolder { get; set; } = "";
    public bool DeleteSource { get; set; }
    public WindowPlacement? Window { get; set; }
    public static readonly int[] Bitrates = [64, 96, 128, 192, 256, 320];
    public static string Folder => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AudioConverter");
    public static string FilePath => Path.Combine(Folder, "settings.json");
    public void Validate()
    {
        if (!Bitrates.Contains(Bitrate)) Bitrate = 192;
        if (Channels is < 0 or > 2) Channels = 0;
        if (SampleRate is not (0 or 44100 or 48000)) SampleRate = 0;
        OutputFolder ??= "";
    }
    public static Settings Load(out string? warning)
    {
        warning = null;
        try
        {
            var settings = File.Exists(FilePath) ? JsonSerializer.Deserialize<Settings>(File.ReadAllText(FilePath)) ?? new() : new Settings();
            settings.Validate();
            return settings;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            warning = "設定を読み込めなかったため初期値を使用します: " + ex.Message;
            return new();
        }
    }
    public void Save()
    {
        Directory.CreateDirectory(Folder);
        var temp = Path.Combine(Folder, Guid.NewGuid() + ".tmp");
        try
        {
            File.WriteAllText(temp, JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true }));
            File.Move(temp, FilePath, true);
        }
        finally { if (File.Exists(temp)) File.Delete(temp); }
    }
}

public sealed class WindowPlacement
{
    public int X { get; set; }
    public int Y { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public int Dpi { get; set; } = 96;
    public bool Maximized { get; set; }
    public int InputHeight { get; set; } = 180;
}
