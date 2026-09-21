# AudioConverter 構成管理

- Semantic Versioningを使用。互換性を破る変更はユーザー指示または明確な設計変更に限る。
- バージョン更新時にHISTORY.mdへ日時と概要を記録し、バージョン更新コミットを作成する。
- csproj、AppxManifest.xml、リリーススクリプトの既定バージョンを一致させる。
- 2.0.0以降はFFmpegを配布ZIP/MSIXに含めない。利用者が別途用意しPATHへ登録する。
- 旧同梱版は W:\dev\local-archives\AudioConverter\1.0.1-ffmpeg-bundled に保管し、Gitへ追加しない。
- build-release.ps1で作成し、verify-release.ps1で検証する。
- tests/AudioConverter.Tests.csprojの実変換テストに外部FFmpegとGit管理外の出力先を指定する。

## Windowsアプリ・ゲームの共通配布規則

- この共通規則はAF-SDR、AudioConverter、Breakout3D、Minesweeper2D、NekoDungeon、ShareXImageEditorContextだけに適用する。OreComic等には適用しない。
- ローカル開発中のZIPはGit管理対象外。公開時に選定・検証したZIPだけを配布物としてコミット・pushする。
- 通常のビルドで公開済みZIPを更新しない。開発ZIPを一括でGit追加しない。
- 公開済みの同じ版数への上書きを禁止。公開済み製品の版数を初版へ戻さない。
- 各製品の検証を完了後、Prepare-Release.ps1で明示的に選定する。公開履歴を記録してからコミット・pushする。
- 選定後にゲーム・アプリのソースを変更した場合は配布物との対応を確認し直す。
- 詳細とローカル出力先はRELEASE-POLICY.mdを参照。規約・配布手順だけの変更で既存バイナリの版数を変更しない。
