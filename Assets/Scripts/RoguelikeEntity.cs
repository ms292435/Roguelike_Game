namespace Roguelike
{
    /// <summary>
    /// Core gameplay entity class defining combat and movement attributes (Health, Damage, Speed).
    /// Inherited by player and OOP entity implementations.
    /// </summary>
    public abstract class RoguelikeEntity : EntityBase
    {
        /// <summary>
        /// Current health points of the entity.
        /// </summary>
        public float Health { get; set; }

        /// <summary>
        /// Base damage multiplier or damage output dealt by the entity.
        /// </summary>
        public float Damage { get; set; }

        /// <summary>
        /// Base movement speed in world units per second.
        /// </summary>
        public float Speed { get; set; }
    }
}