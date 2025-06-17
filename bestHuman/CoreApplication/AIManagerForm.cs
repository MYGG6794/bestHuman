using System;
using System.Windows.Forms;
using System.Drawing;
using System.Threading.Tasks; // Added for async operations

namespace CoreApplication
{
    public partial class AIManagerForm : Form
    {        private readonly AIService _aiService;
        private readonly SettingsForm.AppSettings _appSettings; // Store app settings
        private DigitalHumanDisplay _digitalHumanDisplayControl = null!; // Initialized in constructor path
        // 暂时注释掉原生窗口引用，避免编译错误
        // private NativeLayeredWindow? _nativeLayeredWindowInstance = null; // Instance of the native window if used

        private TextBox? _txtModelPath;
        private TextBox? _txtKnowledgeBasePath;
        private CheckBox? _chkUseGPU;
        private CheckBox? _chkEnableCloudFallback;
        private TextBox? _txtCloudAPIKey;
        private TextBox? _txtCloudEndpoint;
        private Button? _btnInitialize;
        private Button? _btnImportKnowledge;
        private RichTextBox? _txtKnowledgePreview;
        private Label? _lblStatus;
        private ProgressBar? _progressLoading;
        private GroupBox? _grpModelInfo;
        private Label? _lblModelName;
        private Label? _lblVocabSize;
        private Label? _lblInputShape;

        // Constructor updated to accept AppSettings
        public AIManagerForm(AIService aiService, SettingsForm.AppSettings appSettings)
        {
            _aiService = aiService;
            _appSettings = appSettings; // Store the settings

            InitializeComponent(); // Initialize standard components
            // _digitalHumanDisplayControl will be initialized by InitializeDigitalHumanDisplay
            InitializeDigitalHumanDisplaySyncPart(); 
            // Then call the async part without awaiting in constructor, or make constructor async if appropriate.
            // For simplicity, let's assume InitializeDigitalHumanDisplay handles its async parts internally or via Form_Load.
            // If InitializeDigitalHumanDisplay must be fully async and awaited, AIManagerForm creation needs to be async.
            // Alternative: Call the async part in Form_Load or a similar event.
            this.Load += async (s, e) => await InitializeDigitalHumanDisplayAsyncPart();

            // Subscribe to events
            _aiService.OnAIResponse += AIService_OnAIResponse;
            _aiService.OnError += AIService_OnError;
        }

        private void InitializeDigitalHumanDisplaySyncPart()
        {
            _digitalHumanDisplayControl = new DigitalHumanDisplay();
        }

        private async Task InitializeDigitalHumanDisplayAsyncPart()
        {
            // ApplyDisplayMode will handle whether to use WebView or Native Window
            await _digitalHumanDisplayControl.ApplyDisplayMode(_appSettings.UseNativeLayeredWindow, _appSettings);            // 暂时注释掉原生窗口逻辑，因为现在由 Program.cs 主窗口处理
            /*
            if (_appSettings.UseNativeLayeredWindow)
            {
                _nativeLayeredWindowInstance = _digitalHumanDisplayControl.GetNativeWindow();
                if (_nativeLayeredWindowInstance != null)
                {
                    _nativeLayeredWindowInstance.Size = new Size(_appSettings.WindowWidth, _appSettings.WindowHeight);
                    if (_appSettings.WindowX != -1 && _appSettings.WindowY != -1)
                    {
                        _nativeLayeredWindowInstance.Location = new Point(_appSettings.WindowX, _appSettings.WindowY);
                    }
                    else
                    {
                        _nativeLayeredWindowInstance.StartPosition = FormStartPosition.CenterScreen;
                    }
                    _nativeLayeredWindowInstance.TopMost = _appSettings.TopMostEnabled;
                    _nativeLayeredWindowInstance.Show();
                    _nativeLayeredWindowInstance.FormClosed += (s, e) => { _nativeLayeredWindowInstance = null; }; 
                    Logger.LogInfo("NativeLayeredWindow shown and configured.");
                }
                else
                {
                    Logger.LogError("Failed to get NativeLayeredWindow instance from DigitalHumanDisplay.");
                    MessageBox.Show("无法初始化原生透明窗口。将回退到 WebView2 模式。", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    _appSettings.UseNativeLayeredWindow = false;
                    await _digitalHumanDisplayControl.ApplyDisplayMode(false, _appSettings);
                    InitializeWebViewDisplay(); 
                }
            }
            */            if (!_appSettings.UseNativeLayeredWindow)
            {
                InitializeWebViewDisplay();
            }
        }

