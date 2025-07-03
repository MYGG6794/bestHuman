using System;
using System.Drawing;
using System.Windows.Forms;
using System.Runtime.InteropServices;

namespace CoreApplication
{
    public class NativeLayeredWindow : Form
    {
        private PictureBox? pictureBox;
        private System.Windows.Forms.Timer? timer;
        private Bitmap? currentFrame;
        private string? streamAddress;
        private Color chromaKeyColor = Color.Lime;
        private bool enableChromaKey = true;

        // Win32 API常量
        private const int WS_EX_APPWINDOW = 0x00040000;
        private const int WS_EX_TOOLWINDOW = 0x00000080;
        private const int WM_NCHITTEST = 0x84;
        private const int HTCAPTION = 0x2;
        private const int WM_SYSCOMMAND = 0x112;
        private const int SC_CLOSE = 0xF060;
        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);
        [DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);
        [DllImport("user32.dll")]
        private static extern bool ReleaseCapture();
        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);
        private const int GWL_EXSTYLE = -20;

        // 全局热键支持
        private const int WM_HOTKEY = 0x0312;
        private const int HOTKEY_ID_SETTINGS_S = 1001;
        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);
        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);
        private const uint MOD_ALT = 0x0001;
        private const uint VK_S = 0x53;
        private SettingsForm? settingsForm;

        private SettingsForm.AppSettings? appSettings;
        private AIService? aiService;

        public NativeLayeredWindow(string streamAddress) : this()
        {
            this.streamAddress = streamAddress;
            // 加载配置
            this.appSettings = SettingsForm.AppSettings.Load();
            // 创建AIService实例（与Program.cs保持一致）
            if (this.appSettings != null)
            {
                var aiServiceConfig = new AIServiceConfig
                {
                    ModelPath = this.appSettings.ModelPath,
                    KnowledgeBasePath = this.appSettings.KnowledgeBasePath,
                    UseGPU = this.appSettings.UseGPU,
                    EnableCloudFallback = this.appSettings.EnableCloudFallback,
                    CloudAPIKey = this.appSettings.CloudAPIKey,
                    CloudAPIEndpoint = this.appSettings.CloudAPIEndpoint
                };
                this.aiService = new AIService(Program.WebSocketClient, aiServiceConfig);
            }
            InitUI();
            StartStream();
        }

        public NativeLayeredWindow()
        {
            this.Text = "bestHuman 数字人助手 - 原生透明窗口";
            this.FormBorderStyle = FormBorderStyle.None;
            this.TopMost = true;
            this.ShowInTaskbar = true; // 让窗口显示在任务栏
            this.Size = new Size(800, 800);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = chromaKeyColor;
            this.TransparencyKey = chromaKeyColor;
            // 设置应用图标（可替换为项目资源）
            try { this.Icon = SystemIcons.Application; } catch { }
            // 修改扩展样式，确保显示在任务栏和Alt+Tab
            this.Load += (s, e) => {
                int exStyle = GetWindowLong(this.Handle, GWL_EXSTYLE);
                exStyle |= WS_EX_APPWINDOW;
                exStyle &= ~WS_EX_TOOLWINDOW;
                SetWindowLong(this.Handle, GWL_EXSTYLE, exStyle);
            };
            // 支持右键菜单
            this.ContextMenuStrip = BuildContextMenu();
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            RegisterHotKey(this.Handle, HOTKEY_ID_SETTINGS_S, MOD_ALT, VK_S);
        }

        protected override void OnHandleDestroyed(EventArgs e)
        {
            UnregisterHotKey(this.Handle, HOTKEY_ID_SETTINGS_S);
            base.OnHandleDestroyed(e);
        }

        private void InitUI()
        {
            pictureBox = new PictureBox
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent,
                SizeMode = PictureBoxSizeMode.Zoom
            };
            this.Controls.Add(pictureBox);
        }

        private void StartStream()
        {
            timer = new System.Windows.Forms.Timer { Interval = 1000 / 30 };
            timer.Tick += (s, e) =>
            {
                if (currentFrame != null && pictureBox != null)
                {
                    var bmp = ApplyChromaKey(currentFrame);
                    pictureBox.Image = bmp;
                }
            };
            timer.Start();
        }

        public void SetChromaKeyColor(Color color)
        {
            chromaKeyColor = color;
            this.BackColor = color;
            this.TransparencyKey = color;
        }

        public void EnableChromaKey(bool enable)
        {
            enableChromaKey = enable;
        }

        public void UpdateFrame(Bitmap frame)
        {
            currentFrame = frame;
        }

        private Bitmap ApplyChromaKey(Bitmap src)
        {
            if (!enableChromaKey) return src;
            Bitmap bmp = new Bitmap(src.Width, src.Height);
            for (int y = 0; y < src.Height; y++)
            for (int x = 0; x < src.Width; x++)
            {
                Color c = src.GetPixel(x, y);
                if (IsGreen(c))
                    bmp.SetPixel(x, y, Color.FromArgb(0, c));
                else
                    bmp.SetPixel(x, y, c);
            }
            return bmp;
        }

        private bool IsGreen(Color c)
        {
            return c.G > 180 && c.G > c.R + 40 && c.G > c.B + 40 && !(c.R > 200 && c.G > 200 && c.B > 200);
        }

        // 支持窗口拖动
        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_NCHITTEST)
            {
                base.WndProc(ref m);
                if ((int)m.Result == 1) // HTCLIENT
                    m.Result = (IntPtr)HTCAPTION;
                return;
            }
            if (m.Msg == WM_HOTKEY && m.WParam.ToInt32() == HOTKEY_ID_SETTINGS_S)
            {
                ShowSettingsForm();
                return;
            }
            base.WndProc(ref m);
        }

        private void ShowSettingsForm()
        {
            if (settingsForm == null || settingsForm.IsDisposed)
                settingsForm = new SettingsForm(appSettings ?? SettingsForm.AppSettings.Load(), aiService);
            // 设置窗口显示位置：主窗口右侧优先，若超出屏幕则居中
            var screen = Screen.FromControl(this);
            int offset = 40; // 偏移量
            int targetX = this.Right + offset;
            int targetY = this.Top + offset;
            // 如果右侧空间足够，则放右侧，否则居中
            if (targetX + settingsForm.Width < screen.WorkingArea.Right)
            {
                settingsForm.StartPosition = FormStartPosition.Manual;
                settingsForm.Location = new Point(targetX, Math.Max(screen.WorkingArea.Top, targetY));
            }
            else
            {
                settingsForm.StartPosition = FormStartPosition.CenterScreen;
            }
            settingsForm.TopMost = true; // 确保在最前
            settingsForm.Show();
            settingsForm.BringToFront();
            settingsForm.Activate();
        }

        // 构建右键菜单
        private ContextMenuStrip BuildContextMenu()
        {
            var menu = new ContextMenuStrip();
            menu.Items.Add("关闭", null, (s, e) => this.Close());
            menu.Items.Add("重置位置", null, (s, e) => this.CenterToScreen());
            return menu;
        }
    }
}
