using System;
using System.Drawing;
using System.Windows.Forms;

namespace CoreApplication
{
    public partial class ChromaKeyControlForm : Form
    {
        public event EventHandler<ChromaKeyChangedEventArgs>? ChromaKeyChanged;
        
        private DigitalHumanDisplay? _digitalHumanDisplay;
        private System.Windows.Forms.Timer _debounceTimer; // 防抖定时器
        private bool _isUpdatingUI = false; // 防止循环更新
        
        private Button btnColorPicker = null!;
        private TrackBar trkGreenThreshold = null!;
        private TrackBar trkColorTolerance = null!;
        private TrackBar trkMinBrightness = null!;
        private TrackBar trkMaxBrightness = null!;
        private CheckBox chkEnableChromaKey = null!;
        private Label lblPreview = null!;
        private Label lblGreenThreshold = null!;
        private Label lblColorTolerance = null!;
        private Label lblMinBrightness = null!;
        private Label lblMaxBrightness = null!;
        
        public Color ChromaKeyColor { get; private set; } = Color.Lime;
        public int GreenThreshold { get; private set; } = 100;
        public int ColorTolerance { get; private set; } = 50;
        public int MinBrightness { get; private set; } = 50;
        public int MaxBrightness { get; private set; } = 255;
        public bool EnableChromaKey { get; private set; } = true;

        public ChromaKeyControlForm(DigitalHumanDisplay? digitalHumanDisplay = null)
        {
            _digitalHumanDisplay = digitalHumanDisplay;
            
            // 初始化防抖定时器
            _debounceTimer = new System.Windows.Forms.Timer();
            _debounceTimer.Interval = 150; // 减少到150毫秒，提高响应性
            _debounceTimer.Tick += (s, e) => {
                _debounceTimer.Stop();
                ApplyChangesInternal();
            };
            
            InitializeComponent();
            LoadCurrentSettings();
        }

