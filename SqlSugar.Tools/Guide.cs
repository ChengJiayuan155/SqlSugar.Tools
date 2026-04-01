using NetDimension.NanUI;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace SqlSugar.Tools
{
    public class Guide : Formium
    {
        public static Guide _Guide = null;

        public Guide()
            : base("http://my.resource.local/pages/Guide.html")
        {
            this.MinimumSize = new Size(1100, 690);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormClosed += Guide_FormClosed;

            GlobalObject.AddFunction("toHome").Execute += (func, args) =>
            {
                this.RequireUIThread(() =>
                {
                    this.Close();
                    GC.Collect();
                });
            };
        }

        private void Guide_FormClosed(object sender, FormClosedEventArgs e)
        {
            _Guide?.Dispose();
            _Guide = null;
            GC.Collect();
        }

        public static void ShowWindow()
        {
            if (_Guide == null)
            {
                _Guide = new Guide();
            }
            _Guide.Show();
            _Guide.WindowState = FormWindowState.Maximized;
            _Guide.Focus();
        }
    }
}

