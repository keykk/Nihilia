using UnityEngine;

namespace Supercyan.AnimalPeopleSample
{
    public class PlayerLocomotionState : PlayerBaseState
    {
        private enum ControlMode
        {
            Tank,
            Direct
        }

        [SerializeField] private ControlMode m_controlMode = ControlMode.Direct;

        private Vector3 m_currentDirection = Vector3.zero;

        public PlayerLocomotionState(PlayerController player, PlayerStateMachine stateMachine) 
            : base(player, stateMachine) { }

        public override void Enter()
        {
            // Reset do combo quando entra em locomotion
            m_player.OnComboUpdate?.Invoke(0);
        }

        public override void Update()
        {
            // Handle jump input specifically for locomotion
            if (m_player.JumpInput)
            {
                m_player.TriggerJump();
            }
        }

        public override void FixedUpdate()
        {
            switch (m_controlMode)
            {
                case ControlMode.Direct:
                    DirectUpdate();
                    break;
                case ControlMode.Tank:
                    TankUpdate();
                    break;
            }
        }

        public override void HandleInput()
        {
            // Right mouse button - spin attack
            if (Input.GetMouseButtonDown(1))
            {
                m_stateMachine.ChangeState(PlayerState.Spin);
                return;
            }

            // Left mouse button - combo attack
            if (Input.GetMouseButtonDown(0))
            {
                m_stateMachine.ChangeState(PlayerState.Combo);
                return;
            }
        }

        public override void Exit()
        {
            // Clean up if needed
        }

        private void TankUpdate()
        {
            float v = Input.GetAxis("Vertical");
            float h = Input.GetAxis("Horizontal");

            bool walk = Input.GetKey(KeyCode.LeftShift);

            if (v < 0)
            {
                if (walk) { v *= m_player.BackwardsWalkScale; }
                else { v *= m_player.BackwardRunScale; }
            }
            else if (walk)
            {
                v *= m_player.WalkScale;
            }

            float currentV = Mathf.Lerp(m_player.CurrentV, v, Time.deltaTime * m_player.Interpolation);
            float currentH = Mathf.Lerp(m_player.CurrentH, h, Time.deltaTime * m_player.Interpolation);

            m_player.transform.position += m_player.transform.forward * currentV * m_player.MoveSpeed * Time.deltaTime;
            m_player.transform.Rotate(0, currentH * m_player.TurnSpeed * Time.deltaTime, 0);

            m_player.UpdateAnimatorMoveSpeed(currentV);
            m_player.SetMovementValues(currentV, currentH);
        }

        private void DirectUpdate()
        {
            float v = Input.GetAxis("Vertical");
            float h = Input.GetAxis("Horizontal");

            Transform camera = Camera.main.transform;

            if (Input.GetKey(KeyCode.LeftShift))
            {
                v *= m_player.WalkScale;
                h *= m_player.WalkScale;
            }

            float currentV = Mathf.Lerp(m_player.CurrentV, v, Time.deltaTime * m_player.Interpolation);
            float currentH = Mathf.Lerp(m_player.CurrentH, h, Time.deltaTime * m_player.Interpolation);

            Vector3 direction = camera.forward * currentV + camera.right * currentH;

            float directionLength = direction.magnitude;
            direction.y = 0;
            direction = direction.normalized * directionLength;

            if (direction != Vector3.zero)
            {
                m_currentDirection = Vector3.Slerp(m_currentDirection, direction, Time.deltaTime * m_player.Interpolation);

                m_player.transform.rotation = Quaternion.LookRotation(m_currentDirection);
                m_player.transform.position += m_currentDirection * m_player.MoveSpeed * Time.deltaTime;

                m_player.UpdateAnimatorMoveSpeed(direction.magnitude);
            }

            m_player.SetMovementValues(currentV, currentH);
        }
    }
}