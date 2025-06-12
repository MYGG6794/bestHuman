using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using System.Linq;
using System.Text;

namespace CoreApplication
{
    /// <summary>
    /// DeepSeek Distillation 7B 模型的推理包装器
    /// </summary>
    public class ModelInference : IDisposable
    {
        private readonly InferenceSession _session;
        private readonly string _modelPath;
        private const int MaxInputLength = 2048; // 模型最大输入长度
        private const int MaxOutputLength = 512; // 生成文本的最大长度
        private Dictionary<string, int>? _tokenizer; // 词元映射表

        public ModelInference(string modelPath, bool useGPU = false)
        {
            var sessionOptions = new SessionOptions();
            if (useGPU)
            {
                // 配置GPU执行提供程序
                sessionOptions.AppendExecutionProvider_CUDA();
            }

            _modelPath = modelPath;
            _session = new InferenceSession(modelPath, sessionOptions);
            LoadTokenizer();
        }

        /// <summary>
        /// 加载词元映射表
        /// </summary>
        private void LoadTokenizer()
        {
            try
            {
                // 从模型同目录下加载词元映射文件
                string tokenizerPath = System.IO.Path.Combine(
                    System.IO.Path.GetDirectoryName(_modelPath) ?? "",
                    "tokenizer.json"
                );
                string json = System.IO.File.ReadAllText(tokenizerPath);
                _tokenizer = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, int>>(json);

                if (_tokenizer == null)
                {
                    throw new InvalidOperationException("词元映射表加载失败");
                }

                Logger.LogInfo($"词元映射表加载成功，包含 {_tokenizer.Count} 个词元");
            }
            catch (Exception ex)
            {
                Logger.LogError($"加载词元映射表失败: {ex.Message}", ex);
                throw;
            }
        }

        /// <summary>
        /// 对输入文本进行分词
        /// </summary>
        private List<int> Tokenize(string text)
        {
            if (_tokenizer == null)
            {
                throw new InvalidOperationException("词元映射表未初始化");
            }

            var tokens = new List<int>();
            var words = text.Split(' '); // 简单的按空格分词，实际需要更复杂的分词算法

            foreach (var word in words)
            {
                if (_tokenizer.TryGetValue(word.ToLower(), out int tokenId))
                {
                    tokens.Add(tokenId);
                }
                else
                {
                    // 处理未知词，可以使用特殊的 [UNK] token 或子词分词
                    tokens.Add(_tokenizer["[UNK]"]);
                }
            }

            return tokens;
        }

        /// <summary>
        /// 将词元ID转换回文本
        /// </summary>
        private string Detokenize(List<int> tokens)
        {
            if (_tokenizer == null)
            {
                throw new InvalidOperationException("词元映射表未初始化");
            }

            var inverseTokenizer = _tokenizer.ToDictionary(x => x.Value, x => x.Key);
            var words = tokens.Select(t => inverseTokenizer.TryGetValue(t, out string? word) ? word : "[UNK]");
            return string.Join(" ", words);
        }

        /// <summary>
        /// 执行模型推理
        /// </summary>
        public async Task<string> InferAsync(string input)
        {
            try
            {
                // 1. 对输入文本进行分词
                var inputTokens = Tokenize(input);
                if (inputTokens.Count > MaxInputLength)
                {
                    inputTokens = inputTokens.Take(MaxInputLength).ToList();
                }

                // 2. 创建输入张量
                var inputTensor = new DenseTensor<long>(
                    inputTokens.Select(x => (long)x).ToArray(),
                    new[] { 1, inputTokens.Count } // [batch_size, sequence_length]
                );

                // 3. 准备模型输入
                var inputs = new List<NamedOnnxValue>
                {
                    NamedOnnxValue.CreateFromTensor("input_ids", inputTensor)
                };

                // 4. 执行推理
                using var outputs = await Task.Run(() => _session.Run(inputs));
                
                // 5. 处理输出
                var outputTensor = outputs.First().AsTensor<long>();
                var outputTokens = outputTensor.ToArray()
                    .Select(x => (int)x)
                    .Where(x => x > 0) // 过滤填充token
                    .Take(MaxOutputLength)
                    .ToList();

                // 6. 将输出词元转换回文本
                return Detokenize(outputTokens);
            }
            catch (Exception ex)
            {
                Logger.LogError($"模型推理失败: {ex.Message}", ex);
                throw;
            }
        }

        /// <summary>
        /// 获取模型信息
        /// </summary>
        public ModelInfo GetModelInfo()
        {
            return new ModelInfo
            {
                Name = System.IO.Path.GetFileName(_modelPath),
                InputNames = _session.InputMetadata.Keys.ToList(),
                OutputNames = _session.OutputMetadata.Keys.ToList(),
                InputShapes = _session.InputMetadata.Values
                    .Select(m => string.Join("x", m.Dimensions))
                    .ToList(),
                TokenizerVocabSize = _tokenizer?.Count ?? 0
            };
        }

        public void Dispose()
        {
            _session.Dispose();
            GC.SuppressFinalize(this);
        }
    }

    /// <summary>
    /// 模型信息
    /// </summary>
    public class ModelInfo
    {
        public string Name { get; set; } = "";
        public List<string> InputNames { get; set; } = new();
        public List<string> OutputNames { get; set; } = new();
        public List<string> InputShapes { get; set; } = new();
        public int TokenizerVocabSize { get; set; }
    }
}