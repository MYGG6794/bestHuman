using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CoreApplication
{
    /// <summary>
    /// 原生Windows透明窗口，支持真正的透明穿透
    /// 直接使用GDI+绘制视频帧，无需WebView2
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

        [DllImport("user32.dll")]
        private static extern bool UpdateLayeredWindow(IntPtr hwnd, IntPtr hdcDst, ref Point pptDst,
            ref Size psize, IntPtr hdcSrc, ref Point pptSrc, uint crKey, ref BLENDFUNCTION pblend, uint dwFlags);

        [DllImport("gdi32.dll")]
        private static extern IntPtr CreateCompatibleDC(IntPtr hdc);

        [DllImport("gdi32.dll")]
        private static extern IntPtr CreateCompatibleBitmap(IntPtr hdc, int nWidth, int nHeight);

        [DllImport("gdi32.dll")]
        private static extern IntPtr SelectObject(IntPtr hdc, IntPtr hgdiobj);

        [DllImport("gdi32.dll")]
        private static extern bool DeleteObject(IntPtr hObject);

        [DllImport("gdi32.dll")]
        private static extern bool DeleteDC(IntPtr hdc);

        [DllImport("user32.dll")]
        private static extern IntPtr GetDC(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool ReleaseDC(IntPtr hWnd, IntPtr hDC);

        // 常量
        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_LAYERED = 0x80000;
        private const int WS_EX_TRANSPARENT = 0x20;
        private const uint LWA_COLORKEY = 0x1;
        private const uint LWA_ALPHA = 0x2;
        private const uint ULW_ALPHA = 0x2;
        private const uint ULW_COLORKEY = 0x1;
        private const uint ULW_OPAQUE = 0x4;

        [StructLayout(LayoutKind.Sequential)]
        private struct BLENDFUNCTION
        {
            public byte BlendOp;
            public byte BlendFlags;
            public byte SourceConstantAlpha;
            public byte AlphaFormat;        }
        #endregion

        // 字段
        private readonly System.Net.Http.HttpClient _httpClient;
        private readonly System.Windows.Forms.Timer _frameTimer;
        private Bitmap? _currentFrame;
        private Color _chromaKeyColor = Color.Green;
        private int _tolerance = 30;
        private bool _enableChromaKey = true;
        private string _streamUrl = "";

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Color ChromaKeyColor
        {
            get => _chromaKeyColor;
            set
            {
                _chromaKeyColor = value;
                UpdateTransparencyKey();
                Logger.LogInfo($"抠像颜色更新为: R={value.R}, G={value.G}, B={value.B}");
            }
        }

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public int Tolerance
        {
            get => _tolerance;
            set => _tolerance = Math.Max(0, Math.Min(255, value));
        }

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool EnableChromaKey
        {
            get => _enableChromaKey;
            set
            {
                _enableChromaKey = value;
                UpdateTransparencyKey();
                Logger.LogInfo($"抠像功能: {(value ? "启用" : "禁用")}");
            }
        }

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string StreamUrl
        {
            get => _streamUrl;
            set
            {
                _streamUrl = value;
                Logger.LogInfo($"流地址设置为: {value}");
            }
        }        public NativeTransparentWindow()
        {
            _httpClient = new System.Net.Http.HttpClient();
            _frameTimer = new System.Windows.Forms.Timer();
            _frameTimer.Interval = 33; // ~30 FPS
            _frameTimer.Tick += OnFrameTimer;

            InitializeWindow();
            Logger.LogInfo("原生透明窗口初始化完成");
        }

        private void InitializeWindow()
        {
            // 基础窗口设置
            this.FormBorderStyle = FormBorderStyle.None;
            this.TopMost = true;
            this.ShowInTaskbar = false;
            this.StartPosition = FormStartPosition.Manual;
            this.Size = new Size(800, 600);
            this.BackColor = _chromaKeyColor;

            // 设置窗口样式为分层窗口
            this.SetStyle(ControlStyles.AllPaintingInWmPaint, true);
            this.SetStyle(ControlStyles.UserPaint, true);
            this.SetStyle(ControlStyles.DoubleBuffer, true);
            this.SetStyle(ControlStyles.SupportsTransparentBackColor, true);

            // 应用透明设置
            this.Load += (s, e) => UpdateTransparencyKey();
        }

        private void UpdateTransparencyKey()
        {
            if (!this.IsHandleCreated) return;

            try
            {
                if (_enableChromaKey)
                {
                    // 设置为分层窗口
                    int exStyle = GetWindowLong(this.Handle, GWL_EXSTYLE);
                    SetWindowLong(this.Handle, GWL_EXSTYLE, exStyle | WS_EX_LAYERED);

                    // 设置透明色
                    uint colorKey = (uint)((_chromaKeyColor.R << 16) | (_chromaKeyColor.G << 8) | _chromaKeyColor.B);
                    SetLayeredWindowAttributes(this.Handle, colorKey, 255, LWA_COLORKEY);

                    this.BackColor = _chromaKeyColor;
                    this.TransparencyKey = _chromaKeyColor;

                    Logger.LogInfo($"原生窗口透明设置完成，颜色: 0x{colorKey:X6}");
                }
                else
                {
                    // 移除分层窗口
                    int exStyle = GetWindowLong(this.Handle, GWL_EXSTYLE);
                    SetWindowLong(this.Handle, GWL_EXSTYLE, exStyle & ~WS_EX_LAYERED);

                    this.BackColor = Color.Black;
                    this.TransparencyKey = Color.Empty;

                    Logger.LogInfo("原生窗口透明已禁用");
                }

                this.Invalidate();
            }
            catch (Exception ex)
            {
                Logger.LogError($"更新透明设置失败: {ex.Message}", ex);
            }
        }

        public void StartStreaming()
        {
            if (!string.IsNullOrEmpty(_streamUrl))
            {
                _frameTimer.Start();
                Logger.LogInfo("开始原生视频流显示");
            }
        }

        public void StopStreaming()
        {
            _frameTimer.Stop();
            Logger.LogInfo("停止原生视频流显示");
        }

        private async void OnFrameTimer(object? sender, EventArgs e)
        {
            try
            {
                // 这里应该从视频流获取帧数据
                // 暂时创建一个示例帧来演示抠像效果
                await CreateDemoFrame();
            }
            catch (Exception ex)
            {
                Logger.LogError($"帧处理错误: {ex.Message}", ex);
            }
        }

        private async Task CreateDemoFrame()
        {
            await Task.Run(() =>
            {
                // 创建示例帧（实际应用中应该从视频流获取）
                var bitmap = new Bitmap(this.Width, this.Height, PixelFormat.Format32bppArgb);
                using (var g = Graphics.FromImage(bitmap))
                {
                    // 填充抠像颜色背景
                    g.Clear(_chromaKeyColor);

                    // 绘制一个示例对象（非抠像颜色）
                    using (var brush = new SolidBrush(Color.Blue))
                    {
                        var rect = new Rectangle(
                            (int)(this.Width * 0.3),
                            (int)(this.Height * 0.3),
                            (int)(this.Width * 0.4),
                            (int)(this.Height * 0.4)
                        );
                        g.FillEllipse(brush, rect);
                    }

                    // 添加文字
                    using (var font = new Font("Arial", 20, FontStyle.Bold))
                    using (var brush = new SolidBrush(Color.White))
                    {
                        var text = "原生透明窗口测试";
                        var size = g.MeasureString(text, font);
                        var point = new PointF(
                            (this.Width - size.Width) / 2,
                            (this.Height - size.Height) / 2
                        );
                        g.DrawString(text, font, brush, point);
                    }
                }

                // 应用抠像处理
                if (_enableChromaKey)
                {
                    ProcessChromaKey(bitmap);
                }

                // 更新显示
                this.Invoke(() =>
                {
                    _currentFrame?.Dispose();
                    _currentFrame = bitmap;
                    this.Invalidate();
                });
            });
        }        private void ProcessChromaKey(Bitmap bitmap)
        {
            if (bitmap == null) return;

            try
            {
                var bitmapData = bitmap.LockBits(
                    new Rectangle(0, 0, bitmap.Width, bitmap.Height),
                    ImageLockMode.ReadWrite,
                    PixelFormat.Format32bppArgb);

                // 使用安全代码替代unsafe
                int bytes = Math.Abs(bitmapData.Stride) * bitmap.Height;
                byte[] pixelBuffer = new byte[bytes];
                
                // 复制像素数据到缓冲区
                Marshal.Copy(bitmapData.Scan0, pixelBuffer, 0, bytes);

                // 处理像素
                for (int i = 0; i < bytes; i += 4)
                {
                    byte b = pixelBuffer[i];     // Blue
                    byte g = pixelBuffer[i + 1]; // Green
                    byte r = pixelBuffer[i + 2]; // Red
                    byte a = pixelBuffer[i + 3]; // Alpha

                    // 计算与目标颜色的距离
                    double distance = Math.Sqrt(
                        Math.Pow(r - _chromaKeyColor.R, 2) +
                        Math.Pow(g - _chromaKeyColor.G, 2) +
                        Math.Pow(b - _chromaKeyColor.B, 2)
                    );

                    if (distance <= _tolerance)
                    {
                        // 设置为完全透明
                        pixelBuffer[i + 3] = 0;
                    }
                }

                // 复制处理后的像素数据回位图
                Marshal.Copy(pixelBuffer, 0, bitmapData.Scan0, bytes);
                bitmap.UnlockBits(bitmapData);
            }
            catch (Exception ex)
            {
                Logger.LogError($"抠像处理失败: {ex.Message}", ex);
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            if (_currentFrame != null)
            {
                e.Graphics.DrawImage(_currentFrame, 0, 0, this.Width, this.Height);
            }
            else
            {
                // 绘制背景色
                using (var brush = new SolidBrush(this.BackColor))
                {
                    e.Graphics.FillRectangle(brush, this.ClientRectangle);
                }

                // 显示提示信息
                using (var font = new Font("Arial", 16))
                using (var brush = new SolidBrush(Color.White))
                {
                    var text = "原生透明窗口\n等待视频流...";
                    var size = e.Graphics.MeasureString(text, font);
                    var point = new PointF(
                        (this.Width - size.Width) / 2,
                        (this.Height - size.Height) / 2
                    );
                    e.Graphics.DrawString(text, font, brush, point);
                }
            }

            base.OnPaint(e);
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            StopStreaming();
            _currentFrame?.Dispose();
            _httpClient?.Dispose();
            _frameTimer?.Dispose();
            base.OnFormClosing(e);
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
    }
}
