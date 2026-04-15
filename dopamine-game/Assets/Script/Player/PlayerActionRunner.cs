using System.Collections.Generic;
using DopamineGame.Runtime;
using UnityEngine;

namespace DopamineGame.Gameplay.Player
{
    public sealed class PlayerActionRunner : MonoBehaviour
    {
        [SerializeField] private float pistolRange = 9f;
        [SerializeField] private float swordRange = 1.55f;
        [SerializeField] private float rollSpeed = 11f;
        [SerializeField] private float rollDuration = 0.28f;
        [SerializeField] private float dashSpeed = 16f;
        [SerializeField] private float dashDuration = 0.16f;
        [SerializeField] private float counterWindowDuration = 0.5f;
        [SerializeField] private float switchCounterWindowDuration = 0.2f;
        [SerializeField] private float swordComboChainWindow = 0.3f;

        private PlayerMotor motor;
        private PlayerVisuals visuals;
        private GameServices services;
        private Dictionary<PlayerWeaponType, IWeaponActionSet> weaponActionSets;
        private PlayerWeaponType currentWeapon = PlayerWeaponType.Pistol;
        private PlayerActionKind currentAction;
        private float actionLockRemaining;
        private float counterWindowRemaining;
        private float switchCounterWindowRemaining;
        private float swordComboWindowRemaining;
        private int swordComboStep;

        public PlayerWeaponType CurrentWeapon => currentWeapon;

        public bool CanPhaseThroughEnemies => currentAction == PlayerActionKind.Roll || currentAction == PlayerActionKind.Dash;

        public void Initialize(PlayerMotor targetMotor, PlayerVisuals targetVisuals, GameServices targetServices)
        {
            motor = targetMotor;
            visuals = targetVisuals;
            services = targetServices;
            visuals.SetWeapon(currentWeapon);

            weaponActionSets = new Dictionary<PlayerWeaponType, IWeaponActionSet>
            {
                { PlayerWeaponType.Pistol, new PistolActionSet(this) },
                { PlayerWeaponType.Katana, new KatanaActionSet(this) },
            };
        }

        public void Tick(PlayerInputFrame frame, float deltaTime)
        {
            if (motor == null || visuals == null)
            {
                return;
            }

            UpdateTimers(deltaTime);

            if (frame.MenuPressed)
            {
                services.Menu.TogglePauseMenu();
            }

            if (frame.SwitchWeaponPressed && CanStartLockedAction())
            {
                StartWeaponSwitch();
                return;
            }

            if (frame.CounterPressed && CanStartLockedAction())
            {
                StartCounterWindow();
                return;
            }

            if (frame.DashPressed && CanStartLockedAction())
            {
                weaponActionSets[currentWeapon].TryStartEvade(frame);
                return;
            }

            if (frame.AttackPressed && CanAcceptAttackInput())
            {
                weaponActionSets[currentWeapon].TryStartAttack(frame);
            }
        }

        public bool TryConsumeCounter(ICounterable counterable)
        {
            if (!IsCounterWindowOpen() || counterable == null)
            {
                return false;
            }

            counterWindowRemaining = 0f;
            switchCounterWindowRemaining = 0f;
            StartAction(PlayerActionKind.CounterResolve, 0.2f, lockMovement: true, comboStep: 0);
            counterable.OnCountered(currentWeapon);
            return true;
        }

        private bool IsCounterWindowOpen()
        {
            return counterWindowRemaining > 0f || switchCounterWindowRemaining > 0f;
        }

        private bool CanStartLockedAction()
        {
            return actionLockRemaining <= 0f;
        }

        private bool CanAcceptAttackInput()
        {
            if (currentWeapon == PlayerWeaponType.Pistol)
            {
                return actionLockRemaining <= 0f;
            }

            return actionLockRemaining <= 0f || (IsSwordAttack(currentAction) && swordComboWindowRemaining > 0f && actionLockRemaining <= 0.06f);
        }

        private void StartCounterWindow()
        {
            counterWindowRemaining = counterWindowDuration;
            StartAction(PlayerActionKind.CounterStance, 0.2f, lockMovement: true, comboStep: 0);
        }

        private void StartWeaponSwitch()
        {
            currentWeapon = currentWeapon == PlayerWeaponType.Pistol ? PlayerWeaponType.Katana : PlayerWeaponType.Pistol;
            switchCounterWindowRemaining = switchCounterWindowDuration;
            swordComboStep = 0;
            swordComboWindowRemaining = 0f;
            StartAction(PlayerActionKind.WeaponSwitch, 0.18f, lockMovement: true, comboStep: 0);
            visuals.SetWeapon(currentWeapon);
            services.Audio.PlayCue("player.weapon_switch", transform.position);
        }

        private void StartAction(PlayerActionKind actionKind, float duration, bool lockMovement, int comboStep)
        {
            if (actionKind != PlayerActionKind.CounterStance && actionKind != PlayerActionKind.CounterResolve)
            {
                counterWindowRemaining = 0f;
            }

            if (actionKind != PlayerActionKind.WeaponSwitch)
            {
                switchCounterWindowRemaining = 0f;
            }

            currentAction = actionKind;
            actionLockRemaining = duration;
            motor.SetLocomotionEnabled(!lockMovement);
            visuals.PlayAction(actionKind, currentWeapon, comboStep);
        }

