using Chromium.Event;
using NetDimension.NanUI;
using Newtonsoft.Json;
using SqlSugar.Tools.Model;
using SqlSugar.Tools.SQLHelper;
using SqlSugar.Tools.Tools;
using SqlSugar.Tools.CodeGen;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Web;

namespace SqlSugar.Tools
{
    public partial class CreateEntity : Formium
    {
        public static CreateEntity _CreateEntity = null;

        public CreateEntity()
            : base("http://my.resource.local/pages/CreateEntity.html")
        {
            InitializeComponent();
            this.MinimumSize = new Size(1100, 690);
            this.StartPosition = FormStartPosition.CenterParent;
            GlobalObject.AddFunction("exit").Execute += (func, args) =>
            {
                this.RequireUIThread(() =>
                {
                    this.Close();
                    GC.Collect();
                });
            };
            GlobalObject.AddFunction("openFolder").Execute += (func, args) =>
            {
                var path = ((args.Arguments.FirstOrDefault(p => p.IsString)?.StringValue) ?? string.Empty).Trim();
                if (!string.IsNullOrWhiteSpace(path) && Directory.Exists(path))
                {
                    Process.Start("explorer.exe", path);
                }
            };
            GlobalObject.AddFunction("selectFolder").Execute += (func, args) =>
            {
                var initPath = ((args.Arguments.FirstOrDefault(p => p.IsString)?.StringValue) ?? string.Empty).Trim();
                if (string.IsNullOrWhiteSpace(initPath))
                {
                    initPath = @"D:\Users\14440\Desktop";
                }
                if (!Directory.Exists(initPath))
                {
                    try { Directory.CreateDirectory(initPath); } catch { }
                }
                using (var folderBrowserDialog = new FolderBrowserDialog
                {
                    SelectedPath = initPath,
                    Description = "选择输出文件夹（将自动创建分层目录）"
                })
                {
                    if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
                    {
                        var selected = folderBrowserDialog.SelectedPath ?? string.Empty;
                        var safe = selected.Replace("\\", "\\\\").Replace("'", "\\'");
                        EvaluateJavascript($"setLayerOutputPath('{safe}')", (value, exception) => { });
                    }
                }
            };
            this.RegiestSQLServerFunc();
            this.RegiestSQLiteFunc();
            this.RegiestMySqlFunc();
            this.RegiestPGSqlFunc();
            this.RegiestOracleFunc();
            base.LoadHandler.OnLoadEnd += LoadHandler_OnLoadEnd;
        }

