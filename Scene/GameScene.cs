using Microsoft.Xna.Framework;
using Nez;

namespace TeamProject3.Scene
{
    public class GameScene : Nez.Scene
    {
        private Entity _playerCharacterEntity;
        private Entity _bossEntity;
        private const float _animationFramerate = 20.0f;
        private const float _projectileVelocity = 350.0f;
        private Vector2 _startPosition = new Vector2(250.0f);

        public GameScene()
        {
            
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

        }
    }
}
