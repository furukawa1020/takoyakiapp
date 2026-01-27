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

        // 2. Create UI
        GameObject uiRoot = new GameObject("--- UI ---");
        GameObject canvas = new GameObject("Canvas");
        canvas.transform.SetParent(uiRoot.transform);
        Canvas c = canvas.AddComponent<Canvas>();
        c.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.AddComponent<CanvasScaler>();
        canvas.AddComponent<GraphicRaycaster>();
        
        // Make Title transparent blue so we can see the Takoyaki waiting
        GameObject titlePanel = CreatePanel(canvas, "TitlePanel", new Color(0, 0, 1, 0.1f));
        GameObject hudPanel = CreatePanel(canvas, "GameHUD", Color.clear);
        GameObject resultPanel = CreatePanel(canvas, "ResultPanel", new Color(0, 1, 0, 0.3f));
        
        // Add "Tap to Start" Hint (Simulated by empty GameObject name for now as we don't have Text setup logic fully automated)
        GameObject hint = new GameObject("TEXT: TAP TO START");
        hint.transform.SetParent(titlePanel.transform);
        
        resultPanel.SetActive(false);
        hudPanel.SetActive(false);

        UIManager uiMgr = CreateManager<UIManager>(managers);
        uiMgr.titlePanel = titlePanel;
        uiMgr.gameHUD = hudPanel;
        uiMgr.resultPanel = resultPanel;

        // 3. Create Game World
        GameObject world = new GameObject("--- WORLD ---");
        
        // Pan (Static)
        GameObject pan = GameObject.CreatePrimitive(PrimitiveType.Plane);
        pan.name = "TakoyakiPan";
        pan.transform.SetParent(world.transform);
        pan.transform.localScale = Vector3.one * 0.5f;

        // Takoyaki (The Player/Object)
        GameObject tako = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        tako.name = "Takoyaki_Player";
        tako.transform.SetParent(world.transform);
        tako.transform.position = new Vector3(0, 0.5f, 0);

        TakoyakiController ctrl = tako.AddComponent<TakoyakiController>();
        tako.AddComponent<TakoyakiVisuals>();
        // Add new High Fidelity Physics
        tako.AddComponent<TakoyakiSoftBody>();
        tako.AddComponent<ParticleController>();
        tako.AddComponent<RuntimeTextureSetup>();
        
        // Ensure Rigidbody exists
        Rigidbody rb = tako.GetComponent<Rigidbody>();
        if (rb == null) rb = tako.AddComponent<Rigidbody>();
        
        rb.mass = 0.1f; // Light dough
        ctrl.Rb = rb;
        ctrl.MeshRenderer = tako.GetComponent<Renderer>();

        // Camera
        GameObject camObj = new GameObject("Main Camera");
        camObj.transform.SetParent(world.transform);
        camObj.transform.position = new Vector3(0, 5, -5);
        camObj.transform.LookAt(Vector3.zero);
        Camera cam = camObj.AddComponent<Camera>();
        camObj.AddComponent<AudioListener>();

        // Lights
        GameObject lightObj = new GameObject("Directional Light");
        lightObj.transform.SetParent(world.transform);
        Light light = lightObj.AddComponent<Light>();
        light.type = LightType.Directional;
        lightObj.transform.rotation = Quaternion.Euler(50, -30, 0);

        // Material Setup
        Material mat = new Material(Shader.Find("Takoyaki/TakoyakiCinematic"));
        mat.SetTexture("_MainTex", ProceduralTextureGen.GenerateBatterTexture());
        mat.SetTexture("_CookedTex", ProceduralTextureGen.GenerateCookedTexture());
        mat.SetTexture("_BurntTex", ProceduralTextureGen.GenerateBurntTexture());
        mat.SetTexture("_NoiseTex", ProceduralTextureGen.GenerateNoiseMap());
        
        // Settings for "Cinematic" Look
        mat.SetFloat("_SSSIntensity", 0.5f);
        mat.SetColor("_SSSColor", new Color(1f, 0.8f, 0.6f));
        mat.SetFloat("_OilFresnel", 5.0f);
        mat.SetFloat("_OilRoughness", 0.2f);
        mat.SetFloat("_DisplacementStrength", 0.05f); // Visible displacement

        MeshRenderer mr = tako.GetComponent<MeshRenderer>();
        mr.sharedMaterial = mat;

        Debug.Log("Takoyaki Scene Structure Created Successfully! (Procedural Textures Included)");
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
