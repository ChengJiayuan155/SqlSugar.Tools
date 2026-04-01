using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace SqlSugar.Tools.CodeGen
{
    public static class LogicalFkInfer
    {
        // 现代命名：UserId / CategoryId / AdminId
        public static List<ForeignKeyMeta> InferByColumnName(List<string> allTableNames, TableMeta table)
        {
            var result = new List<ForeignKeyMeta>();
            if (table == null) return result;
            if (table.Columns == null || table.Columns.Count == 0) return result;

            var tableSet = new HashSet<string>((allTableNames ?? new List<string>()).Where(t => !string.IsNullOrWhiteSpace(t)),
                StringComparer.OrdinalIgnoreCase);

            foreach (var col in table.Columns)
            {
                if (col == null) continue;
                if (string.IsNullOrWhiteSpace(col.Name)) continue;
                if (col.IsPrimaryKey) continue;

                var name = col.Name.Trim();

                // 过滤明显不是外键的：Id 本身、或不以 Id 结尾
                if (name.Equals("Id", StringComparison.OrdinalIgnoreCase)) continue;
                if (!name.EndsWith("Id", StringComparison.OrdinalIgnoreCase)) continue;

                var prefix = name.Substring(0, name.Length - 2);
                if (string.IsNullOrWhiteSpace(prefix)) continue;

                // 支持 user_id 这种蛇形：user_id -> user_
                prefix = Regex.Replace(prefix, @"[_\s]+$", "");
                if (prefix.Length == 0) continue;

                // 目标表名候选：prefix / 去复数 s / 去下划线
                var candidates = new List<string>
                {
                    prefix,
                    TrimPluralS(prefix),
                    Normalize(prefix)
                }.Distinct(StringComparer.OrdinalIgnoreCase).ToList();

                var refTable = candidates.FirstOrDefault(c => tableSet.Contains(c));
                if (refTable == null) continue;

                result.Add(new ForeignKeyMeta
                {
                    ColumnName = name,
                    RefTable = refTable,
                    RefColumn = "Id",
                    NavigationName = ToPascalCase(refTable)
                });
            }

            return result;
        }

        private static string TrimPluralS(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return s;
            if (s.EndsWith("s", StringComparison.OrdinalIgnoreCase) && s.Length > 1)
            {
                return s.Substring(0, s.Length - 1);
            }
            return s;
        }

        private static string Normalize(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return s;
            // user_profile -> UserProfile (但用于匹配表名时先去掉分隔符)
            return Regex.Replace(s.Trim(), @"[^a-zA-Z0-9]+", "");
        }

        private static string ToPascalCase(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return "Unknown";
            s = s.Trim();
            s = Regex.Replace(s, @"[^a-zA-Z0-9]+", " ");
            var parts = s.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            var result = "";
            foreach (var p in parts)
            {
                if (p.Length == 0) continue;
                if (p.Length == 1) result += p.ToUpperInvariant();
                else result += char.ToUpperInvariant(p[0]) + p.Substring(1);
            }
            return result.Length == 0 ? "Unknown" : result;
        }
    }
}

