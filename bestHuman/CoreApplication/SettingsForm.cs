using System;
using System.Drawing;
using System.Windows.Forms;

namespace CoreApplication
{
    public partial class SettingsForm : Form
    {
        // 定义事件，用于通知主窗口设置已更改
        public event EventHandler<SettingsChangedEventArgs>? SettingsChanged;

        // 假设的配置类
        public class AppSettings
        {
            public string StreamAddress { get; set; } = "ws://localhost:8888";
            public string WebSocketServerAddress { get; set; } = "ws://localhost:8080"; // 新增 WebSocket 服务器地址
            public Color ChromaKeyColor { get; set; } = Color.Green;
            public int ChromaKeyTolerance { get; set; } = 30;
            public int WindowWidth { get; set; } = 800;
            public int WindowHeight { get; set; } = 600;
            public int WindowX { get; set; } = -1; // -1 表示居中
            public int WindowY { get; set; } = -1; // -1 表示居中
            public bool ClickThroughEnabled { get; set; } = true;
            public bool TopMostEnabled { get; set; } = true;
            public string TtsVoiceName { get; set; } = "Microsoft Huihui"; // 默认音色
            public int TtsRate { get; set; } = 0; // 语速
            public int TtsVolume { get; set; } = 100; // 音量

            // AI服务配置
            public string ModelPath { get; set; } = ""; // AI模型路径
            public string KnowledgeBasePath { get; set; } = ""; // 知识库路径
            public bool UseGPU { get; set; } = false; // 是否启用GPU推理
            public bool EnableCloudFallback { get; set; } = false; // 是否启用云端回退
            public string? CloudAPIKey { get; set; } // 云端API密钥
            public string? CloudAPIEndpoint { get; set; } // 云端API地址
        }

        public class SettingsChangedEventArgs : EventArgs
        {
            public AppSettings Settings { get; }
            public SettingsChangedEventArgs(AppSettings settings)
            {
                Settings = settings;
            }
        }

        private AppSettings _currentSettings;
        private ScriptEditForm? _scriptEditForm;
        private ScriptPlayerForm? _scriptPlayerForm;
        private AIManagerForm? _aiManagerForm;
        private readonly ScriptService _scriptService;
        private readonly AIService _aiService;

        public SettingsForm(AppSettings initialSettings)
        {
            _currentSettings = initialSettings;
            
            // 初始化服务
            _scriptService = new ScriptService(
                Program.WebSocketClient,
                new SpeechService()
            );

            _aiService = new AIService(
                Program.WebSocketClient,
                new AIServiceConfig
                {
                    ModelPath = Path.Combine(Application.StartupPath, "models", "model.onnx"),
                    KnowledgeBasePath = Path.Combine(Application.StartupPath, "data", "knowledge.json"),
                    UseGPU = false,
                    EnableCloudFallback = false
                }
            );
            
            InitializeComponent(); // 自动生成的组件初始化方法
            LoadSettingsToUI(_currentSettings);
        }

