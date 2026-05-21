using UnityEngine;
using OceanFactory.Core;
using OceanFactory.Data;

namespace OceanFactory.Audio
{
    public class AudioManager : MonoBehaviour
    {
        [SerializeField] private AudioConfigSO config;
        [SerializeField] private AudioSource musicSource;
        [SerializeField] private AudioSource sfxSource;

        private void Awake()
        {
            Services.Register(this);
            transform.SetParent(null);
            DontDestroyOnLoad(gameObject);
            ApplyVolumes();
        }

        private void OnEnable()
        {
            EventBus.Subscribe<BuildingPlacedEvent>(OnBuildingPlaced);
            EventBus.Subscribe<BuildingRemovedEvent>(OnBuildingRemoved);
            EventBus.Subscribe<ConveyorPlacedEvent>(OnConveyorPlaced);
            EventBus.Subscribe<ConveyorRemovedEvent>(OnConveyorRemoved);
            EventBus.Subscribe<ItemDeliveredEvent>(OnItemDelivered);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<BuildingPlacedEvent>(OnBuildingPlaced);
            EventBus.Unsubscribe<BuildingRemovedEvent>(OnBuildingRemoved);
            EventBus.Unsubscribe<ConveyorPlacedEvent>(OnConveyorPlaced);
            EventBus.Unsubscribe<ConveyorRemovedEvent>(OnConveyorRemoved);
            EventBus.Unsubscribe<ItemDeliveredEvent>(OnItemDelivered);
        }

        private void OnDestroy()
        {
            Services.Unregister<AudioManager>();
        }

        private void ApplyVolumes()
        {
            if (config == null) return;
            if (musicSource != null) musicSource.volume = config.master * config.music;
            if (sfxSource != null) sfxSource.volume = config.master * config.sfx;
        }

        public void PlayMusic(AudioClip clip, bool loop = true)
        {
            if (clip == null || musicSource == null) return;
            musicSource.clip = clip;
            musicSource.loop = loop;
            musicSource.Play();
        }

        public void StopMusic()
        {
            if (musicSource != null) musicSource.Stop();
        }

        public void PlaySfx(AudioClip clip)
        {
            if (clip == null || sfxSource == null) return;
            sfxSource.PlayOneShot(clip);
        }

        public void SetMaster(float v)
        {
            if (config != null) config.master = Mathf.Clamp01(v);
            ApplyVolumes();
        }

        private void OnBuildingPlaced(BuildingPlacedEvent _) => PlaySfx(config != null ? config.buildingPlace : null);
        private void OnBuildingRemoved(BuildingRemovedEvent _) => PlaySfx(config != null ? config.buildingRemove : null);
        private void OnConveyorPlaced(ConveyorPlacedEvent _) => PlaySfx(config != null ? config.conveyorPlace : null);
        private void OnConveyorRemoved(ConveyorRemovedEvent _) => PlaySfx(config != null ? config.conveyorRemove : null);
        private void OnItemDelivered(ItemDeliveredEvent _) => PlaySfx(config != null ? config.itemDelivered : null);
    }
}
