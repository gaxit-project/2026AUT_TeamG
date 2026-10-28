# デバッグメニュー

ゲーム中にパラメータを変えたり、シーンを移動したりするためのメニューです。
シーンに何も置かなくても、Play 開始時に自動で生成されます。

- コード: `Assets/_Project/Scripts/Runtime/Debug/`（アセンブリ `TeamG.DebugTools`）
- エディタと Development Build にだけ含まれます。リリースビルドにはコードごと入りません。

## 開き方

| 入力 | 操作 |
|---|---|
| キーボード | `F1` |
| ゲームパッド | L3（左スティック押し込み）を押しながら R3 |

ウィンドウはタイトル部分をドラッグして動かせます。画面右上の FPS 表示は「システム/FPS表示」で消せます。

## 標準で入っている項目

| カテゴリ | 項目 |
|---|---|
| システム | FPS表示、タイムスケール、一時停止、解像度、メモリ、ビルド情報 |
| シーン | 現在のシーン、読み直し、Build Profiles に登録した全シーンへのジャンプ |

## 項目の追加方法

### 1. いつでも使える項目 → 属性をつける

`Assets/_Project/Scripts/Runtime/Debug/` に static クラスを作り、`[DebugMenuItem("カテゴリ/項目名")]` をつけます。
メンバーの種類で表示が自動で決まります。

| メンバー | 表示 |
|---|---|
| 引数なしの static メソッド | ボタン |
| `bool` の static プロパティ（get / set） | トグル |
| `float` / `int` の static プロパティ（get / set） | スライダー（`Min` / `Max` で範囲を指定） |
| get だけの static プロパティ | 値の表示 |

```csharp
namespace TeamG.DebugTools
{
    internal static class PlayerDebugItems
    {
        [DebugMenuItem("プレイヤー/無敵")]
        static bool Invincible
        {
            get => TeamG.Gameplay.GameplayFlags.Invincible;
            set => TeamG.Gameplay.GameplayFlags.Invincible = value;
        }

        [DebugMenuItem("プレイヤー/移動速度倍率", Min = 0.5f, Max = 5f)]
        static float SpeedScale
        {
            get => TeamG.Gameplay.GameplayFlags.SpeedScale;
            set => TeamG.Gameplay.GameplayFlags.SpeedScale = value;
        }
    }
}
```

`Order` を指定すると、カテゴリ内の並び順を変えられます（小さいほど上）。

### 2. シーン上のオブジェクトに紐づく項目 → コードで登録する

登録すると `DebugItem` が返るので、オブジェクトが消えるときに `Dispose()` します。

```csharp
var item = DebugMenuRegistry.AddButton("敵/全員倒す", () => KillAllEnemies());
// …
item.Dispose();
```

`AddToggle` / `AddSlider` / `AddIntSlider` / `AddLabel` もあります。

## ゲーム側（Gameplay など）とのつなぎ方

`TeamG.Gameplay` からは `TeamG.DebugTools` を参照できません（リリースビルドに存在しないため）。
そのため、つなぎ方は次のようにします。

1. Gameplay 側に、チートで変えたい値を `public static` のフラグや設定として用意する（例: `GameplayFlags.Invincible`）
2. DebugTools 側の項目からその値を書き換える

Gameplay 側でデバッグメニューが開いているか知りたい場合は、Core にイベントやフラグを置き、DebugTools 側から設定してください。
