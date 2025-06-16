using System;
using System.Drawing;
using System.Windows.Forms;
using System.IO;

namespace CoreApplication
{
    public partial class SettingsForm : Form
    {
        // 定义事件，用于通知主窗口设置已更改
        public event EventHandler<SettingsChangedEventArgs>? SettingsChanged;        // 假设的配置类
        public class AppSettings
        {            public string StreamAddress { get; set; } = "http://127.0.0.1:11188/video.html";
            public string WebSocketServerAddress { get; set; } = "ws://localhost:8080";
            public Color ChromaKeyColor { get; set; } = Color.Green;
            public int ChromaKeyTolerance { get; set; } = 30;
            public bool UsePreprocessedStream { get; set; } = false; // 是否使用预处理过的视频流（已经抠像）
            public bool EnableChromaKey { get; set; } = false; // 默认禁用客户端抠像功能，避免影响性能
            public int WindowWidth { get; set; } = 800;
            public int WindowHeight { get; set; } = 600;
            public int WindowX { get; set; } = -1;
            public int WindowY { get; set; } = -1;
            public bool ClickThroughEnabled { get; set; } = true;
            public bool TopMostEnabled { get; set; } = true;
            public string TtsVoiceName { get; set; } = "Microsoft Huihui";
            public int TtsRate { get; set; } = 0;
            public int TtsVolume { get; set; } = 100;

