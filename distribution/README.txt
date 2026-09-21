AudioConverter 1.0.1 — WAV → MP3

対象: Windows 11 / Intel・AMD x64。.NETとFFmpegの別途導入・PATH設定は不要です。
同じ利用者の所有PC間での個人利用を想定しています。

導入
1. ZIPをローカルフォルダにすべて展開してください。
2. Install.cmdを通常のダブルクリックで実行します。
   アプリ自体は現在のユーザーにインストールされます。
3. 初回の公開証明書登録だけUACによる管理者承認が必要です。
4. WAVを1個または複数選び、右クリックの「MP3に変換」を選びます。
   メニューが反映されない場合はサインアウトして再サインインしてください。
インストール後は展開した配布フォルダを移動・削除できます。
MSIX内のアプリとFFmpegはWindowsが管理する場所に配置されます。

使い方
スタートメニューのAudioConverterから直接起動できます。
開発・展開版のAudioConverter.exeを直接起動しても使用できます。
WAVをファイル追加またはドラッグ＆ドロップで追加し、設定して「MP3に変換」。
初期値: 192 kbps／チャンネル・サンプルレートは元のまま／ノーマライズOFF／
元のフォルダに出力／元WAV削除OFF。最後の設定は次回に復元します。
既存MP3は上書き・スキップ・キャンセルから選びます。
「以降すべて同じ処理」はその変換バッチだけに適用します。
変換結果・エラー詳細は画面下部で確認・コピーできます。
「中止」または変換中のウィンドウを閉じる操作で、実行中のFFmpegを停止します。
完了済みのMP3は残り、処理中の一時MP3は削除されます。

音声と削除に関する注意
libmp3lame、指定ビットレートでエンコードします。
ノーマライズはFFmpeg dynaudnormの既定値による動的音量補正です。
元のままでは-ac/-arを指定しません。MP3が対応しないチャンネル数等は
FFmpegの自動調整または変換エラーになります。厳密な可逆保存ではありません。
元WAV削除ONは、FFmpeg正常終了・空でないMP3・全体のデコード検証・
出力確定の全てが成功した場合にのみ削除します。ごみ箱には移動しません。
削除失敗は成功（警告）として表示し、元WAVを残します。
既存MP3を上書きする場合も、変換失敗・中止なら元のMP3を保持します。

設定
直接起動版: %LOCALAPPDATA%\AudioConverter\settings.json
MSIX版: Windowsのファイル仮想化により通常は
%LOCALAPPDATA%\Packages\Local.AudioConverter_<publisher-id>\LocalCache\Local\AudioConverter\settings.json
に保存されます。入力一覧は保存しません。MSIXアンインストールで設定が消えることがあります。

削除
Uninstall.cmd（またはWindows設定のアプリ一覧）でAudioConverterを削除します。
利用者のWAV/MP3と署名用証明書は削除しません。
直接起動版の設定を消す場合は%LOCALAPPDATA%\AudioConverterを削除します。
証明書を削除する場合はcertlm.mscの「信頼されたユーザー」でAudioConverterを選び、
package-info.jsonのCertificateThumbprintとの一致を確認してください。
同じ証明書で署名した更新版にも影響します。

証明書
個人利用用の自己署名です。秘密鍵は同梱しません。
初回に公開証明書をLocalMachine\TrustedPeopleへ登録します。
開発者モード、SmartScreen、実行ポリシーの永続設定は変更しません。
組織ポリシーで導入が拒否されるPCには、管理者による許可が必要です。
証明書期限はpackage-info.jsonを参照。期限後の新規導入には再署名が必要です。
開発用登録が存在する場合はinstall-modern.ps1 -Uninstallで解除後に導入します。

FFmpeg
Gyan.dev 9.0.1 full static build / GPLv3-or-laterを無改変で同梱。
FFmpeg-LICENSE.txtとFFmpeg-NOTICE.txtを参照してください。
同一利用者のPC間の私的利用を対象とし、第三者向け再配布用の
完全な対応ソース一式は本ZIPには含めていません。

画面位置・サイズ・最大化状態とWAV一覧の高さを次回起動時に復元します。
WAV一覧の直下の灰色の境界を上下にドラッグして一覧とログの高さを調整できます。
ウィンドウを縦に広げた分はログに割り当てます。モニター変更時は画面内へ戻します。