        /// <summary>
        /// 注册SQL Server数据库操作要用到的方法到JS
        /// </summary>
        private void RegiestSQLServerFunc()
        {
            var sqlServer = base.GlobalObject.AddObject("sqlServer");
            var testLink = sqlServer.AddFunction("testLink");    //测试数据库连接
            testLink.Execute += async (func, args) =>
            {
                var linkString = ((args.Arguments.FirstOrDefault(p => p.IsString)?.StringValue) ?? string.Empty).Trim();
                if (!string.IsNullOrWhiteSpace(linkString))
                {
                    try
                    {
                        if (await SQLServerHelper.TestLink(linkString))
                        {
                            EvaluateJavascript("testSuccessMsg()", (value, exception) => { });
                            var dbList = await SQLServerHelper.QueryDataTable(linkString, "select name from sysdatabases where dbid>4");
                            var dbListJson = JsonConvert.SerializeObject(dbList);
                            dbList.Clear(); dbList.Dispose(); dbList = null;
                            EvaluateJavascript($"setDbList('{dbListJson}')", (value, exception) => { });
                        }
                        else
                        {
                            MessageBox.Show("测试连接失败", "测试连接SQL Server", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, "测试连接SQL Server", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    finally
                    {
                        EvaluateJavascript("hideLoading()", (value, exception) => { });
                        GC.Collect();
                    }
                }
                else
                {
                    MessageBox.Show("获取数据库连接字符串错误", "测试连接SQL Server", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    EvaluateJavascript("hideLoading()", (value, exception) => { });
                }
            };

            var loadingTables = sqlServer.AddFunction("loadingTables");    //加载数据库的表
            loadingTables.Execute += async (func, args) =>
            {
                var linkString = ((args.Arguments.FirstOrDefault(p => p.IsString)?.StringValue) ?? string.Empty).Trim();
                if (!string.IsNullOrWhiteSpace(linkString))
                {
                    try
                    {
                        var tables = await this.LoadingTables(linkString, DataBaseType.SQLServer);
                        tables.Columns["TableName"].ColumnName = "label";
                        var tablesJson = JsonConvert.SerializeObject(tables).Replace("\r\n", "").Replace("\\r\\n", "").Replace("\\", "\\\\");
                        tables.Clear(); tables.Dispose(); tables = null;
                        EvaluateJavascript($"setTables('{tablesJson}')", (value, exception) => { });
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, "加载表", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    finally
                    {
                        EvaluateJavascript("hideLoading()", (value, exception) => { });
                        GC.Collect();
                    }
                }
                else
                {
                    MessageBox.Show("获取数据库连接字符串错误", "加载表", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    EvaluateJavascript("hideLoading()", (value, exception) => { });
                }
            };

            var createOne = sqlServer.AddFunction("createOne");    //生成一个表
            createOne.Execute += async (func, args) =>
            {
                var info = ((args.Arguments.FirstOrDefault(p => p.IsString)?.StringValue) ?? string.Empty).Trim();
                if (!string.IsNullOrWhiteSpace(info))
                {
                    try
                    {
                        var infos = JsonConvert.DeserializeObject<Dictionary<string, string>>(info);
                        var settings = JsonConvert.DeserializeObject<SettingsModel>(infos["settings"]);
                        var code = await this.GetEntityCode(infos["linkString"], infos["tableName"], infos["tableDesc"], settings, DataBaseType.SQLServer, true);
                        code = code
                            .Replace("\r\n", "<br/>")
                            .Replace("using ", "<span style=\"color:#CE04B0\">using </span>")
                            .Replace("namespace ", "<span style=\"color:#CE04B0\">namespace </span>")
                            .Replace("public ", "<span style=\"color:#CE04B0\">public </span>")
                            .Replace("private ", "<span style=\"color:#CE04B0\">private </span>")
                            .Replace("class ", "<span style=\"color:#CE04B0\">class </span>")
                            .Replace("get ", "<span style=\"color:#CE04B0\">get </span>")
                            .Replace("set ", "<span style=\"color:#CE04B0\">set </span>")
                            .Replace("get;", "<span style=\"color:#CE04B0\">get;</span>")
                            .Replace("set;", "<span style=\"color:#CE04B0\">set;</span>")
                            .Replace("return ", "<span style=\"color:#FF4500\">return </span>")
                            .Replace("this.", "<span style=\"color:#CE04B0\">this.</span>")
                            .Replace("SugarColumn", "<span style=\"color:red\">SugarColumn</span>")
                            .Replace("true", "<span style=\"color:#008B8B\">true</span>")
                            .Replace("??", "<span style=\"color:#E9D372\">??</span>")
                            .Replace("?.", "<span style=\"color:#E9D372\">?.</span>")
                            .Replace("default(", "<span style=\"color:#CE04B0\">default(</span>");
                        code = Regex.Replace(code, @"/// <summary>(?<str>.*?)/// </summary>", "<span style=\"color:green\">/// &lt;summary&gt;${str}/// &lt;/summary&gt;</span>");
			code = HttpUtility.JavaScriptStringEncode(code);
                        EvaluateJavascript($"getEntityCode('{code}')", (value, exception) => { });
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, "预览代码", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    finally
                    {
                        EvaluateJavascript("hideLoading()", (value, exception) => { });
                        GC.Collect();
                    }
                }
                else
                {
                    MessageBox.Show("获取数据库连接字符串错误", "预览代码", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    EvaluateJavascript("hideLoading()", (value, exception) => { });
                }
            };

            var saveOne = sqlServer.AddFunction("saveOne"); //保存单个实体类
            saveOne.Execute += async (func, args) =>
            {
                var info = ((args.Arguments.FirstOrDefault(p => p.IsString)?.StringValue) ?? string.Empty).Trim();
                if (!string.IsNullOrWhiteSpace(info))
                {
                    try
                    {
                        var infos = JsonConvert.DeserializeObject<Dictionary<string, string>>(info);
                        var settings = JsonConvert.DeserializeObject<SettingsModel>(infos["settings"]);
                        var code = await this.GetEntityCode(infos["linkString"], infos["tableName"], infos["tableDesc"], settings, DataBaseType.SQLServer, false);
                        using (var saveFileDialog = new SaveFileDialog()
                        {
                            DefaultExt = "cs",
                            Filter = "C#类(*.cs)|*.cs",
                            FileName = $"{(settings.ClassCapsCount > 0 ? infos["tableName"].SetLengthToUpperByStart((int)settings.ClassCapsCount) : infos["tableName"])}.cs",
                            RestoreDirectory = true,
                            Title = "保存单个实体类"
                        })
                        {
                            if (saveFileDialog.ShowDialog() == DialogResult.OK)
                            {
                                var localFilePath = saveFileDialog.FileName.ToString();
                                using (StreamWriter sw = new StreamWriter(localFilePath, false))
                                {
                                    await sw.WriteLineAsync(code);
                                }
                                EvaluateJavascript("saveOneSuccess()", (value, exception) => { });
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, "保存实体类", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    finally
                    {
                        EvaluateJavascript("hideLoading()", (value, exception) => { });
                        GC.Collect();
                    }
                }
                else
                {
                    MessageBox.Show("获取数据库连接字符串错误", "保存实体类", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    EvaluateJavascript("hideLoading()", (value, exception) => { });
                }
            };

            var saveAllTables = sqlServer.AddFunction("saveAllTables"); //保存所有表生成的实体类
            saveAllTables.Execute += async (func, args) =>
            {
                var info = ((args.Arguments.FirstOrDefault(p => p.IsString)?.StringValue) ?? string.Empty).Trim();
                if (!string.IsNullOrWhiteSpace(info))
                {
                    try
                    {
                        var initPath = "";
                        if (File.Exists($"{Environment.CurrentDirectory}\\default.ini"))
                        {
                            initPath = File.ReadAllText($"{Environment.CurrentDirectory}\\default.ini", Encoding.Default);
                        }
                        var infos = JsonConvert.DeserializeObject<Dictionary<string, string>>(info);
                        var settings = JsonConvert.DeserializeObject<SettingsModel>(infos["settings"]);
                        var tableList = JsonConvert.DeserializeObject<List<Dictionary<string, string>>>(infos["tableList"]);
                        using (var folderBrowserDialog = new FolderBrowserDialog { SelectedPath = initPath })
                        {
                            if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
                            {
                                foreach (var item in tableList)
                                {
                                    var code = await this.GetEntityCode(infos["linkString"], item["label"], item["TableDesc"], settings, DataBaseType.SQLServer, false);
                                    using (StreamWriter sw = new StreamWriter(folderBrowserDialog.SelectedPath + "\\" + (settings.ClassCapsCount > 0 ? item["label"].SetLengthToUpperByStart((int)settings.ClassCapsCount) : item["label"]) + ".cs"))
                                    {
                                        await sw.WriteAsync(code);
                                    }
                                }
                                using (StreamWriter sw = new StreamWriter($"{Environment.CurrentDirectory}\\default.ini"))
                                {
                                    await sw.WriteAsync(folderBrowserDialog.SelectedPath);
                                }
                                EvaluateJavascript("saveAllTablesSuccess()", (value, exception) => { });
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, "保存所有实体类", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    finally
                    {
                        EvaluateJavascript("hideLoading()", (value, exception) => { });
                        GC.Collect();
                    }
                }
                else
                {
                    MessageBox.Show("获取数据库连接字符串错误", "保存所有实体类", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    EvaluateJavascript("hideLoading()", (value, exception) => { });
                }
            };

            var generateLayers = sqlServer.AddFunction("generateLayers"); //分层生成（多表勾选）
            generateLayers.Execute += async (func, args) =>
            {
                var info = ((args.Arguments.FirstOrDefault(p => p.IsString)?.StringValue) ?? string.Empty).Trim();
                if (string.IsNullOrWhiteSpace(info))
                {
                    MessageBox.Show("获取数据库连接字符串错误", "分层生成", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    EvaluateJavascript("hideLoading()", (value, exception) => { });
                    return;
                }
                try
                {
                    await GenerateLayersInternal(info, DataBaseType.SQLServer);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "分层生成", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                finally
                {
                    EvaluateJavascript("hideLoading()", (value, exception) => { });
                    GC.Collect();
                }
            };
        }

        /// <summary>
        /// 注册SQLite数据库操作要用到的方法到JS
        /// </summary>
        private void RegiestSQLiteFunc()
        {
            var sqlite = base.GlobalObject.AddObject("sqlite");
            var selectDBFile = sqlite.AddFunction("selectDBFile");  //选择db文件方法
            selectDBFile.Execute += (func, args) =>
            {
                using(var openFileDialog = new OpenFileDialog
                {
                    Multiselect = false,
                    Title = "请选择SQLite文件",
                    Filter = "SQLite文件(*.db)|*.db|所有文件(*.*)|*.*"
                })
                {
                    if (openFileDialog.ShowDialog()== DialogResult.OK)
                    {
                        //string file = openFileDialog.FileName;//返回文件的完整路径
                        EvaluateJavascript($"setSQLiteFilePath('{openFileDialog.FileName.Replace("\\","\\\\")}')", (value, exception) => { });
                    }
                }
            };

            var testLink = sqlite.AddFunction("testLink");    //测试数据库连接
            testLink.Execute += async (func, args) =>
            {
                var linkString = ((args.Arguments.FirstOrDefault(p => p.IsString)?.StringValue) ?? string.Empty).Trim();
                if (!string.IsNullOrWhiteSpace(linkString))
                {
                    try
                    {
                        if (await SQLiteHelper.TestLink(linkString))
                        {
                            EvaluateJavascript("testSuccessMsg()", (value, exception) => { });
                        }
                        else
                        {
                            MessageBox.Show("测试连接失败", "测试连接SQLite", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, "测试连接SQLite", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    finally
                    {
                        EvaluateJavascript("hideLoading()", (value, exception) => { });
                        GC.Collect();
                    }
                }
                else
                {
                    MessageBox.Show("获取数据库连接字符串错误", "测试连接SQLite", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    EvaluateJavascript("hideLoading()", (value, exception) => { });
                }
            };

            var loadingTables = sqlite.AddFunction("loadingTables");    //加载数据库的表
            loadingTables.Execute += async (func, args) =>
            {
                var linkString = ((args.Arguments.FirstOrDefault(p => p.IsString)?.StringValue) ?? string.Empty).Trim();
                if (!string.IsNullOrWhiteSpace(linkString))
                {
                    try
                    {
                        var tables = await this.LoadingTables(linkString, DataBaseType.SQLite);
                        tables.Columns["name"].ColumnName = "label";
                        var tablesJson = JsonConvert.SerializeObject(tables);
                        tables.Clear(); tables.Dispose(); tables = null;
                        EvaluateJavascript($"setTables('{tablesJson}')", (value, exception) => { });
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, "加载表", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    finally
                    {
                        EvaluateJavascript("hideLoading()", (value, exception) => { });
                        GC.Collect();
                    }
                }
                else
                {
                    MessageBox.Show("获取数据库连接字符串错误", "加载表", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    EvaluateJavascript("hideLoading()", (value, exception) => { });
                }
            };

            var createOne = sqlite.AddFunction("createOne");    //生成一个表
            createOne.Execute += async (func, args) =>
            {
                var info = ((args.Arguments.FirstOrDefault(p => p.IsString)?.StringValue) ?? string.Empty).Trim();
                if (!string.IsNullOrWhiteSpace(info))
                {
                    try
                    {
                        var infos = JsonConvert.DeserializeObject<Dictionary<string, string>>(info);
                        var settings = JsonConvert.DeserializeObject<SettingsModel>(infos["settings"]);
                        var code = await this.GetEntityCode(infos["linkString"], infos["tableName"], infos["tableName"], settings, DataBaseType.SQLite, true);
                        code = code
                            .Replace("\r\n", "<br/>")
                            .Replace("using ", "<span style=\"color:#CE04B0\">using </span>")
                            .Replace("namespace ", "<span style=\"color:#CE04B0\">namespace </span>")
                            .Replace("public ", "<span style=\"color:#CE04B0\">public </span>")
                            .Replace("private ", "<span style=\"color:#CE04B0\">private </span>")
                            .Replace("class ", "<span style=\"color:#CE04B0\">class </span>")
                            .Replace("get ", "<span style=\"color:#CE04B0\">get </span>")
                            .Replace("set ", "<span style=\"color:#CE04B0\">set </span>")
                            .Replace("get;", "<span style=\"color:#CE04B0\">get;</span>")
                            .Replace("set;", "<span style=\"color:#CE04B0\">set;</span>")
                            .Replace("return ", "<span style=\"color:#FF4500\">return </span>")
                            .Replace("this.", "<span style=\"color:#CE04B0\">this.</span>")
                            .Replace("SugarColumn", "<span style=\"color:red\">SugarColumn</span>")
                            .Replace("true", "<span style=\"color:#008B8B\">true</span>")
                            .Replace("??", "<span style=\"color:#E9D372\">??</span>")
                            .Replace("?.", "<span style=\"color:#E9D372\">?.</span>")
                            .Replace("default(", "<span style=\"color:#CE04B0\">default(</span>");
                        code = Regex.Replace(code, @"/// <summary>(?<str>.*?)/// </summary>", "<span style=\"color:green\">/// &lt;summary&gt;${str}/// &lt;/summary&gt;</span>");
                        EvaluateJavascript($"getEntityCode('{code}')", (value, exception) => { });
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, "预览代码", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    finally
                    {
                        EvaluateJavascript("hideLoading()", (value, exception) => { });
                        GC.Collect();
                    }
                }
                else
                {
                    MessageBox.Show("获取数据库连接字符串错误", "预览代码", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    EvaluateJavascript("hideLoading()", (value, exception) => { });
                }
            };

            var saveOne = sqlite.AddFunction("saveOne"); //保存单个实体类
            saveOne.Execute += async (func, args) =>
            {
                var info = ((args.Arguments.FirstOrDefault(p => p.IsString)?.StringValue) ?? string.Empty).Trim();
                if (!string.IsNullOrWhiteSpace(info))
                {
                    try
                    {
                        var infos = JsonConvert.DeserializeObject<Dictionary<string, string>>(info);
                        var settings = JsonConvert.DeserializeObject<SettingsModel>(infos["settings"]);
                        var code = await this.GetEntityCode(infos["linkString"], infos["tableName"], infos["tableName"], settings, DataBaseType.SQLite, false);
                        using (var saveFileDialog = new SaveFileDialog()
                        {
                            DefaultExt = "cs",
                            Filter = "C#类(*.cs)|*.cs",
                            FileName = $"{(settings.ClassCapsCount > 0 ? infos["tableName"].SetLengthToUpperByStart((int)settings.ClassCapsCount) : infos["tableName"])}.cs",
                            RestoreDirectory = true,
                            Title = "保存单个实体类"
                        })
                        {
                            if (saveFileDialog.ShowDialog() == DialogResult.OK)
                            {
                                var localFilePath = saveFileDialog.FileName.ToString();
                                using (StreamWriter sw = new StreamWriter(localFilePath, false))
                                {
                                    await sw.WriteLineAsync(code);
                                }
                                EvaluateJavascript("saveOneSuccess()", (value, exception) => { });
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, "保存实体类", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    finally
                    {
                        EvaluateJavascript("hideLoading()", (value, exception) => { });
                        GC.Collect();
                    }
                }
                else
                {
                    MessageBox.Show("获取数据库连接字符串错误", "保存实体类", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    EvaluateJavascript("hideLoading()", (value, exception) => { });
                }
            };

            var saveAllTables = sqlite.AddFunction("saveAllTables"); //保存所有表生成的实体类
            saveAllTables.Execute += async (func, args) =>
            {
                var info = ((args.Arguments.FirstOrDefault(p => p.IsString)?.StringValue) ?? string.Empty).Trim();
                if (!string.IsNullOrWhiteSpace(info))
                {
                    try
                    {
                        var initPath = "";
                        if (File.Exists($"{Environment.CurrentDirectory}\\default.ini"))
                        {
                            initPath = File.ReadAllText($"{Environment.CurrentDirectory}\\default.ini", Encoding.Default);
                        }
                        var infos = JsonConvert.DeserializeObject<Dictionary<string, string>>(info);
                        var settings = JsonConvert.DeserializeObject<SettingsModel>(infos["settings"]);
                        var tableList = JsonConvert.DeserializeObject<List<Dictionary<string, string>>>(infos["tableList"]);
                        using (var folderBrowserDialog = new FolderBrowserDialog { SelectedPath = initPath })
                        {
                            if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
                            {
                                foreach (var item in tableList)
                                {
                                    var code = await this.GetEntityCode(infos["linkString"], item["label"], item["label"], settings, DataBaseType.SQLite, false);
                                    using (StreamWriter sw = new StreamWriter(folderBrowserDialog.SelectedPath + "\\" + (settings.ClassCapsCount > 0 ? item["label"].SetLengthToUpperByStart((int)settings.ClassCapsCount) : item["label"]) + ".cs"))
                                    {
                                        await sw.WriteAsync(code);
                                    }
                                }
                                using (StreamWriter sw = new StreamWriter($"{Environment.CurrentDirectory}\\default.ini"))
                                {
                                    await sw.WriteAsync(folderBrowserDialog.SelectedPath);
                                }
                                EvaluateJavascript("saveAllTablesSuccess()", (value, exception) => { });
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, "保存所有实体类", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    finally
                    {
                        EvaluateJavascript("hideLoading()", (value, exception) => { });
                        GC.Collect();
                    }
                }
                else
                {
                    MessageBox.Show("获取数据库连接字符串错误", "保存所有实体类", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    EvaluateJavascript("hideLoading()", (value, exception) => { });
                }
            };

            var generateLayers = sqlite.AddFunction("generateLayers"); //分层生成（多表勾选）
            generateLayers.Execute += async (func, args) =>
            {
                var info = ((args.Arguments.FirstOrDefault(p => p.IsString)?.StringValue) ?? string.Empty).Trim();
                if (string.IsNullOrWhiteSpace(info))
                {
                    MessageBox.Show("获取数据库连接字符串错误", "分层生成", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    EvaluateJavascript("hideLoading()", (value, exception) => { });
                    return;
                }
                try
                {
                    await GenerateLayersInternal(info, DataBaseType.SQLite);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "分层生成", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                finally
                {
                    EvaluateJavascript("hideLoading()", (value, exception) => { });
                    GC.Collect();
                }
            };
        }

        /// <summary>
        /// 注册MySql数据库操作要用到的方法到JS
        /// </summary>
        private void RegiestMySqlFunc()
        {
            var mysql = base.GlobalObject.AddObject("mysql");
            var testLink = mysql.AddFunction("testLink");    //测试数据库连接
            testLink.Execute += async (func, args) =>
            {
                var linkString = ((args.Arguments.FirstOrDefault(p => p.IsString)?.StringValue) ?? string.Empty).Trim();
                if (!string.IsNullOrWhiteSpace(linkString))
                {
                    try
                    {
                        if (await MySQLHelper.TestLink(linkString))
                        {
                            EvaluateJavascript("testSuccessMsg()", (value, exception) => { });
                            var dbList = await MySQLHelper.QueryDataTable(linkString, "SELECT `SCHEMA_NAME` as name  FROM `information_schema`.`SCHEMATA` order by `SCHEMA_NAME`");
                            var dbListJson = JsonConvert.SerializeObject(dbList);
                            dbList.Clear(); dbList.Dispose(); dbList = null;
                            EvaluateJavascript($"setDbList('{dbListJson}')", (value, exception) => { });
                        }
                        else
                        {
                            MessageBox.Show("测试连接失败", "测试连接MySql", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, "测试连接MySql", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    finally
                    {
                        EvaluateJavascript("hideLoading()", (value, exception) => { });
                        GC.Collect();
                    }
                }
                else
                {
                    MessageBox.Show("获取数据库连接字符串错误", "测试连接MySql", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    EvaluateJavascript("hideLoading()", (value, exception) => { });
                }
            };

            var loadingTables = mysql.AddFunction("loadingTables");    //加载数据库的表
            loadingTables.Execute += async (func, args) =>
            {
                var linkString = ((args.Arguments.FirstOrDefault(p => p.IsString)?.StringValue) ?? string.Empty).Trim();
                if (!string.IsNullOrWhiteSpace(linkString))
                {
                    try
                    {
                        var tables = await this.LoadingTables(linkString, DataBaseType.MySQL);
                        tables.Columns["TableName"].ColumnName = "label";
                        var tablesJson = JsonConvert.SerializeObject(tables).Replace("\r\n", "").Replace("\\r\\n", "").Replace("\\", "\\\\");
                        tables.Clear(); tables.Dispose(); tables = null;
                        EvaluateJavascript($"setTables('{tablesJson}')", (value, exception) => { });
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, "加载表", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    finally
                    {
                        EvaluateJavascript("hideLoading()", (value, exception) => { });
                        GC.Collect();
                    }
                }
                else
                {
                    MessageBox.Show("获取数据库连接字符串错误", "加载表", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    EvaluateJavascript("hideLoading()", (value, exception) => { });
                }
            };

            var createOne = mysql.AddFunction("createOne");    //生成一个表
            createOne.Execute += async (func, args) =>
            {
                var info = ((args.Arguments.FirstOrDefault(p => p.IsString)?.StringValue) ?? string.Empty).Trim();
                if (!string.IsNullOrWhiteSpace(info))
                {
                    try
                    {
                        var infos = JsonConvert.DeserializeObject<Dictionary<string, string>>(info);
                        var settings = JsonConvert.DeserializeObject<SettingsModel>(infos["settings"]);
                        var code = await this.GetEntityCode(infos["linkString"], infos["tableName"], infos["tableDesc"], settings, DataBaseType.MySQL, true);
                        code = code
                            .Replace("\r\n", "<br/>")
                            .Replace("using ", "<span style=\"color:#CE04B0\">using </span>")
                            .Replace("namespace ", "<span style=\"color:#CE04B0\">namespace </span>")
                            .Replace("public ", "<span style=\"color:#CE04B0\">public </span>")
                            .Replace("private ", "<span style=\"color:#CE04B0\">private </span>")
                            .Replace("class ", "<span style=\"color:#CE04B0\">class </span>")
                            .Replace("get ", "<span style=\"color:#CE04B0\">get </span>")
                            .Replace("set ", "<span style=\"color:#CE04B0\">set </span>")
                            .Replace("get;", "<span style=\"color:#CE04B0\">get;</span>")
                            .Replace("set;", "<span style=\"color:#CE04B0\">set;</span>")
                            .Replace("return ", "<span style=\"color:#FF4500\">return </span>")
                            .Replace("this.", "<span style=\"color:#CE04B0\">this.</span>")
                            .Replace("SugarColumn", "<span style=\"color:red\">SugarColumn</span>")
                            .Replace("true", "<span style=\"color:#008B8B\">true</span>")
                            .Replace("??", "<span style=\"color:#E9D372\">??</span>")
                            .Replace("?.", "<span style=\"color:#E9D372\">?.</span>")
                            .Replace("default(", "<span style=\"color:#CE04B0\">default(</span>");
                        code = Regex.Replace(code, @"/// <summary>(?<str>.*?)/// </summary>", "<span style=\"color:green\">/// &lt;summary&gt;${str}/// &lt;/summary&gt;</span>");
                        EvaluateJavascript($"getEntityCode('{code}')", (value, exception) => { });
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, "预览代码", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    finally
                    {
                        EvaluateJavascript("hideLoading()", (value, exception) => { });
                        GC.Collect();
                    }
                }
                else
                {
                    MessageBox.Show("获取数据库连接字符串错误", "预览代码", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    EvaluateJavascript("hideLoading()", (value, exception) => { });
                }
            };

            var saveOne = mysql.AddFunction("saveOne"); //保存单个实体类
            saveOne.Execute += async (func, args) =>
            {
                var info = ((args.Arguments.FirstOrDefault(p => p.IsString)?.StringValue) ?? string.Empty).Trim();
                if (!string.IsNullOrWhiteSpace(info))
                {
                    try
                    {
                        var infos = JsonConvert.DeserializeObject<Dictionary<string, string>>(info);
                        var settings = JsonConvert.DeserializeObject<SettingsModel>(infos["settings"]);
                        var code = await this.GetEntityCode(infos["linkString"], infos["tableName"], infos["tableDesc"], settings, DataBaseType.MySQL, false);
                        using (var saveFileDialog = new SaveFileDialog()
                        {
                            DefaultExt = "cs",
                            Filter = "C#类(*.cs)|*.cs",
                            FileName = $"{(settings.ClassCapsCount > 0 ? infos["tableName"].SetLengthToUpperByStart((int)settings.ClassCapsCount) : infos["tableName"])}.cs",
                            RestoreDirectory = true,
                            Title = "保存单个实体类"
                        })
                        {
                            if (saveFileDialog.ShowDialog() == DialogResult.OK)
                            {
                                var localFilePath = saveFileDialog.FileName.ToString();
                                using (StreamWriter sw = new StreamWriter(localFilePath, false))
                                {
                                    await sw.WriteLineAsync(code);
                                }
                                EvaluateJavascript("saveOneSuccess()", (value, exception) => { });
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, "保存实体类", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    finally
                    {
                        EvaluateJavascript("hideLoading()", (value, exception) => { });
                        GC.Collect();
                    }
                }
                else
                {
                    MessageBox.Show("获取数据库连接字符串错误", "保存实体类", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    EvaluateJavascript("hideLoading()", (value, exception) => { });
                }
            };

            var saveAllTables = mysql.AddFunction("saveAllTables"); //保存所有表生成的实体类
            saveAllTables.Execute += async (func, args) =>
            {
                var info = ((args.Arguments.FirstOrDefault(p => p.IsString)?.StringValue) ?? string.Empty).Trim();
                if (!string.IsNullOrWhiteSpace(info))
                {
                    try
                    {
                        var initPath = "";
                        if (File.Exists($"{Environment.CurrentDirectory}\\default.ini"))
                        {
                            initPath = File.ReadAllText($"{Environment.CurrentDirectory}\\default.ini", Encoding.Default);
                        }
                        var infos = JsonConvert.DeserializeObject<Dictionary<string, string>>(info);
                        var settings = JsonConvert.DeserializeObject<SettingsModel>(infos["settings"]);
                        var tableList = JsonConvert.DeserializeObject<List<Dictionary<string, string>>>(infos["tableList"]);
                        using (var folderBrowserDialog = new FolderBrowserDialog { SelectedPath = initPath })
                        {
                            if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
                            {
                                foreach (var item in tableList)
                                {
                                    var code = await this.GetEntityCode(infos["linkString"], item["label"], item["TableDesc"], settings, DataBaseType.MySQL, false);
                                    using (StreamWriter sw = new StreamWriter(folderBrowserDialog.SelectedPath + "\\" + (settings.ClassCapsCount > 0 ? item["label"].SetLengthToUpperByStart((int)settings.ClassCapsCount) : item["label"]) + ".cs"))
                                    {
                                        await sw.WriteAsync(code);
                                    }
                                }
                                using (StreamWriter sw = new StreamWriter($"{Environment.CurrentDirectory}\\default.ini"))
                                {
                                    await sw.WriteAsync(folderBrowserDialog.SelectedPath);
                                }
                                EvaluateJavascript("saveAllTablesSuccess()", (value, exception) => { });
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, "保存所有实体类", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    finally
                    {
                        EvaluateJavascript("hideLoading()", (value, exception) => { });
                        GC.Collect();
                    }
                }
                else
                {
                    MessageBox.Show("获取数据库连接字符串错误", "保存所有实体类", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    EvaluateJavascript("hideLoading()", (value, exception) => { });
                }
            };

            var generateLayers = mysql.AddFunction("generateLayers"); //分层生成（多表勾选）
            generateLayers.Execute += async (func, args) =>
            {
                var info = ((args.Arguments.FirstOrDefault(p => p.IsString)?.StringValue) ?? string.Empty).Trim();
                if (string.IsNullOrWhiteSpace(info))
                {
                    MessageBox.Show("获取数据库连接字符串错误", "分层生成", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    EvaluateJavascript("hideLoading()", (value, exception) => { });
                    return;
                }
                try
                {
                    await GenerateLayersInternal(info, DataBaseType.MySQL);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "分层生成", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                finally
                {
                    EvaluateJavascript("hideLoading()", (value, exception) => { });
                    GC.Collect();
                }
            };
        }

        /// <summary>
        /// 注册pgSql数据库操作要用到的方法到JS
        /// </summary>
        private void RegiestPGSqlFunc()
        {
            var pgsql = base.GlobalObject.AddObject("pgsql");
            var testLink = pgsql.AddFunction("testLink");    //测试数据库连接
            testLink.Execute += async (func, args) =>
            {
                var linkString = ((args.Arguments.FirstOrDefault(p => p.IsString)?.StringValue) ?? string.Empty).Trim();
                if (!string.IsNullOrWhiteSpace(linkString))
                {
                    try
                    {
                        if (await PostgreSqlHelper.TestLink(linkString))
                        {
                            EvaluateJavascript("testSuccessMsg()", (value, exception) => { });
                        }
                        else
                        {
                            MessageBox.Show("测试连接失败", "测试连接PostgreSQL", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, "测试连接PostgreSQL", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    finally
                    {
                        EvaluateJavascript("hideLoading()", (value, exception) => { });
                        GC.Collect();
                    }
                }
                else
                {
                    MessageBox.Show("获取数据库连接字符串错误", "测试连接PostgreSQL", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    EvaluateJavascript("hideLoading()", (value, exception) => { });
                }
            };

            var loadingTables = pgsql.AddFunction("loadingTables");    //加载数据库的表
            loadingTables.Execute += async (func, args) =>
            {
                var linkString = ((args.Arguments.FirstOrDefault(p => p.IsString)?.StringValue) ?? string.Empty).Trim();
                if (!string.IsNullOrWhiteSpace(linkString))
                {
                    try
                    {
                        var tables = await this.LoadingTables(linkString, DataBaseType.PostgreSQL);
                        tables.Columns["TableName"].ColumnName = "label";
                        tables.Columns["tabledesc"].ColumnName = "TableDesc";
                        var tablesJson = JsonConvert.SerializeObject(tables).Replace("\r\n", "").Replace("\\r\\n", "").Replace("\\", "\\\\");
                        tables.Clear(); tables.Dispose(); tables = null;
                        EvaluateJavascript($"setTables('{tablesJson}')", (value, exception) => { });
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, "加载表", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    finally
                    {
                        EvaluateJavascript("hideLoading()", (value, exception) => { });
                        GC.Collect();
                    }
                }
                else
                {
                    MessageBox.Show("获取数据库连接字符串错误", "加载表", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    EvaluateJavascript("hideLoading()", (value, exception) => { });
                }
            };

            var createOne = pgsql.AddFunction("createOne");    //生成一个表
            createOne.Execute += async (func, args) =>
            {
                var info = ((args.Arguments.FirstOrDefault(p => p.IsString)?.StringValue) ?? string.Empty).Trim();
                if (!string.IsNullOrWhiteSpace(info))
                {
                    try
                    {
                        var infos = JsonConvert.DeserializeObject<Dictionary<string, string>>(info);
                        var settings = JsonConvert.DeserializeObject<SettingsModel>(infos["settings"]);
                        var code = await this.GetEntityCode(infos["linkString"], infos["tableName"], infos["tableDesc"], settings, DataBaseType.PostgreSQL, true);
                        code = code
                            .Replace("\r\n", "<br/>")
                            .Replace("using ", "<span style=\"color:#CE04B0\">using </span>")
                            .Replace("namespace ", "<span style=\"color:#CE04B0\">namespace </span>")
                            .Replace("public ", "<span style=\"color:#CE04B0\">public </span>")
                            .Replace("private ", "<span style=\"color:#CE04B0\">private </span>")
                            .Replace("class ", "<span style=\"color:#CE04B0\">class </span>")
                            .Replace("get ", "<span style=\"color:#CE04B0\">get </span>")
                            .Replace("set ", "<span style=\"color:#CE04B0\">set </span>")
                            .Replace("get;", "<span style=\"color:#CE04B0\">get;</span>")
                            .Replace("set;", "<span style=\"color:#CE04B0\">set;</span>")
                            .Replace("return ", "<span style=\"color:#FF4500\">return </span>")
                            .Replace("this.", "<span style=\"color:#CE04B0\">this.</span>")
                            .Replace("SugarColumn", "<span style=\"color:red\">SugarColumn</span>")
                            .Replace("true", "<span style=\"color:#008B8B\">true</span>")
                            .Replace("??", "<span style=\"color:#E9D372\">??</span>")
                            .Replace("?.", "<span style=\"color:#E9D372\">?.</span>")
                            .Replace("default(", "<span style=\"color:#CE04B0\">default(</span>");
                        code = Regex.Replace(code, @"/// <summary>(?<str>.*?)/// </summary>", "<span style=\"color:green\">/// &lt;summary&gt;${str}/// &lt;/summary&gt;</span>");
                        EvaluateJavascript($"getEntityCode('{code}')", (value, exception) => { });
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, "预览代码", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    finally
                    {
                        EvaluateJavascript("hideLoading()", (value, exception) => { });
                        GC.Collect();
                    }
                }
                else
                {
                    MessageBox.Show("获取数据库连接字符串错误", "预览代码", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    EvaluateJavascript("hideLoading()", (value, exception) => { });
                }
            };

            var saveOne = pgsql.AddFunction("saveOne"); //保存单个实体类
            saveOne.Execute += async (func, args) =>
            {
                var info = ((args.Arguments.FirstOrDefault(p => p.IsString)?.StringValue) ?? string.Empty).Trim();
                if (!string.IsNullOrWhiteSpace(info))
                {
                    try
                    {
                        var infos = JsonConvert.DeserializeObject<Dictionary<string, string>>(info);
                        var settings = JsonConvert.DeserializeObject<SettingsModel>(infos["settings"]);
                        var code = await this.GetEntityCode(infos["linkString"], infos["tableName"], infos["tableDesc"], settings, DataBaseType.PostgreSQL, false);
                        using (var saveFileDialog = new SaveFileDialog()
                        {
                            DefaultExt = "cs",
                            Filter = "C#类(*.cs)|*.cs",
                            FileName = $"{(settings.ClassCapsCount > 0 ? infos["tableName"].SetLengthToUpperByStart((int)settings.ClassCapsCount) : infos["tableName"])}.cs",
                            RestoreDirectory = true,
                            Title = "保存单个实体类"
                        })
                        {
                            if (saveFileDialog.ShowDialog() == DialogResult.OK)
                            {
                                var localFilePath = saveFileDialog.FileName.ToString();
                                using (StreamWriter sw = new StreamWriter(localFilePath, false))
                                {
                                    await sw.WriteLineAsync(code);
                                }
                                EvaluateJavascript("saveOneSuccess()", (value, exception) => { });
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, "保存实体类", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    finally
                    {
                        EvaluateJavascript("hideLoading()", (value, exception) => { });
                        GC.Collect();
                    }
                }
                else
                {
                    MessageBox.Show("获取数据库连接字符串错误", "保存实体类", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    EvaluateJavascript("hideLoading()", (value, exception) => { });
                }
            };

            var saveAllTables = pgsql.AddFunction("saveAllTables"); //保存所有表生成的实体类
            saveAllTables.Execute += async (func, args) =>
            {
                var info = ((args.Arguments.FirstOrDefault(p => p.IsString)?.StringValue) ?? string.Empty).Trim();
                if (!string.IsNullOrWhiteSpace(info))
                {
                    try
                    {
                        var initPath = "";
                        if (File.Exists($"{Environment.CurrentDirectory}\\default.ini"))
                        {
                            initPath = File.ReadAllText($"{Environment.CurrentDirectory}\\default.ini", Encoding.Default);
                        }
                        var infos = JsonConvert.DeserializeObject<Dictionary<string, string>>(info);
                        var settings = JsonConvert.DeserializeObject<SettingsModel>(infos["settings"]);
                        var tableList = JsonConvert.DeserializeObject<List<Dictionary<string, string>>>(infos["tableList"]);
                        using (var folderBrowserDialog = new FolderBrowserDialog { SelectedPath = initPath })
                        {
                            if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
                            {
                                foreach (var item in tableList)
                                {
                                    var code = await this.GetEntityCode(infos["linkString"], item["label"], item["TableDesc"], settings, DataBaseType.PostgreSQL, false);
                                    using (StreamWriter sw = new StreamWriter(folderBrowserDialog.SelectedPath + "\\" + (settings.ClassCapsCount > 0 ? item["label"].SetLengthToUpperByStart((int)settings.ClassCapsCount) : item["label"]) + ".cs"))
                                    {
                                        await sw.WriteAsync(code);
                                    }
                                }
                                using (StreamWriter sw = new StreamWriter($"{Environment.CurrentDirectory}\\default.ini"))
                                {
                                    await sw.WriteAsync(folderBrowserDialog.SelectedPath);
                                }
                                EvaluateJavascript("saveAllTablesSuccess()", (value, exception) => { });
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, "保存所有实体类", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    finally
                    {
                        EvaluateJavascript("hideLoading()", (value, exception) => { });
                        GC.Collect();
                    }
                }
                else
                {
                    MessageBox.Show("获取数据库连接字符串错误", "保存所有实体类", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    EvaluateJavascript("hideLoading()", (value, exception) => { });
                }
            };

            var generateLayers = pgsql.AddFunction("generateLayers"); //分层生成（多表勾选）
            generateLayers.Execute += async (func, args) =>
            {
                var info = ((args.Arguments.FirstOrDefault(p => p.IsString)?.StringValue) ?? string.Empty).Trim();
                if (string.IsNullOrWhiteSpace(info))
                {
                    MessageBox.Show("获取数据库连接字符串错误", "分层生成", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    EvaluateJavascript("hideLoading()", (value, exception) => { });
                    return;
                }
                try
                {
                    await GenerateLayersInternal(info, DataBaseType.PostgreSQL);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "分层生成", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                finally
                {
                    EvaluateJavascript("hideLoading()", (value, exception) => { });
                    GC.Collect();
                }
            };
        }

        /// <summary>
        /// 注册Oracle数据库操作要用到的方法到JS
        /// </summary>
        private void RegiestOracleFunc()
        {
            var oracle = base.GlobalObject.AddObject("oracle");
            var testLink = oracle.AddFunction("testLink");    //测试数据库连接
            testLink.Execute += async (func, args) =>
            {
                var linkString = ((args.Arguments.FirstOrDefault(p => p.IsString)?.StringValue) ?? string.Empty).Trim();
                if (!string.IsNullOrWhiteSpace(linkString))
                {
                    try
                    {
                        if (await OracleHelper.TestLink(linkString))
                        {
                            EvaluateJavascript("testSuccessMsg()", (value, exception) => { });
                        }
                        else
                        {
                            MessageBox.Show("测试连接失败", "测试连接Oracle", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, "测试连接Oracle", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    finally
                    {
                        EvaluateJavascript("hideLoading()", (value, exception) => { });
                        GC.Collect();
                    }
                }
                else
                {
                    MessageBox.Show("获取数据库连接字符串错误", "测试连接Oracle", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    EvaluateJavascript("hideLoading()", (value, exception) => { });
                }
            };

            var loadingTables = oracle.AddFunction("loadingTables");    //加载数据库的表
            loadingTables.Execute += async (func, args) =>
            {
                var linkString = ((args.Arguments.FirstOrDefault(p => p.IsString)?.StringValue) ?? string.Empty).Trim();
                if (!string.IsNullOrWhiteSpace(linkString))
                {
                    try
                    {
                        var tables = await this.LoadingTables(linkString, DataBaseType.Oracler);
                        tables.Columns["TableName"].ColumnName = "label";
                        tables.Columns["tabledesc"].ColumnName = "TableDesc";
                        var tablesJson = JsonConvert.SerializeObject(tables).Replace("\r\n", "").Replace("\\r\\n", "").Replace("\\", "\\\\");
                        tables.Clear(); tables.Dispose(); tables = null;
                        EvaluateJavascript($"setTables('{tablesJson}')", (value, exception) => { });
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, "加载表", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    finally
                    {
                        EvaluateJavascript("hideLoading()", (value, exception) => { });
                        GC.Collect();
                    }
                }
                else
                {
                    MessageBox.Show("获取数据库连接字符串错误", "加载表", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    EvaluateJavascript("hideLoading()", (value, exception) => { });
                }
            };

            var createOne = oracle.AddFunction("createOne");    //生成一个表
            createOne.Execute += async (func, args) =>
            {
                var info = ((args.Arguments.FirstOrDefault(p => p.IsString)?.StringValue) ?? string.Empty).Trim();
                if (!string.IsNullOrWhiteSpace(info))
                {
                    try
                    {
                        var infos = JsonConvert.DeserializeObject<Dictionary<string, string>>(info);
                        var settings = JsonConvert.DeserializeObject<SettingsModel>(infos["settings"]);
                        var code = await this.GetEntityCode(infos["linkString"], infos["tableName"], infos["tableDesc"], settings, DataBaseType.Oracler, true);
                        code = code
                            .Replace("\r\n", "<br/>")
                            .Replace("using ", "<span style=\"color:#CE04B0\">using </span>")
                            .Replace("namespace ", "<span style=\"color:#CE04B0\">namespace </span>")
                            .Replace("public ", "<span style=\"color:#CE04B0\">public </span>")
                            .Replace("private ", "<span style=\"color:#CE04B0\">private </span>")
                            .Replace("class ", "<span style=\"color:#CE04B0\">class </span>")
                            .Replace("get ", "<span style=\"color:#CE04B0\">get </span>")
                            .Replace("set ", "<span style=\"color:#CE04B0\">set </span>")
                            .Replace("get;", "<span style=\"color:#CE04B0\">get;</span>")
                            .Replace("set;", "<span style=\"color:#CE04B0\">set;</span>")
                            .Replace("return ", "<span style=\"color:#FF4500\">return </span>")
                            .Replace("this.", "<span style=\"color:#CE04B0\">this.</span>")
                            .Replace("SugarColumn", "<span style=\"color:red\">SugarColumn</span>")
                            .Replace("true", "<span style=\"color:#008B8B\">true</span>")
                            .Replace("??", "<span style=\"color:#E9D372\">??</span>")
                            .Replace("?.", "<span style=\"color:#E9D372\">?.</span>")
                            .Replace("default(", "<span style=\"color:#CE04B0\">default(</span>");
                        code = Regex.Replace(code, @"/// <summary>(?<str>.*?)/// </summary>", "<span style=\"color:green\">/// &lt;summary&gt;${str}/// &lt;/summary&gt;</span>");
                        EvaluateJavascript($"getEntityCode('{code}')", (value, exception) => { });
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, "预览代码", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    finally
                    {
                        EvaluateJavascript("hideLoading()", (value, exception) => { });
                        GC.Collect();
                    }
                }
                else
                {
                    MessageBox.Show("获取数据库连接字符串错误", "预览代码", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    EvaluateJavascript("hideLoading()", (value, exception) => { });
                }
            };

            var saveOne = oracle.AddFunction("saveOne"); //保存单个实体类
            saveOne.Execute += async (func, args) =>
            {
                var info = ((args.Arguments.FirstOrDefault(p => p.IsString)?.StringValue) ?? string.Empty).Trim();
                if (!string.IsNullOrWhiteSpace(info))
                {
                    try
                    {
                        var infos = JsonConvert.DeserializeObject<Dictionary<string, string>>(info);
                        var settings = JsonConvert.DeserializeObject<SettingsModel>(infos["settings"]);
                        var code = await this.GetEntityCode(infos["linkString"], infos["tableName"], infos["tableDesc"], settings, DataBaseType.Oracler, false);
                        using (var saveFileDialog = new SaveFileDialog()
                        {
                            DefaultExt = "cs",
                            Filter = "C#类(*.cs)|*.cs",
                            FileName = $"{(settings.ClassCapsCount > 0 ? infos["tableName"].SetLengthToUpperByStart((int)settings.ClassCapsCount) : infos["tableName"])}.cs",
                            RestoreDirectory = true,
                            Title = "保存单个实体类"
                        })
                        {
                            if (saveFileDialog.ShowDialog() == DialogResult.OK)
                            {
                                var localFilePath = saveFileDialog.FileName.ToString();
                                using (StreamWriter sw = new StreamWriter(localFilePath, false))
                                {
                                    await sw.WriteLineAsync(code);
                                }
                                EvaluateJavascript("saveOneSuccess()", (value, exception) => { });
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, "保存实体类", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    finally
                    {
                        EvaluateJavascript("hideLoading()", (value, exception) => { });
                        GC.Collect();
                    }
                }
                else
                {
                    MessageBox.Show("获取数据库连接字符串错误", "保存实体类", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    EvaluateJavascript("hideLoading()", (value, exception) => { });
                }
            };

            var saveAllTables = oracle.AddFunction("saveAllTables"); //保存所有表生成的实体类
            saveAllTables.Execute += async (func, args) =>
            {
                var info = ((args.Arguments.FirstOrDefault(p => p.IsString)?.StringValue) ?? string.Empty).Trim();
                if (!string.IsNullOrWhiteSpace(info))
                {
                    try
                    {
                        var initPath = "";
                        if (File.Exists($"{Environment.CurrentDirectory}\\default.ini"))
                        {
                            initPath = File.ReadAllText($"{Environment.CurrentDirectory}\\default.ini", Encoding.Default);
                        }
                        var infos = JsonConvert.DeserializeObject<Dictionary<string, string>>(info);
                        var settings = JsonConvert.DeserializeObject<SettingsModel>(infos["settings"]);
                        var tableList = JsonConvert.DeserializeObject<List<Dictionary<string, string>>>(infos["tableList"]);
                        using (var folderBrowserDialog = new FolderBrowserDialog { SelectedPath = initPath })
                        {
                            if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
                            {
                                foreach (var item in tableList)
                                {
                                    var code = await this.GetEntityCode(infos["linkString"], item["label"], item["TableDesc"], settings, DataBaseType.Oracler, false);
                                    using (StreamWriter sw = new StreamWriter(folderBrowserDialog.SelectedPath + "\\" + (settings.ClassCapsCount > 0 ? item["label"].SetLengthToUpperByStart((int)settings.ClassCapsCount) : item["label"]) + ".cs"))
                                    {
                                        await sw.WriteAsync(code);
                                    }
                                }
                                using (StreamWriter sw = new StreamWriter($"{Environment.CurrentDirectory}\\default.ini"))
                                {
                                    await sw.WriteAsync(folderBrowserDialog.SelectedPath);
                                }
                                EvaluateJavascript("saveAllTablesSuccess()", (value, exception) => { });
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, "保存所有实体类", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    finally
                    {
                        EvaluateJavascript("hideLoading()", (value, exception) => { });
                        GC.Collect();
                    }
                }
                else
                {
                    MessageBox.Show("获取数据库连接字符串错误", "保存所有实体类", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    EvaluateJavascript("hideLoading()", (value, exception) => { });
                }
            };

            var generateLayers = oracle.AddFunction("generateLayers"); //分层生成（多表勾选）
            generateLayers.Execute += async (func, args) =>
            {
                var info = ((args.Arguments.FirstOrDefault(p => p.IsString)?.StringValue) ?? string.Empty).Trim();
                if (string.IsNullOrWhiteSpace(info))
                {
                    MessageBox.Show("获取数据库连接字符串错误", "分层生成", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    EvaluateJavascript("hideLoading()", (value, exception) => { });
                    return;
                }
                try
                {
                    await GenerateLayersInternal(info, DataBaseType.Oracler);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "分层生成", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                finally
                {
                    EvaluateJavascript("hideLoading()", (value, exception) => { });
                    GC.Collect();
                }
            };
        }

        private async Task GenerateLayersInternal(string info, DataBaseType dbType)
        {
            var initPath = "";
            if (File.Exists($"{Environment.CurrentDirectory}\\default.ini"))
            {
                initPath = File.ReadAllText($"{Environment.CurrentDirectory}\\default.ini", Encoding.Default);
            }
            if (string.IsNullOrWhiteSpace(initPath))
            {
                // 兜底默认路径（按用户要求）
                initPath = @"D:\Users\14440\Desktop";
            }

            var infos = JsonConvert.DeserializeObject<Dictionary<string, string>>(info);
            var settings = JsonConvert.DeserializeObject<SettingsModel>(infos["settings"]);
            var layer = JsonConvert.DeserializeObject<Dictionary<string, object>>(infos["layer"]);
            var tableList = JsonConvert.DeserializeObject<List<Dictionary<string, string>>>(infos["tableList"]);

            var opt = new LayerGenOptions
            {
                RootNamespacePrefix = (layer.ContainsKey("rootNamespacePrefix") ? Convert.ToString(layer["rootNamespacePrefix"]) : "Company"),
                ProjectName = (layer.ContainsKey("projectName") ? Convert.ToString(layer["projectName"]) : "Demo"),
                OutputPath = (layer.ContainsKey("outputPath") ? Convert.ToString(layer["outputPath"]) : string.Empty),
                GenerateModel = layer.ContainsKey("generateModel") && Convert.ToBoolean(layer["generateModel"]),
                GenerateCommon = layer.ContainsKey("generateCommon") && Convert.ToBoolean(layer["generateCommon"]),
                GenerateDal = layer.ContainsKey("generateDal") && Convert.ToBoolean(layer["generateDal"]),
                GenerateBll = layer.ContainsKey("generateBll") && Convert.ToBoolean(layer["generateBll"]),
                GenerateWebApi = layer.ContainsKey("generateWebApi") && Convert.ToBoolean(layer["generateWebApi"]),
                GenerateServices = layer.ContainsKey("generateServices") && Convert.ToBoolean(layer["generateServices"]),
                GenerateWebApiBase = layer.ContainsKey("generateWebApiBase") && Convert.ToBoolean(layer["generateWebApiBase"]),
                GenerateWebApiExtensions = !layer.ContainsKey("generateWebApiExtensions") || Convert.ToBoolean(layer["generateWebApiExtensions"]),
                GenerateWebApiProgramSkeleton = layer.ContainsKey("generateWebApiProgramSkeleton") && Convert.ToBoolean(layer["generateWebApiProgramSkeleton"]),
                EnableSwagger = !layer.ContainsKey("enableSwagger") || Convert.ToBoolean(layer["enableSwagger"]),
                EnableCors = !layer.ContainsKey("enableCors") || Convert.ToBoolean(layer["enableCors"]),
                EnableJwtAuth = !layer.ContainsKey("enableJwtAuth") || Convert.ToBoolean(layer["enableJwtAuth"]),
                EnableGlobalException = !layer.ContainsKey("enableGlobalException") || Convert.ToBoolean(layer["enableGlobalException"]),
                EnablePaging = !layer.ContainsKey("enablePaging") || Convert.ToBoolean(layer["enablePaging"]),
                EnableNLog = !layer.ContainsKey("enableNLog") || Convert.ToBoolean(layer["enableNLog"]),
                GenerateNLogConfig = !layer.ContainsKey("generateNLogConfig") || Convert.ToBoolean(layer["generateNLogConfig"]),
                GenerateAuth = false
            };

            var outputBase = (opt.OutputPath ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(outputBase))
            {
                using (var folderBrowserDialog = new FolderBrowserDialog { SelectedPath = initPath, Description = "选择输出文件夹（将自动创建分层目录）" })
                {
                    if (folderBrowserDialog.ShowDialog() != DialogResult.OK)
                    {
                        return;
                    }
                    outputBase = folderBrowserDialog.SelectedPath;
                }
            }

            // 先创建“项目根目录”（按用户要求：outputPath\projectName\ 下再生成 6 层）
            var projectRoot = Path.Combine(outputBase, opt.ProjectName);
            Directory.CreateDirectory(projectRoot);

            {
                var entityNames = new List<string>();
                var filesCount = 0;

                // 1) Model：按 generat/temp + 每表一个目录输出
                if (opt.GenerateModel)
                {
                    var modelRoot = Path.Combine(projectRoot, opt.ModelNamespace);
                    var modelGeneratRoot = Path.Combine(modelRoot, "generat");
                    var modelTempRoot = Path.Combine(modelRoot, "temp");

                    foreach (var t in tableList)
                    {
                        var rawTableName = t.ContainsKey("label") ? t["label"] : "";
                        var tableDesc = t.ContainsKey("TableDesc") ? t["TableDesc"] : "";
                        var entityName = (settings.ClassCapsCount > 0 ? rawTableName.SetLengthToUpperByStart((int)settings.ClassCapsCount) : rawTableName);
                        entityNames.Add(entityName);

                        var modelClassName = $"Model{entityName}";

                        Directory.CreateDirectory(Path.Combine(modelGeneratRoot, entityName));
                        Directory.CreateDirectory(Path.Combine(modelTempRoot, entityName));

                        var settingsCopy = JsonConvert.DeserializeObject<SettingsModel>(JsonConvert.SerializeObject(settings));
                        settingsCopy.EntityNamespace = opt.ModelNamespace;

                        var code = await this.GetEntityCode(infos["linkString"], rawTableName, tableDesc, settingsCopy, dbType, false);

                        // 把原本的 public class XXX 改成 public partial class ModelXXX，并修正构造函数名
                        code = Regex.Replace(code, @"public\s+class\s+" + Regex.Escape(entityName) + @"\b", $"public partial class {modelClassName}");
                        code = Regex.Replace(code, @"public\s+" + Regex.Escape(entityName) + @"\s*\(", $"public {modelClassName}(");

                        var modelMappingPath = Path.Combine(modelGeneratRoot, entityName, $"{modelClassName}.cs");
                        using (var sw = new StreamWriter(modelMappingPath, false, Encoding.UTF8))
                        {
                            await sw.WriteAsync(code ?? string.Empty);
                        }
                        filesCount++;

                        // partial（扩展字段）
                        var partialPath = Path.Combine(modelTempRoot, entityName, $"{modelClassName}_partial.cs");
                        var partialContent =
$@"using System;
using SqlSugar;

namespace {opt.ModelNamespace}
{{
    // 本文件用于扩展/导航/枚举等（temp：不参与数据库映射）
    public partial class {modelClassName}
    {{
    }}
}}";
                        using (var sw = new StreamWriter(partialPath, false, Encoding.UTF8))
                        {
                            await sw.WriteAsync(partialContent);
                        }
                        filesCount++;

                        // search（后台搜索类）
                        var searchPath = Path.Combine(modelTempRoot, entityName, $"{modelClassName}_search.cs");
                        var searchContent =
$@"using System;

namespace {opt.ModelNamespace}
{{
    // 本文件用于后台搜索/筛选参数（temp：由模板/业务补齐）
    public partial class {modelClassName}_search
    {{
    }}
}}";
                        using (var sw = new StreamWriter(searchPath, false, Encoding.UTF8))
                        {
                            await sw.WriteAsync(searchContent);
                        }
                        filesCount++;
                    }
                }

                // 2) scaffold：Common/DAL/BLL/WebApi
                var files = new List<GeneratedFile>();
                files.AddRange(LayerScaffoldBuilder.BuildCommonAndDalScaffold(opt));
                if (opt.GenerateBll || opt.GenerateWebApi)
                {
                    files.AddRange(LayerScaffoldBuilder.BuildBllAndWebApiForEntities(opt, entityNames));
                }
                files.AddRange(BuildWebApiAdditionalFiles(opt, entityNames));

                foreach (var f in files)
                {
                    var rel = f.RelativePath.Replace("/", "\\");
                    string full;
                    if (rel.StartsWith("Common\\"))
                    {
                        full = Path.Combine(projectRoot, opt.CommonNamespace, rel.Substring("Common\\".Length));
                    }
                    else if (rel.StartsWith("DAL\\"))
                    {
                        full = Path.Combine(projectRoot, opt.DalNamespace, rel.Substring("DAL\\".Length));
                    }
                    else if (rel.StartsWith("BLL\\"))
                    {
                        full = Path.Combine(projectRoot, opt.BllNamespace, rel.Substring("BLL\\".Length));
                    }
                    else if (rel.StartsWith("WebApi\\"))
                    {
                        full = Path.Combine(projectRoot, opt.WebApiNamespace, rel.Substring("WebApi\\".Length));
                    }
                    else
                    {
                        full = Path.Combine(projectRoot, rel);
                    }

                    var dir = Path.GetDirectoryName(full);
                    if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                    using (var sw = new StreamWriter(full, false, Encoding.UTF8))
                    {
                        await sw.WriteAsync(f.Content ?? string.Empty);
                    }
                    filesCount++;
                }

                using (var sw = new StreamWriter($"{Environment.CurrentDirectory}\\default.ini"))
                {
                    // 记住“上级输出目录”（而不是 projectRoot），下次依旧从这个目录开始
                    await sw.WriteAsync(outputBase);
                }

                var result = new
                {
                    message = $"分层生成完成：{opt.ProjectName}（{filesCount} 个文件）",
                    outputPath = projectRoot,
                    fileCount = filesCount
                };
                var json = HttpUtility.JavaScriptStringEncode(JsonConvert.SerializeObject(result));
                EvaluateJavascript($"generateLayersSuccess('{json}')", (value, exception) => { });
            }
        }

        private List<GeneratedFile> BuildWebApiAdditionalFiles(LayerGenOptions opt, List<string> entityNames)
        {
            var files = new List<GeneratedFile>();
            if (opt == null) return files;

            if (opt.GenerateWebApiBase)
            {
                files.Add(new GeneratedFile
                {
                    RelativePath = "WebApi/_base/_BaseApiController.cs",
                    Content = $@"using Microsoft.AspNetCore.Mvc;

namespace {opt.WebApiNamespace}._base
{{
    [ApiController]
    [Route(""api/[controller]"")]
    public class _BaseApiController : ControllerBase
    {{
    }}
}}"
                });
                files.Add(new GeneratedFile
                {
                    RelativePath = "WebApi/_base/BaseApiController.cs",
                    Content = $@"namespace {opt.WebApiNamespace}._base
{{
    public class BaseApiController : _BaseApiController
    {{
    }}
}}"
                });
                files.Add(new GeneratedFile
                {
                    RelativePath = "WebApi/_base/BaseController.cs",
                    Content = $@"using {opt.WebApiNamespace}._base.Results;

namespace {opt.WebApiNamespace}._base
{{
    public class BaseController : BaseApiController
    {{
        protected SysResultString SuccessString(string msg = ""成功"") => SysResult.SuccessString(msg);
        protected SysResultString ErrorString(string msg = ""失败"") => SysResult.ErrorString(msg);
    }}
}}"
                });
                files.Add(new GeneratedFile
                {
                    RelativePath = "WebApi/_base/Results/SysResult.cs",
                    Content = $@"using System;
using System.Collections.Generic;

namespace {opt.WebApiNamespace}._base.Results
{{
    /// <summary>
    /// 统一返回体（与 qingqia 生成器风格对齐）
    /// code: 0=成功，其它=失败/异常
    /// msg : 提示信息
    /// data: 业务数据
    /// </summary>
    public class SysResult
    {{
        public int code {{ get; set; }}
        public string errCode {{ get; set; }}
        public string msg {{ get; set; }}
        public object data {{ get; set; }}
        public string traceId {{ get; set; }}
        public long time {{ get; set; }}
        public Dictionary<string, string[]> errors {{ get; set; }}

        public static SysResult Ok(object data = null, string msg = ""成功"")
        {{
            return new SysResult {{ code = 0, msg = msg, data = data, time = DateTimeOffset.Now.ToUnixTimeMilliseconds() }};
        }}

        public static SysResult Fail(string msg = ""失败"", int code = 1, object data = null, string errCode = null, string traceId = null, Dictionary<string, string[]> errors = null)
        {{
            return new SysResult {{ code = code, msg = msg, data = data, errCode = errCode, traceId = traceId, errors = errors, time = DateTimeOffset.Now.ToUnixTimeMilliseconds() }};
        }}

        public static SysResultString SuccessString(string msg = ""成功"") => new SysResultString {{ code = 0, msg = msg }};
        public static SysResultString ErrorString(string msg = ""失败"", int code = 1) => new SysResultString {{ code = code, msg = msg }};

        public static SysResult<T> Ok<T>(T data, string msg = ""成功"")
        {{
            return new SysResult<T> {{ code = 0, msg = msg, data = data, time = DateTimeOffset.Now.ToUnixTimeMilliseconds() }};
        }}

        public static SysResult<T> Fail<T>(string msg = ""失败"", int code = 1, string errCode = null, string traceId = null, Dictionary<string, string[]> errors = null)
        {{
            return new SysResult<T> {{ code = code, msg = msg, errCode = errCode, traceId = traceId, errors = errors, time = DateTimeOffset.Now.ToUnixTimeMilliseconds() }};
        }}

        public static SysResult_layui_table Return_layui<T>(PagedResult<T> page, string msg = ""成功"")
        {{
            return new SysResult_layui_table
            {{
                code = 0,
                msg = msg,
                count = page == null ? 0 : page.total,
                data = page == null ? new List<T>() : (page.list ?? new List<T>())
            }};
        }}
    }}

    public class SysResultString
    {{
        public int code {{ get; set; }}
        public string errCode {{ get; set; }}
        public string msg {{ get; set; }}
        public string traceId {{ get; set; }}
        public long time {{ get; set; }}
    }}

    public class SysResult<T>
    {{
        public int code {{ get; set; }}
        public string errCode {{ get; set; }}
        public string msg {{ get; set; }}
        public T data {{ get; set; }}
        public string traceId {{ get; set; }}
        public long time {{ get; set; }}
        public Dictionary<string, string[]> errors {{ get; set; }}
    }}

    public class SysResult_layui_table
    {{
        public int code {{ get; set; }}
        public string errCode {{ get; set; }}
        public string msg {{ get; set; }}
        public int count {{ get; set; }}
        public object data {{ get; set; }}
        public string traceId {{ get; set; }}
        public long time {{ get; set; }}
    }}

    public class PagedResult<T>
    {{
        public int total {{ get; set; }}
        public List<T> list {{ get; set; }}
    }}

    public class formparam_PagerInfo_LayUI
    {{
        public int page {{ get; set; }} = 1;
        public int limit {{ get; set; }} = 20;
    }}

    public class formbody_param_del
    {{
        public string ids {{ get; set; }}
    }}

    public class formbody_param_switch
    {{
        public string id {{ get; set; }}
        public bool data {{ get; set; }}
    }}
}}"
                });
                files.Add(new GeneratedFile
                {
                    RelativePath = "WebApi/_base/HelperPowers/PowerMenu.cs",
                    Content = $@"namespace {opt.WebApiNamespace}._base.HelperPowers
{{
    // 全局权限菜单入口，可按业务补齐
    public partial class PowerMenu
    {{
    }}

    public partial class PowerController
    {{
    }}

    // 常用权限动作（可按业务扩展）
    public enum PowerAction
    {{
        查看列表,
        添加,
        编辑,
        删除
    }}

    // 以下特性用于让生成代码“即拷即用”，具体鉴权逻辑由宿主项目实现/替换
    public class AttrControllerAttribute : System.Attribute
    {{
        public AttrControllerAttribute(string controller) {{ }}
    }}

    public class AttrKeyAttribute : System.Attribute
    {{
        public AttrKeyAttribute(string key) {{ }}
    }}

    public class HasPowerAttribute : System.Attribute
    {{
        public HasPowerAttribute(PowerAction action) {{ }}
    }}
}}"
                });
                files.Add(new GeneratedFile
                {
                    RelativePath = "WebApi/_base/DefaultController.cs",
                    Content = $@"using Microsoft.AspNetCore.Mvc;
using {opt.WebApiNamespace}._base.Results;

namespace {opt.WebApiNamespace}._base
{{
    public class DefaultController : BaseController
    {{
        [HttpGet(""ping"")]
        public SysResultString Ping() => SysResult.SuccessString(""ok"");
    }}
}}"
                });
                files.Add(new GeneratedFile
                {
                    RelativePath = "WebApi/_base/HomeController.cs",
                    Content = $@"using Microsoft.AspNetCore.Mvc;

namespace {opt.WebApiNamespace}._base
{{
    public class HomeController : BaseController
    {{
    }}
}}"
                });
                files.Add(new GeneratedFile
                {
                    RelativePath = "WebApi/_base/LoginController.cs",
                    Content = $@"using Microsoft.AspNetCore.Mvc;

namespace {opt.WebApiNamespace}._base
{{
    public class LoginController : BaseController
    {{
    }}
}}"
                });
                files.Add(new GeneratedFile
                {
                    RelativePath = "WebApi/_base/RedisController.cs",
                    Content = $@"using Microsoft.AspNetCore.Mvc;

namespace {opt.WebApiNamespace}._base
{{
    public class RedisController : BaseController
    {{
    }}
}}"
                });
            }

            if (opt.GenerateServices)
            {
                files.Add(new GeneratedFile
                {
                    RelativePath = "WebApi/Services/ServiceCollectionExtensions.cs",
                    Content = BuildServiceRegistrationCode(opt, entityNames)
                });
            }

            if (opt.GenerateWebApiExtensions)
            {
                // Extensions：让 Program.cs 极薄（builder.Services / app.Use 拆分）
                files.Add(new GeneratedFile
                {
                    RelativePath = "WebApi/Extensions/AppHostExtensions.cs",
                    Content = $@"using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace {opt.WebApiNamespace}.Extensions
{{
    /// <summary>
    /// WebApi 宿主入口扩展：让 Program.cs 保持极薄。
    /// </summary>
    public static class AppHostExtensions
    {{
        /// <summary>
        /// 注册应用所需服务（按模块拆分）。
        /// </summary>
        /// <param name=""services"">DI 容器</param>
        /// <param name=""config"">配置</param>
        /// <param name=""env"">宿主环境</param>
        /// <returns>services</returns>
        public static IServiceCollection AddAppServices(this IServiceCollection services, IConfiguration config, IWebHostEnvironment env)
        {{
            services
                .AddLoggingModule(config, env)
                .AddGeneratedLayerServices(config, env)
                .AddMvcModule(config)
                .AddCorsModule(config)
                .AddSwaggerModule(config)
                .AddJwtAuthModule(config);

            return services;
        }}

        /// <summary>
        /// 注册请求处理管道（中间件流水线，按模块拆分）。
        /// </summary>
        /// <param name=""app"">WebApplication</param>
        /// <returns>app</returns>
        public static WebApplication UseAppPipeline(this WebApplication app)
        {{
            app
                .UseGlobalExceptionModule()
                .UseCorsModule()
                .UseSwaggerModule()
                .UseJwtAuthModule();

            app.MapControllers();
            return app;
        }}
    }}
}}"
                });

                // NLog：最常用日志（宿主需安装 NLog.Web.AspNetCore / NLog.Extensions.Logging）
                files.Add(new GeneratedFile
                {
                    RelativePath = "WebApi/Extensions/ServiceRegistration/LoggingModuleExtensions.cs",
                    Content = $@"using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;
using NLog.Web;

namespace {opt.WebApiNamespace}.Extensions
{{
    /// <summary>
    /// 日志模块（NLog）。宿主项目需安装 NLog 相关 NuGet 包，并在根目录提供 nlog.config。
    /// </summary>
    public static class LoggingModuleExtensions
    {{
        /// <summary>
        /// 注册 NLog 日志（建议在任何依赖 ILogger&lt;T&gt; 的服务之前调用）。
        /// </summary>
        /// <param name=""services"">DI 容器</param>
        /// <param name=""config"">配置</param>
        /// <param name=""env"">环境</param>
        /// <returns>services</returns>
        public static IServiceCollection AddLoggingModule(this IServiceCollection services, IConfiguration config, IWebHostEnvironment env)
        {{
            if (!{(opt.EnableNLog ? "true" : "false")}) return services;

            services.AddLogging(builder =>
            {{
                builder.ClearProviders();
                builder.SetMinimumLevel(LogLevel.Trace);
                builder.AddNLog();
            }});

            // 让 NLog 读取根目录 nlog.config（如果你把配置放别处，可自行改）
            NLogBuilder.ConfigureNLog(""nlog.config"");
            return services;
        }}
    }}
}}"
                });

                if (opt.GenerateNLogConfig)
                {
                    files.Add(new GeneratedFile
                    {
                        // 放在 WebApi 项目根目录，方便宿主直接复制到输出根
                        RelativePath = "WebApi/nlog.config",
                        Content = @"<?xml version=""1.0"" encoding=""utf-8"" ?>
<nlog xmlns=""http://www.nlog-project.org/schemas/NLog.xsd""
      xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance""
      autoReload=""true""
      internalLogLevel=""Warn""
      internalLogFile=""${basedir}/logs/nlog-internal.log"">

  <targets>
    <target xsi:type=""File""
            name=""allfile""
            fileName=""${basedir}/logs/${shortdate}.log""
            layout=""${longdate}|${uppercase:${level}}|${logger}|${message} ${exception:format=ToString}"" />
    <target xsi:type=""Console""
            name=""console""
            layout=""${longdate}|${uppercase:${level}}|${logger}|${message} ${exception:format=ToString}"" />
  </targets>

  <rules>
    <logger name=""*"" minlevel=""Info"" writeTo=""console,allfile"" />
  </rules>
</nlog>
"
                    });
                }

                files.Add(new GeneratedFile
                {
                    RelativePath = "WebApi/Extensions/ServiceRegistration/GeneratedLayerExtensions.cs",
                    Content = $@"using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SqlSugar;

using {opt.CommonNamespace};
using {opt.DalNamespace};
using {opt.WebApiNamespace}.Services;

namespace {opt.WebApiNamespace}.Extensions
{{
    public static class GeneratedLayerExtensions
    {{
        /// <summary>
        /// 注册生成层（Common/DAL/BLL）所需依赖。宿主项目可在此处替换 SqlSugarScope 的构建方式（单库/多库/读写分离）。
        /// </summary>
        public static IServiceCollection AddGeneratedLayerServices(this IServiceCollection services, IConfiguration config, IWebHostEnvironment env)
        {{
            // 1) SqlSugarScope（示例：从配置读取连接串；可按项目改成多库）
            services.AddScoped(sp =>
            {{
                var connStr = config.GetConnectionString(""Default"") ?? string.Empty;
                var db = new SqlSugarScope(new ConnectionConfig
                {{
                    ConnectionString = connStr,
                    DbType = DbType.SqlServer,
                    IsAutoCloseConnection = true
                }});
                return db;
            }});

            // 2) DIP：DbContext / Repository
            services.AddScoped<ISqlSugarDbContext, SqlSugarDbContext>();
            services.AddScoped(typeof(IRepository<>), typeof(Repository<>));

            // 3) 生成的 BLL（每表一个）
            services.AddGeneratedServices();
            return services;
        }}
    }}
}}"
                });

                files.Add(new GeneratedFile
                {
                    RelativePath = "WebApi/Extensions/ServiceRegistration/MvcModuleExtensions.cs",
                    Content = $@"using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace {opt.WebApiNamespace}.Extensions
{{
    /// <summary>
    /// MVC 模块：控制器/JSON 等。
    /// </summary>
    public static class MvcModuleExtensions
    {{
        /// <summary>
        /// 注册 MVC/Controllers。
        /// </summary>
        /// <param name=""services"">DI 容器</param>
        /// <param name=""config"">配置</param>
        /// <returns>services</returns>
        public static IServiceCollection AddMvcModule(this IServiceCollection services, IConfiguration config)
        {{
            services.AddControllers();
            return services;
        }}
    }}
}}"
                });

                files.Add(new GeneratedFile
                {
                    RelativePath = "WebApi/Extensions/ServiceRegistration/CorsModuleExtensions.cs",
                    Content = $@"using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace {opt.WebApiNamespace}.Extensions
{{
    /// <summary>
    /// 跨域模块（CORS）。
    /// </summary>
    public static class CorsModuleExtensions
    {{
        public const string DefaultCorsPolicyName = ""default_cors"";

        /// <summary>
        /// 注册 CORS 策略。
        /// </summary>
        /// <param name=""services"">DI 容器</param>
        /// <param name=""config"">配置</param>
        /// <returns>services</returns>
        public static IServiceCollection AddCorsModule(this IServiceCollection services, IConfiguration config)
        {{
            if (!{(opt.EnableCors ? "true" : "false")}) return services;

            services.AddCors(options =>
            {{
                options.AddPolicy(DefaultCorsPolicyName, p =>
                    p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
            }});
            return services;
        }}

        /// <summary>
        /// 启用 CORS 中间件。
        /// </summary>
        /// <param name=""app"">WebApplication</param>
        /// <returns>app</returns>
        public static WebApplication UseCorsModule(this WebApplication app)
        {{
            if (!{(opt.EnableCors ? "true" : "false")}) return app;
            app.UseCors(DefaultCorsPolicyName);
            return app;
        }}
    }}
}}"
                });

                files.Add(new GeneratedFile
                {
                    RelativePath = "WebApi/Extensions/ServiceRegistration/SwaggerModuleExtensions.cs",
                    Content = $@"using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace {opt.WebApiNamespace}.Extensions
{{
    /// <summary>
    /// Swagger/OpenAPI 模块。
    /// </summary>
    public static class SwaggerModuleExtensions
    {{
        /// <summary>
        /// 注册 Swagger 服务。
        /// </summary>
        /// <param name=""services"">DI 容器</param>
        /// <param name=""config"">配置</param>
        /// <returns>services</returns>
        public static IServiceCollection AddSwaggerModule(this IServiceCollection services, IConfiguration config)
        {{
            if (!{(opt.EnableSwagger ? "true" : "false")}) return services;

            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen();
            return services;
        }}

        /// <summary>
        /// 启用 Swagger 中间件。
        /// </summary>
        /// <param name=""app"">WebApplication</param>
        /// <returns>app</returns>
        public static WebApplication UseSwaggerModule(this WebApplication app)
        {{
            if (!{(opt.EnableSwagger ? "true" : "false")}) return app;

            app.UseSwagger();
            app.UseSwaggerUI();
            return app;
        }}
    }}
}}"
                });

                files.Add(new GeneratedFile
                {
                    RelativePath = "WebApi/Extensions/ServiceRegistration/JwtAuthModuleExtensions.cs",
                    Content = $@"using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace {opt.WebApiNamespace}.Extensions
{{
    /// <summary>
    /// JWT 鉴权模块（最小可运行版本）。
    /// </summary>
    public static class JwtAuthModuleExtensions
    {{
        /// <summary>
        /// 注册 JWT 认证与授权。
        /// </summary>
        /// <param name=""services"">DI 容器</param>
        /// <param name=""config"">配置</param>
        /// <returns>services</returns>
        public static IServiceCollection AddJwtAuthModule(this IServiceCollection services, IConfiguration config)
        {{
            if (!{(opt.EnableJwtAuth ? "true" : "false")}) return services;

            var key = config[""jwt:key""] ?? ""change_me"";
            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {{
                    options.TokenValidationParameters = new TokenValidationParameters
                    {{
                        ValidateIssuer = false,
                        ValidateAudience = false,
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key))
                    }};
                }});

            services.AddAuthorization();
            return services;
        }}

        /// <summary>
        /// 启用 JWT 认证/授权中间件。
        /// </summary>
        /// <param name=""app"">WebApplication</param>
        /// <returns>app</returns>
        public static WebApplication UseJwtAuthModule(this WebApplication app)
        {{
            if (!{(opt.EnableJwtAuth ? "true" : "false")}) return app;
            app.UseAuthentication();
            app.UseAuthorization();
            return app;
        }}
    }}
}}"
                });

                files.Add(new GeneratedFile
                {
                    RelativePath = "WebApi/Extensions/Middleware/GlobalExceptionMiddleware.cs",
                    Content = $@"using Microsoft.AspNetCore.Http;
using System;
using System.Threading.Tasks;
using {opt.WebApiNamespace}._base.Results;

namespace {opt.WebApiNamespace}.Extensions.Middleware
{{
    /// <summary>
    /// 全局异常处理中间件：捕获异常并转换为统一返回体（SysResult）。
    /// </summary>
    public class GlobalExceptionMiddleware
    {{
        private readonly RequestDelegate _next;
        public GlobalExceptionMiddleware(RequestDelegate next) {{ _next = next; }}

        /// <summary>
        /// 执行中间件。
        /// </summary>
        /// <param name=""context"">HttpContext</param>
        /// <returns>Task</returns>
        public async Task Invoke(HttpContext context)
        {{
            try
            {{
                await _next(context);
            }}
            catch (Exception ex)
            {{
                context.Response.StatusCode = 200;
                context.Response.ContentType = ""application/json; charset=utf-8"";
                var traceId = context.TraceIdentifier;
                var payload = SysResult.Fail(ex.Message, 500, data: null, errCode: ""EXCEPTION"", traceId: traceId);
                await context.Response.WriteAsJsonAsync(payload);
            }}
        }}
    }}
}}"
                });

                files.Add(new GeneratedFile
                {
                    RelativePath = "WebApi/Extensions/ServiceRegistration/GlobalExceptionModuleExtensions.cs",
                    Content = $@"using Microsoft.AspNetCore.Builder;
using {opt.WebApiNamespace}.Extensions.Middleware;

namespace {opt.WebApiNamespace}.Extensions
{{
    /// <summary>
    /// 全局异常模块：将异常转换为统一返回体。
    /// </summary>
    public static class GlobalExceptionModuleExtensions
    {{
        /// <summary>
        /// 启用全局异常处理中间件。
        /// </summary>
        /// <param name=""app"">WebApplication</param>
        /// <returns>app</returns>
        public static WebApplication UseGlobalExceptionModule(this WebApplication app)
        {{
            if (!{(opt.EnableGlobalException ? "true" : "false")}) return app;
            app.UseMiddleware<GlobalExceptionMiddleware>();
            return app;
        }}
    }}
}}"
                });

                if (opt.GenerateWebApiProgramSkeleton)
                {
                    files.Add(new GeneratedFile
                    {
                        RelativePath = "WebApi/Program.sample.cs",
                        Content = $@"using {opt.WebApiNamespace}.Extensions;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddAppServices(builder.Configuration, builder.Environment);

var app = builder.Build();
app.UseAppPipeline();

app.Run();
"
                    });
                }
            }

            if (opt.GenerateWebApiBase)
            {
                foreach (var n in entityNames ?? new List<string>())
                {
                    files.Add(new GeneratedFile
                    {
                        RelativePath = $"WebApi/_base/HelperPowers/PowerMenu_{n}.cs",
                        Content = $@"namespace {opt.WebApiNamespace}._base.HelperPowers
{{
    // 每表权限声明（按 qingqia 习惯生成）
    public partial class PowerMenu
    {{
        public const string {n} = ""menu_{n}"";
    }}

    public partial class PowerController
    {{
        public const string {n} = nameof({opt.WebApiNamespace}.{n}Controller);
    }}
}}"
                    });
                }
            }

            return files;
        }

        private string BuildServiceRegistrationCode(LayerGenOptions opt, List<string> entityNames)
        {
            var lines = new StringBuilder();
            lines.AppendLine("using Microsoft.Extensions.DependencyInjection;");
            lines.AppendLine($"using {opt.BllNamespace};");
            lines.AppendLine();
            lines.AppendLine($"namespace {opt.WebApiNamespace}.Services");
            lines.AppendLine("{");
            lines.AppendLine("    public static class ServiceCollectionExtensions");
            lines.AppendLine("    {");
            lines.AppendLine("        public static IServiceCollection AddGeneratedServices(this IServiceCollection services)");
            lines.AppendLine("        {");
            foreach (var n in entityNames ?? new List<string>())
            {
                lines.AppendLine($"            services.AddScoped<Bll{n}>();");
            }
            lines.AppendLine("            return services;");
            lines.AppendLine("        }");
            lines.AppendLine("    }");
            lines.AppendLine("}");
            return lines.ToString();
        }

        public static void ShowWindow()
        {
            if (CreateEntity._CreateEntity == null)
            {
                CreateEntity._CreateEntity = new CreateEntity();
            }
            CreateEntity._CreateEntity.Show();
            CreateEntity._CreateEntity.WindowState = FormWindowState.Maximized;
            CreateEntity._CreateEntity.Focus();
        }

        private void CreateEntity_FormClosed(object sender, FormClosedEventArgs e)
        {
            CreateEntity._CreateEntity.Dispose();
            this.Dispose();
            CreateEntity._CreateEntity = null;
            GC.Collect();
        }

        private void LoadHandler_OnLoadEnd(object sender, CfxOnLoadEndEventArgs e)
        {
            // Check if it is the main frame when page has loaded.
            //if (e.Frame.IsMain)
            //{
            //    EvaluateJavascript("sayHelloToSomeone('C#1111111')", (value, exception) =>
            //    {
            //        if (value.IsString)
            //        {
            //            // Get value from Javascript.
            //            var jsValue = value.StringValue;

            //            MessageBox.Show(jsValue);
            //        }
            //    });
            //}
            //base.Chromium.ShowDevTools();
        }

        /// <summary>
        /// 加载所有表
        /// </summary>
        /// <param name="linkString">连接字符串</param>
        private async Task<DataTable> LoadingTables(string linkString, DataBaseType type)
        {
            switch (type)
            {
                case DataBaseType.SQLServer:
                    var sql = @"select name as TableName, ISNULL(j.TableDesc, '') as TableDesc  From sysobjects g
left join
(
select * from
(SELECT 
    TableName       = case when a.colorder=1 then d.name else '' end,
    TableDesc     = case when a.colorder=1 then isnull(f.value,'') else '' end
FROM 
    syscolumns a
inner join 
    sysobjects d 
on 
    a.id=d.id  and d.xtype='U' and  d.name<>'dtproperties'
inner join
sys.extended_properties f
on 
    d.id=f.major_id and f.minor_id=0) t
	where t.TableName!=''
	) j on g.name = j.TableName
	Where g.xtype='U'
	order by TableName ASC";
                    var table1 = await SQLServerHelper.QueryDataTable(linkString, sql);
                    sql = @"select name as TableName,'' as TableDesc   From sysobjects j where j.xtype='V' order by name asc";
                    var table2 = await SQLServerHelper.QueryDataTable(linkString, sql);
                    DataTable newDataTable = table1.Clone();
                    object[] obj = new object[newDataTable.Columns.Count];
                    for (int i = 0; i < table1.Rows.Count; i++)
                    {
                        table1.Rows[i].ItemArray.CopyTo(obj, 0);
                        newDataTable.Rows.Add(obj);
                    }

                    for (int i = 0; i < table2.Rows.Count; i++)
                    {
                        table2.Rows[i].ItemArray.CopyTo(obj, 0);
                        newDataTable.Rows.Add(obj);
                    }

                    return newDataTable;
                case DataBaseType.MySQL:
                    var database = linkString.Substring(linkString.IndexOf("Database=") + 9, linkString.IndexOf(";port=") - linkString.IndexOf("Database=") - 9);
                    var sql1 = $"SELECT TABLE_NAME as TableName, Table_Comment as TableDesc FROM INFORMATION_SCHEMA.TABLES where TABLE_SCHEMA = '{database}' order by TableName asc";
                    return await MySQLHelper.QueryDataTable(linkString, sql1);
                case DataBaseType.Oracler:
                    var oracleSql = "select table_name as TableName,comments as tabledesc from user_tab_comments order by table_name asc";
                    return await OracleHelper.QueryDataTable(linkString, oracleSql);
                case DataBaseType.SQLite:
                    return await SQLiteHelper.QueryDataTable(linkString, "SELECT name FROM sqlite_master order by name asc");
                case DataBaseType.PostgreSQL:
                    //var tableowner = linkString.Substring(linkString.IndexOf("Username=") + 9, linkString.IndexOf(";Password=") - linkString.IndexOf("Username=") - 9);
                    var sql2 = $@"SELECT
	t2.tablename AS TableName,
	CAST (obj_description(t1.oid, 'pg_class') AS VARCHAR) AS TableDesc 
FROM
	pg_class t1
	LEFT JOIN pg_tables t2 ON t1.relname = t2.tablename 
WHERE
	t2.schemaname = 'public'
ORDER BY
	t1.relname ASC";
                    return await PostgreSqlHelper.QueryDataTable(linkString, sql2);
                default:
                    return null;
            }
        }

        /// <summary>
        /// 获得类型
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        Type GetTypeByString(string type)
        {
            switch (type.ToLower())
            {
                case "system.boolean":
                    return Type.GetType("System.Boolean", true, true);
                case "system.byte":
                    return Type.GetType("System.Byte", true, true);
                case "system.sbyte":
                    return Type.GetType("System.SByte", true, true);
                case "system.char":
                    return Type.GetType("System.Char", true, true);
                case "system.decimal":
                    return Type.GetType("System.Decimal", true, true);
                case "system.double":
                    return Type.GetType("System.Double", true, true);
                case "system.single":
                    return Type.GetType("System.Single", true, true);
                case "system.int32":
                    return Type.GetType("System.Int32", true, true);
                case "system.uint32":
                    return Type.GetType("System.UInt32", true, true);
                case "system.int64":
                    return Type.GetType("System.Int64", true, true);
                case "system.uint64":
                    return Type.GetType("System.UInt64", true, true);
                case "system.object":
                    return Type.GetType("System.Object", true, true);
                case "system.int16":
                    return Type.GetType("System.Int16", true, true);
                case "system.uint16":
                    return Type.GetType("System.UInt16", true, true);
                case "system.string":
                    return Type.GetType("System.String", true, true);
                case "system.datetime":
                case "datetime":
                    return Type.GetType("System.DateTime", true, true);
                case "system.guid":
                    return Type.GetType("System.Guid", true, true);
                default:
                    return Type.GetType(type, true, true);
            }
        }

        private static string ToCSharpShortTypeName(string rawTypeName)
        {
            var t = (rawTypeName ?? string.Empty).Trim();
            if (t.Length == 0) return t;

            // 常见 CLR 全名 => C# 友好短名/别名
            switch (t)
            {
                case "System.Boolean": return "bool";
                case "System.Byte": return "byte";
                case "System.SByte": return "sbyte";
                case "System.Char": return "char";
                case "System.Decimal": return "decimal";
                case "System.Double": return "double";
                case "System.Single": return "float";
                case "System.Int32": return "int";
                case "System.UInt32": return "uint";
                case "System.Int64": return "long";
                case "System.UInt64": return "ulong";
                case "System.Int16": return "short";
                case "System.UInt16": return "ushort";
                case "System.String": return "string";
                case "System.Object": return "object";
                case "System.DateTime": return "DateTime";
                case "System.Guid": return "Guid";
                default: return t;
            }
        }

        /// <summary>
        /// 生成实体类代码
        /// </summary>
        /// <param name="linkString">连接字符串</param>
        /// <param name="nodeDesc">表注释</param>
        /// <param name="nodeName">表名称</param>
        /// <param name="settings">设置</param>
        /// <param name="type">数据库类型</param>
        /// <param name="isYuLan">是否是单笔预览</param>
        /// <returns></returns>
        private async Task<string> GetEntityCode(string linkString, string nodeName, string nodeDesc, SettingsModel settings, DataBaseType type, bool isYuLan)
        {
            StringBuilder codeString = new StringBuilder();
            DataTable tableInfo = null;
            DataTable colsInfos = null;
            if (type == DataBaseType.SQLServer)
            {
                tableInfo = await SQLServerHelper.QueryTableInfo(linkString, $"select * from [{nodeName}] where 1=2");
                colsInfos = await SQLServerHelper.QueryDataTable(linkString, "SELECT objname,value FROM ::fn_listextendedproperty (NULL, 'user', 'dbo', 'table', '" + nodeName + "', 'column', DEFAULT)", null);
                this.GetCode(
                    tableInfo,
                    colsInfos,
                    "OBJNAME",
                    "ColumnName",
                    "VALUE",
                    "IsKey",
                    "IsIdentity",
                    "DataType",
                    "AllowDBNull",
                    linkString,
                    nodeName,
                    nodeDesc,
                    settings,
                    isYuLan,
                    codeString);
            }
            else if (type == DataBaseType.MySQL)
            {
                var database = linkString.Substring(linkString.IndexOf("Database=") + 9, linkString.IndexOf(";port=") - linkString.IndexOf("Database=") - 9);
                tableInfo = await MySQLHelper.QueryTableInfo(linkString, $"select * from `{nodeName}` where 1=2");
                colsInfos = await MySQLHelper.QueryDataTable(linkString, $"select COLUMN_NAME as OBJNAME,column_comment as VALUE from INFORMATION_SCHEMA.Columns where table_name='{nodeName}' and table_schema='{database}'", null);
                this.GetCode(
                    tableInfo,
                    colsInfos,
                    "OBJNAME",
                    "ColumnName",
                    "VALUE",
                    "IsKey",
                    "IsAutoIncrement",
                    "DataType",
                    "AllowDBNull",
                    linkString,
                    nodeName,
                    nodeDesc,
                    settings,
                    isYuLan,
                    codeString);
            }
            else if (type == DataBaseType.SQLite)
            {
                tableInfo = await SQLiteHelper.QueryTableInfo(linkString, $"select * from '{nodeName}' where 1=2");
                colsInfos = await SQLiteHelper.QueryDataTable(linkString, $"PRAGMA table_info('{nodeName}')", null);
                this.GetCode(
                    tableInfo,
                    colsInfos,
                    "name",
                    "ColumnName",
                    "name",
                    "IsKey",
                    "IsAutoIncrement",
                    "DataType",
                    "AllowDBNull",
                    linkString,
                    nodeName,
                    nodeDesc,
                    settings,
                    isYuLan,
                    codeString);
            }
            else if (type == DataBaseType.PostgreSQL)
            {
                tableInfo = await PostgreSqlHelper.QueryTableInfo(linkString, $"select * from \"{nodeName}\" where 1=2");
                colsInfos = await PostgreSqlHelper.QueryDataTable(linkString, $@"SELECT
	col_description(A.attrelid, A.attnum) AS value,
	A.attname AS objname
FROM
	pg_class AS C,
	pg_attribute AS A 
WHERE
	C.relname = '{nodeName}'
	AND A.attrelid = C.oid 
	AND A.attnum >0", null);
                this.GetCode(
                    tableInfo,
                    colsInfos,
                    "objname",
                    "ColumnName",
                    "value",
                    "IsKey",
                    "IsAutoIncrement",
                    "DataType",
                    "AllowDBNull",
                    linkString,
                    nodeName,
                    nodeDesc,
                    settings,
                    isYuLan,
                    codeString);
            }
            else if (type == DataBaseType.Oracler)
            {
                tableInfo = await OracleHelper.QueryTableInfo(linkString, $"select * from \"{nodeName}\" where 1=2");
                tableInfo.Columns.Add("IsAutoIncrement", typeof(bool));
                for (int i = 0; i < tableInfo.Rows.Count; i++)
                {
                    tableInfo.Rows[i]["IsAutoIncrement"] = false;
                }
                colsInfos = await OracleHelper.QueryDataTable(linkString, $"select column_name as OBJNAME,comments as VALUE from user_col_comments where table_name = '{nodeName}'", null);
                this.GetCode(
                    tableInfo,
                    colsInfos,
                    "OBJNAME",
                    "ColumnName",
                    "VALUE",
                    "IsKey",
                    "IsAutoIncrement",
                    "DataType",
                    "AllowDBNull",
                    linkString,
                    nodeName,
                    nodeDesc,
                    settings,
                    isYuLan,
                    codeString);
            }
            tableInfo?.Clear();
            tableInfo?.Dispose();
            colsInfos?.Clear();
            colsInfos?.Dispose();
            GC.Collect();
            return codeString.ToString();
        }

        /// <summary>
        /// 获得实体类代码
        /// </summary>
        /// <param name="tableInfo">表信息</param>
        /// <param name="colsInfos">列信息</param>
        /// <param name="objname">从列信息DataTabel中取列名的key</param>
        /// <param name="columnName">从表信息DataTabel中取列名的key</param>
        /// <param name="zhuShiValueName">从列信息DataTabel中取列注释的key</param>
        /// <param name="isKeyName">从表信息DataTabel中取列名是不是主键的key</param>
        /// <param name="isIdentityName">从表信息DataTabel中取列是不是自增的key</param>
        /// <param name="dataTypeName">从表信息DataTabel中取列名数据类型的key</param>
        /// <param name="allowDBNullName">从表信息DataTabel中取列名是不是允许为null的key</param>
        /// <param name="linkString">连接字符串</param>
        /// <param name="nodeName">表名</param>
        /// <param name="nodeDesc">表注释</param>
        /// <param name="settings">设置信息</param>
        /// <param name="isYuLan">是否是预览</param>
        /// <param name="codeString"></param>
        /// <returns></returns>
        private void GetCode(
            DataTable tableInfo, 
            DataTable colsInfos, 
            string objname,
            string columnName,
            string zhuShiValueName,
            string isKeyName,
            string isIdentityName,
            string dataTypeName,
            string allowDBNullName,
            string linkString, 
            string nodeName, 
            string nodeDesc, 
            SettingsModel settings, 
            bool isYuLan, 
            StringBuilder codeString)
        {
            string tableName = (settings.ClassCapsCount > 0 ? nodeName.SetLengthToUpperByStart((int)settings.ClassCapsCount) : nodeName);
            var extraUsings = (settings.Namespace ?? string.Empty).Trim();
            // 生成代码常用：DateTime/Guid 等需要 using System;
            // 即使用户未配置，也自动补齐，避免出现 System.DateTime 这种不友好的全名。
            if (!Regex.IsMatch(extraUsings, @"(^|\r?\n)\s*using\s+System\s*;\s*(\r?\n|$)", RegexOptions.IgnoreCase))
            {
                extraUsings = (string.IsNullOrWhiteSpace(extraUsings) ? "using System;" : $"using System;{Environment.NewLine}{extraUsings}");
            }

            codeString.Append($@"using SqlSugar;{(string.IsNullOrWhiteSpace(extraUsings) ? "" : $"{Environment.NewLine}{extraUsings}")}

namespace {settings.EntityNamespace.Trim()}
{{
    /// <summary>
    /// {nodeDesc}
    /// </summary>{(string.IsNullOrWhiteSpace(settings.CusAttr) ? "" : $"{Environment.NewLine}    {settings.CusAttr.Trim()}")}
    [SugarTable(""{nodeName}"", TableDescription = ""{nodeDesc?.Replace("\"", "\\\"")}"")]
    public class {tableName}{(string.IsNullOrWhiteSpace(settings.BaseClassName) ? "" : $" : {settings.BaseClassName.Trim()}")}
    {{
        /// <summary>
        /// {nodeDesc}
        /// </summary>
        public {tableName}()
        {{{(string.IsNullOrWhiteSpace(settings.CusGouZao) ? "" : Environment.NewLine + "          " + settings.CusGouZao.Trim().Replace("-tableName-", isYuLan ? $"<span style=\"color:yellow\">{tableName}</span>" : tableName))}
        }}
");
            if (settings.PropType== PropType.Easy)  //建议模式, 属性只生成get; set; 属性自定义模版失效
            {
                foreach (DataRow dr in tableInfo.Rows)
                {
                    var zhuShi = string.Empty;//列名注释
                    foreach (DataRow uu in colsInfos.Rows)
                    {
                        if (uu[objname].ToString().ToUpper() == dr[columnName].ToString().ToUpper())
                            zhuShi = uu[zhuShiValueName].ToString();
                    }
                    if ((bool)dr[isKeyName] && !(bool)dr[isIdentityName])
                    {
                        codeString.Append($@"
        /// <summary>
        /// -zhuShi-
        /// </summary>
        [SugarColumn(ColumnDescription = ""-zhuShiAttr-"", IsPrimaryKey = true, IsNullable = -isNullable-)]
        public -dbType- -colName- {{ get; set; }}
");
                    }
                    else if ((bool)dr[isKeyName] && (bool)dr[isIdentityName])
                    {
                        codeString.Append($@"
        /// <summary>
        /// -zhuShi-
        /// </summary>
        [SugarColumn(ColumnDescription = ""-zhuShiAttr-"", IsPrimaryKey = true, IsIdentity = true, IsNullable = -isNullable-)]
        public -dbType- -colName- {{ get; set; }}
");
                    }
                    else if (!(bool)dr[isKeyName] && (bool)dr[isIdentityName])
                    {
                        codeString.Append($@"
        /// <summary>
        /// -zhuShi-
        /// </summary>
        [SugarColumn(ColumnDescription = ""-zhuShiAttr-"", IsIdentity = true, IsNullable = -isNullable-)]
        public -dbType- -colName- {{ get; set; }}
");
                    }
                    else
                    {
                        codeString.Append($@"
        /// <summary>
        /// -zhuShi-
        /// </summary>
        [SugarColumn(ColumnDescription = ""-zhuShiAttr-"", IsNullable = -isNullable-)]
        public -dbType- -colName- {{ get; set; }}
");
                    }
                    Type ttttt = this.GetTypeByString(dr[dataTypeName].ToString());
                    codeString.Replace("-isNullable-", dr[allowDBNullName].ToString() == "True" ? "true" : "false");
                    var rawTypeName = dr[dataTypeName].ToString();
                    var shortTypeName = ToCSharpShortTypeName(rawTypeName);
                    if (ttttt.IsValueType && dr[allowDBNullName].ToString() == "True")
                    {
                        codeString.Replace("-dbType-", isYuLan ? $"<span style=\"color:#23C645\">{shortTypeName}?</span>" : shortTypeName + "?");  //替换数据类型
                        if (settings.PropDefault)
                        {
                            codeString.Replace("-value-", $"value ?? default({(isYuLan ? $"<span style=\"color:#23C645\">{shortTypeName}</span>" : shortTypeName)})");
                        }
                        else
                        {
                            codeString.Replace("-value-", "value");
                        }
                    }
                    else if (ttttt.IsValueType)
                    {
                        codeString.Replace("-dbType-", isYuLan ? $"<span style=\"color:#23C645\">{shortTypeName}</span>" : shortTypeName);  //替换数据类型
                        codeString.Replace("-value-", "value");
                    }
                    else
                    {
                        if (rawTypeName == "System.String" || shortTypeName == "string")
                        {
                            codeString.Replace("-dbType-", isYuLan ? $"<span style=\"color:red\">{shortTypeName}</span>" : shortTypeName);  //替换数据类型
                            if (settings.PropTrim)
                            {
                                codeString.Replace("-value-", "value?.Trim()");
                            }
                            else
                            {
                                codeString.Replace("-value-", "value");
                            }
                        }
                        else
                        {
                            codeString.Replace("-dbType-", isYuLan ? $"<span style=\"color:red\">{shortTypeName}</span>" : shortTypeName);  //替换数据类型
                            codeString.Replace("-value-", "value");
                        }
                    }
                    codeString.Replace("-colName-", settings.PropCapsCount > 0 ? dr[columnName].ToString().SetLengthToUpperByStart((int)settings.PropCapsCount) : dr[columnName].ToString());  //替换列名（属性名）
                    codeString.Replace("-zhuShi-", zhuShi.Replace("\r\n", "\r\n        ///"));
                    codeString.Replace("-zhuShiAttr-", (zhuShi ?? string.Empty).Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\r", " ").Replace("\n", " "));
                }



            }
            else
            {
                var getString = settings.GetCus.Trim();
                if (string.IsNullOrWhiteSpace(getString))
                {
                    getString = "return this._-colName-;";
                }
                else
                {
                    getString = getString.Replace("属性", "-colName-");
                }
                var setString = settings.SetCus.Trim();
                if (string.IsNullOrWhiteSpace(setString))
                {
                    setString = "this._-colName- = -value-;";
                }
                else
                {
                    setString = setString.Replace("属性", "-colName-");
                }
                foreach (DataRow dr in tableInfo.Rows)
                {
                    var zhuShi = string.Empty;//列名注释
                    foreach (DataRow uu in colsInfos.Rows)
                    {
                        if (uu[objname].ToString().ToUpper() == dr[columnName].ToString().ToUpper())
                            zhuShi = uu[zhuShiValueName].ToString();
                    }
                    if ((bool)dr[isKeyName] && !(bool)dr[isIdentityName])
                    {
                        codeString.Append($@"
        private -dbType- _-colName-;
        /// <summary>
        /// -zhuShi-
        /// </summary>
        [SugarColumn(ColumnDescription = ""-zhuShiAttr-"", IsPrimaryKey = true, IsNullable = -isNullable-)]
        public -dbType- -colName- {{ get {{ {getString} }} set {{ {setString} }} }}
");
                    }
                    else if ((bool)dr[isKeyName] && (bool)dr[isIdentityName])
                    {
                        codeString.Append($@"
        private -dbType- _-colName-;
        /// <summary>
        /// -zhuShi-
        /// </summary>
        [SugarColumn(ColumnDescription = ""-zhuShiAttr-"", IsPrimaryKey = true, IsIdentity = true, IsNullable = -isNullable-)]
        public -dbType- -colName- {{ get {{ {getString} }} set {{ {setString} }} }}
");
                    }
                    else if (!(bool)dr[isKeyName] && (bool)dr[isIdentityName])
                    {
                        codeString.Append($@"
        private -dbType- _-colName-;
        /// <summary>
        /// -zhuShi-
        /// </summary>
        [SugarColumn(ColumnDescription = ""-zhuShiAttr-"", IsIdentity = true, IsNullable = -isNullable-)]
        public -dbType- -colName- {{ get {{ {getString} }} set {{ {setString} }} }}
");
                    }
                    else
                    {
                        codeString.Append($@"
        private -dbType- _-colName-;
        /// <summary>
        /// -zhuShi-
        /// </summary>
        [SugarColumn(ColumnDescription = ""-zhuShiAttr-"", IsNullable = -isNullable-)]
        public -dbType- -colName- {{ get {{ {getString} }} set {{ {setString} }} }}
");
                    }
                    Type ttttt = this.GetTypeByString(dr[dataTypeName].ToString());
                    codeString.Replace("-isNullable-", dr[allowDBNullName].ToString() == "True" ? "true" : "false");
                    var rawTypeName = dr[dataTypeName].ToString();
                    var shortTypeName = ToCSharpShortTypeName(rawTypeName);
                    if (ttttt.IsValueType && dr[allowDBNullName].ToString() == "True")
                    {
                        codeString.Replace("-dbType-", isYuLan ? $"<span style=\"color:#23C645\">{shortTypeName}?</span>" : shortTypeName + "?");  //替换数据类型
                        if (settings.PropDefault)
                        {
                            codeString.Replace("-value-", $"value ?? default({(isYuLan ? $"<span style=\"color:#23C645\">{shortTypeName}</span>" : shortTypeName)})");
                        }
                        else
                        {
                            codeString.Replace("-value-", "value");
                        }
                    }
                    else if (ttttt.IsValueType)
                    {
                        codeString.Replace("-dbType-", isYuLan ? $"<span style=\"color:#23C645\">{shortTypeName}</span>" : shortTypeName);  //替换数据类型
                        codeString.Replace("-value-", "value");
                    }
                    else
                    {
                        if (rawTypeName == "System.String" || shortTypeName == "string")
                        {
                            codeString.Replace("-dbType-", isYuLan ? $"<span style=\"color:red\">{shortTypeName}</span>" : shortTypeName);  //替换数据类型
                            if (settings.PropTrim)
                            {
                                codeString.Replace("-value-", "value?.Trim()");
                            }
                            else
                            {
                                codeString.Replace("-value-", "value");
                            }
                        }
                        else
                        {
                            codeString.Replace("-dbType-", isYuLan ? $"<span style=\"color:red\">{shortTypeName}</span>" : shortTypeName);  //替换数据类型
                            codeString.Replace("-value-", "value");
                        }
                    }
                    codeString.Replace("-colName-", settings.PropCapsCount > 0 ? dr[columnName].ToString().SetLengthToUpperByStart((int)settings.PropCapsCount) : dr[columnName].ToString());  //替换列名（属性名）
                    codeString.Replace("-zhuShi-", zhuShi.Replace("\r\n", "\r\n        ///"));
                    codeString.Replace("-zhuShiAttr-", (zhuShi ?? string.Empty).Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\r", " ").Replace("\n", " "));
                }
            }
            codeString.Append(@"    }
}");
        }
    }
}
