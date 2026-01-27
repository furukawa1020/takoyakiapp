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
            ChangeState(GameState.Title);
        }

        public void ChangeState(GameState newState)
        {
            CurrentState = newState;
            Debug.Log($"Game State Changed to: {newState}");
            OnStateChanged?.Invoke(newState);
        }
    }
}
