using UnityEngine;
using OceanFactory.Core;

namespace OceanFactory.UI
{
    public class HudController : MonoBehaviour
    {
        [SerializeField] private GameObject buildPanelRoot;
        [SerializeField] private bool hideBuildPanelOutsidePlaying = false;

        private void OnEnable()
        {
            EventBus.Subscribe<GameStateChangedEvent>(OnStateChanged);
            ApplyState();
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<GameStateChangedEvent>(OnStateChanged);
        }

        private void OnStateChanged(GameStateChangedEvent _) => ApplyState();

        private void ApplyState()
        {
            if (!hideBuildPanelOutsidePlaying) return;
            if (buildPanelRoot == null) return;
            bool playing = Services.TryGet<GameStateManager>(out var gsm) && gsm.IsPlaying;
            buildPanelRoot.SetActive(playing);
        }
    }
}