        private void InitializeComponent()
        {
            this.Text = "抠像实时控制";
            this.Size = new Size(400, 550); // 增加高度以容纳新控件
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.TopMost = true;

            // 启用抠像复选框
            chkEnableChromaKey = new CheckBox
            {
                Text = "启用抠像",
                Location = new Point(20, 20),
                Size = new Size(100, 25),
                Checked = EnableChromaKey
            };
            chkEnableChromaKey.CheckedChanged += OnChromaKeyEnabledChanged;

            // 颜色选择器
            var lblColor = new Label
            {
                Text = "抠像颜色:",
                Location = new Point(20, 60),
                Size = new Size(80, 25)
            };

            btnColorPicker = new Button
            {
                Location = new Point(110, 55),
                Size = new Size(100, 30),
                BackColor = ChromaKeyColor,
                Text = "选择颜色"
            };
            btnColorPicker.Click += OnColorPickerClick;

            lblPreview = new Label
            {
                Location = new Point(220, 55),
                Size = new Size(150, 30),
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = ChromaKeyColor,
                Text = "预览"
            };

            // 绿色阈值
            var lblGreenLabel = new Label
            {
                Text = "绿色阈值:",
                Location = new Point(20, 110),
                Size = new Size(80, 25)
            };

            trkGreenThreshold = new TrackBar
            {
                Location = new Point(110, 105),
                Size = new Size(200, 45),
                Minimum = 50,
                Maximum = 200,
                Value = GreenThreshold,
                TickFrequency = 25
            };
            trkGreenThreshold.ValueChanged += OnGreenThresholdChanged;

            lblGreenThreshold = new Label
            {
                Location = new Point(320, 110),
                Size = new Size(50, 25),
                Text = GreenThreshold.ToString()
            };

            // 颜色容差
            var lblToleranceLabel = new Label
            {
                Text = "颜色容差:",
                Location = new Point(20, 160),
                Size = new Size(80, 25)
            };

            trkColorTolerance = new TrackBar
            {
                Location = new Point(110, 155),
                Size = new Size(200, 45),
                Minimum = 0,
                Maximum = 150,
                Value = ColorTolerance,
                TickFrequency = 25
            };
            trkColorTolerance.ValueChanged += OnColorToleranceChanged;

            lblColorTolerance = new Label
            {
                Location = new Point(320, 160),
                Size = new Size(50, 25),
                Text = ColorTolerance.ToString()
            };

            // 最小亮度
            var lblMinBrightnessLabel = new Label
            {
                Text = "最小亮度:",
                Location = new Point(20, 210),
                Size = new Size(80, 25)
            };

            trkMinBrightness = new TrackBar
            {
                Location = new Point(110, 205),
                Size = new Size(200, 45),
                Minimum = 0,
                Maximum = 200,
                Value = MinBrightness,
                TickFrequency = 25
            };
            trkMinBrightness.ValueChanged += OnMinBrightnessChanged;

            lblMinBrightness = new Label
            {
                Location = new Point(320, 210),
                Size = new Size(50, 25),
                Text = MinBrightness.ToString()
            };

            // 最大亮度
            var lblMaxBrightnessLabel = new Label
            {
                Text = "最大亮度:",
                Location = new Point(20, 260),
                Size = new Size(80, 25)
            };

            trkMaxBrightness = new TrackBar
            {
                Location = new Point(110, 255),
                Size = new Size(200, 45),
                Minimum = 100,
                Maximum = 255,
                Value = MaxBrightness,
                TickFrequency = 25
            };
            trkMaxBrightness.ValueChanged += OnMaxBrightnessChanged;

            lblMaxBrightness = new Label
            {
                Location = new Point(320, 260),
                Size = new Size(50, 25),
                Text = MaxBrightness.ToString()
            };

            // 预设按钮
            var btnPresetGreen = new Button
            {
                Text = "标准绿色",
                Location = new Point(20, 320),
                Size = new Size(80, 30)
            };
            btnPresetGreen.Click += (s, e) => SetPreset(Color.Lime, 100, 50, 50, 255);

            var btnPresetBrightGreen = new Button
            {
                Text = "亮绿色",
                Location = new Point(110, 320),
                Size = new Size(80, 30)
            };
            btnPresetBrightGreen.Click += (s, e) => SetPreset(Color.LightGreen, 80, 60, 100, 255);

            var btnPresetDarkGreen = new Button
            {
                Text = "暗绿色",
                Location = new Point(200, 320),
                Size = new Size(80, 30)
            };
            btnPresetDarkGreen.Click += (s, e) => SetPreset(Color.DarkGreen, 120, 40, 20, 150);

            // 重置按钮
            var btnReset = new Button
            {
                Text = "重置默认",
                Location = new Point(290, 320),
                Size = new Size(80, 30)
            };
            btnReset.Click += OnResetClick;

            // 保存按钮
            var btnSave = new Button
            {
                Text = "保存设置",
                Location = new Point(20, 360),
                Size = new Size(80, 30),
                BackColor = Color.LightGreen
            };
            btnSave.Click += OnSaveClick;

            // 实时预览说明
            var lblInfo = new Label
            {
                Text = "✨ 调节参数会实时应用到视频流并自动保存",
                Location = new Point(20, 400),
                Size = new Size(350, 25),
                ForeColor = Color.Gray
            };

            // 状态显示
            var lblStatus = new Label
            {
                Text = "🎯 当前模式：RGB绿色判定 + 容差兜底 (已优化防抖)",
                Location = new Point(20, 430),
                Size = new Size(350, 25),
                ForeColor = Color.Blue
            };

            // 添加控件到窗体
            this.Controls.AddRange(new Control[] {
                chkEnableChromaKey, lblColor, btnColorPicker, lblPreview,
                lblGreenLabel, trkGreenThreshold, lblGreenThreshold,
                lblToleranceLabel, trkColorTolerance, lblColorTolerance,
                lblMinBrightnessLabel, trkMinBrightness, lblMinBrightness,
                lblMaxBrightnessLabel, trkMaxBrightness, lblMaxBrightness,
                btnPresetGreen, btnPresetBrightGreen, btnPresetDarkGreen, btnReset, btnSave,
                lblInfo, lblStatus
            });
        }

        private void LoadCurrentSettings()
        {
            var settings = SettingsForm.AppSettings.Load();
            if (settings != null)
            {
                _isUpdatingUI = true; // 防止循环更新
                
                ChromaKeyColor = settings.ChromaKeyColor;
                ColorTolerance = settings.ChromaKeyTolerance;
                GreenThreshold = settings.ChromaKeyGreenThreshold;
                MinBrightness = settings.ChromaKeyMinBrightness;
                MaxBrightness = settings.ChromaKeyMaxBrightness;
                EnableChromaKey = settings.EnableChromaKey;
                
                // 更新UI控件
                if (btnColorPicker != null)
                {
                    btnColorPicker.BackColor = ChromaKeyColor;
                    lblPreview.BackColor = ChromaKeyColor;
                    chkEnableChromaKey.Checked = EnableChromaKey;
                    
                    trkGreenThreshold.Value = GreenThreshold;
                    trkColorTolerance.Value = ColorTolerance;
                    trkMinBrightness.Value = MinBrightness;
                    trkMaxBrightness.Value = MaxBrightness;
                    
                    lblGreenThreshold.Text = GreenThreshold.ToString();
                    lblColorTolerance.Text = ColorTolerance.ToString();
                    lblMinBrightness.Text = MinBrightness.ToString();
                    lblMaxBrightness.Text = MaxBrightness.ToString();
                }
                
                _isUpdatingUI = false;
            }
        }

        private void OnChromaKeyEnabledChanged(object? sender, EventArgs e)
        {
            EnableChromaKey = chkEnableChromaKey.Checked;
            ApplyChanges();
        }

