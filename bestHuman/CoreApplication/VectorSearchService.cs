using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Text;
using System.Text.Json;

namespace CoreApplication
{
    public class TextChunk
    {
        public string Text { get; set; } = "";
        public string Source { get; set; } = "";
        public int Position { get; set; }
        public float[]? Vector { get; set; }
    }

    public class ChunkingConfig
    {
        public int MaxChunkSize { get; set; } = 512;
        public int OverlapSize { get; set; } = 50;
        public bool RespectParagraphs { get; set; } = true;
    }

    public class VectorSearchService
    {
        private readonly List<TextChunk> _chunks = new();
        private readonly ChunkingConfig _chunkingConfig;
        private readonly int _embeddingDimension = 384;

        public VectorSearchService(ChunkingConfig? config = null)
        {
            _chunkingConfig = config ?? new ChunkingConfig();
        }

        public List<TextChunk> ChunkText(string text, string source)
        {
            var chunks = new List<TextChunk>();
            var position = 0;

            if (_chunkingConfig.RespectParagraphs)
            {
                var paragraphs = text.Split(
                    new[] { "\r\n\r\n", "\n\n" },
                    StringSplitOptions.RemoveEmptyEntries
                );

                foreach (var paragraph in paragraphs)
                {
                    if (paragraph.Length <= _chunkingConfig.MaxChunkSize)
                    {
                        chunks.Add(new TextChunk
                        {
                            Text = paragraph,
                            Source = source,
                            Position = position
                        });
                        position += paragraph.Length;
                    }
                    else
                    {
                        var paragraphChunks = ChunkLongText(paragraph, position);
                        foreach (var chunk in paragraphChunks)
                        {
                            chunk.Source = source;
                        }
                        chunks.AddRange(paragraphChunks);
                        position += paragraph.Length;
                    }
                }
            }
            else
            {
                chunks.AddRange(ChunkLongText(text, 0));
                foreach (var chunk in chunks)
                {
                    chunk.Source = source;
                }
            }

            return chunks;
        }

        private List<TextChunk> ChunkLongText(string text, int startPosition)
        {
            var chunks = new List<TextChunk>();
            var position = startPosition;
            var remainingText = text;

            while (remainingText.Length > 0)
            {
                var chunkSize = Math.Min(_chunkingConfig.MaxChunkSize, remainingText.Length);
                var chunk = remainingText[..chunkSize];

                if (chunkSize == _chunkingConfig.MaxChunkSize && chunk.Length > 0)
                {
                    var lastSentenceEnd = FindLastSentenceEnd(chunk);
                    if (lastSentenceEnd > 0)
                    {
                        chunkSize = lastSentenceEnd + 1;
                        chunk = remainingText[..chunkSize];
                    }
                }

                chunks.Add(new TextChunk
                {
                    Text = chunk,
                    Position = position
                });

                var advanceSize = Math.Max(chunkSize - _chunkingConfig.OverlapSize, 1);
                remainingText = remainingText[advanceSize..];
                position += advanceSize;
            }

            return chunks;
        }

        private static int FindLastSentenceEnd(string text)
        {
            var lastPeriod = text.LastIndexOf('。');
            var lastExclamation = text.LastIndexOf('！');
            var lastQuestion = text.LastIndexOf('？');
            var lastComma = text.LastIndexOf('，');
            
            var candidates = new[] { lastPeriod, lastExclamation, lastQuestion, lastComma };
            return candidates.Where(pos => pos > 0).DefaultIfEmpty(0).Max();
        }

        private float CalculateCosineSimilarity(float[] vector1, float[] vector2)
        {
            if (vector1.Length != vector2.Length)
            {
                throw new ArgumentException("向量维度不匹配");
            }

            float dotProduct = 0;
            float norm1 = 0;
            float norm2 = 0;

            for (int i = 0; i < vector1.Length; i++)
            {
                dotProduct += vector1[i] * vector2[i];
                norm1 += vector1[i] * vector1[i];
                norm2 += vector2[i] * vector2[i];
            }

            return dotProduct / (float)(Math.Sqrt(norm1) * Math.Sqrt(norm2));
        }

        public async Task LoadDocumentAsync(string filePath)
        {
            var text = await System.IO.File.ReadAllTextAsync(filePath);
            var source = System.IO.Path.GetFileName(filePath);
            await LoadDocumentAsync(text, source);
        }

        public Task LoadDocumentAsync(string text, string source)
        {
            var chunks = ChunkText(text, source);
            var random = new Random(42);

            foreach (var chunk in chunks)
            {
                chunk.Vector = Enumerable.Range(0, _embeddingDimension)
                    .Select(_ => (float)random.NextDouble())
                    .ToArray();
            }

            _chunks.AddRange(chunks);
            Logger.LogInfo($"已加载文档 {source}，共 {chunks.Count} 个分块");
            
            return Task.CompletedTask;
        }

        private class SearchResult
        {
            public TextChunk Chunk { get; set; } = null!;
            public float Score { get; set; }
        }

        public Task<List<TextChunk>> SearchAsync(string query, int topK = 3)
        {
            return Task.Run(() =>
            {
                var random = new Random(42);
                var queryVector = Enumerable.Range(0, _embeddingDimension)
                    .Select(_ => (float)random.NextDouble())
                    .ToArray();

                var results = new List<SearchResult>();
                foreach (var chunk in _chunks)
                {
                    if (chunk.Vector == null) continue;
                    results.Add(new SearchResult
                    {
                        Chunk = chunk,
                        Score = CalculateCosineSimilarity(queryVector, chunk.Vector)
                    });
                }

                var topResults = results
                    .OrderByDescending(r => r.Score)
                    .Take(topK)
                    .Select(r => r.Chunk)
                    .ToList();

                Logger.LogInfo(string.Format("查询\"{0}\"返回 {1} 个相关文本块", query, topResults.Count));
                return topResults;
            });
        }

        public string AssembleContext(List<TextChunk> chunks)
        {
            chunks = chunks.OrderBy(c => c.Position).ToList();

            var context = new StringBuilder();
            foreach (var chunk in chunks)
            {
                context.AppendLine($"来源：{chunk.Source}");
                context.AppendLine(chunk.Text);
                context.AppendLine();
            }

            return context.ToString().TrimEnd();
        }
    }
}