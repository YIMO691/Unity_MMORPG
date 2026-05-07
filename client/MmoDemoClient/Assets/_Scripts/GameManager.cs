using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;

namespace MmoDemo.Client
{
    /// <summary>
    /// Phase 4 game manager. Phase 2 movement + monsters, combat, drops, inventory, quests, chat.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        [SerializeField] private string wsUrl = "ws://localhost:5000/ws";
        [SerializeField] private GameObject localPlayerPrefab;
        [SerializeField] private GameObject otherPlayerPrefab;
        [SerializeField] private GameObject monsterPrefab;
        [SerializeField] private GameObject dropPrefab;
        [SerializeField] private GameObject damageTextPrefab;

        private WebSocketClient _ws;
        private string _playerId, _token, _roleId, _myEntityId, _currentSceneId;
        private readonly Dictionary<string, GameObject> _entities = new();
        private readonly Dictionary<string, GameObject> _drops = new();
        private readonly HashSet<string> _pendingPickups = new();
        private GameObject _localPlayer;
        private GameObject _portalMarker;
        private int _score;
        private float _portalCooldown;
        private bool _isSwitchingScene;
        private float _nextRespawnNoticeAt;

        public bool IsReady { get; private set; }

        // Phase 4 events for UI overlays
        public event Action<string, string> OnChatReceived; // sender, text
        public event Action<string> OnQuestUpdated;          // status text
        public event Action<string> OnQuestCompleted;        // completion text

        // ═══════════ Connection ═══════════

        public async void Connect(string playerId, string token, string roleId)
        {
            _playerId = playerId; _token = token; _roleId = roleId;
            _myEntityId = $"entity_{roleId}";
            _ws = new WebSocketClient(wsUrl);
            _ws.OnMessage += OnWsMessage;
            _ws.OnDisconnected += OnDisconnected;
            await _ws.ConnectAsync();
            SendAuth();
        }

        private async void SendAuth() =>
            await _ws.SendAsync("c2s.auth", $"{{\"playerId\":\"{_playerId}\",\"token\":\"{_token}\",\"roleId\":\"{_roleId}\"}}");

        private async void SendEnterScene(string s) =>
            await _ws.SendAsync("c2s.enter_scene", $"{{\"sceneId\":\"{s}\"}}");

        private async void SendMove(float dx, float dz, float px, float pz) =>
            await _ws.SendAsync("c2s.move",
                "{\"dirX\":" + dx.ToString("F2") + ",\"dirY\":0,\"dirZ\":" + dz.ToString("F2") +
                ",\"posX\":" + px.ToString("F2") + ",\"posY\":0,\"posZ\":" + pz.ToString("F2") + "}");

        private async void SendCastSkill(string targetId, int skillId) =>
            await _ws.SendAsync("c2s.cast_skill", $"{{\"targetId\":\"{targetId}\",\"skillId\":{skillId}}}");

        public async void SendChat(string text) =>
            await _ws.SendAsync("c2s.chat", $"{{\"text\":\"{text}\"}}");

        public async void SendAcceptQuest(int questId) =>
            await _ws.SendAsync("c2s.accept_quest", $"{{\"questId\":{questId}}}");

        // ═══════════ Message Router ═══════════

        private void OnWsMessage(string type, string payload)
        {
            switch (type)
            {
                case "s2c.auth_result":
                    if (ExtractBool(payload, "\"ok\":")) SendEnterScene("city_001");
                    else Debug.LogError("[Game] Auth failed: " + payload);
                    break;
                case "s2c.enter_scene_result":
                    HandleEnterScene(payload); break;
                case "s2c.entity_snapshot":
                    HandleSnapshot(payload); break;
                case "s2c.entity_joined":
                    HandleEntityJoined(ExtractNested(payload, "\"entity\":"));
                    break;
                case "s2c.entity_left":
                    DespawnEntity(ExtractString(payload, "\"entityId\":\""));
                    break;
                case "s2c.combat_event":
                    HandleCombatEvent(payload); break;
                case "s2c.monster_death":
                    HandleMonsterDeath(payload); break;
                case "s2c.drop_spawned":
                    HandleDropSpawned(payload); break;
                case "s2c.drop_picked_up":
                    HandleDropPickedUp(payload); break;
                case "s2c.inventory_data":
                    HandleInventory(payload); break;
                case "s2c.chat_broadcast":
                    HandleChatBroadcast(payload); break;
                case "s2c.quest_updated":
                    HandleQuestUpdated(payload); break;
                case "s2c.quest_completed":
                    HandleQuestCompleted(payload); break;
            }
        }

