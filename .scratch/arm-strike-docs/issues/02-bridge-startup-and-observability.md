# Bridge の起動制御・ログ・復旧を実装する

Status: `ready-for-agent`

## Scope

- Ready ファイル、名前付き Mutex、単一ランチャー連携、ローリングログを実装する。
- `ExhibitionV1` 平滑化プロファイルと実効値のログ出力を追加する。
- 10秒までの自動再接続と再初期化を実装する。

## Acceptance Criteria

- Kinect 未接続でも Ready を発行し、二重起動を防止する。
- ログ書き込み失敗で入力処理が停止しない。
- 復旧順序をログから追跡できる。
