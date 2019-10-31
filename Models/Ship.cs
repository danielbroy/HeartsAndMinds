namespace HeartsAndMinds.Models {
    internal class Ship {
        public float X { get; set; }
        public float Y { get; set; }
        public int TargetId { get; set; }
        public int? Owner { get; set; }
        public float Power { get; set; }
    }
}