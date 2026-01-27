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

        // 0. Clean up existing scene (prevent duplication)
        GameObject[] roots = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
        foreach (var r in roots)
        {
            if (r.name.StartsWith("---") || r.name == "Main Camera" || r.name == "Directional Light" || r.name == "Canvas")
            {
                DestroyImmediate(r);
            }
        }
    
        // 1. Create Core Managers
        GameObject managers = new GameObject("--- MANAGERS ---");
        
        CreateManager<GameManager>(managers);
        CreateManager<InputManager>(managers);
        CreateManager<AudioManager>(managers);
        CreateManager<HapticManager>(managers);
        CreateManager<ScoreManager>(managers);
        CreateManager<ShareManager>(managers);
        CreateManager<TakoyakiPhysics.Game.ToppingManager>(managers);

        // 2. Create UI
        GameObject uiRoot = new GameObject("--- UI ---");
        GameObject canvas = new GameObject("Canvas");
        canvas.transform.SetParent(uiRoot.transform);
        Canvas gameCanvas = canvas.AddComponent<Canvas>(); // Unique name
        gameCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.AddComponent<CanvasScaler>();
        canvas.AddComponent<GraphicRaycaster>();
        
        // Make Title transparent blue
        GameObject titlePanel = CreatePanel(canvas, "TitlePanel", new Color(0, 0, 1, 0.1f));
        GameObject hudPanel = CreatePanel(canvas, "GameHUD", Color.clear);
        GameObject resultPanel = CreatePanel(canvas, "ResultPanel", new Color(0, 1, 0, 0.3f));
        
        // Add "Tap to Start" Hint
        GameObject startTextObj = new GameObject("StartText");
        startTextObj.transform.SetParent(titlePanel.transform, false);
        Text startText = startTextObj.AddComponent<Text>();
        startText.text = "TAP TO START";
        startText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        startText.color = Color.white;
        startText.fontSize = 60;
        startText.alignment = TextAnchor.MiddleCenter;
        startTextObj.GetComponent<RectTransform>().sizeDelta = new Vector2(800, 200);

        // Result Score UI
        GameObject scoreTextObj = new GameObject("ScoreText");
        scoreTextObj.transform.SetParent(resultPanel.transform, false);
        Text scoreText = scoreTextObj.AddComponent<Text>();
        scoreText.text = "Score: --";
        scoreText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        scoreText.color = Color.white;
        scoreText.fontSize = 50;
        scoreText.alignment = TextAnchor.MiddleCenter;
        scoreTextObj.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 50);
        scoreTextObj.GetComponent<RectTransform>().sizeDelta = new Vector2(800, 600);
        
        // HUD Instruction
        GameObject hudTextObj = new GameObject("ControlHint");
        hudTextObj.transform.SetParent(hudPanel.transform, false);
        Text hudText = hudTextObj.AddComponent<Text>();
        hudText.text = "TAP BALLS TO ADD TOPPINGS!\nPRESS 'S' TO FINISH";
        hudText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        hudText.color = new Color(1f, 1f, 1f, 0.8f);
        hudText.fontSize = 30;
        hudText.alignment = TextAnchor.LowerCenter;
        hudTextObj.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 50);
        hudTextObj.GetComponent<RectTransform>().sizeDelta = new Vector2(800, 100);
        
        resultPanel.SetActive(false);
        hudPanel.SetActive(false);

        UIManager uiMgr = CreateManager<UIManager>(managers);
        uiMgr.titlePanel = titlePanel;
        uiMgr.gameHUD = hudPanel;
        uiMgr.resultPanel = resultPanel;

        // 3. Create Game World
        GameObject world = new GameObject("--- WORLD ---");
        
        // Pan (Procedural Realism)
        GameObject pan = new GameObject("TakoyakiPan");
        pan.transform.SetParent(world.transform);
        pan.transform.position = Vector3.zero;
        
        MeshFilter mf = pan.AddComponent<MeshFilter>();
        MeshRenderer panMr = pan.AddComponent<MeshRenderer>();
        MeshCollider mc = pan.AddComponent<MeshCollider>();
        
        // Generate Mesh (3x3 Grid)
        int rows = 3;
        int cols = 3;
        float spacing = 1.4f; 
        
        Mesh panMesh = ProceduralPanMesh.Generate(rows, cols, spacing);
        mf.mesh = panMesh;
        mc.sharedMesh = panMesh; 
        
        // Material for Pan (Iron)
        string ironShaderPath = "Assets/Scripts/Visuals/IronPan.shader";
        AssetDatabase.ImportAsset(ironShaderPath, ImportAssetOptions.ForceUpdate);
        
        Shader ironShader = AssetDatabase.LoadAssetAtPath<Shader>(ironShaderPath);
        if (ironShader == null) ironShader = Shader.Find("Takoyaki/IronPan");
        
        Material panMat = new Material(ironShader != null ? ironShader : Shader.Find("Standard"));
        if (ironShader == null) panMat.color = Color.black; 
        
        panMat.SetFloat("_NoiseScale", 200.0f); 
        panMr.sharedMaterial = panMat;

        // Material for Takoyaki (Shared) - Clean Logic
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
        takoMat.SetTexture("_MainTex", ProceduralTextureGen.GenerateBatterTexture());
        takoMat.SetTexture("_CookedTex", ProceduralTextureGen.GenerateCookedTexture());
        takoMat.SetTexture("_BurntTex", ProceduralTextureGen.GenerateBurntTexture());
        takoMat.SetTexture("_NoiseTex", ProceduralTextureGen.GenerateNoiseMap());
        takoMat.SetFloat("_SSSIntensity", 0.5f);
        takoMat.SetColor("_SSSColor", new Color(1f, 0.8f, 0.6f));
        takoMat.SetFloat("_OilFresnel", 5.0f);
        takoMat.SetFloat("_OilRoughness", 0.2f);
        takoMat.SetFloat("_DisplacementStrength", 0.05f); 

        // Spawn Takoyaki Balls (Loop)
        float startX = -((cols - 1) * spacing) / 2.0f;
        float startZ = -((rows - 1) * spacing) / 2.0f;

        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++) // Loop logic ok now since canvas 'c' is renamed
            {
                GameObject tako = new GameObject($"Takoyaki_Player_{r}_{c}");
                tako.transform.SetParent(world.transform);
                
                // Position above each pit
                Vector3 pitPos = new Vector3(startX + c * spacing, 0.5f, startZ + r * spacing);
                tako.transform.position = pitPos;
                tako.transform.localScale = Vector3.one * 0.8f; 

                MeshFilter takoMf = tako.AddComponent<MeshFilter>();
                MeshRenderer takoMr = tako.AddComponent<MeshRenderer>();
                
                takoMf.mesh = ProceduralBallMesh.Generate(250); 
                takoMr.sharedMaterial = takoMat; // Assign the shared material created earlier
                
                SphereCollider sc = tako.AddComponent<SphereCollider>(); 
                sc.radius = 1.0f;

                TakoyakiController ctrl = tako.AddComponent<TakoyakiController>();
                tako.AddComponent<TakoyakiVisuals>();
                tako.AddComponent<TakoyakiSoftBody>();
                tako.AddComponent<ParticleController>(); 
                tako.AddComponent<RuntimeTextureSetup>();
                tako.AddComponent<ToppingVisuals>();
                
                // Rigidbody is added by RequireComponent on TakoyakiController!
                // So we GET it instead of ADDing it to avoid error.
                Rigidbody rb = tako.GetComponent<Rigidbody>();
                if (rb == null) rb = tako.AddComponent<Rigidbody>(); // Fallback
                
                rb.mass = 0.5f;
                rb.linearDamping = 0.5f;
                rb.angularDamping = 0.5f;
                
                ctrl.Rb = rb;
                ctrl.MeshRenderer = takoMr;
            }
        }

        // Camera Update
        GameObject camObj = new GameObject("Main Camera");
        camObj.transform.SetParent(world.transform);
        camObj.transform.position = new Vector3(0, 8.5f, -6.0f); 
        camObj.transform.LookAt(Vector3.zero);
        Camera cam = camObj.AddComponent<Camera>();
        camObj.AddComponent<AudioListener>();

        // Lights Update
        GameObject lightObj = new GameObject("Directional Light");
        lightObj.transform.SetParent(world.transform);
        Light light = lightObj.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.2f;
        light.color = new Color(1.0f, 0.96f, 0.92f); 
        lightObj.transform.rotation = Quaternion.Euler(60, -30, 0);

        GameObject pointLightObj = new GameObject("Point Light (Warmth)");
        pointLightObj.transform.SetParent(world.transform);
        Light pointLight = pointLightObj.AddComponent<Light>();
        pointLight.type = LightType.Point;
        pointLight.intensity = 4.0f; 
        pointLight.range = 15.0f;
        pointLight.color = new Color(1.0f, 0.6f, 0.3f); 
        pointLightObj.transform.position = new Vector3(0, 4, 0);

        Debug.Log("Takoyaki Scene Structure Created Successfully! (Procedural Pan & Textures Included)");
        Selection.activeGameObject = managers;
        
        // Auto-Save the scene so it persists
        string scenePath = "Assets/Scenes/Main.unity";
        if (!System.IO.Directory.Exists("Assets/Scenes"))
        {
            System.IO.Directory.CreateDirectory("Assets/Scenes");
        }
        UnityEditor.SceneManagement.EditorSceneManager.SaveScene(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene(), scenePath);
        Debug.Log($"Scene Auto-Saved to {scenePath}");
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
}
