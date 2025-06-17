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
                Dock = DockStyle.Fill
            };
            // No longer adding to Controls here, ApplyDisplayMode will do it.

            try
            {
                await _webView.EnsureCoreWebView2Async();
                Logger.LogInfo("WebView2 初始化成功");
                _isInitialized = true;

                // 设置页面背景透明
                _webView.CoreWebView2.Settings.AreDefaultScriptDialogsEnabled = false;
                _webView.CoreWebView2.Settings.AreDevToolsEnabled = true;

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
                      // Canvas像素级抠像处理 - 支持自定义颜色 + 强化透明穿透
                    script = $@"
                        (function() {{
                            console.log('🎯 Canvas像素级抠像处理 [v11-透明穿透增强] - 颜色: R={targetR}, G={targetG}, B={targetB}');
                            
                            // 首先设置页面完全透明
                            document.body.style.cssText = `
                                background: transparent !important;
                                margin: 0 !important;
                                padding: 0 !important;
                                overflow: hidden !important;
                            `;
                            document.documentElement.style.cssText = `
                                background: transparent !important;
                                margin: 0 !important;
                                padding: 0 !important;
                                overflow: hidden !important;
                            `;
                            
                            // 查找视频元素
                            const video = document.querySelector('video');
                            if (!video) {{
                                console.log('❌ 未找到视频元素，稍后重试');
                                setTimeout(arguments.callee, 1000);
                                return;
                            }}
                            
                            // 等待视频加载
                            if (video.videoWidth === 0 || video.videoHeight === 0) {{
                                console.log('⏳ 等待视频尺寸加载...');
                                setTimeout(arguments.callee, 1000);
                                return;
                            }}
                            
                            console.log('✅ 找到视频元素，尺寸:', video.videoWidth + 'x' + video.videoHeight);
                            
                            // 移除旧的Canvas
                            const oldCanvas = document.getElementById('chroma-canvas');
                            if (oldCanvas) oldCanvas.remove();
                            
                            // 隐藏原视频，完全由Canvas接管
                            video.style.cssText = `
                                opacity: 0 !important;
                                visibility: hidden !important;
                                position: absolute !important;
                                z-index: -1000 !important;
                            `;
                            
                            // 创建Canvas元素
                            const canvas = document.createElement('canvas');
                            canvas.id = 'chroma-canvas';
                            canvas.width = video.videoWidth;
                            canvas.height = video.videoHeight;
                            canvas.style.cssText = `
                                position: fixed !important;
                                top: 0 !important;
                                left: 0 !important;
                                width: 100% !important;
                                height: 100% !important;
                                z-index: 9999 !important;
                                pointer-events: none !important;
                                background: transparent !important;
                                object-fit: contain !important;
                            `;
                            
                            const ctx = canvas.getContext('2d', {{ alpha: true }});
                            ctx.globalCompositeOperation = 'source-over';
                            
                            // 将Canvas插入到页面顶层
                            document.body.appendChild(canvas);
                            
                            // 抠像参数 - 目标颜色
                            const chromaKey = {{
                                targetR: {targetR},
                                targetG: {targetG}, 
                                targetB: {targetB},
                                tolerance: {_tolerance}  // 容差
                            }};
                            
                            // 抠像处理函数
                            function processFrame() {{
                                if (video.paused || video.ended || video.readyState < 2) {{
                                    requestAnimationFrame(processFrame);
                                    return;
                                }}
                                
                                try {{
                                    // 绘制视频帧到Canvas
                                    ctx.drawImage(video, 0, 0, canvas.width, canvas.height);
                                    
                                    // 获取像素数据
                                    const imageData = ctx.getImageData(0, 0, canvas.width, canvas.height);
                                    const data = imageData.data;
                                    
                                    // 处理每个像素
                                    for (let i = 0; i < data.length; i += 4) {{
                                        const r = data[i];
                                        const g = data[i + 1];
                                        const b = data[i + 2];
                                        
                                        // 检测目标颜色像素 - 使用欧氏距离计算颜色相似度
                                        const colorDistance = Math.sqrt(
                                            Math.pow(r - chromaKey.targetR, 2) +
                                            Math.pow(g - chromaKey.targetG, 2) +
                                            Math.pow(b - chromaKey.targetB, 2)
                                        );
                                        
                                        if (colorDistance <= chromaKey.tolerance) {{
                                            // 将目标颜色像素设为完全透明
                                            data[i + 3] = 0;
                                        }}
                                    }}
                                    
                                    // 更新Canvas
                                    ctx.putImageData(imageData, 0, 0);
                                }} catch (e) {{
                                    console.warn('像素处理错误:', e);
                                }}
                                
                                // 继续处理下一帧
                                requestAnimationFrame(processFrame);
                            }}
                              // 启动抠像处理
                            console.log('✅ 开始Canvas抠像处理，目标颜色: RGB(' + chromaKey.targetR + ',' + chromaKey.targetG + ',' + chromaKey.targetB + ')');
                            processFrame();
                            
                            // 添加CSS透明穿透备用方案
                            setTimeout(() => {{
                                console.log('🔧 应用CSS透明穿透备用方案');
                                
                                // 创建CSS样式
                                const style = document.createElement('style');
                                style.textContent = `
                                    * {{
                                        background: transparent !important;
                                    }}
                                    
                                    body, html {{
                                        background: transparent !important;
                                        backdrop-filter: none !important;
                                        -webkit-backdrop-filter: none !important;
                                    }}
                                    
                                    video {{
                                        mix-blend-mode: multiply !important;
                                        opacity: 0.01 !important;
                                    }}
                                    
                                    #chroma-canvas {{
                                        mix-blend-mode: normal !important;
                                        isolation: isolate !important;
                                    }}
                                `;
                                document.head.appendChild(style);
                                
                                console.log('✅ CSS透明穿透方案已应用');
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
                return Task.CompletedTask;
            }

            try
            {
                Logger.LogInfo($"加载视频流: {url}");
                _webView.CoreWebView2.Navigate(url);
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
