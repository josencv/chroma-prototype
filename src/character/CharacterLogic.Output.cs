namespace ChromaPrototype.Character;

using Godot;

public partial class CharacterLogic
{
    public static class Output
    {
        /// <summary>
        /// Emitted when the desired velocity changes.
        /// The controller uses this to set the CharacterBody3D velocity.
        /// </summary>
        public readonly record struct DesiredVelocityChanged(Vector3 Velocity);
    }
}
