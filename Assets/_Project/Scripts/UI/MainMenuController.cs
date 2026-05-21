using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using OceanFactory.Core;

namespace OceanFactory.UI
{
    public class MainMenuController : MonoBehaviour
    {
        [SerializeField] private Button playButton;
        [SerializeField] private Button quitButton;

        private void OnEnable()
        {
            if (playButton != null) playButton.onClick.AddListener(OnPlay);
            if (quitButton != null) quitButton.onClick.AddListener(OnQuit);
        }

        private void OnDisable()
        {
            if (playButton != null) playButton.onClick.RemoveListener(OnPlay);
            if (quitButton != null) quitButton.onClick.RemoveListener(OnQuit);
        }

        private void OnPlay()
        {
            SceneManager.LoadScene(SceneRoutes.Game);
        }

        private void OnQuit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
