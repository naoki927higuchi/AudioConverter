using System.Diagnostics;
using System.Text;

namespace AudioConverter;

public enum CollisionChoice { Overwrite, Skip, Cancel }
public sealed record CollisionDecision(CollisionChoice Choice, bool ApplyToAll = false);
public sealed record ConversionResult(string Input, string Output, string Status, string? Error = null);
public sealed record BatchProgress(int Completed, int Total, int Succeeded, int Failed, int Skipped, string Current);

public sealed class Converter(string ffmpeg)
{
    public static string FindFFmpeg()
    {
        foreach (var path in new[] { Path.Combine(AppContext.BaseDirectory, "ffmpeg", "ffmpeg.exe"), Path.Combine(AppContext.BaseDirectory, "ffmpeg.exe") })
            if (File.Exists(path)) return path;
        foreach (var folder in (Environment.GetEnvironmentVariable("PATH") ?? "").Split(Path.PathSeparator))
        {
            if (string.IsNullOrWhiteSpace(folder)) continue;
            var path = Path.Combine(folder.Trim('"'), "ffmpeg.exe");
            if (File.Exists(path)) return path;
        }
        throw new FileNotFoundException("FFmpegが見つかりません。配布物を再展開するか、開発時はPATHにffmpeg.exeを追加してください。");
    }

    public static List<string> BuildArguments(string input, string output, Settings settings)
    {
        List<string> args = ["-hide_banner", "-nostdin", "-loglevel", "error", "-xerror", "-n", "-i", input, "-map", "0:a:0", "-vn", "-c:a", "libmp3lame", "-b:a", $"{settings.Bitrate}k"];
        if (settings.Channels != 0) args.AddRange(["-ac", settings.Channels.ToString()]);
        if (settings.SampleRate != 0) args.AddRange(["-ar", settings.SampleRate.ToString()]);
        // FFmpeg's dynamic audio normalizer preserves the input sample rate.
        if (settings.Normalize) args.AddRange(["-af", "dynaudnorm"]);
        args.AddRange(["-f", "mp3", output]);
        return args;
    }

    public async Task<List<ConversionResult>> ConvertAsync(IReadOnlyList<string> inputs, Settings settings,
        Func<string, CollisionDecision> collision, IProgress<BatchProgress>? progress, CancellationToken cancellation)
    {
        var results = new List<ConversionResult>();
        CollisionDecision? remembered = null;
        int success = 0, failed = 0, skipped = 0;
        foreach (var input in inputs)
        {
            if (cancellation.IsCancellationRequested) break;
            progress?.Report(new(results.Count, inputs.Count, success, failed, skipped, Path.GetFileName(input)));
            string output = "", temp = "";
            try
            {
                if (!Path.GetExtension(input).Equals(".wav", StringComparison.OrdinalIgnoreCase)) throw new IOException("WAVファイルを指定してください。");
                output = Path.Combine(settings.CustomOutput ? Path.GetFullPath(settings.OutputFolder) : Path.GetDirectoryName(input)!, Path.GetFileNameWithoutExtension(input) + ".mp3");
                bool overwrite = false;
                if (File.Exists(output))
                {
                    var decision = remembered ?? collision(output);
                    if (decision.ApplyToAll) remembered = decision;
                    if (decision.Choice == CollisionChoice.Cancel) break;
                    if (decision.Choice == CollisionChoice.Skip)
                    {
                        skipped++; results.Add(new(input, output, "スキップ"));
                        continue;
                    }
                    overwrite = true;
                }
                Directory.CreateDirectory(Path.GetDirectoryName(output)!);
                temp = Path.Combine(Path.GetDirectoryName(output)!, $".AudioConverter-{Guid.NewGuid():N}.mp3");
                // Keep the input immutable during conversion and verification.
                using (var source = new FileStream(input, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    await RunAsync(BuildArguments(input, temp, settings), cancellation);
                    if (!File.Exists(temp) || new FileInfo(temp).Length == 0) throw new IOException("MP3が生成されませんでした。");
                    await RunAsync(["-hide_banner", "-nostdin", "-v", "error", "-xerror", "-i", temp, "-map", "0:a:0", "-f", "null", "-"], cancellation);
                    cancellation.ThrowIfCancellationRequested();
                    // A file appearing after the collision check must never be silently replaced.
                    if (overwrite && File.Exists(output)) File.Replace(temp, output, null);
                    else File.Move(temp, output, false);
                }
                string? warning = null;
                if (settings.DeleteSource)
                {
                    try { File.Delete(input); }
                    catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { warning = "変換成功。元WAVの削除に失敗: " + ex.Message; }
                }
                success++;
                results.Add(new(input, output, warning is null ? "成功" : "成功（警告）", warning));
            }
            catch (OperationCanceledException) { results.Add(new(input, output, "中止")); break; }
            catch (Exception ex) { failed++; results.Add(new(input, output, "失敗", ex.Message)); }
            finally
            {
                if (temp.Length > 0 && File.Exists(temp))
                {
                    try { File.Delete(temp); } catch (IOException) { } catch (UnauthorizedAccessException) { }
                }
                progress?.Report(new(results.Count, inputs.Count, success, failed, skipped, ""));
            }
        }
        return results;
    }

    private async Task RunAsync(IEnumerable<string> args, CancellationToken cancellation)
    {
        var start = new ProcessStartInfo(ffmpeg) { UseShellExecute = false, CreateNoWindow = true, RedirectStandardError = true };
        foreach (var arg in args) start.ArgumentList.Add(arg);
        using var process = new Process { StartInfo = start };
        cancellation.ThrowIfCancellationRequested();
        process.Start();
        var errorTask = ReadErrorAsync(process.StandardError);
        try { await process.WaitForExitAsync(cancellation); }
        catch (OperationCanceledException)
        {
            if (!process.HasExited) process.Kill(true);
            await process.WaitForExitAsync(CancellationToken.None);
            await errorTask;
            throw;
        }
        var error = await errorTask;
        if (process.ExitCode != 0) throw new IOException($"FFmpeg終了コード {process.ExitCode}: {error}");
    }
    private static async Task<string> ReadErrorAsync(StreamReader reader)
    {
        var text = new StringBuilder();
        var buffer = new char[2048];
        int read;
        while ((read = await reader.ReadAsync(buffer)) > 0)
        {
            text.Append(buffer, 0, read);
            if (text.Length > 16000) text.Remove(0, text.Length - 16000);
        }
        return text.ToString().Trim();
    }
}
