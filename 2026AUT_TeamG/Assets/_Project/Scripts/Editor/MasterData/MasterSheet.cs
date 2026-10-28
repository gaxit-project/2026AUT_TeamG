using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace TeamG.EditorTools
{
    internal enum MasterColumnType
    {
        Int,
        Float,
        Bool,
        String,
        IntArray,
        FloatArray,
        StringArray,
    }

    internal sealed class MasterColumn
    {
        public string Name;
        public MasterColumnType Type;
        public string Description;
        public int CsvIndex;

        /// <summary>シリアライズされるフィールド名（リフレクションで使う）。</summary>
        public string FieldName => char.ToLowerInvariant(Name[0]) + Name.Substring(1);

        /// <summary>C# コードに書くときの識別子（予約語なら @ を付ける）。</summary>
        public string FieldIdentifier => MasterSheet.CSharpKeywords.Contains(FieldName) ? "@" + FieldName : FieldName;

        public string CSharpType => Type switch
        {
            MasterColumnType.Int => "int",
            MasterColumnType.Float => "float",
            MasterColumnType.Bool => "bool",
            MasterColumnType.String => "string",
            MasterColumnType.IntArray => "int[]",
            MasterColumnType.FloatArray => "float[]",
            MasterColumnType.StringArray => "string[]",
            _ => throw new ArgumentOutOfRangeException(),
        };
    }

    internal sealed class MasterRow
    {
        /// <summary>CSV 上の行番号（Excel で見える行番号と同じ、1始まり）。</summary>
        public int Line;
        public List<string> Cells;
    }

    /// <summary>
    /// CSV 1ファイル分の定義とデータ。
    /// 1行目: 列名 / 2行目: 型 / 3行目: 説明 / 4行目以降: データ
    /// 先頭セルが # の行と、列名が # で始まる列は無視する。
    /// </summary>
    internal sealed class MasterSheet
    {
        public const string ArraySeparator = ";";
        const int HeaderRows = 3;

        static readonly Regex IdentifierPattern = new Regex("^[A-Z][A-Za-z0-9]*$");

        internal static readonly HashSet<string> CSharpKeywords = new HashSet<string>
        {
            "abstract", "as", "base", "bool", "break", "byte", "case", "catch", "char", "checked", "class", "const",
            "continue", "decimal", "default", "delegate", "do", "double", "else", "enum", "event", "explicit",
            "extern", "false", "finally", "fixed", "float", "for", "foreach", "goto", "if", "implicit", "in", "int",
            "interface", "internal", "is", "lock", "long", "namespace", "new", "null", "object", "operator", "out",
            "override", "params", "private", "protected", "public", "readonly", "ref", "return", "sbyte", "sealed",
            "short", "sizeof", "stackalloc", "static", "string", "struct", "switch", "this", "throw", "true", "try",
            "typeof", "uint", "ulong", "unchecked", "unsafe", "ushort", "using", "virtual", "void", "volatile", "while",
        };

        static readonly Dictionary<string, MasterColumnType> TypeNames = new Dictionary<string, MasterColumnType>
        {
            { "int", MasterColumnType.Int },
            { "float", MasterColumnType.Float },
            { "bool", MasterColumnType.Bool },
            { "string", MasterColumnType.String },
            { "int[]", MasterColumnType.IntArray },
            { "float[]", MasterColumnType.FloatArray },
            { "string[]", MasterColumnType.StringArray },
        };

        public string TableName;
        public string SourcePath;
        public readonly List<MasterColumn> Columns = new List<MasterColumn>();
        public readonly List<MasterRow> Rows = new List<MasterRow>();

        public string RecordClassName => TableName + "Record";
        public string TableClassName => TableName + "Master";
        public string FileName => Path.GetFileName(SourcePath);

        /// <summary>CSV を読んで定義を検証する。定義に誤りがあれば errors に追記して null を返す。</summary>
        public static MasterSheet Load(string path, List<string> errors)
        {
            var sheet = new MasterSheet
            {
                TableName = Path.GetFileNameWithoutExtension(path),
                SourcePath = path,
            };
            var errorCount = errors.Count;

            if (!IdentifierPattern.IsMatch(sheet.TableName))
            {
                errors.Add($"{sheet.FileName}: ファイル名は英字の大文字で始まる英数字にしてください（例: Enemy.csv）");
                return null;
            }

            List<List<string>> rows;
            try
            {
                rows = CsvReader.Parse(CsvReader.ReadAllText(path));
            }
            catch (Exception e)
            {
                errors.Add($"{sheet.FileName}: {e.Message}");
                return null;
            }

            if (rows.Count < HeaderRows)
            {
                errors.Add($"{sheet.FileName}: 1行目に列名、2行目に型、3行目に説明が必要です");
                return null;
            }

            var names = rows[0];
            var types = rows[1];
            var descriptions = rows[2];
            for (var i = 0; i < names.Count; i++)
            {
                var name = names[i].Trim();
                if (name.Length == 0 || name.StartsWith("#"))
                {
                    continue;
                }

                var typeName = i < types.Count ? types[i].Trim() : "";
                if (!IdentifierPattern.IsMatch(name))
                {
                    errors.Add($"{sheet.FileName} {ColumnLetter(i)}列: 列名 '{name}' は英字の大文字で始まる英数字にしてください");
                }
                else if (sheet.Columns.Any(c => c.Name == name))
                {
                    errors.Add($"{sheet.FileName} {ColumnLetter(i)}列: 列名 '{name}' が重複しています");
                }
                else if (!TypeNames.TryGetValue(typeName, out var type))
                {
                    errors.Add($"{sheet.FileName} {ColumnLetter(i)}列: 型 '{typeName}' は使えません（{string.Join(" / ", TypeNames.Keys)}）");
                }
                else
                {
                    sheet.Columns.Add(new MasterColumn
                    {
                        Name = name,
                        Type = type,
                        Description = i < descriptions.Count ? descriptions[i].Trim() : "",
                        CsvIndex = i,
                    });
                }
            }

            if (sheet.Columns.Count == 0 || sheet.Columns[0].Name != "Id" || sheet.Columns[0].Type != MasterColumnType.Int)
            {
                errors.Add($"{sheet.FileName}: 最初の列は Id（型 int）にしてください");
            }

            for (var r = HeaderRows; r < rows.Count; r++)
            {
                var cells = rows[r];
                var isBlank = cells.All(string.IsNullOrWhiteSpace);
                var isComment = cells.Count > 0 && cells[0].TrimStart().StartsWith("#");
                if (!isBlank && !isComment)
                {
                    sheet.Rows.Add(new MasterRow { Line = r + 1, Cells = cells });
                }
            }

            return errors.Count == errorCount ? sheet : null;
        }

        /// <summary>セルの文字列を列の型に変換する。空欄は既定値（0 / false / "" / 空配列）。</summary>
        public static bool TryParse(MasterColumnType type, string raw, out object value)
        {
            raw = raw?.Trim() ?? "";
            switch (type)
            {
                case MasterColumnType.Int:
                    value = 0;
                    if (raw.Length == 0) return true;
                    if (!int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var i)) return false;
                    value = i;
                    return true;
                case MasterColumnType.Float:
                    value = 0f;
                    if (raw.Length == 0) return true;
                    if (!float.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var f)) return false;
                    value = f;
                    return true;
                case MasterColumnType.Bool:
                    switch (raw.ToLowerInvariant())
                    {
                        case "": case "false": case "0": value = false; return true;
                        case "true": case "1": value = true; return true;
                        default: value = false; return false;
                    }
                case MasterColumnType.String:
                    value = raw;
                    return true;
                case MasterColumnType.IntArray:
                    return TryParseArray(raw, MasterColumnType.Int, out value, typeof(int));
                case MasterColumnType.FloatArray:
                    return TryParseArray(raw, MasterColumnType.Float, out value, typeof(float));
                case MasterColumnType.StringArray:
                    return TryParseArray(raw, MasterColumnType.String, out value, typeof(string));
                default:
                    throw new ArgumentOutOfRangeException(nameof(type));
            }
        }

        static bool TryParseArray(string raw, MasterColumnType elementType, out object value, Type clrType)
        {
            var parts = raw.Length == 0 ? Array.Empty<string>() : raw.Split(new[] { ArraySeparator }, StringSplitOptions.None);
            var array = Array.CreateInstance(clrType, parts.Length);
            value = array;
            for (var i = 0; i < parts.Length; i++)
            {
                if (!TryParse(elementType, parts[i], out var element))
                {
                    return false;
                }
                array.SetValue(element, i);
            }
            return true;
        }

        /// <summary>0 → A、25 → Z、26 → AA（Excel の列名）。</summary>
        public static string ColumnLetter(int index)
        {
            var letters = "";
            for (index++; index > 0; index = (index - 1) / 26)
            {
                letters = (char)('A' + (index - 1) % 26) + letters;
            }
            return letters;
        }
    }
}
