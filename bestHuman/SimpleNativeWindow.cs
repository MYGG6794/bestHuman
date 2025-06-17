using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace CoreApplication
{
    /// <summary>
    /// 简单的原生透明窗口测试程序
    /// 独立运行，演示真正的透明穿透效果
    /// </summary>
    public class SimpleNativeWindow : Form
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
        #endregion

        private Color _chromaKeyColor = Color.Green;
        private bool _enableChromaKey = true;
        private System.Windows.Forms.Timer _drawTimer;

        public SimpleNativeWindow()
        {
            InitializeWindow();
            
            _drawTimer = new System.Windows.Forms.Timer();
            _drawTimer.Interval = 100; // 10 FPS
            _drawTimer.Tick += OnDrawTimer;
            _drawTimer.Start();
        }

        private void InitializeWindow()
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
            this.SetStyle(ControlStyles.SupportsTransparentBackColor, true);

            this.Load += (s, e) => UpdateTransparency();
            this.KeyDown += (s, e) => 
            {
                if (e.KeyCode == Keys.Escape) this.Close();
                if (e.KeyCode == Keys.Space) 
                {
                    _enableChromaKey = !_enableChromaKey;
                    UpdateTransparency();
                }
                if (e.KeyCode == Keys.C)
                {
                    _chromaKeyColor = _chromaKeyColor == Color.Green ? Color.Blue : Color.Green;
                    UpdateTransparency();
                }
            };
        }

        private void UpdateTransparency()
        {
            if (!this.IsHandleCreated) return;

            try
            {
                if (_enableChromaKey)
                {
                    int exStyle = GetWindowLong(this.Handle, GWL_EXSTYLE);
                    SetWindowLong(this.Handle, GWL_EXSTYLE, exStyle | WS_EX_LAYERED);

                    uint colorKey = (uint)((_chromaKeyColor.R << 16) | (_chromaKeyColor.G << 8) | _chromaKeyColor.B);
                    SetLayeredWindowAttributes(this.Handle, colorKey, 255, LWA_COLORKEY);

                    this.BackColor = _chromaKeyColor;
                    this.TransparencyKey = _chromaKeyColor;
                }
                else
                {
                    int exStyle = GetWindowLong(this.Handle, GWL_EXSTYLE);
                    SetWindowLong(this.Handle, GWL_EXSTYLE, exStyle & ~WS_EX_LAYERED);

                    this.BackColor = Color.Black;
                    this.TransparencyKey = Color.Empty;
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
            // 填充背景
            using (var brush = new SolidBrush(this.BackColor))
            {
                e.Graphics.FillRectangle(brush, this.ClientRectangle);
            }

            // 绘制一个移动的圆形（非透明区域）
            var time = DateTime.Now.Millisecond / 1000.0;
            var x = (int)(Math.Sin(time * Math.PI * 2) * 100 + this.Width / 2 - 50);
            var y = (int)(Math.Cos(time * Math.PI * 2) * 50 + this.Height / 2 - 50);
            
            using (var brush = new SolidBrush(Color.Blue))
            {
                e.Graphics.FillEllipse(brush, x, y, 100, 100);
            }

            // 绘制文字说明
            using (var font = new Font("Arial", 14, FontStyle.Bold))
            using (var brush = new SolidBrush(Color.White))
            {
                var text = $"原生透明窗口测试\n" +
                          $"抠像: {(_enableChromaKey ? "启用" : "禁用")}\n" +
                          $"颜色: {_chromaKeyColor.Name}\n" +
                          $"按空格键切换抠像\n" +
                          $"按C键切换颜色\n" +
                          $"按ESC键退出";
                
                e.Graphics.DrawString(text, font, brush, 10, 10);
            }

            // 绘制边框（帮助识别窗口边界）
            using (var pen = new Pen(Color.Red, 2))
            {
                e.Graphics.DrawRectangle(pen, 1, 1, this.Width - 3, this.Height - 3);
            }

            base.OnPaint(e);
        }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= WS_EX_LAYERED;
                return cp;
            }
        }

        [STAThread]
        public static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            Console.WriteLine("启动原生透明窗口测试");
            Console.WriteLine("这是一个独立的测试程序");
            Console.WriteLine("窗口中绿色区域应该完全透明，可以看到桌面");

            Application.Run(new SimpleNativeWindow());
        }
    }
}
