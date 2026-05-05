using UnityEngine;
using UnityEngine.UI;

namespace MmoDemo.Client
{
    public class CityView : MonoBehaviour
    {
        [SerializeField] private Text nameText;
        [SerializeField] private Text levelText;
        [SerializeField] private Text goldText;
        [SerializeField] private Text statusText;

        private NetworkManager _network;
        private ChatPanel _chatPanel;
        private QuestTracker _questTracker;

        public void Init(NetworkManager network, string roleId)
        {
            _network = network;
            statusText.text = "Entering city...";

            // Phase 4: Create chat + quest UI at runtime
            BuildChatPanel();
            BuildQuestTracker();

            StartCoroutine(_network.EnterCity(roleId, result => OnCityEntered(result, roleId), OnError));
        }

        private void BuildChatPanel()
        {
            var chatGo = new GameObject("ChatPanel", typeof(RectTransform));
            chatGo.transform.SetParent(transform, false);
            var chatRt = chatGo.GetComponent<RectTransform>();
            chatRt.anchorMin = new Vector2(0, 0);
            chatRt.anchorMax = new Vector2(1, 1);
            chatRt.offsetMin = Vector2.zero;
            chatRt.offsetMax = Vector2.zero;

            // Chat log (bottom-left area)
            var logGo = new GameObject("ChatLog", typeof(RectTransform), typeof(Text));
            logGo.transform.SetParent(chatGo.transform, false);
            var logRt = logGo.GetComponent<RectTransform>();
            logRt.anchorMin = new Vector2(0, 0);
            logRt.anchorMax = new Vector2(0.4f, 0.35f);
            logRt.offsetMin = new Vector2(10, 10);
            logRt.offsetMax = new Vector2(0, 0);
            var logText = logGo.GetComponent<Text>();
            logText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            logText.fontSize = 14;
            logText.color = Color.white;
            logText.alignment = TextAnchor.LowerLeft;

            // Input field (bottom-left, below log)
            var inputGo = new GameObject("ChatInput", typeof(RectTransform), typeof(Image), typeof(InputField));
            inputGo.transform.SetParent(chatGo.transform, false);
            var inputRt = inputGo.GetComponent<RectTransform>();
            inputRt.anchorMin = new Vector2(0, 0);
            inputRt.anchorMax = new Vector2(0.4f, 0);
            inputRt.offsetMin = new Vector2(10, 216);
            inputRt.offsetMax = new Vector2(-60, 246);
            inputGo.GetComponent<Image>().color = new Color(0.15f, 0.15f, 0.2f);
            var inputField = inputGo.GetComponent<InputField>();

            var inputTxtGo = new GameObject("Text", typeof(RectTransform), typeof(Text));
            inputTxtGo.transform.SetParent(inputGo.transform, false);
            var inputTxt = inputTxtGo.GetComponent<Text>();
            inputTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            inputTxt.fontSize = 14;
            inputTxt.color = Color.white;
            inputTxt.alignment = TextAnchor.MiddleLeft;
            inputTxt.rectTransform.anchorMin = Vector2.zero;
            inputTxt.rectTransform.anchorMax = Vector2.one;
            inputTxt.rectTransform.offsetMin = new Vector2(5, 2);
            inputTxt.rectTransform.offsetMax = new Vector2(-5, -2);
            inputField.textComponent = inputTxt;

            // Send button
            var sendGo = new GameObject("SendBtn", typeof(RectTransform), typeof(Image), typeof(Button));
            sendGo.transform.SetParent(chatGo.transform, false);
            var sendRt = sendGo.GetComponent<RectTransform>();
            sendRt.anchorMin = new Vector2(0, 0);
            sendRt.anchorMax = new Vector2(0.4f, 0);
            sendRt.offsetMin = new Vector2(330, 216);
            sendRt.offsetMax = new Vector2(390, 246);
            sendGo.GetComponent<Image>().color = new Color(0.2f, 0.6f, 0.2f);
            var sendBtn = sendGo.GetComponent<Button>();

            var sendTxtGo = new GameObject("Label", typeof(RectTransform), typeof(Text));
            sendTxtGo.transform.SetParent(sendGo.transform, false);
            var sendTxt = sendTxtGo.GetComponent<Text>();
            sendTxt.text = "Send";
            sendTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            sendTxt.fontSize = 14;
            sendTxt.color = Color.white;
            sendTxt.alignment = TextAnchor.MiddleCenter;
            sendTxt.rectTransform.anchorMin = Vector2.zero;
            sendTxt.rectTransform.anchorMax = Vector2.one;
            sendTxt.rectTransform.sizeDelta = Vector2.zero;

            _chatPanel = chatGo.AddComponent<ChatPanel>();
            _chatPanel.SetUI(logText, inputField, sendBtn);
        }

