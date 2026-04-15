using DopamineGame.Runtime;
using UnityEngine;

namespace DopamineGame.Gameplay.Player
{
    public sealed class PlayerVisuals : MonoBehaviour
    {
        private const float BaseWidth = 0.8f;
        private const float BaseHeight = 1.35f;

        private static readonly Color PistolColor = new Color(0.27f, 0.92f, 1f, 1f);
        private static readonly Color KatanaColor = new Color(1f, 0.24f, 0.83f, 1f);
        private static readonly Color CounterColor = new Color(1f, 0.94f, 0.31f, 1f);

        private PlayerMotor motor;
        private SpriteRenderer spriteRenderer;
        private Transform visualRoot;
        private VisualClip activeClip;
        private int activeFrameIndex;
        private float activeFrameRemaining;
        private PlayerWeaponType currentWeapon = PlayerWeaponType.Pistol;

        public void Initialize(PlayerMotor targetMotor)
        {
            motor = targetMotor;
            EnsureVisualRoot();
            ApplyPose(new VisualPose(GetWeaponColor(), Vector2.zero, Vector2.one, 0f));
        }

        public void SetWeapon(PlayerWeaponType weaponType)
        {
            currentWeapon = weaponType;
        }

        public void PlayAction(PlayerActionKind actionKind, PlayerWeaponType weaponType, int comboStep)
        {
            currentWeapon = weaponType;
            activeClip = BuildClip(actionKind, weaponType, comboStep);
            activeFrameIndex = 0;
            activeFrameRemaining = activeClip.Frames[0].Duration;
            ApplyPose(activeClip.Frames[0].Pose);
        }

        public void Tick(float deltaTime)
        {
            if (spriteRenderer == null || visualRoot == null || motor == null)
            {
                return;
            }

            if (activeClip != null)
            {
                activeFrameRemaining -= deltaTime;

                while (activeFrameRemaining <= 0f && activeClip != null)
                {
                    activeFrameIndex++;

                    if (activeFrameIndex >= activeClip.Frames.Length)
                    {
                        activeClip = null;
                        break;
                    }

                    activeFrameRemaining += activeClip.Frames[activeFrameIndex].Duration;
                    ApplyPose(activeClip.Frames[activeFrameIndex].Pose);
                }
            }

            if (activeClip == null)
            {
                ApplyLocomotionPose(Time.time);
            }

            visualRoot.localScale = new Vector3(Mathf.Abs(visualRoot.localScale.x) * motor.FacingSign, visualRoot.localScale.y, 1f);
        }

        private void EnsureVisualRoot()
        {
            var existingRoot = transform.Find("Visual");
            visualRoot = existingRoot != null ? existingRoot : new GameObject("Visual").transform;
            visualRoot.SetParent(transform, false);
            visualRoot.localPosition = new Vector3(0f, 0.08f, 0f);

            spriteRenderer = visualRoot.GetComponent<SpriteRenderer>();

            if (spriteRenderer == null)
            {
                spriteRenderer = visualRoot.gameObject.AddComponent<SpriteRenderer>();
            }

            spriteRenderer.sprite = PlaceholderSpriteFactory.CreateSquareSprite();
            spriteRenderer.sortingOrder = 10;
        }

        private void ApplyLocomotionPose(float timeValue)
        {
            var baseColor = GetWeaponColor();

            if (!motor.IsGrounded)
            {
                var airborneScale = new Vector2(0.92f, 1.08f);
                var airborneOffset = new Vector2(0f, Mathf.Sign(motor.Velocity.y) * 0.04f);
                ApplyPose(new VisualPose(baseColor, airborneOffset, airborneScale, -6f * motor.FacingSign));
                return;
            }

            if (Mathf.Abs(motor.Velocity.x) > 0.1f)
            {
                var bob = Mathf.Sin(timeValue * 18f) * 0.04f;
                var squash = 1f + Mathf.Abs(Mathf.Sin(timeValue * 18f)) * 0.08f;
                ApplyPose(new VisualPose(baseColor, new Vector2(0f, bob), new Vector2(1.08f, 1f / squash), 0f));
                return;
            }

            var idleBob = Mathf.Sin(timeValue * 4f) * 0.02f;
            ApplyPose(new VisualPose(baseColor, new Vector2(0f, idleBob), Vector2.one, 0f));
        }

        private void ApplyPose(VisualPose pose)
        {
            spriteRenderer.color = pose.Color;
            visualRoot.localPosition = new Vector3(pose.Offset.x, 0.08f + pose.Offset.y, 0f);
            visualRoot.localRotation = Quaternion.Euler(0f, 0f, pose.RotationDegrees);
            visualRoot.localScale = new Vector3(BaseWidth * pose.Scale.x, BaseHeight * pose.Scale.y, 1f);
        }

        private Color GetWeaponColor()
        {
            return currentWeapon == PlayerWeaponType.Pistol ? PistolColor : KatanaColor;
        }

