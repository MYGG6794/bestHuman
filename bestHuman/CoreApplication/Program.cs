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

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        // Window styles
        public const int GWL_EXSTYLE = -20;
        public const int WS_EX_LAYERED = 0x80000;
        public const int WS_EX_TRANSPARENT = 0x20;
        public const int WS_EX_TOPMOST = 0x00000008; // For topmost

        // SetWindowPos flags
        public const uint SWP_NOSIZE = 0x0001;
        public const uint SWP_NOMOVE = 0x0002;
        public const uint SWP_NOACTIVATE = 0x0010;

        // Special window handles
        public static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        public static readonly IntPtr HWND_NOTOPMOST = new IntPtr(-2);

        private DigitalHumanDisplay digitalHumanDisplay;
        private SettingsForm settingsForm;
        private SettingsForm.AppSettings appSettings;
        private WebSocketClient webSocketClient;
        private SpeechService? speechService; // 声明为可空
        private AIService? aiService;
        private AIManagerForm? aiManagerForm;

        public MainForm()
        {
            Logger.LogInfo("MainForm 构造函数开始。");
            // 加载配置
            appSettings = LoadSettings();

            Text = "bestHuman 数字人助手";
            Size = new System.Drawing.Size(appSettings.WindowWidth, appSettings.WindowHeight);
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.None; // 无边框

            // 初始化 DigitalHumanDisplay 控件
            digitalHumanDisplay = new DigitalHumanDisplay();
            digitalHumanDisplay.Dock = DockStyle.Fill; // 填充整个窗口
            Controls.Add(digitalHumanDisplay);

            // 根据配置设置窗口置顶和点击穿透
            SetTopMost(appSettings.TopMostEnabled);
            EnableClickThrough(appSettings.ClickThroughEnabled);

            // 初始化 SettingsForm
            settingsForm = new SettingsForm(appSettings);
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
            aiManagerForm = new AIManagerForm(aiService);

            // 注册快捷键 (例如：Ctrl + Alt + S)
            this.KeyPreview = true; // 允许窗体接收按键事件
            this.KeyDown += MainForm_KeyDown;

            // 示例：模拟接收像素流数据
            // 为了演示，这里可以创建一个假图像
            // Bitmap dummyImage = new Bitmap(800, 600);
            // using (Graphics g = Graphics.FromImage(dummyImage))
            // {
            //     g.FillRectangle(Brushes.Blue, 0, 0, 800, 600);
            //     g.FillEllipse(Brushes.Red, 100, 100, 200, 200);
            // }
            // digitalHumanDisplay.SetPixelStream(dummyImage);
            Logger.LogInfo("MainForm 构造函数结束。");
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
        }

        // 应用设置到主窗口和数字人显示模块
        private void ApplySettings(SettingsForm.AppSettings settings)
        {
            Logger.LogInfo("应用设置中...");
            // 更新窗口大小和位置
            this.Size = new System.Drawing.Size(settings.WindowWidth, settings.WindowHeight);
            if (settings.WindowX != -1 && settings.WindowY != -1)
            {
                this.Location = new Point(settings.WindowX, settings.WindowY);
            }

            // 更新数字人显示模块的抠像颜色和容差
            digitalHumanDisplay.ChromaKeyColor = settings.ChromaKeyColor;
            digitalHumanDisplay.Tolerance = settings.ChromaKeyTolerance;

            // 更新窗口置顶和点击穿透
            SetTopMost(settings.TopMostEnabled);
            EnableClickThrough(settings.ClickThroughEnabled);

            // TODO: 更新推流地址等，需要网络通信模块
            // digitalHumanDisplay.SetStreamAddress(settings.StreamAddress); // 假设 DigitalHumanDisplay 有此方法
            Logger.LogInfo("设置应用完成。");
        }

        // 调用 AI 管理界面的快捷键：Ctrl + Alt + A
        private void MainForm_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.Control && e.Alt)
            {
                switch (e.KeyCode)
                {
                    case Keys.S:
                        Logger.LogInfo("Ctrl + Alt + S 快捷键被按下。");
                        if (settingsForm.Visible)
                        {
                            settingsForm.Hide();
                            Logger.LogInfo("设置界面已隐藏。");
                        }
                        else
                        {
                            settingsForm.Show();
                            Logger.LogInfo("设置界面已显示。");
                        }
                        e.Handled = true;
                        break;

                    case Keys.A:
                        Logger.LogInfo("Ctrl + Alt + A 快捷键被按下。");
                        if (aiManagerForm?.Visible == true)
                        {
                            aiManagerForm.Hide();
                            Logger.LogInfo("AI管理界面已隐藏。");
                        }
                        else if (aiManagerForm != null)
                        {
                            aiManagerForm.Show();
                            Logger.LogInfo("AI管理界面已显示。");
                        }
                        e.Handled = true;
                        break;
                }
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
                SetWindowLong(this.Handle, GWL_EXSTYLE, exStyle | WS_EX_LAYERED | WS_EX_TRANSPARENT);
            }
            else
            {
                // 移除 WS_EX_LAYERED 和 WS_EX_TRANSPARENT 样式
                SetWindowLong(this.Handle, GWL_EXSTYLE, exStyle & ~(WS_EX_LAYERED | WS_EX_TRANSPARENT));
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
    }
}
