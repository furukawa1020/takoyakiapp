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
        public TakoyakiController ActiveTakoyaki; 

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
            if (ActiveTakoyaki == null) ActiveTakoyaki = FindObjectOfType<TakoyakiController>();
            
            if (ActiveTakoyaki != null)
            {
                ActiveTakoyaki.OnPourComplete += () => ChangeState(GameState.Cooking);
                ActiveTakoyaki.OnBurn += () => {
                     // Maybe Game Over or just visual? 
                     // For now, let's keep cooking but show fail UI later
                };
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
                        if (Meta.ScoreManager.Instance != null && ActiveTakoyaki != null)
                        {
                            Meta.ScoreManager.Instance.CalculateScore(ActiveTakoyaki);
                             // Update Result UI
                             float score = Meta.ScoreManager.Instance.TotalScore;
                             string comment = Meta.CommentGenerator.GetComment(score, ActiveTakoyaki.CookLevel, ActiveTakoyaki.ShapeIntegrity);
                             UI.UIManager.Instance.UpdateResultUI(score, comment);
                        }
                        UI.UIManager.Instance.ShowResult();
                        break;
                }
            }
        }
    }
}