        private void OnColorPickerClick(object? sender, EventArgs e)
        {
            using var colorDialog = new ColorDialog();
            colorDialog.Color = ChromaKeyColor;
            colorDialog.FullOpen = true;
            
            if (colorDialog.ShowDialog() == DialogResult.OK)
            {
                ChromaKeyColor = colorDialog.Color;
                btnColorPicker.BackColor = ChromaKeyColor;
                lblPreview.BackColor = ChromaKeyColor;
                ApplyChanges();
            }
        }

        private void OnGreenThresholdChanged(object? sender, EventArgs e)
        {
            if (_isUpdatingUI) return;
            GreenThreshold = trkGreenThreshold.Value;
            lblGreenThreshold.Text = GreenThreshold.ToString();
            ApplyChanges();
        }

        private void OnColorToleranceChanged(object? sender, EventArgs e)
        {
            if (_isUpdatingUI) return;
            ColorTolerance = trkColorTolerance.Value;
            lblColorTolerance.Text = ColorTolerance.ToString();
            ApplyChanges();
        }

        private void OnMinBrightnessChanged(object? sender, EventArgs e)
        {
            if (_isUpdatingUI) return;
            MinBrightness = trkMinBrightness.Value;
            lblMinBrightness.Text = MinBrightness.ToString();
            ApplyChanges();
        }

        private void OnMaxBrightnessChanged(object? sender, EventArgs e)
        {
            if (_isUpdatingUI) return;
            MaxBrightness = trkMaxBrightness.Value;
            lblMaxBrightness.Text = MaxBrightness.ToString();
            ApplyChanges();
        }

        private void SetPreset(Color color, int greenThreshold, int tolerance, int minBrightness, int maxBrightness)
        {
            ChromaKeyColor = color;
            GreenThreshold = greenThreshold;
            ColorTolerance = tolerance;
            MinBrightness = minBrightness;
            MaxBrightness = maxBrightness;

            // 更新UI
            btnColorPicker.BackColor = color;
            lblPreview.BackColor = color;
            trkGreenThreshold.Value = greenThreshold;
            trkColorTolerance.Value = tolerance;
            trkMinBrightness.Value = minBrightness;
            trkMaxBrightness.Value = maxBrightness;
            
            lblGreenThreshold.Text = greenThreshold.ToString();
            lblColorTolerance.Text = tolerance.ToString();
            lblMinBrightness.Text = minBrightness.ToString();
            lblMaxBrightness.Text = maxBrightness.ToString();

            ApplyChanges();
        }

        private void OnResetClick(object? sender, EventArgs e)
        {
            SetPreset(Color.Lime, 100, 50, 50, 255);
            chkEnableChromaKey.Checked = true;
            ApplyChanges(); // 立即应用重置的设置
        }

        private void OnSaveClick(object? sender, EventArgs e)
        {
            try
            {
                // 手动触发保存（实际上已经在实时保存了）
                ApplyChanges();
                
                // 显示保存成功提示
                MessageBox.Show(
                    $"抠像参数已保存！\n\n" +
                    $"颜色: RGB({ChromaKeyColor.R}, {ChromaKeyColor.G}, {ChromaKeyColor.B})\n" +
                    $"绿色阈值: {GreenThreshold}\n" +
                    $"容差: {ColorTolerance}\n" +
                    $"亮度范围: {MinBrightness} - {MaxBrightness}\n" +
                    $"启用状态: {(EnableChromaKey ? "是" : "否")}",
                    "保存成功",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information
                );
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"保存设置时出错：{ex.Message}",
                    "保存失败",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
        }

        private void ApplyChanges()
        {
            // 重启防抖定时器
            _debounceTimer.Stop();
            _debounceTimer.Start();
        }

        private void ApplyChangesInternal()
        {
            // 触发事件通知主程序（主程序会处理保存和主窗口更新）
            ChromaKeyChanged?.Invoke(this, new ChromaKeyChangedEventArgs
            {
                ChromaKeyColor = this.ChromaKeyColor,
                GreenThreshold = this.GreenThreshold,
                ColorTolerance = this.ColorTolerance,
                MinBrightness = this.MinBrightness,
                MaxBrightness = this.MaxBrightness,
                EnableChromaKey = this.EnableChromaKey
            });

            // 注意：不在这里直接调用 _digitalHumanDisplay 的方法
            // 避免与主程序的事件处理产生重复调用
            // 主程序的 ChromaKeyControlForm_ChromaKeyChanged 会处理实际的参数应用
        }
    }

    public class ChromaKeyChangedEventArgs : EventArgs
    {
        public Color ChromaKeyColor { get; set; }
        public int GreenThreshold { get; set; }
        public int ColorTolerance { get; set; }
        public int MinBrightness { get; set; }
        public int MaxBrightness { get; set; }
        public bool EnableChromaKey { get; set; }
    }
}
