namespace ChromaPrototype.Character;

using Godot;

public partial class CharacterLogic
{
    /// <summary>
    /// Mutable data stored in the logic blackboard.
    /// States access this via Get&lt;Data&gt;().
    /// </summary>
    public sealed class Data
    {
        /// <summary>
        /// The player's desired movement direction in world space (XZ plane).
        /// X = world X, Y = world Z. Magnitude 0-1.
        /// </summary>
        public Vector2 DesiredDirection { get; set; }
    }
}
