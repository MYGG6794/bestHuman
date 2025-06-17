using System;
using System.Windows.Forms;

namespace CoreApplication
{
    /// <summary>
    /// 原生透明窗口测试程序
    /// 独立运行，演示真正的透明穿透效果
    /// </summary>
    public class NativeTransparentTest
    {
        [STAThread]
        public static void RunTest()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            Logger.LogInfo("启动原生透明窗口测试");

            var nativeWindow = new NativeTransparentWindow
            {
                Text = "原生透明窗口测试",
                Size = new System.Drawing.Size(800, 600),
                Location = new System.Drawing.Point(100, 100),
                ChromaKeyColor = System.Drawing.Color.Green,
                EnableChromaKey = true,
                StreamUrl = "test://demo"
            };

            // 显示窗口
            nativeWindow.Show();
            nativeWindow.StartStreaming();

            // 创建一个简单的控制窗口
            var controlForm = new Form
            {
                Text = "原生透明窗口控制",
                Size = new System.Drawing.Size(300, 200),
                Location = new System.Drawing.Point(950, 100),
                TopMost = true,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false
            };

            var btnToggleChroma = new Button
            {
                Text = "切换抠像",
                Size = new System.Drawing.Size(100, 30),
                Location = new System.Drawing.Point(10, 10)
            };
            btnToggleChroma.Click += (s, e) =>
            {
                nativeWindow.EnableChromaKey = !nativeWindow.EnableChromaKey;
                Logger.LogInfo($"抠像功能: {(nativeWindow.EnableChromaKey ? "启用" : "禁用")}");
            };

            var btnChangeColor = new Button
            {
                Text = "更改颜色",
                Size = new System.Drawing.Size(100, 30),
                Location = new System.Drawing.Point(120, 10)
            };
            btnChangeColor.Click += (s, e) =>
            {
                using var colorDialog = new ColorDialog();
                colorDialog.Color = nativeWindow.ChromaKeyColor;
                if (colorDialog.ShowDialog() == DialogResult.OK)
                {
                    nativeWindow.ChromaKeyColor = colorDialog.Color;
                }
            };

            var btnClose = new Button
            {
                Text = "关闭",
                Size = new System.Drawing.Size(100, 30),
                Location = new System.Drawing.Point(10, 50)
            };
            btnClose.Click += (s, e) =>
            {
                nativeWindow.Close();
                controlForm.Close();
            };

            var lblInstructions = new Label
            {
                Text = "这是原生透明窗口测试\n抠像区域应该完全透明\n可以看到桌面背景",
                Size = new System.Drawing.Size(280, 60),
                Location = new System.Drawing.Point(10, 90),
                TextAlign = System.Drawing.ContentAlignment.TopLeft
            };

            controlForm.Controls.AddRange(new Control[] { 
                btnToggleChroma, btnChangeColor, btnClose, lblInstructions 
            });

            controlForm.FormClosed += (s, e) => nativeWindow.Close();
            nativeWindow.FormClosed += (s, e) => controlForm.Close();

            controlForm.Show();

            Logger.LogInfo("原生透明窗口测试启动完成");
            Logger.LogInfo("使用控制窗口来测试抠像功能");
        }
    }
}
