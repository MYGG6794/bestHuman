using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text.Json;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;

namespace CoreApplication
{
    /// <summary>
    /// 知识库条目
    /// </summary>
    public class KnowledgeEntry
    {
        /// <summary>
        /// 问题或关键词
        /// </summary>
        public string Question { get; set; } = "";

        /// <summary>
        /// 答案或相关内容
        /// </summary>
        public string Answer { get; set; } = "";

        /// <summary>
        /// 相关性得分（用于RAG检索）
        /// </summary>
        public float Relevance { get; set; }

        /// <summary>
        /// 最后更新时间
        /// </summary>
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
    }

    /// <summary>
    /// AI服务配置
    /// </summary>
    public class AIServiceConfig
    {
        /// <summary>
        /// 模型文件路径
        /// </summary>
        public string ModelPath { get; set; } = "";

        /// <summary>
        /// 知识库文件路径
        /// </summary>
        public string KnowledgeBasePath { get; set; } = "";

        /// <summary>
        /// 是否启用GPU加速
        /// </summary>
        public bool UseGPU { get; set; } = false;

        /// <summary>
        /// 回退到云端API开关
        /// </summary>
        public bool EnableCloudFallback { get; set; } = false;

        /// <summary>
        /// 云端API密钥（如果启用回退）
        /// </summary>
        public string? CloudAPIKey { get; set; }

        /// <summary>
        /// 云端API地址（如果启用回退）
        /// </summary>
        public string? CloudAPIEndpoint { get; set; }
    }

    /// <summary>
    /// 本地AI与知识库服务类
    /// </summary>
    public class AIService : IDisposable
    {
        private readonly WebSocketClient _webSocketClient;
        private ModelInference? _model;
        private List<KnowledgeEntry> _knowledgeBase;
        private readonly AIServiceConfig _config;
        private ModelInfo? _modelInfo;
        private readonly VectorSearchService _vectorSearch;

        public event EventHandler<string>? OnAIResponse;
        public event EventHandler<string>? OnError;

        public AIService(WebSocketClient webSocketClient, AIServiceConfig config)
        {
            _webSocketClient = webSocketClient;
            _config = config;
            _knowledgeBase = new List<KnowledgeEntry>();
            _vectorSearch = new VectorSearchService(new ChunkingConfig
            {
                MaxChunkSize = 512,
                OverlapSize = 50,
                RespectParagraphs = true
            });

            // 订阅WebSocket消息，处理来自UE的AI请求
            _webSocketClient.OnMessageReceived += WebSocketClient_OnMessageReceived;
        }

