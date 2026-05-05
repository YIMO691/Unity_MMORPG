using System;
using System.Collections.Generic;
using UnityEngine;

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
        private string _playerId, _token, _roleId, _myEntityId;
        private readonly Dictionary<string, GameObject> _entities = new();
        private readonly Dictionary<string, GameObject> _drops = new();
        private GameObject _localPlayer;
        private int _score;

        public bool IsReady { get; private set; }

        // Phase 4 events for UI overlays
        public event Action<string, string> OnChatReceived; // sender, text
        public event Action<string> OnQuestUpdated;          // status text
        public event Action<string> OnQuestCompleted;        // completion text

        // ═══════════ Connection ═══════════

        public async void Connect(string playerId, string token, string roleId)
        {
            _playerId = playerId; _token = token; _roleId = roleId;
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
            Debug.Log($"[Game] Received: type={type} payload={payload[..Math.Min(payload.Length, 100)]}");
            switch (type)
            {
                case "s2c.auth_result":
                    Debug.Log("[Game] Auth response: " + payload);
                    if (ExtractBool(payload, "\"ok\":")) SendEnterScene("city_001");
                    else Debug.LogError("[Game] Auth failed: " + payload);
                    break;
                case "s2c.enter_scene_result":
                    HandleEnterScene(payload); break;
                case "s2c.entity_snapshot":
                    HandleSnapshot(payload); break;
                case "s2c.entity_joined":
                    SpawnEntity(ExtractNested(payload, "\"entity\":"));
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
            if (!ExtractBool(p, "\"ok\":")) return;
            var sx = ExtractFloat(p, "\"spawnX\":");
            var sz = ExtractFloat(p, "\"spawnZ\":");
            _localPlayer = Instantiate(localPlayerPrefab, new Vector3(sx, 1, sz), Quaternion.identity);
            _localPlayer.GetComponent<Renderer>().material.color = Color.blue;
            _myEntityId = ExtractString(p, "\"entityId\":\"");

            // Hide center HUD text but keep canvas active for chat/quest overlays
            foreach (var canvas in FindObjectsOfType<Canvas>())
            {
                foreach (Transform child in canvas.transform)
                {
                    if (child.name is "NameText" or "LevelText" or "GoldText" or "StatusText")
                        child.gameObject.SetActive(false);
                }
            }

            // Spawn entities from the list (players + monsters)
            var arr = ExtractJsonArray(p, "\"entities\":");
            foreach (var ej in arr)
                SpawnEntity(ej);

            IsReady = true;
            Debug.Log("[Game] Scene entered. Score: 0");
        }

        // ═══════════ Entity Spawning ═══════════

        private void SpawnEntity(string json)
        {
            var eid = ExtractString(json, "\"entityId\":\"");
            if (string.IsNullOrEmpty(eid) || eid == _myEntityId || _entities.ContainsKey(eid)) return;

            var etype = ExtractString(json, "\"type\":\"");
            var x = ExtractFloat(json, "\"posX\":");
            var z = ExtractFloat(json, "\"posZ\":");
            var name = ExtractString(json, "\"name\":\"");

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
            Debug.Log($"[Game] Monster killed! +{exp} exp +{gold} gold. Score: {_score}");
        }

        // ═══════════ Phase 3: Drops ═══════════

        private void HandleDropSpawned(string p)
        {
            var dropId = ExtractString(p, "\"dropId\":\"");
            var itemName = ExtractString(p, "\"itemName\":\"");
            var x = ExtractFloat(p, "\"posX\":");
            var z = ExtractFloat(p, "\"posZ\":");
            var go = Instantiate(dropPrefab, new Vector3(x, 0.5f, z), Quaternion.identity);
            go.name = itemName;
            go.GetComponent<Renderer>().material.color = Color.yellow;
            _drops[dropId] = go;
        }

        private void HandleDropPickedUp(string p)
        {
            var dropId = ExtractString(p, "\"dropId\":\"");
            if (_drops.TryGetValue(dropId, out var go)) { Destroy(go); _drops.Remove(dropId); }
        }

        private void HandleInventory(string p)
        {
            var count = 0;
            foreach (var item in ExtractJsonArray(p, "\"items\":"))
                count++;
            Debug.Log($"[Game] Inventory: {count} items");
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
            Debug.Log($"[Game] Quest updated: {name} {progress}/{target}");
        }

        private void HandleQuestCompleted(string p)
        {
            var name = ExtractString(p, "\"name\":\"");
            var exp = (int)ExtractFloat(p, "\"expReward\":");
            var gold = (int)ExtractFloat(p, "\"goldReward\":");
            OnQuestCompleted?.Invoke($"Quest Complete: {name}! +{exp} exp, +{gold} gold");
            _score += gold;
            Debug.Log($"[Game] Quest completed: {name} +{exp}exp +{gold}gold");
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
            var h = Input.GetAxis("Horizontal");
            var v = Input.GetAxis("Vertical");
            if (Mathf.Abs(h) > 0.01f || Mathf.Abs(v) > 0.01f)
            {
                var spd = 5f * Time.deltaTime;
                var pos = _localPlayer.transform.position;
                pos.x += h * spd; pos.z += v * spd;
                _localPlayer.transform.position = pos;
                SendMove(h, v, pos.x, pos.z);
            }

            // Skill hotkeys 1/2/3 → attack nearest monster
            for (int skill = 1; skill <= 3; skill++)
            {
                if (Input.GetKeyDown(KeyCode.Alpha0 + skill))
                    AttackNearest(skill);
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
            Debug.Log($"[Game] Cast skill {skillId} → {_nearestMonsterId}");
        }

        private void OnDisconnected(string reason)
        {
            Debug.LogWarning("[Game] Disconnected: " + reason);
            IsReady = false;
        }

        private void OnDestroy() => _ws?.Dispose();

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
            var end = json.IndexOf('"', i);
            return end < 0 ? json[i..] : json[i..end];
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
