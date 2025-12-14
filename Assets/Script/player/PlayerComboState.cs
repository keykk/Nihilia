using System.Collections;
using UnityEngine;

namespace Supercyan.AnimalPeopleSample
{
    public class PlayerComboState : PlayerBaseState
    {
        private int m_currentCombo = 0;
        private float m_lastComboTime = 0f;
        private bool m_canAcceptComboInput = true;
        private bool m_isAnimating = false;
        private Coroutine m_comboAnimationCoroutine;
        private Coroutine m_comboTimeoutCoroutine;
        private Coroutine m_hitboxCoroutine;

        private readonly string m_combo1Trigger = "atk_combo1";
        private readonly string m_combo2Trigger = "atk_combo2";
        private readonly string m_combo3Trigger = "atk_combo3";

        public PlayerComboState(PlayerController player, PlayerStateMachine stateMachine) 
            : base(player, stateMachine) { }

        public override void Enter()
        {
            // Se já estamos em combo, processa o próximo input
            if (m_currentCombo > 0 && m_canAcceptComboInput && !m_isAnimating)
            {
                ProcessComboInput();
            }
            else
            {
                // Primeiro ataque do combo
                m_currentCombo = 1;
                TriggerComboAnimation();
                StartComboAnimationLock();
                StartComboTimeout();
            }
        }

        public override void Update()
        {
            // Verificação de timeout é feita via coroutine
        }

        public override void FixedUpdate() 
        {
            // Para movimento durante o combo
            m_player.UpdateAnimatorMoveSpeed(0f);
        }

        public override void HandleInput()
        {
            if (Input.GetMouseButtonDown(0) && m_canAcceptComboInput && !m_isAnimating)
            {
                // Se já estamos no estado de combo, processa o próximo input
                if (m_stateMachine.GetCurrentStateType() == PlayerState.Combo)
                {
                    ProcessComboInput();
                }
            }
        }

        public override void Exit()
        {
            if (m_comboAnimationCoroutine != null)
            {
                m_player.StopCoroutine(m_comboAnimationCoroutine);
                m_comboAnimationCoroutine = null;
            }
            
            if (m_comboTimeoutCoroutine != null)
            {
                m_player.StopCoroutine(m_comboTimeoutCoroutine);
                m_comboTimeoutCoroutine = null;
            }
            
            if (m_hitboxCoroutine != null)
            {
                m_player.StopCoroutine(m_hitboxCoroutine);
                m_hitboxCoroutine = null;
            }
            
            // Garante que o hitbox seja desativado ao sair do estado
            if (m_player.Atk1HitBox != null)
            {
                m_player.Atk1HitBox.SetActive(false);
            }
            
            m_isAnimating = false;
            m_canAcceptComboInput = true;
        }

        private void ProcessComboInput()
        {
            if (!m_canAcceptComboInput || m_isAnimating) return;

            m_lastComboTime = Time.time;
            
            // Prepara próximo combo
            m_currentCombo++;
            if (m_currentCombo > 3)
            {
                m_currentCombo = 1; // Reinicia após o 3º ataque
            }

            TriggerComboAnimation();
            StartComboAnimationLock();
            
            // Reinicia o timeout
            if (m_comboTimeoutCoroutine != null)
            {
                m_player.StopCoroutine(m_comboTimeoutCoroutine);
            }
            StartComboTimeout();
        }

        private void TriggerComboAnimation()
        {
            // Limpa triggers anteriores para evitar conflitos
            m_player.Animator.ResetTrigger(m_combo1Trigger);
            m_player.Animator.ResetTrigger(m_combo2Trigger);
            m_player.Animator.ResetTrigger(m_combo3Trigger);

            // Trigger do combo atual
            switch (m_currentCombo)
            {
                case 1:
                    m_player.Animator.SetTrigger(m_combo1Trigger);
                    StartHitboxTiming(); // Inicia o controle do hitbox para combo 1
                    break;
                case 2:
                    m_player.Animator.SetTrigger(m_combo2Trigger);
                    Debug.Log("Combo 2 triggered");
                    StartHitboxTiming();
                    break;
                case 3:
                    m_player.Animator.SetTrigger(m_combo3Trigger);
                    Debug.Log("Combo 3 triggered");
                    StartHitboxTiming();
                    break;
            }

            m_player.OnComboUpdate?.Invoke(m_currentCombo);
        }

