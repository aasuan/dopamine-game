using UnityEngine;

namespace DopamineGame.Gameplay.Player
{
    public enum PlayerWeaponType
    {
        Pistol = 0,
        Katana = 1,
    }

    public enum PlayerActionKind
    {
        None = 0,
        FirePistol = 1,
        SwordAttack1 = 2,
        SwordAttack2 = 3,
        SwordAttack3 = 4,
        SwordAttack4 = 5,
        Roll = 6,
        Dash = 7,
        CounterStance = 8,
        CounterResolve = 9,
        WeaponSwitch = 10,
    }

    public interface IDamageable
    {
        void ApplyDamage(int amount, Vector2 hitDirection);
    }

    public interface ICounterable
    {
        void OnCountered(PlayerWeaponType byWeapon);
    }
}
