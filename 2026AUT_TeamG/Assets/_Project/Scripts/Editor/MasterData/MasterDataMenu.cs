using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace TeamG.EditorTools
{
    /// <summary>
    /// マスターデータ変換のメニュー。
    /// 1. CSV から C# コードを生成 → 2. コンパイル待ち → 3. CSV のデータをアセットに取り込み
    /// </summary>
    internal static class MasterDataMenu
    {
        const string MenuRoot = "TeamG/マスターデータ/";
        const string CodeDirectory = "Assets/_Project/Scripts/Runtime/Data/Generated";
        const string AssetDirectory = "Assets/_Project/GeneratedData/Master";
        const string PendingImportKey = "TeamG.MasterData.PendingImport";

        /// <summary>リポジトリ直下の MasterData フォルダ（Unity プロジェクトの1つ上）。</summary>
        static string SourceDirectory => Path.GetFullPath(Path.Combine(Application.dataPath, "../../MasterData"));

        [MenuItem(MenuRoot + "変換", priority = 0)]
        static void Convert()
        {
            var errors = new List<string>();
            var sheets = LoadSheets(errors);

            var codeChanged = false;
            foreach (var sheet in sheets)
            {
                codeChanged |= MasterCodeGenerator.Write(sheet, CodeDirectory);
            }

            if (codeChanged)
            {
                ReportErrors(errors);
                // コンパイル後のドメインリロードで ResumeAfterCompile からデータ取り込みを続ける
                SessionState.SetBool(PendingImportKey, true);
                Debug.Log("[MasterData] コードを更新しました。コンパイル後にデータを取り込みます");
                AssetDatabase.Refresh();
                return;
            }

            ImportSheets(sheets, errors);
        }

        [MenuItem(MenuRoot + "MasterData フォルダを開く", priority = 20)]
        static void OpenSourceFolder()
        {
            Directory.CreateDirectory(SourceDirectory);
            EditorUtility.RevealInFinder(SourceDirectory + Path.DirectorySeparatorChar);
        }

        [InitializeOnLoadMethod]
        static void ResumeAfterCompile()
        {
            if (!SessionState.GetBool(PendingImportKey, false))
            {
                return;
            }

            SessionState.EraseBool(PendingImportKey);
            EditorApplication.delayCall += () =>
            {
                var errors = new List<string>();
                ImportSheets(LoadSheets(errors), errors);
            };
        }

        static List<MasterSheet> LoadSheets(List<string> errors)
        {
            if (!Directory.Exists(SourceDirectory))
            {
                errors.Add($"MasterData フォルダがありません: {SourceDirectory}");
                return new List<MasterSheet>();
            }

            return Directory.GetFiles(SourceDirectory, "*.csv")
                .OrderBy(p => p)
                .Select(p => MasterSheet.Load(p, errors))
                .Where(s => s != null)
                .ToList();
        }

        static void ImportSheets(List<MasterSheet> sheets, List<string> errors)
        {
            var imported = new List<string>();
            foreach (var sheet in sheets)
            {
                if (MasterDataImporter.Import(sheet, AssetDirectory, errors))
                {
                    imported.Add($"{sheet.TableName}（{sheet.Rows.Count}件）");
                }
            }
            AssetDatabase.SaveAssets();

            if (imported.Count > 0)
            {
                Debug.Log($"[MasterData] 取り込みました: {string.Join("、", imported)}");
            }
            ReportErrors(errors);

            if (errors.Count > 0)
            {
                EditorUtility.DisplayDialog("マスターデータ変換",
                    $"{errors.Count}件のエラーがあります。エラーのあったテーブルは更新していません。\n詳しくは Console を見てください。", "OK");
            }
        }

        static void ReportErrors(List<string> errors)
        {
            foreach (var error in errors)
            {
                Debug.LogError($"[MasterData] {error}");
            }
        }
    }
}
