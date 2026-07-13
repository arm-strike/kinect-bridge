# Bridge の人物ロック・状態プロトコルを実装する

Status: `ready-for-agent`

## Scope

- 5秒の Tracking ID ロック、`tracked:false`、`sensorStatus`/`sensorErrorCode`、`sensorEpochId` を追加する。
- 単一送信ゲートで `frameId`/`timestampMs` を確定し、順序と欠番を扱う。
- 4Hz の利用不可状態パケットと 500ms Skeleton Stream Watchdog を実装する。

## Acceptance Criteria

- 同一 Bridge session でフレーム ID は再利用されず、状態変化を Unity が識別できる。
- ロック対象を失っている5秒間に別人物へ切り替わらない。
- エラー・切断・ストール時に、Bridge 停止によるタイムアウトと区別可能な状態が発行される。

Cross-repo consumer: [Unity Kinect specification](https://github.com/arm-strike/arm-strike-game/blob/feat/kinect-controler/.scratch/arm-strike-docs/spec.md)
