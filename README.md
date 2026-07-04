# Kinect Bridge

Xbox 360 Kinect の腕トラッキング情報を Unity に渡すためのブリッジです。

## 要件

### 送信側

- Windows で動作する .NET Framework 系の C# 実行環境
- Kinect for Xbox 360 本体
- Kinect for Windows SDK 1.8
- `Microsoft.Kinect.dll` への参照
- UDP 送信先にアクセスできるネットワーク設定

### 受信側

- Unity を動かせる環境
- このリポジトリの `UnitySample/Assets/Tracking/` 配下のスクリプトを組み込める Unity プロジェクト
- 受信モードに応じて `Udp` / `Keyboard` / `Replay` を切り替えられること

### 開発・検証

- MSBuild または Visual Studio / Build Tools
- `Tracking.Common.Tests` を起動できるコンソール実行環境

## 構成

- `KinectBridge/`
  - Kinect から関節座標を取得して UDP 送信する実行ファイルです。
- `Tracking.Common/`
  - ワイヤ形式、検証、正規化、メトリクス、状態管理の共通ロジックです。
- `Tracking.Common.Tests/`
  - 共通ロジックの簡易テストです。
- `UnitySample/Assets/Tracking/`
  - Unity 側の受信アダプタと表示サンプルです。

## いまの設計

- `Tracking.Runtime` は Unity 向けの薄いアダプタです。
- UDP と Replay は `WirePacketDto` を厳密に検証してから `ArmTrackingPacket` に変換します。
- Keyboard は内部入力ソースとして直接 `ArmTrackingPacket` を生成します。
- `IArmTrackingSource` からは `Status` と `LastReceiveError` を参照できます。
- `LastReceiveError` は有効パケット受理時とライフサイクル切り替え時にクリアされます。

## 送信データ

相手先には、1 UDP データグラムにつき 1 つの UTF-8 JSON を送ります。
受信側はこの JSON を `WirePacketDto` として読み取り、厳密な検証を通ったものだけを `ArmTrackingPacket` として扱います。

### 共通フィールド

- `version`: 現在は `1`
- `sessionId`: Bridge 起動ごとに一意な GUID 文字列
- `frameId`: 起動後に増える連番
- `timestampMs`: Bridge 起動からの経過ミリ秒
- `tracked`: `true` なら人物追跡中、`false` なら未追跡

### `tracked:true` のとき

- `trackingId`: Kinect のトラッキング ID
- `joints`: 肩、肘、手首、手の関節セット

### `tracked:false` のとき

- `trackingId` と `joints` は省略します

### 関節データ

各関節は次の形です。

- `x`: 座標 X
- `y`: 座標 Y
- `z`: 座標 Z
- `state`: `0=NotTracked`, `1=Inferred`, `2=Tracked`

### 送信例

```json
{
  "version": 1,
  "sessionId": "8d1f4f3c-0a24-4b22-8f0d-7d1d7d4d9b61",
  "frameId": 12,
  "timestampMs": 340,
  "tracked": true,
  "trackingId": 42,
  "joints": {
    "shoulderCenter": { "x": 0.0, "y": 1.4, "z": 2.0, "state": 2 },
    "shoulderLeft": { "x": -0.2, "y": 1.4, "z": 2.0, "state": 2 },
    "elbowLeft": { "x": -0.3, "y": 1.1, "z": 2.0, "state": 2 },
    "wristLeft": { "x": -0.35, "y": 0.85, "z": 2.0, "state": 2 },
    "handLeft": { "x": -0.4, "y": 0.8, "z": 2.0, "state": 2 },
    "shoulderRight": { "x": 0.2, "y": 1.4, "z": 2.0, "state": 2 },
    "elbowRight": { "x": 0.3, "y": 1.1, "z": 2.0, "state": 2 },
    "wristRight": { "x": 0.35, "y": 0.85, "z": 2.0, "state": 2 },
    "handRight": { "x": 0.4, "y": 0.8, "z": 2.0, "state": 2 }
  }
}
```

