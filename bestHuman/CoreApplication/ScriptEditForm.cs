using System;
using System.Windows.Forms;
using System.Drawing;
using System.Collections.Generic;
using System.Text.Json;

namespace CoreApplication
{
    public partial class ScriptEditForm : Form
    {
        private readonly ScriptService _scriptService;
        private Script _currentScript;
        private int _selectedSegmentIndex = -1;

        public ScriptEditForm(ScriptService scriptService)
        {
            _scriptService = scriptService;
            _currentScript = new Script { Id = Guid.NewGuid().ToString() };
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = "讲解词编辑器";
            this.Size = new Size(800, 600);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.MinimizeBox = false;

            // 基本信息部分
            var grpBasicInfo = new GroupBox
            {
                Text = "基本信息",
                Location = new Point(10, 10),
                Size = new Size(760, 80)
            };

            var lblTitle = new Label { Text = "标题:", Location = new Point(10, 25), AutoSize = true };
            var txtTitle = new TextBox 
            { 
                Name = "txtTitle",
                Location = new Point(60, 22),
                Width = 680
            };
            txtTitle.TextChanged += (s, e) => _currentScript.Title = txtTitle.Text;

            grpBasicInfo.Controls.AddRange(new Control[] { lblTitle, txtTitle });

            // 段落列表部分
            var grpSegments = new GroupBox
            {
                Text = "讲解段落",
                Location = new Point(10, 100),
                Size = new Size(760, 400)
            };

            var lstSegments = new ListBox
            {
                Name = "lstSegments",
                Location = new Point(10, 20),
                Size = new Size(200, 370),
                DisplayMember = "Text"
            };
            lstSegments.SelectedIndexChanged += LstSegments_SelectedIndexChanged;

            // 段落编辑部分
            var lblText = new Label { Text = "讲解文本:", Location = new Point(220, 20), AutoSize = true };
            var txtText = new TextBox 
            {
                Name = "txtText",
                Location = new Point(220, 40),
                Size = new Size(520, 100),
                Multiline = true,
                ScrollBars = ScrollBars.Vertical
            };
            txtText.TextChanged += TxtText_TextChanged;

            // 动作编辑部分
            var lblActions = new Label { Text = "动作列表:", Location = new Point(220, 150), AutoSize = true };
            var lstActions = new ListView
            {
                Name = "lstActions",
                Location = new Point(220, 170),
                Size = new Size(520, 150),
                View = View.Details,
                FullRowSelect = true
            };
            lstActions.Columns.AddRange(new[]
            {
                new ColumnHeader { Text = "动作ID", Width = 150 },
                new ColumnHeader { Text = "持续时间", Width = 100 },
                new ColumnHeader { Text = "参数", Width = 250 }
            });

            // 语音参数部分
            var grpVoice = new GroupBox
            {
                Text = "语音参数",
                Location = new Point(220, 330),
                Size = new Size(520, 60)
            };

            var lblRate = new Label { Text = "语速:", Location = new Point(10, 25), AutoSize = true };
            var numRate = new NumericUpDown
            {
                Name = "numRate",
                Location = new Point(50, 23),
                Width = 60,
                Minimum = -10,
                Maximum = 10
            };
            numRate.ValueChanged += NumRate_ValueChanged;

            var lblVolume = new Label { Text = "音量:", Location = new Point(130, 25), AutoSize = true };
            var numVolume = new NumericUpDown
            {
                Name = "numVolume",
                Location = new Point(170, 23),
                Width = 60,
                Minimum = 0,
                Maximum = 100,
                Value = 100
            };
            numVolume.ValueChanged += NumVolume_ValueChanged;

            var lblVoice = new Label { Text = "音色:", Location = new Point(250, 25), AutoSize = true };
            var cmbVoice = new ComboBox
            {
                Name = "cmbVoice",
                Location = new Point(290, 22),
                Width = 200,
                DropDownStyle = ComboBoxStyle.DropDownList
            };

            grpVoice.Controls.AddRange(new Control[]
            {
                lblRate, numRate,
                lblVolume, numVolume,
                lblVoice, cmbVoice
            });

            // 按钮区域
            var btnAddSegment = new Button
            {
                Text = "添加段落",
                Location = new Point(10, 20),
                Size = new Size(80, 30)
            };
            btnAddSegment.Click += BtnAddSegment_Click;

            var btnDeleteSegment = new Button
            {
                Text = "删除段落",
                Location = new Point(100, 20),
                Size = new Size(80, 30)
            };
            btnDeleteSegment.Click += BtnDeleteSegment_Click;

            var btnAddAction = new Button
            {
                Text = "添加动作",
                Location = new Point(220, 20),
                Size = new Size(80, 30)
            };
            btnAddAction.Click += BtnAddAction_Click;

            var btnDeleteAction = new Button
            {
                Text = "删除动作",
                Location = new Point(310, 20),
                Size = new Size(80, 30)
            };
            btnDeleteAction.Click += BtnDeleteAction_Click;

            var btnSave = new Button
            {
                Text = "保存脚本",
                Location = new Point(660, 20),
                Size = new Size(80, 30)
            };
            btnSave.Click += BtnSave_Click;

            var pnlButtons = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 60
            };
            pnlButtons.Controls.AddRange(new Control[]
            {
                btnAddSegment, btnDeleteSegment,
                btnAddAction, btnDeleteAction,
                btnSave
            });

