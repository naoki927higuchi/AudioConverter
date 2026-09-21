# 検証結果 — 2026-09-18

環境: Windows 11 build 26200 / x64、.NET SDK 9.0.305、Visual Studio 2022 C++、Windows SDK 10.0.26100.0。

- .NET Releaseビルド: 警告0、エラー0。
- ネイティブDLL: C++17 /MTでビルド成功。
- 実FFmpeg 9.0.1によるテスト: 31 assertion成功。
  全6ビットレート、Mono/Stereo、44.1/48 kHz、ノーマライズと元音声設定の保持、
  日本語・空白入りパス、一括衝突判断、スキップ・キャンセル、破損WAV、
  失敗後のバッチ継続、失敗時の元WAVと既存MP3保持、成功後の元WAV削除、
  出力先作成、中止時の一時ファイル除去を検証。
- 署名済みMSIXを配布Install.ps1でこのPCに導入。
  初回の証明書信頼登録は利用者がUAC承認。
  Local.AudioConverter 1.0.0.0 / Status=Ok。
- 登録済みCOMをCoCreateInstanceで起動し、タイトル一致、単一WAV・複数WAVで有効、
  WAVとREADME混在で非表示を確認。
- COM Invokeで2つのWAVを1つの設定画面に投入。画面上でも2件を確認。
- MSIXインストール先の同梱FFmpegを使い、GUI操作で2件とも変換成功。
  元WAV2件は保持、生成MP3は各49,571 bytes。
- GUIの初期値、上書き／スキップ／キャンセル＋以降すべて同じ処理のダイアログ、
  スキップ結果表示、成功件数・進捗・結果一覧を実画面で確認。
- ExplorerのWAV右クリックで「MP3に変換」がWindows 11モダンメニューに直接表示されることを確認。
  実際にメニューを選択し、そのWAVが入力一覧に入った設定画面の起動まで確認。
- MSIX版のsettings.jsonをLocalCache/Local/AudioConverterで確認。
  選択受け渡し用Requestsファイルは読込後に残っていない。
- リリース検証: 公開証明書・MSIXのSHA256、署名の暗号学的検証、全MSIXブロック、
  同梱FFmpegの固定SHA256、必須ファイル・manifest、ZIP内容と配布フォルダの一致を確認。
  PFX/秘密鍵・PDB・OBJ等の混入なし。

成果物: release/AudioConverter-1.0.0-x64.zip（155,047,105 bytes）。
本検証で利用者の実WAVは使用せず、生成したテスト用音声のみを使用。
別の物理PCへの導入自体は未実施。FFmpegや.NETを外部に要求しないパッケージ構成と、
当該MSIXからの実動作をこのPC上で検証した。

## 1.0.1 UI更新

- mp3文字＋白いフレームのICO（16～256px）を生成。EXE・ウィンドウ・Explorerメニュー用アイコンとMSIXロゴに適用。
- WAV一覧直下のSplitContainerを実画面でドラッグし、ログ領域の増減を確認。
- 最大化時に一覧の高さを維持し、増えた縦領域がログへ割り当たることを確認。
- 最大化終了→再起動で最大化を復元。通常ウィンドウの移動→終了→再起動→終了で、保存したX/Y/Width/Height/InputHeightの完全一致を確認（144 DPI）。
- Releaseビルド警告0・エラー0。1.0.1の署名、全パッケージハッシュ、ZIP内容検証に成功。
- 本PCのMSIXを1.0.1.0へ更新、Status=Ok。更新前に開いていた入力WAVを画面へ戻した。音声ファイルは変更していない。
