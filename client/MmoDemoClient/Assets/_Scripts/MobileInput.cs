using UnityEngine;

namespace MmoDemo.Client
{
    public class MobileInput : MonoBehaviour
    {
        public static MobileInput Instance { get; private set; }

        public float Horizontal { get; private set; }
        public float Vertical { get; private set; }
        public bool Skill1 { get; private set; }
        public bool Skill2 { get; private set; }
        public bool Skill3 { get; private set; }

        private Rect _joystickArea;
        private Rect _btn1Area, _btn2Area, _btn3Area;
        private int _joystickFinger = -1;
        private Vector2 _joystickOrigin;
        private const float JoystickRadius = 80f;

        public bool IsActive => Application.isMobilePlatform;

        private void Awake()
        {
            Instance = this;
            if (!IsActive) enabled = false;
        }

        private void Start()
        {
            var jSize = JoystickRadius * 2;
            _joystickArea = new Rect(20, Screen.height - jSize - 20, jSize, jSize);

            var btnW = 80f; var btnH = 60f; var btnY = Screen.height - btnH - 20;
            _btn1Area = new Rect(Screen.width - btnW * 3 - 20, btnY, btnW, btnH);
            _btn2Area = new Rect(Screen.width - btnW * 2 - 10, btnY, btnW, btnH);
            _btn3Area = new Rect(Screen.width - btnW - 0, btnY, btnW, btnH);
        }

        private void Update()
        {
            if (!IsActive) return;

            Horizontal = 0; Vertical = 0;
            Skill1 = false; Skill2 = false; Skill3 = false;

            foreach (var touch in Input.touches)
            {
                if (touch.phase == TouchPhase.Began)
                {
                    if (_joystickArea.Contains(touch.position))
                    {
                        _joystickFinger = touch.fingerId;
                        _joystickOrigin = touch.position;
                    }
                    if (_btn1Area.Contains(touch.position)) Skill1 = true;
                    if (_btn2Area.Contains(touch.position)) Skill2 = true;
                    if (_btn3Area.Contains(touch.position)) Skill3 = true;
                }

                if (touch.fingerId == _joystickFinger)
                {
                    if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
                    {
                        _joystickFinger = -1;
                        Horizontal = 0; Vertical = 0;
                    }
                    else
                    {
                        var delta = touch.position - _joystickOrigin;
                        Horizontal = Mathf.Clamp(delta.x / JoystickRadius, -1f, 1f);
                        Vertical = Mathf.Clamp(delta.y / JoystickRadius, -1f, 1f);
                    }
                }
            }
        }

        private void OnGUI()
        {
            if (!IsActive) return;

            var skin = GUI.skin;
            skin.box.fontSize = 24;

            // Joystick background
            GUI.Box(_joystickArea, "Move");

            // Skill buttons
            GUI.backgroundColor = new Color(0.8f, 0.2f, 0.2f);
            GUI.Box(_btn1Area, "1");
            GUI.Box(_btn2Area, "2");
            GUI.Box(_btn3Area, "3");
        }
    }
}