        private void InitializeWebViewDisplay()
        {
            // Setup for WebView2 display (if _digitalHumanDisplayControl is part of this form's UI)
            // This assumes _digitalHumanDisplayControl is a UserControl placed on AIManagerForm.
            // If AIManagerForm is just a controller, this part might be different.
            // _digitalHumanDisplayControl.Dock = DockStyle.Fill; // Example: if it fills a panel
            // Add _digitalHumanDisplayControl to a panel or directly to the form if it's meant to be visible here.
            // For example: this.pnlDisplayArea.Controls.Add(_digitalHumanDisplayControl);
            
            // If AIManagerForm is *itself* the main window that shows the human, then its size etc. should be set.
            // This form (AIManagerForm) is for AI *management*, not the display itself.
            // The DigitalHumanDisplay control or NativeLayeredWindow should be hosted by the *main application form* (e.g., Program.cs might create a main form).
            // For now, we assume AIManagerForm is NOT the main display window.
            // If it IS, then the following lines would apply to `this` (AIManagerForm).
            /*
            this.Size = new Size(_appSettings.WindowWidth, _appSettings.WindowHeight);
            if (_appSettings.WindowX != -1 && _appSettings.WindowY != -1)
            {
                this.Location = new Point(_appSettings.WindowX, _appSettings.WindowY);
            }
            this.TopMost = _appSettings.TopMostEnabled;
            */
            Logger.LogInfo("WebView2 display mode active. Display control is ready for hosting.");
        }        // Example method to simulate receiving video frames for native window
        public void ProcessNewVideoFrame(Bitmap frame)
        {
            // 暂时注释掉原生窗口帧更新逻辑
            /*
            if (_appSettings.UseNativeLayeredWindow && _nativeLayeredWindowInstance != null && _nativeLayeredWindowInstance.IsHandleCreated)
            {
                _nativeLayeredWindowInstance.UpdateFrame(frame);
            }
            else if (!_appSettings.UseNativeLayeredWindow && _digitalHumanDisplayControl != null)
            {
                // If WebView2 mode needs frame updates (e.g., local video source not URL)
                // _digitalHumanDisplayControl.UpdateWebViewFrame(frame); // Hypothetical method
            }
            */
            
            if (!_appSettings.UseNativeLayeredWindow && _digitalHumanDisplayControl != null)
            {
                // If WebView2 mode needs frame updates (e.g., local video source not URL)
                // _digitalHumanDisplayControl.UpdateWebViewFrame(frame); // Hypothetical method
            }
        }

