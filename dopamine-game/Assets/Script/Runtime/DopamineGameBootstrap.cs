using DopamineGame.Gameplay.Player;
using UnityEngine.SceneManagement;
using UnityEngine;

namespace DopamineGame.Runtime
{
    [DefaultExecutionOrder(-1000)]
    public sealed class DopamineGameBootstrap : MonoBehaviour
    {
        private static DopamineGameBootstrap instance;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureRuntime()
        {
            if (FindObjectOfType<DopamineGameBootstrap>() != null)
            {
                return;
            }

            var runtimeObject = new GameObject("DopamineGameRuntime");
            runtimeObject.AddComponent<DopamineGameBootstrap>();
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void Start()
        {
            EnsureSceneRuntime();
        }

        private void OnDestroy()
        {
            if (instance == this)
            {
                SceneManager.sceneLoaded -= OnSceneLoaded;
            }
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode loadSceneMode)
        {
            EnsureSceneRuntime();
        }

        private static void EnsureSceneRuntime()
        {
            EnsureCamera();
            EnsureGround();
            EnsurePlayer();
        }

        private static void EnsureCamera()
        {
            var mainCamera = Camera.main;

            if (mainCamera == null)
            {
                var cameraObject = new GameObject("Main Camera");
                mainCamera = cameraObject.AddComponent<Camera>();
                cameraObject.tag = "MainCamera";
                cameraObject.AddComponent<AudioListener>();
            }

            mainCamera.orthographic = true;
            mainCamera.orthographicSize = 5f;
            mainCamera.transform.position = new Vector3(0f, 0f, -10f);
            mainCamera.backgroundColor = new Color(0.08f, 0.06f, 0.14f, 1f);
            mainCamera.clearFlags = CameraClearFlags.SolidColor;
        }

        private static void EnsureGround()
        {
            var existingGround = GameObject.Find("PrototypeGround");

            if (existingGround != null)
            {
                return;
            }

            var groundObject = new GameObject("PrototypeGround");
            groundObject.transform.position = new Vector3(0f, -2.7f, 0f);

            var renderer = groundObject.AddComponent<SpriteRenderer>();
            renderer.sprite = PlaceholderSpriteFactory.CreateSquareSprite();
            renderer.color = new Color(0.12f, 0.16f, 0.24f, 1f);
            groundObject.transform.localScale = new Vector3(24f, 1.5f, 1f);

            var collider = groundObject.AddComponent<BoxCollider2D>();
            collider.size = Vector2.one;
        }

        private static void EnsurePlayer()
        {
            if (FindObjectOfType<PlayerRoot>() != null)
            {
                return;
            }

            var playerObject = new GameObject("Player");
            playerObject.transform.position = new Vector3(0f, -0.75f, 0f);

            var body = playerObject.AddComponent<Rigidbody2D>();
            body.gravityScale = 4f;
            body.freezeRotation = true;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            var collider = playerObject.AddComponent<CapsuleCollider2D>();
            collider.size = new Vector2(0.8f, 1.8f);
            collider.offset = new Vector2(0f, 0.1f);

            playerObject.AddComponent<PlayerInputReader>();
            playerObject.AddComponent<PlayerMotor>();
            playerObject.AddComponent<PlayerVisuals>();
            playerObject.AddComponent<PlayerActionRunner>();

            var root = playerObject.AddComponent<PlayerRoot>();
            var services = new GameServices(new NullAudioService(), new NullMenuService());
            root.Initialize(services);
        }
    }

    internal static class PlaceholderSpriteFactory
    {
        private static Sprite cachedSquareSprite;

        public static Sprite CreateSquareSprite()
        {
            if (cachedSquareSprite != null)
            {
                return cachedSquareSprite;
            }

            var texture = Texture2D.whiteTexture;
            cachedSquareSprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), texture.width);
            cachedSquareSprite.name = "RuntimeSquare";
            return cachedSquareSprite;
        }
    }
}
