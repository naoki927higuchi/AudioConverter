# AudioConverter

Windows 11のExplorerから、1個または複数のWAVをMP3に変換する個人用アプリ。
.NET 8 WinForms + FFmpeg。Windows 11モダンコンテキストメニューは
ネイティブIExplorerCommandとpackaged COMを使用します。

## 使い方・導入

`release/AudioConverter-1.0.1-x64.zip`を別PCへ展開して`Install.cmd`を実行。
初回だけ署名用公開証明書の信頼登録に管理者承認が必要です。
.NET、FFmpeg、PATH設定は不要です。対象はWindows 11 Intel/AMD x64。
WAVを複数選択してモダン右クリックメニューの「MP3に変換」を選ぶと、
ひとつの設定画面に全選択ファイルを投入します。
反映されない場合はサインアウト・サインインしてください。

スタートメニューのAudioConverter、または`dist/AudioConverter.exe`の直接起動も可能。
「ファイル追加」、ドラッグ＆ドロップ、選択削除、全削除が使えます。
変換後の結果・エラーは画面下部で確認・コピーできます。
成功したファイルは一覧から取り除き、失敗やスキップは再実行用に残します。

設定は192 kbps、チャンネル／サンプルレートは元のまま、元フォルダ出力、
ノーマライズOFF、元WAV削除OFFが初期値。以降は最後の設定を復元します。
同名MP3は上書き・スキップ・キャンセルと、バッチ内の「以降すべて同じ処理」に対応。
「中止」または閉じる操作ではFFmpegを停止して一時ファイルを片付けます。

アンインストールは配布物の`Uninstall.cmd`かWindowsのアプリ設定。
署名証明書と利用者の音声は残します。詳細は`distribution/README.txt`を参照。

## 技術構成・参考実装の調査

`W:\dev\ShareXImageEditorContext`のソース、プロジェクト、全ビルド・検証・導入スクリプト、
manifest、distribution、release内容とpackage-info.jsonを確認しました。
以下を踏襲しました。

- .NET 8 WinForms、自己完結のwin-x64単一EXE発行。
- C++17 / WRL / IExplorerCommand、MSIXのCOMサロゲート登録。
- `dist`、`distribution`、バージョン付き`release`フォルダとZIP。
- 非エクスポート可能な自己署名鍵、公開CERのみ配布、初回TrustedPeople登録。
- Install/Uninstall.cmd、PowerShell、README.txt、ハッシュ付きpackage-info.json。

ShareXの外部アプリ探索や画像ごとの個別起動は流用していません。
今回は独自CLSIDとパッケージIDで.wavに限定し、複数選択はUTF-16応答ファイルで
1プロセスに渡します。Windowsのコマンドライン長制限を回避し、日本語・空白に対応。
DLLは自分自身と同じフォルダのAudioConverter.exeを起動するため絶対配置に依存しません。
応答ファイルは利用者のLocalAppData内のRequestsに作成し、読込後に削除します。

構成: MainForm.cs＝UI、Settings.cs＝JSON、Converter.cs＝変換・FFmpeg、
Program.cs＝起動引数、Command.cpp＝Explorer連携。DIコンテナ等は使用していません。

## ビルド

必要: .NET 8対応SDK（本機は9.0.305）、Visual Studio C++ x64ツール、Windows SDK。
VSはvswhere、SDKはインストール済みパスから検出します。

```powershell
dotnet build .\AudioConverter.sln -c Release
.\prepare-ffmpeg.ps1 -FFmpegRoot 'C:\path\ffmpeg-9.0.1-full_build'
.\publish-modern.ps1
.\dist\AudioConverter.exe
```

