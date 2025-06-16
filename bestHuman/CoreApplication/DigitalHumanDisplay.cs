using System;
using System.Drawing;
using System.Windows.Forms;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
using System.Threading.Tasks;

namespace CoreApplication
{
    public class DigitalHumanDisplay : Panel
    {
        private Color _chromaKeyColor = Color.Green; // 默认抠像颜色为绿色
        private int _tolerance = 30; // 默认颜色容差
        private bool _enableChromaKey = false; // 默认禁用抠像功能，避免影响性能
        private WebView2? _webView;
        private string? _streamUrl;

        [System.ComponentModel.Browsable(false)]
        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public bool EnableChromaKey
        {
            get { return _enableChromaKey; }
            set { _enableChromaKey = value; }
        }

        [System.ComponentModel.Browsable(false)]
        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public Color ChromaKeyColor
        {
            get { return _chromaKeyColor; }
            set { _chromaKeyColor = value; Invalidate(); }
        }

        [System.ComponentModel.Browsable(false)]
        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public int Tolerance
        {
            get { return _tolerance; }
            set { _tolerance = value; Invalidate(); }
        }        public DigitalHumanDisplay()
        {
            DoubleBuffered = true; // 启用双缓冲，减少闪烁
            SetStyle(ControlStyles.SupportsTransparentBackColor | 
                    ControlStyles.AllPaintingInWmPaint | 
                    ControlStyles.UserPaint, true);
            
            // 控件背景统一为透明
            BackColor = Color.Transparent;
            
            // 控件大小变化时重新调整WebView2
            this.SizeChanged += (sender, e) => {
                if (_webView != null) {
                    _webView.Size = this.ClientSize;
                }
            };
            
            InitializeWebView();
        }

        private async void InitializeWebView()
        {
            try
            {
                // 创建WebView2控件
                _webView = new WebView2
                {
                    Dock = DockStyle.Fill, // 填充整个父控件
                    Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                    Location = new Point(0, 0),
                    Size = this.ClientSize, // 确保完全填充
                    Margin = new Padding(0) // 无边距
                };
                
                // WebView2背景设为透明
                _webView.DefaultBackgroundColor = Color.Transparent;
                
                Controls.Add(_webView);
                await _webView.EnsureCoreWebView2Async();

                // WebView2配置
                var settings = _webView.CoreWebView2.Settings;
                settings.IsWebMessageEnabled = true;
                settings.AreDefaultContextMenusEnabled = false;
                settings.IsStatusBarEnabled = false;
                
                Logger.LogInfo("WebView2透明背景配置完成");
                Logger.LogInfo("WebView2 初始化完成");
            }
            catch (Exception ex)
            {
                Logger.LogError($"初始化WebView2失败: {ex.Message}", ex);
            }
        }

