using System;
using System.Speech.Recognition;
using System.Speech.Synthesis;
using System.Threading.Tasks;
using CoreApplication; // For Logger

namespace CoreApplication
{
    public class SpeechService : IDisposable
    {
        private SpeechRecognitionEngine _recognitionEngine;
        private SpeechSynthesizer _synthesizer;

        public event EventHandler<string>? OnSpeechRecognized;
        public event EventHandler? OnSpeechSynthesisStarted;
        public event EventHandler? OnSpeechSynthesisEnded;
        public event EventHandler<string>? OnError;

        public SpeechService()
        {
            // 初始化语音识别引擎
            _recognitionEngine = new SpeechRecognitionEngine();
            _recognitionEngine.SetInputToDefaultAudioDevice(); // 设置默认音频输入设备
            _recognitionEngine.SpeechRecognized += RecognitionEngine_SpeechRecognized;
            _recognitionEngine.SpeechRecognitionRejected += RecognitionEngine_SpeechRecognitionRejected;
            _recognitionEngine.RecognizeCompleted += RecognitionEngine_RecognizeCompleted;

            // 初始化语音合成器
            _synthesizer = new SpeechSynthesizer();
            _synthesizer.SpeakStarted += Synthesizer_SpeakStarted;
            _synthesizer.SpeakCompleted += Synthesizer_SpeakCompleted;
        }

        // ASR 相关方法
        public void StartSpeechRecognition()
        {
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
        public void SynthesizeSpeech(string text, string? voiceName = null, int rate = 0, int volume = 100)
        {
            try
            {
                Logger.LogInfo($"合成语音: '{text}', 音色: '{voiceName}', 语速: {rate}, 音量: {volume}");
                // SelectVoiceByHints 的第四个参数是 CultureInfo，如果不需要特定文化，可以传入 null
                // 移除 VoicePreferredOver，直接根据名称选择音色
                if (!string.IsNullOrEmpty(voiceName))
                {
                    _synthesizer.SelectVoice(voiceName);
                }
                else
                {
                    // 如果没有指定音色，则使用默认音色设置
                    _synthesizer.SelectVoiceByHints(VoiceGender.NotSet, VoiceAge.NotSet);
                }
                _synthesizer.Rate = rate;
                _synthesizer.Volume = volume;
                _synthesizer.SpeakAsync(text);
            }
            catch (Exception ex)
            {
                Logger.LogError($"语音合成失败: {ex.Message}", ex);
                OnError?.Invoke(this, $"语音合成失败: {ex.Message}");
            }
        }

        public VoiceInfo[] GetAvailableVoices()
        {
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