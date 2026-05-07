using System.Collections;
using UnityEngine;

namespace MmoDemo.Client
{
    public class GameLauncher : MonoBehaviour
    {
        [SerializeField] private string serverBaseUrl = "";

        private string ServerUrl =>
            !string.IsNullOrEmpty(serverBaseUrl) ? serverBaseUrl :
            PlayerPrefs.GetString("server_url", Application.isMobilePlatform
                ? "http://192.168.1.100:5000" : "http://localhost:5000");
        [SerializeField] private GameObject loginViewPrefab;
        [SerializeField] private GameObject roleSelectViewPrefab;
        [SerializeField] private GameObject cityViewPrefab;

        private NetworkManager _network;
        private LuaManager _lua;
        private UIManager _ui;
        private ResourceManager _resources;

        public string WelcomeText { get; private set; } = "";

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);

            var url = ServerUrl;
            _network = new NetworkManager(url);
            _lua = new LuaManager();
            _ui = new UIManager(loginViewPrefab, roleSelectViewPrefab, cityViewPrefab, _network, this);
            _resources = new ResourceManager(url);

            _lua.RegisterBridge("network", _network);
            _lua.RegisterBridge("ui", _ui);
        }

        private IEnumerator Start()
        {
            _lua.Start();

            // Phase 1: Health check
            bool healthOk = false;
            yield return _network.CheckHealth(ok => healthOk = ok);
            if (!healthOk)
            {
                Debug.LogError("[Launcher] Cannot reach server at " + ServerUrl);
                yield break;
            }

            // Phase 6: Resource update check
            Debug.Log("[Launcher] Checking for resource updates...");
            bool updateDone = false;
            yield return _resources.CheckForUpdates(
                (done, total) => Debug.Log($"[Launcher] Resource update: {done}/{total}"),
                ok => updateDone = ok);

            if (updateDone)
            {
                var welcome = _resources.ReadCachedText("welcome.txt");
                if (!string.IsNullOrEmpty(welcome))
                {
                    WelcomeText = welcome;
                    Debug.Log("[Launcher] Remote welcome: " + welcome.Trim());
                }
                else
                {
                    WelcomeText = "Welcome! (no remote resources)";
                }
            }

            _ui.ShowLogin();
        }

        public void OnLoginSuccess()
        {
            _ui.ShowRoleSelect();
        }

        public void OnRoleSelected(string roleId)
        {
            _ui.ShowCity(roleId);
        }

        private void OnDestroy()
        {
            _lua?.Dispose();
        }
    }
}
