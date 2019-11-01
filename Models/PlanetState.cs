namespace HeartsAndMinds.Models {
    internal class PlanetState {

        public int? Owner { get; set; }
        public float HealthBeforeGrowth { get; set; }

        public float HealthEndOfTurn { get; set; }

        public override string ToString() {
            return $"owned by {Owner}, with health before growth {HealthBeforeGrowth}, health end of turn {HealthEndOfTurn}";
        }
    }
}