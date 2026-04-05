namespace Roguelike
{
    public abstract class Entity : EntityBase
    {
        public float Health { get; set; }

        public float Damage { get; set; }

        public float Speed { get; set; }
    }
}