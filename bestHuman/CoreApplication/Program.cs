using System;
using System.Windows.Forms;
using System.Runtime.InteropServices; // For P/Invoke
using CoreApplication; // For Logger

namespace CoreApplication
{
    static class Program
    {
        // 提供全局访问点
        public static WebSocketClient WebSocketClient { get; private set; } = null!;        [STAThread]
        static void Main(string[] args)
        {
            Logger.Initialize("app.log"); // 初始化日志系统
            Logger.LogInfo("应用程序启动中...");

            try
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);

                // 加载应用程序设置
                var appSettings = SettingsForm.AppSettings.Load();
                if (appSettings == null)
                {
                    // 如果加载失败，使用默认设置或提示用户
                    Logger.LogWarning("无法加载应用设置，将使用默认设置。");
                    appSettings = new SettingsForm.AppSettings(); 
                    // Consider prompting the user or initializing with safe defaults
                }                // 创建 AIService 实例
                // AIService constructor takes WebSocketClient and AIServiceConfig
                // We need to create WebSocketClient first, then create AIServiceConfig from appSettings
                WebSocketClient = new WebSocketClient();
                
                var aiServiceConfig = new AIServiceConfig
                {
                    ModelPath = appSettings.ModelPath,
                    KnowledgeBasePath = appSettings.KnowledgeBasePath,
                    UseGPU = appSettings.UseGPU,
                    EnableCloudFallback = appSettings.EnableCloudFallback,
                    CloudAPIKey = appSettings.CloudAPIKey,
                    CloudAPIEndpoint = appSettings.CloudAPIEndpoint
                };
                  var aiService = new AIService(WebSocketClient, aiServiceConfig);                // 根据配置决定启动哪种窗口模式                // 根据配置决定启动哪种窗口模式
                // 暂时只支持 WebView2 模式，原生窗口功能开发中
                /*
                if (appSettings.UseNativeLayeredWindow)
                {
                    Logger.LogInfo("使用原生透明窗口模式启动数字人显示。");
                    var nativeWindow = new NativeLayeredWindow(appSettings.StreamAddress);
                    
                    // 应用抠像设置
                    nativeWindow.EnableChromaKey = appSettings.EnableChromaKey;
                    nativeWindow.ChromaKeyColor = appSettings.ChromaKeyColor;
                    
                    Application.Run(nativeWindow);
                }
                else
                {
                */
                    Logger.LogInfo("使用WebView2窗口模式启动数字人显示。");
                    Application.Run(new MainForm(aiService, appSettings));
                //}
            }
            catch (Exception ex)
            {
                Logger.LogError("应用程序发生未处理的异常！", ex);
                MessageBox.Show($"应用程序发生错误：{ex.Message}{Environment.NewLine}请查看 app.log 获取详细信息。", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                Logger.LogInfo("应用程序已关闭。");
            }
        }
    }

    public class MainForm : Form
    {
        // Windows API declarations for click-through and topmost
        [DllImport("user32.dll", SetLastError = true)]
        public static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        public static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll")]
        public static extern bool SetLayeredWindowAttributes(IntPtr hwnd, uint crKey, byte bAlpha, uint dwFlags);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        // 全局热键API
        [DllImport("user32.dll")]
        public static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll")]
        public static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        // 热键修饰符常量
        public const uint MOD_ALT = 0x0001;
        public const uint MOD_CONTROL = 0x0002;
        public const uint MOD_SHIFT = 0x0004;
        public const uint MOD_WIN = 0x0008;

        // 虚拟键码
        public const uint VK_F10 = 0x79;
        public const uint VK_S = 0x53;

        // 热键ID
        private const int HOTKEY_ID_SETTINGS_F10 = 1;
        private const int HOTKEY_ID_SETTINGS_S = 2;

        // Window styles
        public const int GWL_EXSTYLE = -20;
        public const int WS_EX_LAYERED = 0x80000;
        public const int WS_EX_TRANSPARENT = 0x20;
        public const int WS_EX_TOPMOST = 0x00000008; // For topmost

        // SetLayeredWindowAttributes flags
        public const uint LWA_ALPHA = 0x2;
        public const uint LWA_COLORKEY = 0x1;
        // SetWindowPos flags
        public const uint SWP_NOSIZE = 0x0001;
        public const uint SWP_NOMOVE = 0x0002;
        public const uint SWP_NOACTIVATE = 0x0010;

        // Special window handles
        public static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        public static readonly IntPtr HWND_NOTOPMOST = new IntPtr(-2);

        // Windows消息常量
        private const int WM_HOTKEY = 0x0312;        private DigitalHumanDisplay? digitalHumanDisplay; // Made nullable since it's only used in WebView mode
        private SettingsForm settingsForm;
        private SettingsForm.AppSettings appSettings;
        private WebSocketClient webSocketClient;
        private SpeechService? speechService; // 声明为可空
        private AIService? aiService;
        private AIManagerForm? aiManagerForm;
        //private NativeLayeredWindow? nativeWindow; // 原生透明窗口

        // 在MainForm类中添加这个变量来跟踪Alt键状态
        private bool isAltKeyPressed = false;        public MainForm(AIService aiService, SettingsForm.AppSettings appSettings)
        {
            Logger.LogInfo("MainForm 构造函数开始。");
            
            // 使用传入的配置和服务
            this.appSettings = appSettings;
            this.aiService = aiService;
            Text = "bestHuman 数字人助手";
            Size = new System.Drawing.Size(800, 600); // 固定一个合理的大小
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.None; // 无边框窗口            // 根据抠像设置决定背景色和透明Key
            if (appSettings.EnableChromaKey)
            {
                this.BackColor = appSettings.ChromaKeyColor;  // 抠像模式使用选择的抠像颜色背景
                this.TransparencyKey = appSettings.ChromaKeyColor; // 抠像颜色透明
                Logger.LogInfo($"主窗口设置为抠像模式 - 抠像颜色背景透明: R={appSettings.ChromaKeyColor.R}, G={appSettings.ChromaKeyColor.G}, B={appSettings.ChromaKeyColor.B}");
            }
            else
            {
                this.BackColor = Color.Black;  // 非抠像模式使用黑色背景
                this.TransparencyKey = Color.Empty; // 不透明
                Logger.LogInfo("主窗口设置为正常模式 - 黑色背景不透明");
            }
            
            // 强制刷新窗口
            this.Invalidate();
            this.Update();
            
            // 窗口加载完成后强制设置透明（如果需要）
            this.Load += (sender, e) => {
                if (appSettings.EnableChromaKey)
                {
                    ForceWindowTransparency(true);
                    Logger.LogInfo("窗口加载完成，强制设置透明");
                }
            };
            
            // 确保窗口在最前面
            this.TopMost = true;
            this.WindowState = FormWindowState.Normal;
            
            // 确保窗口样式支持透明
            this.SetStyle(ControlStyles.SupportsTransparentBackColor, true);
            this.SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
            this.SetStyle(ControlStyles.AllPaintingInWmPaint, true);
            
            // 强制刷新窗口
            this.Invalidate();
            this.Update();
            
            Logger.LogInfo($"主窗口初始设置 - 尺寸: {this.Width}x{this.Height}, 位置: {this.Location}");
            
            // 确保窗体可以接收键盘事件 - 这是关键步骤
            this.ShowInTaskbar = true; // 显示在任务栏
            this.Enabled = true;
            this.TabStop = true; // 允许 Tab 键焦点导航
            this.KeyPreview = true; // 允许窗体接收按键事件，这是最重要的设置
            this.Focus(); // 确保窗体获得焦点
            
            // 只注册一次键盘事件（避免重复注册）
            this.KeyDown -= MainForm_KeyDown; // 确保先移除已有的订阅
            this.KeyUp -= MainForm_KeyUp;
            this.KeyDown += MainForm_KeyDown; // 添加新的订阅
            this.KeyUp += MainForm_KeyUp;
            
            // 初始化窗体激活事件，用于确保窗体保持对键盘事件的响应能力
            this.Activated += (s, e) => {
                Logger.LogInfo("MainForm 窗体被激活");
                this.Focus(); // 再次确保窗体获得焦点
            };
            
            // 注册全局热键
            RegisterGlobalHotkeys();
            
            // 添加定时器，每10秒检查一次键盘输入状态
            System.Windows.Forms.Timer inputCheckTimer = new System.Windows.Forms.Timer();
            inputCheckTimer.Interval = 10000; // 10秒
            inputCheckTimer.Tick += (s, e) => {
                if (this.ContainsFocus)
                {
                    Logger.LogInfo("定时检查：主窗体拥有焦点，键盘输入正常");
                }
                else
                {
                    Logger.LogInfo("定时检查：主窗体无焦点，尝试重获焦点");
                    this.Activate();
                    this.Focus();
                    ResetKeyboardHandling();
                }
            };
            inputCheckTimer.Start();            // 根据设置初始化显示模式
            if (appSettings.UseNativeLayeredWindow)
            {
                // 使用原生透明窗口模式
                Logger.LogInfo("初始化原生透明窗口模式");
                InitializeNativeLayeredWindowMode();
            }
            else
            {
                // 使用 WebView2 模式
                Logger.LogInfo("初始化 WebView2 显示模式");
                InitializeWebViewMode();
            }

            // 根据配置设置窗口置顶和点击穿透
            SetTopMost(appSettings.TopMostEnabled);
            EnableClickThrough(appSettings.ClickThroughEnabled);            // 初始化 SettingsForm，传入 AIService 实例
            settingsForm = new SettingsForm(appSettings, aiService);
            settingsForm.SettingsChanged += SettingsForm_SettingsChanged;

            // 使用全局 WebSocketClient 实例
            webSocketClient = Program.WebSocketClient;
            webSocketClient.OnConnected += WebSocketClient_OnConnected;
            webSocketClient.OnDisconnected += WebSocketClient_OnDisconnected;
            webSocketClient.OnMessageReceived += WebSocketClient_OnMessageReceived;
            webSocketClient.OnError += WebSocketClient_OnError;

            // 尝试连接 WebSocket
            _ = webSocketClient.ConnectAsync(appSettings.WebSocketServerAddress);            // 初始化 SpeechService
            speechService = new SpeechService();
            speechService.OnSpeechRecognized += SpeechService_OnSpeechRecognized;
            speechService.OnSpeechSynthesisStarted += SpeechService_OnSpeechSynthesisStarted;
            speechService.OnSpeechSynthesisEnded += SpeechService_OnSpeechSynthesisEnded;
            speechService.OnError += SpeechService_OnError;

            // AIService 已从外部传入，无需重新创建
            // 创建 AIManagerForm 实例
            aiManagerForm = new AIManagerForm(aiService, appSettings);
            Logger.LogInfo("MainForm 构造函数结束。");
        }        // 窗口加载完成后确保透明生效
        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            
            // 如果抠像已启用，设置透明色
            if (appSettings.EnableChromaKey)
            {
                this.BackColor = Color.Green; // 使用绿色，与视频绿幕一致
                this.TransparencyKey = Color.Green;
                Logger.LogInfo("窗口加载完成，抠像已启用 - 设置绿色透明");
            }
            else
            {
                Logger.LogInfo("窗口加载完成，抠像未启用 - 保持不透明");
            }
            
            // 显示确认消息（仅在调试模式）
            MessageBox.Show($"bestHuman程序启动成功！\n抠像状态: {(appSettings.EnableChromaKey ? "已启用" : "未启用")}", "启动确认", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        // 窗口显示后再次确保透明
        protected override void SetVisibleCore(bool value)
        {
            base.SetVisibleCore(value);
            
            if (value && appSettings.EnableChromaKey)
            {
                // 如果抠像启用，延迟设置透明
                this.BeginInvoke(new Action(() =>
                {
                    this.BackColor = Color.Green;
                    this.TransparencyKey = Color.Green;
                    this.Refresh();
                    Logger.LogInfo("窗口显示后设置绿色透明");
                }));
            }
        }

        private void WebSocketClient_OnConnected(object? sender, EventArgs e)
        {
            Logger.LogInfo("WebSocket 连接成功事件。");
            // TODO: 更新UI状态，通知其他模块
            speechService?.StartSpeechRecognition(); // 连接成功后启动语音识别
        }

        // 在窗体关闭时断开 WebSocket 连接
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);
            
            // 注销全局热键
            UnregisterGlobalHotkeys();
            
            if (webSocketClient != null)
            {
                _ = webSocketClient.DisconnectAsync();
                webSocketClient.Dispose();
            }
            if (speechService != null)
            {
                speechService.Dispose();
            }
            if (aiService != null)
            {
                aiService.Dispose();
            }
            if (aiManagerForm != null)
            {
                aiManagerForm.Dispose();
            }
        }

        private void WebSocketClient_OnDisconnected(object? sender, EventArgs e)
        {
            Logger.LogInfo("WebSocket 连接断开事件。");
            // TODO: 更新UI状态，通知其他模块
            speechService?.StopSpeechRecognition(); // 断开连接后停止语音识别
        }

        private void WebSocketClient_OnMessageReceived(object? sender, string message)
        {
            Logger.LogInfo($"WebSocket 接收到消息: {message}");
            // 假设接收到的消息是 TTS 文本
            // TODO: 解析消息，分发给其他模块
            // 例如：如果消息是 {"type": "tts", "text": "你好"}
            // string ttsText = ParseTtsTextFromJson(message);
            speechService?.SynthesizeSpeech(message, appSettings.TtsVoiceName, appSettings.TtsRate, appSettings.TtsVolume);
        }

        private void WebSocketClient_OnError(object? sender, string errorMessage)
        {
            Logger.LogError($"WebSocket 错误: {errorMessage}");
            // TODO: 更新UI状态，显示错误信息
        }

        private void SpeechService_OnSpeechRecognized(object? sender, string recognizedText)
        {
            Logger.LogInfo($"语音识别结果事件: {recognizedText}");
            // 将识别结果通过 WebSocket 发送给 UE 端
            _ = webSocketClient.SendMessageAsync($"{{\"type\": \"asr\", \"text\": \"{recognizedText}\"}}");
        }

        private void SpeechService_OnSpeechSynthesisStarted(object? sender, EventArgs e)
        {
            Logger.LogInfo("语音合成开始播放事件。");
            // TODO: 通知 UE 端 TTS 开始播放
            _ = webSocketClient.SendMessageAsync("{\"type\": \"tts_status\", \"status\": \"started\"}");
        }

        private void SpeechService_OnSpeechSynthesisEnded(object? sender, EventArgs e)
        {
            Logger.LogInfo("语音合成播放完成事件。");
            // TODO: 通知 UE 端 TTS 播放完成
            _ = webSocketClient.SendMessageAsync("{\"type\": \"tts_status\", \"status\": \"ended\"}");
        }

        private void SpeechService_OnError(object? sender, string errorMessage)
        {
            Logger.LogError($"语音服务错误: {errorMessage}");
            // TODO: 更新UI状态，显示错误信息
        }

        // 处理 SettingsForm 的设置更改事件
        private void SettingsForm_SettingsChanged(object? sender, SettingsForm.SettingsChangedEventArgs e)
        {
            Logger.LogInfo("SettingsForm_SettingsChanged 事件触发。");
            appSettings = e.Settings;
            ApplySettings(appSettings);
            SaveSettings(appSettings); // 保存更改后的设置

            // 重新连接 WebSocket（如果地址有变化）
            if (webSocketClient.IsConnected)
            {
                _ = webSocketClient.DisconnectAsync();
            }
            _ = webSocketClient.ConnectAsync(appSettings.WebSocketServerAddress);

            // 更新语音服务设置
            speechService?.SynthesizeSpeech("设置已更新。", appSettings.TtsVoiceName, appSettings.TtsRate, appSettings.TtsVolume);
              // 确保主窗体重新获得焦点和键盘事件处理
            this.Activate();
            this.Focus();
            ResetKeyboardHandling();
        }        // 应用设置到主窗口和数字人显示模块
        private void ApplySettings(SettingsForm.AppSettings settings)
        {
            Logger.LogInfo("应用设置中...");
            // 更新窗口大小和位置
            this.Size = new System.Drawing.Size(settings.WindowWidth, settings.WindowHeight);
            if (settings.WindowX != -1 && settings.WindowY != -1)
            {
                this.Location = new Point(settings.WindowX, settings.WindowY);
            }            // 根据抠像设置更新窗口透明色
            if (settings.EnableChromaKey)
            {
                // 启用抠像时设置窗口透明色为当前选择的抠像颜色
                this.BackColor = settings.ChromaKeyColor;
                this.TransparencyKey = settings.ChromaKeyColor;
                
                // 使用Windows API强制设置透明
                ForceWindowTransparency(true);
                Logger.LogInfo($"启用抠像，设置窗口透明色为: R={settings.ChromaKeyColor.R}, G={settings.ChromaKeyColor.G}, B={settings.ChromaKeyColor.B}");
            }
            else
            {
                // 禁用抠像时移除窗口透明色
                this.BackColor = Color.Black;
                this.TransparencyKey = Color.Empty;
                
                // 移除Windows API透明设置
                ForceWindowTransparency(false);
                Logger.LogInfo("禁用抠像，移除窗口透明色");
            }            // 更新数字人显示控件的抠像设置
            if (digitalHumanDisplay != null)
            {
                digitalHumanDisplay.EnableChromaKey = settings.EnableChromaKey;
                digitalHumanDisplay.ChromaKeyColor = settings.ChromaKeyColor;
                digitalHumanDisplay.Tolerance = settings.ChromaKeyTolerance;
                digitalHumanDisplay.SetChromaKeyEnabled(settings.EnableChromaKey);
                Logger.LogInfo($"更新数字人显示控件抠像设置: {settings.EnableChromaKey}, 颜色: R={settings.ChromaKeyColor.R}, G={settings.ChromaKeyColor.G}, B={settings.ChromaKeyColor.B}");
                
                // 更新推流地址
                digitalHumanDisplay.LoadStreamAsync(settings.StreamAddress);
            }
            
            // 强制刷新窗口透明设置
            this.Invalidate();
            this.Update();
            // 更新窗口置顶和点击穿透
            SetTopMost(settings.TopMostEnabled);
            EnableClickThrough(settings.ClickThroughEnabled);
            
            Logger.LogInfo("设置应用完成。");
        }

        private void MainForm_KeyUp(object? sender, KeyEventArgs e)
        {
            Logger.LogInfo($"KeyUp event triggered. KeyCode: {e.KeyCode}, Modifiers: {e.Modifiers}, KeyData: {e.KeyData}, 设置窗口可见:{settingsForm?.Visible}");
            
            // 在KeyUp事件中检测Alt+F10或Alt+S组合键
            if (e.KeyData == (Keys.Alt | Keys.F10) || e.KeyData == (Keys.Alt | Keys.S))
            {
                Logger.LogInfo($"Settings hotkey detected in KeyUp event via KeyData: {e.KeyData}");
                ToggleSettingsWindow();
                e.Handled = true;
                e.SuppressKeyPress = true;
                return;
            }
            
            // 当Alt键被释放时，重置标志
            if (e.KeyCode == Keys.Menu || e.KeyCode == Keys.Alt)
            {
                Logger.LogInfo("Alt key released, resetting flag to false");
                isAltKeyPressed = false;
            }
        }

        private void MainForm_KeyDown(object? sender, KeyEventArgs e)
        {
            Logger.LogInfo($"KeyDown event triggered. KeyCode: {e.KeyCode}, Modifiers: {e.Modifiers}, KeyData: {e.KeyData}, isAltKeyPressed: {isAltKeyPressed}, 设置窗口可见:{settingsForm?.Visible}");

            // 方法1: 直接通过KeyData检测组合键 (更可靠的方式)
            if (e.KeyData == (Keys.Alt | Keys.F10) || e.KeyData == (Keys.Alt | Keys.S))
            {
                Logger.LogInfo($"Settings hotkey detected via KeyData: {e.KeyData}");
                ToggleSettingsWindow();
                e.Handled = true;
                e.SuppressKeyPress = true;
                return;
            }
            
            // 方法2: 通过Modifiers和KeyCode检测组合键
            if ((e.Modifiers == Keys.Alt && e.KeyCode == Keys.F10) || 
                (e.Modifiers == Keys.Alt && e.KeyCode == Keys.S))
            {
                Logger.LogInfo($"Settings hotkey detected via Modifiers+KeyCode: {e.Modifiers}+{e.KeyCode}");
                ToggleSettingsWindow();
                e.Handled = true;
                e.SuppressKeyPress = true;
                return;
            }

            // 方法3: 分步检测 (先检测Alt, 再检测功能键)
            // 检测Alt键按下
            if (e.KeyCode == Keys.Menu || e.KeyCode == Keys.Alt)
            {
                Logger.LogInfo("Alt key pressed, setting flag to true");
                isAltKeyPressed = true;
                return;
            }

            // 特殊处理：检测F10键按下，并且Alt标志为true或Alt修饰符当前激活
            if (e.KeyCode == Keys.F10 && (isAltKeyPressed || e.Modifiers.HasFlag(Keys.Alt)))
            {
                Logger.LogInfo("Alt + F10 hotkey detected with special handling for F10");
                ToggleSettingsWindow();
                e.Handled = true;
                e.SuppressKeyPress = true;
                return;
            }

            // 检测S键按下，并且Alt标志为true
            if (e.KeyCode == Keys.S && isAltKeyPressed)
            {
                Logger.LogInfo("Alt + S hotkey detected via two-step detection");
                ToggleSettingsWindow();
                e.Handled = true;
                e.SuppressKeyPress = true;
                return;
            }

            // 其他快捷键处理 Alt + A
            if (e.KeyCode == Keys.A && isAltKeyPressed)
            {
                Logger.LogInfo("Alt + A 快捷键被按下。");
                if (aiService != null)
                {
                    if (aiManagerForm == null || aiManagerForm.IsDisposed)
                    {
                        Logger.LogInfo("创建新的AI管理界面实例。");
                        aiManagerForm = new AIManagerForm(aiService, appSettings);
                    }

                    if (aiManagerForm.Visible)
                    {
                        Logger.LogInfo("AI管理界面已隐藏。");
                        aiManagerForm.Hide();
                    }
                    else
                    {
                        Logger.LogInfo("AI管理界面已显示。");
                        aiManagerForm.Show();
                        aiManagerForm.BringToFront();
                        aiManagerForm.Activate();
                    }
                }
                else
                {
                    Logger.LogError("AI服务未初始化，无法打开AI管理界面。");
                }
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
        }

        // 提取设置窗口切换逻辑到单独方法，避免代码重复
        private void ToggleSettingsWindow()
        {
            try
            {
                Logger.LogInfo($"ToggleSettingsWindow调用：当前窗口状态 - 存在:{settingsForm != null}，可见:{settingsForm?.Visible}，已释放:{settingsForm?.IsDisposed}");
                
                // 如果设置窗口未创建或已释放，则重新创建
                if (settingsForm == null || settingsForm.IsDisposed)
                {                    Logger.LogInfo("创建新的设置窗口实例。");
                    settingsForm = new SettingsForm(appSettings, aiService);
                    if (settingsForm != null)
                    {
                        settingsForm.SettingsChanged += SettingsForm_SettingsChanged;
                    }
                }                // 切换设置窗口的可见状态
                if (settingsForm != null && settingsForm.Visible)
                {
                    Logger.LogInfo("隐藏设置窗口。");
                    settingsForm.Hide();
                    // 确保主窗体重新获得焦点
                    this.Activate();
                    this.Focus();
                    // 重置键盘事件处理
                    ResetKeyboardHandling();
                }                else if (settingsForm != null)
                {
                    Logger.LogInfo("显示设置窗口并激活。");
                    settingsForm.Show();
                    settingsForm.BringToFront(); // 确保窗口在最前
                    settingsForm.Activate();     // 激活窗口以获取焦点

                    // 确保窗口在屏幕可见区域内
                    Rectangle screenBounds = Screen.FromControl(this).WorkingArea;
                    if (!screenBounds.IntersectsWith(settingsForm.Bounds))
                    {
                        Logger.LogInfo("重置设置窗口位置到屏幕中心。");
                        settingsForm.Location = new Point(
                            screenBounds.X + (screenBounds.Width - settingsForm.Width) / 2,
                            screenBounds.Y + (screenBounds.Height - settingsForm.Height) / 2
                        );
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"处理设置窗口快捷键时出错: {ex.Message}", ex);
                MessageBox.Show(
                    "打开设置窗口时发生错误，请重试。\n\n" + ex.Message,
                    "错误",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
        }

        // 设置窗口置顶
        public void SetTopMost(bool topmost)
        {
            Logger.LogInfo($"设置窗口置顶: {topmost}");
            if (topmost)
            {
                SetWindowPos(this.Handle, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE);
            }
            else
            {
                SetWindowPos(this.Handle, HWND_NOTOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE);
            }
        }

        // 启用/禁用点击穿透
        public void EnableClickThrough(bool enable)
        {
            Logger.LogInfo($"启用点击穿透: {enable}");
            
            int exStyle = GetWindowLong(this.Handle, GWL_EXSTYLE);
            if (enable)
            {
                // 添加 WS_EX_LAYERED 和 WS_EX_TRANSPARENT 样式
                exStyle |= (WS_EX_LAYERED | WS_EX_TRANSPARENT);
                SetWindowLong(this.Handle, GWL_EXSTYLE, exStyle);
                
                // 设置窗口完全不透明，但允许点击穿透
                SetLayeredWindowAttributes(this.Handle, 0, 255, LWA_ALPHA);

                // 即使启用点击穿透，也要确保能接收键盘事件
                this.KeyPreview = true;
                this.ShowInTaskbar = true;
                
                // 重新注册全局热键以确保在点击穿透模式下仍能工作
                Logger.LogInfo("点击穿透模式下重新注册全局热键");
                UnregisterGlobalHotkeys();
                RegisterGlobalHotkeys();
            }
            else
            {
                // 移除 WS_EX_LAYERED 和 WS_EX_TRANSPARENT 样式
                exStyle &= ~(WS_EX_LAYERED | WS_EX_TRANSPARENT);
                SetWindowLong(this.Handle, GWL_EXSTYLE, exStyle);
                
                // 重新注册全局热键
                Logger.LogInfo("非点击穿透模式下重新注册全局热键");
                UnregisterGlobalHotkeys();
                RegisterGlobalHotkeys();
            }

            // 强制窗口重绘以应用更改
            this.Invalidate();
        }

        // 配置持久化：加载设置
        private SettingsForm.AppSettings LoadSettings()
        {
            Logger.LogInfo("加载应用程序设置。");
            // 简单示例：从文件加载，这里先返回默认设置
            // 实际应用中会从 JSON/XML 文件读取
            return new SettingsForm.AppSettings();
        }

        // 配置持久化：保存设置
        private void SaveSettings(SettingsForm.AppSettings settings)
        {
            Logger.LogInfo("保存应用程序设置。");
            // TODO: 实现配置的序列化和保存到文件
            // 例如：string json = System.Text.Json.JsonSerializer.Serialize(settings);
            // System.IO.File.WriteAllText("appsettings.json", json);
        }

        // 提供一个公共方法，用于重置键盘事件处理
        public void ResetKeyboardHandling()
        {
            Logger.LogInfo("重置键盘事件处理");
            // 确保窗体可以接收键盘事件
            this.Focus();
            this.BringToFront();
            
            // 重置Alt键标志
            isAltKeyPressed = false;

            // 重新注册键盘事件（先取消再注册，避免重复）
            this.KeyDown -= MainForm_KeyDown;
            this.KeyUp -= MainForm_KeyUp;
            this.KeyDown += MainForm_KeyDown;
            this.KeyUp += MainForm_KeyUp;
            
            Logger.LogInfo($"主窗体键盘事件重置完成，KeyPreview={this.KeyPreview}, Focused={this.Focused}, Enabled={this.Enabled}, TopLevel={this.TopLevel}");
        }

        // 覆盖ProcessCmdKey方法，确保即使在其他事件处理失效的情况下也能捕获快捷键
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            // 检查是否是我们关注的快捷键
            if (keyData == (Keys.Alt | Keys.F10) || keyData == (Keys.Alt | Keys.S))
            {
                Logger.LogInfo($"通过ProcessCmdKey捕获到快捷键: {keyData}");
                ToggleSettingsWindow();
                return true; // 返回true表示我们已经处理了这个按键
            }
            
            // 对于其他按键，让基类处理
            return base.ProcessCmdKey(ref msg, keyData);
        }

        // 注册全局热键
        private void RegisterGlobalHotkeys()
        {
            try
            {
                // 注册 Alt+F10
                bool result1 = RegisterHotKey(this.Handle, HOTKEY_ID_SETTINGS_F10, MOD_ALT, VK_F10);
                Logger.LogInfo($"注册全局热键 Alt+F10: {(result1 ? "成功" : "失败")}");
                
                // 注册 Alt+S 作为备选
                bool result2 = RegisterHotKey(this.Handle, HOTKEY_ID_SETTINGS_S, MOD_ALT, VK_S);
                Logger.LogInfo($"注册全局热键 Alt+S: {(result2 ? "成功" : "失败")}");
                
                if (!result1 && !result2)
                {
                    Logger.LogWarning("所有全局热键注册失败，将依赖窗体按键事件");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"注册全局热键时出错: {ex.Message}", ex);
            }
        }

        // 注销全局热键
        private void UnregisterGlobalHotkeys()
        {
            try
            {
                UnregisterHotKey(this.Handle, HOTKEY_ID_SETTINGS_F10);
                UnregisterHotKey(this.Handle, HOTKEY_ID_SETTINGS_S);
                Logger.LogInfo("已注销所有全局热键");
            }
            catch (Exception ex)
            {
                Logger.LogError($"注销全局热键时出错: {ex.Message}", ex);
            }
        }

        // 重写WndProc方法来处理全局热键消息
        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_HOTKEY)
            {
                int hotkeyId = m.WParam.ToInt32();
                Logger.LogInfo($"全局热键被触发，ID: {hotkeyId}");
                
                switch (hotkeyId)
                {
                    case HOTKEY_ID_SETTINGS_F10:
                    case HOTKEY_ID_SETTINGS_S:
                        Logger.LogInfo("通过全局热键触发设置窗口切换");
                        ToggleSettingsWindow();
                        return; // 不调用base.WndProc，表示我们已经处理了这个消息
                }
            }
            
            base.WndProc(ref m);
        }

        // 设置抠像模式的主窗口背景
        private void SetChromaKeyBackground(bool enabled)
        {
            if (enabled)
            {
                this.BackColor = Color.Lime; // 启用抠像时设置绿色背景
                Logger.LogInfo("主窗口背景设置为透明键颜色");
            }
            else
            {
                this.BackColor = SystemColors.Control; // 禁用时恢复默认背景
                Logger.LogInfo("主窗口背景恢复为默认颜色");
            }
        }        // 强制设置窗口透明度的方法
        private void ForceWindowTransparency(bool enabled)
        {
            try
            {
                if (enabled)
                {
                    // 使用当前设置的抠像颜色作为透明色
                    Color chromaColor = appSettings?.ChromaKeyColor ?? Color.Green;
                    uint colorValue = (uint)((chromaColor.R << 16) | (chromaColor.G << 8) | chromaColor.B);
                    
                    // 方法1：设置窗口为分层窗口
                    int exStyle = GetWindowLong(this.Handle, GWL_EXSTYLE);
                    SetWindowLong(this.Handle, GWL_EXSTYLE, exStyle | WS_EX_LAYERED);
                    SetLayeredWindowAttributes(this.Handle, colorValue, 255, LWA_COLORKEY);
                    
                    // 方法2：强制刷新窗口区域（确保透明色生效）
                    this.Invalidate();
                    this.Update();
                    
                    // 方法3：强制重新绘制透明区域
                    SetWindowPos(this.Handle, HWND_TOPMOST, 0, 0, 0, 0, 
                        SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE);
                    
                    Logger.LogInfo($"Windows API强制透明设置完成，颜色值: 0x{colorValue:X6} (R={chromaColor.R}, G={chromaColor.G}, B={chromaColor.B})");
                    
                    // 额外的透明强化
                    Task.Run(async () =>
                    {
                        await Task.Delay(1000);
                        this.Invoke(() =>
                        {
                            // 再次确认透明设置
                            this.BackColor = chromaColor;
                            this.TransparencyKey = chromaColor;
                            this.Refresh();
                            Logger.LogInfo("透明设置二次确认完成");
                        });
                    });
                }
                else
                {
                    // 移除分层窗口样式
                    int exStyle = GetWindowLong(this.Handle, GWL_EXSTYLE);
                    SetWindowLong(this.Handle, GWL_EXSTYLE, exStyle & ~WS_EX_LAYERED);
                    
                    Logger.LogInfo("Windows API透明设置已移除");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"设置窗口透明度失败: {ex.Message}", ex);
            }
        }        private void InitializeNativeLayeredWindowMode()
        {
            Logger.LogInfo("开始初始化原生透明窗口模式");
            
            // 在原生透明窗口模式下，MainForm 隐藏，原生透明窗口作为主显示
            this.WindowState = FormWindowState.Minimized;
            this.ShowInTaskbar = false;
            this.Visible = false; // 隐藏控制台窗口
              // 创建并显示原生透明窗口作为主要显示窗口
            try
            {
                // 暂时注释掉原生窗口创建，先确保程序能正常编译运行
                Logger.LogInfo("原生透明窗口创建暂时被注释，将回退到 WebView2 模式");
                
                // 回退到 WebView2 模式
                appSettings.UseNativeLayeredWindow = false;
                InitializeWebViewMode();
                return;
                
                /*
                var nativeWindow = new NativeLayeredWindow(appSettings.StreamAddress);
                nativeWindow.Size = new Size(appSettings.WindowWidth, appSettings.WindowHeight);
                if (appSettings.WindowX != -1 && appSettings.WindowY != -1)
                {
                    nativeWindow.Location = new Point(appSettings.WindowX, appSettings.WindowY);
                }
                else
                {
                    nativeWindow.StartPosition = FormStartPosition.CenterScreen;
                }
                nativeWindow.TopMost = appSettings.TopMostEnabled;
                nativeWindow.SetChromaKeyColor(appSettings.ChromaKeyColor);
                nativeWindow.EnableChromaKey(appSettings.EnableChromaKey);
                nativeWindow.Show();
                
                // 异步加载推流地址
                Task.Run(async () =>
                {
                    await Task.Delay(1000); // 等待窗口完全初始化
                    await nativeWindow.LoadStreamAsync(appSettings.StreamAddress);
                });
                  // 存储原生窗口引用，以便后续控制
                this.nativeWindow = nativeWindow;
                
                Logger.LogInfo("原生透明窗口创建并显示成功，作为主显示窗口");
                */
            }
            catch (Exception ex)
            {
                Logger.LogError($"创建原生透明窗口失败: {ex.Message}", ex);
                MessageBox.Show($"原生透明窗口创建失败：{ex.Message}\n\n将回退到 WebView2 模式", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                
                // 回退到 WebView2 模式
                appSettings.UseNativeLayeredWindow = false;
                InitializeWebViewMode();
                return;
            }
            
            Logger.LogInfo("原生透明窗口模式初始化完成");
        }private void InitializeWebViewMode()
        {
            Logger.LogInfo("开始初始化 WebView2 显示模式");
            
            // 在 WebView2 模式下，MainForm 显示 DigitalHumanDisplay 控件
            this.WindowState = FormWindowState.Normal;
            this.ShowInTaskbar = true;
            this.Visible = true;
            this.Text = "bestHuman 数字人助手 (WebView2模式)";
            this.FormBorderStyle = FormBorderStyle.None; // 无边框
            
            try
            {
                digitalHumanDisplay = new DigitalHumanDisplay();
                digitalHumanDisplay.Dock = DockStyle.Fill;
                Controls.Add(digitalHumanDisplay);
                
                // 应用配置到显示控件
                if (digitalHumanDisplay != null)
                {
                    digitalHumanDisplay.EnableChromaKey = appSettings.EnableChromaKey;
                    digitalHumanDisplay.ChromaKeyColor = appSettings.ChromaKeyColor;
                    digitalHumanDisplay.Tolerance = appSettings.ChromaKeyTolerance;
                    digitalHumanDisplay.SetChromaKeyEnabled(appSettings.EnableChromaKey);
                    
                    Logger.LogInfo($"准备加载推流地址: {appSettings.StreamAddress}");
                    
                    // 异步加载推流地址
                    Task.Run(async () =>
                    {
                        try
                        {
                            await digitalHumanDisplay.LoadStreamAsync(appSettings.StreamAddress);
                            Logger.LogInfo("推流地址加载完成");
                        }
                        catch (Exception ex)
                        {
                            Logger.LogError($"加载推流地址失败: {ex.Message}", ex);
                        }
                    });
                }
                
                Logger.LogInfo("WebView2 显示模式初始化完成");
            }
            catch (Exception ex)
            {
                Logger.LogError($"WebView2 模式初始化失败: {ex.Message}", ex);
                MessageBox.Show($"WebView2 初始化失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
