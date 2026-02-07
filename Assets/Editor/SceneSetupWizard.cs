using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TakoyakiPhysics;
using TakoyakiPhysics.Visuals;
using TakoyakiPhysics.Feedback;
using TakoyakiPhysics.UI;
using TakoyakiPhysics.Meta;

public class SceneSetupWizard : EditorWindow
{
    [MenuItem("Takoyaki/Setup/Create Game Scene")]
    public static void CreateGameScene()
    {
        if (Application.isPlaying)
        {
            EditorUtility.DisplayDialog("Error", "Cannot setup scene while Game is Playing. Please stop play mode first.", "OK");
            return;
        }

        try
        {
            // 0. Clean up existing scene
            GameObject[] roots = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
            foreach (var r in roots)
            {
                if (r.name.StartsWith("---") || r.name == "Main Camera" || r.name == "Directional Light" || r.name == "Canvas" || r.name == "EventSystem")
                {
                    DestroyImmediate(r);
                }
            }

            // ---------------------------------------------------------
            // 1. CREATE GAME WORLD (Priority: Physics & Visuals first)
            // ---------------------------------------------------------
            GameObject world = new GameObject("--- WORLD ---");
            
            // Pan
            GameObject pan = new GameObject("TakoyakiPan");
            pan.transform.SetParent(world.transform);
            pan.transform.position = Vector3.zero;
            
            MeshFilter mf = pan.AddComponent<MeshFilter>();
            MeshRenderer panMr = pan.AddComponent<MeshRenderer>();
            MeshCollider mc = pan.AddComponent<MeshCollider>();
            mc.skinWidth = 0.005f; 
            
            int rows = 3;
            int cols = 3;
            float spacing = 1.4f; 
            
            Mesh panMesh = ProceduralPanMesh.Generate(rows, cols, spacing);
            mf.mesh = panMesh;
            mc.sharedMesh = panMesh; 
            
            // Pan Material
            string ironShaderPath = "Assets/Scripts/Visuals/IronPan.shader";
            AssetDatabase.ImportAsset(ironShaderPath, ImportAssetOptions.ForceUpdate);
            Shader ironShader = AssetDatabase.LoadAssetAtPath<Shader>(ironShaderPath);
            if (ironShader == null) ironShader = Shader.Find("Takoyaki/IronPan");
            Material panMat = new Material(ironShader != null ? ironShader : Shader.Find("Standard"));
            if (ironShader == null) panMat.color = Color.black; 
            panMat.SetFloat("_NoiseScale", 200.0f); 
            panMr.sharedMaterial = panMat;

            // Takoyaki Material
            string takoShaderPath = "Assets/Scripts/Visuals/TakoyakiCinematic.shader";
            AssetDatabase.ImportAsset(takoShaderPath, ImportAssetOptions.ForceUpdate);
            Shader takoShader = Shader.Find("Takoyaki/TakoyakiCinematic");
            if (takoShader == null) takoShader = AssetDatabase.LoadAssetAtPath<Shader>(takoShaderPath);
            
            string takoMatPath = "Assets/Materials/TakoyakiCinematic.mat";
            Material takoMat = AssetDatabase.LoadAssetAtPath<Material>(takoMatPath);
            if (takoMat == null)
            {
                takoMat = new Material(takoShader != null ? takoShader : Shader.Find("Standard"));
                AssetDatabase.CreateAsset(takoMat, takoMatPath);
            }
            else
            {
                takoMat.shader = takoShader != null ? takoShader : Shader.Find("Standard");
            }
            // Better Textures (We will improve generation next)
            takoMat.SetTexture("_MainTex", ProceduralTextureGen.GenerateBatterTexture());
            takoMat.SetTexture("_CookedTex", ProceduralTextureGen.GenerateCookedTexture());
            takoMat.SetTexture("_BurntTex", ProceduralTextureGen.GenerateBurntTexture());
            takoMat.SetTexture("_NoiseTex", ProceduralTextureGen.GenerateNoiseMap());

            // Spawn Balls interactively
            float startX = -((cols - 1) * spacing) / 2.0f;
            float startZ = -((rows - 1) * spacing) / 2.0f;

            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    GameObject tako = new GameObject($"Takoyaki_Player_{r}_{c}");
                    tako.transform.SetParent(world.transform);
                    Vector3 pitPos = new Vector3(startX + c * spacing, 0.5f, startZ + r * spacing);
                    tako.transform.position = pitPos;
                    tako.transform.localScale = Vector3.one * 0.82f; // Slight larger

                    MeshFilter takoMf = tako.AddComponent<MeshFilter>();
                    MeshRenderer takoMr = tako.AddComponent<MeshRenderer>();
                    takoMf.mesh = ProceduralBallMesh.Generate(250); 
                    takoMr.sharedMaterial = takoMat;
                    
                    SphereCollider sc = tako.AddComponent<SphereCollider>(); 
                    sc.radius = 1.0f;

                    TakoyakiController ctrl = tako.AddComponent<TakoyakiController>();
                    tako.AddComponent<TakoyakiVisuals>();
                    tako.AddComponent<TakoyakiSoftBody>();
                    tako.AddComponent<ParticleController>(); 
                    tako.AddComponent<RuntimeTextureSetup>();
                    tako.AddComponent<ToppingVisuals>();
                    
                    Rigidbody rb = tako.GetComponent<Rigidbody>();
                    if (rb == null) rb = tako.AddComponent<Rigidbody>(); 
                    
                    rb.mass = 0.5f;
                    rb.linearDamping = 0.8f; // More stable
                    rb.angularDamping = 0.8f;
                    ctrl.Rb = rb;
                    ctrl.MeshRenderer = takoMr;
                }
            }

