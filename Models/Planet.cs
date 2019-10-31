using System;

namespace HeartsAndMinds.Models {
    internal class Planet {
        public int Id { get; set; }
        public float X { get; set; }
        public float Y { get; set; }
        public float Radius { get; set; }
        public int? Owner { get; set; }

        public float Health { get; set; }

        public int[] Neighbors { get; set; }

        public float DistanceTo(Planet other) {
            var dx = other.X - X;
            var dy = other.Y - Y;
            return MathF.Sqrt(dx * dx + dy * dy);
        }
    }
}