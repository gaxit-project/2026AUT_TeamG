using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace TeamG.EditorTools
{
    /// <summary>RFC 4180 形式の CSV を読む（"" で囲んだセル内のカンマ・改行・"" に対応）。</summary>
    internal static class CsvReader
    {
        static readonly UTF8Encoding StrictUtf8 = new UTF8Encoding(false, true);

        /// <summary>UTF-8 として読む。UTF-8 でなければ例外にする（Excel の通常の CSV は Shift_JIS のため）。</summary>
        public static string ReadAllText(string path)
        {
            var bytes = File.ReadAllBytes(path);
            var offset = bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF ? 3 : 0;
            try
            {
                return StrictUtf8.GetString(bytes, offset, bytes.Length - offset);
            }
            catch (DecoderFallbackException)
            {
                throw new InvalidDataException("UTF-8 ではありません。Excel なら「CSV UTF-8 (コンマ区切り)」で保存してください。");
            }
        }

        public static List<List<string>> Parse(string text)
        {
            var rows = new List<List<string>>();
            var row = new List<string>();
            var cell = new StringBuilder();
            var inQuotes = false;

            for (var i = 0; i < text.Length; i++)
            {
                var c = text[i];
                if (inQuotes)
                {
                    if (c != '"')
                    {
                        cell.Append(c);
                    }
                    else if (i + 1 < text.Length && text[i + 1] == '"')
                    {
                        cell.Append('"');
                        i++;
                    }
                    else
                    {
                        inQuotes = false;
                    }
                    continue;
                }

                switch (c)
                {
                    case '"':
                        inQuotes = true;
                        break;
                    case ',':
                        row.Add(cell.ToString());
                        cell.Clear();
                        break;
                    case '\r':
                        break;
                    case '\n':
                        row.Add(cell.ToString());
                        cell.Clear();
                        rows.Add(row);
                        row = new List<string>();
                        break;
                    default:
                        cell.Append(c);
                        break;
                }
            }

            if (cell.Length > 0 || row.Count > 0)
            {
                row.Add(cell.ToString());
                rows.Add(row);
            }
            return rows;
        }
    }
}
