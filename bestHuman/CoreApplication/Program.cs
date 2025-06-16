using System;
using System.Windows.Forms;
using System.Runtime.InteropServices; // For P/Invoke
using CoreApplication; // For Logger

namespace CoreApplication
{
    static class Program
    {
        // 提供全局访问点
        public static WebSocketClient WebSocketClient { get; private set; } = null!;

        [STAThread]
        static void Main()
        {
            Logger.Initialize("app.log"); // 初始化日志系统
            Logger.LogInfo("应用程序启动中...");

            try
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);

                // 创建全局 WebSocketClient 实例
                WebSocketClient = new WebSocketClient();
                
                // 启动主窗口
                Application.Run(new MainForm());
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
        private const int WM_HOTKEY = 0x0312;

        private DigitalHumanDisplay digitalHumanDisplay;
        private SettingsForm settingsForm;
        private SettingsForm.AppSettings appSettings;
        private WebSocketClient webSocketClient;
        private SpeechService? speechService; // 声明为可空
        private AIService? aiService;
        private AIManagerForm? aiManagerForm;

        // 在MainForm类中添加这个变量来跟踪Alt键状态
        private bool isAltKeyPressed = false;

        public MainForm()
        {
            Logger.LogInfo("MainForm 构造函数开始。");            // 加载配置
            appSettings = LoadSettings();
            Text = "bestHuman 数字人助手";
            Size = new System.Drawing.Size(appSettings.WindowWidth, appSettings.WindowHeight);
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.None; // 无边框
            
            // 初始状态下使用默认背景色，根据抠像状态动态设置透明色
            // 默认不启用抠像时，不设置透明色
            this.BackColor = SystemColors.Control;  
            this.TransparencyKey = Color.Empty;
            
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
            inputCheckTimer.Start();

            // 初始化 DigitalHumanDisplay 控件
            digitalHumanDisplay = new DigitalHumanDisplay();
            digitalHumanDisplay.Dock = DockStyle.Fill; // 填充整个窗口
            Controls.Add(digitalHumanDisplay);

            // 根据配置设置窗口置顶和点击穿透
            SetTopMost(appSettings.TopMostEnabled);
            EnableClickThrough(appSettings.ClickThroughEnabled);

            // 初始化 SettingsForm
            settingsForm = new SettingsForm(appSettings, this);
            settingsForm.SettingsChanged += SettingsForm_SettingsChanged;

            // 使用全局 WebSocketClient 实例
            webSocketClient = Program.WebSocketClient;
            webSocketClient.OnConnected += WebSocketClient_OnConnected;
            webSocketClient.OnDisconnected += WebSocketClient_OnDisconnected;
            webSocketClient.OnMessageReceived += WebSocketClient_OnMessageReceived;
            webSocketClient.OnError += WebSocketClient_OnError;

            // 尝试连接 WebSocket
            _ = webSocketClient.ConnectAsync(appSettings.WebSocketServerAddress);

            // 初始化 SpeechService
            speechService = new SpeechService();
            speechService.OnSpeechRecognized += SpeechService_OnSpeechRecognized;
            speechService.OnSpeechSynthesisStarted += SpeechService_OnSpeechSynthesisStarted;
            speechService.OnSpeechSynthesisEnded += SpeechService_OnSpeechSynthesisEnded;
            speechService.OnError += SpeechService_OnError;

            // 初始化 AI 服务
            aiService = new AIService(webSocketClient, new AIServiceConfig
            {
                ModelPath = System.IO.Path.Combine(Application.StartupPath, "models", "model.onnx"),
                KnowledgeBasePath = System.IO.Path.Combine(Application.StartupPath, "data", "knowledge.json"),
                UseGPU = false,
                EnableCloudFallback = false
            });
            aiManagerForm = new AIManagerForm(aiService);            Logger.LogInfo("MainForm 构造函数结束。");
        }        // 窗口加载完成后确保透明生效
        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            
            // 使用绿色作为透明色，避免洋红边框
            this.BackColor = Color.Lime; // 使用亮绿色，确保与视频绿幕一致
            this.TransparencyKey = Color.Lime;
            
            Logger.LogInfo("窗口加载完成，设置绿色透明");
        }

        // 窗口显示后再次确保透明
        protected override void SetVisibleCore(bool value)
        {
            base.SetVisibleCore(value);
            
            if (value)
            {
                // 延迟设置透明，确保窗口完全创建后再设置
                this.BeginInvoke(new Action(() =>
                {
                    this.BackColor = Color.Lime; // 使用亮绿色
                    this.TransparencyKey = Color.Lime;
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
            ResetKeyboardHandling();        }        // 应用设置到主窗口和数字人显示模块        private void ApplySettings(SettingsForm.AppSettings settings)
        {
            Logger.LogInfo("应用设置中...");
            // 更新窗口大小和位置
            this.Size = new System.Drawing.Size(settings.WindowWidth, settings.WindowHeight);
            if (settings.WindowX != -1 && settings.WindowY != -1)
            {
                this.Location = new Point(settings.WindowX, settings.WindowY);
            }
            
            // 根据抠像设置更新窗口透明色
            if (settings.EnableChromaKey)
            {
                // 启用抠像时设置窗口透明色为绿色
                this.BackColor = Color.Green;
                this.TransparencyKey = Color.Green;
                Logger.LogInfo("启用抠像，设置窗口透明色为绿色");
            }
            else
            {
                // 禁用抠像时移除窗口透明色
                this.BackColor = SystemColors.Control;
                this.TransparencyKey = Color.Empty;
                Logger.LogInfo("禁用抠像，移除窗口透明色");
            }
            
            // 强制刷新窗口透明设置
            this.Invalidate();
            this.Update();
            
            // 更新数字人显示模块的抠像功能
            digitalHumanDisplay.EnableChromaKey = settings.EnableChromaKey;
            digitalHumanDisplay.ChromaKeyColor = settings.ChromaKeyColor;
            digitalHumanDisplay.Tolerance = settings.ChromaKeyTolerance;
            digitalHumanDisplay.SetChromaKeyEnabled(settings.EnableChromaKey);

            // 更新窗口置顶和点击穿透
            SetTopMost(settings.TopMostEnabled);
            EnableClickThrough(settings.ClickThroughEnabled);

            // 更新推流地址
            digitalHumanDisplay.SetStreamUrl(settings.StreamAddress);
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
                        aiManagerForm = new AIManagerForm(aiService);
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
                {
                    Logger.LogInfo("创建新的设置窗口实例。");
                    // 将主窗体作为 Owner 传递给 SettingsForm
                    settingsForm = new SettingsForm(appSettings, this);
                    settingsForm.SettingsChanged += SettingsForm_SettingsChanged;
                }

                // 切换设置窗口的可见状态
                if (settingsForm.Visible)
                {
                    Logger.LogInfo("隐藏设置窗口。");
                    settingsForm.Hide();
                    // 确保主窗体重新获得焦点
                    this.Activate();
                    this.Focus();
                    // 重置键盘事件处理
                    ResetKeyboardHandling();
                }
                else
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
        }
    }
}
