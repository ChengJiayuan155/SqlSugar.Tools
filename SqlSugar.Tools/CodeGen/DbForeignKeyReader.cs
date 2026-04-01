using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace SqlSugar.Tools.CodeGen
{
    public static class DbForeignKeyReader
    {
        // 说明：
        // - 优先使用数据库真实 FK（最准确）
        // - 不同数据库的系统表/视图不同，这里先做通用 ADO.NET + provider 特征判断 + SQL 分支
        // - 如果当前 provider 不支持，调用方再走 LogicalFkInfer
        public static List<ForeignKeyMeta> TryReadForeignKeys(IDbConnection conn, string tableName)
        {
            var result = new List<ForeignKeyMeta>();
            if (conn == null) return result;
            if (string.IsNullOrWhiteSpace(tableName)) return result;

            var providerName = conn.GetType().FullName ?? string.Empty;
            var dbType = GuessDbType(providerName, conn.ConnectionString);

            try
            {
                // 确保连接打开（SqlSugar 可能已打开；这里不强制关闭）
                if (conn.State != ConnectionState.Open) conn.Open();

                switch (dbType)
                {
                    case DbTypeGuess.SqlServer:
                        result.AddRange(ReadSqlServer(conn, tableName));
                        break;
                    case DbTypeGuess.MySql:
                        result.AddRange(ReadMySql(conn, tableName));
                        break;
                    case DbTypeGuess.PostgreSql:
                        result.AddRange(ReadPostgreSql(conn, tableName));
                        break;
                    case DbTypeGuess.Oracle:
                        result.AddRange(ReadOracle(conn, tableName));
                        break;
                    default:
                        break;
                }
            }
            catch
            {
                // 失败就返回空，让上层回退到逻辑外键
                return new List<ForeignKeyMeta>();
            }

            // 去重：同一列多个记录时保留第一条
            return result
                .GroupBy(x => (x.ColumnName ?? string.Empty).Trim(), StringComparer.OrdinalIgnoreCase)
                .Select(g => g.First())
                .ToList();
        }

        private static DbTypeGuess GuessDbType(string providerFullName, string connStr)
        {
            var s = (providerFullName ?? string.Empty).ToLowerInvariant();
            var c = (connStr ?? string.Empty).ToLowerInvariant();

            if (s.Contains("sqlclient") || s.Contains("microsoft.data.sqlclient") || c.Contains("data source=")) return DbTypeGuess.SqlServer;
            if (s.Contains("mysql") || c.Contains("server=") && c.Contains("uid=") && c.Contains("database=")) return DbTypeGuess.MySql;
            if (s.Contains("npgsql") || c.Contains("host=") && c.Contains("username=")) return DbTypeGuess.PostgreSql;
            if (s.Contains("oracle") || c.Contains("user id=") && c.Contains("data source=") && c.Contains("password=")) return DbTypeGuess.Oracle;

            return DbTypeGuess.Unknown;
        }

        private enum DbTypeGuess
        {
            Unknown = 0,
            SqlServer = 1,
            MySql = 2,
            PostgreSql = 3,
            Oracle = 4
        }

        private static IEnumerable<ForeignKeyMeta> ReadSqlServer(IDbConnection conn, string tableName)
        {
            // 支持 dbo.Table 或 Table
            var schema = "dbo";
            var pureTable = tableName;
            if (tableName.Contains("."))
            {
                var parts = tableName.Split('.');
                if (parts.Length == 2)
                {
                    schema = parts[0].Trim('[', ']', '"');
                    pureTable = parts[1].Trim('[', ']', '"');
                }
            }

            var sql = @"
SELECT
    parentCol.name AS ColumnName,
    refTab.name AS RefTable,
    refCol.name AS RefColumn
FROM sys.foreign_key_columns fkc
INNER JOIN sys.tables parentTab ON fkc.parent_object_id = parentTab.object_id
INNER JOIN sys.schemas parentSch ON parentTab.schema_id = parentSch.schema_id
INNER JOIN sys.columns parentCol ON parentCol.object_id = parentTab.object_id AND parentCol.column_id = fkc.parent_column_id
INNER JOIN sys.tables refTab ON fkc.referenced_object_id = refTab.object_id
INNER JOIN sys.columns refCol ON refCol.object_id = refTab.object_id AND refCol.column_id = fkc.referenced_column_id
WHERE parentSch.name = @schema AND parentTab.name = @table
";

            return Query(conn, sql,
                new Param("@schema", schema),
                new Param("@table", pureTable));
        }

        private static IEnumerable<ForeignKeyMeta> ReadMySql(IDbConnection conn, string tableName)
        {
            // 默认当前 schema/database
            var sql = @"
SELECT
  kcu.COLUMN_NAME AS ColumnName,
  kcu.REFERENCED_TABLE_NAME AS RefTable,
  kcu.REFERENCED_COLUMN_NAME AS RefColumn
FROM information_schema.KEY_COLUMN_USAGE kcu
WHERE kcu.TABLE_SCHEMA = DATABASE()
  AND kcu.TABLE_NAME = @table
  AND kcu.REFERENCED_TABLE_NAME IS NOT NULL
";
            return Query(conn, sql, new Param("@table", tableName));
        }

        private static IEnumerable<ForeignKeyMeta> ReadPostgreSql(IDbConnection conn, string tableName)
        {
            // 默认 public schema（简单实现，后续可扩展）
            // 参考 pg_constraint/pg_attribute
            var sql = @"
SELECT
  a.attname AS ColumnName,
  rt.relname AS RefTable,
  ra.attname AS RefColumn
FROM pg_constraint c
JOIN pg_class t ON c.conrelid = t.oid
JOIN pg_namespace n ON t.relnamespace = n.oid
JOIN pg_attribute a ON a.attrelid = t.oid AND a.attnum = ANY (c.conkey)
JOIN pg_class rt ON c.confrelid = rt.oid
JOIN pg_attribute ra ON ra.attrelid = rt.oid AND ra.attnum = ANY (c.confkey)
WHERE c.contype = 'f'
  AND n.nspname = 'public'
  AND t.relname = @table
";
            return Query(conn, sql, new Param("@table", tableName));
        }

        private static IEnumerable<ForeignKeyMeta> ReadOracle(IDbConnection conn, string tableName)
        {
            // 使用当前用户下的约束（简化）
            var sql = @"
SELECT
  cols.column_name AS ColumnName,
  rcols.table_name AS RefTable,
  rcols.column_name AS RefColumn
FROM user_constraints cons
JOIN user_cons_columns cols ON cons.constraint_name = cols.constraint_name
JOIN user_constraints rcons ON cons.r_constraint_name = rcons.constraint_name
JOIN user_cons_columns rcols ON rcons.constraint_name = rcols.constraint_name AND rcols.position = cols.position
WHERE cons.constraint_type = 'R'
  AND cons.table_name = :table
";
            // Oracle 常用 :param
            return Query(conn, sql, new Param(":table", tableName.ToUpperInvariant()));
        }

        private static IEnumerable<ForeignKeyMeta> Query(IDbConnection conn, string sql, params Param[] ps)
        {
            var list = new List<ForeignKeyMeta>();
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = sql;
                foreach (var p in ps ?? new Param[0])
                {
                    var dbp = cmd.CreateParameter();
                    dbp.ParameterName = p.Name;
                    dbp.Value = p.Value ?? DBNull.Value;
                    cmd.Parameters.Add(dbp);
                }

                using (var rd = cmd.ExecuteReader())
                {
                    while (rd.Read())
                    {
                        var col = rd["ColumnName"]?.ToString();
                        var rt = rd["RefTable"]?.ToString();
                        var rc = rd["RefColumn"]?.ToString();
                        if (string.IsNullOrWhiteSpace(col) || string.IsNullOrWhiteSpace(rt)) continue;

                        list.Add(new ForeignKeyMeta
                        {
                            ColumnName = col,
                            RefTable = rt,
                            RefColumn = string.IsNullOrWhiteSpace(rc) ? "Id" : rc,
                            NavigationName = rt
                        });
                    }
                }
            }
            return list;
        }

        private readonly struct Param
        {
            public Param(string name, object value)
            {
                Name = name;
                Value = value;
            }

            public string Name { get; }
            public object Value { get; }
        }
    }
}

