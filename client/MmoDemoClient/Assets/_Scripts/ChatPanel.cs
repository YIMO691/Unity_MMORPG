using System.Collections.Generic;
using UnityEngine;
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
        private const int MaxMessages = 20;

        public void SetUI(Text chatLog, InputField inputField, Button sendButton)
        {
            _chatLog = chatLog;
            _inputField = inputField;
            _sendButton = sendButton;
            _sendButton.onClick.AddListener(Send);
        }

        private void Start()
        {
            _gm = FindObjectOfType<GameManager>();
            if (_gm != null) _gm.OnChatReceived += OnChat;
        }

        private void Update()
        {
            if (_gm == null)
            {
                _gm = FindObjectOfType<GameManager>();
                if (_gm != null) _gm.OnChatReceived += OnChat;
            }
            if (Input.GetKeyDown(KeyCode.Return) && _inputField != null && _inputField.isFocused)
                Send();
        }

        public void Send()
        {
            var text = _inputField?.text?.Trim();
            if (string.IsNullOrEmpty(text)) return;
            if (_gm == null)
            {
                _gm = FindObjectOfType<GameManager>();
                if (_gm != null) _gm.OnChatReceived += OnChat;
            }
            _gm?.SendChat(text);
            _inputField.text = "";
        }

        private void OnChat(string sender, string text)
        {
            _messages.Add($"[{sender}]: {text}");
            while (_messages.Count > MaxMessages) _messages.RemoveAt(0);
            if (_chatLog != null) _chatLog.text = string.Join("\n", _messages);
        }

        private void OnDestroy()
        {
            if (_gm != null) _gm.OnChatReceived -= OnChat;
        }
    }
}
