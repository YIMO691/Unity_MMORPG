using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace MmoDemo.Client
{
    /// <summary>
    /// WebSocket client for Phase 2 real-time gameplay.
    /// Connects after Phase 1 HTTP login/role selection.
    /// </summary>
    public class WebSocketClient : IDisposable
    {
        private ClientWebSocket _socket;
        private readonly string _url;
        private readonly Queue<Action> _mainThreadActions = new();
        private CancellationTokenSource _cts;

        public bool IsConnected => _socket?.State == WebSocketState.Open;

        public event Action<string, string> OnMessage; // type, json
        public event Action OnConnected;
        public event Action<string> OnDisconnected;

        public WebSocketClient(string url)
        {
            _url = url;
        }

        public async Task ConnectAsync()
        {
            _cts = new CancellationTokenSource();
            _socket = new ClientWebSocket();
            await _socket.ConnectAsync(new Uri(_url), _cts.Token);
            Debug.Log($"[WS] Connected to {_url}");
            OnConnected?.Invoke();

            // Start receive loop
            _ = Task.Run(() => ReceiveLoop(_cts.Token));
        }

        public async Task SendAsync(string type, string payloadJson)
        {
            if (_socket?.State != WebSocketState.Open) return;
            var envelope = $"{{\"t\":\"{type}\",\"ts\":{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()},\"p\":{payloadJson}}}";
            var bytes = Encoding.UTF8.GetBytes(envelope);
            await _socket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, _cts.Token);
        }

        public void Update()
        {
            lock (_mainThreadActions)
            {
                while (_mainThreadActions.Count > 0)
                    _mainThreadActions.Dequeue()?.Invoke();
            }
        }

        private async Task ReceiveLoop(CancellationToken ct)
        {
            var buffer = new byte[4096];
            try
            {
                while (_socket?.State == WebSocketState.Open && !ct.IsCancellationRequested)
                {
                    var result = await _socket.ReceiveAsync(new ArraySegment<byte>(buffer), ct);
                    if (result.MessageType == WebSocketMessageType.Close) break;

                    var json = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    var (type, payload) = ParseEnvelope(json);
                    if (type != null)
                    {
                        lock (_mainThreadActions)
                            _mainThreadActions.Enqueue(() => OnMessage?.Invoke(type, payload ?? "{}"));
                    }
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception e) { Debug.LogError($"[WS] Receive error: {e.Message}"); }
            finally
            {
                lock (_mainThreadActions)
                    _mainThreadActions.Enqueue(() => OnDisconnected?.Invoke("Connection closed"));
            }
        }

        private static (string type, string payload) ParseEnvelope(string json)
        {
            try
            {
                // Quick manual parse to avoid JsonUtility limitations
                var tStart = json.IndexOf("\"t\":\"") + 5;
                var tEnd = json.IndexOf("\"", tStart);
                var type = json.Substring(tStart, tEnd - tStart);

                var pStart = json.IndexOf("\"p\":") + 4;
                var pEnd = json.LastIndexOf("}");
                var payload = json.Substring(pStart, pEnd - pStart);

                return (type, payload);
            }
            catch { return ("", "{}"); }
        }

        public async Task DisconnectAsync()
        {
            _cts?.Cancel();
            if (_socket?.State == WebSocketState.Open)
            {
                await _socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
            }
            _socket?.Dispose();
            _socket = null;
        }

        public void Dispose()
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _socket?.Dispose();
        }
    }
}