            // Camera - Ensure functionality even if UI fails
            GameObject camObj = new GameObject("Main Camera");
            camObj.tag = "MainCamera";
            camObj.transform.SetParent(world.transform);
            camObj.transform.position = new Vector3(0, 9.5f, -6.5f); // Slightly further back
            camObj.transform.LookAt(Vector3.zero);
            Camera cam = camObj.AddComponent<Camera>();
            camObj.AddComponent<AudioListener>();

            // Lights
            GameObject lightObj = new GameObject("Directional Light");
            lightObj.transform.SetParent(world.transform);
            Light light = lightObj.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.5f;
            light.color = new Color(1.0f, 0.95f, 0.9f); 
            lightObj.transform.rotation = Quaternion.Euler(60, -30, 0);

            GameObject pointLightObj = new GameObject("Point Light (Warmth)");
            pointLightObj.transform.SetParent(world.transform);
            Light pointLight = pointLightObj.AddComponent<Light>();
            pointLight.type = LightType.Point;
            pointLight.intensity = 4.0f; 
            pointLight.range = 15.0f;
            pointLight.color = new Color(1.0f, 0.6f, 0.3f); 
            pointLightObj.transform.position = new Vector3(0, 4, 0);

            // ---------------------------------------------------------
            // 2. CREATE MANAGERS
            // ---------------------------------------------------------
            GameObject managers = new GameObject("--- MANAGERS ---");
            CreateManager<GameManager>(managers);
            CreateManager<InputManager>(managers);
            CreateManager<AudioManager>(managers);
            CreateManager<HapticManager>(managers);
            CreateManager<ScoreManager>(managers);
            CreateManager<ShareManager>(managers);
            CreateManager<TakoyakiPhysics.Game.ToppingManager>(managers);

