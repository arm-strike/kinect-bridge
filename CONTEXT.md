# Kinect Bridge Context

Kinect Bridge は Kinect の追跡結果とセンサー状態を Unity へ発行するコンテキストである。

## Language

**Kinect障害エピソード**:
Kinect連携が利用不能になった時点から正常な接続状態へ復旧するまでの連続した障害期間。
_Avoid_: タイムアウト、通信エラー

**Bridge Ready**:
Kinect接続の有無とは独立して、BridgeがUDP・監視・状態発行を開始した起動可能状態。
_Avoid_: Kinect接続完了

**再起動後待機時間**:
スタッフがBridgeを再起動した後、新しいsessionで接続復旧を待つ10秒間。