        private void InitializeComponent()
        {
            this.Text = "bestHuman 后台管理";
            this.Size = new System.Drawing.Size(400, 600);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog; // 固定大小对话框
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.ShowInTaskbar = false; // 不在任务栏显示

            // 示例：添加一个文本框用于设置推流地址
            Label lblStreamAddress = new Label { Text = "推流地址:", Location = new Point(20, 20), AutoSize = true };
            TextBox txtStreamAddress = new TextBox { Name = "txtStreamAddress", Location = new Point(120, 20), Width = 200 };
            
            // 示例：添加一个按钮用于保存设置
            Button btnSave = new Button { Text = "保存设置", Location = new Point(20, 500), Width = 100 };
            btnSave.Click += BtnSave_Click;

            this.Controls.Add(lblStreamAddress);
            this.Controls.Add(txtStreamAddress);
            
            // 新增 WebSocket 服务器地址配置
            Label lblWebSocketAddress = new Label { Text = "WebSocket地址:", Location = new Point(20, 50), AutoSize = true };
            TextBox txtWebSocketAddress = new TextBox { Name = "txtWebSocketAddress", Location = new Point(120, 50), Width = 200 };
            this.Controls.Add(lblWebSocketAddress);
            this.Controls.Add(txtWebSocketAddress);

            // 新增语音合成设置
            Label lblTtsVoice = new Label { Text = "TTS 音色:", Location = new Point(20, 80), AutoSize = true };
            ComboBox cmbTtsVoice = new ComboBox { Name = "cmbTtsVoice", Location = new Point(120, 80), Width = 200, DropDownStyle = ComboBoxStyle.DropDownList };
            Label lblTtsRate = new Label { Text = "TTS 语速:", Location = new Point(20, 110), AutoSize = true };
            TrackBar trkTtsRate = new TrackBar { Name = "trkTtsRate", Location = new Point(120, 100), Width = 200, Minimum = -10, Maximum = 10, TickFrequency = 1 };
            Label lblTtsVolume = new Label { Text = "TTS 音量:", Location = new Point(20, 140), AutoSize = true };
            TrackBar trkTtsVolume = new TrackBar { Name = "trkTtsVolume", Location = new Point(120, 130), Width = 200, Minimum = 0, Maximum = 100, TickFrequency = 10 };

            this.Controls.Add(lblTtsVoice);
            this.Controls.Add(cmbTtsVoice);
            this.Controls.Add(lblTtsRate);
            this.Controls.Add(trkTtsRate);
            this.Controls.Add(lblTtsVolume);
            this.Controls.Add(trkTtsVolume);

            // 添加功能管理按钮
            Button btnScriptManager = new Button
            {
                Text = "讲解词管理",
                Location = new Point(20, 170),
                Width = 120,
                Height = 30
            };
            btnScriptManager.Click += BtnScriptManager_Click;

            Button btnScriptPlayer = new Button
            {
                Text = "播放控制",
                Location = new Point(160, 170),
                Width = 120,
                Height = 30
            };
            btnScriptPlayer.Click += BtnScriptPlayer_Click;

            this.Controls.Add(btnScriptManager);
            this.Controls.Add(btnScriptPlayer);
            // 添加AI管理按钮
            Button btnAIManager = new Button
            {
                Text = "AI配置",
                Location = new Point(300, 170),
                Width = 120,
                Height = 30
            };
            btnAIManager.Click += BtnAIManager_Click;

            this.Controls.Add(btnAIManager);
            this.Controls.Add(btnSave);

            // 在控件添加到 Controls 集合后，再设置其属性
            if (_currentSettings != null)
            {
                txtStreamAddress.Text = _currentSettings.StreamAddress;
                txtWebSocketAddress.Text = _currentSettings.WebSocketServerAddress;
                trkTtsRate.Value = _currentSettings.TtsRate;
                trkTtsVolume.Value = _currentSettings.TtsVolume;
            }
        }

        private void LoadSettingsToUI(AppSettings settings)
        {
            // 将设置加载到UI控件
            // 例如：((TextBox)this.Controls["txtStreamAddress"]).Text = settings.StreamAddress;
            // 由于现在是手动设置，这里可以根据需要添加更多控件的加载逻辑
            if (this.Controls["txtStreamAddress"] is TextBox txtStreamAddress)
            {
                txtStreamAddress.Text = settings.StreamAddress;
            }
            if (this.Controls["txtWebSocketAddress"] is TextBox txtWebSocketAddress)
            {
                txtWebSocketAddress.Text = settings.WebSocketServerAddress;
            }
            if (this.Controls["cmbTtsVoice"] is ComboBox cmbTtsVoice)
            {
                // 使用 SpeechService 获取可用语音列表
                using (var speechService = new SpeechService())
                {
                    var voices = speechService.GetAvailableVoices();
                    cmbTtsVoice.Items.Clear();
                    foreach (var voice in voices)
                    {
                        cmbTtsVoice.Items.Add(voice.Name);
                    }
                }
                
                // 设置当前选中的语音
                if (!string.IsNullOrEmpty(settings.TtsVoiceName))
                {
                    int index = cmbTtsVoice.Items.IndexOf(settings.TtsVoiceName);
                    if (index >= 0)
                    {
                        cmbTtsVoice.SelectedIndex = index;
                    }
                    else if (cmbTtsVoice.Items.Count > 0)
                    {
                        // 如果找不到指定的语音，选择第一个可用的语音
                        cmbTtsVoice.SelectedIndex = 0;
                        settings.TtsVoiceName = cmbTtsVoice.Items[0]?.ToString() ?? "Microsoft Huihui";
                    }
                }
                else if (cmbTtsVoice.Items.Count > 0)
                {
                    // 如果没有设置语音，选择第一个可用的语音
                    cmbTtsVoice.SelectedIndex = 0;
                    settings.TtsVoiceName = cmbTtsVoice.Items[0]?.ToString() ?? "Microsoft Huihui";
                }
            }
            if (this.Controls["trkTtsRate"] is TrackBar trkTtsRate)
            {
                settings.TtsRate = trkTtsRate.Value;
            }
            if (this.Controls["trkTtsVolume"] is TrackBar trkTtsVolume)
            {
                settings.TtsVolume = trkTtsVolume.Value;
            }
        }