            grpSegments.Controls.AddRange(new Control[]
            {
                lstSegments, lblText, txtText,
                lblActions, lstActions, grpVoice
            });

            this.Controls.AddRange(new Control[]
            {
                grpBasicInfo,
                grpSegments,
                pnlButtons
            });

            // 初始化语音列表
            using (var speechService = new SpeechService())
            {
                var voices = speechService.GetAvailableVoices();
                foreach (var voice in voices)
                {
                    cmbVoice.Items.Add(voice.Name);
                }
                if (cmbVoice.Items.Count > 0)
                {
                    cmbVoice.SelectedIndex = 0;
                }
            }

            RefreshSegmentsList();
        }

        private void RefreshSegmentsList()
        {
            var lstSegments = (ListBox)Controls.Find("lstSegments", true)[0];
            lstSegments.Items.Clear();
            foreach (var segment in _currentScript.Segments)
            {
                lstSegments.Items.Add(segment);
            }
        }

        private void RefreshActionsList(ScriptSegment segment)
        {
            var lstActions = (ListView)Controls.Find("lstActions", true)[0];
            lstActions.Items.Clear();
            foreach (var action in segment.Actions)
            {
                var item = new ListViewItem(action.ActionId);
                item.SubItems.Add(action.Duration.ToString());
                item.SubItems.Add(action.Parameters != null ? 
                    JsonSerializer.Serialize(action.Parameters) : "");
                item.Tag = action;
                lstActions.Items.Add(item);
            }
        }

