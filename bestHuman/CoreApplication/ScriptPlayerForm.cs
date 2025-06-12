using System;
using System.Windows.Forms;
using System.Drawing;

namespace CoreApplication
{
    public partial class ScriptPlayerForm : Form
    {
        private readonly ScriptService _scriptService;
        private ProgressBar? _progressBar;
        private Button? _btnPlay;
        private Button? _btnPause;
        private Button? _btnStop;
        private CheckBox? _chkLoop;
        private Label? _lblStatus;

        public ScriptPlayerForm(ScriptService scriptService)
        {
            _scriptService = scriptService;
            _scriptService.OnPlayProgress += ScriptService_OnPlayProgress;
            _scriptService.OnScriptFinished += ScriptService_OnScriptFinished;
            _scriptService.OnError += ScriptService_OnError;
            
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = "脚本播放控制";
            this.Size = new Size(400, 200);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            // 创建进度条
            _progressBar = new ProgressBar
            {
                Location = new Point(10, 20),
                Size = new Size(360, 30),
                Style = ProgressBarStyle.Continuous,
                Minimum = 0,
                Maximum = 100
            };

            // 创建状态标签
            _lblStatus = new Label
            {
                Location = new Point(10, 60),
                Size = new Size(360, 20),
                Text = "就绪",
                TextAlign = ContentAlignment.MiddleLeft
            };

            // 创建控制按钮
            var pnlControls = new Panel
            {
                Location = new Point(10, 90),
                Size = new Size(360, 40)
            };

            _btnPlay = new Button
            {
                Location = new Point(0, 5),
                Size = new Size(80, 30),
                Text = "播放",
                Image = null // TODO: 添加播放图标
            };
            _btnPlay.Click += BtnPlay_Click;

            _btnPause = new Button
            {
                Location = new Point(90, 5),
                Size = new Size(80, 30),
                Text = "暂停",
                Enabled = false,
                Image = null // TODO: 添加暂停图标
            };
            _btnPause.Click += BtnPause_Click;

            _btnStop = new Button
            {
                Location = new Point(180, 5),
                Size = new Size(80, 30),
                Text = "停止",
                Enabled = false,
                Image = null // TODO: 添加停止图标
            };
            _btnStop.Click += BtnStop_Click;

            _chkLoop = new CheckBox
            {
                Location = new Point(270, 10),
                Size = new Size(80, 20),
                Text = "循环播放"
            };
            _chkLoop.CheckedChanged += ChkLoop_CheckedChanged;

            pnlControls.Controls.AddRange(new Control[] { _btnPlay, _btnPause, _btnStop, _chkLoop });

            // 添加所有控件到窗体
            this.Controls.AddRange(new Control[] { _progressBar, _lblStatus, pnlControls });

            // 注册窗体关闭事件
            this.FormClosing += ScriptPlayerForm_FormClosing;
        }

        private void BtnPlay_Click(object? sender, EventArgs e)
        {
            try
            {
                _scriptService.PlayScript();
                UpdateControlsState(isPlaying: true);
                _lblStatus!.Text = "正在播放...";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"播放失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnPause_Click(object? sender, EventArgs e)
        {
            try
            {
                _scriptService.PauseScript();
                UpdateControlsState(isPlaying: false, isPaused: true);
                _lblStatus!.Text = "已暂停";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"暂停失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnStop_Click(object? sender, EventArgs e)
        {
            try
            {
                _scriptService.StopScript();
                UpdateControlsState(isPlaying: false);
                _progressBar!.Value = 0;
                _lblStatus!.Text = "就绪";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"停止失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ChkLoop_CheckedChanged(object? sender, EventArgs e)
        {
            if (sender is CheckBox checkBox)
            {
                _scriptService.SetLoopEnabled(checkBox.Checked);
            }
        }

        private void UpdateControlsState(bool isPlaying, bool isPaused = false)
        {
            if (_btnPlay != null) _btnPlay.Enabled = !isPlaying || isPaused;
            if (_btnPause != null) _btnPause.Enabled = isPlaying && !isPaused;
            if (_btnStop != null) _btnStop.Enabled = isPlaying || isPaused;
        }

        private void ScriptService_OnPlayProgress(object? sender, float progress)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => ScriptService_OnPlayProgress(sender, progress)));
                return;
            }

            _progressBar!.Value = (int)(progress * 100);
        }

        private void ScriptService_OnScriptFinished(object? sender, EventArgs e)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => ScriptService_OnScriptFinished(sender, e)));
                return;
            }

            UpdateControlsState(isPlaying: false);
            _progressBar!.Value = 0;
            _lblStatus!.Text = "播放完成";
        }

        private void ScriptService_OnError(object? sender, string error)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => ScriptService_OnError(sender, error)));
                return;
            }

            MessageBox.Show(error, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            UpdateControlsState(isPlaying: false);
            _lblStatus!.Text = "发生错误";
        }

        private void ScriptPlayerForm_FormClosing(object? sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                this.Hide();
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _scriptService.OnPlayProgress -= ScriptService_OnPlayProgress;
                _scriptService.OnScriptFinished -= ScriptService_OnScriptFinished;
                _scriptService.OnError -= ScriptService_OnError;
            }
            base.Dispose(disposing);
        }
    }
}