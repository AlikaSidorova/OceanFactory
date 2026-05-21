using UnityEngine;
using OceanFactory.Core;
using OceanFactory.Data;

namespace OceanFactory.Conveyors
{
    /// <summary>
    /// One conveyor cell: exactly one input side and one output side.
    /// Sprites (at rotation 0, no flip):
    ///   straight — flow enters from South, exits to North.
    ///   turn     — flow enters from West, exits to North (CCW / "left" bend).
    /// InDir = compass side items arrive from (upstream). OutDir = side items leave toward.
    /// </summary>
    public class ConveyorComponent : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer beltRenderer;

        [Header("Sprites")]
        [SerializeField, Tooltip("Canonical straight — South in, North out (vertical, arrows up).")]
        private Sprite straightSprite;
        [SerializeField, Tooltip("Canonical turn — West in, North out (L bend up-right).")]
        private Sprite turnSprite;

        public Vector2Int Cell { get; private set; }
        public Direction OutDir { get; private set; } = Direction.North;
        public Direction InDir  { get; private set; } = Direction.South;
        public ItemTypeSO HeldItem { get; private set; }
        public ItemVisual Visual { get; private set; }

        public bool HasItem => HeldItem != null;
        public Sprite StraightSprite => straightSprite;
        public Sprite TurnSprite => turnSprite;

        public void Configure(Vector2Int cell, Direction outDir)
        {
            Cell = cell;
            OutDir = outDir;
            InDir = outDir.Opposite();
            RefreshVisual();
        }

        public void SetFlow(Direction inDir, Direction outDir)
        {
            InDir = inDir;
            OutDir = outDir;
            RefreshVisual();
        }

        public void SetOutDir(Direction outDir)
        {
            if (OutDir == outDir) return;
            OutDir = outDir;
            RefreshVisual();
        }

        public void SetInDir(Direction inDir)
        {
            if (InDir == inDir) return;
            InDir = inDir;
            RefreshVisual();
        }

        public bool TryPlaceItem(ItemTypeSO item, ItemVisual visual)
        {
            if (HeldItem != null) return false;
            HeldItem = item;
            Visual = visual;
            return true;
        }

        public void ClearItem()
        {
            HeldItem = null;
            Visual = null;
        }

        /// <summary>
        /// Apply belt sprite, flip, and Z rotation for given (inDir, outDir).
        /// Canonical sprites: straight = S in / N out; turn = W in / N out (a left/CCW bend).
        /// Math:
        ///   rotation always = OutDir.ToZAngleDeg()  (aligns canonical N edge with desired exit edge)
        ///   straight  -> flip=false
        ///   left turn  (flow turns CCW, OutDir = InDir.RotateCW())  -> flip=false  // matches canonical
        ///   right turn (flow turns CW,  OutDir = InDir.RotateCCW()) -> flip=true   // mirror canonical
        /// </summary>
        public static void ApplyVisual(
            SpriteRenderer renderer,
            Transform target,
            Direction inDir,
            Direction outDir,
            Sprite straight,
            Sprite turn)
        {
            if (renderer == null || target == null) return;

            bool isStraight = inDir.Opposite() == outDir;
            if (isStraight)
            {
                if (straight != null) renderer.sprite = straight;
                renderer.flipX = false;
            }
            else
            {
                if (turn != null) renderer.sprite = turn;
                renderer.flipX = (outDir == inDir.RotateCCW());
            }

            target.rotation = Quaternion.Euler(0f, 0f, outDir.ToZAngleDeg());
        }

        private void RefreshVisual()
        {
            if (beltRenderer == null) return;
            ApplyVisual(beltRenderer, transform, InDir, OutDir, straightSprite, turnSprite);
        }

        private void Start() => RefreshVisual();
    }
}