        private static VisualClip BuildClip(PlayerActionKind actionKind, PlayerWeaponType weaponType, int comboStep)
        {
            switch (actionKind)
            {
                case PlayerActionKind.FirePistol:
                    return new VisualClip(
                        new VisualFrame(0.05f, new VisualPose(PistolColor, new Vector2(0.15f, 0f), new Vector2(1.35f, 0.78f), -5f)),
                        new VisualFrame(0.08f, new VisualPose(PistolColor, new Vector2(-0.04f, 0f), new Vector2(0.92f, 1.08f), 4f)));

                case PlayerActionKind.Roll:
                    return new VisualClip(
                        new VisualFrame(0.09f, new VisualPose(PistolColor, Vector2.zero, new Vector2(1.35f, 0.68f), -20f)),
                        new VisualFrame(0.1f, new VisualPose(PistolColor, Vector2.zero, new Vector2(1.48f, 0.64f), 18f)),
                        new VisualFrame(0.09f, new VisualPose(PistolColor, Vector2.zero, new Vector2(1.28f, 0.7f), -16f)));

                case PlayerActionKind.Dash:
                    return new VisualClip(
                        new VisualFrame(0.06f, new VisualPose(KatanaColor, new Vector2(0.12f, 0f), new Vector2(1.58f, 0.72f), 0f)),
                        new VisualFrame(0.06f, new VisualPose(KatanaColor, new Vector2(0.2f, 0f), new Vector2(1.8f, 0.6f), 0f)),
                        new VisualFrame(0.05f, new VisualPose(KatanaColor, new Vector2(0.08f, 0f), new Vector2(1.25f, 0.84f), 0f)));

                case PlayerActionKind.CounterStance:
                    return new VisualClip(
                        new VisualFrame(0.16f, new VisualPose(CounterColor, Vector2.zero, new Vector2(0.88f, 1.2f), 0f)),
                        new VisualFrame(0.18f, new VisualPose(CounterColor, new Vector2(0f, 0.02f), new Vector2(0.92f, 1.14f), 0f)));

                case PlayerActionKind.CounterResolve:
                    return new VisualClip(
                        new VisualFrame(0.08f, new VisualPose(CounterColor, new Vector2(0.18f, 0f), new Vector2(1.5f, 0.72f), -18f)),
                        new VisualFrame(0.12f, new VisualPose(CounterColor, new Vector2(0.1f, 0f), new Vector2(1.2f, 0.84f), 8f)));

                case PlayerActionKind.WeaponSwitch:
                    return new VisualClip(
                        new VisualFrame(0.08f, new VisualPose(weaponType == PlayerWeaponType.Pistol ? PistolColor : KatanaColor, new Vector2(0f, 0.08f), new Vector2(0.7f, 1.25f), 0f)),
                        new VisualFrame(0.1f, new VisualPose(weaponType == PlayerWeaponType.Pistol ? PistolColor : KatanaColor, Vector2.zero, new Vector2(1.05f, 0.95f), 0f)));

                case PlayerActionKind.SwordAttack1:
                case PlayerActionKind.SwordAttack2:
                case PlayerActionKind.SwordAttack3:
                case PlayerActionKind.SwordAttack4:
                    return BuildSwordClip(comboStep);

                default:
                    return new VisualClip(new VisualFrame(0.1f, new VisualPose(weaponType == PlayerWeaponType.Pistol ? PistolColor : KatanaColor, Vector2.zero, Vector2.one, 0f)));
            }
        }

        private static VisualClip BuildSwordClip(int comboStep)
        {
            var swingColor = Color.Lerp(KatanaColor, CounterColor, comboStep * 0.12f);
            var forwardOffset = 0.12f + comboStep * 0.03f;

            return new VisualClip(
                new VisualFrame(0.06f, new VisualPose(swingColor, new Vector2(0.04f, 0.02f), new Vector2(0.82f, 1.2f), -12f)),
                new VisualFrame(0.08f, new VisualPose(swingColor, new Vector2(forwardOffset, 0f), new Vector2(1.6f, 0.72f), comboStep % 2 == 0 ? 18f : -18f)),
                new VisualFrame(0.07f, new VisualPose(swingColor, new Vector2(0.08f, 0f), new Vector2(1.18f, 0.9f), 8f)));
        }

        private readonly struct VisualPose
        {
            public VisualPose(Color color, Vector2 offset, Vector2 scale, float rotationDegrees)
            {
                Color = color;
                Offset = offset;
                Scale = scale;
                RotationDegrees = rotationDegrees;
            }

            public Color Color { get; }

            public Vector2 Offset { get; }

            public Vector2 Scale { get; }

            public float RotationDegrees { get; }
        }

        private readonly struct VisualFrame
        {
            public VisualFrame(float duration, VisualPose pose)
            {
                Duration = duration;
                Pose = pose;
            }

            public float Duration { get; }

            public VisualPose Pose { get; }
        }

        private sealed class VisualClip
        {
            public VisualClip(params VisualFrame[] frames)
            {
                Frames = frames;
            }

            public VisualFrame[] Frames { get; }
        }
    }
}
