# Kinect Bridge 信頼性・プロトコル仕様

Status: `ready-for-agent`

Unity 受信・入力・ジェスチャーの正本は[arm-strike-game の仕様](https://github.com/arm-strike/arm-strike-game/blob/feat/kinect-controler/.scratch/arm-strike-docs/spec.md)に置く。この文書は Kinect、人物追跡、UDP/JSON、Bridge 運用の正本であり、ゲーム仕様は扱わない。両仕様の `main` へのマージ時には、相互リンクを両方とも `main` に切り替える。

## Problem Statement

Bridge は Kinect SDK 1.8 の SkeletonFrameReady ごとに UDP JSON を送信するが、現行実装には人物ロック、センサー状態パケット、ストリーム監視、起動完了通知、送信直列化がない。Kinect の切断・再初期化・人物入れ替わりを Unity が安全に区別できる境界を定める必要がある。

## Current Implementation

- .NET 4.8 のコンソール Bridge が Kinect SDK 1.8 を使い、`127.0.0.1:5005` へ SkeletonFrameReady 単位で UDP JSON を送る。
- `sessionId` は Bridge 起動時に生成され、`frameId` は連番、`timestampMs` は Bridge 起動後の単調増加時刻として送られる。人物がいないフレームは `tracked:false` だが、センサー切断時には状態パケットを送らない。
- 最も近い fully-tracked skeleton を各フレームで選ぶ。Tracking ID の保持、`sensorStatus`、`sensorEpochId`、送信ゲート、Watchdog、Ready ファイル、ローリングログは未実装である。
- 現行 SDK 平滑化値は Smoothing 0.7、Correction 0.3、Prediction 0.5、JitterRadius 0.05、MaxDeviationRadius 0.04。

## Solution

Bridge を Kinect 状態の唯一の発行者とする。人物ロックとセンサー状態を明示し、順序付けた UDP パケットを継続送信する。Unity はこのプロトコルの必須項目と互換バージョンだけを受理する。

## User Stories

1. 展示スタッフとして、センサーが未接続でも Bridge の起動完了を確認して Unity を安全に起動したい。
2. 利用者として、追跡中の人物が一時的に見失われても、別人へ即座に切り替わらないようにしたい。
3. Unity 側として、Bridge 停止と Kinect の利用不能を通信状態と `sensorStatus` で区別し、どちらでも入力を中立化したい。
4. Unity 側として、再初期化後の骨格ストリームを前の姿勢履歴から切り離したい。
5. 運用者として、フレーム停止・送信失敗・センサーエラーをログから判断したい。
6. Unity 側として、送信順序と `frameId` の欠番を安全に扱いたい。
7. Unity 側として、Kinect利用不可時にも状態パケットを受け取り、Bridge停止と区別したい。
8. 運用者として、Bridgeが自動再接続中であることを保ったまま、人の判断で再起動したい。
9. 運用者として、Kinect障害エピソード中でもBridgeを自動終了・自動再起動させず、原因を調査したい。
10. 実機検証者として、Kinect個体と接続構成が対応環境として適合していることを記録したい。
11. Unity 側として、従来v1の正常パケットを受けながら段階的に状態プロトコルへ移行したい。
12. 保守担当として、既存モデルのコンパイル不良をKinect連携機能と独立して修正したい。

## Implementation Decisions

- 通信先は開発・本番とも同一 Windows PC の `127.0.0.1:5005`。外部 JSON 設定と別 PC 通信は対象外。
- `sessionId` は Bridge プロセス単位、`sensorEpochId` は Skeleton Stream の成功開始・再初期化単位の任意 GUID とする。破壊的変更時だけプロトコル版を上げる。未知の追加項目は無視できる。
- `sensorStatus` は `initializing`、`connected`、`disconnected`、`error` の4状態。`sensorErrorCode` は任意の安定コード（例: `sensor_not_found`、`sensor_not_powered`、`sensor_not_ready`、`sensor_initialization_failed`、`skeleton_stream_start_failed`、`skeleton_stream_stalled`、`sensor_sdk_error`）。未知コードは許容する。
- 正常時は各 SkeletonFrameReady（約30fps）、Kinect 利用不可時は約4Hz、状態変更時は即時に状態パケットを送る。`tracked:false` は人物ロック中に対象を失った場合も送る。
- 最初の fully-tracked で最も近い人物をロックする。別人物には切り替えず、同じ Tracking ID を最大5秒待つ。期限後または `sensorEpochId` 変更時にロックを解除し、新規人物は基準フレームから開始する。
- 送信は単一ゲートで直列化し、ゲート取得後に `frameId` と `timestampMs` を確定する。送信失敗でも ID を再利用せず欠番を記録する。
- Connected 中に 500ms SkeletonFrameReady を得られなければ `skeleton_stream_stalled` として中立化可能な状態を送信し、自動再初期化する。500ms は実機試験で確定する暫定値。
- 平滑化は名前付き固定プロファイル `ExhibitionV1` とし、起動時に実効値をログへ記録する。値の変更には再適合試験を要する。
- Ready は Kinect 接続を条件にせず、UDP・監視・状態発行を開始した時点で `%LOCALAPPDATA%\ArmStrike\KinectRuntime\bridge-ready.json` を原子的に更新する。名前付き Mutex で二重起動を防ぐ。ランチャーは Ready を検証してから Unity を起動し、終了時は Unity、Bridge の順とする。
- Bridge の自動再接続は無期限に継続する。自動でBridgeを終了・再起動しない。Kinect障害エピソードの案内とスタッフによる再起動判断は Unity、プロセス終了・再起動と失敗の記録はランチャーが担当する。責務境界は[Unity側ADR](https://github.com/arm-strike/arm-strike-game/blob/feat/kinect-controler/docs/adr/0001-kinect-fault-escalation-responsibilities.md)を正本とする。
- ログは `%LOCALAPPDATA%\ArmStrike\KinectLogs\` に各プロセス 10MB×5世代で保存する。ログ失敗は入力処理を停止させない。

## Testing Decisions

- 自動試験で、送信ゲートの単調な `frameId`、状態パケット頻度、ロックの5秒保持、Epoch 変更、Watchdog、Ready/Mutex を検証する。
- 実機受入では10分連続で平均25fps以上、通信タイムアウトなし、外部映像計測による入力反映中央値・最大値（目標100ms以内）を複数回記録する。
- Kinect 切断時も自動再接続は継続する。Unityは障害継続時間が10秒に達するとランチャーの「Bridgeを再起動」操作を案内する。新しい `sessionId` を受信後は再起動後待機時間を別に10秒計測し、復旧しなければKinect連携利用不可・展示停止・技術担当への連絡を案内する。Unity 再起動は受信側障害が確認された場合のみ行う。

## Out of Scope

- Unity の UDP FIFO、入力状態、攻撃/チャージの判定、ゲームルール、表示 UI、シーン設定。
- Kinect 以外の入力のゲーム内利用方法。

## Further Notes

### `needs-info`

- Windows 11 のエディション・OS ビルド、Kinect ドライバー詳細、.NET Framework 4.8 Release 値、USB 構成、モデル/シリアル、電源アダプター、USB 変換ケーブル、接続ポート、使用個体の適合結果は未確認。
- Kinect for Xbox 360 を使用予定。使用個体の適合試験に合格した構成のみを対応環境として記録する。予備個体は用意しない。

### 実装・受入の区別

上記の Current Implementation 以外は目標仕様であり未実装。自動試験が通った時点をコード完了、実機試験の通過を受入完了とし、実機確認待ちは `ready-for-human` とする。
