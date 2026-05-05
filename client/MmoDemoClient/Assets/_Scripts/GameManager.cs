using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace MmoDemo.Client
{
    /// <summary>
    /// Phase 2 game state manager. Handles WebSocket connection, movement,
    /// and entity sync after Phase 1 login/role selection.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        [SerializeField] private string wsUrl = "ws://localhost:5000/ws";
        [SerializeField] private GameObject otherPlayerPrefab;
        [SerializeField] private GameObject localPlayerPrefab;

        private WebSocketClient _ws;
        private string _playerId;
        private string _token;
        private string _roleId;
        private string _myEntityId;

        private readonly Dictionary<string, GameObject> _entities = new();
        private GameObject _localPlayer;

        public bool IsReady { get; private set; }

        public async void Connect(string playerId, string token, string roleId)
        {
            _playerId = playerId;
            _token = token;
            _roleId = roleId;

            _ws = new WebSocketClient(wsUrl);
            _ws.OnMessage += OnWsMessage;
            _ws.OnDisconnected += OnDisconnected;

            await _ws.ConnectAsync();
            SendAuth();
        }

        private async void SendAuth()
        {
            var payload = $"{{\"playerId\":\"{_playerId}\",\"token\":\"{_token}\",\"roleId\":\"{_roleId}\"}}";
            await _ws.SendAsync("c2s.auth", payload);
        }

        private async void SendEnterScene(string sceneId)
        {
            var payload = $"{{\"sceneId\":\"{sceneId}\"}}";
            await _ws.SendAsync("c2s.enter_scene", payload);
        }

        private async void SendMove(float dirX, float dirZ, float posX, float posZ)
        {
            var payload = $"{{\"dirX\":{dirX:F2},\"dirY\":0,\"dirZ\":{dirZ:F2},\"posX\":{posX:F2},\"posY\":0,\"posZ\":{posZ:F2}}}";
            await _ws.SendAsync("c2s.move", payload);
        }

        private void OnWsMessage(string type, string payload)
        {
            switch (type)
            {
                case "s2c.auth_result":
                    HandleAuthResult(payload);
                    break;
                case "s2c.enter_scene_result":
                    HandleEnterSceneResult(payload);
                    break;
                case "s2c.entity_snapshot":
                    HandleEntitySnapshot(payload);
                    break;
                case "s2c.entity_joined":
                    HandleEntityJoined(payload);
                    break;
                case "s2c.entity_left":
                    HandleEntityLeft(payload);
                    break;
            }
        }

        private void HandleAuthResult(string payload)
        {
            var ok = ExtractString(payload, "\"ok\":") == "true";
            if (ok)
            {
                Debug.Log("[Game] Auth OK, entering city...");
                SendEnterScene("city_001");
            }
            else
            {
                Debug.LogError("[Game] Auth failed: " + ExtractString(payload, "\"message\":\""));
            }
        }

        private void HandleEnterSceneResult(string payload)
        {
            var ok = ExtractString(payload, "\"ok\":") == "true";
            if (!ok) { Debug.LogError("[Game] Enter scene failed"); return; }

            // Spawn local player
            var spawnX = ExtractFloat(payload, "\"spawnX\":");
            var spawnZ = ExtractFloat(payload, "\"spawnZ\":");
            _localPlayer = Instantiate(localPlayerPrefab, new Vector3(spawnX, 0, spawnZ), Quaternion.identity);
            _myEntityId = ExtractString(payload, "\"entityId\":\"");

            // Spawn existing entities
            var entitiesStart = payload.IndexOf("\"entities\":[");
            var entitiesEnd = payload.LastIndexOf("]");
            if (entitiesStart > 0)
            {
                var entitiesJson = payload.Substring(entitiesStart + 12, entitiesEnd - entitiesStart - 11);
                // Parse each entity from the array
                foreach (var entityJson in SplitJsonArray(entitiesJson))
                    SpawnEntity(entityJson);
            }

            IsReady = true;
            Debug.Log("[Game] Scene entered! Spawned at " + spawnX + "," + spawnZ);
        }

        private void HandleEntitySnapshot(string payload)
        {
            if (!IsReady) return;

            // Parse entities array
            var entitiesStart = payload.IndexOf("\"entities\":[");
            var entitiesEnd = payload.LastIndexOf("]");
            if (entitiesStart < 0) return;

            var entitiesJson = payload.Substring(entitiesStart + 12, entitiesEnd - entitiesStart - 11);
            foreach (var entityJson in SplitJsonArray(entitiesJson))
                UpdateEntity(entityJson);
        }

        private void HandleEntityJoined(string payload)
        {
            var entityStart = payload.IndexOf("\"entity\":{");
            var entityEnd = payload.LastIndexOf("}");
            if (entityStart < 0) return;

            var entityJson = payload.Substring(entityStart + 9, entityEnd - entityStart - 8);
            SpawnEntity(entityJson);
        }

        private void HandleEntityLeft(string payload)
        {
            var entityId = ExtractString(payload, "\"entityId\":\"");
            if (!string.IsNullOrEmpty(entityId))
                DespawnEntity(entityId);
        }

        private void OnDisconnected(string reason)
        {
            Debug.LogWarning("[Game] Disconnected: " + reason);
            IsReady = false;
        }

        // ── Entity management ──

        private void SpawnEntity(string json)
        {
            var entityId = ExtractString(json, "\"entityId\":\"");
            if (string.IsNullOrEmpty(entityId) || entityId == _myEntityId || _entities.ContainsKey(entityId))
                return;

            var x = ExtractFloat(json, "\"posX\":");
            var z = ExtractFloat(json, "\"posZ\":");
            var name = ExtractString(json, "\"name\":\"");

            var go = Instantiate(otherPlayerPrefab, new Vector3(x, 0, z), Quaternion.identity);
            go.name = name;
            _entities[entityId] = go;
        }

        private void UpdateEntity(string json)
        {
            var entityId = ExtractString(json, "\"entityId\":\"");
            if (entityId == _myEntityId || !_entities.TryGetValue(entityId, out var go))
                return;

            var x = ExtractFloat(json, "\"posX\":");
            var z = ExtractFloat(json, "\"posZ\":");
            go.transform.position = Vector3.Lerp(go.transform.position, new Vector3(x, 0, z), 0.3f);
        }

        private void DespawnEntity(string entityId)
        {
            if (_entities.TryGetValue(entityId, out var go))
            {
                Destroy(go);
                _entities.Remove(entityId);
            }
        }

        // ── Update loop ──

        private void Update()
        {
            _ws?.Update();
            if (!IsReady || _localPlayer == null) return;

            // Simple movement: WASD / arrows
            var h = Input.GetAxis("Horizontal");
            var v = Input.GetAxis("Vertical");

            if (Mathf.Abs(h) > 0.01f || Mathf.Abs(v) > 0.01f)
            {
                var speed = 5f * Time.deltaTime;
                var pos = _localPlayer.transform.position;
                pos.x += h * speed;
                pos.z += v * speed;
                _localPlayer.transform.position = pos;

                SendMove(h, v, pos.x, pos.z);
            }
        }

        private void OnDestroy()
        {
            _ws?.Dispose();
        }

        // ── Simple JSON parsers (avoiding JsonUtility allocation overhead) ──

        private static float ExtractFloat(string json, string key)
        {
            var i = json.IndexOf(key);
            if (i < 0) return 0;
            i += key.Length;
            var end = json.IndexOfAny(new[] { ',', '}', ' ' }, i);
            if (end < 0) end = json.Length;
            return float.TryParse(json.Substring(i, end - i), out var v) ? v : 0;
        }

        private static string ExtractString(string json, string key)
        {
            var i = json.IndexOf(key);
            if (i < 0) return "";
            i += key.Length;
            var end = json.IndexOf('"', i);
            if (end < 0) return json.Substring(i);
            return json.Substring(i, end - i);
        }

        private static string[] SplitJsonArray(string json)
        {
            var items = new List<string>();
            var depth = 0;
            var start = 0;
            for (var i = 0; i < json.Length; i++)
            {
                if (json[i] == '{') depth++;
                else if (json[i] == '}')
                {
                    depth--;
                    if (depth == 0)
                    {
                        items.Add(json.Substring(start, i - start + 1));
                        start = i + 2; // skip comma
                    }
                }
            }
            return items.ToArray();
        }
    }
}
