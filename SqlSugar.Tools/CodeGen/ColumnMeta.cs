using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace SqlSugar.Tools.CodeGen
{
    public class ColumnMeta
    {
        public string Name { get; set; }
        public string Comment { get; set; }
        public Type ClrType { get; set; }
        public bool IsNullable { get; set; }
        public bool IsPrimaryKey { get; set; }
        public bool IsIdentity { get; set; }

        public EnumMeta Enum { get; set; }

        public string GetCSharpTypeName(bool useNullableRef = true)
        {
            var t = ClrType ?? typeof(string);

            string name;
            if (t == typeof(string)) name = "string";
            else if (t == typeof(int)) name = "int";
            else if (t == typeof(long)) name = "long";
            else if (t == typeof(short)) name = "short";
            else if (t == typeof(byte)) name = "byte";
            else if (t == typeof(bool)) name = "bool";
            else if (t == typeof(decimal)) name = "decimal";
            else if (t == typeof(double)) name = "double";
            else if (t == typeof(float)) name = "float";
            else if (t == typeof(DateTime)) name = "DateTime";
            else if (t == typeof(Guid)) name = "Guid";
            else name = t.Name;

            var isRefType = t == typeof(string) || (!t.IsValueType);

            if (isRefType)
            {
                if (useNullableRef)
                {
                    return IsNullable ? $"{name}?" : name;
                }
                return name;
            }

            if (IsNullable && name != "string")
            {
                return $"{name}?";
            }
            return name;
        }

        public static EnumMeta TryParseEnumFromComment(string columnName, string comment, string underlyingTypeName = "int")
        {
            if (string.IsNullOrWhiteSpace(comment)) return null;

            // 约定：列备注包含 (Enum:0=禁用,1=启用) 或 (Enum:0=Disabled;1=Enabled)
            var m = Regex.Match(comment, @"\(\s*Enum\s*:\s*(?<body>[^)]*?)\s*\)", RegexOptions.IgnoreCase);
            if (!m.Success) return null;

            var body = (m.Groups["body"].Value ?? string.Empty).Trim();
            if (body.Length == 0) return null;

            var enumMeta = new EnumMeta
            {
                EnumName = $"{ToPascalCase(columnName)}Enum",
                UnderlyingTypeName = string.IsNullOrWhiteSpace(underlyingTypeName) ? "int" : underlyingTypeName
            };

            foreach (var rawPart in SplitEnumBody(body))
            {
                var part = rawPart?.Trim();
                if (string.IsNullOrWhiteSpace(part)) continue;

                var idx = part.IndexOf('=');
                if (idx <= 0 || idx >= part.Length - 1) continue;

                var value = part.Substring(0, idx).Trim();
                var nameOrComment = part.Substring(idx + 1).Trim();
                if (value.Length == 0 || nameOrComment.Length == 0) continue;

                var item = new EnumItemMeta
                {
                    ValueLiteral = value,
                    // 备注里通常是“显示名”，我们把它也用于枚举项名（转成 PascalCase）
                    Name = ToSafeIdentifier(ToPascalCase(nameOrComment)),
                    Comment = nameOrComment
                };
                enumMeta.Items.Add(item);
            }

            if (enumMeta.Items.Count == 0) return null;
            return enumMeta;
        }

        private static IEnumerable<string> SplitEnumBody(string body)
        {
            // 支持 , ; | 换行 分隔
            return Regex.Split(body, @"\s*[,;|\r\n]+\s*");
        }

        private static string ToPascalCase(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return "Unknown";
            s = s.Trim();
            // 常见外键/字段：user_id / userId / UserId -> UserId
            s = Regex.Replace(s, @"[^a-zA-Z0-9]+", " ");
            var parts = s.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0) return "Unknown";
            var result = "";
            foreach (var p in parts)
            {
                if (p.Length == 0) continue;
                if (p.Length == 1) result += p.ToUpperInvariant();
                else result += char.ToUpperInvariant(p[0]) + p.Substring(1);
            }
            return result;
        }

        private static string ToSafeIdentifier(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return "Unknown";
            // 去掉不合法字符
            s = Regex.Replace(s, @"[^a-zA-Z0-9_]", "");
            if (s.Length == 0) return "Unknown";
            // C# 标识符不能以数字开头
            if (char.IsDigit(s[0])) s = "_" + s;
            return s;
        }
    }
}

