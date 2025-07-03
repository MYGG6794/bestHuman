using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Web.WebView2.WinForms;
using Microsoft.Web.WebView2.Core;

namespace CoreApplication
{    public partial class DigitalHumanDisplay : UserControl
    {
        private WebView2? _webView = null; // Nullable for conditional initialization
        // private NativeLayeredWindow? _nativeWindow = null; // For native transparency - 暂时注释
        private bool _useNativeWindow = false;

        private bool _isInitialized = false;
        private Color _chromaKeyColor = Color.Green; 
        private int _tolerance = 30; 
        private bool _enableChromaKey = false;
        
        // 防抖机制相关
        private System.Windows.Forms.Timer? _updateTimer;
        private DateTime _lastUpdateTime = DateTime.MinValue;
        private const int UPDATE_DEBOUNCE_MS = 100; // 100毫秒防抖

        // 属性
        [System.ComponentModel.Browsable(false)]
        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public bool EnableChromaKey
        {
            get { return _enableChromaKey; }
            set { _enableChromaKey = value; }
        }

        // 抠像颜色
        [System.ComponentModel.Browsable(false)]
        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public Color ChromaKeyColor
        {
            get { return _chromaKeyColor; }
            set { _chromaKeyColor = value; Invalidate(); }
        }

        // 容差
        [System.ComponentModel.Browsable(false)]
        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public int Tolerance
        {
            get { return _tolerance; }
            set { _tolerance = Math.Max(0, Math.Min(255, value)); }
        }

        public DigitalHumanDisplay()
        {
            InitializeComponent();
            // Initialization of WebView or NativeWindow will be handled by ApplyDisplayMode
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            this.AutoScaleDimensions = new SizeF(7F, 15F);
            this.AutoScaleMode = AutoScaleMode.Font;
            this.Name = "DigitalHumanDisplay";
            this.Size = new Size(800, 600);
            this.ResumeLayout(false);
        }        public async Task ApplyDisplayMode(bool useNative, SettingsForm.AppSettings settings)
        {
            _useNativeWindow = useNative;
            _chromaKeyColor = settings.ChromaKeyColor;
            _enableChromaKey = true; // Chroma key is conceptually always on for transparency
            // Tolerance might be specific to WebView2, NativeLayeredWindow handles its own logic

            this.Controls.Clear(); // Clear previous controls
            _webView?.Dispose();
            _webView = null;
            // 注释掉原生窗口代码
            // _nativeWindow?.Dispose();
            // _nativeWindow = null;
            _isInitialized = false;

            if (_useNativeWindow)
            {
                Logger.LogInfo("原生窗口模式由 Program.cs 主窗口处理，此控件显示占位符");
                this.Controls.Add(new Label { Text = "原生透明窗口模式已启用\n(窗口由主程序管理)", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter });
                _isInitialized = true;
            }
            else
            {
                Logger.LogInfo("Initializing WebView2 mode.");
                InitializeWebView(); // Re-initialize WebView
                if (_webView != null)
                {
                    this.Controls.Add(_webView);
                    await LoadStreamAsync(settings.StreamAddress); // Load stream after re-init
                }
            }
        }

        // Call this method from AIManagerForm to get the native window instance        // 暂时注释掉原生窗口相关方法
        /*
        public NativeLayeredWindow? GetNativeWindow()
        {
            return _nativeWindow;
        }

        // Call this to update the frame in native mode
        public void UpdateNativeFrame(Bitmap frame)
        {
            if (_useNativeWindow && _nativeWindow != null && _nativeWindow.IsHandleCreated)
            {
                _nativeWindow.UpdateFrame(frame);
            }
        }
        */

        private async void InitializeWebView()
        {
            if (_webView != null) // Dispose if already exists (e.g., switching modes)
            {
                _webView.Dispose();
            }

            _webView = new WebView2
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Lime // 关键：控件背景色与主窗体一致
            };
            // No longer adding to Controls here, ApplyDisplayMode will do it.

            try
            {
                await _webView.EnsureCoreWebView2Async();
                Logger.LogInfo("WebView2 初始化成功");
                _isInitialized = true;

                // 设置WebView2控件和父控件透明
                _webView.DefaultBackgroundColor = System.Drawing.Color.Transparent;
                this.BackColor = Color.Lime;
                this.SetStyle(ControlStyles.SupportsTransparentBackColor, true);

                // 设置UserAgent为标准Chrome
                try {
                    string ua = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/123.0.0.0 Safari/537.36";
                    _webView.CoreWebView2.Settings.UserAgent = ua;
                    Logger.LogInfo($"WebView2 UserAgent已设置: {ua}");
                } catch (Exception ex) {
                    Logger.LogWarning($"设置UserAgent失败: {ex.Message}");
                }

                // 订阅导航完成/失败事件
                _webView.CoreWebView2.NavigationCompleted += (s, e) => {
                    Logger.LogInfo($"NavigationCompleted: {e.IsSuccess}, Error: {e.WebErrorStatus}");
                    if (!e.IsSuccess) MessageBox.Show($"WebView2导航失败: {e.WebErrorStatus}");
                };
                _webView.CoreWebView2.NavigationStarting += (s, e) => {
                    Logger.LogInfo($"NavigationStarting: {e.Uri}");
                };
                // _webView.CoreWebView2.NavigationFailed += (s, e) => {
                //     Logger.LogError($"NavigationFailed: {e.WebErrorStatus}");
                //     MessageBox.Show($"WebView2导航失败: {e.WebErrorStatus}");
                // };

                // 启用开发者工具
                try {
                    _webView.CoreWebView2.OpenDevToolsWindow();
                } catch { }

                // 注入严格canvas抠像脚本
                string chromaJs = @"
(function() {
    function isGreen(r, g, b) {
        // 绿色分量高且远高于红蓝，且不是白色
        return (
            g > 180 &&
            g > r + 40 &&
            g > b + 40 &&
            !(r > 200 && g > 200 && b > 200) // 排除白色
        );
    }
    function applyChromaKey() {
        var video = document.querySelector('video');
        if (!video) return setTimeout(applyChromaKey, 500);
        var canvas = document.createElement('canvas');
        canvas.width = video.videoWidth;
        canvas.height = video.videoHeight;
        canvas.style.position = 'absolute';
        canvas.style.left = video.offsetLeft + 'px';
        canvas.style.top = video.offsetTop + 'px';
        canvas.style.pointerEvents = 'none';
        canvas.style.zIndex = 9999;
        video.parentElement.appendChild(canvas);
        video.style.visibility = 'hidden';
        var ctx = canvas.getContext('2d');
        function render() {
            ctx.drawImage(video, 0, 0, canvas.width, canvas.height);
            var img = ctx.getImageData(0, 0, canvas.width, canvas.height);
            var data = img.data;
            for (var i = 0; i < data.length; i += 4) {
                var r = data[i], g = data[i+1], b = data[i+2];
                if (isGreen(r, g, b)) {
                    data[i+3] = 0;
                }
            }
            ctx.putImageData(img, 0, 0);
            requestAnimationFrame(render);
        }
        render();
    }
    document.body.style.background = 'transparent';
    document.documentElement.style.background = 'transparent';
    setTimeout(applyChromaKey, 1000);
})();
";
                await _webView.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync(chromaJs);

                // 启用透明背景
                _webView.CoreWebView2.Settings.IsGeneralAutofillEnabled = false;
                _webView.CoreWebView2.Settings.IsPasswordAutosaveEnabled = false;                // 尝试设置WebView2为透明背景
                try
                {
                    // 设置WebView2控件本身的透明背景
                    _webView.BackColor = Color.Transparent;
                    this.BackColor = Color.Transparent;
                    
                    // 设置支持透明背景（只对当前控件设置）
                    this.SetStyle(ControlStyles.SupportsTransparentBackColor, true);
                    
                    Logger.LogInfo("WebView2透明背景配置完成");
                }
                catch (Exception ex)
                {
                    Logger.LogWarning($"WebView2透明设置失败: {ex.Message}");
                }

                // 监听页面导航完成事件
                _webView.CoreWebView2.NavigationCompleted += async (sender, args) =>
                {
                    Logger.LogInfo($"页面导航完成: {args.IsSuccess}");
                    if (args.IsSuccess)
                    {
                        await Task.Delay(2000); // 等待页面完全加载
                        
                        // 设置调试
                        await SetupWebView2DebuggingAsync();
                        
                        // 根据抠像设置注入脚本
                        if (_enableChromaKey)
                        {
                            Logger.LogInfo("页面加载完成，开始注入抠像脚本");
                            await InjectChromaKeyScript(true);
                        }
                    }
                };

                // 设置默认背景
                if (_enableChromaKey)
                {
                    this.BackColor = _chromaKeyColor;
                }
                else
                {
                    this.BackColor = Color.Black;
                }
                
                Logger.LogInfo("DigitalHumanDisplay 初始化完成");
            }
            catch (Exception ex)
            {
                Logger.LogError($"WebView2 初始化失败: {ex.Message}", ex);
            }
        }        public async void SetChromaKeyEnabled(bool enabled)
        {
            _enableChromaKey = enabled;
            // 注释掉原生窗口逻辑
            /*
            if (_useNativeWindow && _nativeWindow != null)
            {
                _nativeWindow.EnableChromaKey(enabled); 
            }
            else 
            */
            if (!_useNativeWindow && _isInitialized && _webView?.CoreWebView2 != null)
            {
                Logger.LogInfo($"设置抠像功能: {enabled}");
                await InjectChromaKeyScript(enabled);
            }
        }

