using UnityEngine;
using UnityEngine.UI;

namespace TakoyakiPhysics.UI
{
    public class UIManager : MonoBehaviour
    {
        public static UIManager Instance { get; private set; }

        [Header("Panels")]
        public GameObject titlePanel;
        public GameObject gameHUD;
        public GameObject resultPanel;

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

        public void ShowTitle()
        {
            titlePanel.SetActive(true);
            gameHUD.SetActive(false);
            resultPanel.SetActive(false);
        }

        public void ShowGameHUD()
        {
            titlePanel.SetActive(false);
            gameHUD.SetActive(true);
            resultPanel.SetActive(false);
        }

        public void ShowResult()
        {
            titlePanel.SetActive(false);
            gameHUD.SetActive(false);
            resultPanel.SetActive(true);
        }

        public void UpdateResultUI(float score, string comment)
        {
            // Ideally assign to Text components. 
            // For code-first without dragging, we might need to find them or create them.
            // But let's assume valid references or just Log for this pass if UI isn't built.
            Debug.Log($"[UI] SCORE: {score:F0} / COMMENT: {comment}");
            
            // Simple child lookup if fields are not assigned (Auto-wiring)
            Text scoreText = resultPanel.GetComponentInChildren<Text>(); 
            if (scoreText != null) scoreText.text = $"Score: {score:F0}\n\n{comment}";
        }
    }
}