        private void SaveSettingsFromUI(AppSettings settings)
        {
            // 从UI控件获取设置并保存
            // 例如：settings.StreamAddress = ((TextBox)this.Controls["txtStreamAddress"]).Text;
            if (this.Controls["txtStreamAddress"] is TextBox txtStreamAddress)
            {
                settings.StreamAddress = txtStreamAddress.Text;
            }
            if (this.Controls["txtWebSocketAddress"] is TextBox txtWebSocketAddress)
            {
                settings.WebSocketServerAddress = txtWebSocketAddress.Text;
            }
            // 保存语音设置
            if (this.Controls["cmbTtsVoice"] is ComboBox cmbTtsVoice)
            {
                settings.TtsVoiceName = cmbTtsVoice.SelectedItem?.ToString() ?? settings.TtsVoiceName ?? "Microsoft Huihui";
            }
            
            // 保存语速设置
            if (this.Controls["trkTtsRate"] is TrackBar trkTtsRate)
            {
                try
                {
                    settings.TtsRate = trkTtsRate.Value;
                }
                catch
                {
                    settings.TtsRate = 0; // 如果出现异常，使用默认值
                }
            }
            
            // 保存音量设置
            if (this.Controls["trkTtsVolume"] is TrackBar trkTtsVolume)
            {
                try
                {
                    settings.TtsVolume = trkTtsVolume.Value;
                }
                catch
                {
                    settings.TtsVolume = 100; // 如果出现异常，使用默认值
                }
            }
        }

        private void BtnSave_Click(object? sender, EventArgs e)
        {
            SaveSettingsFromUI(_currentSettings);
            // 触发设置更改事件
            SettingsChanged?.Invoke(this, new SettingsChangedEventArgs(_currentSettings));
            MessageBox.Show("设置已保存！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            this.Hide(); // 保存后隐藏窗口
        }

        private void BtnScriptManager_Click(object? sender, EventArgs e)
        {
            if (_scriptEditForm == null || _scriptEditForm.IsDisposed)
            {
                _scriptEditForm = new ScriptEditForm(_scriptService);
            }
            _scriptEditForm.Show();
            if (_scriptEditForm.WindowState == FormWindowState.Minimized)
            {
                _scriptEditForm.WindowState = FormWindowState.Normal;
            }
            _scriptEditForm.BringToFront();
        }

        private void BtnScriptPlayer_Click(object? sender, EventArgs e)
        {
            if (_scriptPlayerForm == null || _scriptPlayerForm.IsDisposed)
            {
                _scriptPlayerForm = new ScriptPlayerForm(_scriptService);
            }
            _scriptPlayerForm.Show();
            if (_scriptPlayerForm.WindowState == FormWindowState.Minimized)
            {
                _scriptPlayerForm.WindowState = FormWindowState.Normal;
            }
            _scriptPlayerForm.BringToFront();
        }

        private void BtnAIManager_Click(object? sender, EventArgs e)
        {
            if (_aiManagerForm == null || _aiManagerForm.IsDisposed)
            {
                _aiManagerForm = new AIManagerForm(_aiService);
            }
            _aiManagerForm.Show();
            if (_aiManagerForm.WindowState == FormWindowState.Minimized)
            {
                _aiManagerForm.WindowState = FormWindowState.Normal;
            }
            _aiManagerForm.BringToFront();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _scriptEditForm?.Dispose();
                _scriptPlayerForm?.Dispose();
                _scriptService.Dispose();
                _aiService.Dispose();
                _aiManagerForm?.Dispose();
            }
            base.Dispose(disposing);
        }

        // 快捷键管理将在 MainForm 中实现，用于显示/隐藏此窗口
    }
}