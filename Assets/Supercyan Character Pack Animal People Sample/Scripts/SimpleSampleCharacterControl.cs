using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Supercyan.AnimalPeopleSample
{
    public class SimpleSampleCharacterControl : MonoBehaviour
    {
        private enum ControlMode
        {
            Tank,
            Direct
        }

        [SerializeField] private float m_moveSpeed = 2;
        [SerializeField] private float m_turnSpeed = 200;
        [SerializeField] private float m_jumpForce = 4;

        [SerializeField] private Animator m_animator = null;
        [SerializeField] private Rigidbody m_rigidBody = null;

        [SerializeField] private ControlMode m_controlMode = ControlMode.Direct;

        // Variáveis para o efeito spin
        [SerializeField] private float m_spinSpeed = 720f;
        [SerializeField] private float m_spinDuration = 1f;
        [SerializeField] private float m_attackCooldown = 5f;
        private bool m_isSpinning = false;
        private bool m_canAttack = true;
        private Coroutine m_spinCoroutine;
        private Coroutine m_cooldownCoroutine;

        // Variáveis para o sistema de combo SIMPLIFICADO
        [SerializeField] private float m_comboWindow = 0.8f; // Janela de tempo entre ataques
        [SerializeField] private float m_minComboDuration = 0.5f; // Tempo mínimo para cada animação executar
        private int m_currentCombo = 0;
        private float m_lastComboTime = 0f;
        private bool m_canAcceptComboInput = true;
        private bool m_isComboAnimating = false;
        private Coroutine m_comboAnimationCoroutine;

        private float m_currentV = 0;
        private float m_currentH = 0;

        private readonly float m_interpolation = 10;
        private readonly float m_walkScale = 0.33f;
        private readonly float m_backwardsWalkScale = 0.16f;
        private readonly float m_backwardRunScale = 0.66f;

        private bool m_wasGrounded;
        private Vector3 m_currentDirection = Vector3.zero;

        private float m_jumpTimeStamp = 0;
        private float m_minJumpInterval = 0.25f;
        private bool m_jumpInput = false;

        private bool m_isGrounded;

        private List<Collider> m_collisions = new List<Collider>();

        // Nomes dos triggers de combo
        private readonly string m_attackTrigger = "attack1";
        private readonly string m_combo1Trigger = "atk_combo1";
        private readonly string m_combo2Trigger = "atk_combo2";
        private readonly string m_combo3Trigger = "atk_combo3";

        // Eventos para UI
        public System.Action<float> OnCooldownUpdate;
        public System.Action<int> OnComboUpdate;

        private void Awake()
        {
            if (!m_animator) { gameObject.GetComponent<Animator>(); }
            if (!m_rigidBody) { gameObject.GetComponent<Animator>(); }
        }

        private void OnCollisionEnter(Collision collision)
        {
            ContactPoint[] contactPoints = collision.contacts;
            for (int i = 0; i < contactPoints.Length; i++)
            {
                if (Vector3.Dot(contactPoints[i].normal, Vector3.up) > 0.5f)
                {
                    if (!m_collisions.Contains(collision.collider))
                    {
                        m_collisions.Add(collision.collider);
                    }
                    m_isGrounded = true;
                }
            }
        }

        private void OnCollisionStay(Collision collision)
        {
            ContactPoint[] contactPoints = collision.contacts;
            bool validSurfaceNormal = false;
            for (int i = 0; i < contactPoints.Length; i++)
            {
                if (Vector3.Dot(contactPoints[i].normal, Vector3.up) > 0.5f)
                {
                    validSurfaceNormal = true; break;
                }
            }

            if (validSurfaceNormal)
            {
                m_isGrounded = true;
                if (!m_collisions.Contains(collision.collider))
                {
                    m_collisions.Add(collision.collider);
                }
            }
            else
            {
                if (m_collisions.Contains(collision.collider))
                {
                    m_collisions.Remove(collision.collider);
                }
                if (m_collisions.Count == 0) { m_isGrounded = false; }
            }
        }

        private void OnCollisionExit(Collision collision)
        {
            if (m_collisions.Contains(collision.collider))
            {
                m_collisions.Remove(collision.collider);
            }
            if (m_collisions.Count == 0) { m_isGrounded = false; }
        }

        private void Update()
        {
            if (!m_jumpInput && Input.GetKey(KeyCode.Space))
            {
                m_jumpInput = true;
            }

            // Verifica se o botão direito do mouse foi pressionado
            if (Input.GetMouseButtonDown(1) && m_canAttack && !m_isSpinning && !m_isComboAnimating)
            {
                m_animator.SetTrigger(m_attackTrigger);
                StartSpinWithAttack();
                StartCooldown();
            }

            // SISTEMA DE COMBO SIMPLIFICADO - clique esquerdo
            if (Input.GetMouseButtonDown(0) && m_canAcceptComboInput && !m_isSpinning && !m_isComboAnimating)
            {
                ProcessComboInput();
            }

            // Verifica se a janela de combo expirou (só se não estiver animando)
            if (m_currentCombo > 0 && Time.time - m_lastComboTime > m_comboWindow && !m_isComboAnimating)
            {
                ResetCombo();
            }
        }

        private void ProcessComboInput()
        {
            m_lastComboTime = Time.time;

            // Se passou muito tempo desde o último combo, reinicia
            if (Time.time - m_lastComboTime > m_comboWindow * 2f)
            {
                m_currentCombo = 0;
            }

            m_currentCombo++;

            // Limita o combo a 3 ataques
            if (m_currentCombo > 3)
            {
                m_currentCombo = 1; // Reinicia o ciclo após o 3º ataque
            }

            // Executa o ataque correspondente
            switch (m_currentCombo)
            {
                case 1:
                    m_animator.SetTrigger(m_combo1Trigger);
                    break;
                case 2:
                    m_animator.SetTrigger(m_combo2Trigger);
                    break;
                case 3:
                    m_animator.SetTrigger(m_combo3Trigger);
                    break;
            }

            // Inicia o bloqueio de input durante a animação
            StartComboAnimationLock();

            // Dispara evento de combo atualizado
            OnComboUpdate?.Invoke(m_currentCombo);

            // Impede input muito rápido (proteção contra spam)
            StartCoroutine(ComboInputCooldown());
        }

        // Bloqueia novos inputs durante a execução da animação
        private void StartComboAnimationLock()
        {
            m_isComboAnimating = true;
            
            if (m_comboAnimationCoroutine != null)
                StopCoroutine(m_comboAnimationCoroutine);
                
            m_comboAnimationCoroutine = StartCoroutine(ComboAnimationLockRoutine());
        }

        private IEnumerator ComboAnimationLockRoutine()
        {
            // Aguarda o tempo mínimo para a animação executar
            yield return new WaitForSeconds(m_minComboDuration);
            
            // Libera para próximo input
            m_isComboAnimating = false;
            m_comboAnimationCoroutine = null;
        }

        // Pequeno cooldown para evitar input muito rápido
        private IEnumerator ComboInputCooldown()
        {
            m_canAcceptComboInput = false;
            yield return new WaitForSeconds(0.1f); // 100ms de cooldown
            m_canAcceptComboInput = true;
        }

        // Reset do combo
        private void ResetCombo()
        {
            m_currentCombo = 0;
            m_isComboAnimating = false;
            OnComboUpdate?.Invoke(0);
        }

        private void FixedUpdate()
        {
            m_animator.SetBool("Grounded", m_isGrounded);

            if (!m_isSpinning && !m_isComboAnimating)
            {
                switch (m_controlMode)
                {
                    case ControlMode.Direct:
                        DirectUpdate();
                        break;

                    case ControlMode.Tank:
                        TankUpdate();
                        break;

                    default:
                        Debug.LogError("Unsupported state");
                        break;
                }
            }
            else if (m_isSpinning)
            {
                transform.Rotate(0, m_spinSpeed * Time.deltaTime, 0);
            }

            m_wasGrounded = m_isGrounded;
            m_jumpInput = false;
        }

        private void TankUpdate()
        {
            float v = Input.GetAxis("Vertical");
            float h = Input.GetAxis("Horizontal");

            bool walk = Input.GetKey(KeyCode.LeftShift);

            if (v < 0)
            {
                if (walk) { v *= m_backwardsWalkScale; }
                else { v *= m_backwardRunScale; }
            }
            else if (walk)
            {
                v *= m_walkScale;
            }

            m_currentV = Mathf.Lerp(m_currentV, v, Time.deltaTime * m_interpolation);
            m_currentH = Mathf.Lerp(m_currentH, h, Time.deltaTime * m_interpolation);

            transform.position += transform.forward * m_currentV * m_moveSpeed * Time.deltaTime;
            transform.Rotate(0, m_currentH * m_turnSpeed * Time.deltaTime, 0);

            m_animator.SetFloat("MoveSpeed", m_currentV);

            JumpingAndLanding();
        }

        private void DirectUpdate()
        {
            float v = Input.GetAxis("Vertical");
            float h = Input.GetAxis("Horizontal");

            Transform camera = Camera.main.transform;

            if (Input.GetKey(KeyCode.LeftShift))
            {
                v *= m_walkScale;
                h *= m_walkScale;
            }

            m_currentV = Mathf.Lerp(m_currentV, v, Time.deltaTime * m_interpolation);
            m_currentH = Mathf.Lerp(m_currentH, h, Time.deltaTime * m_interpolation);

            Vector3 direction = camera.forward * m_currentV + camera.right * m_currentH;

            float directionLength = direction.magnitude;
            direction.y = 0;
            direction = direction.normalized * directionLength;

            if (direction != Vector3.zero)
            {
                m_currentDirection = Vector3.Slerp(m_currentDirection, direction, Time.deltaTime * m_interpolation);

                transform.rotation = Quaternion.LookRotation(m_currentDirection);
                transform.position += m_currentDirection * m_moveSpeed * Time.deltaTime;

                m_animator.SetFloat("MoveSpeed", direction.magnitude);
            }

            JumpingAndLanding();
        }

        private void JumpingAndLanding()
        {
            bool jumpCooldownOver = (Time.time - m_jumpTimeStamp) >= m_minJumpInterval;

            if (jumpCooldownOver && m_isGrounded && m_jumpInput)
            {
                m_jumpTimeStamp = Time.time;
                m_rigidBody.AddForce(Vector3.up * m_jumpForce, ForceMode.Impulse);
            }
        }

        // Método para iniciar o spin
        private void StartSpinWithAttack()
        {
            if (m_spinCoroutine != null) StopCoroutine(m_spinCoroutine);
            m_spinCoroutine = StartCoroutine(SpinRoutine());
        }

        private IEnumerator SpinRoutine()
        {
            m_isSpinning = true;
            m_animator.SetFloat("MoveSpeed", 0f);
            yield return new WaitForSeconds(m_spinDuration);
            m_isSpinning = false;
            m_spinCoroutine = null;
        }

        // Método para iniciar o cooldown do ataque especial
        private void StartCooldown()
        {
            m_canAttack = false;
            if (m_cooldownCoroutine != null) StopCoroutine(m_cooldownCoroutine);
            m_cooldownCoroutine = StartCoroutine(CooldownRoutine());
        }

        private IEnumerator CooldownRoutine()
        {
            float cooldownTimer = m_attackCooldown;
            while (cooldownTimer > 0)
            {
                OnCooldownUpdate?.Invoke(cooldownTimer / m_attackCooldown);
                cooldownTimer -= Time.deltaTime;
                yield return null;
            }
            m_canAttack = true;
            OnCooldownUpdate?.Invoke(0f);
            m_cooldownCoroutine = null;
        }

        // Métodos públicos
        public bool IsSpinning() { return m_isSpinning; }
        public bool CanAttack() { return m_canAttack; }
        public int GetCurrentCombo() { return m_currentCombo; }
        public bool IsComboAnimating() { return m_isComboAnimating; }

        public void SetSpinSpeed(float speed) { m_spinSpeed = speed; }
        public void SetSpinDuration(float duration) { m_spinDuration = duration; }
        public void SetAttackCooldown(float cooldown) { m_attackCooldown = cooldown; }
        public void SetComboWindow(float window) { m_comboWindow = window; }
        public void SetMinComboDuration(float duration) { m_minComboDuration = duration; }
    }
}