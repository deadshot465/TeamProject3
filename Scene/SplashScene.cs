using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Nez;
using Nez.Sprites;

namespace TeamProject3.Scene
{
    public class SplashScene :Nez.Scene
    {
        private Entity _splashScreenEntity;

        public SplashScene()
        {
        }

        public override void Initialize()
        {
            base.Initialize();

            var splashScreen = Content.Load<Texture2D>("splash_screen");
            _splashScreenEntity = CreateEntity("splash-screen");
            _splashScreenEntity.Position =
                new Vector2(Helper.ScreenWidth / 2, Helper.ScreenHeight / 2);
            _splashScreenEntity.AddComponent(new SpriteRenderer(splashScreen));
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
        }
    }
}