            // AI服务配置
            public string ModelPath { get; set; } = "";
            public string KnowledgeBasePath { get; set; } = "";
            public bool UseGPU { get; set; } = false;
            public bool EnableCloudFallback { get; set; } = false;
            public string? CloudAPIKey { get; set; }
            public string? CloudAPIEndpoint { get; set; }
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
        private TextBox txtStreamAddress;
        private TextBox txtWebSocketAddress;
        private Button btnChromaKeyColor;
        private NumericUpDown numChromaKeyTolerance;
        private CheckBox chkEnableChromaKey;
        private CheckBox chkTopMost;
        private CheckBox chkClickThrough;
        private NumericUpDown numWidth;
        private NumericUpDown numHeight;
        private ComboBox cboVoices;
        private TrackBar trkRate;
        private TrackBar trkVolume;
        private TextBox txtModelPath;
        private TextBox txtKnowledgeBasePath;
        private CheckBox chkUseGPU;
        private CheckBox chkEnableCloudFallback;
        private TextBox txtCloudAPIKey;
        private TextBox txtCloudAPIEndpoint;
        private Button btnAIManager;
        private Button btnScriptManager;
        private Button btnScriptPlayer;public SettingsForm(AppSettings initialSettings, Form? owner = null)
        {
            _currentSettings = initialSettings;
            
            // 设置窗口样式
            this.ShowInTaskbar = true;
            this.FormBorderStyle = FormBorderStyle.Sizable;
            this.MaximizeBox = false;
            this.MinimizeBox = true;
            this.StartPosition = FormStartPosition.CenterScreen;
            
            // 设置窗口所有者，确保正确的Z序和焦点管理
            if (owner != null)
            {
                this.Owner = owner;
                Logger.LogInfo($"设置窗口所有者: {owner.GetType().Name}");
            }
            
            // 添加窗口状态事件处理
            this.FormClosing += SettingsForm_FormClosing;
            this.VisibleChanged += SettingsForm_VisibleChanged;
            this.Activated += SettingsForm_Activated;
            this.Deactivate += SettingsForm_Deactivate;
            
            try
            {
                // 初始化服务
                var speechService = new SpeechService();
                _scriptService = new ScriptService(
                    Program.WebSocketClient,
                    speechService
                );                _aiService = new AIService(
                    Program.WebSocketClient,
                    new AIServiceConfig
                    {
                        ModelPath = Path.Combine(Application.StartupPath, "models", "model.onnx"),
                        KnowledgeBasePath = Path.Combine(Application.StartupPath, "data", "knowledge.json"),
                        UseGPU = false,
                        EnableCloudFallback = false
                    }
                );

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
            txtWebSocketAddress = new TextBox { Location = new Point(120, 47), Width = 400 };
              // 抠像设置
            var grpChromaKey = new GroupBox { Text = "抠像设置", Location = new Point(10, 80), Size = new Size(540, 120) };
              // 启用抠像复选框
            chkEnableChromaKey = new CheckBox 
            { 
                Text = "启用客户端抠像 (⚠️ 可能影响性能)", 
                Location = new Point(10, 25), 
                AutoSize = true,
                Checked = _currentSettings.EnableChromaKey
            };
            
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

            // 添加预处理流选项
            var chkUsePreprocessedStream = new CheckBox 
            { 
                Text = "使用预处理透明流", 
                Location = new Point(250, 57), 
                AutoSize = true,
                Checked = _currentSettings.UsePreprocessedStream
            };            chkUsePreprocessedStream.CheckedChanged += (s, e) =>
            {
                _currentSettings.UsePreprocessedStream = chkUsePreprocessedStream.Checked;
                btnChromaKeyColor.Enabled = !chkUsePreprocessedStream.Checked;
                numChromaKeyTolerance.Enabled = !chkUsePreprocessedStream.Checked;
                chkEnableChromaKey.Enabled = !chkUsePreprocessedStream.Checked;
            };            // 添加一个标志避免递归调用
            bool isCheckboxUpdating = false;
              chkEnableChromaKey.CheckedChanged += (s, e) =>
            {
                if (isCheckboxUpdating) return; // 避免递归调用
                
                if (chkEnableChromaKey.Checked)
                {
                    var result = MessageBox.Show(
                        "客户端抠像功能会对性能产生影响，可能降低视频流的帧率。\n\n" +
                        "建议只在以下情况启用：\n" +
                        "1. 需要在客户端实时抠像处理\n" +
                        "2. 服务端未提供透明流\n" +
                        "3. 性能要求不高的场景\n\n" +
                        "是否确认启用？",
                        "性能警告",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Warning);
                    
                    if (result == DialogResult.No)
                    {
                        isCheckboxUpdating = true;
                        chkEnableChromaKey.Checked = false;
                        isCheckboxUpdating = false;
                        _currentSettings.EnableChromaKey = false;
                        Logger.LogInfo("用户取消启用客户端抠像功能");
                        return;
                    }
                    
                    Logger.LogInfo("用户确认启用客户端抠像功能");
                    // 确保在确认后正确设置状态
                    isCheckboxUpdating = true;
                    chkEnableChromaKey.Checked = true;
                    isCheckboxUpdating = false;
                    _currentSettings.EnableChromaKey = true;
                    Logger.LogInfo("抠像功能设置更改: 启用");
                }
                else
                {
                    // 用户取消选中复选框
                    _currentSettings.EnableChromaKey = false;
                    Logger.LogInfo("抠像功能设置更改: 禁用");
                }
            };
            
            grpChromaKey.Controls.AddRange(new Control[] { 
                chkEnableChromaKey,
                btnChromaKeyColor, 
                lblTolerance, 
                numChromaKeyTolerance,
                chkUsePreprocessedStream 
            });
            
            // 窗口设置
            var grpWindow = new GroupBox { Text = "窗口设置", Location = new Point(10, 190), Size = new Size(540, 120) };
            chkTopMost = new CheckBox { Text = "窗口置顶", Location = new Point(10, 25), AutoSize = true };
            chkClickThrough = new CheckBox { Text = "点击穿透", Location = new Point(120, 25), AutoSize = true };
            
            var lblSize = new Label { Text = "窗口大小:", Location = new Point(10, 55), AutoSize = true };
            var lblWidth = new Label { Text = "宽度:", Location = new Point(80, 55), AutoSize = true };
            numWidth = new NumericUpDown 
            { 
                Location = new Point(120, 53), 
                Width = 60,
                Minimum = 100,
                Maximum = 1920,
                Value = 800
            };
            var lblHeight = new Label { Text = "高度:", Location = new Point(200, 55), AutoSize = true };
            numHeight = new NumericUpDown 
            { 
                Location = new Point(240, 53), 
                Width = 60,
                Minimum = 100,
                Maximum = 1080,
                Value = 600
            };
            
            grpWindow.Controls.AddRange(new Control[] 
            { 
                chkTopMost, chkClickThrough, 
                lblSize, lblWidth, numWidth, 
                lblHeight, numHeight 
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
                colorDialog.Color = btnChromaKeyColor.BackColor; // 设置当前颜色为默认选择
                if (colorDialog.ShowDialog() == DialogResult.OK)
                {
                    btnChromaKeyColor.BackColor = colorDialog.Color;
                    _currentSettings.ChromaKeyColor = colorDialog.Color; // 立即更新设置
                    Logger.LogInfo($"抠像颜色已更改为: R={colorDialog.Color.R}, G={colorDialog.Color.G}, B={colorDialog.Color.B}");
                }
            };
            
            chkEnableCloudFallback.CheckedChanged += (s, e) =>
            {
                txtCloudAPIKey.Enabled = chkEnableCloudFallback.Checked;
                txtCloudAPIEndpoint.Enabled = chkEnableCloudFallback.Checked;
            };
        }        private void SaveSettingsFromUI(AppSettings settings)
        {
            // 从UI控件获取设置并保存
            settings.StreamAddress = txtStreamAddress.Text;
            settings.WebSocketServerAddress = txtWebSocketAddress.Text;
            settings.EnableChromaKey = chkEnableChromaKey.Checked;
            settings.ChromaKeyColor = btnChromaKeyColor.BackColor;
            settings.ChromaKeyTolerance = (int)numChromaKeyTolerance.Value;
            settings.TopMostEnabled = chkTopMost.Checked;
            settings.ClickThroughEnabled = chkClickThrough.Checked;
            settings.WindowWidth = (int)numWidth.Value;
            settings.WindowHeight = (int)numHeight.Value;
            settings.TtsVoiceName = cboVoices.Text;
            settings.TtsRate = trkRate.Value;
            settings.TtsVolume = trkVolume.Value;
            settings.ModelPath = txtModelPath.Text;
            settings.KnowledgeBasePath = txtKnowledgeBasePath.Text;
            settings.UseGPU = chkUseGPU.Checked;
            settings.EnableCloudFallback = chkEnableCloudFallback.Checked;
            settings.CloudAPIKey = txtCloudAPIKey.Text;
            settings.CloudAPIEndpoint = txtCloudAPIEndpoint.Text;
        }private void LoadSettingsToUI(AppSettings settings)
        {
            // 将设置加载到UI控件
            txtStreamAddress.Text = settings.StreamAddress;
            txtWebSocketAddress.Text = settings.WebSocketServerAddress;
            chkEnableChromaKey.Checked = settings.EnableChromaKey;
            btnChromaKeyColor.BackColor = settings.ChromaKeyColor;
            numChromaKeyTolerance.Value = settings.ChromaKeyTolerance;
            btnChromaKeyColor.Enabled = !settings.UsePreprocessedStream;
            numChromaKeyTolerance.Enabled = !settings.UsePreprocessedStream;
            chkEnableChromaKey.Enabled = !settings.UsePreprocessedStream;
            chkTopMost.Checked = settings.TopMostEnabled;
            chkClickThrough.Checked = settings.ClickThroughEnabled;
            numWidth.Value = settings.WindowWidth;
            numHeight.Value = settings.WindowHeight;
            cboVoices.Text = settings.TtsVoiceName;
            trkRate.Value = settings.TtsRate;
            trkVolume.Value = settings.TtsVolume;
            txtModelPath.Text = settings.ModelPath;
            txtKnowledgeBasePath.Text = settings.KnowledgeBasePath;
            chkUseGPU.Checked = settings.UseGPU;
            chkEnableCloudFallback.Checked = settings.EnableCloudFallback;
            txtCloudAPIKey.Text = settings.CloudAPIKey ?? string.Empty;
            txtCloudAPIEndpoint.Text = settings.CloudAPIEndpoint ?? string.Empty;
        }

        private void BtnSave_Click(object? sender, EventArgs e)
        {
            SaveSettingsFromUI(_currentSettings);
            // 触发设置更改事件
            Logger.LogInfo("保存设置并触发SettingsChanged事件");
            SettingsChanged?.Invoke(this, new SettingsChangedEventArgs(_currentSettings));
            MessageBox.Show("设置已保存！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            Logger.LogInfo("保存后隐藏设置窗口");
            this.Hide(); // 保存后隐藏窗口
            
            // 确保主窗体重新获得焦点
            if (Owner != null)
            {
                Logger.LogInfo("主窗体重新获得焦点");
                Owner.Activate();
                Owner.Focus();
                // 确保主窗体可以接收键盘事件
                if (Owner is MainForm mainForm)
                {
                    Logger.LogInfo("重新设置主窗体键盘事件处理");
                    mainForm.ResetKeyboardHandling();
                }
            }
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
        }        private void SettingsForm_FormClosing(object? sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true; // 取消关闭
                this.Hide(); // 改为隐藏
                Logger.LogInfo("设置界面已隐藏");
            }
        }        private void SettingsForm_Activated(object? sender, EventArgs e)
        {
            // 窗口激活时不重新加载设置，避免覆盖用户正在编辑的状态
            Logger.LogInfo("设置界面已激活");
        }

        private void SettingsForm_Deactivate(object? sender, EventArgs e)
        {
            // 窗口失去焦点时的处理
            Logger.LogInfo("设置界面失去焦点");
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

        private void LoadSettings()
        {
            try
            {
                // 加载当前设置到UI控件
                txtStreamAddress.Text = _currentSettings.StreamAddress;
                txtWebSocketAddress.Text = _currentSettings.WebSocketServerAddress;
                
                // 抠像设置
                btnChromaKeyColor.BackColor = _currentSettings.ChromaKeyColor;
                numChromaKeyTolerance.Value = _currentSettings.ChromaKeyTolerance;
                
                // 窗口设置
                chkTopMost.Checked = _currentSettings.TopMostEnabled;
                chkClickThrough.Checked = _currentSettings.ClickThroughEnabled;
                numWidth.Value = _currentSettings.WindowWidth;
                numHeight.Value = _currentSettings.WindowHeight;
                
                // TTS设置
                cboVoices.Text = _currentSettings.TtsVoiceName;
                trkRate.Value = _currentSettings.TtsRate;
                trkVolume.Value = _currentSettings.TtsVolume;

                // AI设置
                txtModelPath.Text = _currentSettings.ModelPath;
                txtKnowledgeBasePath.Text = _currentSettings.KnowledgeBasePath;
                chkUseGPU.Checked = _currentSettings.UseGPU;
                chkEnableCloudFallback.Checked = _currentSettings.EnableCloudFallback;
                txtCloudAPIKey.Text = _currentSettings.CloudAPIKey;
                txtCloudAPIEndpoint.Text = _currentSettings.CloudAPIEndpoint;
            }
            catch (Exception ex)
            {
                Logger.LogError("加载设置时发生错误", ex);
                MessageBox.Show(
                    $"加载设置时发生错误：{ex.Message}",
                    "设置错误",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
        }        private void SettingsForm_VisibleChanged(object? sender, EventArgs e)
        {
            Logger.LogInfo($"设置窗口可见性改变: {this.Visible}");
            if (this.Visible)
            {
                Logger.LogInfo("设置界面已显示");
                this.BringToFront();
                this.Activate();
                // 不重新加载设置，避免覆盖用户正在编辑的状态
            }
            else
            {
                Logger.LogInfo("设置界面已隐藏");
            }
        }
    }
}