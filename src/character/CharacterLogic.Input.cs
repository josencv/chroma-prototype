namespace ChromaPrototype.Character;

using Godot;

public partial class CharacterLogic
{
    public static class Input
    {
        /// <summary>
        /// Per-frame physics tick.
        /// </summary>
        public readonly record struct Tick(double DeltaSeconds, Settings Settings);

        /// <summary>
        /// Updates the desired movement direction (in world space).
        /// </summary>
        public readonly record struct SetDesiredDirection(Vector2 Direction);
    }

    /// <summary>
    /// Character movement settings.
    /// </summary>
    public readonly record struct Settings(
        float MaxSpeed = 6.0f,
        float MoveThreshold = 0.1f);
}
