using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.IO;
using System.Threading.Tasks;

namespace CoreApplication
{
    /// <summary>
    /// 表示一个数字人动作
    /// </summary>
    public class DigitalHumanAction
    {
        /// <summary>
        /// 动作的唯一标识符
        /// </summary>
        public string ActionId { get; set; } = "";

        /// <summary>
        /// 动作的持续时间（秒）
        /// </summary>
        public float Duration { get; set; }

        /// <summary>
        /// 动作的附加参数
        /// </summary>
        public Dictionary<string, object>? Parameters { get; set; }
    }

    /// <summary>
    /// 表示一个讲解段落
    /// </summary>
    public class ScriptSegment
    {
        /// <summary>
        /// 讲解文本
        /// </summary>
        public string Text { get; set; } = "";

        /// <summary>
        /// 段落对应的动作列表
        /// </summary>
        public List<DigitalHumanAction> Actions { get; set; } = new List<DigitalHumanAction>();

        /// <summary>
        /// 语音速率（可选，-10到10）
        /// </summary>
        public int? SpeechRate { get; set; }

        /// <summary>
        /// 语音音量（可选，0到100）
        /// </summary>
        public int? SpeechVolume { get; set; }

        /// <summary>
        /// 语音音色名称（可选）
        /// </summary>
        public string? VoiceName { get; set; }
    }

    /// <summary>
    /// 表示一个完整的讲解脚本
    /// </summary>
    public class Script
    {
        /// <summary>
        /// 脚本的唯一标识符
        /// </summary>
        public string Id { get; set; } = "";

        /// <summary>
        /// 脚本标题
        /// </summary>
        public string Title { get; set; } = "";

        /// <summary>
        /// 讲解段落列表
        /// </summary>
        public List<ScriptSegment> Segments { get; set; } = new List<ScriptSegment>();

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        /// <summary>
        /// 最后修改时间
        /// </summary>
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
    }

    /// <summary>
    /// 负责管理讲解词和动作脚本的服务类
    /// </summary>
    public class ScriptService : IDisposable
    {
        private readonly WebSocketClient _webSocketClient;
        private readonly SpeechService _speechService;
        private Script? _currentScript;
        private int _currentSegmentIndex = -1;
        private bool _isPlaying;
        private bool _isPaused;
        private bool _isLoopEnabled;

        public event EventHandler<float>? OnPlayProgress;
        public event EventHandler? OnScriptFinished;
        public event EventHandler<string>? OnError;

        public ScriptService(WebSocketClient webSocketClient, SpeechService speechService)
        {
            _webSocketClient = webSocketClient;
            _speechService = speechService;
            _speechService.OnSpeechSynthesisEnded += SpeechService_SpeechSynthesisEnded;
        }

        /// <summary>
        /// 从文件加载脚本
        /// </summary>
        public async Task<Script?> LoadScriptAsync(string filePath)
        {
            try
            {
                string jsonContent = await File.ReadAllTextAsync(filePath);
                _currentScript = JsonSerializer.Deserialize<Script>(jsonContent);
                Logger.LogInfo($"已加载脚本：{_currentScript?.Title}");
                return _currentScript;
            }
            catch (Exception ex)
            {
                Logger.LogError($"加载脚本失败：{ex.Message}", ex);
                OnError?.Invoke(this, $"加载脚本失败：{ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 保存脚本到文件
        /// </summary>
        public async Task SaveScriptAsync(string filePath, Script script)
        {
            try
            {
                script.UpdatedAt = DateTime.Now;
                string jsonContent = JsonSerializer.Serialize(script, new JsonSerializerOptions { WriteIndented = true });
                await File.WriteAllTextAsync(filePath, jsonContent);
                Logger.LogInfo($"已保存脚本：{script.Title}");
            }
            catch (Exception ex)
            {
                Logger.LogError($"保存脚本失败：{ex.Message}", ex);
                OnError?.Invoke(this, $"保存脚本失败：{ex.Message}");
            }
        }

        /// <summary>
        /// 开始播放当前脚本
        /// </summary>
        public void PlayScript()
        {
            if (_currentScript == null || _currentScript.Segments.Count == 0)
            {
                OnError?.Invoke(this, "没有可播放的脚本");
                return;
            }

            if (_isPaused)
            {
                _isPaused = false;
                Logger.LogInfo("继续播放脚本");
            }
            else
            {
                _currentSegmentIndex = 0;
                Logger.LogInfo("开始播放脚本");
            }

            _isPlaying = true;
            PlayCurrentSegment();
        }

        /// <summary>
        /// 暂停播放
        /// </summary>
        public void PauseScript()
        {
            if (_isPlaying)
            {
                _isPaused = true;
                _isPlaying = false;
                // TODO: 暂停语音播放和动作
                Logger.LogInfo("脚本播放已暂停");
            }
        }

        /// <summary>
        /// 停止播放
        /// </summary>
        public void StopScript()
        {
            _isPlaying = false;
            _isPaused = false;
            _currentSegmentIndex = -1;
            // TODO: 停止语音播放和动作
            Logger.LogInfo("脚本播放已停止");
        }

        /// <summary>
        /// 设置是否循环播放
        /// </summary>
        public void SetLoopEnabled(bool enabled)
        {
            _isLoopEnabled = enabled;
            Logger.LogInfo($"循环播放已{(enabled ? "启用" : "禁用")}");
        }

        private void PlayCurrentSegment()
        {
            if (_currentScript == null || _currentSegmentIndex < 0 || _currentSegmentIndex >= _currentScript.Segments.Count)
            {
                return;
            }

            var segment = _currentScript.Segments[_currentSegmentIndex];

            // 发送动作指令给UE
            foreach (var action in segment.Actions)
            {
                var actionMessage = new
                {
                    type = "TRIGGER_ACTION",
                    data = new
                    {
                        actionId = action.ActionId,
                        duration = action.Duration,
                        parameters = action.Parameters
                    }
                };
                string jsonMessage = JsonSerializer.Serialize(actionMessage);
                _ = _webSocketClient.SendMessageAsync(jsonMessage);
            }

            // 使用语音服务播放文本
            _speechService.SynthesizeSpeech(
                segment.Text,
                segment.VoiceName,
                segment.SpeechRate ?? 0,
                segment.SpeechVolume ?? 100
            );

            // 更新播放进度
            float progress = (_currentSegmentIndex + 1.0f) / _currentScript.Segments.Count;
            OnPlayProgress?.Invoke(this, progress);
        }

        private void SpeechService_SpeechSynthesisEnded(object? sender, EventArgs e)
        {
            if (!_isPlaying || _isPaused)
            {
                return;
            }

            _currentSegmentIndex++;
            
            if (_currentScript != null && _currentSegmentIndex >= _currentScript.Segments.Count)
            {
                if (_isLoopEnabled)
                {
                    _currentSegmentIndex = 0;
                    PlayCurrentSegment();
                }
                else
                {
                    _isPlaying = false;
                    _currentSegmentIndex = -1;
                    OnScriptFinished?.Invoke(this, EventArgs.Empty);
                }
            }
            else
            {
                PlayCurrentSegment();
            }
        }

        public void Dispose()
        {
            if (_speechService != null)
            {
                _speechService.OnSpeechSynthesisEnded -= SpeechService_SpeechSynthesisEnded;
            }
        }
    }
}