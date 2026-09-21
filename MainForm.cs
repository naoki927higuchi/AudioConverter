namespace AudioConverter;

internal sealed class MainForm : Form
{
    private readonly ListBox files = new() { Dock = DockStyle.Fill, SelectionMode = SelectionMode.MultiExtended, HorizontalScrollbar = true, IntegralHeight = false };
    private readonly ComboBox bitrate = Combo(Settings.Bitrates.Select(x => $"{x} kbps").ToArray());
    private readonly ComboBox channels = Combo(["元のまま", "Mono", "Stereo"]);
    private readonly ComboBox sampleRate = Combo(["元のまま", "44.1 kHz", "48 kHz"]);
    private readonly CheckBox normalize = new() { Text = "音量ノーマライズ", AutoSize = true };
    private readonly CheckBox delete = new() { Text = "変換後に元WAVを削除（正常生成・検証後のみ）", AutoSize = true };
    private readonly RadioButton sameFolder = new() { Text = "元ファイルと同じフォルダ", AutoSize = true };
    private readonly RadioButton customFolder = new() { Text = "指定フォルダ", AutoSize = true };
    private readonly TextBox outputFolder = new() { Width = 440 };
    private readonly Button browse = new() { Text = "参照…", AutoSize = true };
    private readonly Button convert = new() { Text = "MP3に変換", AutoSize = true, Height = 34 };
    private readonly Button cancel = new() { Text = "中止", AutoSize = true, Enabled = false };
    private readonly Label status = new() { Text = "WAVを追加、またはここへドラッグ＆ドロップ", Dock = DockStyle.Fill, AutoEllipsis = true, TextAlign = ContentAlignment.MiddleLeft };
    private readonly ProgressBar progress = new() { Dock = DockStyle.Fill };
    private readonly TextBox details = new() { Dock = DockStyle.Fill, Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Both, WordWrap = false };
    private readonly TableLayoutPanel inputPanel = new() { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 6, Margin = Padding.Empty };
    private readonly SplitContainer split = new()
    {
        Dock = DockStyle.Fill, Orientation = Orientation.Horizontal, FixedPanel = FixedPanel.Panel1,
        Size = new Size(612, 622), SplitterWidth = 8, SplitterDistance = 180,
        Panel1MinSize = 70, Panel2MinSize = 350, TabStop = true,
        AccessibleName = "WAV一覧の高さ（上下にドラッグ）"
    };
    private readonly WindowPlacement? savedWindow;
    private CancellationTokenSource? cancellation;
    private bool closingAfterCancel;