        public void SetChromaKeyColorInternal(Color color) // Renamed to avoid conflict with property
        {
            _chromaKeyColor = color;
            // 注释掉原生窗口逻辑
            /*
            if (_useNativeWindow && _nativeWindow != null)
            {
                _nativeWindow.SetChromaKeyColor(color);
            }
            else 
            */
            if (!_useNativeWindow && _isInitialized && _webView?.CoreWebView2 != null)
            {
                // Re-inject script with new color if WebView2 is active and chromakey is on
                if (_enableChromaKey)
                {
                    Task.Run(async () => await InjectChromaKeyScript(true));
                }
            }
            Invalidate(); // For UserControl's own BackColor if still relevant
        }

        private async Task InjectChromaKeyScript(bool enabled)
        {
            if (_webView == null || _webView.CoreWebView2 == null) // Added null check
            {
                Logger.LogWarning("InjectChromaKeyScript: WebView2 not available.");
                return;
            }

            try
            {
                string script;
                if (enabled)
                {
                    // 获取当前抠像颜色的RGB值
                    int targetR = _chromaKeyColor.R;
                    int targetG = _chromaKeyColor.G;
                    int targetB = _chromaKeyColor.B;
                    int tolerance = _tolerance; // 兼容原有容差
                    script = $@"
                        (function() {{
                            console.log('🎯 Canvas像素级抠像处理 [HSL精准绿色抠像] - 颜色: R={targetR}, G={targetG}, B={targetB}');
                            
                            // 创建全局参数对象，供后续参数更新使用
                            window.chromaKeyParams = {{
                                targetR: {targetR},
                                targetG: {targetG},
                                targetB: {targetB},
                                greenThreshold: 100,
                                colorTolerance: {tolerance},
                                minBrightness: 50,
                                maxBrightness: 255
                            }};
                            console.log('✅ 抠像参数对象已初始化', window.chromaKeyParams);
                            
                            document.body.style.cssText = `background: transparent !important; margin: 0 !important; padding: 0 !important; overflow: hidden !important;`;
                            document.documentElement.style.cssText = `background: transparent !important; margin: 0 !important; padding: 0 !important; overflow: hidden !important;`;
                            const video = document.querySelector('video');
                            if (!video) {{ setTimeout(arguments.callee, 1000); return; }}
                            if (video.videoWidth === 0 || video.videoHeight === 0) {{ setTimeout(arguments.callee, 1000); return; }}
                            const oldCanvas = document.getElementById('chroma-canvas');
                            if (oldCanvas) oldCanvas.remove();
                            video.style.cssText = `opacity: 0 !important; visibility: hidden !important; position: absolute !important; z-index: -1000 !important;`;
                            const canvas = document.createElement('canvas');
                            canvas.id = 'chroma-canvas';
                            canvas.width = video.videoWidth;
                            canvas.height = video.videoHeight;
                            canvas.style.cssText = `position: fixed !important; top: 0 !important; left: 0 !important; width: 100% !important; height: 100% !important; z-index: 9999 !important; pointer-events: none !important; background: transparent !important; object-fit: contain !important;`;
                            const ctx = canvas.getContext('2d', {{ alpha: true }});
                            ctx.globalCompositeOperation = 'source-over';
                            document.body.appendChild(canvas);
                            // RGB转HSL
                            function rgb2hsl(r, g, b) {{
                                r /= 255; g /= 255; b /= 255;
                                let max = Math.max(r, g, b), min = Math.min(r, g, b);
                                let h, s, l = (max + min) / 2;
                                if (max === min) {{ h = s = 0; }}
                                else {{
                                    let d = max - min;
                                    s = l > 0.5 ? d / (2 - max - min) : d / (max + min);
                                    switch (max) {{
                                        case r: h = (g - b) / d + (g < b ? 6 : 0); break;
                                        case g: h = (b - r) / d + 2; break;
                                        case b: h = (r - g) / d + 4; break;
                                    }}
                                    h /= 6;
                                }}
                                return [h * 360, s, l];
                            }}
                            function processFrame() {{
                                if (video.paused || video.ended || video.readyState < 2) {{ requestAnimationFrame(processFrame); return; }}
                                try {{
                                    ctx.drawImage(video, 0, 0, canvas.width, canvas.height);
                                    const imageData = ctx.getImageData(0, 0, canvas.width, canvas.height);
                                    const data = imageData.data;
                                    
                                    // 使用动态参数
                                    const params = window.chromaKeyParams || {{
                                        targetR: {targetR}, targetG: {targetG}, targetB: {targetB},
                                        greenThreshold: 100, colorTolerance: {tolerance}, 
                                        minBrightness: 50, maxBrightness: 255
                                    }};
                                    
                                    for (let i = 0; i < data.length; i += 4) {{
                                        const r = data[i], g = data[i+1], b = data[i+2];
                                        
                                        // 亮度过滤
                                        const brightness = (r + g + b) / 3;
                                        if (brightness < params.minBrightness || brightness > params.maxBrightness) {{
                                            continue; // 跳过过亮或过暗的像素
                                        }}
                                        
                                        // 使用动态参数进行绿色判定
                                        if (g > params.greenThreshold && g > r + 50 && g > b + 50) {{
                                            data[i+3] = 0; // 设为透明
                                        }}
                                        // 或者使用传统RGB距离判定
                                        else if (params.colorTolerance > 0) {{
                                            const colorDistance = Math.sqrt(Math.pow(r - params.targetR, 2) + Math.pow(g - params.targetG, 2) + Math.pow(b - params.targetB, 2));
                                            if (colorDistance <= params.colorTolerance) data[i+3] = 0;
                                        }}
                                    }}
                                    ctx.putImageData(imageData, 0, 0);
                                }} catch (e) {{ console.warn('像素处理错误:', e); }}
                                requestAnimationFrame(processFrame);
                            }}
                            processFrame();
                            
                            // 提供停止函数
                            window.stopChromaKey = function() {{
                                console.log('🛑 停止抠像处理');
                                // 可以在这里添加停止逻辑
                            }};
                            
                            setTimeout(() => {{
                                const style = document.createElement('style');
                                style.textContent = `* {{background: transparent !important;}} body, html {{background: transparent !important;backdrop-filter: none !important;-webkit-backdrop-filter: none !important;}} video {{mix-blend-mode: multiply !important;opacity: 0.01 !important;}} #chroma-canvas {{mix-blend-mode: normal !important;isolation: isolate !important;}}`;
                                document.head.appendChild(style);
                            }}, 3000);
                        }})();
                    ";
                }
                else
                {
                    // 禁用抠像：移除所有样式
                    script = @"
                        (function() {
                            console.log('🎯 禁用抠像处理');
                            
                            // 清理参数对象
                            if (window.chromaKeyParams) {{
                                delete window.chromaKeyParams;
                                console.log('✅ 抠像参数对象已清理');
                            }}
                            
                            // 停止处理函数
                            if (window.stopChromaKey) {{
                                window.stopChromaKey();
                            }}
                            
                            // 移除Canvas
                            const canvas = document.getElementById('chroma-canvas');
                            if (canvas) canvas.remove();
                            
                            // 恢复视频样式
                            const video = document.querySelector('video');
                            if (video) {
                                video.style.opacity = '';
                                video.style.display = '';
                            }
                            
                            // 恢复页面样式
                            document.body.style.background = '';
                            document.documentElement.style.background = '';
                            
                            console.log('✅ 抠像处理已禁用');
                        })();
                    ";
                }

                await _webView.CoreWebView2.ExecuteScriptAsync(script);
                Logger.LogInfo($"抠像脚本注入完成: {(enabled ? "启用" : "禁用")} - 目标颜色: R={_chromaKeyColor.R}, G={_chromaKeyColor.G}, B={_chromaKeyColor.B}");
            }
            catch (Exception ex)
            {
                Logger.LogError($"注入抠像脚本失败: {ex.Message}", ex);
            }
        }

