using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Nez;
using Nez.Sprites;

namespace TeamProject3.Scene
{
    public class GameScene : Nez.Scene
    {
        private Entity _playerCharacterEntity;
        private Entity _bossEntity;
        private Entity _groundEntity;
        private const float _animationFramerate = 20.0f;
        private const float _projectileVelocity = 350.0f;
        private Vector2 _startPosition = new Vector2(250.0f, 100.0f);

        public Vector2 Viewport { get; set; } = Vector2.Zero;

        public GameScene(Vector2 viewportCenter)
        {
            Viewport = viewportCenter;
        }

        public override void Initialize()
        {
            base.Initialize();

            _playerCharacterEntity = CreateEntity("player-entity");
            _playerCharacterEntity
                .AddComponent(new Player(200.0f, _startPosition, _animationFramerate));

            _bossEntity = CreateEntity("boss-entity");
            _bossEntity.AddComponent(new Boss(new Vector2(_startPosition.X + 400,
                _startPosition.Y), BossSettings.ImportBossSettings()));

            var groundTexture = Content.Load<Texture2D>("sample_ground");
            _groundEntity = CreateEntity("ground");
            _groundEntity.AddComponent(new SpriteRenderer(groundTexture));
            var collider = _groundEntity.AddComponent<BoxCollider>();
            Flags.SetFlagExclusive(ref collider.CollidesWithLayers, 0);
            Flags.SetFlagExclusive(ref collider.PhysicsLayer, 1);

            BossSettings.ExportBossSettings();
        }

        public override void OnStart()
        {
            base.OnStart();
        }

        public override void Unload()
        {
            base.Unload();
        }

        public override void Update()
        {
            base.Update();
            ClearColor = Color.Black;

            _groundEntity.Position = new Vector2(Viewport.X / 2, 395.0f);
        }
    }
}
