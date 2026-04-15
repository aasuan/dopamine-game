using UnityEngine;

namespace DopamineGame.Gameplay.Player
{
    public sealed class PlayerMotor : MonoBehaviour
    {
        [SerializeField] private float maxMoveSpeed = 7.5f;
        [SerializeField] private float jumpImpulse = 13f;
        [SerializeField] private float groundedProbeDistance = 0.08f;

        private Rigidbody2D body;
        private CapsuleCollider2D capsuleCollider;
        private float baseGravityScale;
        private bool locomotionEnabled = true;
        private bool isGrounded;
        private readonly RaycastHit2D[] groundedHits = new RaycastHit2D[4];
        private int facingSign = 1;
        private float forcedMovementRemaining;
        private Vector2 forcedVelocity;
        private bool forcedSuppressGravity;

        public bool IsGrounded => isGrounded;

        public int FacingSign => facingSign;

        public bool IsForcedMovementActive => forcedMovementRemaining > 0f;

        public Vector2 Velocity => body != null ? body.velocity : Vector2.zero;

        public Vector2 WorldCenter => body != null ? body.worldCenterOfMass : (Vector2)transform.position;

        public Rigidbody2D Body => body;

        public void Initialize(Rigidbody2D targetBody, CapsuleCollider2D targetCollider)
        {
            body = targetBody;
            capsuleCollider = targetCollider;
            baseGravityScale = body.gravityScale;
            UpdateGroundedState();
        }

        public void SetLocomotionEnabled(bool enabled)
        {
            locomotionEnabled = enabled;
        }

        public void StartForcedMovement(Vector2 direction, float speed, float duration, bool suppressGravity)
        {
            var resolvedDirection = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector2.right * facingSign;

            forcedVelocity = resolvedDirection * speed;
            forcedMovementRemaining = duration;
            forcedSuppressGravity = suppressGravity;

            if (Mathf.Abs(resolvedDirection.x) > 0.01f)
            {
                facingSign = resolvedDirection.x > 0f ? 1 : -1;
            }
        }

        public void Tick(float deltaTime, float moveX, bool jumpPressed)
        {
            if (body == null)
            {
                return;
            }

            UpdateGroundedState();

            if (Mathf.Abs(moveX) > 0.01f)
            {
                facingSign = moveX > 0f ? 1 : -1;
            }

            if (forcedMovementRemaining > 0f)
            {
                forcedMovementRemaining -= deltaTime;
                body.gravityScale = forcedSuppressGravity ? 0f : baseGravityScale;
                body.velocity = forcedVelocity;
                return;
            }

            body.gravityScale = baseGravityScale;

            var targetVelocityX = locomotionEnabled ? moveX * maxMoveSpeed : 0f;
            body.velocity = new Vector2(targetVelocityX, body.velocity.y);

            if (locomotionEnabled && jumpPressed && isGrounded)
            {
                body.velocity = new Vector2(body.velocity.x, jumpImpulse);
                isGrounded = false;
            }
        }

        public Vector2 ResolveDirectionalInput(float moveX, float moveY)
        {
            var inputDirection = new Vector2(moveX, moveY);

            if (inputDirection.sqrMagnitude <= 0.0001f)
            {
                return Vector2.right * facingSign;
            }

            return inputDirection.normalized;
        }

        private void UpdateGroundedState()
        {
            if (capsuleCollider == null)
            {
                isGrounded = false;
                return;
            }

            var contactFilter = new ContactFilter2D();
            contactFilter.useTriggers = false;
            contactFilter.SetLayerMask(Physics2D.AllLayers);

            var hitCount = capsuleCollider.Cast(Vector2.down, contactFilter, groundedHits, groundedProbeDistance);
            isGrounded = hitCount > 0;
        }
    }
}
