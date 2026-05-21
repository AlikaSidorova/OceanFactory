using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using OceanFactory.Core;
using OceanFactory.Input;

namespace OceanFactory.UI
{
    public class PauseController : MonoBehaviour
    {
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button mainMenuButton;

        private InputReader input;
        private bool isPaused;

        private void Awake()
        {
            if (panelRoot != null) panelRoot.SetActive(false);
        }

        private void OnEnable()
        {
            StartCoroutine(BindWhenReady());
            if (resumeButton != null) resumeButton.onClick.AddListener(Resume);
            if (mainMenuButton != null) mainMenuButton.onClick.AddListener(GoToMenu);
        }

        private IEnumerator BindWhenReady()
        {
            while (!Services.TryGet(out input)) yield return null;
            input.OnTogglePause += Toggle;
        }

        private void OnDisable()
        {
            if (input != null) input.OnTogglePause -= Toggle;
            if (resumeButton != null) resumeButton.onClick.RemoveListener(Resume);
            if (mainMenuButton != null) mainMenuButton.onClick.RemoveListener(GoToMenu);
        }

        // InputReader gates Esc with priority: picker close > build-mode cancel > pause toggle.
        // OnTogglePause only fires when nothing higher-priority claimed Esc, so we just toggle.
        private void Toggle()
        {
            if (isPaused) Resume();
            else Pause();
        }

        private void Pause()
        {
            isPaused = true;
            Time.timeScale = 0f;
            if (panelRoot != null) panelRoot.SetActive(true);
            if (Services.TryGet<GameStateManager>(out var gsm) && gsm.Current == GameState.Playing)
            {
                gsm.TransitionTo(GameState.Paused);
            }
        }

        private void Resume()
        {
            isPaused = false;
            Time.timeScale = 1f;
            if (panelRoot != null) panelRoot.SetActive(false);
            if (Services.TryGet<GameStateManager>(out var gsm) && gsm.Current == GameState.Paused)
            {
                gsm.TransitionTo(GameState.Playing);
            }
        }

        private void GoToMenu()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneRoutes.MainMenu);
        }
    }
}
