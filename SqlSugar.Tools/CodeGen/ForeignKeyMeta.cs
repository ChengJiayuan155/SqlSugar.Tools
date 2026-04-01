namespace SqlSugar.Tools.CodeGen
{
    public class ForeignKeyMeta
    {
        public string ColumnName { get; set; }
        public string RefTable { get; set; }
        public string RefColumn { get; set; }

        // 生成导航属性名（默认用引用表名）
        public string NavigationName { get; set; }
    }
}

