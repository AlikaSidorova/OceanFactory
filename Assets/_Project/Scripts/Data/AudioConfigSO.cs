using UnityEngine;

namespace OceanFactory.Data
{
    [CreateAssetMenu(fileName = "AudioConfig", menuName = "OceanFactory/Audio Config")]
    public class AudioConfigSO : ScriptableObject
    {
        [Header("Music")]
        public AudioClip menuMusic;
        public AudioClip gameMusic;

        [Header("SFX")]
        public AudioClip uiClick;
        public AudioClip buildingPlace;
        public AudioClip buildingRemove;
        public AudioClip conveyorPlace;
        public AudioClip conveyorRemove;
        public AudioClip itemDelivered;

        [Header("Volumes")]
        [Range(0, 1)] public float master = 0.8f;
        [Range(0, 1)] public float music = 0.6f;
        [Range(0, 1)] public float sfx = 0.9f;
    }
}