        public async void UpdateChromaKeyScript(Color chromaKeyColor, int greenThreshold, int colorTolerance, int minBrightness, int maxBrightness, bool enableChromaKey)
        {
            _chromaKeyColor = chromaKeyColor;
            _tolerance = colorTolerance;
            _enableChromaKey = enableChromaKey;
            
            // 防抖：避免过于频繁的更新
            var currentTime = DateTime.Now;
            if ((currentTime - _lastUpdateTime).TotalMilliseconds < UPDATE_DEBOUNCE_MS)
            {
                // 如果距离上次更新时间太短，启动或重置定时器
                if (_updateTimer == null)
                {
                    _updateTimer = new System.Windows.Forms.Timer();
                    _updateTimer.Interval = UPDATE_DEBOUNCE_MS;
                    _updateTimer.Tick += (s, e) =>
                    {
                        _updateTimer.Stop();
                        _updateTimer = null;
                        _ = UpdateChromaKeyScriptInternal(chromaKeyColor, greenThreshold, colorTolerance, minBrightness, maxBrightness, enableChromaKey);
                    };
                }
                _updateTimer.Stop();
                _updateTimer.Start();
                return;
            }
            
            _lastUpdateTime = currentTime;
            await UpdateChromaKeyScriptInternal(chromaKeyColor, greenThreshold, colorTolerance, minBrightness, maxBrightness, enableChromaKey);
        }
        