        // ═══════════ Scene Entry ═══════════

        private void HandleEnterScene(string p)
        {
            if (!ExtractBool(p, "\"ok\":"))
            {
                _isSwitchingScene = false;
                IsReady = _localPlayer != null;
                NotifySystem("Scene change failed.");
                return;
            }

            // Phase 7: Despawn old entities on scene switch
            foreach (var kv in _entities)
                Destroy(kv.Value);
            _entities.Clear();
            foreach (var kv in _drops)
                Destroy(kv.Value);
            _drops.Clear();
            _pendingPickups.Clear();
            if (_localPlayer != null) Destroy(_localPlayer);
            if (_portalMarker != null) Destroy(_portalMarker);

            _currentSceneId = ExtractString(p, "\"sceneId\":\"");
            _myEntityId = ExtractString(p, "\"entityId\":\"");
            if (string.IsNullOrEmpty(_myEntityId))
                _myEntityId = $"entity_{_roleId}";

            var sx = ExtractFloat(p, "\"spawnX\":");
            var sz = ExtractFloat(p, "\"spawnZ\":");
            _localPlayer = Instantiate(localPlayerPrefab, new Vector3(sx, 1, sz), Quaternion.identity);
            _localPlayer.GetComponent<Renderer>().material.color = Color.blue;

            // Camera follow player
            var cam = Camera.main;
            if (cam != null)
            {
                var follow = cam.GetComponent<CameraFollow>();
                if (follow == null) follow = cam.gameObject.AddComponent<CameraFollow>();
                follow.target = _localPlayer.transform;
            }
            // Only hide StatusText, keep NameText/LevelText/GoldText visible
            foreach (var canvas in FindObjectsOfType<Canvas>())
            {
                foreach (Transform child in canvas.transform)
                {
                    if (child.name is "StatusText")
                        child.gameObject.SetActive(false);
                }
            }

            // Spawn entities from the list (players + monsters)
            var arr = ExtractJsonArray(p, "\"entities\":");
            foreach (var ej in arr)
                SpawnEntity(ej, out _, out _);

            _portalCooldown = 3f;
            _isSwitchingScene = false;
            IsReady = true;
            CreatePortalMarker();
            EventSystem.current?.SetSelectedGameObject(null);
            NotifySystem($"Entered {GetSceneName(_currentSceneId)}.");
        }

        // ═══════════ Entity Spawning ═══════════

        private void HandleEntityJoined(string json)
        {
            var spawned = SpawnEntity(json, out var type, out var name);
            if (spawned && type == "monster" && IsReady)
                NotifyRespawn(name);
        }

        private bool SpawnEntity(string json, out string entityType, out string entityName)
        {
            entityType = "";
            entityName = "";
            var eid = ExtractString(json, "\"entityId\":\"");
            if (string.IsNullOrEmpty(eid) || eid == _myEntityId) return false;
            if (_entities.TryGetValue(eid, out var existing))
            {
                Destroy(existing);
                _entities.Remove(eid);
            }

            var etype = ExtractString(json, "\"type\":\"");
            var x = ExtractFloat(json, "\"posX\":");
            var z = ExtractFloat(json, "\"posZ\":");
            var name = ExtractString(json, "\"name\":\"");
            entityType = etype;
            entityName = string.IsNullOrEmpty(name) ? etype : name;

            GameObject go;
            if (etype == "monster")
            {
                go = Instantiate(monsterPrefab, new Vector3(x, 1, z), Quaternion.identity);
                go.GetComponent<Renderer>().material.color = new Color(0.2f, 0.8f, 0.2f); // green
            }
            else
            {
                go = Instantiate(otherPlayerPrefab, new Vector3(x, 1, z), Quaternion.identity);
                go.GetComponent<Renderer>().material.color = Color.red;
            }

            go.name = $"{name}({eid[..6]})";
            _entities[eid] = go;
            return true;
        }

        private void UpdateEntity(string json)
        {
            var eid = ExtractString(json, "\"entityId\":\"");
            if (eid == _myEntityId || !_entities.TryGetValue(eid, out var go)) return;
            go.transform.position = Vector3.Lerp(go.transform.position,
                new Vector3(ExtractFloat(json, "\"posX\":"), 0, ExtractFloat(json, "\"posZ\":")), 0.3f);
        }

        private void DespawnEntity(string eid)
        {
            if (_entities.TryGetValue(eid, out var go)) { Destroy(go); _entities.Remove(eid); }
        }

        // ═══════════ Phase 3: Combat ═══════════

        private string _nearestMonsterId;

