using System;
using System.Drawing;
using System.Windows.Forms;
using System.IO;
using System.Text.Json;

namespace CoreApplication
{
    public partial class SettingsForm : Form
    {
        // 定义事件，用于通知主窗口设置已更改
        public event EventHandler<SettingsChangedEventArgs>? SettingsChanged;        // 假设的配置类
        public class AppSettings
        {            
            public string StreamAddress { get; set; } = "http://127.0.0.1:11188/video.html";
            public string WebSocketServerAddress { get; set; } = "ws://localhost:8080";
            public Color ChromaKeyColor { get; set; } = Color.Green;
            public int ChromaKeyTolerance { get; set; } = 30; // For WebView2 or future use
            public bool EnableChromaKey { get; set; } = false; // Added EnableChromaKey property
            public int WindowWidth { get; set; } = 800;
            public int WindowHeight { get; set; } = 600;
            public int WindowX { get; set; } = -1;
            public int WindowY { get; set; } = -1;
            public bool ClickThroughEnabled { get; set; } = true; // May need adjustment for layered window
            public bool TopMostEnabled { get; set; } = true;
            public string TtsVoiceName { get; set; } = "Microsoft Huihui";
            public int TtsRate { get; set; } = 0;
            public int TtsVolume { get; set; } = 100;
            public bool UseNativeLayeredWindow { get; set; } = false; // New setting

            // AI服务配置
            public string ModelPath { get; set; } = "";
            public string KnowledgeBasePath { get; set; } = "";
            public bool UseGPU { get; set; } = false;
            public bool EnableCloudFallback { get; set; } = false;
            public string? CloudAPIKey { get; set; }
            public string? CloudAPIEndpoint { get; set; }

            // Static methods for configuration persistence
            private static string GetSettingsFilePath()
            {
                return Path.Combine(Application.StartupPath, "appsettings.json");
            }

            public static AppSettings Load()
            {
                try
                {
                    string filePath = GetSettingsFilePath();
                    if (File.Exists(filePath))
                    {
                        string json = File.ReadAllText(filePath);
                        var settings = JsonSerializer.Deserialize<AppSettings>(json);
                        return settings ?? new AppSettings();
                    }
                }
                catch (Exception ex)
                {
                    // Log error if Logger is available
                    try { Logger.LogError($"Error loading settings: {ex.Message}", ex); } catch { }
                }
                return new AppSettings();
            }

            public void Save()
            {
                try
                {
                    string filePath = GetSettingsFilePath();
                    string json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
                    File.WriteAllText(filePath, json);
                }
                catch (Exception ex)
                {
                    // Log error if Logger is available
                    try { Logger.LogError($"Error saving settings: {ex.Message}", ex); } catch { }
                }
            }
        }

        public class SettingsChangedEventArgs : EventArgs
        {
            public AppSettings Settings { get; }
            public SettingsChangedEventArgs(AppSettings settings)
            {
                Settings = settings;
            }
        }

