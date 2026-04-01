using System.Collections.Generic;
using System.Linq;

namespace SqlSugar.Tools.CodeGen
{
    public class TableMeta
    {
        public string TableName { get; set; }
        public string TableComment { get; set; }
        public string EntityName { get; set; }
        public List<ColumnMeta> Columns { get; set; } = new List<ColumnMeta>();
        public List<ForeignKeyMeta> ForeignKeys { get; set; } = new List<ForeignKeyMeta>();

        public ColumnMeta PrimaryKey => Columns.FirstOrDefault(c => c.IsPrimaryKey) ?? Columns.FirstOrDefault();
    }
}

