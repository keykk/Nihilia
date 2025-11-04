using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Supercyan.AnimalPeopleSample
{
    public class PlayerController : MonoBehaviour
    {
        [Header("Movement Settings")]
        [SerializeField] private float m_moveSpeed = 2;
        [SerializeField] private float m_turnSpeed = 200;
        [SerializeField] private float m_jumpForce = 4;

        [Header("Components")]
        [SerializeField] private Animator m_animator = null;
        [SerializeField] private Rigidbody m_rigidBody = null;

        [Header("Attack Settings")]
        [SerializeField] private float m_spinSpeed = 720f;
        [SerializeField] private float m_spinDuration = 1f;
        [SerializeField] private float m_attackCooldown = 5f;
        [SerializeField] private float m_comboWindow = 0.8f;
        [SerializeField] private float m_minComboDuration = 0.5f;
        [SerializeField] private float atk1KnockbackImpulse = 10f;

        private PlayerStateMachine m_stateMachine;

        // Movement variables
        private float m_currentV = 0;
        private float m_currentH = 0;
        private readonly float m_interpolation = 10;
        private readonly float m_walkScale = 0.33f;
        private readonly float m_backwardsWalkScale = 0.16f;
        private readonly float m_backwardRunScale = 0.66f;

        // Ground detection
        private bool m_isGrounded;
        private bool m_wasGrounded;
        private List<Collider> m_collisions = new List<Collider>();

        // Jump variables
        private float m_jumpTimeStamp = 0;
        private float m_minJumpInterval = 0.25f;
        private bool m_jumpInput = false;

        // Events
        public System.Action<float> OnCooldownUpdate;
        public System.Action<int> OnComboUpdate;

        // Properties
        public Animator Animator => m_animator;
        public Rigidbody Rigidbody => m_rigidBody;
        public bool IsGrounded => m_isGrounded;
        public bool WasGrounded => m_wasGrounded;
        public bool JumpInput => m_jumpInput;
        public float MoveSpeed => m_moveSpeed;
        public float TurnSpeed => m_turnSpeed;
        public float JumpForce => m_jumpForce;
        public float CurrentV => m_currentV;
        public float CurrentH => m_currentH;
        public float Interpolation => m_interpolation;
        public float WalkScale => m_walkScale;
        public float BackwardsWalkScale => m_backwardsWalkScale;
        public float BackwardRunScale => m_backwardRunScale;
        public float SpinSpeed => m_spinSpeed;
        public float SpinDuration => m_spinDuration;
        public float AttackCooldown => m_attackCooldown;
        public float ComboWindow => m_comboWindow;
        public float MinComboDuration => m_minComboDuration;

        //
        public GameObject Atk1HitBox;

        private void Start()
        {
            Atk1HitBox.SetActive(false);
        }

        private void Awake()
        {
            if (!m_animator) m_animator = GetComponent<Animator>();
            if (!m_rigidBody) m_rigidBody = GetComponent<Rigidbody>();
            
            m_stateMachine = new PlayerStateMachine(this);
        }

        private void Update()
        {
            HandleInput();
            m_stateMachine.Update();
        }

        private void FixedUpdate()
        {
            m_animator.SetBool("Grounded", m_isGrounded);
            m_stateMachine.FixedUpdate();
            UpdateGroundDetection();
        }

        private void HandleInput()
        {
            // Jump input
            if (!m_jumpInput && Input.GetKey(KeyCode.Space))
            {
                m_jumpInput = true;
            }

            // Delegate other inputs to state machine
            m_stateMachine.HandleInput();
        }

        private void UpdateGroundDetection()
        {
            m_wasGrounded = m_isGrounded;
            m_jumpInput = false;
        }

        // Movement methods
        public void SetMovementValues(float v, float h)
        {
            m_currentV = v;
            m_currentH = h;
        }

        public void UpdateAnimatorMoveSpeed(float speed)
        {
            m_animator.SetFloat("MoveSpeed", speed);
        }

        public void TriggerJump()
        {
            bool jumpCooldownOver = (Time.time - m_jumpTimeStamp) >= m_minJumpInterval;
            
            if (jumpCooldownOver && m_isGrounded)
            {
                m_jumpTimeStamp = Time.time;
                m_rigidBody.AddForce(Vector3.up * m_jumpForce, ForceMode.Impulse);
            }
        }

        // Collision detection
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

        public void OnAtk1ColliderEnter(Collider other)
        {
            //Debug.Log("Atk1 Hitbox Entered");
            var otherObject = other.gameObject;
            var otherRigidbody = otherObject.GetComponent<Rigidbody>();
            var isTarget = otherObject.layer == LayerMask.NameToLayer("Target");
            if (isTarget && otherRigidbody != null)
            {
                var positionDiff = otherObject.transform.position - gameObject.transform.position;
                var impuseVector = new Vector3(positionDiff.normalized.x, 0, positionDiff.normalized.z);
                impuseVector *= atk1KnockbackImpulse;
                otherRigidbody.AddForce(impuseVector, ForceMode.Impulse);
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

        // Public methods
        public PlayerStateMachine GetStateMachine() => m_stateMachine;
        public new Coroutine StartCoroutine(IEnumerator routine) => base.StartCoroutine(routine);
        public new void StopCoroutine(Coroutine routine) => base.StopCoroutine(routine);
    }
}