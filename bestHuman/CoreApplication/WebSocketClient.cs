using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CoreApplication; // For Logger

namespace CoreApplication
{
    public class WebSocketClient : IDisposable
    {
        private ClientWebSocket? _webSocket;
        private CancellationTokenSource? _cancellationTokenSource;
        private string? _serverUri;

        public event EventHandler? OnConnected;
        public event EventHandler? OnDisconnected;
        public event EventHandler<string>? OnMessageReceived;
        public event EventHandler<string>? OnError;

        public bool IsConnected => _webSocket != null && _webSocket.State == WebSocketState.Open;

        public WebSocketClient()
        {
            InitializeWebSocket();
        }

        private void InitializeWebSocket()
        {
            _webSocket?.Dispose();
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();

            _webSocket = new ClientWebSocket();
            _cancellationTokenSource = new CancellationTokenSource();
        }

        public async Task ConnectAsync(string uri)
        {
            if (IsConnected)
            {
                await DisconnectAsync();
            }

            InitializeWebSocket();
            _serverUri = uri;

            try
            {
                if (_webSocket == null || _cancellationTokenSource == null)
                {
                    throw new InvalidOperationException("WebSocket not initialized");
                }

                Logger.LogInfo($"尝试连接到 WebSocket 服务器: {_serverUri}");
                await _webSocket.ConnectAsync(new Uri(_serverUri), _cancellationTokenSource.Token);
                Logger.LogInfo("WebSocket 连接成功。");
                OnConnected?.Invoke(this, EventArgs.Empty);
                _ = ReceiveMessagesAsync(); // 开始接收消息
            }
            catch (WebSocketException ex)
            {
                Logger.LogError($"WebSocket 连接失败: {ex.Message}", ex);
                OnError?.Invoke(this, $"连接失败: {ex.Message}");
            }
            catch (Exception ex)
            {
                Logger.LogError($"连接过程中发生未知错误: {ex.Message}", ex);
                OnError?.Invoke(this, $"未知错误: {ex.Message}");
            }
        }

        public async Task DisconnectAsync()
        {
            if (_webSocket.State == WebSocketState.Open || _webSocket.State == WebSocketState.Connecting)
            {
                try
                {
                    Logger.LogInfo("尝试断开 WebSocket 连接。");
                    _cancellationTokenSource.Cancel(); // 取消接收循环
                    await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Client initiated disconnect", CancellationToken.None);
                    Logger.LogInfo("WebSocket 连接已断开。");
                    OnDisconnected?.Invoke(this, EventArgs.Empty);
                }
                catch (Exception ex)
                {
                    Logger.LogError($"断开连接过程中发生错误: {ex.Message}", ex);
                    OnError?.Invoke(this, $"断开连接错误: {ex.Message}");
                }
            }
        }

        public async Task SendMessageAsync(string message)
        {
            if (_webSocket.State == WebSocketState.Open)
            {
                try
                {
                    byte[] buffer = Encoding.UTF8.GetBytes(message);
                    await _webSocket.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, _cancellationTokenSource.Token);
                    Logger.LogInfo($"发送 WebSocket 消息: {message}");
                }
                catch (Exception ex)
                {
                    Logger.LogError($"发送 WebSocket 消消息失败: {ex.Message}", ex);
                    OnError?.Invoke(this, $"发送消息失败: {ex.Message}");
                }
            }
            else
            {
                Logger.LogWarning("WebSocket 未连接，无法发送消息。");
                OnError?.Invoke(this, "WebSocket 未连接。");
            }
        }

        private async Task ReceiveMessagesAsync()
        {
            byte[] buffer = new byte[1024 * 4]; // 4KB 缓冲区
            try
            {
                while (_webSocket.State == WebSocketState.Open && !_cancellationTokenSource.IsCancellationRequested)
                {
                    WebSocketReceiveResult result = await _webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), _cancellationTokenSource.Token);

                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        Logger.LogInfo("WebSocket 接收到关闭消息。");
                        await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Server initiated disconnect", CancellationToken.None);
                        OnDisconnected?.Invoke(this, EventArgs.Empty);
                    }
                    else if (result.MessageType == WebSocketMessageType.Text)
                    {
                        string receivedMessage = Encoding.UTF8.GetString(buffer, 0, result.Count);
                        Logger.LogInfo($"接收到 WebSocket 消息: {receivedMessage}");
                        OnMessageReceived?.Invoke(this, receivedMessage);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                Logger.LogInfo("WebSocket 消息接收操作已取消。");
            }
            catch (Exception ex)
            {
                Logger.LogError($"接收 WebSocket 消息时发生错误: {ex.Message}", ex);
                OnError?.Invoke(this, $"接收消息错误: {ex.Message}");
            }
            finally
            {
                if (_webSocket.State != WebSocketState.Closed)
                {
                    await DisconnectAsync();
                }
            }
        }

        public void Dispose()
        {
            _cancellationTokenSource?.Cancel();
            _webSocket?.Dispose();
            _cancellationTokenSource?.Dispose();
        }
    }
}