        private async Task UpdateChromaKeyScriptInternal(Color chromaKeyColor, int greenThreshold, int colorTolerance, int minBrightness, int maxBrightness, bool enableChromaKey)
        {
            _chromaKeyColor = chromaKeyColor;
            _tolerance = colorTolerance;
            _enableChromaKey = enableChromaKey;
            
            if (_webView?.CoreWebView2 == null) return;
            
            try
            {
                int targetR = chromaKeyColor.R;
                int targetG = chromaKeyColor.G;
                int targetB = chromaKeyColor.B;
                
                if (enableChromaKey)
                {
                    // 先检查系统是否已初始化，如果没有则先初始化
                    string checkScript = @"
                        (function() {
                            return window.chromaKeyParams ? 'initialized' : 'not_initialized';
                        })();
                    ";
                    
                    string checkResult = await _webView.CoreWebView2.ExecuteScriptAsync(checkScript);
                    checkResult = checkResult.Trim('"'); // 移除引号
                    
                    if (checkResult == "not_initialized")
                    {
                        Logger.LogWarning("抠像系统未初始化，正在重新初始化...");
                        // 重新初始化抠像系统
                        await InjectChromaKeyScript(true);
                        // 等待一下让初始化完成
                        await Task.Delay(500);
                    }
                    
                    // 更新参数
                    string script = $@"
                        (function() {{
                            console.log('🔄 实时更新抠像参数: 绿色阈值={greenThreshold}, 容差={colorTolerance}, 亮度={minBrightness}-{maxBrightness}');
                            
                            // 更新全局参数
                            if (window.chromaKeyParams) {{
                                window.chromaKeyParams.targetR = {targetR};
                                window.chromaKeyParams.targetG = {targetG};
                                window.chromaKeyParams.targetB = {targetB};
                                window.chromaKeyParams.greenThreshold = {greenThreshold};
                                window.chromaKeyParams.colorTolerance = {colorTolerance};
                                window.chromaKeyParams.minBrightness = {minBrightness};
                                window.chromaKeyParams.maxBrightness = {maxBrightness};
                                console.log('✅ 参数已更新', window.chromaKeyParams);
                                return 'success';
                            }} else {{
                                console.log('❌ 抠像系统初始化失败，无法更新参数');
                                return 'failed';
                            }}
                        }})();
                    ";
                    
                    string result = await _webView.CoreWebView2.ExecuteScriptAsync(script);
                    result = result.Trim('"');
                    
                    if (result == "failed")
                    {
                        Logger.LogError("抠像参数更新失败，系统可能未正确初始化");
                    }
                }
                else
                {
                    // 禁用抠像时停止处理
                    string disableScript = @"
                        (function() {
                            if (window.stopChromaKey) {
                                window.stopChromaKey();
                                console.log('🛑 抠像处理已停止');
                            }
                        })();
                    ";
                    await _webView.CoreWebView2.ExecuteScriptAsync(disableScript);
                }
                
                Logger.LogInfo($"✅ 抠像参数实时更新完成: 颜色=RGB({targetR},{targetG},{targetB}), 绿色阈值={greenThreshold}, 容差={colorTolerance}");
            }
            catch (Exception ex)
            {
                Logger.LogError($"❌ 更新抠像参数时出错: {ex.Message}", ex);
            }
        }