        private void StartHitboxTiming()
        {
            if (m_hitboxCoroutine != null)
            {
                m_player.StopCoroutine(m_hitboxCoroutine);
            }
            m_hitboxCoroutine = m_player.StartCoroutine(HitboxTimingRoutine());
        }

        private IEnumerator HitboxTimingRoutine()
        {
            // Aguarda 0.3 segundos antes de ativar o hitbox
            yield return new WaitForSeconds(0.3f);
            
            // Ativa o hitbox
            if (m_player.Atk1HitBox != null)
            {
                m_player.Atk1HitBox.SetActive(true);
                Debug.Log("Hitbox activated");
            }
            
            // Aguarda mais 0.2 segundos (total 0.5s) antes de desativar
            yield return new WaitForSeconds(0.2f);
            
            // Desativa o hitbox
            if (m_player.Atk1HitBox != null)
            {
                m_player.Atk1HitBox.SetActive(false);
                Debug.Log("Hitbox deactivated");
            }
            
            m_hitboxCoroutine = null;
        }

        // Método mantido para compatibilidade com Animation Events se necessário
        public void Atk1Hit()
        {
            // Método chamado via Animation Event no final do combo 1
            Debug.Log("Atk1 Hit executed via Animation Event");

            // Desativar o hitbox após o ataque
            if (m_player.Atk1HitBox != null)
            {
                m_player.Atk1HitBox.SetActive(false);
            }
            
            // Para a corrotina do hitbox se ainda estiver rodando
            if (m_hitboxCoroutine != null)
            {
                m_player.StopCoroutine(m_hitboxCoroutine);
                m_hitboxCoroutine = null;
            }
        }

        private void StartComboAnimationLock()
        {
            if (m_comboAnimationCoroutine != null)
            {
                m_player.StopCoroutine(m_comboAnimationCoroutine);
            }
            
            m_isAnimating = true;
            m_canAcceptComboInput = false;
            
            m_comboAnimationCoroutine = m_player.StartCoroutine(ComboAnimationLockRoutine());
        }

        private IEnumerator ComboAnimationLockRoutine()
        {
            // Aguarda o tempo mínimo de 0.5 segundos para a animação executar
            yield return new WaitForSeconds(m_player.MinComboDuration);
            
            // Libera para próximo input após a animação mínima
            m_isAnimating = false;
            m_canAcceptComboInput = true;
            
            m_comboAnimationCoroutine = null;
            
            Debug.Log($"Combo {m_currentCombo} animation lock released");
        }

        private void StartComboTimeout()
        {
            if (m_comboTimeoutCoroutine != null)
            {
                m_player.StopCoroutine(m_comboTimeoutCoroutine);
            }
            m_comboTimeoutCoroutine = m_player.StartCoroutine(ComboTimeoutRoutine());
        }

        private IEnumerator ComboTimeoutRoutine()
        {
            // Aguarda a janela de combo (0.8 segundos) para timeout
            yield return new WaitForSeconds(m_player.ComboWindow);
            
            // Timeout - volta para locomotion
            Debug.Log("Combo timeout - returning to locomotion");
            ResetCombo();
            m_stateMachine.ChangeState(PlayerState.Locomotion);
        }

        private void ResetCombo()
        {
            m_currentCombo = 0;
            m_isAnimating = false;
            m_canAcceptComboInput = true;
            m_player.OnComboUpdate?.Invoke(0);
        }
    }
}