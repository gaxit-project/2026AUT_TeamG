# フォルダ構成

## リポジトリ直下

Unity プロジェクトはリポジトリの一部（`2026AUT_TeamG/` サブフォルダ）として置き、
ゲーム外のデータやツールはリポジトリ直下に置きます。

```
(リポジトリ直下)/
├── 2026AUT_TeamG/     # Unity プロジェクト本体
│   ├── Assets/
│   ├── Packages/      # 社内ツールの com.teamg.tools を含む
│   └── ProjectSettings/
├── Tools/             # Unity 外のツール（Python、バッチなど）
├── MasterData/        # マスターデータの元データ（Excel / CSV）
├── Localization/      # 翻訳テキストの元データ
├── CI/                # ビルド自動化のスクリプト
└── Docs/              # 仕様・ルール・ツールのマニュアル
```


## Assets/

```
Assets/
├── _Project/                    # 自分たちで作ったものはすべてここ
│   ├── Scripts/
│   │   ├── Runtime/
│   │   │   ├── Core/            # 基盤（シーン管理、入力、セーブ、ログ）   TeamG.Core
│   │   │   ├── Data/            # マスターデータの型定義                 TeamG.Data
│   │   │   │   └── Generated/   # ツールが自動生成するコード（手で編集しない）
│   │   │   ├── Gameplay/        # ゲームロジック                         TeamG.Gameplay
│   │   │   ├── UI/              #                                        TeamG.UI
│   │   │   └── Debug/           # デバッグメニュー（開発ビルドのみ）     TeamG.DebugTools
│   │   └── Editor/              # このプロジェクト専用のエディタ拡張     TeamG.Editor
│   ├── Art/
│   │   ├── Characters/          # キャラ単位でまとめる（例: chr_0001/）
│   │   ├── Environments/
│   │   ├── Effects/
│   │   └── UI/
│   ├── Audio/  (BGM / SE / Voice)
│   ├── Scenes/
│   │   ├── Boot/                # 起動シーン
│   │   ├── Stages/
│   │   └── Dev/                 # 個人のテスト用シーン（ビルドに含めない）
│   ├── Prefabs/
│   ├── Settings/                # URP・入力などの設定アセット
│   └── GeneratedData/           # ツールが書き出すデータ（手で編集しない）
├── Plugins/                     # 外部ライブラリ（DLL など）
└── ThirdParty/                  # Asset Store のアセット（_Project と混ぜない）
```

## ルール

- **アセットは種類別ではなく、まとまりごとに置く。** 1キャラ分のモデル・テクスチャ・マテリアル・アニメは `Art/Characters/chr_0001/` にまとめる。
- **`Generated` / `GeneratedData` は手で編集しない。** ツールが上書きします。
- **ファイルの移動・名前変更は Unity エディタ上で行う。** エクスプローラーで動かす場合は必ず `.meta` も一緒に動かす。
- **空フォルダには `.gitkeep` が入っています。** 中身を置いたら消して構いません。

## Assembly Definition の依存関係

矢印の向きにしか参照できない（逆向きに参照するとコンパイルエラーになる）

```
Core ← Data ← Gameplay ← UI
                 ↑         ↑
                 └ DebugTools ┘ （UNITY_EDITOR か DEVELOPMENT_BUILD のときだけコンパイル）

Editor → すべての Runtime を参照（エディタ専用）
```

- Core から Gameplay を参照したくなったら、インターフェースやイベントを Core に置いて依存を逆転させます。
- 本番コードから `TeamG.DebugTools` を参照しないでください（リリースビルドで存在しません）。
- 名前空間を `TeamG.Debug` にしないでください。`TeamG` 配下のコードで `Debug.Log` が `UnityEngine.Debug` ではなくその名前空間として解釈され、コンパイルエラーになります。

## 社内ツールパッケージ

`Packages/com.teamg.tools/` は次のプロジェクトにも持ち出せる汎用ツールを置く場所です。
このゲーム専用のツールは `Assets/_Project/Scripts/Editor/` に置きます。
