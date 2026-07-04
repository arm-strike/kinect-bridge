# Kinect Tracking Bridge / Unity Integration

## 1. 使用機材

- Kinect for Xbox 360
- Windows PC
- Kinect for Windows SDK 1.8
- Unity プロジェクト側は Kinect SDK / `Microsoft.Kinect.dll` を直接参照しない

## 2. サポート前提

- Xbox 360 Kinect は Kinect for Windows SDK 1.8 の公式サポート対象外
- そのため、センサー認識や Skeleton Stream の開始可否は、環境差や個体差を前提に確認する
- センサーが見つからない場合でも Bridge は即終了せず、再検出を続ける

## 3. `Microsoft.Kinect.dll` の参照方法

- `KinectBridge` プロジェクトは Visual Studio 上で次の DLL を参照する
- 既定の参照候補:
  - `C:\Program Files\Microsoft SDKs\Kinect\v1.8\Assemblies\Microsoft.Kinect.dll`
- 環境によっては `Program Files (x86)` 側にインストールされている場合があるため、その場合は参照先を手動で切り替える

## 4. リポジトリ構成

- `KinectBridge.sln`
- `KinectBridge/`
- `Tracking.Common/`
- `Tracking.Common.Tests/`
- `UnitySample/Assets/Tracking/Runtime/`
- `UnitySample/Assets/Tracking/Debug/`

## 5. Bridge のビルド方法

1. Windows 上で `KinectBridge.sln` を Visual Studio で開く
2. `Tracking.Common` をビルドする
3. `KinectBridge` をビルドする
4. `Tracking.Common.Tests` はコンソールアプリとして実行する

## 6. Bridge の起動方法

- ビルド後、`KinectBridge.exe` を起動する
- 起動時に次をログ出力する
  - `sessionId`
  - UDP 送信先
  - Kinect 接続状態
- `Ctrl+C` で正常終了する

## 7. Bridge の動作

- `frameId` は Bridge プロセス起動時から連番で増加する
- Kinect の切断・再接続では `frameId` はリセットしない
- `timestampMs` は `Stopwatch` による Bridge 起動からの単調増加ミリ秒
- `tracked:false` のフレームでは `joints` と `trackingId` を JSON から省略する
- UDP は `127.0.0.1:5005` 宛てに送信する
- 1 UDP データグラムにつき 1 つの完全な UTF-8 JSON を格納する

## 8. Unity 側の使用方法

- `UnitySample/Assets/Tracking/Runtime/` 配下のスクリプトを Unity プロジェクトに入れる
- `ArmTrackingSourceHost` を Scene の任意の GameObject にアタッチする
- `SourceKind` で `Udp / Keyboard / Replay` を切り替える
- `ArmTrackingDebugVisualizer` を同じ GameObject か別 GameObject に追加し、`sourceHost` を割り当てる
- 既存 Scene や Prefab は編集不要

## 9. Unity の UDP 受信

- `127.0.0.1:5005` で受信する
- Unity のメインスレッドをブロックしない
- JSON パースは Unity のメインスレッドで行う
- 有効パケットの受理時刻で `IsConnected` を判定する
- `tracked:false` と通信断は別扱いにする

## 10. 正規化

- 左右の肩の中点を肩中央にする
- 左右の肩の距離を肩幅として使う
- 肩中央を原点として各関節を引き算する
- 肩幅で割って体格差を減らす
- 肩幅が極端に小さいフレームは正規化失敗として扱う

## 11. テスト

- `Tracking.Common.Tests` は純粋 C# のコンソールアプリ
- `UnityEngine` に依存しないロジックをテストする
- テスト項目:
  - JSON シリアライズ / デシリアライズ
  - 関節名とデータ構造の一致
  - 肩中央基準の座標変換
  - 肩幅による正規化
  - 肘角度の計算
  - 通信タイムアウト判定
  - `tracked:false` の処理

## 12. Windows ファイアウォール

- ローカル loopback (`127.0.0.1`) だけを使う場合は通常影響が小さい
- もし別アドレスに変更する場合は、Windows ファイアウォールの許可設定を確認する

## 13. 本番展示時の起動順序

1. Kinect センサーを PC に接続する
2. Windows で Kinect の各デバイスが `OK` になっていることを確認する
3. `KinectBridge.exe` を起動する
4. Kinect が Connected になり Skeleton Stream が開始されることを確認する
5. Unity アプリを起動する
6. Unity 側で受信状態と関節可視化を確認する

## 14. 現時点で未実装または今後の改善項目

- TrackingId の固定運用
- プレイエリア制限
- 展示用の追跡対象切り替えルールの細分化
- Unity の実 Scene / Prefab への組み込み
- 実機での最終確認ログの取得

## 15. 実装後の確認結果

### ユーザー側で確認済み

- Windows で Kinect が正常認識されている
- 次のデバイスがすべて `OK`
  - Kinect for Windows Camera
  - Kinect for Windows Device
  - Kinect for Windows Audio Array Control
  - Kinect USB Audio

### この作業環境では未確認

- `KinectBridge.exe` の実行
- Kinect SDK から `Connected` としてセンサーを取得できるか
- Skeleton Stream を開始できるか
- 左右の肩・肘・手首・手の座標が取得できるか
- UDP 送信の実動作
- Unity 側の受信動作
- Unity 側の正規化 / 可視化 / 疑似入力

## 16. 補足

- `sessionId` は Bridge 起動ごとに一意な GUID 文字列
- Unity 側は同じ `sessionId` 内で古い `frameId` を破棄する
- Unity 側の通信断判定は、UTF-8 デコード・JSON 解析・プロトコルバージョン確認に成功したパケットの受理時刻で行う
