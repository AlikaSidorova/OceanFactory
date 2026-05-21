using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using OceanFactory.Buildings;
using OceanFactory.Core;

namespace OceanFactory.UI
{
    public class BuildPanelController : MonoBehaviour
    {
        [SerializeField] private Button extractorButton;
        [SerializeField] private Button conveyorButton;
        [SerializeField] private Button assemblerButton;
        [SerializeField] private Button removeButton;
        [Tooltip("Optional. If assigned, clicking it returns to the main menu scene.")]
        [SerializeField] private Button mainMenuButton;

        private void OnEnable()
        {
            Bind(extractorButton,  nameof(extractorButton),  () => SetMode(BuildMode.Extractor));
            Bind(conveyorButton,   nameof(conveyorButton),   () => SetMode(BuildMode.Conveyor));
            Bind(assemblerButton,  nameof(assemblerButton),  () => SetMode(BuildMode.Assembler));
            Bind(removeButton,     nameof(removeButton),     () => SetMode(BuildMode.Remove));
            Bind(mainMenuButton,   nameof(mainMenuButton),   GoToMainMenu);
        }

        private void OnDisable()
        {
            if (extractorButton)  extractorButton.onClick.RemoveAllListeners();
            if (conveyorButton)   conveyorButton.onClick.RemoveAllListeners();
            if (assemblerButton)  assemblerButton.onClick.RemoveAllListeners();
            if (removeButton)     removeButton.onClick.RemoveAllListeners();
            if (mainMenuButton)   mainMenuButton.onClick.RemoveAllListeners();
        }

        private static void Bind(Button b, string fieldName, UnityEngine.Events.UnityAction action)
        {
            if (b == null)
            {
                Debug.LogWarning($"[BuildPanel] '{fieldName}' is not assigned in Inspector — button disabled.");
                return;
            }
            b.onClick.AddListener(action);
        }

        private static void SetMode(BuildMode mode)
        {
            if (Services.TryGet<BuildModeService>(out var bms))
            {
                bms.SetMode(mode);
            }
            else
            {
                Debug.LogWarning($"[BuildPanel] BuildModeService not registered — cannot set mode {mode}. Make sure BuildModeService component is in [Systems].");
            }
        }

        private static void GoToMainMenu()
        {
            // Pause panel can leave timescale at 0 if the player exits without resuming first.
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneRoutes.MainMenu);
        }
    }
}