        private void InitializeComponent()
        {
            this.Text = "AI 管理";
            this.Size = new Size(600, 500);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;

            // 模型信息组
            _grpModelInfo = new GroupBox
            {
                Text = "模型信息",
                Location = new Point(10, 10),
                Size = new Size(560, 100),
                Visible = false
            };

            _lblModelName = new Label
            {
                Location = new Point(10, 25),
                AutoSize = true
            };

            _lblVocabSize = new Label
            {
                Location = new Point(10, 45),
                AutoSize = true
            };

            _lblInputShape = new Label
            {
                Location = new Point(10, 65),
                AutoSize = true
            };

            _grpModelInfo.Controls.AddRange(new Control[]
            {
                _lblModelName,
                _lblVocabSize,
                _lblInputShape
            });

            var grpPaths = new GroupBox
            {
                Text = "路径配置",
                Location = new Point(10, 10), // Adjusted: This will overlap with _grpModelInfo if visible
                Size = new Size(560, 100)
            };
            // If _grpModelInfo is shown, grpPaths should be placed below it, e.g., new Point(10, 120)

            var lblModelPath = new Label
            {
                Text = "模型路径:",
                Location = new Point(10, 25),
                AutoSize = true
            };
            _txtModelPath = new TextBox
            {
                Location = new Point(80, 22),
                Width = 400
            };
            var btnBrowseModel = new Button
            {
                Text = "浏览...",
                Location = new Point(490, 20),
                Width = 60
            };
            btnBrowseModel.Click += BtnBrowseModel_Click;

            var lblKbPath = new Label
            {
                Text = "知识库:",
                Location = new Point(10, 55),
                AutoSize = true
            };
            _txtKnowledgeBasePath = new TextBox
            {
                Location = new Point(80, 52),
                Width = 400
            };
            var btnBrowseKb = new Button
            {
                Text = "浏览...",
                Location = new Point(490, 50),
                Width = 60
            };
            btnBrowseKb.Click += BtnBrowseKb_Click;

            grpPaths.Controls.AddRange(new Control[]
            {
                lblModelPath, _txtModelPath, btnBrowseModel,
                lblKbPath, _txtKnowledgeBasePath, btnBrowseKb
            });

            var grpSettings = new GroupBox
            {
                Text = "设置",
                Location = new Point(10, 120), // Adjusted: Assumes grpPaths is at (10,10)
                Size = new Size(560, 100)
            };

            _chkUseGPU = new CheckBox
            {
                Text = "启用GPU加速",
                Location = new Point(10, 25),
                AutoSize = true
            };

            _chkEnableCloudFallback = new CheckBox
            {
                Text = "启用云端回退",
                Location = new Point(10, 50),
                AutoSize = true
            };
            _chkEnableCloudFallback.CheckedChanged += ChkEnableCloudFallback_CheckedChanged;

            _txtCloudAPIKey = new TextBox
            {
                Location = new Point(200, 25),
                Width = 340,
                Enabled = false,
                PasswordChar = '*'
            };

            _txtCloudEndpoint = new TextBox
            {
                Location = new Point(200, 52),
                Width = 340,
                Enabled = false
            };

            var lblCloudKey = new Label
            {
                Text = "API密钥:",
                Location = new Point(140, 28),
                AutoSize = true
            };

            var lblCloudEndpoint = new Label
            {
                Text = "API地址:",
                Location = new Point(140, 55),
                AutoSize = true
            };

            grpSettings.Controls.AddRange(new Control[]
            {
                _chkUseGPU,
                _chkEnableCloudFallback,
                lblCloudKey, _txtCloudAPIKey,
                lblCloudEndpoint, _txtCloudEndpoint
            });

            _btnInitialize = new Button
            {
                Text = "初始化AI服务",
                Location = new Point(10, 230),
                Width = 120,
                Height = 30
            };
            _btnInitialize.Click += BtnInitialize_Click;

            _btnImportKnowledge = new Button
            {
                Text = "导入知识",
                Location = new Point(140, 230),
                Width = 120,
                Height = 30
            };
            _btnImportKnowledge.Click += BtnImportKnowledge_Click;

            _progressLoading = new ProgressBar
            {
                Location = new Point(270, 230),
                Width = 300,
                Height = 30,
                Style = ProgressBarStyle.Marquee,
                Visible = false
            };

            _lblStatus = new Label
            {
                Location = new Point(10, 270),
                Size = new Size(560, 20),
                TextAlign = ContentAlignment.MiddleLeft
            };

            _txtKnowledgePreview = new RichTextBox
            {
                Location = new Point(10, 300),
                Size = new Size(560, 150),
                ReadOnly = true
            };

            this.Controls.AddRange(new Control[]
            {
                _grpModelInfo, // This should be added first if it's at the top
                grpPaths,      // Then this one
                grpSettings,
                _btnInitialize,
                _btnImportKnowledge,
                _progressLoading,
                _lblStatus,
                _txtKnowledgePreview
            });
        }

        private void ChkEnableCloudFallback_CheckedChanged(object? sender, EventArgs e)
        {
            if (_txtCloudAPIKey != null && _txtCloudEndpoint != null && sender is CheckBox checkBox)
            {
                _txtCloudAPIKey.Enabled = checkBox.Checked;
                _txtCloudEndpoint.Enabled = checkBox.Checked;
            }
        }

        private void BtnBrowseModel_Click(object? sender, EventArgs e)
        {
            using var dialog = new OpenFileDialog
            {
                Filter = "ONNX模型文件 (*.onnx)|*.onnx|所有文件 (*.*)|*.*",
                Title = "选择模型文件"
            };

            if (dialog.ShowDialog() == DialogResult.OK && _txtModelPath != null)
            {
                _txtModelPath.Text = dialog.FileName;
            }
        }

        private void BtnBrowseKb_Click(object? sender, EventArgs e)
        {
            using var dialog = new OpenFileDialog
            {
                Filter = "JSON文件 (*.json)|*.json|所有文件 (*.*)|*.*",
                Title = "选择知识库文件"
            };

            if (dialog.ShowDialog() == DialogResult.OK && _txtKnowledgeBasePath != null)
            {
                _txtKnowledgeBasePath.Text = dialog.FileName;
            }
        }

