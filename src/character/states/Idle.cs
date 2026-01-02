namespace ChromaPrototype.Character;

using Godot;

public partial class CharacterLogic
{
    public partial record State
    {
        public sealed partial record Idle : State
        {
            public override Transition On(in Input.Tick input)
            {
                var data = Get<Data>();
                var magnitude = data.DesiredDirection.Length();

                if (magnitude > input.Settings.MoveThreshold)
                {
                    var velocity = ComputeDesiredVelocity(data.DesiredDirection, input.Settings.MaxSpeed);
                    Output(new Output.DesiredVelocityChanged(velocity));
                    return To<Move>();
                }

                Output(new Output.DesiredVelocityChanged(Vector3.Zero));
                return ToSelf();
            }
        }
    }
}
