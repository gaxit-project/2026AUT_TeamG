# マスターデータ変換

`MasterData/*.csv` から、C# のクラスと ScriptableObject のアセットを自動で作るツールです。
プランナーは CSV（Excel / スプレッドシート）だけを編集すればよく、コードを書く必要はありません。

## 使い方

1. `MasterData/` に CSV を置く（または編集する）
2. Unity のメニュー **TeamG > マスターデータ > 変換** を実行する
3. 列を追加・変更したときは自動でコンパイルが走り、終わるとデータが取り込まれる

| 生成されるもの | 場所 |
|---|---|
| C# コード（`EnemyRecord` / `EnemyMaster`） | `Assets/_Project/Scripts/Runtime/Data/Generated/` |
| アセット（`EnemyMaster.asset`） | `Assets/_Project/GeneratedData/Master/` |

どちらも**手で編集しないでください**。変換のたびに上書きされます。

## CSV の書き方

`MasterData/Enemy.csv` が見本です。

| 行 | 内容 |
|---|---|
| 1行目 | 列名（英字の大文字で始まる英数字。例: `MoveSpeed`） |
| 2行目 | 型 |
| 3行目 | 説明（生成コードのコメントになる。空欄でもよい） |
| 4行目〜 | データ |

```
Id,Name,Hp,MoveSpeed,IsBoss,DropItemIds,#メモ
int,string,int,float,bool,int[],
ID,表示名,最大HP,移動速度,ボスかどうか,ドロップするアイテムのID,
1,スライム,30,1.5,FALSE,101;102,序盤用
```

- **ファイル名**がテーブル名になります（`Enemy.csv` → `EnemyMaster`）。英字の大文字で始まる英数字にしてください。
- **最初の列は `Id`（int）** にしてください。重複するとエラーになります。
- **列名が `#` で始まる列**は無視されます（メモ用）。
- **先頭のセルが `#` で始まる行**は無視されます（一時的にデータを外したいとき）。
- **空欄**は 0 / false / 空文字 / 空配列になります。
- **文字コードは UTF-8** にしてください。Excel の場合は「CSV UTF-8 (コンマ区切り)」で保存します。普通の「CSV」は Shift_JIS になり、エラーになります。

### 使える型

| 型 | 例 |
|---|---|
| `int` | `30` |
| `float` | `1.5` |
| `bool` | `TRUE` / `FALSE` / `1` / `0` |
| `string` | `スライム`（カンマを含む場合は `"` で囲む） |
| `int[]` / `float[]` / `string[]` | `101;102;103`（`;` 区切り） |

## エラーが出たとき

Console に `Enemy.csv 5行目 D列（Hp）: 'abc' は int として読めません` のように、Excel で見える行番号・列名つきで表示されます。
エラーのあったテーブルは更新されません（前回のデータのままになります）。

## ゲームから使う

`EnemyMaster.asset` をコンポーネントのフィールドにドラッグして使います。

```csharp
using TeamG.Data;

public class EnemySpawner : MonoBehaviour
{
    [SerializeField] EnemyMaster enemyMaster;

    void Spawn(int enemyId)
    {
        var enemy = enemyMaster.Get(enemyId);   // 見つからなければ例外
        Debug.Log($"{enemy.Name} HP:{enemy.Hp}");

        foreach (var e in enemyMaster.All) { /* 全件 */ }
        if (enemyMaster.TryGet(99, out var maybe)) { /* 見つからなくても例外にしない */ }
    }
}
```

## 今後の拡張候補

- enum 型の列（文字列で書いて enum に変換）
- 別テーブルの Id を参照する列のチェック（例: `DropItemIds` が Item.csv に存在するか）
- CSV を保存したら自動で変換する