        private async void BtnInitialize_Click(object? sender, EventArgs e)
        {
            if (_btnInitialize == null || _progressLoading == null || _lblStatus == null)
                return;

            try
            {
                _btnInitialize.Enabled = false;
                _progressLoading.Visible = true;
                _lblStatus.Text = "正在初始化AI服务...";

                // AIService constructor now takes the config, and InitializeAsync takes no parameters.
                // We assume _aiService is already constructed with the initial or default config.
                // If config can change and re-initialization is needed, AIService might need a ReinitializeAsync(newConfig) method,
                // or be disposed and recreated.
                // For now, let's assume the config passed to AIManagerForm's constructor was used for AIService construction.
                // And if these UI elements are for *changing* that config, then AIService needs a way to update.

                // Option 1: If AIService's config is mutable and it has a method to apply new settings from UI
                // _aiService.UpdateConfiguration(new AIServiceConfig { ... }); 
                // await _aiService.InitializeAsync();

                // Option 2: If AIService must be recreated with new config (more robust for some changes like model path)
                // This would mean _aiService in this form needs to be replaceable, or this form shouldn't manage _aiService directly.
                // For simplicity, let's assume InitializeAsync re-reads its existing _config if called again,
                // or that the UI here is for the *initial* setup and matches the config AIService was created with.
                // If the UI is for changing settings, the Program.cs or main controller would need to handle recreating AIService
                // and potentially AIManagerForm if it depends on a specific AIService instance.

                // Corrected call: InitializeAsync should not take parameters if it uses the config it was constructed with.
                await _aiService.InitializeAsync(); 

                // 显示模型信息
                var modelInfo = _aiService.GetModelInfo();
                if (modelInfo != null &&
                    _lblModelName != null &&
                    _lblVocabSize != null &&
                    _lblInputShape != null &&
                    _grpModelInfo != null)
                {
                    _lblModelName.Text = $"模型名称：{modelInfo.Name}";
                    _lblVocabSize.Text = $"词汇表大小：{modelInfo.TokenizerVocabSize:N0}";
                    _lblInputShape.Text = $"输入形状：{string.Join(", ", modelInfo.InputShapes)}";
                    _grpModelInfo.Visible = true;
                    // Adjust layout if _grpModelInfo becomes visible
                    // grpPaths.Location = new Point(10, _grpModelInfo.Bottom + 5);
                    // grpSettings.Location = new Point(10, grpPaths.Bottom + 5);
                    // etc. for other controls, or use a FlowLayoutPanel/TableLayoutPanel
                }

                _lblStatus.Text = "AI服务初始化成功！";
            }
            catch (Exception ex)
            {
                _lblStatus.Text = $"初始化失败：{ex.Message}";
                MessageBox.Show(ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                if (_btnInitialize != null) _btnInitialize.Enabled = true;
                if (_progressLoading != null) _progressLoading.Visible = false;
            }
        }

        private async void BtnImportKnowledge_Click(object? sender, EventArgs e)
        {
            using var dialog = new OpenFileDialog
            {
                Filter = "文本文件 (*.txt)|*.txt|Markdown文件 (*.md)|*.md|所有文件 (*.*)|*.*",
                Title = "选择要导入的知识文件"
            };

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    string content = await System.IO.File.ReadAllTextAsync(dialog.FileName);
                    
                    var entry = new KnowledgeEntry
                    {
                        Question = System.IO.Path.GetFileNameWithoutExtension(dialog.FileName),
                        Answer = content,
                        UpdatedAt = DateTime.Now
                    };

                    await _aiService.UpdateKnowledgeBaseAsync(_txtKnowledgeBasePath?.Text ?? "", entry);
                    
                    if (_txtKnowledgePreview != null)
                    {
                        _txtKnowledgePreview.AppendText($"已导入知识：{entry.Question}\n");
                    }

                    MessageBox.Show("知识导入成功！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"导入失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void AIService_OnAIResponse(object? sender, string response)
        {
            if (_txtKnowledgePreview?.InvokeRequired ?? false)
            {
                _txtKnowledgePreview?.Invoke(new Action(() => AIService_OnAIResponse(sender, response)));
                return;
            }

            _txtKnowledgePreview?.AppendText($"AI响应: {response}\n");
        }

        private void AIService_OnError(object? sender, string error)
        {
            if (_lblStatus?.InvokeRequired ?? false)
            {
                _lblStatus?.Invoke(new Action(() => AIService_OnError(sender, error)));
                return;
            }

            if (_lblStatus != null)
            {
                _lblStatus.Text = $"错误: {error}";
            }
        }


        protected override void OnFormClosing(FormClosingEventArgs e)        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                this.Hide();
                // _nativeLayeredWindowInstance?.Hide(); // Also hide native window if it exists
            }
            base.OnFormClosing(e);
        }

        // Ensure to dispose the native window when AIManagerForm is disposed
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                // _nativeLayeredWindowInstance?.Close(); // Close will trigger its Dispose
                // _nativeLayeredWindowInstance?.Dispose();
                _digitalHumanDisplayControl?.Dispose();
                // Unsubscribe from events
                if (_aiService != null)
                {
                    _aiService.OnAIResponse -= AIService_OnAIResponse;
                    _aiService.OnError -= AIService_OnError;
                }
            }
            base.Dispose(disposing);
        }
    }
}