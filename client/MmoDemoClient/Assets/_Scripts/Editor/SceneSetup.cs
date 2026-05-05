using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace MmoDemo.Client.Editor
{
    /// <summary>
    /// Creates the Bootstrap scene with GameLauncher and basic UI canvases.
    /// Run from CLI: Unity -batchmode -quit -projectPath . -executeMethod MmoDemo.Client.Editor.SceneSetup.CreateAll
    /// </summary>
    public static class SceneSetup
    {
        [MenuItem("MmoDemo/Setup All Scenes")]
        public static void CreateAll()
        {
            CreatePlayerPrefabs();
            CreateUIPrefabs();
            AssetDatabase.Refresh();
            CreateBootstrapScene();
            AssetDatabase.Refresh();
            Debug.Log("[SceneSetup] All scenes and prefabs created.");
        }

        public static void CreatePlayerPrefabs()
        {
            // Local player prefab (blue capsule)
            var localPlayer = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            localPlayer.name = "LocalPlayer";
            localPlayer.GetComponent<Renderer>().sharedMaterial.color = Color.blue;
            SavePrefab(localPlayer, "LocalPlayer");

            // Other player prefab (red capsule)
            var otherPlayer = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            otherPlayer.name = "OtherPlayer";
            otherPlayer.GetComponent<Renderer>().sharedMaterial.color = Color.red;
            SavePrefab(otherPlayer, "OtherPlayer");
        }

        public static void CreateBootstrapScene()
        {
            var loginPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/Prefabs/LoginView.prefab");
            var rolePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/Prefabs/RoleSelectView.prefab");
            var cityPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/Prefabs/CityView.prefab");
            var localPlayerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/Prefabs/LocalPlayer.prefab");
            var otherPlayerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/Prefabs/OtherPlayer.prefab");

            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            // GameLauncher
            var launcherObj = new GameObject("GameLauncher");
            var launcher = launcherObj.AddComponent<GameLauncher>();
            var launcherSo = new SerializedObject(launcher);
            launcherSo.FindProperty("loginViewPrefab").objectReferenceValue = loginPrefab;
            launcherSo.FindProperty("roleSelectViewPrefab").objectReferenceValue = rolePrefab;
            launcherSo.FindProperty("cityViewPrefab").objectReferenceValue = cityPrefab;
            launcherSo.ApplyModifiedProperties();

            // GameManager (Phase 2)
            var gmObj = new GameObject("GameManager");
            var gm = gmObj.AddComponent<GameManager>();
            var gmSo = new SerializedObject(gm);
            gmSo.FindProperty("localPlayerPrefab").objectReferenceValue = localPlayerPrefab;
            gmSo.FindProperty("otherPlayerPrefab").objectReferenceValue = otherPlayerPrefab;
            gmSo.ApplyModifiedProperties();

            // EventSystem
            var eventSys = new GameObject("EventSystem");
            eventSys.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSys.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();

            EditorSceneManager.SaveScene(scene, "Assets/_Scenes/Bootstrap.unity", false);
            Debug.Log("[SceneSetup] Bootstrap scene created with Phase 2 GameManager.");
        }

        public static void CreateUIPrefabs()
        {
            CreateLoginViewPrefab();
            CreateRoleSelectViewPrefab();
            CreateCityViewPrefab();
        }

        private static void CreateLoginViewPrefab()
        {
            var go = CreateCanvas("LoginView");
            go.AddComponent<LoginView>();

            // Login button
            var btnGo = CreateUIElement("LoginButton", go.transform, new Vector2(0, 0), new Vector2(200, 60));
            var btnImg = btnGo.AddComponent<Image>();
            btnImg.color = new Color(0.3f, 0.6f, 1f);
            var btn = btnGo.AddComponent<Button>();
            var btnTxt = CreateText("Login", btnGo.transform, "Login");

            // Status text
            var status = CreateText("Connecting...", go.transform, "StatusText");
            status.rectTransform.anchoredPosition = new Vector2(0, 60);

            // Wire up references
            var view = go.GetComponent<LoginView>();
            var so = new SerializedObject(view);
            so.FindProperty("loginButton").objectReferenceValue = btn;
            so.FindProperty("statusText").objectReferenceValue = status;
            so.ApplyModifiedProperties();

            SavePrefab(go, "LoginView");
        }

        private static void CreateRoleSelectViewPrefab()
        {
            var go = CreateCanvas("RoleSelectView");
            go.AddComponent<RoleSelectView>();

            var container = new GameObject("RoleListContainer", typeof(RectTransform));
            container.transform.SetParent(go.transform, false);

            // Role button template
            var btnTpl = CreateUIElement("RoleButton", container.transform, new Vector2(0, 0), new Vector2(280, 50));
            btnTpl.AddComponent<Image>().color = new Color(0.25f, 0.25f, 0.3f);
            btnTpl.AddComponent<Button>();
            var tplTxt = CreateText("", btnTpl.transform, "");
            tplTxt.resizeTextForBestFit = true;

            // Create panel
            var createPanel = new GameObject("CreateRolePanel", typeof(RectTransform));
            createPanel.transform.SetParent(go.transform, false);

            var nameInput = CreateInputField("NameInput", createPanel.transform, new Vector2(0, 60));

            // Class selection buttons instead of Dropdown
            var warriorBtn = CreateButton("WarriorBtn", createPanel.transform, new Vector2(-80, 0), "Warrior");
            var mageBtn = CreateButton("MageBtn", createPanel.transform, new Vector2(0, 0), "Mage");
            var archerBtn = CreateButton("ArcherBtn", createPanel.transform, new Vector2(80, 0), "Archer");
            warriorBtn.GetComponent<RectTransform>().sizeDelta = new Vector2(70, 40);
            mageBtn.GetComponent<RectTransform>().sizeDelta = new Vector2(70, 40);
            archerBtn.GetComponent<RectTransform>().sizeDelta = new Vector2(70, 40);

            var createBtn = CreateButton("CreateButton", createPanel.transform, new Vector2(0, -60), "Create");

            var status = CreateText("", go.transform, "StatusText");
            status.rectTransform.anchoredPosition = new Vector2(0, 140);

            var view = go.GetComponent<RoleSelectView>();
            var so = new SerializedObject(view);
            so.FindProperty("roleListContainer").objectReferenceValue = container.transform;
            so.FindProperty("roleButtonPrefab").objectReferenceValue = btnTpl;
            so.FindProperty("createRolePanel").objectReferenceValue = createPanel;
            so.FindProperty("nameInput").objectReferenceValue = nameInput;
            so.FindProperty("warriorButton").objectReferenceValue = warriorBtn;
            so.FindProperty("mageButton").objectReferenceValue = mageBtn;
            so.FindProperty("archerButton").objectReferenceValue = archerBtn;
            so.FindProperty("createButton").objectReferenceValue = createBtn;
            so.FindProperty("statusText").objectReferenceValue = status;
            so.ApplyModifiedProperties();

            SavePrefab(go, "RoleSelectView");
        }

        private static void CreateCityViewPrefab()
        {
            var go = CreateCanvas("CityView");
            go.AddComponent<CityView>();

            var nameTxt = CreateText("PlayerName", go.transform, "NameText");
            nameTxt.rectTransform.anchoredPosition = new Vector2(0, 100);
            nameTxt.fontSize = 32;

            var levelTxt = CreateText("Level 1", go.transform, "LevelText");
            levelTxt.rectTransform.anchoredPosition = new Vector2(0, 40);
            levelTxt.fontSize = 24;

            var goldTxt = CreateText("Gold: 0", go.transform, "GoldText");
            goldTxt.rectTransform.anchoredPosition = new Vector2(0, 0);
            goldTxt.fontSize = 24;

            var statusTxt = CreateText("Entering city...", go.transform, "StatusText");
            statusTxt.rectTransform.anchoredPosition = new Vector2(0, -60);

            var view = go.GetComponent<CityView>();
            var so = new SerializedObject(view);
            so.FindProperty("nameText").objectReferenceValue = nameTxt;
            so.FindProperty("levelText").objectReferenceValue = levelTxt;
            so.FindProperty("goldText").objectReferenceValue = goldTxt;
            so.FindProperty("statusText").objectReferenceValue = statusTxt;
            so.ApplyModifiedProperties();

            SavePrefab(go, "CityView");
        }

        // ── Helpers ──

        private static GameObject CreateCanvas(string name)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(800, 600);
            return go;
        }

        private static GameObject CreateUIElement(string name, Transform parent, Vector2 pos, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            return go;
        }

        private static Text CreateText(string text, Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var txt = go.AddComponent<Text>();
            txt.text = text;
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.fontSize = 18;
            txt.color = Color.white;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.rectTransform.sizeDelta = new Vector2(300, 40);
            return txt;
        }

        private static Button CreateButton(string name, Transform parent, Vector2 pos, string label)
        {
            var go = CreateUIElement(name, parent, pos, new Vector2(200, 50));
            go.AddComponent<Image>().color = new Color(0.3f, 0.6f, 1f);
            var btn = go.AddComponent<Button>();
            CreateText(label, go.transform, "Label").text = label;
            return btn;
        }

        private static InputField CreateInputField(string name, Transform parent, Vector2 pos)
        {
            var go = CreateUIElement(name, parent, pos, new Vector2(200, 40));
            go.AddComponent<Image>().color = Color.white;
            var ifield = go.AddComponent<InputField>();

            // Placeholder text
            var placeholder = new GameObject("Placeholder", typeof(RectTransform), typeof(Text));
            placeholder.transform.SetParent(go.transform, false);
            var pt = placeholder.GetComponent<Text>();
            pt.text = "Enter name...";
            pt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            pt.fontSize = 16;
            pt.color = Color.gray;
            pt.alignment = TextAnchor.MiddleLeft;
            ifield.placeholder = pt;

            // Text component
            var text = new GameObject("Text", typeof(RectTransform), typeof(Text));
            text.transform.SetParent(go.transform, false);
            var tt = text.GetComponent<Text>();
            tt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            tt.fontSize = 16;
            tt.color = Color.black;
            tt.alignment = TextAnchor.MiddleLeft;
            tt.rectTransform.sizeDelta = new Vector2(190, 30);
            ifield.textComponent = tt;

            return ifield;
        }

        private static void SavePrefab(GameObject go, string name)
        {
            var path = $"Assets/Resources/Prefabs/{name}.prefab";
            PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
        }
    }
}
