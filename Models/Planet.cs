﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace HeartsAndMinds.Models {
    internal class Planet : IEquatable<Planet>
    {
        public int Id { get; set; }
        public float X { get; set; }
        public float Y { get; set; }
        public float Radius { get; set; }
        public int? Owner { get; set; }

        // -1 - planet is neutral
        //  0 - planet is on the front-line (bordering neutral or enemy planet)
        // >0 - the number of steps to reach an own planet on the front-line
        public int? DistanceToFrontLine { get; set; }

        public float Health { get; set; }

        public int[] Neighbors { get; set; }

        public List<Planet> NeighbouringPlanets { get; set; }

        public List<PlanetState> FutureHealth { get; set; }

        public bool WillLoseControlInTheFuture
        {
            get
            {
                return this.FutureHealth.Where(state => state.Owner != this.Owner).Any();
            }
        }

        public bool IsOnFrontline {  get { return (DistanceToFrontLine ?? -1) == 0; } }

        public float DistanceTo(Planet other) {
            var dx = other.X - X;
            var dy = other.Y - Y;
            return MathF.Sqrt(dx * dx + dy * dy);
        }
        public override string ToString()
        {
            return $"planet {Id} - ({X},{Y}) - Owner {Owner} - Distance {DistanceToFrontLine}";
        }

        public override bool Equals(object obj)
        {
            if ((obj == null) || !(obj is Planet)) return false;

            return ((Planet)obj).Id == this.Id;
        }

        public bool Equals(Planet other)
        {
            return other != null &&
                   Id == other.Id;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Id);
        }
    }
}