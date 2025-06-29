using System;
using System.Speech.Recognition;
using System.Speech.Synthesis;
using System.Threading.Tasks;
using CoreApplication; // For Logger

namespace CoreApplication
{
    public class SpeechService : IDisposable
    {
        private SpeechRecognitionEngine? _recognitionEngine;
        private SpeechSynthesizer? _synthesizer;

        public event EventHandler<string>? OnSpeechRecognized;
        public event EventHandler? OnSpeechSynthesisStarted;
        public event EventHandler? OnSpeechSynthesisEnded;
        public event EventHandler<string>? OnError;        public SpeechService()
        {
            // 初始化语音合成器（这个通常不需要音频输入设备）
            try
            {
                _synthesizer = new SpeechSynthesizer();
                _synthesizer.SpeakStarted += Synthesizer_SpeakStarted;
                _synthesizer.SpeakCompleted += Synthesizer_SpeakCompleted;
            }
            catch (Exception ex)
            {
                Logger.LogWarning($"语音合成初始化失败: {ex.Message}");
                _synthesizer = null; // 将合成器设为null以表示初始化失败
            }

            // 初始化语音识别引擎
            try
            {
                _recognitionEngine = new SpeechRecognitionEngine();
                _recognitionEngine.SetInputToDefaultAudioDevice(); // 设置默认音频输入设备
                _recognitionEngine.SpeechRecognized += RecognitionEngine_SpeechRecognized;
                _recognitionEngine.SpeechRecognitionRejected += RecognitionEngine_SpeechRecognitionRejected;
                _recognitionEngine.RecognizeCompleted += RecognitionEngine_RecognizeCompleted;
                
                // 添加语音识别规则
                _recognitionEngine.LoadGrammar(new DictationGrammar());
                Logger.LogInfo("语音识别引擎初始化成功。");
            }
            catch (Exception ex)
            {
                Logger.LogWarning($"语音识别初始化失败（可能是因为没有麦克风）: {ex.Message}");
                _recognitionEngine = null; // 将引擎设为null以表示初始化失败
            }
        }

        // ASR 相关方法
        public void StartSpeechRecognition()
        {
            if (_recognitionEngine == null)
            {
                var message = "语音识别引擎未初始化，请检查音频输入设备。";
                Logger.LogWarning(message);
                OnError?.Invoke(this, message);
                return;
            }

            try
            {
                Logger.LogInfo("启动语音识别。");
                _recognitionEngine.RecognizeAsync(RecognizeMode.Multiple); // 持续识别
            }
            catch (Exception ex)
            {
                Logger.LogError($"启动语音识别失败: {ex.Message}", ex);
                OnError?.Invoke(this, $"启动语音识别失败: {ex.Message}");
            }
        }

        public void StopSpeechRecognition()
        {
            if (_recognitionEngine == null) return;

            try
            {
                Logger.LogInfo("停止语音识别。");
                _recognitionEngine.RecognizeAsyncStop();
            }
            catch (Exception ex)
            {
                Logger.LogError($"停止语音识别失败: {ex.Message}", ex);
                OnError?.Invoke(this, $"停止语音识别失败: {ex.Message}");
            }
        }

        private void RecognitionEngine_SpeechRecognized(object? sender, SpeechRecognizedEventArgs e)
        {
            string recognizedText = e.Result.Text;
            Logger.LogInfo($"语音识别结果: {recognizedText}");
            OnSpeechRecognized?.Invoke(this, recognizedText);
        }

        private void RecognitionEngine_SpeechRecognitionRejected(object? sender, SpeechRecognitionRejectedEventArgs e)
        {
            Logger.LogWarning("语音识别被拒绝。");
            // 可以根据需要处理识别拒绝的情况
        }

        private void RecognitionEngine_RecognizeCompleted(object? sender, RecognizeCompletedEventArgs e)
        {
            Logger.LogInfo("语音识别完成。");
            if (e.Error != null)
            {
                Logger.LogError($"语音识别错误: {e.Error.Message}", e.Error);
                OnError?.Invoke(this, $"语音识别错误: {e.Error.Message}");
            }
            else if (e.Cancelled)
            {
                Logger.LogInfo("语音识别已取消。");
            }
        }

        // TTS 相关方法
        public Task<bool> SynthesizeSpeech(string text, string? voiceName = null, int rate = 0, int volume = 100)
        {
            var tcs = new TaskCompletionSource<bool>();

            if (_synthesizer == null)
            {
                OnError?.Invoke(this, "语音合成器未初始化");
                tcs.SetResult(false);
                return tcs.Task;
            }

            try
            {
                if (!string.IsNullOrEmpty(voiceName))
                {
                    try
                    {
                        _synthesizer.SelectVoice(voiceName);
                    }
                    catch (ArgumentException ex)
                    {
                        Logger.LogWarning($"选择语音 {voiceName} 失败，使用默认语音: {ex.Message}");
                    }
                }
                
                _synthesizer.Rate = rate;
                _synthesizer.Volume = volume;

                _synthesizer.SpeakCompleted += OnSynthesisCompleted;
                _synthesizer.SpeakAsync(text);

                void OnSynthesisCompleted(object? sender, SpeakCompletedEventArgs e)
                {
                    _synthesizer.SpeakCompleted -= OnSynthesisCompleted;
                    tcs.SetResult(!e.Cancelled);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"语音合成失败: {ex.Message}", ex);
                OnError?.Invoke(this, $"语音合成失败: {ex.Message}");
                tcs.SetResult(false);
            }

            return tcs.Task;
        }

        public void Speak(string text)
        {
            if (_synthesizer == null)
            {
                OnError?.Invoke(this, "语音合成器未初始化");
                return;
            }

            try
            {
                _synthesizer.SpeakAsync(text);
            }
            catch (Exception ex)
            {
                Logger.LogError($"语音合成失败: {ex.Message}", ex);
                OnError?.Invoke(this, $"语音合成失败: {ex.Message}");
            }
        }

        public void StopSpeaking()
        {
            _synthesizer?.SpeakAsyncCancelAll();
        }        public VoiceInfo[] GetAvailableVoices()
        {
            if (_synthesizer == null)
            {
                Logger.LogWarning("语音合成器未初始化，无法获取可用语音。");
                return Array.Empty<VoiceInfo>();
            }
            return _synthesizer.GetInstalledVoices().Select(v => v.VoiceInfo).ToArray();
        }

        // TODO: GetAvailableRecognitionLanguages() 需要更复杂的实现，可能需要枚举系统语言包

        private void Synthesizer_SpeakStarted(object? sender, SpeakStartedEventArgs e)
        {
            Logger.LogInfo("语音合成开始播放。");
            OnSpeechSynthesisStarted?.Invoke(this, EventArgs.Empty);
        }

        private void Synthesizer_SpeakCompleted(object? sender, SpeakCompletedEventArgs e)
        {
            Logger.LogInfo("语音合成播放完成。");
            OnSpeechSynthesisEnded?.Invoke(this, EventArgs.Empty);
        }

        public void Dispose()
        {
            _recognitionEngine?.Dispose();
            _synthesizer?.Dispose();
        }
    }
}