using System;
using System.Drawing;
using System.Windows.Forms;

namespace CoreApplication
{
    public class NativeLayeredWindow : Form
    {
        public bool EnableChromaKey { get; set; }
        public Color ChromaKeyColor { get; set; } = Color.Green;

        public NativeLayeredWindow()
        {
            this.Text = "bestHuman 数字人助手 - 原生透明窗口";
            this.FormBorderStyle = FormBorderStyle.None;
            this.TopMost = true;
            this.ShowInTaskbar = false;
            this.Size = new Size(600, 400);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.Green;
        }

        public NativeLayeredWindow(string streamAddress) : this()
        {
            // 暂时忽略 streamAddress 参数，后续会实现 WebView2 加载
        }
    }
}
