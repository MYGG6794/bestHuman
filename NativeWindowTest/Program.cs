using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace NativeWindowTest
{
    /// <summary>
    /// 独立的原生透明窗口测试程序
    /// 完全独立运行，演示真正的透明穿透效果
    /// </summary>
    public class NativeTransparentWindow : Form
    {
        #region Windows API
        [DllImport("user32.dll", SetLastError = true)]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll")]
        private static extern bool SetLayeredWindowAttributes(IntPtr hwnd, uint crKey, byte bAlpha, uint dwFlags);

        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_LAYERED = 0x80000;
        private const uint LWA_COLORKEY = 0x1;

        [DllImport("user32.dll", SetLastError = true)]
        static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        const uint SWP_NOMOVE = 0x0002;
        const uint SWP_NOSIZE = 0x0001;
        const uint SWP_NOZORDER = 0x0004;
        const uint SWP_FRAMECHANGED = 0x0020; 
        #endregion

        private Color _chromaKeyColor = Color.Green;
        private bool _enableChromaKey = false; // Changed to false
        private System.Windows.Forms.Timer _drawTimer;
        private Random _random = new Random();

        public NativeTransparentWindow()
        {
            InitializeWindow();
            
            _drawTimer = new System.Windows.Forms.Timer();
            _drawTimer.Interval = 50; // 20 FPS
            _drawTimer.Tick += OnDrawTimer;
            _drawTimer.Start();
        }        private void InitializeWindow()
        {
            this.Text = "原生透明窗口测试";
            this.FormBorderStyle = FormBorderStyle.None;
            this.TopMost = true;
            this.ShowInTaskbar = false;
            this.Size = new Size(600, 400);
            this.StartPosition = FormStartPosition.CenterScreen;
            
            this.SetStyle(ControlStyles.AllPaintingInWmPaint, true);
            this.SetStyle(ControlStyles.UserPaint, true);
            this.SetStyle(ControlStyles.DoubleBuffer, true);
            this.SetStyle(ControlStyles.SupportsTransparentBackColor, true);            // 确保窗口能接收焦点和键盘事件
            this.KeyPreview = true;
            this.TabStop = true;
            
            this.Load += OnFormLoad;
            this.KeyDown += OnKeyDown;
            this.Click += (s, e) => this.Focus(); // 点击窗口时获得焦点
        }

        private void OnFormLoad(object sender, EventArgs e)
        {
            UpdateTransparency();
            this.Focus(); // 窗口加载后立即获得焦点
            this.Activate(); // 激活窗口
        }        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            Console.WriteLine($"键盘事件: {e.KeyCode}");
            
            switch (e.KeyCode)
            {
                case Keys.Escape:
                    Console.WriteLine("退出程序");
                    this.Close();
                    break;
                case Keys.Space:
                    _enableChromaKey = !_enableChromaKey;
                    Console.WriteLine($"切换透明: {(_enableChromaKey ? "启用" : "禁用")}");
                    UpdateTransparency();
                    break;
                case Keys.C:
                    _chromaKeyColor = _chromaKeyColor == Color.Green ? Color.Blue : 
                                    _chromaKeyColor == Color.Blue ? Color.Red : Color.Green;
                    Console.WriteLine($"切换颜色: {_chromaKeyColor.Name}");
                    UpdateTransparency();
                    break;
            }
            
            e.Handled = true; // 标记事件已处理
        }

        private void UpdateTransparency()
        {
            if (!this.IsHandleCreated) return;

            try
            {
                int originalExStyle = GetWindowLong(this.Handle, GWL_EXSTYLE);
                int newExStyle = originalExStyle;

                if (_enableChromaKey)
                {
                    newExStyle |= WS_EX_LAYERED;
                    if (newExStyle != originalExStyle)
                    {
                        SetWindowLong(this.Handle, GWL_EXSTYLE, newExStyle);
                    }

                    // Set the transparency key
                    uint colorKeyRgb = (uint)((_chromaKeyColor.R << 0) | (_chromaKeyColor.G << 8) | (_chromaKeyColor.B << 16)); // BGR format for crKey
                    // The API expects COLORREF, which is 0x00BBGGRR. C# Color.ToArgb gives 0xAARRGGBB.
                    // Let's ensure correct RGB for the API:
                    colorKeyRgb = (uint)((_chromaKeyColor.B << 16) | (_chromaKeyColor.G << 8) | _chromaKeyColor.R);


                    bool result = SetLayeredWindowAttributes(this.Handle, colorKeyRgb, 255, LWA_COLORKEY); 

                    this.BackColor = _chromaKeyColor; 
                    // No longer using this.TransparencyKey here

                    Console.WriteLine($"透明设置: 颜色={_chromaKeyColor.Name}, RGB for API=0x{colorKeyRgb:X6}, API调用结果={result}, WS_EX_LAYERED set.");
                }
                else
                {
                    newExStyle &= ~WS_EX_LAYERED;
                    if (newExStyle != originalExStyle)
                    {
                         SetWindowLong(this.Handle, GWL_EXSTYLE, newExStyle);
                    }
                    
                    this.BackColor = Color.DarkGray; // Consistent with OnPaint's else branch
                    // No longer using this.TransparencyKey here

                    // Force the window to repaint with the new style
                    SetWindowPos(this.Handle, IntPtr.Zero, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOZORDER | SWP_FRAMECHANGED);
                    Console.WriteLine("透明已禁用, WS_EX_LAYERED removed. BackColor set to DarkGray.");
                }
                this.Invalidate(); 
            }
            catch (Exception ex)
            {
                Console.WriteLine($"透明设置失败: {ex.Message}");
            }
        }

        private void OnDrawTimer(object sender, EventArgs e)
        {
            this.Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            // 清空背景为透明色
            e.Graphics.Clear(this.BackColor);

            if (_enableChromaKey)
            {
                // 绘制一些不透明的内容来测试效果
                var time = DateTime.Now.Millisecond / 1000.0;
                
                // 绘制一个移动的圆形
                var x = (int)(Math.Sin(time * Math.PI * 2) * 150 + this.Width / 2 - 50);
                var y = (int)(Math.Cos(time * Math.PI * 2) * 80 + this.Height / 2 - 50);
                
                using (var brush = new SolidBrush(Color.Blue))
                {
                    e.Graphics.FillEllipse(brush, x, y, 100, 100);
                }

                // 绘制文字信息
                using (var font = new Font("Microsoft YaHei", 12, FontStyle.Bold))
                using (var brush = new SolidBrush(Color.White))
                using (var shadowBrush = new SolidBrush(Color.Black))
                {
                    var text = $"原生透明窗口测试\n" +
                              $"抠像: {(_enableChromaKey ? "启用" : "禁用")}\n" +
                              $"背景色: {_chromaKeyColor.Name}\n" +
                              $"空格: 切换抠像\n" +
                              $"C键: 切换颜色\n" +
                              $"ESC: 退出";
                    
                    // 绘制阴影
                    e.Graphics.DrawString(text, font, shadowBrush, 11, 11);
                    // 绘制文字
                    e.Graphics.DrawString(text, font, brush, 10, 10);
                }

                // 绘制边框
                using (var pen = new Pen(Color.Yellow, 3))
                {
                    e.Graphics.DrawRectangle(pen, 2, 2, this.Width - 5, this.Height - 5);
                }

                // 绘制几个测试矩形
                using (var redBrush = new SolidBrush(Color.Red))
                using (var blueBrush = new SolidBrush(Color.Blue))
                {
                    e.Graphics.FillRectangle(redBrush, this.Width - 150, 10, 100, 50);
                    e.Graphics.FillRectangle(blueBrush, this.Width - 150, 70, 100, 50);
                }
            }
            else
            {
                // 非透明模式，显示完整界面
                using (var brush = new SolidBrush(Color.DarkGray))
                {
                    e.Graphics.FillRectangle(brush, this.ClientRectangle);
                }

                using (var font = new Font("Microsoft YaHei", 16, FontStyle.Bold))
                using (var brush = new SolidBrush(Color.White))
                {
                    var text = "透明已禁用\n按空格键启用透明";
                    var rect = this.ClientRectangle;
                    var sf = new StringFormat();
                    sf.Alignment = StringAlignment.Center;
                    sf.LineAlignment = StringAlignment.Center;
                    e.Graphics.DrawString(text, font, brush, rect, sf);
                }
            }

            base.OnPaint(e);
        }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                if (_enableChromaKey)
                {
                    cp.ExStyle |= WS_EX_LAYERED;
                }
                return cp;
            }
        }

        [STAThread]
        public static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            Console.WriteLine("=======================================");
            Console.WriteLine("启动原生透明窗口测试");
            Console.WriteLine("=======================================");
            Console.WriteLine("说明:");
            Console.WriteLine("- 窗口中的绿色区域应该完全透明");
            Console.WriteLine("- 透明区域应该能看到桌面内容");
            Console.WriteLine("- 蓝色圆形和文字应该不透明");
            Console.WriteLine("- 按空格键切换透明/不透明");
            Console.WriteLine("- 按C键切换透明色（绿/蓝/红）");
            Console.WriteLine("- 按ESC键退出");
            Console.WriteLine("=======================================");

            var window = new NativeTransparentWindow();
            Application.Run(window);
        }
    }
}
