using AudioConverter;
using System.Diagnostics;
using System.Security.Cryptography;

var ffmpeg = Path.GetFullPath(args[0]);
var root = Path.GetFullPath(args[1]);
Directory.CreateDirectory(root);
var converter = new Converter(ffmpeg);
int assertions = 0;
void Check(bool test, string name) { if (!test) throw new Exception(name); assertions++; Console.WriteLine("PASS " + name); }
// Exercise the public package's external-FFmpeg setup without changing user settings.
var savedPath = Environment.GetEnvironmentVariable("PATH");
try
{
    Environment.SetEnvironmentVariable("PATH", "");
    bool missingExplained = false;
    try { Converter.FindFFmpeg(); }
    catch (FileNotFoundException ex) { missingExplained = ex.Message.Contains("PATH") && ex.Message.Contains("別途導入"); }
    Check(missingExplained, "missing external FFmpeg explains installation and PATH");
    Environment.SetEnvironmentVariable("PATH", Path.GetDirectoryName(ffmpeg));
    Check(Path.GetFullPath(Converter.FindFFmpeg()) == ffmpeg, "find user-provided FFmpeg on PATH");
}
finally { Environment.SetEnvironmentVariable("PATH", savedPath); }
async Task<string> FF(params string[] arguments)
{
    var start = new ProcessStartInfo(ffmpeg) { RedirectStandardError = true, RedirectStandardOutput = true, UseShellExecute = false, CreateNoWindow = true };
    foreach (var arg in arguments) start.ArgumentList.Add(arg);
    using var p = Process.Start(start)!;
    var error = p.StandardError.ReadToEndAsync(); var output = p.StandardOutput.ReadToEndAsync();
    await p.WaitForExitAsync();
    var text = await error + await output;
    if (p.ExitCode != 0) throw new Exception(text);
    return text;
}
async Task<string> Wav(string name, int rate = 44100, int channels = 2, int seconds = 1)
{
    var path = Path.Combine(root, name + ".wav");
    await FF("-hide_banner", "-loglevel", "error", "-y", "-f", "lavfi", "-i", $"sine=frequency=440:duration={seconds}:sample_rate={rate}", "-ac", channels.ToString(), path);
    return path;
}
async Task<List<ConversionResult>> Convert(string[] paths, Settings? settings = null, Func<string, CollisionDecision>? collision = null, CancellationToken token = default)
    => await converter.ConvertAsync(paths, settings ?? new(), collision ?? (_ => new(CollisionChoice.Overwrite)), null, token);
var original = await Wav("日本語 空白 & 音声");
var defaults = Converter.BuildArguments(original, "out.mp3", new());
Check(!defaults.Contains("-ar") && !defaults.Contains("-ac") && !defaults.Contains("-af"), "original settings omit channel/rate/filter options");
foreach (var rate in Settings.Bitrates)
{
    var result = await Convert([original], new() { Bitrate = rate });
    Check(result.Single().Status == "成功", $"real MP3 conversion {rate} kbps");
    var probe = await FF("-hide_banner", "-i", result[0].Output, "-f", "null", "-");
    Check(probe.Contains($"{rate} kb/s") && probe.Contains("44100 Hz") && probe.Contains("stereo"), $"MP3 stream properties {rate} kbps");
}
foreach (var rate in new[] { 44100, 48000 })
foreach (var channel in new[] { 1, 2 })
{
    var result = await Convert([original], new() { SampleRate = rate, Channels = channel, Normalize = true });
    Check(result[0].Status == "成功", $"normalize {rate} Hz {channel}ch");
    var probe = await FF("-hide_banner", "-i", result[0].Output, "-f", "null", "-");
    Check(probe.Contains($"{rate} Hz") && probe.Contains(channel == 1 ? "mono" : "stereo"), "requested stream properties");
}
var mono = await Wav("mono48", 48000, 1);
var preserved = await Convert([mono], new() { Normalize = true });
var monoProbe = await FF("-hide_banner", "-i", preserved[0].Output, "-f", "null", "-");
Check(monoProbe.Contains("48000 Hz") && monoProbe.Contains("mono"), "normalize preserves original sample rate/channel");
string Hash(string path) => ConvertHex(SHA256.HashData(File.ReadAllBytes(path)));
string ConvertHex(byte[] bytes) => System.Convert.ToHexString(bytes);
var existing = Path.ChangeExtension(original, ".mp3"); var before = Hash(existing);
var skipped = await Convert([original], new() { DeleteSource = true }, _ => new(CollisionChoice.Skip));
Check(skipped[0].Status == "スキップ" && Hash(existing) == before && File.Exists(original), "skip preserves MP3 and WAV");
int prompts = 0;
var skipAll = await Convert([original, mono], collision: _ => { prompts++; return new(CollisionChoice.Skip, true); });
Check(prompts == 1 && skipAll.All(x => x.Status == "スキップ"), "apply-to-all collision decision");
var cancelCollision = await Convert([original, mono], collision: _ => new(CollisionChoice.Cancel));
Check(cancelCollision.Count == 0 && Hash(existing) == before, "collision cancel stops batch");
var bad = Path.Combine(root, "broken.wav"); File.WriteAllText(bad, "not audio");
var badMp3 = Path.ChangeExtension(bad, ".mp3"); File.WriteAllText(badMp3, "existing file must survive");
var badHash = Hash(badMp3);
var badResult = await Convert([bad, original], new() { DeleteSource = true });
Check(badResult[0].Status == "失敗" && File.Exists(bad) && Hash(badMp3) == badHash, "failed conversion preserves source and old output");
Check(badResult[1].Status == "成功" && !File.Exists(original), "batch continues; successful verified conversion deletes WAV");
var custom = Path.Combine(root, "custom destination");
var customResult = await Convert([mono], new() { CustomOutput = true, OutputFolder = custom });
Check(File.Exists(Path.Combine(custom, "mono48.mp3")) && customResult[0].Status == "成功", "custom output folder created");
var longWav = await Wav("cancel", seconds: 120);
using (var cts = new CancellationTokenSource())
{
    cts.CancelAfter(25);
    var cancelled = await Convert([longWav], new() { Normalize = true, DeleteSource = true }, token: cts.Token);
    Check(cancelled.Count <= 1 && !File.Exists(Path.ChangeExtension(longWav, ".mp3")) && File.Exists(longWav), "cancellation preserves input and leaves no MP3");
}
var invalidSettings = new Settings { Bitrate = 1, Channels = 99, SampleRate = 1 }; invalidSettings.Validate();
Check(invalidSettings.Bitrate == 192 && invalidSettings.Channels == 0 && invalidSettings.SampleRate == 0, "invalid saved settings fall back to defaults");
Check(!Directory.EnumerateFiles(root, ".AudioConverter-*.mp3", SearchOption.AllDirectories).Any(), "no temporary output remains");
Console.WriteLine($"All {assertions} assertions passed.");