        private void LstSegments_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (sender is ListBox lstSegments)
            {
                _selectedSegmentIndex = lstSegments.SelectedIndex;
                if (_selectedSegmentIndex >= 0 && _selectedSegmentIndex < _currentScript.Segments.Count)
                {
                    var segment = _currentScript.Segments[_selectedSegmentIndex];
                    var txtText = (TextBox)Controls.Find("txtText", true)[0];
                    var numRate = (NumericUpDown)Controls.Find("numRate", true)[0];
                    var numVolume = (NumericUpDown)Controls.Find("numVolume", true)[0];
                    var cmbVoice = (ComboBox)Controls.Find("cmbVoice", true)[0];

                    txtText.Text = segment.Text;
                    numRate.Value = segment.SpeechRate ?? 0;
                    numVolume.Value = segment.SpeechVolume ?? 100;
                    if (!string.IsNullOrEmpty(segment.VoiceName))
                    {
                        int index = cmbVoice.Items.IndexOf(segment.VoiceName);
                        if (index >= 0)
                        {
                            cmbVoice.SelectedIndex = index;
                        }
                    }

                    RefreshActionsList(segment);
                }
            }
        }

        private void TxtText_TextChanged(object? sender, EventArgs e)
        {
            if (_selectedSegmentIndex >= 0 && sender is TextBox txtText)
            {
                _currentScript.Segments[_selectedSegmentIndex].Text = txtText.Text;
                RefreshSegmentsList();
            }
        }

        private void NumRate_ValueChanged(object? sender, EventArgs e)
        {
            if (_selectedSegmentIndex >= 0 && sender is NumericUpDown numRate)
            {
                _currentScript.Segments[_selectedSegmentIndex].SpeechRate = (int)numRate.Value;
            }
        }

        private void NumVolume_ValueChanged(object? sender, EventArgs e)
        {
            if (_selectedSegmentIndex >= 0 && sender is NumericUpDown numVolume)
            {
                _currentScript.Segments[_selectedSegmentIndex].SpeechVolume = (int)numVolume.Value;
            }
        }

        private void BtnAddSegment_Click(object? sender, EventArgs e)
        {
            var segment = new ScriptSegment
            {
                Text = "新讲解段落",
                Actions = new List<DigitalHumanAction>()
            };
            _currentScript.Segments.Add(segment);
            RefreshSegmentsList();
        }

        private void BtnDeleteSegment_Click(object? sender, EventArgs e)
        {
            if (_selectedSegmentIndex >= 0)
            {
                _currentScript.Segments.RemoveAt(_selectedSegmentIndex);
                _selectedSegmentIndex = -1;
                RefreshSegmentsList();
            }
        }

        private void BtnAddAction_Click(object? sender, EventArgs e)
        {
            if (_selectedSegmentIndex >= 0)
            {
                using (var dialog = new ActionEditDialog())
                {
                    if (dialog.ShowDialog() == DialogResult.OK)
                    {
                        var action = dialog.Action;
                        _currentScript.Segments[_selectedSegmentIndex].Actions.Add(action);
                        RefreshActionsList(_currentScript.Segments[_selectedSegmentIndex]);
                    }
                }
            }
        }

        private void BtnDeleteAction_Click(object? sender, EventArgs e)
        {
            if (_selectedSegmentIndex >= 0)
            {
                var lstActions = (ListView)Controls.Find("lstActions", true)[0];
                if (lstActions.SelectedItems.Count > 0)
                {
                    if (lstActions.SelectedItems[0].Tag is DigitalHumanAction action)
                    {
                        _currentScript.Segments[_selectedSegmentIndex].Actions.Remove(action);
                    }
                    RefreshActionsList(_currentScript.Segments[_selectedSegmentIndex]);
                }
            }
        }

        private async void BtnSave_Click(object? sender, EventArgs e)
        {
            using (var dialog = new SaveFileDialog())
            {
                dialog.Filter = "JSON Files (*.json)|*.json|All Files (*.*)|*.*";
                dialog.DefaultExt = "json";
                dialog.AddExtension = true;

                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    await _scriptService.SaveScriptAsync(dialog.FileName, _currentScript);
                    MessageBox.Show("脚本保存成功！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }
    }

    /// <summary>
    /// 动作编辑对话框
    /// </summary>
    public class ActionEditDialog : Form
    {
        public DigitalHumanAction Action { get; private set; }

        public ActionEditDialog()
        {
            Action = new DigitalHumanAction();
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = "编辑动作";
            this.Size = new Size(400, 300);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            var lblActionId = new Label { Text = "动作ID:", Location = new Point(10, 20), AutoSize = true };
            var txtActionId = new TextBox { Location = new Point(80, 17), Width = 280 };

            var lblDuration = new Label { Text = "持续时间:", Location = new Point(10, 50), AutoSize = true };
            var numDuration = new NumericUpDown
            {
                Location = new Point(80, 48),
                Width = 100,
                DecimalPlaces = 1,
                Minimum = 0,
                Maximum = 100,
                Value = 1
            };

            var lblParams = new Label { Text = "参数(JSON):", Location = new Point(10, 80), AutoSize = true };
            var txtParams = new TextBox
            {
                Location = new Point(10, 100),
                Size = new Size(360, 100),
                Multiline = true,
                ScrollBars = ScrollBars.Vertical
            };

            var btnOK = new Button
            {
                Text = "确定",
                DialogResult = DialogResult.OK,
                Location = new Point(200, 220),
                Size = new Size(80, 30)
            };
            btnOK.Click += (s, e) =>
            {
                Action.ActionId = txtActionId.Text;
                Action.Duration = (float)numDuration.Value;
                try
                {
                    if (!string.IsNullOrWhiteSpace(txtParams.Text))
                    {
                        Action.Parameters = JsonSerializer.Deserialize<Dictionary<string, object>>(txtParams.Text);
                    }
                    this.Close();
                }
                catch (Exception)
                {
                    MessageBox.Show("参数JSON格式无效！", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    this.DialogResult = DialogResult.None;
                }
            };

            var btnCancel = new Button
            {
                Text = "取消",
                DialogResult = DialogResult.Cancel,
                Location = new Point(290, 220),
                Size = new Size(80, 30)
            };

            this.Controls.AddRange(new Control[]
            {
                lblActionId, txtActionId,
                lblDuration, numDuration,
                lblParams, txtParams,
                btnOK, btnCancel
            });
        }
    }
}