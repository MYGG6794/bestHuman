using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Runtime.InteropServices; // For P/Invoke
using CoreApplication; // For Logger

namespace CoreApplication
{
    static class Program
    {
        // 提供全局访问点
        public static WebSocketClient WebSocketClient { get; private set; } = null!;
        // 存储原生窗口实例，如果需要从其他地方访问
        private static NativeLayeredWindow? nativeWindowInstance = null;

        [STAThread]
        static void Main(string[] args)
        {
            Logger.Initialize("app.log"); // 初始化日志系统
            Logger.LogInfo("[Main] bestHuman 主程序入口已调用");
            Logger.LogInfo($"[Main] 当前工作目录: {Environment.CurrentDirectory}");
            Logger.LogInfo($"[Main] 启动参数: {string.Join(", ", args)}");
            Logger.LogInfo($"[Main] 启动时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");

            try
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);

                // 加载应用程序设置
                var appSettings = SettingsForm.AppSettings.Load();
                if (appSettings == null)
                {
                    Logger.LogWarning("无法加载应用设置，将使用默认设置。");
                    appSettings = new SettingsForm.AppSettings();
                }
                // 注释掉强制原生窗口模式，让用户自由选择
                // appSettings.UseNativeLayeredWindow = true;

                // 创建 WebSocketClient 和 AIService 实例
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
                
                // 使用 using 语句确保 AIService 被正确释放
                using var aiService = new AIService(WebSocketClient, aiServiceConfig);

                Form mainFormToRun;

                if (appSettings.UseNativeLayeredWindow)
                {
                    Logger.LogInfo("使用原生透明窗口模式启动数字人显示。");
                    nativeWindowInstance = new NativeLayeredWindow(appSettings.StreamAddress);
                    // 应用抠像设置
                    nativeWindowInstance.EnableChromaKey(appSettings.EnableChromaKey);
                    nativeWindowInstance.SetChromaKeyColor(appSettings.ChromaKeyColor);
                    // 原生窗口的流加载和抠像处理逻辑在 NativeLayeredWindow 类内部实现
                    mainFormToRun = nativeWindowInstance;
                }
                else
                {
                    Logger.LogInfo("使用WebView2窗口模式启动数字人显示。");
                    mainFormToRun = new MainForm(aiService, appSettings);
                }

                Application.Run(mainFormToRun);
            }
            catch (Exception ex)
            {
                Logger.LogError("应用程序发生未处理的异常！", ex);
                MessageBox.Show($"应用程序发生错误：{ex.Message}{Environment.NewLine}请查看 app.log 获取详细信息。", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                // 确保全局资源被清理
                try
                {
                    if (WebSocketClient != null)
                    {
                        WebSocketClient.Dispose();
                    }
                    
                    if (nativeWindowInstance != null && !nativeWindowInstance.IsDisposed)
                    {
                        nativeWindowInstance.Dispose();
                    }
                    
                    Logger.LogInfo("全局资源清理完成");
                }
                catch (Exception ex)
                {
                    Logger.LogError($"清理全局资源时发生错误: {ex.Message}", ex);
                }
                
                Logger.LogInfo("应用程序已关闭。");
            }
        }

