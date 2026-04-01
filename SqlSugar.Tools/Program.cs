using Chromium;
using NetDimension.NanUI;
using System;
using System.Windows.Forms;

namespace SqlSugar.Tools
{
    static class Program
    {
        /// <summary>
        /// 应用程序的主入口点。
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            try
            {
                //指定CEF架构和文件目录结构，并初始化CEF
                if (Bootstrap.Load(Settings(), CommandLine()))
                {
                    LoadResources();
                    Application.Run(new Main());
                }
                else
                {
                    MessageBox.Show(
                        "CEF 初始化失败（Bootstrap.Load 返回 false），程序已退出。\r\n\r\n"
                        + "常见原因：\r\n"
                        + "1. 运行目录不完整：不要只复制 SqlSugar.Tools.exe，须与生成/发布输出目录中全部文件一起拷贝（含 libcef.dll、swiftshader、locales 等 NanUI CEF 运行时）。\r\n"
                        + "2. 使用 dotnet publish 时，请检查发布目录内是否同样存在上述 CEF 文件；若没有，需在发布后把 bin\\Release 下完整内容一并部署或调整发布流程。\r\n"
                        + "3. 请勿从压缩包内直接运行未解压的 exe，工作目录须为含依赖的文件夹。\r\n",
                        "SqlSugar.Tools 无法启动",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "启动时发生异常：\r\n" + ex.Message + "\r\n\r\n" + ex,
                    "SqlSugar.Tools 启动失败",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }
        /// <summary>
        /// 加载资源
        /// </summary>
        static void LoadResources()
        {
            //注册嵌入资源，默认资源指定假的域名res.app.local
            Bootstrap.RegisterAssemblyResources(System.Reflection.Assembly.GetExecutingAssembly());
            //注册嵌入资源，并为指定资源指定一个假的域名my.resource.local
            Bootstrap.RegisterAssemblyResources(System.Reflection.Assembly.GetExecutingAssembly(), "wwwroot", "my.resource.local");
            //加载分离式(外部)的资源
            //var separateAssembly = System.Reflection.Assembly.LoadFile(System.IO.Path.Combine(Application.StartupPath, "EmbeddedResourcesInSplitAssembly.dll"));
            //注册外部的嵌入资源，并为指定资源指定一个假的域名separate.resource.local
            //Bootstrap.RegisterAssemblyResources(separateAssembly, "separate.resource.local");
        }
        static Action<CfxSettings> Settings()
        {
            return settings =>
            {
                settings.LogSeverity = CfxLogSeverity.Disable;//禁用日志

                //指定中文为当前CEF环境的默认语言
                settings.AcceptLanguageList = "zh-CN";
                settings.Locale = "zh-CN";
            };
        }
        static Action<CfxCommandLine> CommandLine()
        {
            return commandLine =>
            {
                //在启动参数中添加disable-web-security开关，禁用跨域安全检测
                commandLine.AppendSwitch("disable-web-security");
            };
        }
    }
}
