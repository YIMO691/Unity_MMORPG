using UnityEngine;
using UnityEngine.UI;

namespace MmoDemo.Client
{
    public class QuestTracker : MonoBehaviour
    {
        private Text _statusText;
        private Button _btn1, _btn2, _btn3;
        private GameManager _gm;

        public void SetUI(Text statusText, Button quest1, Button quest2, Button quest3)
        {
            _statusText = statusText;
            _btn1 = quest1; _btn2 = quest2; _btn3 = quest3;
            _btn1?.onClick.AddListener(() => AcceptQuest(1));
            _btn2?.onClick.AddListener(() => AcceptQuest(2));
            _btn3?.onClick.AddListener(() => AcceptQuest(3));
        }

        private void Start()
        {
            _gm = FindObjectOfType<GameManager>();
            if (_gm != null)
            {
                _gm.OnQuestUpdated += OnUpdated;
                _gm.OnQuestCompleted += OnCompleted;
            }
        }

        private void Update()
        {
            if (_gm == null)
            {
                _gm = FindObjectOfType<GameManager>();
                if (_gm != null)
                {
                    _gm.OnQuestUpdated += OnUpdated;
                    _gm.OnQuestCompleted += OnCompleted;
                }
            }
        }

        private void AcceptQuest(int questId)
        {
            if (_gm == null)
            {
                _gm = FindObjectOfType<GameManager>();
                if (_gm != null)
                {
                    _gm.OnQuestUpdated += OnUpdated;
                    _gm.OnQuestCompleted += OnCompleted;
                }
            }
            _gm?.SendAcceptQuest(questId);
        }

        private void OnUpdated(string info)
        {
            if (_statusText != null) _statusText.text = info;
            SetButtons(false);
        }

        private void OnCompleted(string info)
        {
            if (_statusText != null) _statusText.text = info;
            SetButtons(true);
        }

        private void SetButtons(bool visible)
        {
            if (_btn1) _btn1.gameObject.SetActive(visible);
            if (_btn2) _btn2.gameObject.SetActive(visible);
            if (_btn3) _btn3.gameObject.SetActive(visible);
        }

        private void OnDestroy()
        {
            if (_gm != null)
            {
                _gm.OnQuestUpdated -= OnUpdated;
                _gm.OnQuestCompleted -= OnCompleted;
            }
        }
    }
}
