using System;
using System.Windows.Forms;
using System.Drawing;

namespace CoreApplication
{
    public partial class AIManagerForm : Form
    {
        private readonly AIService _aiService;
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

        public AIManagerForm(AIService aiService)
        {
            _aiService = aiService;
            InitializeComponent();

            // 订阅事件
            _aiService.OnAIResponse += AIService_OnAIResponse;
            _aiService.OnError += AIService_OnError;
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
                Location = new Point(10, 10),
                Size = new Size(560, 100)
            };

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
                Location = new Point(10, 120),
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
                _grpModelInfo,
                grpPaths,
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

                var config = new AIServiceConfig
                {
                    ModelPath = _txtModelPath?.Text ?? "",
                    KnowledgeBasePath = _txtKnowledgeBasePath?.Text ?? "",
                    UseGPU = _chkUseGPU?.Checked ?? false,
                    EnableCloudFallback = _chkEnableCloudFallback?.Checked ?? false,
                    CloudAPIKey = _txtCloudAPIKey?.Text,
                    CloudAPIEndpoint = _txtCloudEndpoint?.Text
                };

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
                _btnInitialize.Enabled = true;
                _progressLoading.Visible = false;
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
                    
                    // 创建新的知识条目
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

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                this.Hide();
            }
            base.OnFormClosing(e);
        }
    }
}