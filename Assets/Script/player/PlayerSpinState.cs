using System.Collections;
using UnityEngine;

namespace Supercyan.AnimalPeopleSample
{
    public class PlayerSpinState : PlayerBaseState
    {
        private bool m_canAttack = true;
        private Coroutine m_spinCoroutine;
        private Coroutine m_cooldownCoroutine;

        private readonly string m_attackTrigger = "attack1";

        public PlayerSpinState(PlayerController player, PlayerStateMachine stateMachine) 
            : base(player, stateMachine) { }

        public override void Enter()
        {
            if (m_canAttack)
            {
                m_player.Animator.SetTrigger(m_attackTrigger);
                StartSpin();
                StartCooldown();
            }
            else
            {
                m_stateMachine.ChangeState(PlayerState.Locomotion);
            }
        }

        public override void Update() { }

        public override void FixedUpdate()
        {
            if (m_spinCoroutine != null)
            {
                m_player.transform.Rotate(0, m_player.SpinSpeed * Time.deltaTime, 0);
            }
        }

        public override void HandleInput() { }

        public override void Exit()
        {
            if (m_spinCoroutine != null)
            {
                m_player.StopCoroutine(m_spinCoroutine);
                m_spinCoroutine = null;
            }
        }

        private void StartSpin()
        {
            if (m_spinCoroutine != null) return;
            m_spinCoroutine = m_player.StartCoroutine(SpinRoutine());
        }

        private IEnumerator SpinRoutine()
        {
            m_player.UpdateAnimatorMoveSpeed(0f);
            yield return new WaitForSeconds(m_player.SpinDuration);
            m_spinCoroutine = null;
            m_stateMachine.ChangeState(PlayerState.Locomotion);
        }

        private void StartCooldown()
        {
            m_canAttack = false;
            if (m_cooldownCoroutine != null) m_player.StopCoroutine(m_cooldownCoroutine);
            m_cooldownCoroutine = m_player.StartCoroutine(CooldownRoutine());
        }

        private IEnumerator CooldownRoutine()
        {
            float cooldownTimer = m_player.AttackCooldown;
            while (cooldownTimer > 0)
            {
                m_player.OnCooldownUpdate?.Invoke(cooldownTimer / m_player.AttackCooldown);
                cooldownTimer -= Time.deltaTime;
                yield return null;
            }
            m_canAttack = true;
            m_player.OnCooldownUpdate?.Invoke(0f);
            m_cooldownCoroutine = null;
        }
    }
}