        private void HandleCombatEvent(string p)
        {
            var dmg = (int)ExtractFloat(p, "\"damage\":");
            var crit = ExtractBool(p, "\"crit\":");
            var targetDied = ExtractBool(p, "\"targetDied\":");
            var targetId = ExtractString(p, "\"targetId\":\"");
            var casterHp = (int)ExtractFloat(p, "\"casterHp\":");

            // Show damage text on target
            if (_entities.TryGetValue(targetId, out var target))
            {
                var dt = Instantiate(damageTextPrefab, target.transform.position + Vector3.up * 2, Quaternion.identity);
                var txt = dt.GetComponent<TextMesh>() ?? dt.AddComponent<TextMesh>();
                txt.text = crit ? $"CRIT! {dmg}" : $"-{dmg}";
                txt.color = crit ? Color.yellow : Color.white;
                txt.fontSize = 36;
                Destroy(dt, 1.5f);
            }

            if (targetDied && _entities.TryGetValue(targetId, out var dead))
            {
                Destroy(dead);
                _entities.Remove(targetId);
            }

            _score += dmg;
        }

        private void HandleMonsterDeath(string p)
        {
            var eid = ExtractString(p, "\"entityId\":\"");
            var exp = (int)ExtractFloat(p, "\"expReward\":");
            var gold = (int)ExtractFloat(p, "\"goldReward\":");
            DespawnEntity(eid);
            _score += gold;
        }

        // ═══════════ Phase 3: Drops ═══════════

        private void HandleDropSpawned(string p)
        {
            var dropId = ExtractString(p, "\"dropId\":\"");
            if (string.IsNullOrEmpty(dropId)) return;
            var itemName = ExtractString(p, "\"itemName\":\"");
            var x = ExtractFloat(p, "\"posX\":");
            var z = ExtractFloat(p, "\"posZ\":");
            if (_drops.TryGetValue(dropId, out var oldDrop))
                Destroy(oldDrop);

            var go = Instantiate(dropPrefab, new Vector3(x, 0.5f, z), Quaternion.identity);
            go.name = itemName;
            go.GetComponent<Renderer>().material.color = Color.yellow;
            _drops[dropId] = go;
            _pendingPickups.Remove(dropId);
        }

        private void HandleDropPickedUp(string p)
        {
            var dropId = ExtractString(p, "\"dropId\":\"");
            var pickedBy = ExtractString(p, "\"pickedBy\":\"");
            _pendingPickups.Remove(dropId);
            if (_drops.TryGetValue(dropId, out var go))
            {
                if (pickedBy == _myEntityId)
                    NotifySystem($"Picked up {go.name}.");
                Destroy(go);
                _drops.Remove(dropId);
            }
        }

        private void HandleInventory(string p)
        {
            // Inventory UI is not implemented yet; keep server sync silent during normal play.
        }

        // ═══════════ Phase 4: Chat ═══════════

        private void HandleChatBroadcast(string p)
        {
            var sender = ExtractString(p, "\"senderName\":\"");
            var text = ExtractString(p, "\"text\":\"");
            OnChatReceived?.Invoke(sender, text);
        }

        // ═══════════ Phase 4: Quest ═══════════

        private void HandleQuestUpdated(string p)
        {
            var ok = ExtractBool(p, "\"ok\":");
            if (!ok) return;
            var name = ExtractString(p, "\"name\":\"");
            var progress = (int)ExtractFloat(p, "\"progress\":");
            var target = (int)ExtractFloat(p, "\"targetCount\":");
            var desc = ExtractString(p, "\"description\":\"");
            OnQuestUpdated?.Invoke($"{desc}: {progress}/{target}");
        }

        private void HandleQuestCompleted(string p)
        {
            var name = ExtractString(p, "\"name\":\"");
            var exp = (int)ExtractFloat(p, "\"expReward\":");
            var gold = (int)ExtractFloat(p, "\"goldReward\":");
            OnQuestCompleted?.Invoke($"Quest Complete: {name}! +{exp} exp, +{gold} gold");
            _score += gold;
        }

        // ═══════════ Snapshot ═══════════

        private void HandleSnapshot(string p)
        {
            foreach (var ej in ExtractJsonArray(p, "\"entities\":"))
                UpdateEntity(ej);
        }

        // ═══════════ Update ═══════════

