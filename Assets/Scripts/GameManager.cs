using UnityEngine;
using System;

namespace TakoyakiPhysics
{
    public enum GameState
    {
        Title,
        Preparation,
        Pouring,
        Cooking,
        Turning,
        Finishing,
        Serving,
        Result
    }

    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        public GameState CurrentState { get; private set; }

        public event Action<GameState> OnStateChanged;
        
        // Reference to the active takoyaki logic
        public TakoyakiController[] ActiveTakoyakis; 

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            // Ideally find the Takoyaki in the scene if not set
            if (ActiveTakoyakis == null || ActiveTakoyakis.Length == 0) 
                ActiveTakoyakis = FindObjectsOfType<TakoyakiController>();
            
            if (ActiveTakoyakis != null)
            {
                foreach (var tako in ActiveTakoyakis)
                {
                    // For now, any one of them finishing pour triggers state? 
                    // Or wait for all? Let's just listen to the first one for simplicity or aggregate.
                    tako.OnPourComplete += () => {
                         if (CurrentState != GameState.Cooking) ChangeState(GameState.Cooking);
                    };
                }
            }

            ChangeState(GameState.Title);
        }
        
        private void Update()
        {
             // Global Input for Game Flow
             if (CurrentState == GameState.Title && Input.GetMouseButtonDown(0))
             {
                 ChangeState(GameState.Pouring);
             }
             
             if (CurrentState == GameState.Cooking)
             {
                 // Check for "Serve" gesture (Push forward)
                 // Using simple key/touch for now
                 if (Input.GetKeyDown(KeyCode.S) || (Input.touchCount > 1))
                 {
                     ChangeState(GameState.Result);
                 }
             }
             
             if (CurrentState == GameState.Result && Input.GetMouseButtonDown(0))
             {
                 // Restart
                 UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
             }
        }

        public void ChangeState(GameState newState)
        {
            CurrentState = newState;
            Debug.Log($"Game State Changed to: {newState}");
            OnStateChanged?.Invoke(newState);
            
            // UI Reaction
            if (UI.UIManager.Instance != null)
            {
                switch (newState)
                {
                    case GameState.Title: 
                        UI.UIManager.Instance.ShowTitle(); 
                        break;
                    case GameState.Pouring:
                    case GameState.Cooking:
                        UI.UIManager.Instance.ShowGameHUD();
                        break;
                    case GameState.Result:
                        // Calculate Score
                        // Calculate Score
                        if (Meta.ScoreManager.Instance != null && ActiveTakoyakis != null)
                        {
                            Meta.ScoreManager.Instance.CalculateScore(ActiveTakoyakis);
                             // Update Result UI
                             float score = Meta.ScoreManager.Instance.TotalScore;
                             // Use average cook level for comment
                             float avgCook = 0;
                             float avgShape = 0;
                             foreach(var t in ActiveTakoyakis) { avgCook += t.CookLevel; avgShape += t.ShapeIntegrity; }
                             avgCook /= ActiveTakoyakis.Length;
                             avgShape /= ActiveTakoyakis.Length;
                             
                             string comment = Meta.CommentGenerator.GetComment(score, avgCook, avgShape);
                             UI.UIManager.Instance.UpdateResultUI(score, comment);
                        }
                        UI.UIManager.Instance.ShowResult();
                        break;
                }
            }
        }
    }
}