            // ---------------------------------------------------------
            // 3. CREATE UI (Robust)
            // ---------------------------------------------------------
            try 
            {
                GameObject uiRoot = new GameObject("--- UI ---");
                
                // EventSystem (Critical for Input!)
                GameObject esObj = new GameObject("EventSystem");
                esObj.transform.SetParent(uiRoot.transform);
                esObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
                esObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();

                GameObject canvas = new GameObject("Canvas");
                canvas.transform.SetParent(uiRoot.transform);
                Canvas gameCanvas = canvas.AddComponent<Canvas>();
                gameCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.AddComponent<CanvasScaler>();
                canvas.AddComponent<GraphicRaycaster>();
                
                // Safe Font Loading
                Font uiFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
                if (uiFont == null) uiFont = Resources.GetBuiltinResource<Font>("Arial");
                
                GameObject titlePanel = CreatePanel(canvas, "TitlePanel", new Color(0, 0, 1, 0.1f));
                GameObject hudPanel = CreatePanel(canvas, "GameHUD", Color.clear);
                GameObject resultPanel = CreatePanel(canvas, "ResultPanel", new Color(0, 1, 0, 0.3f));
                
                // Title
                CreateText(titlePanel, "StartText", "TAP TO START", 60, new Vector2(0, 0), new Vector2(800, 200), uiFont);

                // HUD Instructions
                CreateText(hudPanel, "ControlHint", "TAP BALLS TO COOK!\nUSE ARROWS TO TILT!", 24, new Vector2(0, -300), new Vector2(800, 100), uiFont);

                // Arrow Controls (Anchored Bottom Right)
                CreateArrowButton(hudPanel, "ArrowUp", new Vector2(0, 1), new Vector2(350, -100), 0);
                CreateArrowButton(hudPanel, "ArrowDown", new Vector2(0, -1), new Vector2(350, -200), 180);
                CreateArrowButton(hudPanel, "ArrowLeft", new Vector2(-1, 0), new Vector2(250, -150), 90);
                CreateArrowButton(hudPanel, "ArrowRight", new Vector2(1, 0), new Vector2(450, -150), -90);

                // Result
                CreateText(resultPanel, "ScoreText", "Score: --", 50, new Vector2(0, 50), new Vector2(800, 600), uiFont);
                
                resultPanel.SetActive(false);
                hudPanel.SetActive(false);

                UIManager uiMgr = CreateManager<UIManager>(managers);
                uiMgr.titlePanel = titlePanel;
                uiMgr.gameHUD = hudPanel;
                uiMgr.resultPanel = resultPanel;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"UI Setup Failed: {e.Message}. Scene was created but UI might be missing.");
            }

            Debug.Log("Takoyaki Scene Setup Completed Successfully!");
            Selection.activeGameObject = managers;
            
            // Auto-Save
            string scenePath = "Assets/Scenes/Main.unity";
            if (!System.IO.Directory.Exists("Assets/Scenes")) System.IO.Directory.CreateDirectory("Assets/Scenes");
            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene(), scenePath);
        }
        catch (System.Exception ex)
        {
            EditorUtility.DisplayDialog("Setup Error", $"Fatal Error: {ex.Message}\nCheck Console.", "OK");
            Debug.LogException(ex);
        }
    }
    
    private static T CreateManager<T>(GameObject parent) where T : Component
    {
        GameObject obj = new GameObject(typeof(T).Name);
        obj.transform.SetParent(parent.transform);
        return obj.AddComponent<T>();
    }

    private static GameObject CreatePanel(GameObject canvas, string name, Color debugColor)
    {
        GameObject panel = new GameObject(name);
        panel.transform.SetParent(canvas.transform, false);
        RectTransform rt = panel.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one; // Stretch
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        // Add Image for debug visibility
        UnityEngine.UI.Image img = panel.AddComponent<UnityEngine.UI.Image>();
        img.color = new Color(debugColor.r, debugColor.g, debugColor.b, 0.3f);

        return panel;
    }

    private static GameObject CreateText(GameObject parent, string name, string text, int fontSize, Vector2 position, Vector2 sizeDelta, Font font)
    {
        GameObject textObj = new GameObject(name);
        textObj.transform.SetParent(parent.transform, false);
        RectTransform rt = textObj.AddComponent<RectTransform>();
        rt.anchoredPosition = position;
        rt.sizeDelta = sizeDelta;

        Text textComp = textObj.AddComponent<Text>();
        textComp.text = text;
        textComp.fontSize = fontSize;
        textComp.alignment = TextAnchor.MiddleCenter;
        textComp.color = Color.white;
        if (font != null) textComp.font = font;

        return textObj;
    }

    private static GameObject CreateArrowButton(GameObject parent, string name, Vector2 direction, Vector2 position, float rotation)
    {
        GameObject buttonObj = new GameObject(name);
        buttonObj.transform.SetParent(parent.transform, false);
        RectTransform rt = buttonObj.AddComponent<RectTransform>();
        rt.anchoredPosition = position;
        rt.sizeDelta = new Vector2(100, 100);
        rt.localRotation = Quaternion.Euler(0, 0, rotation);

        UnityEngine.UI.Image img = buttonObj.AddComponent<UnityEngine.UI.Image>();
        img.color = new Color(1, 1, 1, 0.5f);

        VirtualArrowButton vab = buttonObj.AddComponent<VirtualArrowButton>();
        vab.direction = direction;

        return buttonObj;
    }
}
