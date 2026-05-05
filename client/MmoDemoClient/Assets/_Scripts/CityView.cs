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
            // Remove any old copy (from prefab or previous init)
            var existing = transform.Find("ChatPanel");
            if (existing != null) Destroy(existing.gameObject);

            // Container fills parent canvas
            var chatGo = NewUIChild("ChatPanel", transform);
            Stretch(chatGo);

            // Chat log: bottom-left, 250x130
            var logGo = NewUIChild("ChatLog", chatGo.transform);
            var logText = logGo.AddComponent<Text>();
            logText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            logText.fontSize = 14;
            logText.color = Color.white;
            logText.alignment = TextAnchor.LowerLeft;
            Position(logGo, new Vector2(-390, -230), new Vector2(250, 130));

            // Input background: bottom-left, 180x28
            var inputGo = NewUIChild("ChatInput", chatGo.transform);
            inputGo.AddComponent<Image>().color = new Color(0.15f, 0.15f, 0.2f);
            var inputField = inputGo.AddComponent<InputField>();
            Position(inputGo, new Vector2(-390, -275), new Vector2(180, 28));

            // Input text
            var inputTxtGo = NewUIChild("Text", inputGo.transform);
            var inputTxt = inputTxtGo.AddComponent<Text>();
            inputTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            inputTxt.fontSize = 14;
            inputTxt.color = Color.white;
            inputTxt.alignment = TextAnchor.MiddleLeft;
            FillParent(inputTxtGo);
            inputField.textComponent = inputTxt;

            // Send button
            var sendGo = NewUIChild("SendBtn", chatGo.transform);
            sendGo.AddComponent<Image>().color = new Color(0.2f, 0.6f, 0.2f);
            var sendBtn = sendGo.AddComponent<Button>();
            Position(sendGo, new Vector2(-200, -275), new Vector2(55, 28));

            var sendTxtGo = NewUIChild("Label", sendGo.transform);
            var sendTxt = sendTxtGo.AddComponent<Text>();
            sendTxt.text = "Send";
            sendTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            sendTxt.fontSize = 13;
            sendTxt.color = Color.white;
            sendTxt.alignment = TextAnchor.MiddleCenter;
            FillParent(sendTxtGo);

            _chatPanel = chatGo.AddComponent<ChatPanel>();
            _chatPanel.SetUI(logText, inputField, sendBtn);
        }

        private void BuildQuestTracker()
        {
            // Remove any old copy (from prefab or previous init)
            var existing = transform.Find("QuestTracker");
            if (existing != null) Destroy(existing.gameObject);

            var questGo = NewUIChild("QuestTracker", transform);
            Stretch(questGo);

            // Status text: top-right
            var statusGo = NewUIChild("QuestStatus", questGo.transform);
            var questText = statusGo.AddComponent<Text>();
            questText.text = "Select a quest:";
            questText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            questText.fontSize = 15;
            questText.color = Color.yellow;
            questText.alignment = TextAnchor.UpperRight;
            Position(statusGo, new Vector2(300, 260), new Vector2(380, 28));

            // Quest buttons stacked top-right
            var q1 = MakeQuestBtn(questGo.transform, "Slime x3", 240);
            var q2 = MakeQuestBtn(questGo.transform, "Goblins x2", 225);
            var q3 = MakeQuestBtn(questGo.transform, "Wolf x1", 210);

            _questTracker = questGo.AddComponent<QuestTracker>();
            _questTracker.SetUI(questText, q1, q2, q3);
        }

        private Button MakeQuestBtn(Transform parent, string label, float y)
        {
            var go = NewUIChild("QuestBtn", parent);
            go.AddComponent<Image>().color = new Color(0.3f, 0.3f, 0.5f);
            var btn = go.AddComponent<Button>();
            Position(go, new Vector2(360, y), new Vector2(140, 24));

            var txtGo = NewUIChild("Label", go.transform);
            var txt = txtGo.AddComponent<Text>();
            txt.text = label;
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.fontSize = 13;
            txt.color = Color.white;
            txt.alignment = TextAnchor.MiddleCenter;
            FillParent(txtGo);
            return btn;
        }

        // ── UI helpers ──

        private static GameObject NewUIChild(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
        }

        private static void Position(GameObject go, Vector2 pos, Vector2 size)
        {
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
        }

        private static void Stretch(GameObject go)
        {
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }

        private static void FillParent(GameObject go)
        {
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
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
