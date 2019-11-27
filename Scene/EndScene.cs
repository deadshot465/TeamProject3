using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Nez;
using Nez.Sprites;

namespace TeamProject3.Scene
{
    public class EndScene : Nez.Scene
    {
        private Entity _gameClearEntity;
        private Entity _gameOverEntity;
        private SpriteRenderer _gameClearSprite;
        private SpriteRenderer _gameOverSprite;
        private bool _gameClear = false;

        public EndScene(bool gameClear = true)
        {
            _gameClear = gameClear;
        }

        public override void Initialize()
        {
            base.Initialize();

            var gameClear = Content.Load<Texture2D>("gameclear");
            var gameOver = Content.Load<Texture2D>("gameover");
            _gameClearEntity = CreateEntity("game-clear");
            _gameClearEntity.Position =
                new Vector2(Helper.ScreenWidth / 2, Helper.ScreenHeight / 2);
            _gameOverEntity = CreateEntity("game-over");
            _gameOverEntity.Position =
                new Vector2(Helper.ScreenWidth / 2, Helper.ScreenHeight / 2);
            _gameClearSprite = _gameClearEntity
                .AddComponent(new SpriteRenderer(gameClear));
            _gameOverSprite = _gameOverEntity
                .AddComponent(new SpriteRenderer(gameOver));
        }

        public override void Update()
        {
            base.Update();

            _gameClearSprite.Enabled = _gameClear;
            _gameOverSprite.Enabled = !_gameClear;
        }
    }
}