        /// <summary>
        /// 初始化AI服务，加载模型和知识库
        /// </summary>
        public async Task InitializeAsync()
        {
            try
            {
                Logger.LogInfo("正在初始化AI服务...");

                // 初始化模型
                _model = new ModelInference(_config.ModelPath, _config.UseGPU);
                _modelInfo = _model.GetModelInfo();
                Logger.LogInfo($"模型[{_modelInfo.Name}]加载完成，词汇表大小：{_modelInfo.TokenizerVocabSize}");

                // 加载知识库
                await LoadKnowledgeBaseAsync(_config.KnowledgeBasePath);
                Logger.LogInfo("知识库加载完成。");
            }
            catch (Exception ex)
            {
                Logger.LogError($"AI服务初始化失败: {ex.Message}", ex);
                OnError?.Invoke(this, $"AI服务初始化失败: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 加载知识库
        /// </summary>
        public async Task LoadKnowledgeBaseAsync(string path)
        {
            try
            {
                // 加载知识库条目
                string jsonContent = await System.IO.File.ReadAllTextAsync(path);
                _knowledgeBase = JsonSerializer.Deserialize<List<KnowledgeEntry>>(jsonContent)
                    ?? new List<KnowledgeEntry>();
                Logger.LogInfo($"已加载知识库条目：{_knowledgeBase.Count} 条");

                // 将每个条目转换为文本块并加载到向量搜索服务
                foreach (var entry in _knowledgeBase)
                {
                    string content = $"Q: {entry.Question}\nA: {entry.Answer}";
                    await _vectorSearch.LoadDocumentAsync(content, entry.Question);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"加载知识库失败: {ex.Message}", ex);
                OnError?.Invoke(this, $"加载知识库失败: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 更新知识库
        /// </summary>
        public async Task UpdateKnowledgeBaseAsync(string path, KnowledgeEntry newEntry)
        {
            try
            {
                _knowledgeBase.Add(newEntry);
                string jsonContent = JsonSerializer.Serialize(_knowledgeBase, new JsonSerializerOptions 
                { 
                    WriteIndented = true 
                });
                await System.IO.File.WriteAllTextAsync(path, jsonContent);
                Logger.LogInfo("知识库已更新。");
            }
            catch (Exception ex)
            {
                Logger.LogError($"更新知识库失败: {ex.Message}", ex);
                OnError?.Invoke(this, $"更新知识库失败: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 获取相关的知识库条目
        /// </summary>
        private async Task<List<TextChunk>> RetrieveRelevantChunks(string question, int topK = 3)
        {
            return await _vectorSearch.SearchAsync(question, topK);
        }

        /// <summary>
        /// 处理来自UE的AI请求
        /// </summary>
        private void WebSocketClient_OnMessageReceived(object? sender, string message)
        {
            try
            {
                // 解析消息类型
                var messageObj = JsonSerializer.Deserialize<JsonElement>(message);
                if (messageObj.TryGetProperty("type", out var typeElement) && 
                    typeElement.GetString() == "ai_request")
                {
                    var question = messageObj.GetProperty("data").GetProperty("question").GetString();
                    if (question != null)
                    {
                        _ = HandleQuestionAsync(question);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"处理AI请求消息失败: {ex.Message}", ex);
                OnError?.Invoke(this, $"处理AI请求消息失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 处理用户问题
        /// </summary>
        public async Task HandleQuestionAsync(string question)
        {
            try
            {
                // 1. 从知识库检索相关文本块
                var relevantChunks = await RetrieveRelevantChunks(question);
                
                // 2. 构建提示词，包含相关上下文
                string context = _vectorSearch.AssembleContext(relevantChunks);
                string prompt = $"基于以下上下文回答问题：\n\n{context}\n\n问题：{question}\n回答：";

                // 3. 模型推理
                string response = await InferAsync(prompt);

                // 4. 通过WebSocket发送回答
                var responseMessage = new
                {
                    type = "ai_response",
                    data = new
                    {
                        question,
                        answer = response
                    }
                };
                string jsonResponse = JsonSerializer.Serialize(responseMessage);
                await _webSocketClient.SendMessageAsync(jsonResponse);

                // 5. 触发回答事件
                OnAIResponse?.Invoke(this, response);
            }
            catch (Exception ex)
            {
                Logger.LogError($"处理问题失败: {ex.Message}", ex);
                OnError?.Invoke(this, $"处理问题失败: {ex.Message}");

                // 如果启用了云端回退，尝试使用云端API
                if (_config.EnableCloudFallback)
                {
                    await FallbackToCloudAPIAsync(question);
                }
            }
        }

        /// <summary>
        /// 模型推理
        /// </summary>
        private async Task<string> InferAsync(string input)
        {
            if (_model == null)
            {
                throw new InvalidOperationException("模型未初始化");
            }
            return await _model.InferAsync(input);
        }

        /// <summary>
        /// 回退到云端API
        /// </summary>
        private async Task FallbackToCloudAPIAsync(string question)
        {
            if (string.IsNullOrEmpty(_config.CloudAPIKey) || string.IsNullOrEmpty(_config.CloudAPIEndpoint))
            {
                throw new InvalidOperationException("云端API配置无效");
            }

            // TODO: 实现调用云端API的逻辑
            // 例如：调用OpenAI或百度文心一言API
            await Task.CompletedTask;
        }

        public void Dispose()
        {
            _model?.Dispose();
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// 获取模型信息
        /// </summary>
        public ModelInfo? GetModelInfo()
        {
            return _modelInfo;
        }
    }
}