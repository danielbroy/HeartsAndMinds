namespace HeartsAndMinds.Models {
    internal class Move {
        public Move(float power, int source, int target)
        {
            Power = power;
            Source = source;
            Target = target;
        }

        public float Power { get; set; }
        public int Source { get; set; }
        public int Target { get; set; }

        public override string ToString() {
            return $"send-ship {Power} {Source} {Target}";
        }
    }
}