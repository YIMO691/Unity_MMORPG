using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MmoDemo.Client
{
    public class ChatPanel : MonoBehaviour
    {
        private Text _chatLog;
        private InputField _inputField;
        private Button _sendButton;
        private GameManager _gm;
        private readonly List<string> _messages = new();
        private readonly List<string> _pendingLocalEchoes = new();
        private const int MaxMessages = 20;

        public void SetUI(Text chatLog, InputField inputField, Button sendButton)
        {
            _chatLog = chatLog;
            _inputField = inputField;
            _sendButton = sendButton;
            if (_sendButton != null)
            {
                _sendButton.onClick.RemoveListener(Send);
                _sendButton.onClick.AddListener(Send);
            }
        }

        private void Start()
        {
            BindGameManager();
        }

        private void Update()
        {
            if (_gm == null)
                BindGameManager();

            if (Input.GetKeyDown(KeyCode.Return) && _inputField != null && _inputField.isFocused)
                Send();
        }

        public void Send()
        {
            var text = _inputField?.text?.Trim();
            if (string.IsNullOrEmpty(text)) return;
            if (_gm == null)
                BindGameManager();

            if (_gm != null)
            {
                _gm.SendChat(text);
                AddLocalEcho(text);
            }

            _inputField.text = "";
            _inputField.DeactivateInputField();
            EventSystem.current?.SetSelectedGameObject(null);
        }

        private void OnChat(string sender, string text)
        {
            if (sender != "System" && TryConfirmLocalEcho(sender, text))
                return;

            AddMessage(sender, text);
        }

        private void BindGameManager()
        {
            var gm = FindObjectOfType<GameManager>();
            if (gm == null || gm == _gm) return;

            if (_gm != null) _gm.OnChatReceived -= OnChat;
            _gm = gm;
            _gm.OnChatReceived += OnChat;
        }

        private void AddLocalEcho(string text)
        {
            _pendingLocalEchoes.Add(text);
            while (_pendingLocalEchoes.Count > MaxMessages)
                _pendingLocalEchoes.RemoveAt(0);

            AddMessage("Me", text);
        }

        private bool TryConfirmLocalEcho(string sender, string text)
        {
            var pendingIndex = _pendingLocalEchoes.LastIndexOf(text);
            if (pendingIndex < 0)
                return false;

            _pendingLocalEchoes.RemoveAt(pendingIndex);
            var localEcho = $"[Me]: {text}";
            for (var i = _messages.Count - 1; i >= 0; i--)
            {
                if (_messages[i] != localEcho) continue;
                _messages[i] = $"[{sender}]: {text}";
                RefreshLog();
                return true;
            }

            return false;
        }

        private void AddMessage(string sender, string text)
        {
            _messages.Add($"[{sender}]: {text}");
            while (_messages.Count > MaxMessages) _messages.RemoveAt(0);
            RefreshLog();
        }

        private void RefreshLog()
        {
            if (_chatLog != null) _chatLog.text = string.Join("\n", _messages);
        }

        private void OnDestroy()
        {
            if (_gm != null) _gm.OnChatReceived -= OnChat;
        }
    }
}