FFmpegは同梱`ffmpeg/ffmpeg.exe`→EXE隣接→PATHの順で探索します。
開発時はPATH版も利用可能。初回準備済みの本作業フォルダではprepareの再実行は不要。
FFmpegを新たに準備する場合、[Gyan.dev](https://www.gyan.dev/ffmpeg/builds/)の
9.0.1 full buildを展開してprepareスクリプトへ渡します。EXEのSHA256を検証します。
更新時はビルドのライセンスと構成を確認し、vendor/ffmpegの記録も更新してください。

## リリース作成

```powershell
.\build-release.ps1 -Version 1.0.1
powershell -NoProfile -ExecutionPolicy Bypass -File .\verify-release.ps1 -Version 1.0.1
```

毎回ビルド・自己完結発行・FFmpeg同梱・MSIX作成・署名・ZIP生成を行います。
出力: `release/AudioConverter-<version>-x64` と同名ZIP。
秘密鍵・PDB・OBJ・テストファイルをMSIX/ZIPへ入れません。
署名鍵はCurrentUser/MyのCN=AudioConverter（非エクスポート可能）。
更新版では同じ有効な証明書を再利用。開発PC変更時は新しい証明書の信頼登録が必要です。
証明書を含む署名処理はWindows上で実行してください。
`artifacts/payload-*`はそのビルドのステージングとして残します。

開発用登録（配布用インストールとは別）:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\install-modern.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File .\install-modern.ps1 -Uninstall
```

開発用登録には環境の開発者モード等の条件が必要です。設定は自動変更しません。
実用・別PC導入には署名済みリリースのInstall.cmdを使用してください。

## 音声処理・安全性

- libmp3lame、指定ビットレート。元のままでは-ac/-arを省略。
- ノーマライズは一般的なFFmpeg `dynaudnorm`の既定値による動的補正。
  サンプルレートを変えず、固定LUFS目標のマスタリングではありません。
- 一時MP3に変換→FFmpeg終了コード0→サイズ確認→全体をデコード検証→出力確定。
- 失敗・中止は元WAVと既存MP3を保持。出力確定後だけ元WAV削除。
  元WAV削除はごみ箱に入りません。削除できなかった場合は成功（警告）表示。
- 新規出力の衝突チェック後に同名ファイルが現れた場合は上書きせず失敗扱い。
- MP3非対応の高サンプルレート／多チャンネルはFFmpegの調整または失敗になります。
  厳密な元音声の保持が必要な場合はWAVを残してください。

## 設定保存場所

直接起動版: `%LOCALAPPDATA%\AudioConverter\settings.json`。
MSIX版は通常Windowsのファイル仮想化により
`%LOCALAPPDATA%\Packages\Local.AudioConverter_<publisher-id>\LocalCache\Local\AudioConverter\settings.json`。
入力一覧は保存しません。破損JSONは警告を表示し初期値を使います。
MSIX削除でパッケージ側の設定が消えることがあります。

## FFmpegのライセンス

Gyan.devの9.0.1 full static GPLv3-or-later版を外部プロセスとして無改変で使用。
libmp3lameが有効で、nonfree構成ではないことを確認しました。
ライセンス本文・上流README・バージョン／SHA256／ソース参照を同梱しています。
今回の完成物は同じ利用者が自分のPCへコピーする私的利用のためのものです。
第三者へ配布する場合の完全な対応ソース（依存ライブラリやビルドスクリプトを含む）
一式は本ZIPに含めていません。その用途に広げる場合は別途GPLの提供条件を満たす必要があります。

根拠:
[FFmpeg license](https://ffmpeg.org/legal.html)、
[ビルド提供元](https://www.gyan.dev/ffmpeg/builds/)、
[GNU private-use FAQ](https://www.gnu.org/licenses/gpl-faq.html#GPLRequireSourcePostedPublic)、
[Microsoft Explorer統合](https://learn.microsoft.com/en-us/windows/apps/desktop/modernize/integrate-packaged-app-with-file-explorer)、
[dynaudnorm](https://ffmpeg.org/ffmpeg-filters.html#dynaudnorm)。

## 検証

今回の実機検証結果は[VERIFICATION.md](VERIFICATION.md)を参照してください。

```powershell
dotnet run --project .\tests\AudioConverter.Tests.csproj -c Release -- .\vendor\ffmpeg\ffmpeg.exe .\artifacts\conversion-tests
.\verify.cmd .\artifacts\ui-test-1.wav .\artifacts\ui-test-2.wav .\README.md --invoke
```

音声テストは生成したWAVのみを使用。全ビットレート・チャンネル・レート・ノーマライズ、
スキップ・キャンセル・一括衝突判断、失敗時保護、正常時削除、出力先を検証します。
native検証は登録済みCOMの起動、WAV単一／複数で表示、混在選択で非表示を検査します。
`--invoke`は2つのWAVを実際にアプリへ投入します。
リリース検証は署名の暗号学的検証、全MSIXブロックのハッシュ、ZIP内の秘密鍵混入を検査します。

画面位置・サイズ・最大化状態とWAV一覧の高さを次回起動時に復元します。
WAV一覧の直下の灰色の境界を上下にドラッグして一覧とログの高さを調整できます。
ウィンドウを縦に広げた分はログに割り当てます。モニター変更時は画面内へ戻します。

