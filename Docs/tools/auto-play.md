# 自動プレイ（ボット）

ゲームを自動で遊ばせて、例外・エラー・スタック・FPS 低下を見つけるツールです。
**仮想のゲームパッド**を追加してボットに操作させるので、ゲーム側のコードに手を入れる必要はありません。

- コード: `Assets/_Project/Scripts/Runtime/Debug/AutoPlay/`（デバッグメニューと同じ `TeamG.DebugTools`。リリースビルドには入りません）
- 今あるボット: **ランダム入力**（モンキーテスト）

## 始め方

| 場所 | 操作 |
|---|---|
| Unity のメニュー | **TeamG > 自動プレイ > 再生して開始（5分）** など。開いているシーンを再生してすぐ始まり、終わると再生も止まります |
| デバッグメニュー（F1） | 「自動プレイ」カテゴリ。好きな場面まで手で進めてから始められます |
| コマンドライン（Development Build） | 下記 |

```
Game.exe -autoplay -autoplayDuration 600 -autoplaySeed 123 -autoplayTimeScale 2 -autoplayQuit
```

| 引数 | 意味 | 既定値 |
|---|---|---|
| `-autoplay` | 起動したら自動プレイを始める | — |
| `-autoplayDuration 秒` | 実行時間（0 で無制限） | 300 |
| `-autoplaySeed 数` | 乱数のシード（同じ値なら同じ入力列） | 起動時刻から決める |
| `-autoplayTimeScale 倍率` | ゲームの速度 | 1 |
| `-autoplayQuit` | 終わったら終了する。**問題があれば終了コード 1** | なし |

`-autoplayQuit` の終了コードを見れば、夜間テストや CI で成功・失敗を判定できます。

## 見張っているもの

| 種類 | 条件 |
|---|---|
| 例外 | `Exception` が出た |
| エラーログ | `Debug.LogError` / `Assert` が出た |
| スタック | `Player` タグのオブジェクトが 15 秒以上 0.5m 以上動かない（ポーズ中は数えない） |
| FPS 低下 | 1 秒間の平均が 20 FPS 未満（シーン読み込み直後の 3 秒は除く） |

同じ内容は1件にまとめて回数を数えます。新しい問題が見つかるたびにスクリーンショットを撮ります（最大 30 枚）。

**スタック検出を使うには、プレイヤーのオブジェクトに `Player` タグをつけてください。** タグがなければスタック検出だけ行われません。

## レポート

`2026AUT_TeamG/AutoPlayReports/日時/` に出力されます（Git には入りません）。
メニューの **TeamG > 自動プレイ > レポートフォルダを開く** で開けます。

| ファイル | 中身 |
|---|---|
| `report.md` | 人が読むまとめ（実行時間、FPS、問題の一覧、スタックトレース、スクリーンショット） |
| `report.json` | ツールで集計する用 |
| `shot_001.png` … | 問題が起きた瞬間の画面 |

**バグを報告するときはシード値も書いてください。** 同じシードで流すと、同じ入力列を再現できます（フレーム時間が違うと完全には一致しません）。

## 注意

- ランダム入力は L3 / R3（デバッグメニューの開閉）と Select を押しません。Start はまれに押します。
- 自動プレイ中はゲーム側でタイムスケールを変えると、そちらが優先されます。
- キーボード・マウスだけで操作するゲームの場合は、仮想キーボードを使うボットを足す必要があります。

## ボットを増やす

`IAutoPlayBehaviour` を実装して、毎フレームのゲームパッド入力（`GamepadState`）を返すクラスを作ります。

```csharp
public sealed class WalkForwardBehaviour : IAutoPlayBehaviour
{
    public string Name => "前進し続ける";

    public GamepadState Tick(float dt)
    {
        return new GamepadState { leftStick = Vector2.up }.WithButton(GamepadButton.South);
    }
}

AutoPlayer.Begin(new AutoPlaySettings { DurationSeconds = 60f }, new WalkForwardBehaviour());
```

ゲームが形になってきたら、次のようなボットを足していく予定です。

- 入力の記録と再生（人のプレイをそのまま再現する）
- シナリオ型（目的地まで NavMesh で歩く、敵を見つけたら攻撃する など）
