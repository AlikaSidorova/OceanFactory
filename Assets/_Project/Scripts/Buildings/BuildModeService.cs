using System;
using UnityEngine;
using OceanFactory.Core;

namespace OceanFactory.Buildings
{
    public class BuildModeService : MonoBehaviour
    {
        [SerializeField, Tooltip("Print a console line whenever the build mode changes. Handy when buttons feel unresponsive.")]
        private bool logModeChanges = true;

        public BuildMode Current { get; private set; } = BuildMode.None;

        public event Action<BuildMode> OnChanged;

        private void Awake()
        {
            Services.Register(this);
        }

        private void OnDestroy()
        {
            Services.Unregister<BuildModeService>();
        }

        public void SetMode(BuildMode mode)
        {
            if (mode == Current) return;
            Current = mode;
            if (logModeChanges) Debug.Log($"[BuildMode] -> {mode}");
            OnChanged?.Invoke(mode);
        }
    }
}
