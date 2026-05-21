using UnityEngine;

namespace OceanFactory.Core
{
    public class GameStateManager : MonoBehaviour
    {
        public GameState Current { get; private set; } = GameState.Boot;

        public bool IsPlaying => Current == GameState.Playing;

        private void Awake()
        {
            Services.Register(this);
            transform.SetParent(null);
            DontDestroyOnLoad(gameObject);
        }

        private void OnDestroy()
        {
            Services.Unregister<GameStateManager>();
        }

        public void TransitionTo(GameState next)
        {
            if (next == Current) return;
            var prev = Current;
            Current = next;
            EventBus.Publish(new GameStateChangedEvent(prev, next));
        }
    }
}