        private void Update()
        {
            _ws?.Update();
            if (!IsReady || _localPlayer == null) return;

            // Movement
            // Phase 9: Mobile or PC input
            float h, v;
            bool s1, s2, s3;
            if (MobileInput.Instance != null && MobileInput.Instance.IsActive)
            {
                h = MobileInput.Instance.Horizontal;
                v = MobileInput.Instance.Vertical;
                s1 = MobileInput.Instance.Skill1;
                s2 = MobileInput.Instance.Skill2;
                s3 = MobileInput.Instance.Skill3;
            }
            else
            {
                h = Input.GetAxis("Horizontal");
                v = Input.GetAxis("Vertical");
                s1 = Input.GetKeyDown(KeyCode.Alpha1);
                s2 = Input.GetKeyDown(KeyCode.Alpha2);
                s3 = Input.GetKeyDown(KeyCode.Alpha3);
            }

            if (Mathf.Abs(h) > 0.01f || Mathf.Abs(v) > 0.01f)
            {
                var spd = 5f * Time.deltaTime;
                var pos = _localPlayer.transform.position;
                pos.x += h * spd; pos.z += v * spd;
                _localPlayer.transform.position = pos;
                SendMove(h, v, pos.x, pos.z);
            }

            if (s1) AttackNearest(1);
            if (s2) AttackNearest(2);
            if (s3) AttackNearest(3);

            // Phase 7: Portal / scene transition
            if (_portalCooldown > 0) _portalCooldown -= Time.deltaTime;
            if (_portalCooldown <= 0)
            {
                var pos = _localPlayer.transform.position;
                var portal = GetPortalPosition(_currentSceneId);
                var dx = pos.x - portal.x;
                var dz = pos.z - portal.y;
                if (dx * dx + dz * dz <= 6.25f)
                    RequestSceneChange(GetPortalTargetScene(_currentSceneId));
            }

            // Phase 3: Auto-pickup nearby drops (within 2 units)
            var playerPos = _localPlayer.transform.position;
            string nearbyDrop = null;
            foreach (var kv in _drops)
            {
                if (kv.Value == null) continue;
                if (_pendingPickups.Contains(kv.Key)) continue;
                var dropPos = kv.Value.transform.position;
                var dx = playerPos.x - dropPos.x;
                var dz = playerPos.z - dropPos.z;
                if (dx * dx + dz * dz <= 9f)
                { nearbyDrop = kv.Key; break; }
            }
            if (nearbyDrop != null)
            {
                _pendingPickups.Add(nearbyDrop);
                _ws.SendAsync("c2s.pickup_item", $"{{\"dropId\":\"{nearbyDrop}\"}}");
            }

            // Auto-target: highlight nearest monster
            UpdateNearestMonster();
        }

        private void UpdateNearestMonster()
        {
            float bestDist = 8f;
            string bestId = null;
            foreach (var kv in _entities)
            {
                if (kv.Value == null) continue;
                var dist = Vector3.Distance(_localPlayer.transform.position, kv.Value.transform.position);
                if (dist < bestDist) { bestDist = dist; bestId = kv.Key; }
            }
            _nearestMonsterId = bestId;
        }

        private void AttackNearest(int skillId)
        {
            if (string.IsNullOrEmpty(_nearestMonsterId)) return;
            SendCastSkill(_nearestMonsterId, skillId);
        }

        private void RequestSceneChange(string sceneId)
        {
            if (_isSwitchingScene || string.IsNullOrEmpty(sceneId)) return;

            _isSwitchingScene = true;
            IsReady = false;
            _nearestMonsterId = null;
            _portalCooldown = 3f;
            EventSystem.current?.SetSelectedGameObject(null);
            NotifySystem($"Entering {GetSceneName(sceneId)}...");
            SendEnterScene(sceneId);
        }

        private void NotifySystem(string message)
        {
            OnChatReceived?.Invoke("System", message);
        }

        private void NotifyRespawn(string monsterName)
        {
            if (Time.time < _nextRespawnNoticeAt) return;
            _nextRespawnNoticeAt = Time.time + 3f;
            NotifySystem("Monsters respawned nearby.");
        }

        private void CreatePortalMarker()
        {
            var targetScene = GetPortalTargetScene(_currentSceneId);
            if (string.IsNullOrEmpty(targetScene)) return;

            var portal = GetPortalPosition(_currentSceneId);
            _portalMarker = new GameObject($"Portal_To_{targetScene}");
            _portalMarker.transform.position = new Vector3(portal.x, 0f, portal.y);

            var ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ring.name = "PortalRing";
            ring.transform.SetParent(_portalMarker.transform);
            ring.transform.localPosition = new Vector3(0f, 0.05f, 0f);
            ring.transform.localScale = new Vector3(3.5f, 0.08f, 3.5f);

            var renderer = ring.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = _currentSceneId == "city_001"
                    ? new Color(0.1f, 0.75f, 1f)
                    : new Color(1f, 0.75f, 0.1f);
            }

