using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TeamG.Data;
using UnityEditor;
using UnityEngine;

namespace TeamG.EditorTools
{
    /// <summary>CSV のデータを、生成済みの Master アセット（ScriptableObject）に流し込む。</summary>
    internal static class MasterDataImporter
    {
        const BindingFlags FieldFlags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

        /// <summary>1テーブル分を取り込む。データに誤りがあればアセットは更新せず errors に追記する。</summary>
        public static bool Import(MasterSheet sheet, string assetDirectory, List<string> errors)
        {
            var tableType = TypeCache.GetTypesDerivedFrom<MasterTableBase>()
                .FirstOrDefault(t => t.Name == sheet.TableClassName && !t.IsAbstract);
            if (tableType == null)
            {
                errors.Add($"{sheet.FileName}: {sheet.TableClassName} クラスが見つかりません。コンパイルエラーがないか確認してください");
                return false;
            }

            var errorCount = errors.Count;
            // MasterTable<TRecord> の TRecord
            var recordType = tableType.BaseType.GetGenericArguments()[0];
            var fields = new FieldInfo[sheet.Columns.Count];
            for (var i = 0; i < sheet.Columns.Count; i++)
            {
                fields[i] = recordType.GetField(sheet.Columns[i].FieldName, FieldFlags);
                if (fields[i] == null)
                {
                    errors.Add($"{sheet.FileName}: {recordType.Name} に {sheet.Columns[i].Name} がありません。もう一度変換してください");
                }
            }
            if (errors.Count != errorCount)
            {
                return false;
            }

            var records = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(recordType));
            var lineById = new Dictionary<int, int>();
            foreach (var row in sheet.Rows)
            {
                var record = Activator.CreateInstance(recordType);
                for (var i = 0; i < sheet.Columns.Count; i++)
                {
                    var column = sheet.Columns[i];
                    var raw = column.CsvIndex < row.Cells.Count ? row.Cells[column.CsvIndex] : "";
                    if (MasterSheet.TryParse(column.Type, raw, out var value))
                    {
                        fields[i].SetValue(record, value);
                    }
                    else
                    {
                        errors.Add($"{sheet.FileName} {row.Line}行目 {MasterSheet.ColumnLetter(column.CsvIndex)}列（{column.Name}）: '{raw}' は {column.CSharpType} として読めません");
                    }
                }

                var id = ((IMasterRecord)record).Id;
                if (lineById.TryGetValue(id, out var firstLine))
                {
                    errors.Add($"{sheet.FileName} {row.Line}行目: Id {id} が {firstLine}行目と重複しています");
                }
                else
                {
                    lineById.Add(id, row.Line);
                }
                records.Add(record);
            }

            if (errors.Count != errorCount)
            {
                return false;
            }

            var assetPath = $"{assetDirectory}/{sheet.TableClassName}.asset";
            var asset = AssetDatabase.LoadAssetAtPath<MasterTableBase>(assetPath);
            if (asset == null)
            {
                asset = (MasterTableBase)ScriptableObject.CreateInstance(tableType);
                EnsureFolder(assetDirectory);
                AssetDatabase.CreateAsset(asset, assetPath);
            }

            asset.EditorSetRecords(records);
            EditorUtility.SetDirty(asset);
            return true;
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            var parent = path.Substring(0, path.LastIndexOf('/'));
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, path.Substring(parent.Length + 1));
        }
    }
}
