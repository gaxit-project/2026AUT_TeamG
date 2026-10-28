# 命名規則

ファイル名は半角英数字・小文字・アンダースコア区切りにします（日本語・空白は使わない）。
スクリプトとクラス名だけは C# の慣習に合わせて PascalCase にします。

## アセットの接頭辞

| 種類 | 接頭辞 | 例 |
|---|---|---|
| キャラクター | `chr_` | `chr_0001_body.fbx` |
| 背景 | `env_` | `env_forest_rock01.fbx` |
| エフェクト | `fx_` | `fx_hit_fire01.prefab` |
| UI | `ui_` | `ui_btn_ok.png` |
| BGM | `bgm_` | `bgm_title.wav` |
| 効果音 | `se_` | `se_ui_decide.wav` |
| ボイス | `vo_` | `vo_chr_0001_attack01.wav` |
| マテリアル | `mat_` | `mat_chr_0001_body.mat` |
| アニメーション | `anim_` | `anim_chr_0001_run.anim` |
| シーン | なし（PascalCase） | `Boot.unity`、`Stage01.unity` |

## テクスチャの接尾辞

| 接尾辞 | 用途 |
|---|---|
| `_D` | Diffuse / Base Color |
| `_N` | Normal |
| `_M` | Mask（Metallic / AO / Smoothness など） |
| `_E` | Emission |

例: `chr_0001_body_D.png`、`chr_0001_body_N.png`

## スクリプト

- 名前空間はフォルダと asmdef に合わせる（例: `TeamG.Gameplay`）
- 1ファイルに1クラス、ファイル名とクラス名を一致させる