    public MainForm(IEnumerable<string> inputs)
    {
        Text = "AudioConverter — WAV → MP3";
        Font = new Font("Yu Gothic UI", 9F);
        AutoScaleMode = AutoScaleMode.Dpi;
        ClientSize = new Size(640, 650);
        MinimumSize = new Size(600, 640);
        StartPosition = FormStartPosition.CenterScreen;
        Icon = Icon.ExtractAssociatedIcon(Environment.ProcessPath!);
        AllowDrop = true;
        files.AccessibleName = "変換対象WAV";
        bitrate.AccessibleName = "ビットレート";
        channels.AccessibleName = "チャンネル";
        sampleRate.AccessibleName = "サンプリングレート";
        outputFolder.AccessibleName = "指定出力フォルダ";
        details.AccessibleName = "変換結果・エラー詳細";
        var frame = new Panel { Dock = DockStyle.Fill, Padding = new Padding(14) };
        Controls.Add(frame);
        frame.Controls.Add(split);
        split.BackColor = SystemColors.ControlDark;
        split.Panel1.BackColor = split.Panel2.BackColor = SystemColors.Control;
        var listPanel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2 };
        listPanel.RowStyles.Add(new(SizeType.Absolute, 26));
        listPanel.RowStyles.Add(new(SizeType.Percent, 100));
        listPanel.Controls.Add(new Label { Text = "変換対象 WAV", AutoSize = true }, 0, 0);
        listPanel.Controls.Add(files, 0, 1);
        split.Panel1.Controls.Add(listPanel);
        var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 5 };
        root.RowStyles.Add(new(SizeType.Absolute, 206));
        root.RowStyles.Add(new(SizeType.Absolute, 40));
        root.RowStyles.Add(new(SizeType.Absolute, 32));
        root.RowStyles.Add(new(SizeType.Absolute, 18));
        root.RowStyles.Add(new(SizeType.Percent, 100));
        split.Panel2.Controls.Add(root);
        root.Controls.Add(inputPanel, 0, 0);
        foreach (var height in new[] { 38, 26, 38, 34, 38, 32 })
            inputPanel.RowStyles.Add(new(SizeType.Absolute, height));
        var fileActions = Flow();
        fileActions.Controls.Add(Button("ファイル追加…", (_, _) => AddFromDialog()));
        fileActions.Controls.Add(Button("選択削除", (_, _) => { foreach (var item in files.SelectedItems.Cast<string>().ToArray()) files.Items.Remove(item); }));
        fileActions.Controls.Add(Button("全削除", (_, _) => files.Items.Clear()));
        inputPanel.Controls.Add(fileActions, 0, 0);
        var labels = Flow();
        foreach (var label in new[] { "ビットレート", "チャンネル", "サンプリングレート" }) labels.Controls.Add(new Label { Text = label, Width = 170 });
        inputPanel.Controls.Add(labels, 0, 1);
        var options = Flow(); options.Controls.AddRange([bitrate, channels, sampleRate]);
        inputPanel.Controls.Add(options, 0, 2);
        var destination = Flow(); destination.Controls.AddRange([sameFolder, customFolder]);
        inputPanel.Controls.Add(destination, 0, 3);
        var folder = Flow(); folder.Controls.AddRange([outputFolder, browse]);
        inputPanel.Controls.Add(folder, 0, 4);
        var checks = Flow(); checks.Controls.AddRange([normalize, delete]);
        inputPanel.Controls.Add(checks, 0, 5);
        var actions = Flow(); actions.FlowDirection = FlowDirection.RightToLeft; actions.Controls.AddRange([convert, cancel]);
        root.Controls.Add(actions, 0, 1); root.Controls.Add(status, 0, 2); root.Controls.Add(progress, 0, 3); root.Controls.Add(details, 0, 4);
        var settings = Settings.Load(out var warning);
        savedWindow = settings.Window;
        bitrate.SelectedIndex = Array.IndexOf(Settings.Bitrates, settings.Bitrate);
        channels.SelectedIndex = settings.Channels;
        sampleRate.SelectedIndex = settings.SampleRate == 44100 ? 1 : settings.SampleRate == 48000 ? 2 : 0;
        normalize.Checked = settings.Normalize; delete.Checked = settings.DeleteSource;
        outputFolder.Text = settings.OutputFolder;
        sameFolder.Checked = !settings.CustomOutput; customFolder.Checked = settings.CustomOutput;
        customFolder.CheckedChanged += (_, _) => UpdateFolder(); UpdateFolder();
        browse.Click += (_, _) => { using var dialog = new FolderBrowserDialog { Description = "MP3の出力先", UseDescriptionForTitle = true }; if (dialog.ShowDialog(this) == DialogResult.OK) outputFolder.Text = dialog.SelectedPath; };
        convert.Click += async (_, _) => await StartConversion();
        cancel.Click += (_, _) => cancellation?.Cancel();
        DragEnter += (_, e) => e.Effect = cancellation is null && e.Data?.GetDataPresent(DataFormats.FileDrop) == true ? DragDropEffects.Copy : DragDropEffects.None;
        DragDrop += (_, e) => { if (cancellation is null && e.Data?.GetData(DataFormats.FileDrop) is string[] paths) AddFiles(paths); };
        FormClosing += OnClosing;
        AddFiles(inputs);
        if (warning is not null) details.Text = warning;
    }
    private static ComboBox Combo(string[] choices)
    {
        var combo = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 170 };
        combo.Items.AddRange(choices); return combo;
    }
    private static FlowLayoutPanel Flow() => new() { Dock = DockStyle.Fill, WrapContents = false, Margin = Padding.Empty };
    private static Button Button(string text, EventHandler click) { var button = new Button { Text = text, AutoSize = true }; button.Click += click; return button; }
    private void UpdateFolder() => outputFolder.Enabled = browse.Enabled = customFolder.Checked;
    private void AddFromDialog()
    {
        using var dialog = new OpenFileDialog { Filter = "WAVファイル (*.wav)|*.wav", Multiselect = true, CheckFileExists = true };
        if (dialog.ShowDialog(this) == DialogResult.OK) AddFiles(dialog.FileNames);
    }
    private void AddFiles(IEnumerable<string> paths)
    {
        int ignored = 0;
        foreach (var path in paths)
        {
            if (!File.Exists(path) || !Path.GetExtension(path).Equals(".wav", StringComparison.OrdinalIgnoreCase)) { ignored++; continue; }
            var full = Path.GetFullPath(path);
            if (!files.Items.Cast<string>().Contains(full, StringComparer.OrdinalIgnoreCase)) files.Items.Add(full);
        }
        status.Text = $"{files.Items.Count} 件のWAV" + (ignored > 0 ? $"（対象外・存在しないファイル {ignored} 件を除外）" : " — 追加またはドラッグ＆ドロップ");
    }
    private Settings ReadSettings() => new()
    {
        Bitrate = Settings.Bitrates[bitrate.SelectedIndex], Channels = channels.SelectedIndex,
        SampleRate = sampleRate.SelectedIndex == 1 ? 44100 : sampleRate.SelectedIndex == 2 ? 48000 : 0,
        Normalize = normalize.Checked, DeleteSource = delete.Checked, CustomOutput = customFolder.Checked, OutputFolder = outputFolder.Text.Trim(),
        Window = CaptureWindow()
    };
    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        if (savedWindow is { Width: > 0, Height: > 0 } placement)
        {
            var area = Screen.FromRectangle(new Rectangle(placement.X, placement.Y, placement.Width, placement.Height)).WorkingArea;
            var scale = DeviceDpi / (double)Math.Clamp(placement.Dpi, 48, 768);
            int width = (int)Math.Clamp(placement.Width * scale, MinimumSize.Width, Math.Max(MinimumSize.Width, area.Width));
            int height = (int)Math.Clamp(placement.Height * scale, MinimumSize.Height, Math.Max(MinimumSize.Height, area.Height));
            StartPosition = FormStartPosition.Manual;
            Bounds = new Rectangle(Math.Clamp(placement.X, area.Left, Math.Max(area.Left, area.Right - width)),
                Math.Clamp(placement.Y, area.Top, Math.Max(area.Top, area.Bottom - height)), width, height);
        }
        int desired = savedWindow is null ? split.SplitterDistance
            : (int)(Math.Clamp(savedWindow.InputHeight, 70, 4000) * (DeviceDpi / 96.0));
        split.SplitterDistance = Math.Clamp(desired, split.Panel1MinSize, Math.Max(split.Panel1MinSize, split.Height - split.Panel2MinSize - split.SplitterWidth));
        if (savedWindow?.Maximized == true) WindowState = FormWindowState.Maximized;
    }
    private WindowPlacement CaptureWindow()
    {
        var bounds = WindowState == FormWindowState.Normal ? Bounds : RestoreBounds;
        return new() { X = bounds.X, Y = bounds.Y, Width = bounds.Width, Height = bounds.Height,
            Dpi = DeviceDpi, Maximized = WindowState == FormWindowState.Maximized,
            InputHeight = (int)Math.Round(split.SplitterDistance * 96.0 / DeviceDpi) };
    }
    private async Task StartConversion()
    {
        if (files.Items.Count == 0) { status.Text = "変換するWAVを追加してください。"; return; }
        var settings = ReadSettings();
        if (settings.CustomOutput && (string.IsNullOrWhiteSpace(settings.OutputFolder) || !Path.IsPathFullyQualified(settings.OutputFolder))) { status.Text = "出力先の絶対パスを指定してください。"; return; }
        try
        {
            var converter = new Converter(Converter.FindFFmpeg());
            settings.Save();
            cancellation = new(); files.Enabled = inputPanel.Enabled = convert.Enabled = false; cancel.Enabled = true; details.Clear();
            var input = files.Items.Cast<string>().ToArray();
            progress.Maximum = input.Length; progress.Value = 0;
            var reporter = new Progress<BatchProgress>(p =>
            {
                progress.Value = p.Completed;
                status.Text = $"{p.Completed}/{p.Total} 件　成功 {p.Succeeded}／失敗 {p.Failed}／スキップ {p.Skipped}　{p.Current}";
            });
            var result = await converter.ConvertAsync(input, settings, AskCollision, reporter, cancellation.Token);
            // Flush queued progress updates before writing the final summary.
            await Task.Yield();
            int successes = result.Count(x => x.Status.StartsWith("成功"));
            int failures = result.Count(x => x.Status == "失敗");
            int skips = result.Count(x => x.Status == "スキップ");
            bool stopped = cancellation.IsCancellationRequested || result.Count < input.Length || result.Any(x => x.Status == "中止");
            status.Text = $"{(stopped ? "中止" : "完了")}：成功 {successes}／失敗 {failures}／スキップ {skips}／未完了 {input.Length - successes - failures - skips} 件";
            details.Text = string.Join(Environment.NewLine + Environment.NewLine, result.Select(x => $"[{x.Status}] {x.Input}\r\n→ {x.Output}" + (x.Error is null ? "" : "\r\n" + x.Error)));
            foreach (var item in result.Where(x => x.Status.StartsWith("成功"))) files.Items.Remove(item.Input);
        }
        catch (Exception ex) { status.Text = "変換を開始できませんでした。"; details.Text = ex.Message; }
        finally
        {
            cancellation?.Dispose(); cancellation = null; files.Enabled = inputPanel.Enabled = convert.Enabled = true; cancel.Enabled = false;
            if (closingAfterCancel) Close();
        }
    }
    private CollisionDecision AskCollision(string path)
    {
        using var dialog = new Form { Text = "同名MP3が存在します", Font = Font, ClientSize = new Size(510, 185), FormBorderStyle = FormBorderStyle.FixedDialog, StartPosition = FormStartPosition.CenterParent, MaximizeBox = false, MinimizeBox = false };
        var name = new TextBox { Text = path, ReadOnly = true, Multiline = true, Bounds = new Rectangle(16, 16, 478, 65) };
        var all = new CheckBox { Text = "以降すべて同じ処理", AutoSize = true, Location = new Point(16, 92) };
        var overwrite = new Button { Text = "上書き", DialogResult = DialogResult.Yes, Bounds = new Rectangle(180, 133, 100, 32) };
        var skip = new Button { Text = "スキップ", DialogResult = DialogResult.No, Bounds = new Rectangle(287, 133, 100, 32) };
        var stop = new Button { Text = "キャンセル", DialogResult = DialogResult.Cancel, Bounds = new Rectangle(394, 133, 100, 32) };
        dialog.Controls.AddRange([name, all, overwrite, skip, stop]); dialog.CancelButton = stop; dialog.AcceptButton = skip;
        var answer = dialog.ShowDialog(this);
        return new(answer == DialogResult.Yes ? CollisionChoice.Overwrite : answer == DialogResult.No ? CollisionChoice.Skip : CollisionChoice.Cancel, all.Checked);
    }
    private void OnClosing(object? sender, FormClosingEventArgs e)
    {
        if (cancellation is not null) { closingAfterCancel = true; cancellation.Cancel(); e.Cancel = true; return; }
        try { ReadSettings().Save(); }
        catch (Exception ex) { MessageBox.Show(this, "設定を保存できませんでした。\n" + ex.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Warning); }
    }
}
