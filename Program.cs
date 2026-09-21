namespace AudioConverter;

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        ApplicationConfiguration.Initialize();
        try { Application.Run(new MainForm(ReadInputs(args))); }
        catch (Exception ex) { MessageBox.Show(ex.Message, "AudioConverter", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }

    internal static string[] ReadInputs(string[] args)
    {
        if (args.Length != 2 || args[0] != "--selection") return args;
        // Only consume our native extension's per-user, UTF-16 selection files.
        var path = Path.GetFullPath(args[1]);
        var root = Path.Combine(Settings.Folder, "Requests");
        if (!string.Equals(Path.GetDirectoryName(path), root, StringComparison.OrdinalIgnoreCase)
            || !Path.GetFileName(path).StartsWith("selection-", StringComparison.Ordinal)
            || Path.GetExtension(path) != ".txt") throw new IOException("選択ファイルの場所が不正です。");
        try { return File.ReadAllLines(path, System.Text.Encoding.Unicode); }
        finally { File.Delete(path); }
    }
}