        private async Task SetupWebView2DebuggingAsync()
        {
            if (_webView?.CoreWebView2 == null) return;
            
            try
            {
                // 注入调试脚本
                string debugScript = @"
                    // 添加调试功能
                    console.log('WebView2 调试模式已启用');
                    
                    // 监听页面错误
                    window.addEventListener('error', function(e) {
                        console.error('页面错误:', e.error);
                    });
                    
                    // 重写 console.log 以便捕获
                    const originalLog = console.log;
                    console.log = function(...args) {
                        originalLog.apply(console, args);
                        // 可以在这里添加额外的日志处理
                    };
                ";
                
                await _webView.CoreWebView2.ExecuteScriptAsync(debugScript);
                Logger.LogInfo("WebView2 调试模式已启用");
            }
            catch (Exception ex)
            {
                Logger.LogError($"设置WebView2调试失败: {ex.Message}", ex);
            }
        }        public Task LoadStreamAsync(string url)
        {
            Logger.LogInfo($"=== LoadStreamAsync 调用开始 ===");
            Logger.LogInfo($"参数 URL: {url}");
            Logger.LogInfo($"_useNativeWindow: {_useNativeWindow}");
            Logger.LogInfo($"_isInitialized: {_isInitialized}");
            Logger.LogInfo($"_webView != null: {_webView != null}");
            Logger.LogInfo($"_webView?.CoreWebView2 != null: {_webView?.CoreWebView2 != null}");
            
            if (_useNativeWindow)
            {
                Logger.LogInfo("Native window mode: Stream loading handled by video capture.");
                // In native mode, video frames will be pushed via UpdateNativeFrame.
                // This UserControl won't directly load a URL into a WebView.
                return Task.CompletedTask;
            }

            if (!_isInitialized || _webView?.CoreWebView2 == null)
            {
                Logger.LogWarning("WebView2 未初始化，无法加载视频流");
                Logger.LogWarning($"详细状态: _isInitialized={_isInitialized}, _webView={_webView != null}, CoreWebView2={_webView?.CoreWebView2 != null}");
                return Task.CompletedTask;
            }

            try
            {
                Logger.LogInfo($"正在导航到: {url}");
                _webView.CoreWebView2.Navigate(url);
                Logger.LogInfo("Navigate() 方法调用完成");
                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                Logger.LogError($"加载视频流失败: {ex.Message}", ex);
                return Task.CompletedTask;
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            
            if (!_useNativeWindow) // Only set BackColor if not using native window (which has its own)
            {
                if (_enableChromaKey)
                {
                    this.BackColor = _chromaKeyColor;
                }
                else
                {
                    this.BackColor = Color.Black;
                }
            }
            else
            {
                // When native window is active, this UserControl's background might not be visible
                // or could be set to a specific color if it's a placeholder.
                this.BackColor = Color.Fuchsia; // Placeholder color to indicate native mode
            }
        }        // Ensure resources are cleaned up
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _webView?.Dispose();
                // _nativeWindow?.Dispose(); // 注释掉原生窗口逻辑
            }
            base.Dispose(disposing);
        }
    }
}
