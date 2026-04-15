using UnityEngine;

namespace DopamineGame.Gameplay.Player
{
    public readonly struct PlayerInputFrame
    {
        public PlayerInputFrame(float moveX, float moveY, bool jumpPressed, bool attackPressed, bool dashPressed, bool counterPressed, bool switchWeaponPressed, bool interactPressed, bool menuPressed)
        {
            MoveX = moveX;
            MoveY = moveY;
            JumpPressed = jumpPressed;
            AttackPressed = attackPressed;
            DashPressed = dashPressed;
            CounterPressed = counterPressed;
            SwitchWeaponPressed = switchWeaponPressed;
            InteractPressed = interactPressed;
            MenuPressed = menuPressed;
        }

        public float MoveX { get; }

        public float MoveY { get; }

        public bool JumpPressed { get; }

        public bool AttackPressed { get; }

        public bool DashPressed { get; }

        public bool CounterPressed { get; }

        public bool SwitchWeaponPressed { get; }

        public bool InteractPressed { get; }

        public bool MenuPressed { get; }
    }

    public sealed class PlayerInputReader : MonoBehaviour
    {
        public PlayerInputFrame ReadFrame()
        {
            var leftHeld = Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow);
            var rightHeld = Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow);
            var upHeld = Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow);
            var downHeld = Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow);

            var moveX = 0f;
            moveX += leftHeld ? -1f : 0f;
            moveX += rightHeld ? 1f : 0f;

            var moveY = 0f;
            moveY += downHeld ? -1f : 0f;
            moveY += upHeld ? 1f : 0f;

            return new PlayerInputFrame(
                Mathf.Clamp(moveX, -1f, 1f),
                Mathf.Clamp(moveY, -1f, 1f),
                Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.K) || Input.GetKeyDown(KeyCode.UpArrow),
                Input.GetKeyDown(KeyCode.J),
                Input.GetKeyDown(KeyCode.L) || Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.RightShift),
                Input.GetKeyDown(KeyCode.F),
                Input.GetKeyDown(KeyCode.R),
                Input.GetKeyDown(KeyCode.E),
                Input.GetKeyDown(KeyCode.Escape));
        }
    }
}
