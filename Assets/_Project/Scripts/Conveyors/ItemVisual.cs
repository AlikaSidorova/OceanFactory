using UnityEngine;
using OceanFactory.Data;

namespace OceanFactory.Conveyors
{
    public class ItemVisual : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer spriteRenderer;

        private Vector3 fromWorld;
        private Vector3 toWorld;

        public ItemTypeSO Item { get; private set; }

        public void Setup(ItemTypeSO item, Sprite fallbackSprite)
        {
            Item = item;
            if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                spriteRenderer.sprite = item != null ? (item.ItemOrFallback != null ? item.ItemOrFallback : fallbackSprite) : fallbackSprite;
                // Item sprites are authored with their own colors — no per-item tint here.
                spriteRenderer.color = Color.white;
                spriteRenderer.enabled = true;
            }
        }

        public void SetMotion(Vector3 from, Vector3 to)
        {
            fromWorld = from;
            toWorld = to;
        }

        public void SetStatic(Vector3 worldPos)
        {
            fromWorld = worldPos;
            toWorld = worldPos;
            transform.position = worldPos;
        }

        public void RenderAt(float t01)
        {
            transform.position = Vector3.Lerp(fromWorld, toWorld, t01);
        }

        public void Hide()
        {
            if (spriteRenderer != null) spriteRenderer.enabled = false;
        }
    }
}
