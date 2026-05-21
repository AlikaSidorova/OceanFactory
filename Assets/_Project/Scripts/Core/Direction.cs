using UnityEngine;

namespace OceanFactory.Core
{
    public enum Direction : byte
    {
        North = 0,
        East = 1,
        South = 2,
        West = 3
    }

    public static class DirectionExtensions
    {
        public static Vector2Int ToVector(this Direction d)
        {
            switch (d)
            {
                case Direction.North: return new Vector2Int(0, 1);
                case Direction.East: return new Vector2Int(1, 0);
                case Direction.South: return new Vector2Int(0, -1);
                case Direction.West: return new Vector2Int(-1, 0);
                default: return Vector2Int.zero;
            }
        }

        public static Direction Opposite(this Direction d)
        {
            switch (d)
            {
                case Direction.North: return Direction.South;
                case Direction.East: return Direction.West;
                case Direction.South: return Direction.North;
                case Direction.West: return Direction.East;
                default: return d;
            }
        }

        public static Direction RotateCW(this Direction d)
        {
            switch (d)
            {
                case Direction.North: return Direction.East;
                case Direction.East: return Direction.South;
                case Direction.South: return Direction.West;
                case Direction.West: return Direction.North;
                default: return d;
            }
        }

        public static Direction RotateCCW(this Direction d)
        {
            switch (d)
            {
                case Direction.North: return Direction.West;
                case Direction.West: return Direction.South;
                case Direction.South: return Direction.East;
                case Direction.East: return Direction.North;
                default: return d;
            }
        }

        public static float ToZAngleDeg(this Direction d)
        {
            switch (d)
            {
                case Direction.North: return 0f;
                case Direction.East: return -90f;
                case Direction.South: return 180f;
                case Direction.West: return 90f;
                default: return 0f;
            }
        }
    }
}
