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
    }
}
