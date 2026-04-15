using DopamineGame.Runtime;
using UnityEngine;

namespace DopamineGame.Gameplay.Player
{
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(CapsuleCollider2D))]
    [RequireComponent(typeof(PlayerInputReader))]
    [RequireComponent(typeof(PlayerMotor))]
    [RequireComponent(typeof(PlayerVisuals))]
    [RequireComponent(typeof(PlayerActionRunner))]
    public sealed class PlayerRoot : MonoBehaviour
    {
        private GameServices services;
        private PlayerInputReader inputReader;
        private PlayerMotor motor;
        private PlayerVisuals visuals;
        private PlayerActionRunner actionRunner;
        private Rigidbody2D body;
        private CapsuleCollider2D capsuleCollider;
        private float bufferedMoveX;
        private bool bufferedJumpPressed;
        private bool initialized;

        public void Initialize(GameServices runtimeServices)
        {
            services = runtimeServices;
            CacheComponents();
            WireComponents();
            initialized = true;
        }

        private void Awake()
        {
            CacheComponents();
        }

        private void Start()
        {
            if (!initialized)
            {
                Initialize(new GameServices(new NullAudioService(), new NullMenuService()));
            }
        }

        private void Update()
        {
            if (!initialized)
            {
                return;
            }

            var currentInput = inputReader.ReadFrame();
            bufferedMoveX = currentInput.MoveX;
            bufferedJumpPressed |= currentInput.JumpPressed;
            actionRunner.Tick(currentInput, Time.deltaTime);
            visuals.Tick(Time.deltaTime);
        }

        private void FixedUpdate()
        {
            if (!initialized)
            {
                return;
            }

            motor.Tick(Time.fixedDeltaTime, bufferedMoveX, bufferedJumpPressed);
            bufferedJumpPressed = false;
        }

        private void CacheComponents()
        {
            body = GetComponent<Rigidbody2D>();
            capsuleCollider = GetComponent<CapsuleCollider2D>();
            inputReader = GetComponent<PlayerInputReader>();
            motor = GetComponent<PlayerMotor>();
            visuals = GetComponent<PlayerVisuals>();
            actionRunner = GetComponent<PlayerActionRunner>();
        }

        private void WireComponents()
        {
            motor.Initialize(body, capsuleCollider);
            visuals.Initialize(motor);
            actionRunner.Initialize(motor, visuals, services);
        }
    }
}
