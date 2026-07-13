# Tracking.Common の重複メンバーを修正する

Status: `ready-for-agent`

この問題は Kinect 連携文書の変更以前から `main` に存在するコード不具合であり、Kinect連携実装の前提として別コミットで扱う。

## Current Problem

- `ArmTrackingJointCollection` の `spine` と `hipCenter` が二重定義されている。
- `Clone()` のオブジェクト初期化子でも同じ2項目が二重指定されている。
- `ArmTrackingMetrics` の左右 `HandForwardFromShoulderMeters` が二重定義されている。

## Acceptance Criteria

- 重複メンバーと重複初期化子だけを除去する。
- JSON構造と既存挙動を変更しない。
- Bridgeと共通ライブラリがコンパイルでき、既存テストが通る。
