using System;

namespace SqlSugar.Tools.CodeGen
{
    public class LayerGenOptions
    {
        public string RootNamespacePrefix { get; set; } = "Demo";
        public string ProjectName { get; set; } = "Demo";

        public bool GenerateModel { get; set; } = true;
        public bool GenerateDal { get; set; } = true;
        public bool GenerateBll { get; set; } = true;
        public bool GenerateWebApi { get; set; } = true;
        public bool GenerateCommon { get; set; } = true;
        public bool GenerateAuth { get; set; } = true;

        public string ModelNamespace => $"{RootNamespacePrefix}.{ProjectName}.Model";
        public string DalNamespace => $"{RootNamespacePrefix}.{ProjectName}.DAL";
        public string BllNamespace => $"{RootNamespacePrefix}.{ProjectName}.BLL";
        public string CommonNamespace => $"{RootNamespacePrefix}.{ProjectName}.Common";
        public string WebApiNamespace => $"{RootNamespacePrefix}.{ProjectName}.WebApi";

        public string NormalizeName(string s)
        {
            return (s ?? string.Empty).Trim();
        }
    }
}

