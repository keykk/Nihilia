// PlayerStateMachine.cs
using System.Collections.Generic;
using UnityEngine;

namespace Supercyan.AnimalPeopleSample
{
    public class PlayerStateMachine
    {
        private PlayerController m_player;
        private PlayerBaseState m_currentState;
        private Dictionary<PlayerState, PlayerBaseState> m_states;

        public PlayerStateMachine(PlayerController player)
        {
            m_player = player;
            InitializeStates();
            ChangeState(PlayerState.Locomotion);
        }

        private void InitializeStates()
        {
            m_states = new Dictionary<PlayerState, PlayerBaseState>
            {
                { PlayerState.Locomotion, new PlayerLocomotionState(m_player, this) },
                { PlayerState.Spin, new PlayerSpinState(m_player, this) },
                { PlayerState.Combo, new PlayerComboState(m_player, this) }
            };
        }

        public void Update()
        {
            m_currentState?.Update();
        }

        public void FixedUpdate()
        {
            m_currentState?.FixedUpdate();
        }

        public void HandleInput()
        {
            m_currentState?.HandleInput();
        }

        public void ChangeState(PlayerState newState)
        {
            m_currentState?.Exit();
            m_currentState = m_states[newState];
            m_currentState.Enter();
        }

        public PlayerState GetCurrentStateType()
        {
            foreach (var state in m_states)
            {
                if (state.Value == m_currentState)
                    return state.Key;
            }
            return PlayerState.Locomotion;
        }

        public bool IsInState(PlayerState state) => m_currentState == m_states[state];
    }

    public enum PlayerState
    {
        Locomotion,
        Spin,
        Combo
    }
}