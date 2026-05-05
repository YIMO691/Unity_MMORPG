using UnityEngine;
using UnityEngine.UI;

namespace MmoDemo.Client
{
    /// <summary>
    /// Phase 2 city UI. Displays role info then connects WebSocket for real-time gameplay.
    /// </summary>
    public class CityView : MonoBehaviour
    {
        [SerializeField] private Text nameText;
        [SerializeField] private Text levelText;
        [SerializeField] private Text goldText;
        [SerializeField] private Text statusText;

        private NetworkManager _network;

        public void Init(NetworkManager network, string roleId)
        {
            _network = network;
            statusText.text = "Entering city...";

            // Phase 1: HTTP enter city to get role display data
            StartCoroutine(_network.EnterCity(roleId, result => OnCityEntered(result, roleId), OnError));
        }

        private void OnCityEntered(EnterCityResult result, string roleId)
        {
            if (result.role != null)
            {
                nameText.text = result.role.name;
                levelText.text = $"Level {result.role.level}";
                goldText.text = $"Gold: {result.role.gold:N0}";
                statusText.text = "Connecting to game server...";

                // Phase 2: Connect WebSocket for real-time gameplay
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
