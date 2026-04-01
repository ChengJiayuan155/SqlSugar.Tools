using System.Collections.Generic;

namespace SqlSugar.Tools.CodeGen
{
    public class EnumMeta
    {
        public string EnumName { get; set; }
        public string UnderlyingTypeName { get; set; } = "int";
        public List<EnumItemMeta> Items { get; set; } = new List<EnumItemMeta>();
    }

    public class EnumItemMeta
    {
        public string Name { get; set; }
        public string ValueLiteral { get; set; }
        public string Comment { get; set; }
    }
}