        private void BuildQuestTracker()
        {
            var questGo = new GameObject("QuestTracker", typeof(RectTransform));
            questGo.transform.SetParent(transform, false);
            var questRt = questGo.GetComponent<RectTransform>();
            questRt.anchorMin = new Vector2(0, 0);
            questRt.anchorMax = new Vector2(1, 1);
            questRt.offsetMin = Vector2.zero;
            questRt.offsetMax = Vector2.zero;

            // Status text (top-right)
            var statusGo = new GameObject("QuestStatus", typeof(RectTransform), typeof(Text));
            statusGo.transform.SetParent(questGo.transform, false);
            var statusRt = statusGo.GetComponent<RectTransform>();
            statusRt.anchorMin = new Vector2(0.6f, 0.8f);
            statusRt.anchorMax = new Vector2(1, 0.95f);
            statusRt.offsetMin = Vector2.zero;
            statusRt.offsetMax = new Vector2(-10, 0);
            var questText = statusGo.GetComponent<Text>();
            questText.text = "Select a quest:";
            questText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            questText.fontSize = 16;
            questText.color = Color.yellow;
            questText.alignment = TextAnchor.UpperRight;

            // Quest buttons (top-right, stacked)
            var q1 = CreateQuestButton(questGo.transform, "Slime x3", 0.74f);
            var q2 = CreateQuestButton(questGo.transform, "Goblins x2", 0.68f);
            var q3 = CreateQuestButton(questGo.transform, "Wolf x1", 0.62f);

            _questTracker = questGo.AddComponent<QuestTracker>();
            _questTracker.SetUI(questText, q1, q2, q3);
        }

        private Button CreateQuestButton(Transform parent, string label, float yAnchor)
        {
            var go = new GameObject("QuestBtn", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.75f, yAnchor);
            rt.anchorMax = new Vector2(0.95f, yAnchor + 0.05f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            go.GetComponent<Image>().color = new Color(0.3f, 0.3f, 0.5f);
            var btn = go.GetComponent<Button>();

            var txtGo = new GameObject("Label", typeof(RectTransform), typeof(Text));
            txtGo.transform.SetParent(go.transform, false);
            var txt = txtGo.GetComponent<Text>();
            txt.text = label;
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.fontSize = 14;
            txt.color = Color.white;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.rectTransform.anchorMin = Vector2.zero;
            txt.rectTransform.anchorMax = Vector2.one;
            txt.rectTransform.sizeDelta = Vector2.zero;

            return btn;
        }

        private void OnCityEntered(EnterCityResult result, string roleId)
        {
            if (result.role != null)
            {
                nameText.text = result.role.name;
                levelText.text = $"Level {result.role.level}";
                goldText.text = $"Gold: {result.role.gold:N0}";
                statusText.text = "Connecting to game server...";

                var gm = FindObjectOfType<GameManager>();
                if (gm != null)
                {
                    gm.Connect(_network.PlayerId, _network.Token, roleId);
                }
                else
                {
                    statusText.text = "Welcome to the city! (WebSocket not available)";
                }
            }
        }

        private void OnError(string error)
        {
            statusText.text = $"Error: {error}";
        }
    }
}