            var label = new GameObject("PortalLabel");
            label.transform.SetParent(_portalMarker.transform);
            label.transform.localPosition = new Vector3(0f, 2.2f, 0f);
            label.transform.localRotation = Quaternion.Euler(60f, 0f, 0f);
            label.transform.localScale = new Vector3(0.22f, 0.22f, 0.22f);

            var text = label.AddComponent<TextMesh>();
            text.text = $"To {GetSceneName(targetScene)}";
            text.anchor = TextAnchor.MiddleCenter;
            text.alignment = TextAlignment.Center;
            text.color = Color.white;
            text.fontSize = 36;
        }

        private static string GetSceneName(string sceneId) =>
            sceneId == "field_001" ? "Wilderness" :
            sceneId == "city_001" ? "Main City" :
            sceneId;

        private static Vector2 GetPortalPosition(string sceneId) =>
            sceneId == "city_001" ? new Vector2(16f, 0f) :
            sceneId == "field_001" ? new Vector2(-44f, -30f) :
            Vector2.zero;

        private static string GetPortalTargetScene(string sceneId) =>
            sceneId == "city_001" ? "field_001" :
            sceneId == "field_001" ? "city_001" :
            "";

        private void OnDisconnected(string reason)
        {
            Debug.LogWarning("[Game] Disconnected: " + reason);
            IsReady = false;
        }

        private void OnDestroy()
        {
            if (_portalMarker != null) Destroy(_portalMarker);
            _ws?.Dispose();
        }

        // ═══════════ JSON Helpers ═══════════

        private static float ExtractFloat(string json, string key)
        {
            var i = json.IndexOf(key); if (i < 0) return 0; i += key.Length;
            var end = json.IndexOfAny(new[] { ',', '}', ' ' }, i);
            if (end < 0) end = json.Length;
            return float.TryParse(json.Substring(i, end - i), out var v) ? v : 0;
        }

        private static bool ExtractBool(string json, string key) =>
            json.IndexOf(key + "true", StringComparison.Ordinal) >= 0;

        private static string ExtractString(string json, string key)
        {
            var i = json.IndexOf(key); if (i < 0) return ""; i += key.Length;
            if (i < json.Length && json[i] == '"') i++;
            var sb = new StringBuilder();
            while (i < json.Length)
            {
                var c = json[i++];
                if (c == '"') break;
                if (c != '\\')
                {
                    sb.Append(c);
                    continue;
                }

                if (i >= json.Length) break;
                var escaped = json[i++];
                switch (escaped)
                {
                    case '"': sb.Append('"'); break;
                    case '\\': sb.Append('\\'); break;
                    case '/': sb.Append('/'); break;
                    case 'b': sb.Append('\b'); break;
                    case 'f': sb.Append('\f'); break;
                    case 'n': sb.Append('\n'); break;
                    case 'r': sb.Append('\r'); break;
                    case 't': sb.Append('\t'); break;
                    case 'u':
                        if (i + 4 <= json.Length)
                        {
                            try
                            {
                                sb.Append((char)Convert.ToInt32(json.Substring(i, 4), 16));
                                i += 4;
                            }
                            catch
                            {
                                sb.Append("\\u");
                            }
                        }
                        break;
                    default:
                        sb.Append(escaped);
                        break;
                }
            }

            return sb.ToString();
        }

        private static string ExtractNested(string json, string key)
        {
            var i = json.IndexOf(key); if (i < 0) return "{}";
            i += key.Length;
            if (i >= json.Length || json[i] != '{') return "{}";
            var depth = 1; var j = i + 1;
            while (j < json.Length && depth > 0)
            {
                if (json[j] == '{') depth++;
                else if (json[j] == '}') depth--;
                j++;
            }
            return json[i..j];
        }

        private static string[] ExtractJsonArray(string json, string key)
        {
            var i = json.IndexOf(key); if (i < 0) return new string[0];
            i += key.Length; if (i >= json.Length || json[i] != '[') return new string[0];
            var depth = 1; var start = i + 1; var items = new List<string>();
            var objDepth = 0;
            for (var j = start; j < json.Length && depth > 0; j++)
            {
                if (json[j] == '{') objDepth++;
                else if (json[j] == '}') objDepth--;
                else if (json[j] == ',' && objDepth == 0 && depth > 0)
                {
                    items.Add(json[start..j]);
                    start = j + 1;
                }
                else if (json[j] == ']' && objDepth == 0) { depth--; items.Add(json[start..j]); }
            }
            return items.ToArray();
        }
    }
}