`tracked:false` の場合は次のようになります。

```json
{
  "version": 1,
  "sessionId": "8d1f4f3c-0a24-4b22-8f0d-7d1d7d4d9b61",
  "frameId": 13,
  "timestampMs": 360,
  "tracked": false
}
```

## セットアップ

### 1. 依存関係を用意する

- Kinect センサーを接続し、Windows 側で認識されていることを確認します。
- Kinect for Windows SDK 1.8 をインストールします。
- `Microsoft.Kinect.dll` が参照できる状態にします。
- Visual Studio または Build Tools を使える状態にします。

### 2. リポジトリを開く

- このリポジトリをクローン、または作業フォルダとして開きます。
- Unity 側を使う場合は、`UnitySample/Assets/Tracking/` の内容を Unity プロジェクトへ取り込みます。

### 3. 送信側をビルドする

- `KinectBridge.sln` を開いて `KinectBridge` をビルドします。
- 必要なら `Tracking.Common` も一緒にビルドします。
- `Tracking.Common.Tests` も同じソリューションまたは個別プロジェクトとしてビルドできます。

### 4. 検証用テストを実行する

- `Tracking.Common.Tests` を起動して、wire 検証や正規化の基本挙動を確認します。

### 5. 実行する

- `KinectBridge.exe` を起動します。
- Unity 側で `ArmTrackingSourceHost` を配置し、`Udp` / `Keyboard` / `Replay` のいずれかを選びます。
- `ArmTrackingDebugVisualizer` を追加すると、状態に応じた可視化を確認できます。

## ビルド

このリポジトリは、`KinectBridge` と `Tracking.Common.Tests` をビルドできる .NET Framework 系の C# 環境を前提にしています。
MSBuild / Visual Studio / Build Tools のどれを使うかは、使っている環境に合わせてください。

Visual Studio の開発者向けコマンドプロンプトや PowerShell から、利用可能な `MSBuild.exe` を呼び出してビルドします。
下の例はパスを固定していますが、実際には自分の環境にある `MSBuild.exe` の場所へ置き換えてください。

```powershell
& "MSBuild.exe" `
  "path\to\kinect-bridge\KinectBridge.sln" `
  /t:Build /p:Configuration=Debug /p:Platform="Any CPU"
```

個別にビルドする場合は次の 2 つです。

```powershell
& "MSBuild.exe" `
  "path\to\kinect-bridge\KinectBridge\KinectBridge.csproj" `
  /t:Build /p:Configuration=Debug /p:Platform=AnyCPU

& "MSBuild.exe" `
  "path\to\kinect-bridge\Tracking.Common.Tests\Tracking.Common.Tests.csproj" `
  /t:Build /p:Configuration=Debug /p:Platform=AnyCPU
```

## テスト

`Tracking.Common.Tests` はコンソールアプリです。ビルド後に生成された実行ファイルを直接起動します。
出力先は構成や環境によって変わるので、`bin/Debug` や `bin/Release` など、自分のビルド先を確認してください。

```powershell
& "path\to\kinect-bridge\Tracking.Common.Tests\bin\Debug\Tracking.Common.Tests.exe"
```

## Unity 側の見方

- `ArmTrackingSourceHost` で UDP / Keyboard / Replay を切り替えます。
- `ArmTrackingDebugVisualizer` は `Status` に応じて表示色を変えます。
- 正規化済み姿勢を使うかどうかは `useNormalizedPose` で切り替えられます。

## 補足

- 送信側の JSON 形式は `Tracking.Common` の `WirePacketDecoder` で検証されます。
- 公開 API はできるだけ維持していますが、`IArmTrackingSource` に `Status` と `LastReceiveError` が追加されています。
- この README では固定の絶対パスを避けています。必要に応じて、あなたの環境のパスに読み替えてください。
