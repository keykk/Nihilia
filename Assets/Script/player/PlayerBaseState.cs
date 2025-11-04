// PlayerBaseState.cs
namespace Supercyan.AnimalPeopleSample
{
    public abstract class PlayerBaseState
    {
        protected PlayerController m_player;
        protected PlayerStateMachine m_stateMachine;

        protected PlayerBaseState(PlayerController player, PlayerStateMachine stateMachine)
        {
            m_player = player;
            m_stateMachine = stateMachine;
        }

        public abstract void Enter();
        public abstract void Update();
        public abstract void FixedUpdate();
        public abstract void HandleInput();
        public abstract void Exit();
    }
}