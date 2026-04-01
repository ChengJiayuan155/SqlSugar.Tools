using System;

namespace SqlSugar.Tools.CodeGen
{
    public class LayerGenOptions
    {
        public string RootNamespacePrefix { get; set; } = "Demo";
        public string ProjectName { get; set; } = "Demo";

        /// <summary>
        /// 输出根目录（由宿主/前端传入；为空则由 UI 选择文件夹兜底）
        /// </summary>
        public string OutputPath { get; set; } = string.Empty;

        public bool GenerateModel { get; set; } = true;
        public bool GenerateDal { get; set; } = true;
        public bool GenerateBll { get; set; } = true;
        public bool GenerateWebApi { get; set; } = true;
        public bool GenerateCommon { get; set; } = true;
        public bool GenerateServices { get; set; } = true;
        public bool GenerateWebApiBase { get; set; } = true;
        public bool GenerateAuth { get; set; } = true;

        // WebApi 宿主/扩展（按模块生成，可在向导中勾选）
        public bool GenerateWebApiExtensions { get; set; } = true;
        public bool GenerateWebApiProgramSkeleton { get; set; } = false;
        public bool EnableSwagger { get; set; } = true;
        public bool EnableCors { get; set; } = true;
        public bool EnableJwtAuth { get; set; } = true;
        public bool EnableGlobalException { get; set; } = true;
        public bool EnablePaging { get; set; } = true;
        public bool EnableNLog { get; set; } = true;
        public bool GenerateNLogConfig { get; set; } = true;
        public bool GenerateAdminManagement { get; set; } = false;
        public bool GenerateAdminApi { get; set; } = false;
        public bool GenerateWebHost { get; set; } = false;

        public string ModelNamespace => $"{RootNamespacePrefix}.{ProjectName}.Model";
        public string DalNamespace => $"{RootNamespacePrefix}.{ProjectName}.DAL";
        public string BllNamespace => $"{RootNamespacePrefix}.{ProjectName}.BLL";
        public string CommonNamespace => $"{RootNamespacePrefix}.{ProjectName}.Common";
        public string WebApiNamespace => $"{RootNamespacePrefix}.{ProjectName}.WebApi";
        public string WebApiAdminNamespace => $"{RootNamespacePrefix}.{ProjectName}.WebApi.Admin";
        public string WebHostNamespace => $"{RootNamespacePrefix}.{ProjectName}.Web";

        public string NormalizeName(string s)
        {
            return (s ?? string.Empty).Trim();
        }
    }
}

