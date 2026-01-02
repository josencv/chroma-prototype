namespace ChromaPrototype.Character;

using Chickensoft.Introspection;
using Chickensoft.LogicBlocks;

[Meta]
[LogicBlock(typeof(State))]
public partial class CharacterLogic : LogicBlock<CharacterLogic.State>
{
    public CharacterLogic()
    {
        Set(new Data());
    }

    public override Transition GetInitialState() => To<State.Idle>();
}