        // 提供一个公共方法获取原生窗口实例（如果需要）
        public static NativeLayeredWindow? GetNativeWindowInstance()
        {
            return nativeWindowInstance;
        }
    }

    public class MainForm : Form, IDisposable
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
        public const uint VK_C = 0x43; // C键用于抠像控制
        public const uint VK_T = 0x54; // T键用于切换点击穿透

        // 热键ID
        private const int HOTKEY_ID_SETTINGS_F10 = 1;
        private const int HOTKEY_ID_SETTINGS_S = 2;
        private const int HOTKEY_ID_CHROMA_C = 3;
        private const int HOTKEY_ID_TOGGLE_CLICKTHROUGH = 4;

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
        private ChromaKeyControlForm? chromaKeyControlForm; // 抠像控制窗口
        //private NativeLayeredWindow? nativeWindow; // 原生透明窗口
        
        // 系统托盘支持
        private NotifyIcon? notifyIcon;
        private ContextMenuStrip? trayMenu;
        
        // 状态显示
        private StatusStrip? statusStrip;
        private ToolStripStatusLabel? statusLabel;
        private ToolStripStatusLabel? chromaKeyStatusLabel;
        private ToolStripStatusLabel? webSocketStatusLabel;

        // 需要清理的资源
        private System.Windows.Forms.Timer? inputCheckTimer; // 定时器需要清理

        // 在MainForm类中添加这个变量来跟踪Alt键状态
        private bool isAltKeyPressed = false;
        private bool isExiting = false; // 标识是否正在退出程序

        public MainForm(AIService aiService, SettingsForm.AppSettings appSettings)
        {
            Logger.LogInfo("MainForm 构造函数开始。");
            
            // 使用传入的配置和服务
            this.appSettings = appSettings;
            this.aiService = aiService;
            Text = "bestHuman 数字人助手";
            
            // 应用保存的窗口大小，确保最小尺寸
            int windowWidth = Math.Max(appSettings.WindowWidth, 400);
            int windowHeight = Math.Max(appSettings.WindowHeight, 300);
            Size = new System.Drawing.Size(windowWidth, windowHeight);
            Logger.LogInfo($"设置窗口大小: {windowWidth}x{windowHeight}");
            
            // 应用保存的窗口位置，注意 -1 表示未设置
            if (appSettings.WindowX != -1 && appSettings.WindowY != -1)
            {
                StartPosition = FormStartPosition.Manual;
                Location = new Point(appSettings.WindowX, appSettings.WindowY);
                Logger.LogInfo($"恢复窗口位置: {appSettings.WindowX}, {appSettings.WindowY}");
            }
            else
            {
                StartPosition = FormStartPosition.CenterScreen;
                Logger.LogInfo("使用默认居中位置");
            }
            
            FormBorderStyle = FormBorderStyle.None; // 无边框窗口

            // 根据抠像设置决定背景色和透明Key
            if (appSettings.EnableChromaKey)
            {
                this.BackColor = appSettings.ChromaKeyColor;  // 抠像模式使用选择的抠像颜色背景
                this.TransparencyKey = appSettings.ChromaKeyColor; // 抠像颜色透明
                Logger.LogInfo($"主窗口设置为抠像模式 - 抠像颜色背景透明: R={appSettings.ChromaKeyColor.R}, G={appSettings.ChromaKeyColor.G}, B={appSettings.ChromaKeyColor.B}");
            }
            else
            {
                // 非抠像模式使用黑色背景
                this.BackColor = Color.Black;  
                this.TransparencyKey = Color.Empty; // 不透明
                Logger.LogInfo("主窗口设置为非抠像模式 - 黑色背景");
            }
            
            // 确保窗口在最前面
            this.TopMost = true;
            this.WindowState = FormWindowState.Normal;
            
            // 确保窗口样式支持透明
            this.SetStyle(ControlStyles.SupportsTransparentBackColor, true);
            this.SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
            this.SetStyle(ControlStyles.AllPaintingInWmPaint, true);
            
            // 初始化时不添加拖动事件，在ApplySettings中根据点击穿透状态决定
            // 这样避免冲突
            
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
            inputCheckTimer = new System.Windows.Forms.Timer();
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
                // 原生LayeredWindow模式已在Program.Main中处理，这里无需再处理
                Logger.LogInfo("原生LayeredWindow模式已在Program.Main中处理，MainForm不再负责。");
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
            settingsForm.OnOpenChromaKeyControl += () => ToggleChromaKeyControlWindow();

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
        }

        // 重写OnLoad方法，在窗体加载时初始化托盘和状态栏
        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            
            // 初始化系统托盘
            InitializeSystemTray();
            
            // 在像素流模式下不显示状态栏，避免影响拖动和视觉效果
            if (!appSettings.UseNativeLayeredWindow)
            {
                Logger.LogInfo("像素流模式下跳过状态栏初始化，避免底部拖动区域被遮挡");
            }
            else
            {
                // 初始化状态栏（仅在原生模式下）
                InitializeStatusBar();
                Logger.LogInfo("原生模式下已初始化状态栏");
            }
            
            // 应用所有设置，包括拖动事件注册
            ApplySettings(appSettings);
            
            // 设置正确的透明色
            if (appSettings.EnableChromaKey)
            {
                this.BackColor = appSettings.ChromaKeyColor;
                this.TransparencyKey = appSettings.ChromaKeyColor;
                Logger.LogInfo($"窗口加载完成，抠像已启用 - 设置抠像颜色透明: {appSettings.ChromaKeyColor}");
            }
            else
            {
                this.BackColor = Color.Black;
                this.TransparencyKey = Color.Empty;
                Logger.LogInfo("窗口加载完成，抠像未启用 - 保持不透明");
            }
            
            // 移除调试消息框，避免干扰用户体验
            // MessageBox.Show($"bestHuman程序启动成功！\n抠像状态: {(appSettings.EnableChromaKey ? "已启用" : "未启用")}", "启动确认", MessageBoxButtons.OK, MessageBoxIcon.Information);
            
            if (statusLabel != null)
            {
                UpdateMainStatus("程序启动完成", Color.LightGreen);
            }
            
            // 输出调试信息
            Logger.LogInfo($"窗口加载完成，点击穿透状态: {appSettings.ClickThroughEnabled}");
            Logger.LogInfo($"拖动事件注册状态: 主窗口事件数={GetEventHandlerCount()}, digitalHumanDisplay={digitalHumanDisplay != null}");
        }
        
        // 调试用方法：获取事件处理器数量
        private int GetEventHandlerCount()
        {
            try
            {
                var mouseDownEvent = typeof(Control).GetField("EVENT_MOUSEDOWN", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
                if (mouseDownEvent != null)
                {
                    var events = typeof(Control).GetProperty("Events", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)?.GetValue(this);
                    if (events != null)
                    {
                        var eventKey = mouseDownEvent.GetValue(null);
                        if (eventKey != null)
                        {
                            var handler = events.GetType().GetMethod("get_Item")?.Invoke(events, new object[] { eventKey });
                            return handler != null ? 1 : 0;
                        }
                    }
                }
            }
            catch { }
            return -1; // 无法获取
        }

        // 窗口显示后再次确保透明
        protected override void SetVisibleCore(bool value)
        {
            try
            {
                base.SetVisibleCore(value);
                
                if (value && appSettings.EnableChromaKey)
                {
                    // 如果抠像启用，延迟设置透明
                    this.BeginInvoke(new Action(() =>
                    {
                        try
                        {
                            this.BackColor = appSettings.ChromaKeyColor;
                            this.TransparencyKey = appSettings.ChromaKeyColor;
                            this.Refresh();
                            Logger.LogInfo($"窗口显示后设置透明色: {appSettings.ChromaKeyColor}");
                        }
                        catch (Exception ex)
                        {
                            Logger.LogWarning($"设置窗口透明色失败: {ex.Message}");
                        }
                    }));
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"SetVisibleCore失败: {ex.Message}", ex);
                // 继续执行，不要阻止窗口显示
            }
        }

        private void WebSocketClient_OnConnected(object? sender, EventArgs e)
        {
            Logger.LogInfo("WebSocket 连接成功事件。");
            UpdateWebSocketStatus(true); // 更新状态栏
            UpdateMainStatus("WebSocket已连接", Color.LightGreen);
            speechService?.StartSpeechRecognition(); // 连接成功后启动语音识别
        }

        // 在窗体关闭时断开 WebSocket 连接
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            // 如果用户点击关闭按钮且不是正在退出，隐藏到托盘而不是退出
            if (e.CloseReason == CloseReason.UserClosing && !isExiting)
            {
                e.Cancel = true;
                this.Hide();
                if (notifyIcon != null)
                {
                    notifyIcon.ShowBalloonTip(2000, "bestHuman", "程序已最小化到系统托盘", ToolTipIcon.Info);
                }
                return;
            }
            
            base.OnFormClosing(e);
            
            // 注销全局热键
            UnregisterGlobalHotkeys();
            
            // 清理定时器
            if (inputCheckTimer != null)
            {
                inputCheckTimer.Stop();
                inputCheckTimer.Dispose();
                inputCheckTimer = null;
            }
            
            // 清理系统托盘
            if (notifyIcon != null)
            {
                notifyIcon.Visible = false;
                notifyIcon.Dispose();
                notifyIcon = null;
            }
            if (trayMenu != null)
            {
                trayMenu.Dispose();
                trayMenu = null;
            }
            
            // 清理状态栏控件
            if (statusStrip != null)
            {
                statusStrip.Dispose();
                statusStrip = null;
            }
            
            // 清理 WebSocketClient
            if (webSocketClient != null)
            {
                _ = webSocketClient.DisconnectAsync();
                webSocketClient.Dispose();
            }
            
            // 清理 SpeechService
            if (speechService != null)
            {
                speechService.Dispose();
                speechService = null;
            }
            
            // 清理 AIService
            if (aiService != null)
            {
                aiService.Dispose();
                aiService = null;
            }
            
            // 清理窗体实例
            if (aiManagerForm != null)
            {
                if (!aiManagerForm.IsDisposed)
                {
                    aiManagerForm.Dispose();
                }
                aiManagerForm = null;
            }
            
            if (chromaKeyControlForm != null)
            {
                if (!chromaKeyControlForm.IsDisposed)
                {
                    chromaKeyControlForm.Dispose();
                }
                chromaKeyControlForm = null;
            }
            
            if (settingsForm != null)
            {
                if (!settingsForm.IsDisposed)
                {
                    settingsForm.Dispose();
                }
                // settingsForm 不能设为 null，因为它被声明为非空类型
            }
            
            // 清理 DigitalHumanDisplay
            if (digitalHumanDisplay != null)
            {
                if (!digitalHumanDisplay.IsDisposed)
                {
                    digitalHumanDisplay.Dispose();
                }
                digitalHumanDisplay = null;
            }
        }

        private void WebSocketClient_OnDisconnected(object? sender, EventArgs e)
        {
            Logger.LogInfo("WebSocket 连接断开事件。");
            UpdateWebSocketStatus(false); // 更新状态栏
            UpdateMainStatus("WebSocket已断开", Color.Orange);
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
            UpdateWebSocketStatus(false); // 更新状态栏
            UpdateMainStatus($"WebSocket错误: {errorMessage}", Color.Red);
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
            
            // 只更新设置界面管理的参数，保留抠像控制面板管理的高级参数
            var newSettings = e.Settings;
            appSettings.StreamAddress = newSettings.StreamAddress;
            appSettings.WebSocketServerAddress = newSettings.WebSocketServerAddress;
            appSettings.EnableChromaKey = newSettings.EnableChromaKey;
            appSettings.ChromaKeyColor = newSettings.ChromaKeyColor;
            // 不更新 ChromaKeyTolerance, ChromaKeyGreenThreshold, ChromaKeyMinBrightness, ChromaKeyMaxBrightness
            // 这些参数由抠像控制面板专门管理
            
            appSettings.TopMostEnabled = newSettings.TopMostEnabled;
            appSettings.ClickThroughEnabled = newSettings.ClickThroughEnabled;
            appSettings.WindowWidth = newSettings.WindowWidth;
            appSettings.WindowHeight = newSettings.WindowHeight;
            appSettings.UseNativeLayeredWindow = newSettings.UseNativeLayeredWindow;
            appSettings.TtsVoiceName = newSettings.TtsVoiceName;
            appSettings.TtsRate = newSettings.TtsRate;
            appSettings.TtsVolume = newSettings.TtsVolume;
            appSettings.ModelPath = newSettings.ModelPath;
            appSettings.KnowledgeBasePath = newSettings.KnowledgeBasePath;
            appSettings.UseGPU = newSettings.UseGPU;
            appSettings.EnableCloudFallback = newSettings.EnableCloudFallback;
            appSettings.CloudAPIKey = newSettings.CloudAPIKey;
            appSettings.CloudAPIEndpoint = newSettings.CloudAPIEndpoint;
            
            ApplySettings(appSettings);
            appSettings.Save(); // 使用AppSettings.Save()方法保存到文件

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
        }

        // 处理抠像控制窗口参数变化事件
        private void ChromaKeyControlForm_ChromaKeyChanged(object? sender, ChromaKeyChangedEventArgs e)
        {
            Logger.LogInfo("ChromaKeyControlForm_ChromaKeyChanged 事件触发。");
            
            try
            {
                // 更新设置中的抠像参数
                appSettings.EnableChromaKey = e.EnableChromaKey;
                appSettings.ChromaKeyColor = e.ChromaKeyColor;
                appSettings.ChromaKeyTolerance = e.ColorTolerance;
                appSettings.ChromaKeyGreenThreshold = e.GreenThreshold;
                appSettings.ChromaKeyMinBrightness = e.MinBrightness;
                appSettings.ChromaKeyMaxBrightness = e.MaxBrightness;
                
                // 立即应用新的抠像设置到数字人显示
                if (digitalHumanDisplay != null)
                {
                    digitalHumanDisplay.UpdateChromaKeyScript(
                        e.ChromaKeyColor,
                        e.GreenThreshold,
                        e.ColorTolerance,
                        e.MinBrightness,
                        e.MaxBrightness,
                        e.EnableChromaKey
                    );
                    Logger.LogInfo($"实时更新抠像参数: 颜色={e.ChromaKeyColor}, 绿色阈值={e.GreenThreshold}, 容差={e.ColorTolerance}, 亮度={e.MinBrightness}-{e.MaxBrightness}");
                }
                
                // 更新主窗口的透明色设置
                if (e.EnableChromaKey)
                {
                    this.BackColor = e.ChromaKeyColor;
                    this.TransparencyKey = e.ChromaKeyColor;
                    ForceWindowTransparency(true);
                    
                    // 立即刷新窗口透明效果
                    this.Invalidate();
                    this.Update();
                    this.Refresh();
                }
                else
                {
                    this.BackColor = Color.Black;
                    this.TransparencyKey = Color.Empty;
                    ForceWindowTransparency(false);
                }
                
                // 保存更新后的设置
                appSettings.Save(); // 使用AppSettings.Save()方法保存到文件
                
                // 更新状态栏显示
                UpdateChromaKeyStatus();
                UpdateMainStatus("抠像参数已更新", Color.LightGreen);
                
                Logger.LogInfo("抠像参数已实时更新并保存。");
            }
            catch (Exception ex)
            {
                Logger.LogError($"处理抠像参数变化时出错: {ex.Message}", ex);
            }
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
            
            // Alt + T 切换点击穿透
            if (e.KeyCode == Keys.T && isAltKeyPressed)
            {
                Logger.LogInfo("Alt + T 快捷键被按下 - 切换点击穿透状态。");
                ToggleClickThrough();
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
                        settingsForm.OnOpenChromaKeyControl += () => ToggleChromaKeyControlWindow();
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

        // 提取抠像控制窗口切换逻辑到单独方法
        private void ToggleChromaKeyControlWindow()
        {
            try
            {
                Logger.LogInfo($"ToggleChromaKeyControlWindow调用：当前窗口状态 - 存在:{chromaKeyControlForm != null}，可见:{chromaKeyControlForm?.Visible}，已释放:{chromaKeyControlForm?.IsDisposed}");
                
                // 如果抠像控制窗口未创建或已释放，则重新创建
                if (chromaKeyControlForm == null || chromaKeyControlForm.IsDisposed)
                {
                    Logger.LogInfo("创建新的抠像控制窗口实例。");
                    chromaKeyControlForm = new ChromaKeyControlForm(digitalHumanDisplay);
                    if (chromaKeyControlForm != null)
                    {
                        // 订阅抠像参数变化事件
                        chromaKeyControlForm.ChromaKeyChanged += ChromaKeyControlForm_ChromaKeyChanged;
                    }
                }

                // 切换抠像控制窗口的可见状态
                if (chromaKeyControlForm != null && chromaKeyControlForm.Visible)
                {
                    Logger.LogInfo("隐藏抠像控制窗口。");
                    chromaKeyControlForm.Hide();
                    // 确保主窗体重新获得焦点
                    this.Activate();
                    this.Focus();
                    // 重置键盘事件处理
                    ResetKeyboardHandling();
                }
                else if (chromaKeyControlForm != null)
                {
                    Logger.LogInfo("显示抠像控制窗口并激活。");
                    chromaKeyControlForm.Show();
                    chromaKeyControlForm.BringToFront(); // 确保窗口在最前
                    chromaKeyControlForm.Activate();     // 激活窗口以获取焦点

                    // 确保窗口在屏幕可见区域内
                    Rectangle screenBounds = Screen.FromControl(this).WorkingArea;
                    if (!screenBounds.IntersectsWith(chromaKeyControlForm.Bounds))
                    {
                        Logger.LogInfo("重置抠像控制窗口位置到屏幕中心。");
                        chromaKeyControlForm.Location = new Point(
                            screenBounds.X + (screenBounds.Width - chromaKeyControlForm.Width) / 2,
                            screenBounds.Y + (screenBounds.Height - chromaKeyControlForm.Height) / 2
                        );
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"处理抠像控制窗口快捷键时出错: {ex.Message}", ex);
                MessageBox.Show(
                    "打开抠像控制窗口时发生错误，请重试。\n\n" + ex.Message,
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

                // 启用点击穿透时，禁用拖动功能
                RemoveDragEventHandlers();
                
                // 即使启用点击穿透，也要确保能接收键盘事件
                this.KeyPreview = true;
                this.ShowInTaskbar = true;
                
                // 重新注册全局热键以确保在点击穿透模式下仍能工作
                Logger.LogInfo("点击穿透模式下重新注册全局热键");
                UnregisterGlobalHotkeys();
                RegisterGlobalHotkeys();
                
                UpdateMainStatus("点击穿透已启用 - 无法拖动窗口", Color.Yellow);
            }
            else
            {
                // 移除 WS_EX_LAYERED 和 WS_EX_TRANSPARENT 样式
                exStyle &= ~(WS_EX_LAYERED | WS_EX_TRANSPARENT);
                SetWindowLong(this.Handle, GWL_EXSTYLE, exStyle);
                
                // 禁用点击穿透时，启用拖动功能
                AddDragEventHandlers();
                
                // 重新注册全局热键
                Logger.LogInfo("非点击穿透模式下重新注册全局热键");
                UnregisterGlobalHotkeys();
                RegisterGlobalHotkeys();
                
                UpdateMainStatus("点击穿透已禁用 - 可以拖动窗口", Color.LightGreen);
            }

            // 强制窗口重绘以应用更改
            this.Invalidate();
        }
        
        // 移除拖动事件处理器
        private void RemoveDragEventHandlers()
        {
            Logger.LogInfo($"RemoveDragEventHandlers 开始: digitalHumanDisplay={digitalHumanDisplay != null}, isDisposed={digitalHumanDisplay?.IsDisposed}");
            
            this.MouseDown -= MainForm_MouseDown;
            this.MouseMove -= MainForm_MouseMove;
            this.MouseUp -= MainForm_MouseUp;
            Logger.LogInfo("主窗口拖动事件已移除");
            
            if (digitalHumanDisplay != null && !digitalHumanDisplay.IsDisposed)
            {
                digitalHumanDisplay.MouseDown -= DigitalHumanDisplay_MouseDown;
                digitalHumanDisplay.MouseMove -= DigitalHumanDisplay_MouseMove;
                digitalHumanDisplay.MouseUp -= DigitalHumanDisplay_MouseUp;
                Logger.LogInfo("digitalHumanDisplay 拖动事件已移除");
            }
            else
            {
                Logger.LogInfo($"digitalHumanDisplay 状态异常，跳过事件移除: null={digitalHumanDisplay == null}, disposed={digitalHumanDisplay?.IsDisposed}");
            }
            
            Logger.LogInfo("RemoveDragEventHandlers 完成");
        }
        
        // 添加拖动事件处理器
        private void AddDragEventHandlers()
        {
            Logger.LogInfo($"AddDragEventHandlers 开始: digitalHumanDisplay={digitalHumanDisplay != null}, isDisposed={digitalHumanDisplay?.IsDisposed}");
            
            // 先移除避免重复注册
            RemoveDragEventHandlers();
            
            // 添加主窗口事件
            this.MouseDown += MainForm_MouseDown;
            this.MouseMove += MainForm_MouseMove;
            this.MouseUp += MainForm_MouseUp;
            Logger.LogInfo("主窗口拖动事件已添加");
            
            // 添加子控件事件（用于事件冒泡）
            if (digitalHumanDisplay != null && !digitalHumanDisplay.IsDisposed)
            {
                digitalHumanDisplay.MouseDown += DigitalHumanDisplay_MouseDown;
                digitalHumanDisplay.MouseMove += DigitalHumanDisplay_MouseMove;
                digitalHumanDisplay.MouseUp += DigitalHumanDisplay_MouseUp;
                Logger.LogInfo("digitalHumanDisplay 拖动事件已添加");
            }
            else
            {
                Logger.LogWarning($"digitalHumanDisplay 状态异常，无法添加拖动事件: null={digitalHumanDisplay == null}, disposed={digitalHumanDisplay?.IsDisposed}");
            }
            
            Logger.LogInfo("AddDragEventHandlers 完成");
        }
        
        // 子控件鼠标事件处理器 - 将事件冒泡到父窗口
        private void DigitalHumanDisplay_MouseDown(object? sender, MouseEventArgs e)
        {
            Logger.LogInfo($"digitalHumanDisplay MouseDown 冒泡: {e.Location}");
            MainForm_MouseDown(this, e);
        }
        
        private void DigitalHumanDisplay_MouseMove(object? sender, MouseEventArgs e)
        {
            // 只在拖动或调整大小时才转发 MouseMove 事件以避免性能问题
            if (isDragging || isResizing)
            {
                MainForm_MouseMove(this, e);
            }
            else
            {
                // 即使不在拖动，也要更新鼠标光标
                UpdateCursor(e.Location);
            }
        }
        
        private void DigitalHumanDisplay_MouseUp(object? sender, MouseEventArgs e)
        {
            if (isDragging || isResizing)
            {
                Logger.LogInfo($"digitalHumanDisplay MouseUp 冒泡: {e.Location}");
                MainForm_MouseUp(this, e);
            }
        }

        // 切换点击穿透状态
        private void ToggleClickThrough()
        {
            try
            {
                appSettings.ClickThroughEnabled = !appSettings.ClickThroughEnabled;
                EnableClickThrough(appSettings.ClickThroughEnabled);
                appSettings.Save();
                
                string status = appSettings.ClickThroughEnabled ? "已启用" : "已禁用";
                Logger.LogInfo($"点击穿透状态已切换: {status}");
                
                // 通过系统托盘显示通知
                if (notifyIcon != null)
                {
                    string message = appSettings.ClickThroughEnabled ? 
                        "点击穿透已启用 - 无法拖动窗口\n使用 Alt+T 可重新启用拖动" : 
                        "点击穿透已禁用 - 可以拖动和调整窗口大小";
                    notifyIcon.ShowBalloonTip(3000, "bestHuman", message, ToolTipIcon.Info);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"切换点击穿透状态时出错: {ex.Message}", ex);
            }
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
                
                // 注册 Alt+C 用于抠像控制
                bool result3 = RegisterHotKey(this.Handle, HOTKEY_ID_CHROMA_C, MOD_ALT, VK_C);
                Logger.LogInfo($"注册全局热键 Alt+C (抠像控制): {(result3 ? "成功" : "失败")}");
                
                // 注册 Alt+T 用于切换点击穿透
                bool result4 = RegisterHotKey(this.Handle, HOTKEY_ID_TOGGLE_CLICKTHROUGH, MOD_ALT, VK_T);
                Logger.LogInfo($"注册全局热键 Alt+T (切换点击穿透): {(result4 ? "成功" : "失败")}");
                
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
                UnregisterHotKey(this.Handle, HOTKEY_ID_CHROMA_C); // 添加缺失的抠像控制热键注销
                UnregisterHotKey(this.Handle, HOTKEY_ID_TOGGLE_CLICKTHROUGH); // 点击穿透切换热键注销
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
                    
                    case HOTKEY_ID_CHROMA_C:
                        Logger.LogInfo("通过全局热键 Alt+C 触发抠像控制窗口切换");
                        ToggleChromaKeyControlWindow();
                        return; // 不调用base.WndProc，表示我们已经处理了这个消息
                    
                    case HOTKEY_ID_TOGGLE_CLICKTHROUGH:
                        Logger.LogInfo("通过全局热键 Alt+T 触发点击穿透切换");
                        ToggleClickThrough();
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
        }        private async void InitializeWebViewMode()
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
                // 恢复 digitalHumanDisplay 填满窗口的布局，拖动功能通过事件冒泡实现
                digitalHumanDisplay.Dock = DockStyle.Fill;
                
                Controls.Add(digitalHumanDisplay);
                
                // 事件处理器的添加统一由 EnableClickThrough 方法管理
                // 这里不再有条件地添加，避免时序问题
                
                // 应用配置到显示控件
                if (digitalHumanDisplay != null)
                {
                    digitalHumanDisplay.EnableChromaKey = appSettings.EnableChromaKey;
                    digitalHumanDisplay.ChromaKeyColor = appSettings.ChromaKeyColor;
                    digitalHumanDisplay.Tolerance = appSettings.ChromaKeyTolerance;
                    digitalHumanDisplay.SetChromaKeyEnabled(appSettings.EnableChromaKey);
                      Logger.LogInfo($"准备加载推流地址: {appSettings.StreamAddress}");
                    
                    // 等待一段时间确保WebView2完全初始化
                    await Task.Delay(3000);
                    Logger.LogInfo("开始应用显示模式和加载推流");
                    
                    // 应用显示模式
                    await digitalHumanDisplay.ApplyDisplayMode(false, appSettings);
                    Logger.LogInfo("显示模式应用完成");
                    
                    // 再等待一下确保ApplyDisplayMode完成
                    await Task.Delay(2000);
                    
                    // 加载推流地址
                    await digitalHumanDisplay.LoadStreamAsync(appSettings.StreamAddress);
                    Logger.LogInfo("推流地址加载完成");
                }
                
                Logger.LogInfo("WebView2 显示模式初始化完成");
            }
            catch (Exception ex)
            {
                Logger.LogError($"WebView2 模式初始化失败: {ex.Message}", ex);
                MessageBox.Show($"WebView2 初始化失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        #region 窗口拖动和调整大小支持
        private bool isDragging = false;
        private bool isResizing = false;
        private Point dragStartPoint;
        private Size originalSize;
        private ResizeDirection resizeDirection = ResizeDirection.None;
        
        // 调整大小的方向
        private enum ResizeDirection
        {
            None,
            N, S, E, W,
            NE, NW, SE, SW
        }
        
        // 调整大小的边界检测阈值
        private const int ResizeBorderThickness = 8;
        
        // 窗口吸附阈值
        private const int SnapThreshold = 20;

        private void MainForm_MouseDown(object? sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                // 检测是否在调整大小的边界区域
                resizeDirection = GetResizeDirection(e.Location);
                
                if (resizeDirection != ResizeDirection.None)
                {
                    // 开始调整大小
                    isResizing = true;
                    originalSize = this.Size;
                    dragStartPoint = e.Location;
                    Logger.LogInfo($"开始调整窗口大小，方向: {resizeDirection}，起始位置: {e.Location}");
                }
                else
                {
                    // 开始拖动
                    isDragging = true;
                    dragStartPoint = e.Location;
                    Logger.LogInfo($"开始拖动窗口，起始位置: {e.Location}");
                }
            }
        }

        private void MainForm_MouseMove(object? sender, MouseEventArgs e)
        {
            if (isResizing && e.Button == MouseButtons.Left)
            {
                // 调整窗口大小
                ResizeWindow(e.Location);
            }
            else if (isDragging && e.Button == MouseButtons.Left)
            {
                // 拖动窗口
                Point currentLocation = this.Location;
                currentLocation.X += e.X - dragStartPoint.X;
                currentLocation.Y += e.Y - dragStartPoint.Y;
                
                // 应用窗口吸附功能
                currentLocation = ApplyWindowSnapping(currentLocation);
                
                this.Location = currentLocation;
            }
            else
            {
                // 更新鼠标光标
                UpdateCursor(e.Location);
            }
        }

        private void MainForm_MouseUp(object? sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                if (isResizing)
                {
                    isResizing = false;
                    Logger.LogInfo($"停止调整窗口大小，当前尺寸: {this.Size}");
                }
                
                if (isDragging)
                {
                    isDragging = false;
                    Logger.LogInfo($"停止拖动窗口，当前位置: {this.Location}");
                }
                
                // 保存窗口位置和大小到设置
                if (appSettings != null)
                {
                    appSettings.WindowX = this.Location.X;
                    appSettings.WindowY = this.Location.Y;
                    appSettings.WindowWidth = this.Width;
                    appSettings.WindowHeight = this.Height;
                    appSettings.Save();
                    Logger.LogInfo($"保存窗口状态: 位置({appSettings.WindowX}, {appSettings.WindowY}), 大小({appSettings.WindowWidth}x{appSettings.WindowHeight})");
                }
                
                resizeDirection = ResizeDirection.None;
                this.Cursor = Cursors.Default;
            }
        }
        
        private ResizeDirection GetResizeDirection(Point location)
        {
            // 检测鼠标位置是否在窗口边界区域
            Rectangle clientRect = this.ClientRectangle;
            
            bool onLeft = location.X <= ResizeBorderThickness;
            bool onRight = location.X >= clientRect.Width - ResizeBorderThickness;
            bool onTop = location.Y <= ResizeBorderThickness;
            bool onBottom = location.Y >= clientRect.Height - ResizeBorderThickness;
            
            if (onTop && onLeft) return ResizeDirection.NW;
            if (onTop && onRight) return ResizeDirection.NE;
            if (onBottom && onLeft) return ResizeDirection.SW;
            if (onBottom && onRight) return ResizeDirection.SE;
            if (onTop) return ResizeDirection.N;
            if (onBottom) return ResizeDirection.S;
            if (onLeft) return ResizeDirection.W;
            if (onRight) return ResizeDirection.E;
            
            return ResizeDirection.None;
        }
        
        private void UpdateCursor(Point location)
        {
            ResizeDirection direction = GetResizeDirection(location);
            
            switch (direction)
            {
                case ResizeDirection.N:
                case ResizeDirection.S:
                    this.Cursor = Cursors.SizeNS;
                    break;
                case ResizeDirection.E:
                case ResizeDirection.W:
                    this.Cursor = Cursors.SizeWE;
                    break;
                case ResizeDirection.NE:
                case ResizeDirection.SW:
                    this.Cursor = Cursors.SizeNESW;
                    break;
                case ResizeDirection.NW:
                case ResizeDirection.SE:
                    this.Cursor = Cursors.SizeNWSE;
                    break;
                default:
                    this.Cursor = Cursors.Default;
                    break;
            }
        }
        
        private void ResizeWindow(Point currentLocation)
        {
            int deltaX = currentLocation.X - dragStartPoint.X;
            int deltaY = currentLocation.Y - dragStartPoint.Y;
            
            Rectangle newBounds = this.Bounds;
            
            switch (resizeDirection)
            {
                case ResizeDirection.N:
                    newBounds.Y += deltaY;
                    newBounds.Height -= deltaY;
                    break;
                case ResizeDirection.S:
                    newBounds.Height = originalSize.Height + deltaY;
                    break;
                case ResizeDirection.E:
                    newBounds.Width = originalSize.Width + deltaX;
                    break;
                case ResizeDirection.W:
                    newBounds.X += deltaX;
                    newBounds.Width -= deltaX;
                    break;
                case ResizeDirection.NE:
                    newBounds.Y += deltaY;
                    newBounds.Height -= deltaY;
                    newBounds.Width = originalSize.Width + deltaX;
                    break;
                case ResizeDirection.NW:
                    newBounds.Y += deltaY;
                    newBounds.Height -= deltaY;
                    newBounds.X += deltaX;
                    newBounds.Width -= deltaX;
                    break;
                case ResizeDirection.SE:
                    newBounds.Height = originalSize.Height + deltaY;
                    newBounds.Width = originalSize.Width + deltaX;
                    break;
                case ResizeDirection.SW:
                    newBounds.Height = originalSize.Height + deltaY;
                    newBounds.X += deltaX;
                    newBounds.Width -= deltaX;
                    break;
            }
            
            // 设置最小窗口大小
            const int minWidth = 200;
            const int minHeight = 150;
            
            if (newBounds.Width >= minWidth && newBounds.Height >= minHeight)
            {
                this.Bounds = newBounds;
            }
        }

        private Point ApplyWindowSnapping(Point newLocation)
        {
            try
            {
                // 获取当前屏幕的工作区域
                Screen currentScreen = Screen.FromPoint(newLocation);
                Rectangle workingArea = currentScreen.WorkingArea;
                
                Point snappedLocation = newLocation;
                bool snapped = false;
                
                // 左边缘吸附
                if (Math.Abs(newLocation.X - workingArea.Left) <= SnapThreshold)
                {
                    snappedLocation.X = workingArea.Left;
                    snapped = true;
                    Logger.LogInfo("窗口吸附到屏幕左边缘");
                }
                // 右边缘吸附
                else if (Math.Abs((newLocation.X + this.Width) - workingArea.Right) <= SnapThreshold)
                {
                    snappedLocation.X = workingArea.Right - this.Width;
                    snapped = true;
                    Logger.LogInfo("窗口吸附到屏幕右边缘");
                }
                
                // 上边缘吸附
                if (Math.Abs(newLocation.Y - workingArea.Top) <= SnapThreshold)
                {
                    snappedLocation.Y = workingArea.Top;
                    snapped = true;
                    Logger.LogInfo("窗口吸附到屏幕上边缘");
                }
                // 下边缘吸附
                else if (Math.Abs((newLocation.Y + this.Height) - workingArea.Bottom) <= SnapThreshold)
                {
                    snappedLocation.Y = workingArea.Bottom - this.Height;
                    snapped = true;
                    Logger.LogInfo("窗口吸附到屏幕下边缘");
                }
                
                // 如果发生了吸附，更新状态栏显示
                if (snapped && statusLabel != null)
                {
                    UpdateMainStatus($"窗口已吸附到屏幕边缘: {snappedLocation}", Color.LightBlue);
                }
                
                return snappedLocation;
            }
            catch (Exception ex)
            {
                Logger.LogError($"应用窗口吸附时出错: {ex.Message}", ex);
                return newLocation;
            }
        }
        #endregion

        #region 状态栏支持
        private void InitializeStatusBar()
        {
            try
            {
                // 创建状态栏
                statusStrip = new StatusStrip();
                statusStrip.BackColor = Color.FromArgb(45, 45, 48); // 深色背景
                statusStrip.ForeColor = Color.White;
                
                // 主状态标签
                statusLabel = new ToolStripStatusLabel();
                statusLabel.Text = "就绪";
                statusLabel.ForeColor = Color.LightGreen;
                statusLabel.Spring = true; // 自动填充剩余空间
                statusLabel.TextAlign = ContentAlignment.MiddleLeft;
                
                // 抠像状态标签
                chromaKeyStatusLabel = new ToolStripStatusLabel();
                UpdateChromaKeyStatus();
                
                // WebSocket状态标签
                webSocketStatusLabel = new ToolStripStatusLabel();
                webSocketStatusLabel.Text = "WebSocket: 断开";
                webSocketStatusLabel.ForeColor = Color.Orange;
                
                // 添加到状态栏
                statusStrip.Items.AddRange(new ToolStripItem[] {
                    statusLabel,
                    new ToolStripSeparator(),
                    chromaKeyStatusLabel,
                    new ToolStripSeparator(),
                    webSocketStatusLabel
                });
                
                // 添加到窗口
                this.Controls.Add(statusStrip);
                
                Logger.LogInfo("状态栏初始化完成");
            }
            catch (Exception ex)
            {
                Logger.LogError($"初始化状态栏失败: {ex.Message}", ex);
            }
        }
        
        private void UpdateChromaKeyStatus()
        {
            if (chromaKeyStatusLabel != null)
            {
                if (appSettings.EnableChromaKey)
                {
                    chromaKeyStatusLabel.Text = "抠像: 启用";
                    chromaKeyStatusLabel.ForeColor = Color.LightGreen;
                }
                else
                {
                    chromaKeyStatusLabel.Text = "抠像: 禁用";
                    chromaKeyStatusLabel.ForeColor = Color.Gray;
                }
            }
        }
        
        private void UpdateWebSocketStatus(bool connected)
        {
            if (webSocketStatusLabel != null)
            {
                if (connected)
                {
                    webSocketStatusLabel.Text = "WebSocket: 已连接";
                    webSocketStatusLabel.ForeColor = Color.LightGreen;
                }
                else
                {
                    webSocketStatusLabel.Text = "WebSocket: 断开";
                    webSocketStatusLabel.ForeColor = Color.Orange;
                }
            }
        }
        
        private void UpdateMainStatus(string message, Color? color = null)
        {
            if (statusLabel != null)
            {
                statusLabel.Text = message;
                if (color.HasValue)
                {
                    statusLabel.ForeColor = color.Value;
                }
            }
        }
        #endregion

        #region 系统托盘支持
        private void InitializeSystemTray()
        {
            try
            {
                // 创建托盘菜单
                trayMenu = new ContextMenuStrip();
                trayMenu.Items.Add("显示主窗口", null, (s, e) => {
                    this.Show();
                    this.WindowState = FormWindowState.Normal;
                    this.Activate();
                });
                trayMenu.Items.Add("-");
                trayMenu.Items.Add("设置", null, (s, e) => ToggleSettingsWindow());
                trayMenu.Items.Add("抠像控制", null, (s, e) => ToggleChromaKeyControlWindow());
                trayMenu.Items.Add("切换点击穿透 (Alt+T)", null, (s, e) => ToggleClickThrough());
                trayMenu.Items.Add("AI管理", null, (s, e) => {
                    if (aiService != null) {
                        if (aiManagerForm == null || aiManagerForm.IsDisposed)
                            aiManagerForm = new AIManagerForm(aiService, appSettings);
                        aiManagerForm.Show();
                        aiManagerForm.Activate();
                    }
                });
                trayMenu.Items.Add("-");
                trayMenu.Items.Add("退出", null, (s, e) => {
                    // 设置退出标志并关闭程序
                    isExiting = true;
                    this.Close();
                    Application.Exit();
                });

                // 创建托盘图标
                notifyIcon = new NotifyIcon();
                notifyIcon.Icon = SystemIcons.Application; // 使用系统默认图标
                notifyIcon.Text = "bestHuman 数字人助手";
                notifyIcon.ContextMenuStrip = trayMenu;
                notifyIcon.Visible = true;
                
                // 双击托盘图标显示主窗口
                notifyIcon.DoubleClick += (s, e) => {
                    if (this.WindowState == FormWindowState.Minimized || !this.Visible)
                    {
                        this.Show();
                        this.WindowState = FormWindowState.Normal;
                        this.Activate();
                    }
                    else
                    {
                        this.Hide();
                    }
                };

                Logger.LogInfo("系统托盘初始化完成");
            }
            catch (Exception ex)
            {
                Logger.LogError($"初始化系统托盘失败: {ex.Message}", ex);
            }
        }
        #endregion
        
        #region IDisposable 实现
        private bool disposed = false;
        
        /// <summary>
        /// 实现 IDisposable 接口，确保所有资源被正确释放
        /// </summary>
        /// <param name="disposing">是否正在释放托管资源</param>
        protected override void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    // 释放托管资源
                    try
                    {
                        // 停止并释放定时器
                        if (inputCheckTimer != null)
                        {
                            inputCheckTimer.Stop();
                            inputCheckTimer.Dispose();
                            inputCheckTimer = null;
                        }
                        
                        // 注销全局热键
                        UnregisterGlobalHotkeys();
                        
                        // 释放系统托盘资源
                        if (notifyIcon != null)
                        {
                            notifyIcon.Visible = false;
                            notifyIcon.Dispose();
                            notifyIcon = null;
                        }
                        
                        if (trayMenu != null)
                        {
                            trayMenu.Dispose();
                            trayMenu = null;
                        }
                        
                        // 释放状态栏资源
                        if (statusStrip != null)
                        {
                            statusStrip.Dispose();
                            statusStrip = null;
                        }
                        
                        // 释放服务资源
                        if (speechService != null)
                        {
                            speechService.Dispose();
                            speechService = null;
                        }
                        
                        if (aiService != null)
                        {
                            aiService.Dispose();
                            aiService = null;
                        }
                        
                        // 释放窗体资源
                        if (aiManagerForm != null && !aiManagerForm.IsDisposed)
                        {
                            aiManagerForm.Dispose();
                            aiManagerForm = null;
                        }
                        
                        if (chromaKeyControlForm != null && !chromaKeyControlForm.IsDisposed)
                        {
                            chromaKeyControlForm.Dispose();
                            chromaKeyControlForm = null;
                        }
                        
                        if (settingsForm != null && !settingsForm.IsDisposed)
                        {
                            settingsForm.Dispose();
                            // settingsForm 不能设为 null，因为它被声明为非空类型
                        }
                        
                        if (digitalHumanDisplay != null && !digitalHumanDisplay.IsDisposed)
                        {
                            digitalHumanDisplay.Dispose();
                            digitalHumanDisplay = null;
                        }
                        
                        Logger.LogInfo("MainForm 资源释放完成");
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError($"释放 MainForm 资源时发生错误: {ex.Message}", ex);
                    }
                }
                
                disposed = true;
            }
            
            // 调用基类的 Dispose 方法
            base.Dispose(disposing);
        }
        
        /// <summary>
        /// 公共 Dispose 方法
        /// </summary>
        public new void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}
