namespace ChromaPrototype.Character;

using Chickensoft.LogicBlocks;
using Godot;

public partial class CharacterLogic
{
    public abstract partial record State : StateLogic<State>,
        IGet<Input.Tick>,
        IGet<Input.SetDesiredDirection>
    {
        /// <summary>
        /// Updates the desired direction in the blackboard.
        /// Handled at base state level so all states receive direction updates.
        /// </summary>
        public Transition On(in Input.SetDesiredDirection input)
        {
            Get<Data>().DesiredDirection = input.Direction;
            return ToSelf();
        }

        public abstract Transition On(in Input.Tick input);

        /// <summary>
        /// Computes desired velocity from direction and max speed.
        /// </summary>
        protected static Vector3 ComputeDesiredVelocity(Vector2 direction, float maxSpeed)
        {
            var dir = direction;
            if (dir.LengthSquared() > 1.0f)
            {
                dir = dir.Normalized();
            }

            // X = world X, Y (from Vector2) = world Z
            return new Vector3(dir.X, 0f, dir.Y) * maxSpeed;
        }
    }
}