        private void UpdateTimers(float deltaTime)
        {
            if (actionLockRemaining > 0f)
            {
                actionLockRemaining -= deltaTime;

                if (actionLockRemaining <= 0f)
                {
                    currentAction = PlayerActionKind.None;
                    motor.SetLocomotionEnabled(true);
                }
            }

            if (counterWindowRemaining > 0f)
            {
                counterWindowRemaining -= deltaTime;
            }

            if (switchCounterWindowRemaining > 0f)
            {
                switchCounterWindowRemaining -= deltaTime;
            }

            if (swordComboWindowRemaining > 0f)
            {
                swordComboWindowRemaining -= deltaTime;

                if (swordComboWindowRemaining <= 0f)
                {
                    swordComboStep = 0;
                }
            }
        }

        private void FirePistol()
        {
            StartAction(PlayerActionKind.FirePistol, 0.12f, lockMovement: true, comboStep: 0);
            services.Audio.PlayCue("player.pistol_fire", transform.position);

            var origin = motor.WorldCenter + Vector2.up * 0.1f;
            var direction = Vector2.right * motor.FacingSign;
            var hits = Physics2D.RaycastAll(origin, direction, pistolRange);

            for (var index = 0; index < hits.Length; index++)
            {
                var hitCollider = hits[index].collider;

                if (hitCollider == null || hitCollider.attachedRigidbody == motor.Body)
                {
                    continue;
                }

                if (TryGetDamageable(hitCollider, out var damageable))
                {
                    damageable.ApplyDamage(1, direction);
                    break;
                }
            }

            Debug.DrawRay(origin, direction * pistolRange, Color.cyan, 0.25f);
        }

        private void StartRoll(PlayerInputFrame frame)
        {
            var direction = motor.ResolveDirectionalInput(frame.MoveX, 0f);
            StartAction(PlayerActionKind.Roll, rollDuration, lockMovement: true, comboStep: 0);
            motor.StartForcedMovement(direction, rollSpeed, rollDuration, suppressGravity: false);
            services.Audio.PlayCue("player.roll", transform.position);
        }

        private void StartDash(PlayerInputFrame frame)
        {
            var direction = motor.ResolveDirectionalInput(frame.MoveX, frame.MoveY);
            StartAction(PlayerActionKind.Dash, dashDuration, lockMovement: true, comboStep: 0);
            motor.StartForcedMovement(direction, dashSpeed, dashDuration, suppressGravity: true);
            DealSwordDamage(direction, 1);
            services.Audio.PlayCue("player.dash", transform.position);
        }

        private void StartSwordAttack()
        {
            swordComboStep = swordComboWindowRemaining > 0f ? (swordComboStep + 1) % 4 : 0;
            swordComboWindowRemaining = swordComboChainWindow;

            var actionKind = (PlayerActionKind)((int)PlayerActionKind.SwordAttack1 + swordComboStep);
            var actionDuration = 0.16f + swordComboStep * 0.02f;
            StartAction(actionKind, actionDuration, lockMovement: true, comboStep: swordComboStep);
            DealSwordDamage(Vector2.right * motor.FacingSign, 1);
            services.Audio.PlayCue("player.katana_attack", transform.position);
        }

        private void DealSwordDamage(Vector2 direction, int damage)
        {
            var normalizedDirection = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector2.right * motor.FacingSign;
            var center = motor.WorldCenter + normalizedDirection * (swordRange * 0.5f);
            var hits = Physics2D.OverlapCircleAll(center, swordRange * 0.55f);

            for (var index = 0; index < hits.Length; index++)
            {
                var hitCollider = hits[index];

                if (hitCollider == null || hitCollider.attachedRigidbody == motor.Body)
                {
                    continue;
                }

                if (TryGetDamageable(hitCollider, out var damageable))
                {
                    damageable.ApplyDamage(damage, direction);
                }
            }
        }

        private static bool TryGetDamageable(Collider2D hitCollider, out IDamageable damageable)
        {
            var behaviours = hitCollider.GetComponents<MonoBehaviour>();

            for (var index = 0; index < behaviours.Length; index++)
            {
                if (behaviours[index] is IDamageable typedDamageable)
                {
                    damageable = typedDamageable;
                    return true;
                }
            }

            damageable = null;
            return false;
        }

        private static bool IsSwordAttack(PlayerActionKind actionKind)
        {
            return actionKind == PlayerActionKind.SwordAttack1
                || actionKind == PlayerActionKind.SwordAttack2
                || actionKind == PlayerActionKind.SwordAttack3
                || actionKind == PlayerActionKind.SwordAttack4;
        }

        private interface IWeaponActionSet
        {
            void TryStartAttack(PlayerInputFrame frame);

            void TryStartEvade(PlayerInputFrame frame);
        }

        private sealed class PistolActionSet : IWeaponActionSet
        {
            private readonly PlayerActionRunner runner;

            public PistolActionSet(PlayerActionRunner runner)
            {
                this.runner = runner;
            }

            public void TryStartAttack(PlayerInputFrame frame)
            {
                runner.FirePistol();
            }

            public void TryStartEvade(PlayerInputFrame frame)
            {
                runner.StartRoll(frame);
            }
        }

        private sealed class KatanaActionSet : IWeaponActionSet
        {
            private readonly PlayerActionRunner runner;

            public KatanaActionSet(PlayerActionRunner runner)
            {
                this.runner = runner;
            }

            public void TryStartAttack(PlayerInputFrame frame)
            {
                runner.StartSwordAttack();
            }

            public void TryStartEvade(PlayerInputFrame frame)
            {
                runner.StartDash(frame);
            }
        }
    }
}
