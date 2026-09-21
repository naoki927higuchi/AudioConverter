# AudioConverter 構成管理

- Semantic Versioningを使用。互換性を破る変更はユーザー指示または明確な設計変更に限る。
- バージョン更新時にHISTORY.mdへ日時と概要を記録し、バージョン更新コミットを作成する。
- csproj、AppxManifest.xml、リリーススクリプトの既定バージョンを一致させる。
- 2.0.0以降はFFmpegを配布ZIP/MSIXに含めない。利用者が別途用意しPATHへ登録する。
- 旧同梱版は W:\dev\local-archives\AudioConverter\1.0.1-ffmpeg-bundled に保管し、Gitへ追加しない。
- build-release.ps1で作成し、verify-release.ps1で検証する。
- tests/AudioConverter.Tests.csprojの実変換テストに外部FFmpegとGit管理外の出力先を指定する。