        // 字段定义
        private readonly AppSettings _currentSettings;
        private ScriptEditForm? _scriptEditForm;
        private ScriptPlayerForm? _scriptPlayerForm;
        private AIManagerForm? _aiManagerForm;
        private readonly ScriptService _scriptService;
        private readonly AIService _aiService;        // UI控件字段
        private TextBox txtStreamAddress = null!;
        private TextBox txtWebSocketAddress = null!;
        private CheckBox chkEnableChromaKey = null!; // 添加启用抠像的复选框字段
        private Button btnChromaKeyColor = null!;
        private NumericUpDown numChromaKeyTolerance = null!;
        private CheckBox chkTopMost = null!;
        private CheckBox chkClickThrough = null!;
        private NumericUpDown numWidth = null!;
        private NumericUpDown numHeight = null!;
        private CheckBox chkUseNativeLayeredWindow = null!; // New CheckBox field
        private ComboBox cboVoices = null!;
        private TrackBar trkRate = null!;
        private TrackBar trkVolume = null!;
        private TextBox txtModelPath = null!;
        private TextBox txtKnowledgeBasePath = null!;
        private CheckBox chkUseGPU = null!;
        private CheckBox chkEnableCloudFallback = null!;
        private TextBox txtCloudAPIKey = null!;
        private TextBox txtCloudAPIEndpoint = null!;
        private Button btnAIManager = null!;
        private Button btnScriptManager = null!;
        private Button btnScriptPlayer = null!;public SettingsForm(AppSettings initialSettings, AIService? aiService = null, ScriptService? scriptService = null)
        {
            _currentSettings = initialSettings;
            
            // 添加窗体关闭事件处理
            this.FormClosing += (s, e) => {
                if (e.CloseReason == CloseReason.UserClosing)
                {
                    e.Cancel = true;  // 取消关闭
                    this.Hide();      // 改为隐藏
                }
            };
            
            try
            {
                // 使用传入的服务实例，或创建新的实例
                if (aiService != null)
                {
                    _aiService = aiService;
                }
                else
                {
                    _aiService = new AIService(
                        Program.WebSocketClient,
                        new AIServiceConfig
                        {
                            ModelPath = _currentSettings.ModelPath,
                            KnowledgeBasePath = _currentSettings.KnowledgeBasePath,
                            UseGPU = _currentSettings.UseGPU,
                            EnableCloudFallback = _currentSettings.EnableCloudFallback,
                            CloudAPIKey = _currentSettings.CloudAPIKey,
                            CloudAPIEndpoint = _currentSettings.CloudAPIEndpoint
                        }
                    );
                }

                if (scriptService != null)
                {
                    _scriptService = scriptService;
                }
                else
                {
                    // 初始化默认服务
                    var speechService = new SpeechService();
                    _scriptService = new ScriptService(
                        Program.WebSocketClient,
                        speechService
                    );
                }

                InitializeComponent();
                LoadSettingsToUI(_currentSettings);
            }
            catch (Exception ex)
            {
                Logger.LogError("初始化SettingsForm失败", ex);
                MessageBox.Show(
                    "初始化设置窗口时发生错误。\n" +
                    "请检查系统音频设备是否正常。\n\n" +
                    $"详细信息：{ex.Message}",
                    "初始化错误",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
                throw;
            }
        }

        private void InitializeComponent()
        {
            this.Text = "设置";
            this.Size = new Size(600, 800);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            var tabControl = new TabControl
            {
                Dock = DockStyle.Fill,
                Padding = new Point(10, 10)
            };

            // 数字人显示设置页
            var tabDisplay = new TabPage("显示设置");
            var pnlDisplay = new Panel { Dock = DockStyle.Fill, Padding = new Padding(10) };
            
            // 推流地址
            var lblStreamAddress = new Label { Text = "推流地址:", Location = new Point(10, 20), AutoSize = true };
            txtStreamAddress = new TextBox { Location = new Point(120, 17), Width = 400 };
            
            // WebSocket服务器地址
            var lblWebSocketAddress = new Label { Text = "WebSocket地址:", Location = new Point(10, 50), AutoSize = true };
            txtWebSocketAddress = new TextBox { Location = new Point(120, 47), Width = 400 };            // 抠像设置
            var grpChromaKey = new GroupBox { Text = "抠像设置", Location = new Point(10, 80), Size = new Size(540, 120) }; // 增加高度
            
            // 添加启用抠像的复选框
            chkEnableChromaKey = new CheckBox { Text = "启用抠像功能", Location = new Point(10, 25), AutoSize = true };
            
            btnChromaKeyColor = new Button { Text = "选择背景色", Location = new Point(10, 55), Width = 100 };
            var lblTolerance = new Label { Text = "容差:", Location = new Point(120, 57), AutoSize = true };
            numChromaKeyTolerance = new NumericUpDown 
            { 
                Location = new Point(170, 55), 
                Width = 60,
                Minimum = 0,
                Maximum = 100,
                Value = 30
            };
            
            grpChromaKey.Controls.AddRange(new Control[] { chkEnableChromaKey, btnChromaKeyColor, lblTolerance, numChromaKeyTolerance });
              // 窗口设置
            var grpWindow = new GroupBox { Text = "窗口设置", Location = new Point(10, 210), Size = new Size(540, 150) }; // 调整位置到 210
            chkTopMost = new CheckBox { Text = "窗口置顶", Location = new Point(10, 25), AutoSize = true };
            chkClickThrough = new CheckBox { Text = "点击穿透 (WebView2)", Location = new Point(120, 25), AutoSize = true }; // Clarified for WebView2
            
            var lblSize = new Label { Text = "窗口大小:", Location = new Point(10, 55), AutoSize = true };
            var lblWidth = new Label { Text = "宽度:", Location = new Point(80, 55), AutoSize = true };
            numWidth = new NumericUpDown 
            { 
                Location = new Point(120, 53), 
                Width = 60,
                Minimum = 100,
                Maximum = 3840, // Increased max width
                Value = 800
            };
            var lblHeight = new Label { Text = "高度:", Location = new Point(200, 55), AutoSize = true };
            numHeight = new NumericUpDown 
            { 
                Location = new Point(240, 53), 
                Width = 60,
                Minimum = 100,
                Maximum = 2160, // Increased max height
                Value = 600
            };

            chkUseNativeLayeredWindow = new CheckBox { Text = "使用原生透明窗口", Location = new Point(10, 85), AutoSize = true };
            
            grpWindow.Controls.AddRange(new Control[] 
            { 
                chkTopMost, chkClickThrough, 
                lblSize, lblWidth, numWidth, 
                lblHeight, numHeight,
                chkUseNativeLayeredWindow // Added new checkbox
            });
            
            pnlDisplay.Controls.AddRange(new Control[] 
            {
                lblStreamAddress, txtStreamAddress,
                lblWebSocketAddress, txtWebSocketAddress,
                grpChromaKey, grpWindow
            });
            
            tabDisplay.Controls.Add(pnlDisplay);
            
            // TTS设置页
            var tabTTS = new TabPage("语音设置");
            var pnlTTS = new Panel { Dock = DockStyle.Fill, Padding = new Padding(10) };
            
            var lblVoice = new Label { Text = "语音:", Location = new Point(10, 20), AutoSize = true };
            cboVoices = new ComboBox { Location = new Point(120, 17), Width = 200, DropDownStyle = ComboBoxStyle.DropDownList };
            
            var lblRate = new Label { Text = "语速:", Location = new Point(10, 50), AutoSize = true };
            trkRate = new TrackBar 
            { 
                Location = new Point(120, 47), 
                Width = 200,
                Minimum = -10,
                Maximum = 10
            };
            
            var lblVolume = new Label { Text = "音量:", Location = new Point(10, 80), AutoSize = true };
            trkVolume = new TrackBar 
            { 
                Location = new Point(120, 77), 
                Width = 200,
                Minimum = 0,
                Maximum = 100,
                Value = 100
            };
            
            pnlTTS.Controls.AddRange(new Control[] 
            {
                lblVoice, cboVoices,
                lblRate, trkRate,
                lblVolume, trkVolume
            });
            
            tabTTS.Controls.Add(pnlTTS);
            
            // AI设置页
            var tabAI = new TabPage("AI设置");
            var pnlAI = new Panel { Dock = DockStyle.Fill, Padding = new Padding(10) };
            
            var lblModelPath = new Label { Text = "模型路径:", Location = new Point(10, 20), AutoSize = true };
            txtModelPath = new TextBox { Location = new Point(120, 17), Width = 400 };
            
            var lblKnowledgeBasePath = new Label { Text = "知识库路径:", Location = new Point(10, 50), AutoSize = true };
            txtKnowledgeBasePath = new TextBox { Location = new Point(120, 47), Width = 400 };
            
            chkUseGPU = new CheckBox { Text = "启用GPU加速", Location = new Point(10, 80), AutoSize = true };
            chkEnableCloudFallback = new CheckBox { Text = "启用云端回退", Location = new Point(10, 110), AutoSize = true };
            
            var lblCloudAPIKey = new Label { Text = "API密钥:", Location = new Point(30, 140), AutoSize = true };
            txtCloudAPIKey = new TextBox { Location = new Point(120, 137), Width = 400 };
            
            var lblCloudAPIEndpoint = new Label { Text = "API地址:", Location = new Point(30, 170), AutoSize = true };
            txtCloudAPIEndpoint = new TextBox { Location = new Point(120, 167), Width = 400 };
            
            // AI管理按钮
            btnAIManager = new Button { Text = "AI管理", Location = new Point(10, 200), Width = 100 };
            btnAIManager.Click += BtnAIManager_Click;
            
            pnlAI.Controls.AddRange(new Control[] 
            {
                lblModelPath, txtModelPath,
                lblKnowledgeBasePath, txtKnowledgeBasePath,
                chkUseGPU, chkEnableCloudFallback,
                lblCloudAPIKey, txtCloudAPIKey,
                lblCloudAPIEndpoint, txtCloudAPIEndpoint,
                btnAIManager
            });
            
            tabAI.Controls.Add(pnlAI);
            
            // 讲解词管理页
            var tabScript = new TabPage("讲解词管理");
            var pnlScript = new Panel { Dock = DockStyle.Fill, Padding = new Padding(10) };
            
            // 讲解词编辑和播放按钮
            btnScriptManager = new Button { Text = "讲解词编辑", Location = new Point(10, 20), Width = 100 };
            btnScriptManager.Click += BtnScriptManager_Click;
            
            btnScriptPlayer = new Button { Text = "播放控制", Location = new Point(120, 20), Width = 100 };
            btnScriptPlayer.Click += BtnScriptPlayer_Click;
            
            pnlScript.Controls.AddRange(new Control[] { btnScriptManager, btnScriptPlayer });
            
            tabScript.Controls.Add(pnlScript);
            
            // 添加所有标签页
            tabControl.TabPages.AddRange(new TabPage[] { tabDisplay, tabTTS, tabAI, tabScript });
            
            // 添加保存按钮
            var btnSave = new Button 
            { 
                Text = "保存",
                Dock = DockStyle.Bottom,
                Height = 30
            };
            btnSave.Click += BtnSave_Click;
            
            this.Controls.AddRange(new Control[] { tabControl, btnSave });
            
            // 事件处理
            btnChromaKeyColor.Click += (s, e) =>
            {
                using var colorDialog = new ColorDialog();
                if (colorDialog.ShowDialog() == DialogResult.OK)
                {
                    btnChromaKeyColor.BackColor = colorDialog.Color;
                }
            };
            
            chkEnableCloudFallback.CheckedChanged += (s, e) =>
            {
                txtCloudAPIKey.Enabled = chkEnableCloudFallback.Checked;
                txtCloudAPIEndpoint.Enabled = chkEnableCloudFallback.Checked;
            };
        }        private void LoadSettingsToUI(AppSettings settings)
        {
            txtStreamAddress.Text = settings.StreamAddress;
            txtWebSocketAddress.Text = settings.WebSocketServerAddress;
            chkEnableChromaKey.Checked = settings.EnableChromaKey; // 加载抠像启用状态
            btnChromaKeyColor.BackColor = settings.ChromaKeyColor;
            numChromaKeyTolerance.Value = settings.ChromaKeyTolerance;
            chkTopMost.Checked = settings.TopMostEnabled;
            chkClickThrough.Checked = settings.ClickThroughEnabled;
            numWidth.Value = settings.WindowWidth;
            numHeight.Value = settings.WindowHeight;
            chkUseNativeLayeredWindow.Checked = settings.UseNativeLayeredWindow; // Load new setting
            cboVoices.Text = settings.TtsVoiceName;
            trkRate.Value = settings.TtsRate;
            trkVolume.Value = settings.TtsVolume;
            txtModelPath.Text = settings.ModelPath;
            txtKnowledgeBasePath.Text = settings.KnowledgeBasePath;
            chkUseGPU.Checked = settings.UseGPU;
            chkEnableCloudFallback.Checked = settings.EnableCloudFallback;
            txtCloudAPIKey.Text = settings.CloudAPIKey ?? string.Empty;
            txtCloudAPIEndpoint.Text = settings.CloudAPIEndpoint ?? string.Empty;
        }        private void SaveSettingsFromUI(AppSettings settings)
        {
            if (txtStreamAddress != null) settings.StreamAddress = txtStreamAddress.Text;
            if (txtWebSocketAddress != null) settings.WebSocketServerAddress = txtWebSocketAddress.Text;
            if (chkEnableChromaKey != null) settings.EnableChromaKey = chkEnableChromaKey.Checked; // 保存抠像启用状态
            if (btnChromaKeyColor != null) settings.ChromaKeyColor = btnChromaKeyColor.BackColor;
            if (numChromaKeyTolerance != null) settings.ChromaKeyTolerance = (int)numChromaKeyTolerance.Value;
            if (chkTopMost != null) settings.TopMostEnabled = chkTopMost.Checked;
            if (chkClickThrough != null) settings.ClickThroughEnabled = chkClickThrough.Checked;
            if (numWidth != null) settings.WindowWidth = (int)numWidth.Value;
            if (numHeight != null) settings.WindowHeight = (int)numHeight.Value;
            if (chkUseNativeLayeredWindow != null) settings.UseNativeLayeredWindow = chkUseNativeLayeredWindow.Checked;
            if (cboVoices != null) settings.TtsVoiceName = cboVoices.Text;
            if (trkRate != null) settings.TtsRate = trkRate.Value;
            if (trkVolume != null) settings.TtsVolume = trkVolume.Value;
            if (txtModelPath != null) settings.ModelPath = txtModelPath.Text;
            if (txtKnowledgeBasePath != null) settings.KnowledgeBasePath = txtKnowledgeBasePath.Text;
            if (chkUseGPU != null) settings.UseGPU = chkUseGPU.Checked;
            if (chkEnableCloudFallback != null) settings.EnableCloudFallback = chkEnableCloudFallback.Checked;
            if (txtCloudAPIKey != null) settings.CloudAPIKey = txtCloudAPIKey.Text;
            if (txtCloudAPIEndpoint != null) settings.CloudAPIEndpoint = txtCloudAPIEndpoint.Text;
        }private void BtnSave_Click(object? sender, EventArgs e)
        {
            SaveSettingsFromUI(_currentSettings);
            _currentSettings.Save(); // Save to file
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
        }        private void BtnAIManager_Click(object? sender, EventArgs e)
        {
            if (_aiManagerForm == null || _aiManagerForm.IsDisposed)
            {
                _aiManagerForm = new AIManagerForm(_aiService, _currentSettings);
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
    }
}