        public void SetStreamUrl(string url)
        {
            _streamUrl = url;
            Logger.LogInfo($"设置流URL: {url}");
            if (_webView?.CoreWebView2 != null)
            {
                try 
                {
                    _webView.CoreWebView2.Navigate(url);
                    Logger.LogInfo("成功导航到流URL");
                    
                    // 等待页面加载完成后启用调试
                    _webView.CoreWebView2.NavigationCompleted += async (sender, args) =>
                    {
                        if (args.IsSuccess)
                        {
                            Logger.LogInfo("页面导航完成，启用调试模式");
                            await Task.Delay(1000); // 等待页面稳定
                            await SetupWebView2DebuggingAsync();
                            
                            // 如果抠像已启用，重新应用
                            if (_enableChromaKey)
                            {
                                Logger.LogInfo("页面加载完成，重新应用抠像效果");
                                await Task.Delay(500);
                                InjectChromaKeyScript(true);
                            }
                        }
                        else
                        {
                            Logger.LogError("页面导航失败");
                        }
                    };
                }
                catch (Exception ex)
                {
                    Logger.LogError($"导航到流URL失败: {ex.Message}", ex);
                }
            }
        }        public async void SetChromaKeyEnabled(bool enabled)
        {
            _enableChromaKey = enabled;
            Logger.LogInfo($"抠像功能已{(enabled ? "启用" : "禁用")}");
            
            // 获取父窗口引用
            Form? parentForm = this.FindForm();
            
            if (parentForm != null)
            {
                if (enabled)
                {
                    // 启用抠像时，设置窗口透明色为绿色
                    parentForm.BackColor = Color.Green;
                    parentForm.TransparencyKey = Color.Green;
                    Logger.LogInfo("已设置窗口透明色为绿色");
                }
                else
                {
                    // 禁用抠像时，移除窗口透明色
                    parentForm.BackColor = SystemColors.Control;
                    parentForm.TransparencyKey = Color.Empty;
                    Logger.LogInfo("已移除窗口透明色");
                }
                
                // 强制刷新窗口透明设置
                parentForm.Invalidate();
                parentForm.Update();
            }
            
            if (_webView?.CoreWebView2 != null)
            {
                // 确保页面已完全加载，然后注入抠像脚本
                await Task.Delay(300); // 短暂延迟确保稳定
                InjectChromaKeyScript(enabled);
            }
            
            this.Invalidate();
        }private async void InjectChromaKeyScript(bool enabled)
        {
            if (_webView?.CoreWebView2 == null) return;
            
            try
            {
                string script;
                if (enabled)
                {
                    // 纯净版抠像脚本 - 简单地将绿色背景变透明，不添加任何滤镜或遮罩
                    script = @"
                        (function() {
                            console.log('🎯 绿屏抠像处理 [v6 纯净版]');
                            
                            // 查找视频元素
                            const video = document.querySelector('video');
                            if (!video) {
                                console.log('❌ 未找到视频元素');
                                setTimeout(arguments.callee, 2000);
                                return;
                            }
                            
                            console.log('✅ 找到视频元素，尺寸:', video.videoWidth + 'x' + video.videoHeight);
                            
                            // 等待视频加载
                            if (video.videoWidth === 0 || video.videoHeight === 0) {
                                console.log('⏳ 等待视频加载...');
                                setTimeout(arguments.callee, 2000);
                                return;
                            }
                            
                            // 移除可能影响颜色的旧样式
                            document.querySelectorAll('style').forEach(style => {
                                if (style.textContent.includes('filter') || 
                                    style.textContent.includes('mask') ||
                                    style.textContent.includes('chroma')) {
                                    style.remove();
                                    console.log('✅ 已移除旧样式');
                                }
                            });
                            
                            // 纯化页面背景，保持绿色底透明
                            document.body.style.background = 'transparent';
                            document.documentElement.style.background = 'transparent';
                            
                            // 设置纯净样式，不使用遮罩或滤镜
                            const style = document.createElement('style');
                            style.id = 'clean-chroma-style';
                            style.textContent = `
                                body, html {
                                    background: transparent !important;
                                    margin: 0 !important;
                                    padding: 0 !important;
                                    overflow: hidden !important;
                                }
                                
                                video {
                                    /* 不应用任何滤镜或遮罩，保持原始颜色 */
                                    filter: none !important;
                                    -webkit-mask: none !important;
                                    mask: none !important;
                                    background: transparent !important;
                                    width: 100% !important;
                                    height: 100% !important;
                                    object-fit: cover !important;
                                }
                                
                                /* 移除其他干扰元素 */
                                .overlay, .controls, .watermark {
                                    display: none !important;
                                }
                            `;
                            document.head.appendChild(style);
                            console.log('✅ 纯净版绿屏抠像已应用');
                        })();
                    ";
                }
                else
                {
                    // 禁用抠像：恢复正常显示，移除所有特殊样式
                    script = @"
                        (function() {
                            // 移除所有自定义样式
                            document.querySelectorAll('style').forEach(style => {
                                if (style.id === 'clean-chroma-style' || 
                                    style.textContent.includes('transparent')) {
                                    style.remove();
                                }
                            });
                            
                            // 恢复视频默认样式
                            const video = document.querySelector('video');
                            if (video) {
                                video.style = '';
                            }
                            
                            // 恢复页面默认样式
                            document.body.style.background = '';
                            document.documentElement.style.background = '';
                            
                            console.log('✅ 抠像处理已禁用，恢复原始显示');
                        })();
                    ";
                }
                
                await _webView.CoreWebView2.ExecuteScriptAsync(script);
                Logger.LogInfo($"抠像脚本: {(enabled ? "启用" : "禁用")} - JavaScript处理完成");
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
                        console.error('页面错误:', e.message, e.filename, e.lineno);
                    });
                    
                    // 监听DOM变化
                    if (typeof MutationObserver !== 'undefined') {
                        const observer = new MutationObserver(function(mutations) {
                            mutations.forEach(function(mutation) {
                                if (mutation.type === 'childList' && mutation.addedNodes.length > 0) {
                                    console.log('DOM元素已添加:', mutation.addedNodes);
                                }
                            });
                        });
                        observer.observe(document.body, { childList: true, subtree: true });
                    }
                ";
                
                await _webView.CoreWebView2.ExecuteScriptAsync(debugScript);
                Logger.LogInfo("WebView2调试脚本已注入");
            }
            catch (Exception ex)
            {
                Logger.LogError($"设置WebView2调试失败: {ex.Message}", ex);
            }
        }

        // 重写Paint方法，确保透明效果
        protected override void OnPaint(PaintEventArgs e)
        {
            // 不绘制任何背景，保持完全透明
            // base.OnPaint(e); 不调用基类绘制
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                this.BackgroundImage?.Dispose();
                _webView?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
